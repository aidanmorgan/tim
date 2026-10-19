using System.Globalization;
using System.Text.RegularExpressions;

namespace CuriousContraptions.E2eRunner;

/// <summary>A file's outcome. Pass needs every condition of the spec's pass rule; TimedOut is a killed file; NotRun was never started.</summary>
internal enum FileOutcome { Pass = 1, Fail, TimedOut, NotRun }

/// <summary>Why a file failed (all that apply are reported).</summary>
internal enum FileFailure { NonZeroExit = 1, IncompletePlan, NoTests, FailedTests, CancelledTests, SkippedTests, TodoTests, NotOkLine, TimedOut }

/// <summary>The signals the driver prints as `E2E_OBSERVED &lt;signal&gt; …` (workshop-driver.ts), counted per file.</summary>
internal enum ObservedSignal { DebtFault = 1, ClockRecalibrated, ClockReplyRejected, ClockRenewedLate, ClockOffsetWide, SimulationUnresponsive }

/// <summary>One suite file's parsed TAP and console signals (the candidate-5 column names, plus debtFaults).</summary>
internal sealed partial record TapResult(
    int Tests, int Pass, int Fail, int Cancelled, int Skipped, int Todo, int NotOk, bool PlanComplete,
    int CloseFindings, int Unresponsive, int Rejected, int Recalibrated, int RenewedLate, int LateRenewalsReported, int WideOffset,
    int DebtFaults)
{
    [GeneratedRegex(@"^# (tests|pass|fail|cancelled|skipped|todo) (\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex Counter();
    [GeneratedRegex(@"^1\.\.(\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex TopLevelPlan();
    [GeneratedRegex(@"^(not )?ok (\d+) - (.*)$", RegexOptions.Multiline)]
    private static partial Regex TopLevelPoint();
    [GeneratedRegex(@"^\s*not ok \d+", RegexOptions.Multiline)]
    private static partial Regex AnyNotOk();
    [GeneratedRegex(@"^# E2E_OBSERVED (\w+)\b", RegexOptions.Multiline)]
    private static partial Regex Observed();

    /// <summary>The TAP boundary. A test point named after the suite file itself is node's synthetic per-file entry (an empty file
    /// or a file-level failure), not a test, so it is excluded from <see cref="Tests"/>.</summary>
    public static TapResult Parse(string tap, string suiteFileName)
    {
        ArgumentNullException.ThrowIfNull(tap);
        int Count(string name) => Counter().Matches(tap).Where(match => match.Groups[1].Value == name)
            .Select(match => int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)).LastOrDefault();
        var points = TopLevelPoint().Matches(tap);
        var plan = TopLevelPlan().Matches(tap).LastOrDefault();
        var planComplete = plan is not null && int.Parse(plan.Groups[1].Value, CultureInfo.InvariantCulture) == points.Count && points.Count > 0;
        var synthetic = points.Count(point => IsFileEntry(point.Groups[3].Value.Trim(), suiteFileName));
        var signals = Observed().Matches(tap).Select(match => match.Groups[1].Value).ToArray();
        int Signal(ObservedSignal signal) => signals.Count(name => name == signal.ToString());
        int Lines(string text) => tap.Split('\n').Count(line => line.StartsWith("# ", StringComparison.Ordinal) && line.Contains(text, StringComparison.Ordinal));
        return new TapResult(
            Tests: Math.Max(0, Count("tests") - synthetic), Pass: Count("pass"), Fail: Count("fail"), Cancelled: Count("cancelled"),
            Skipped: Count("skipped"), Todo: Count("todo"),
            NotOk: AnyNotOk().Count(tap), PlanComplete: planComplete,
            CloseFindings: Lines("Session close finding"), Unresponsive: Signal(ObservedSignal.SimulationUnresponsive),
            Rejected: Signal(ObservedSignal.ClockReplyRejected), Recalibrated: Signal(ObservedSignal.ClockRecalibrated),
            RenewedLate: Signal(ObservedSignal.ClockRenewedLate), LateRenewalsReported: Lines("Session clock late renewals"),
            WideOffset: Signal(ObservedSignal.ClockOffsetWide), DebtFaults: Signal(ObservedSignal.DebtFault));
    }

    private static bool IsFileEntry(string name, string suiteFileName) =>
        name == suiteFileName || name.EndsWith("/" + suiteFileName, StringComparison.Ordinal);

    /// <summary>The pass rule: exit 0, a complete plan, tests &gt; 0, no failed, cancelled, skipped or todo test, and no `not ok` line.</summary>
    public static IReadOnlyList<FileFailure> Failures(int exitCode, TapResult? tap)
    {
        var failures = new List<FileFailure>();
        if (exitCode != 0) failures.Add(FileFailure.NonZeroExit);
        if (tap is null) { failures.Add(FileFailure.IncompletePlan); failures.Add(FileFailure.NoTests); return failures; }
        if (!tap.PlanComplete) failures.Add(FileFailure.IncompletePlan);
        if (tap.Tests <= 0) failures.Add(FileFailure.NoTests);
        if (tap.Fail > 0) failures.Add(FileFailure.FailedTests);
        if (tap.Cancelled > 0) failures.Add(FileFailure.CancelledTests);
        if (tap.Skipped > 0) failures.Add(FileFailure.SkippedTests);
        if (tap.Todo > 0) failures.Add(FileFailure.TodoTests);
        if (tap.NotOk > 0) failures.Add(FileFailure.NotOkLine);
        return failures;
    }
}
