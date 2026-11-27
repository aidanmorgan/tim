using System.Collections;
using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>What one run executes and where it writes.</summary>
internal sealed record RunOptions(string Root, string OutputDirectory, string BundleDirectory, string Verb, IReadOnlyList<ScheduledFile> Files,
    int Workers, IReadOnlyDictionary<string, double>? Durations, IReadOnlyList<string> Selection,
    IReadOnlyDictionary<string, string> Header);

/// <summary>The run's collaborators, injected so the identity, process and slot behaviour can be tested without Chrome.</summary>
internal sealed class RunDependencies
{
    public required Func<string> SuiteSources { get; init; }
    public required Action VerifyArtifact { get; init; }
    public required Func<string, IBundleWatcher> Watch { get; init; }
    public required IPortSource Ports { get; init; }
    public required string TsxLoader { get; init; }
    public required IDictionary ParentEnvironment { get; init; }
    public required TimeSpan WallLimit { get; init; }
    public required Func<string, string> SuitePath { get; init; }
    /// <summary>Sample the 1-minute load average around every SerialAlone file.</summary>
    public bool SampleLoad { get; init; }
    /// <summary>A SerialAlone observation above this load is Contended (null: measure only, as calibrate does).</summary>
    public double? ContentionThreshold { get; init; }
    /// <summary>Wait for foreign headless Chromes to finish before each SerialAlone file.</summary>
    public bool CheckForeignChrome { get; init; }
    public TimeSpan QuietWait { get; init; } = TimeSpan.FromMinutes(10);
    /// <summary>The run's HTTP timeout: each request of the preview's readiness wait, and the one shared deadline of each served
    /// check.</summary>
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromMinutes(2);
}

internal static class RunEngine
{
    private const string PreviewNotResponding = "preview not responding";

    /// <summary>What the start of a run fixed: its identity, header, start manifest and start suite sources.</summary>
    private sealed record RunStart(string RunId, DateTimeOffset Started, Dictionary<string, string> Header, BundleManifest Manifest, string Sources);

    /// <summary>The end re-hash of the bundle, or the identity detail saying why it could not be taken.</summary>
    private sealed record EndHash(BundleManifest? Manifest, string? Failure);

    public static async Task<RunSummary> RunAsync(RunOptions options, RunDependencies dependencies, CancellationToken external)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dependencies);
        dependencies.VerifyArtifact();
        var started = DateTimeOffset.UtcNow;
        var runId = FormattableString.Invariant($"{started:yyyyMMddTHHmmssZ}-{Environment.ProcessId}");
        var evidenceRoot = Path.Combine(options.Root, ".anvil", "e2e-runs", runId);
        Directory.CreateDirectory(evidenceRoot);
        var header = new Dictionary<string, string>(options.Header, StringComparer.Ordinal)
        {
            ["runId"] = runId, ["workers"] = options.Workers.ToString(CultureInfo.InvariantCulture),
            ["evidenceRoot"] = evidenceRoot, ["tsxLoader"] = dependencies.TsxLoader,
            ["contentionThreshold"] = dependencies.ContentionThreshold?.ToString("F2", CultureInfo.InvariantCulture) ?? "not judged",
        };

        using var abort = CancellationTokenSource.CreateLinkedTokenSource(external);
        using var watcher = dependencies.Watch(options.BundleDirectory);
        string? identityChange = null;
        watcher.Changed += change =>
        {
            Interlocked.CompareExchange(ref identityChange, $"bundle watcher: {change}", null);
            abort.Cancel();
        };
        var startSources = dependencies.SuiteSources();
        var manifest = BundleManifest.Compute(options.BundleDirectory);
        header["suiteSourceDigest.start"] = startSources;
        header["bundleIdentity.start"] = manifest.Identity;
        header["bundleFiles"] = FormattableString.Invariant(
            $"{manifest.Entries.Count} ({manifest.Entries.Count(entry => entry.Serve == ServeStatus.NotServed)} NotServed dot-files)");
        var start = new RunStart(runId, started, header, manifest, startSources);

        using var http = new HttpClient { Timeout = dependencies.HttpTimeout };
        LaunchChild launcher = ChildProcess.Start;
        PreviewHost preview;
        try
        {
            preview = await PreviewHost.StartAsync(options.Root, options.BundleDirectory, manifest, dependencies.Ports, http,
                Path.Combine(options.OutputDirectory, "preview.log"), launcher, abort.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException cancelled) when (abort.IsCancellationRequested)
        {
            // The preview's start was stopped by a bundle change, or by the run's caller (which Program never does; Spec Change Log 3(c)):
            // the attempt in progress was stopped and no file ran; no summary is written.
            throw identityChange is { } change
                ? new RunnerException(RunnerError.ServedIdentityMismatch, $"the bundle changed while the preview was starting: {change}", cancelled)
                : new RunnerException(RunnerError.PreviewUnavailable, "the preview start was cancelled by the run's caller", cancelled);
        }
        try
        {
            header["preview"] = preview.BaseUri.ToString();
            header["servedCheck.start"] = FormattableString.Invariant($"pass ({manifest.Entries.Count(entry => entry.Serve == ServeStatus.Served)} files)");

            var table = new ProcessTable(launcher, ProcessTable.SnapshotLimit);
            using var sampler = new ProcessSampler(table, TimeSpan.FromSeconds(2));
            IncompleteReason? incomplete = null;
            string? incompleteDetail = null;

            async Task<FileResult> RunOnce(ScheduledFile file, RunPhase phase, int? slot, CancellationToken token, double? loadBefore)
            {
                var environment = SuiteProcess.Environment(dependencies.ParentEnvironment, preview.BaseUri.ToString(), options.OutputDirectory,
                    file.Label, evidenceRoot);
                var launch = new SuiteLaunch(file.Label, dependencies.SuitePath(file.Label), options.Root, options.OutputDirectory, environment,
                    dependencies.WallLimit, dependencies.TsxLoader);
                var run = await SuiteProcess.RunAsync(launch, sampler, launcher, ProcessLifecycle.Grace, token).ConfigureAwait(false);
                double? loadAfter = dependencies.SampleLoad && phase == RunPhase.SerialAlone ? ProcessTable.LoadAverage() : null;
                var contended = dependencies.ContentionThreshold is { } threshold && (loadBefore > threshold || loadAfter > threshold);
                return FileResult.Evaluate(run, options.OutputDirectory, Path.GetFileName(launch.SuitePath), phase, slot,
                    contended ? Observation.Contended : Observation.Clean, loadBefore, loadAfter);
            }

            async Task<IReadOnlyList<FileResult>> Execute(ScheduledFile file, RunPhase phase, int? slot, CancellationToken token)
            {
                if (phase == RunPhase.Parallel) return [await RunOnce(file, phase, slot, token, null).ConfigureAwait(false)];
                if (dependencies.CheckForeignChrome)
                {
                    var foreign = await WaitForNoForeignChromeAsync(table, dependencies.QuietWait, token).ConfigureAwait(false);
                    if (foreign.Count > 0)
                    {
                        incomplete = IncompleteReason.ForeignChrome;
                        incompleteDetail = $"foreign headless Chrome before {file.Label}: pids {string.Join(",", foreign)}";
                        await abort.CancelAsync().ConfigureAwait(false);
                        return [];
                    }
                }
                var before = await QuietLoadAsync(dependencies, token).ConfigureAwait(false);
                var first = await RunOnce(file, phase, slot, token, before).ConfigureAwait(false);
                if (first.Observation == Observation.Clean || token.IsCancellationRequested) return [first];
                // A contended observation is invalid, never silently replaced: it is kept, and the file runs once more when quiet.
                var again = await QuietLoadAsync(dependencies, token).ConfigureAwait(false);
                return [first, await RunOnce(file, phase, slot, token, again).ConfigureAwait(false)];
            }

            var results = await Scheduler.RunAsync(options.Files, options.Workers, options.Durations, Execute, abort.Token).ConfigureAwait(false);

            var end = Rehash(options.BundleDirectory);
            var servedEnd = end.Manifest is { } endManifest
                ? await endManifest.CheckServedAsync(http, preview.BaseUri, CancellationToken.None).ConfigureAwait(false)
                : null;
            preview.Stop();
            header["servedCheck.end"] = servedEnd switch
            {
                null => "not run: the bundle could not be re-hashed",
                { Outcome: ServedOutcome.Match } => "pass",
                { Outcome: ServedOutcome.NotServedInTime } => $"{PreviewNotResponding}: {servedEnd.Detail}",
                _ => servedEnd.Detail,
            };
            header["peakRssBytes"] = sampler.PeakRssBytes.ToString(CultureInfo.InvariantCulture);
            header["sampler"] = SamplerHeader(sampler);
            return Summarize(options, dependencies, start, end, servedEnd, results, identityChange, incomplete, incompleteDetail, external);
        }
        catch (Exception failure)
        {
            // The paths that leave early stop the preview too (nothing once it is stopped); see PreviewHost.StopAfterFailure.
            preview.StopAfterFailure(failure);
            throw;
        }
    }

    /// <summary>The end re-hash; one that fails (the bundle being rewritten or made unreadable) is recorded, not thrown.</summary>
    private static EndHash Rehash(string bundleDirectory)
    {
        try { return new EndHash(BundleManifest.Compute(bundleDirectory), null); }
        catch (Exception cause) when (cause is IOException or UnauthorizedAccessException or RunnerException)
        {
            return new EndHash(null, $"the bundle could not be re-hashed ({cause.Message})");
        }
    }

    /// <summary>Judges and writes the run's summary from its end identity checks and its file results. An end served check that ran
    /// out of time is Incomplete (PreviewNotResponding), not a bundle change.</summary>
    private static RunSummary Summarize(RunOptions options, RunDependencies dependencies, RunStart start, EndHash end,
        ServedCheck? servedEnd, IReadOnlyList<FileResult> results, string? identityChange, IncompleteReason? incomplete,
        string? incompleteDetail, CancellationToken external)
    {
        var endSources = dependencies.SuiteSources();
        start.Header["bundleIdentity.end"] = end.Manifest?.Identity ?? "not computed";
        start.Header["suiteSourceDigest.end"] = endSources;

        var identityDetails = new List<string>();
        try { dependencies.VerifyArtifact(); }
        catch (RunnerException error) { identityDetails.Add(error.Message); }
        if (identityChange is not null) identityDetails.Add(identityChange);
        if (end.Failure is not null) identityDetails.Add(end.Failure);
        else if (end.Manifest?.Identity != start.Manifest.Identity) identityDetails.Add("the bundle re-hash differs from the start identity");
        if (servedEnd is { Outcome: ServedOutcome.Mismatch or ServedOutcome.ConnectionError })
            identityDetails.Add("the end served check differs from the bundle");
        if (servedEnd is { Outcome: ServedOutcome.NotServedInTime } && incomplete is null)
        {
            incomplete = IncompleteReason.PreviewNotResponding;
            incompleteDetail = $"{PreviewNotResponding}: {servedEnd.Detail}";
        }
        if (endSources != start.Sources) identityDetails.Add("the suite sources changed during the run");

        Verdict verdict;
        string? detail = null;
        if (identityDetails.Count > 0) { verdict = Verdict.IdentityChanged; detail = string.Join("; ", identityDetails); }
        else if (external.IsCancellationRequested) { verdict = Verdict.Aborted; detail = "interrupted"; }
        else if (incomplete is not null) { verdict = Verdict.Incomplete; detail = incompleteDetail; }
        else (verdict, incomplete) = RunSummary.Judge(results, [.. options.Files.Select(file => file.Label)]);
        var summary = new RunSummary(start.RunId, options.Verb, verdict, verdict == Verdict.Incomplete ? incomplete : null, detail, start.Header,
            options.Selection, [.. results.OrderBy(result => result.Start)], start.Started, DateTimeOffset.UtcNow);
        summary.Write(options.OutputDirectory);
        return summary;
    }

    /// <summary>The sampler's skipped samples, for the run header.</summary>
    internal static string SamplerHeader(ProcessSampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        return sampler.SkippedSamples == 0
            ? "no sample skipped"
            : FormattableString.Invariant($"{sampler.SkippedSamples} sample(s) skipped; first: {sampler.FirstSkipReason}");
    }

    private static async Task<double?> QuietLoadAsync(RunDependencies dependencies, CancellationToken token)
    {
        if (!dependencies.SampleLoad) return null;
        var load = ProcessTable.LoadAverage();
        if (dependencies.ContentionThreshold is not { } threshold) return load;
        var deadline = DateTimeOffset.UtcNow + dependencies.QuietWait;
        while (load > threshold && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            load = ProcessTable.LoadAverage();
        }
        return load;
    }

    /// <summary>Foreign headless Chromes (another agent's ad-hoc run); waits up to the limit and returns the pids still running.</summary>
    private static async Task<IReadOnlyList<int>> WaitForNoForeignChromeAsync(ProcessTable table, TimeSpan limit, CancellationToken token)
    {
        var deadline = DateTimeOffset.UtcNow + limit;
        while (true)
        {
            var foreign = table.Snapshot().Where(row => row.Command.StartsWith(Toolchain.ChromeBinary, StringComparison.Ordinal) &&
                row.Command.Contains(" --headless", StringComparison.Ordinal)).Select(row => row.Pid).ToArray();
            if (foreign.Length == 0 || DateTimeOffset.UtcNow >= deadline) return foreign;
            await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
        }
    }
}
