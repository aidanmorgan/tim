using System.Collections;
using System.Diagnostics;

namespace CuriousContraptions.E2eRunner;

/// <summary>Everything one suite process needs: its label (file stem), its repository-relative path, the run's output directory,
/// its explicit environment and its wall limit.</summary>
internal sealed record SuiteLaunch(string Label, string SuitePath, string Root, string OutputDirectory,
    IReadOnlyDictionary<string, string> Environment, TimeSpan WallLimit, string TsxLoader);

/// <summary>One finished suite process: exit code, whether the wall limit stopped it, the owned processes that outlived it (each then
/// killed), and its interval.</summary>
internal sealed record SuiteRun(string Label, int ExitCode, bool TimedOut, IReadOnlyList<string> Survivors, DateTimeOffset Start, DateTimeOffset End)
{
    public string TapPath(string outputDirectory) => Path.Combine(outputDirectory, Label + ".tap");
}

internal static class SuiteProcess
{
    /// <summary>The 15-minute wall limit per file.</summary>
    public static readonly TimeSpan WallLimit = TimeSpan.FromMinutes(15);

    /// <summary>The inherited parent variables; every other parent variable (E2E_*, CLOCK_*, *_PREVIEW_URL, …) is dropped.</summary>
    public static readonly IReadOnlyList<string> Inherited = ["PATH", "HOME", "TMPDIR"];

    /// <summary>A suite's explicit environment (spec SuiteProcess task).</summary>
    public static Dictionary<string, string> Environment(IDictionary parent, string previewUrl, string outputDirectory, string label, string evidenceRoot)
    {
        ArgumentNullException.ThrowIfNull(parent);
        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in Inherited)
            if (parent[name] is string value) environment[name] = value;
        environment["E2E_PREVIEW_URL"] = previewUrl;
        environment["CLOCK_DERIVATION_LOG"] = Path.Combine(outputDirectory, label + ".derivations.jsonl");
        environment["CLOCK_EVIDENCE_SCREENSHOT"] = Path.Combine(outputDirectory, "clock-master.simulation-unresponsive.png");
        environment["CAT015B_EVIDENCE_DIR"] = Path.Combine(evidenceRoot, label + "-cat015b");
        environment["PRELOAD_EVIDENCE_DIR"] = Path.Combine(evidenceRoot, label + "-preload");
        environment["SPRING_EVIDENCE_DIR"] = Path.Combine(evidenceRoot, label + "-spring");
        return environment;
    }

    /// <summary>Runs one suite file in its own node process through the process lifecycle. At the wall limit or an abort its tree is
    /// stopped once; after the root's exit or the stop, every other process the tree was seen to hold is swept; the output is drained
    /// when the root ended; the runner's lines close the log. A root still running after the stop, or a failed sweep, is a typed
    /// ProcessNotStopped refusal naming the suite.</summary>
    public static async Task<SuiteRun> RunAsync(SuiteLaunch launch, ProcessSampler sampler, LaunchChild launcher, TimeSpan grace, CancellationToken abort)
    {
        ArgumentNullException.ThrowIfNull(launch);
        ArgumentNullException.ThrowIfNull(sampler);
        var sink = new OutputSink(Path.Combine(launch.OutputDirectory, launch.Label + ".log"), append: false, "");
        var started = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        using var child = ProcessLifecycle.Start(launcher, StartInfo(launch), sink);
        sampler.Register(child.Id);
        var (ended, aborted) = await WaitForRootAsync(child, launch.WallLimit, abort).ConfigureAwait(false);
        StopOutcome? stop = null;
        var lines = new List<string>();
        if (!ended)
        {
            sampler.Sample();
            stop = await ProcessLifecycle.StopAsync(child, grace).ConfigureAwait(false);
            // A root the kill did not end still owns its pid: one more sample finds the children it spawned during the grace.
            if (stop.Root == RootState.StillRunning) sampler.Sample();
            lines.Add($"E2E_RUNNER stop ({(aborted ? "stop requested" : $"wall limit {launch.WallLimit} reached")}): {stop.Detail}");
        }
        var finished = DateTimeOffset.UtcNow;
        var sweep = sampler.Sweep(child.Id);
        lines.AddRange(sweep.Killed.Select(survivor => $"E2E_RUNNER survivor {survivor.Pid} killed: {survivor.Command}"));
        lines.AddRange(sweep.Failures.Select(failure => "E2E_RUNNER " + Describe(failure)));
        var root = stop?.Root ?? RootState.Ended;
        var left = ended ? launch.WallLimit - clock.Elapsed : TimeSpan.Zero;
        var outputFailure = await ProcessLifecycle.FinishAsync(child, sink, $"suite {launch.Label}", root, left, grace, lines).ConfigureAwait(false);
        if (root == RootState.StillRunning || sweep.Failures.Count > 0) throw NotStopped(launch.Label, stop, sweep);
        if (outputFailure is not null) throw outputFailure;
        return new SuiteRun(launch.Label, child.ExitCode, !ended && !aborted, [.. sweep.Killed.Select(Describe)], started, finished);
    }

    private static ProcessStartInfo StartInfo(SuiteLaunch launch)
    {
        var start = new ProcessStartInfo("node") { WorkingDirectory = launch.Root };
        start.Environment.Clear();
        foreach (var (name, value) in launch.Environment) start.Environment[name] = value;
        foreach (var argument in new[]
        {
            "--import", launch.TsxLoader, "--test", "--test-concurrency=1", "--test-timeout=150000", "--test-force-exit",
            "--test-reporter=tap", "--test-reporter-destination=" + Path.Combine(launch.OutputDirectory, launch.Label + ".tap"), launch.SuitePath,
        }) start.ArgumentList.Add(argument);
        return start;
    }

    /// <summary>Waits for the root up to the wall limit: (true, false) when it exited, (false, false) at the limit, (false, true) on an abort.</summary>
    private static async Task<(bool Ended, bool Aborted)> WaitForRootAsync(IChildProcess child, TimeSpan limit, CancellationToken abort)
    {
        try { return (await child.WaitForRootAsync(limit, abort).ConfigureAwait(false), false); }
        catch (OperationCanceledException) when (abort.IsCancellationRequested) { return (false, true); }
    }

    private static string Describe(ProcessRow survivor) => $"{survivor.Pid} {survivor.Command}";

    private static string Describe(SweepFailure failure) => failure.Pid is { } pid
        ? $"survivor {pid} could not be killed: {failure.Cause}"
        : $"survivor sweep failed: {failure.Cause}";

    private static RunnerException NotStopped(string label, StopOutcome? stop, SurvivorSweep sweep)
    {
        var parts = new List<string>();
        if (stop is not null)
            parts.Add(stop.Root == RootState.StillRunning ? $"its root was still running after the kill ({stop.Detail})" : $"its stop: {stop.Detail}");
        if (sweep.Killed.Count > 0) parts.Add("survivors killed: " + string.Join(", ", sweep.Killed.Select(Describe)));
        parts.AddRange(sweep.Failures.Select(Describe));
        return new RunnerException(RunnerError.ProcessNotStopped, $"Suite {label} could not be stopped: {string.Join("; ", parts)}.");
    }
}
