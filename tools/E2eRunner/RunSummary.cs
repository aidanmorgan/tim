using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions.E2eRunner;

/// <summary>A run's verdict. Exit codes: 0 Pass, 1 Fail, 2 any other verdict.</summary>
internal enum Verdict { Pass = 1, Fail, Incomplete, IdentityChanged, Aborted }

/// <summary>Why a run is Incomplete.</summary>
internal enum IncompleteReason { ForeignChrome = 1, Contended, NotRun }

/// <summary>Phase S runs SerialAlone files alone; Phase P runs Parallel files on N workers.</summary>
internal enum RunPhase { SerialAlone = 1, Parallel }

/// <summary>A SerialAlone observation is Contended when the load average after it exceeded the measured idle load plus 2.</summary>
internal enum Observation { Clean = 1, Contended }

/// <summary>One file's row in the summary.</summary>
internal sealed record FileResult(
    string Label, RunPhase Phase, int? Slot, FileOutcome Outcome, IReadOnlyList<FileFailure> Failures, int ExitCode, TapResult? Tap,
    DateTimeOffset Start, DateTimeOffset End, IReadOnlyList<string> Survivors, Observation Observation, double? LoadBefore, double? LoadAfter)
{
    public double DurationMs => (End - Start).TotalMilliseconds;

    /// <summary>A finished suite process judged by the pass rule (exit code authoritative; TAP fully checked).</summary>
    public static FileResult Evaluate(SuiteRun run, string outputDirectory, string suiteFileName, RunPhase phase, int? slot,
        Observation observation = Observation.Clean, double? loadBefore = null, double? loadAfter = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        var tapPath = run.TapPath(outputDirectory);
        var tap = File.Exists(tapPath) ? TapResult.Parse(File.ReadAllText(tapPath), suiteFileName) : null;
        var failures = TapResult.Failures(run.ExitCode, tap).ToList();
        if (run.TimedOut) failures.Insert(0, FileFailure.TimedOut);
        var outcome = run.TimedOut ? FileOutcome.TimedOut : failures.Count == 0 && run.Survivors.Count == 0 ? FileOutcome.Pass : FileOutcome.Fail;
        return new FileResult(run.Label, phase, slot, outcome, failures, run.ExitCode, tap, run.Start, run.End, run.Survivors, observation,
            loadBefore, loadAfter);
    }

    public string Line()
    {
        var tap = Tap ?? new TapResult(0, 0, 0, 0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, 0, 0);
        var slot = Slot is { } value ? value.ToString(CultureInfo.InvariantCulture) : "-";
        var failures = Failures.Count > 0 ? " failures=" + string.Join(",", Failures) : "";
        var survivors = Survivors.Count > 0 ? FormattableString.Invariant($" survivors={Survivors.Count}") : "";
        return FormattableString.Invariant(
            $"{Label} outcome={Outcome} exit={ExitCode} tests={tap.Tests} pass={tap.Pass} fail={tap.Fail} cancelled={tap.Cancelled} skipped={tap.Skipped} todo={tap.Todo} notok={tap.NotOk} closeFindings={tap.CloseFindings} unresponsive={tap.Unresponsive} rejected={tap.Rejected} recal={tap.Recalibrated} renewedLate={tap.RenewedLate} lateRenewalsReported={tap.LateRenewalsReported} wideOffset={tap.WideOffset} debtFaults={tap.DebtFaults} durationMs={DurationMs:F0} phase={Phase} slot={slot} start={Start:O} end={End:O} observation={Observation}{failures}{survivors}");
    }
}

/// <summary>The run header and rows written to summary.txt and summary.json.</summary>
internal sealed record RunSummary(
    string RunId, string Verb, Verdict Verdict, IncompleteReason? Incomplete, string? Detail,
    IReadOnlyDictionary<string, string> Header, IReadOnlyList<string> Selection, IReadOnlyList<FileResult> Files,
    DateTimeOffset Started, DateTimeOffset Finished)
{
    public static int ExitCode(Verdict verdict) => verdict switch
    {
        Verdict.Pass => 0,
        Verdict.Fail => 1,
        Verdict.Incomplete or Verdict.IdentityChanged or Verdict.Aborted => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Undefined verdict."),
    };

    /// <summary>Fail when any record failed or timed out (a contended failure included: it is never erased); Pass only when every
    /// scheduled file has a clean passing record; otherwise Incomplete (Contended when only contended passes exist for a file).</summary>
    public static (Verdict Verdict, IncompleteReason? Reason) Judge(IReadOnlyList<FileResult> files, IReadOnlyList<string> scheduled)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(scheduled);
        if (files.Any(file => file.Outcome is FileOutcome.Fail or FileOutcome.TimedOut)) return (Verdict.Fail, null);
        var missing = scheduled.Where(label => !files.Any(file => file.Label == label && file.Outcome == FileOutcome.Pass &&
            file.Observation == Observation.Clean)).ToArray();
        if (missing.Length == 0) return (Verdict.Pass, null);
        return (Verdict.Incomplete, missing.All(label => files.Any(file => file.Label == label)) ? IncompleteReason.Contended : IncompleteReason.NotRun);
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true, Converters = { new JsonStringEnumConverter() }, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Write(string outputDirectory)
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"run {RunId} verb={Verb} verdict={Verdict}{(Incomplete is { } reason ? $"({reason})" : "")}");
        if (Detail is not null) text.AppendLine(CultureInfo.InvariantCulture, $"detail: {Detail}");
        text.AppendLine(CultureInfo.InvariantCulture, $"started={Started:O} finished={Finished:O} wallSeconds={(Finished - Started).TotalSeconds:F1}");
        foreach (var (key, value) in Header) text.AppendLine(CultureInfo.InvariantCulture, $"{key}: {value}");
        foreach (var line in Selection) text.AppendLine(CultureInfo.InvariantCulture, $"selection: {line}");
        foreach (var file in Files) text.AppendLine(file.Line());
        var totals = Files.Select(file => file.Tap).OfType<TapResult>().ToArray();
        text.AppendLine(CultureInfo.InvariantCulture,
            $"run totals: files={Files.Count} pass={Files.Count(file => file.Outcome == FileOutcome.Pass)} nonzeroExit={Files.Count(file => file.ExitCode != 0)} " +
            $"tests={totals.Sum(tap => tap.Tests)} testsPassed={totals.Sum(tap => tap.Pass)} fail={totals.Sum(tap => tap.Fail)} notok={totals.Sum(tap => tap.NotOk)} " +
            $"closeFindings={totals.Sum(tap => tap.CloseFindings)} recal={totals.Sum(tap => tap.Recalibrated)} renewedLate={totals.Sum(tap => tap.RenewedLate)} " +
            $"lateRenewalsReported={totals.Sum(tap => tap.LateRenewalsReported)} wideOffset={totals.Sum(tap => tap.WideOffset)} debtFaults={totals.Sum(tap => tap.DebtFaults)}");
        File.WriteAllText(Path.Combine(outputDirectory, "summary.txt"), text.ToString());
        File.WriteAllText(Path.Combine(outputDirectory, "summary.json"), JsonSerializer.Serialize(this, Json));
    }

    /// <summary>Per-file durations (ms) from an earlier summary.json, for longest-first ordering.</summary>
    public static IReadOnlyDictionary<string, double> Durations(string summaryJson)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(summaryJson));
        return document.RootElement.GetProperty(nameof(Files)).EnumerateArray()
            .ToDictionary(file => file.GetProperty(nameof(FileResult.Label)).GetString()!,
                file => file.GetProperty(nameof(FileResult.DurationMs)).GetDouble(), StringComparer.Ordinal);
    }
}
