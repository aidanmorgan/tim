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
}

internal static class RunEngine
{
    public static async Task<RunSummary> RunAsync(RunOptions options, RunDependencies dependencies, CancellationToken external)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dependencies);
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

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var previewLog = new StreamWriter(Path.Combine(options.OutputDirectory, "preview.log")) { AutoFlush = true };
        using var preview = await PreviewHost.StartAsync(options.Root, options.BundleDirectory, manifest, dependencies.Ports, http, previewLog,
            abort.Token).ConfigureAwait(false);
        header["preview"] = preview.BaseUri.ToString();
        header["servedCheck.start"] = FormattableString.Invariant($"pass ({manifest.Entries.Count(entry => entry.Serve == ServeStatus.Served)} files)");

        using var sampler = new ProcessSampler(TimeSpan.FromSeconds(2));
        IncompleteReason? incomplete = null;
        string? incompleteDetail = null;

        async Task<FileResult> RunOnce(ScheduledFile file, RunPhase phase, int? slot, CancellationToken token, double? loadBefore)
        {
            var environment = SuiteProcess.Environment(dependencies.ParentEnvironment, preview.BaseUri.ToString(), options.OutputDirectory,
                file.Label, evidenceRoot);
            var launch = new SuiteLaunch(file.Label, dependencies.SuitePath(file.Label), options.Root, options.OutputDirectory, environment,
                dependencies.WallLimit, dependencies.TsxLoader);
            var run = await SuiteProcess.RunAsync(launch, sampler, token).ConfigureAwait(false);
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
                var foreign = await WaitForNoForeignChromeAsync(dependencies.QuietWait, token).ConfigureAwait(false);
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

        var endManifest = BundleManifest.Compute(options.BundleDirectory);
        var servedEnd = await endManifest.ServedMismatchesAsync(http, preview.BaseUri, CancellationToken.None).ConfigureAwait(false);
        var endSources = dependencies.SuiteSources();
        header["bundleIdentity.end"] = endManifest.Identity;
        header["servedCheck.end"] = servedEnd.Count == 0 ? "pass" : $"{servedEnd.Count} mismatch(es); first {servedEnd[0]}";
        header["suiteSourceDigest.end"] = endSources;
        header["peakRssBytes"] = sampler.PeakRssBytes.ToString(CultureInfo.InvariantCulture);

        var identityDetails = new List<string>();
        if (identityChange is not null) identityDetails.Add(identityChange);
        if (endManifest.Identity != manifest.Identity) identityDetails.Add("the bundle re-hash differs from the start identity");
        if (servedEnd.Count > 0) identityDetails.Add("the end served check differs from the bundle");
        if (endSources != startSources) identityDetails.Add("the suite sources changed during the run");

        Verdict verdict;
        string? detail = null;
        if (identityDetails.Count > 0) { verdict = Verdict.IdentityChanged; detail = string.Join("; ", identityDetails); }
        else if (external.IsCancellationRequested) { verdict = Verdict.Aborted; detail = "interrupted"; }
        else if (incomplete is not null) { verdict = Verdict.Incomplete; detail = incompleteDetail; }
        else (verdict, incomplete) = RunSummary.Judge(results, [.. options.Files.Select(file => file.Label)]);
        var summary = new RunSummary(runId, options.Verb, verdict, verdict == Verdict.Incomplete ? incomplete : null, detail, header,
            options.Selection, [.. results.OrderBy(result => result.Start)], started, DateTimeOffset.UtcNow);
        summary.Write(options.OutputDirectory);
        return summary;
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
    private static async Task<IReadOnlyList<int>> WaitForNoForeignChromeAsync(TimeSpan limit, CancellationToken token)
    {
        var deadline = DateTimeOffset.UtcNow + limit;
        while (true)
        {
            var foreign = ProcessTable.Snapshot().Where(row => row.Command.StartsWith(Toolchain.ChromeBinary, StringComparison.Ordinal) &&
                row.Command.Contains(" --headless", StringComparison.Ordinal)).Select(row => row.Pid).ToArray();
            if (foreign.Length == 0 || DateTimeOffset.UtcNow >= deadline) return foreign;
            await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
        }
    }
}
