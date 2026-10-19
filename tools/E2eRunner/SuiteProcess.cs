using System.Collections;
using System.Diagnostics;

namespace CuriousContraptions.E2eRunner;

/// <summary>Everything one suite process needs: its label (file stem), its repository-relative path, the run's output directory,
/// its explicit environment and its wall limit.</summary>
internal sealed record SuiteLaunch(string Label, string SuitePath, string Root, string OutputDirectory,
    IReadOnlyDictionary<string, string> Environment, TimeSpan WallLimit, string TsxLoader);

/// <summary>One finished suite process: exit code, whether the wall limit killed it, the owned processes that outlived it (each then
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

    /// <summary>Runs one suite file in its own node process; on the wall limit or an abort the whole process tree is killed and every
    /// process it held is checked gone.</summary>
    public static async Task<SuiteRun> RunAsync(SuiteLaunch launch, ProcessSampler sampler, CancellationToken abort)
    {
        ArgumentNullException.ThrowIfNull(launch);
        ArgumentNullException.ThrowIfNull(sampler);
        var start = new ProcessStartInfo("node")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = launch.Root,
        };
        start.Environment.Clear();
        foreach (var (name, value) in launch.Environment) start.Environment[name] = value;
        foreach (var argument in new[]
        {
            "--import", launch.TsxLoader, "--test", "--test-concurrency=1", "--test-timeout=150000", "--test-force-exit",
            "--test-reporter=tap", "--test-reporter-destination=" + Path.Combine(launch.OutputDirectory, launch.Label + ".tap"), launch.SuitePath,
        }) start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        using var log = new StreamWriter(Path.Combine(launch.OutputDirectory, launch.Label + ".log"), append: false) { AutoFlush = true };
        var logGate = new Lock();
        void Write(object sender, DataReceivedEventArgs line) { if (line.Data is not null) lock (logGate) log.WriteLine(line.Data); }
        process.OutputDataReceived += Write;
        process.ErrorDataReceived += Write;
        var started = DateTimeOffset.UtcNow;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        sampler.Register(process.Id);

        var timedOut = false;
        using (var limit = CancellationTokenSource.CreateLinkedTokenSource(abort))
        {
            limit.CancelAfter(launch.WallLimit);
            try { await process.WaitForExitAsync(limit.Token).ConfigureAwait(false); }
            catch (OperationCanceledException)
            {
                timedOut = !abort.IsCancellationRequested;
                sampler.Sample();
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        // WaitForExitAsync has also drained both redirected streams, so no output callback runs after this point.
        var ended = DateTimeOffset.UtcNow;
        var seen = sampler.Unregister(process.Id);
        var survivors = ProcessTable.StillRunning(seen);
        foreach (var survivor in survivors) ProcessTable.KillOwned(survivor.Pid);
        if (timedOut) await log.WriteLineAsync($"E2E_RUNNER wall limit {launch.WallLimit} reached: the process tree was killed.").ConfigureAwait(false);
        foreach (var survivor in survivors) await log.WriteLineAsync($"E2E_RUNNER survivor {survivor.Pid} killed: {survivor.Command}").ConfigureAwait(false);
        return new SuiteRun(launch.Label, process.ExitCode, timedOut, [.. survivors.Select(survivor => $"{survivor.Pid} {survivor.Command}")], started, ended);
    }
}
