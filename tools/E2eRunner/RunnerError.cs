using System.Diagnostics;

namespace CuriousContraptions.E2eRunner;

/// <summary>Every refusal the runner reports before or instead of a verdict (exit code 2).</summary>
internal enum RunnerError
{
    Usage = 1, UndeclaredSuite, InvalidChangedPath, GitFailed, ToolchainMissing, PreviewUnavailable, ServedIdentityMismatch,
    OutputExists, Uncalibrated, RunnerBusy, NoPrimarySuite, InvalidArtifact, ProcessTimedOut, ProcessNotStarted, ProcessNotStopped,
    ProcessTableUnreadable, ProcessOutputFailed, SystemQueryFailed,
}

/// <summary>A typed runner refusal.</summary>
internal sealed class RunnerException : Exception
{
    public RunnerException(RunnerError error, string message) : base($"{error}: {message}") => Error = error;
    public RunnerException(RunnerError error, string message, Exception innerException) : base($"{error}: {message}", innerException) => Error = error;
    public RunnerException() : base(RunnerError.Usage.ToString()) => Error = RunnerError.Usage;
    public RunnerException(string message) : base(message) => Error = RunnerError.Usage;
    public RunnerException(string message, Exception innerException) : base(message, innerException) => Error = RunnerError.Usage;

    public RunnerError Error { get; }
}

/// <summary>The result of a short child process run to completion.</summary>
internal sealed record ProcessResult(int ExitCode, string Output, string Error);

internal static class Processes
{
    /// <summary>The longest a short command may run, output included, unless its caller gives another limit.</summary>
    public static readonly TimeSpan DefaultLimit = TimeSpan.FromMinutes(10);

    /// <summary>Runs a short command to completion through the process lifecycle and returns its exit code and the exact text of both
    /// outputs (git, sysctl, ps, node --version). A program that cannot start is refused as ProcessNotStarted. A command still running
    /// at its limit is stopped (its tree killed once, then one grace wait for its root) and refused as ProcessTimedOut when its root
    /// ended, or ProcessNotStopped when it still runs; its captured text is discarded. A command that exited but whose output stays
    /// open past max(time left before its limit, grace) is refused as ProcessTimedOut; an output it cannot read as ProcessOutputFailed.</summary>
    public static ProcessResult Run(string file, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? limit = null) =>
        Run(ChildProcess.Start, ProcessLifecycle.Grace, file, arguments, workingDirectory, limit ?? DefaultLimit);

    /// <summary><see cref="Run(string, IEnumerable{string}, string?, TimeSpan?)"/> through a given launcher and grace.</summary>
    internal static ProcessResult Run(LaunchChild launch, TimeSpan grace, string file, IEnumerable<string> arguments, string? workingDirectory,
        TimeSpan limit)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var start = new ProcessStartInfo(file) { WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory() };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return RunAsync(launch, grace, start, limit).GetAwaiter().GetResult();
    }

    private static async Task<ProcessResult> RunAsync(LaunchChild launch, TimeSpan grace, ProcessStartInfo start, TimeSpan limit)
    {
        var output = new CapturedOutput();
        var clock = Stopwatch.StartNew();
        using var child = launch(start, output);
        if (!await child.WaitForRootAsync(limit, CancellationToken.None).ConfigureAwait(false))
        {
            var stop = await ProcessLifecycle.StopAsync(child, grace).ConfigureAwait(false);
            throw stop.Root == RootState.Ended
                ? TimedOut(start, limit, stop.Detail)
                : new RunnerException(RunnerError.ProcessNotStopped, $"{Command(start)} ran past its {ProcessLifecycle.Seconds(limit)} s limit "
                    + $"and its root was still running {ProcessLifecycle.Seconds(grace)} s after the kill ({stop.Detail}).");
        }
        if (!await ProcessLifecycle.DrainAsync(child, start.FileName, limit - clock.Elapsed, grace).ConfigureAwait(false))
            throw TimedOut(start, limit, "it exited but its output stayed open");
        return new ProcessResult(child.ExitCode, output.Output, output.Error);
    }

    private static string Command(ProcessStartInfo start) => $"{start.FileName} {string.Join(' ', start.ArgumentList)}";

    private static RunnerException TimedOut(ProcessStartInfo start, TimeSpan limit, string detail) =>
        new(RunnerError.ProcessTimedOut, $"{Command(start)} ran past its {ProcessLifecycle.Seconds(limit)} s limit; {detail}.");

    /// <summary>Runs git in a repository and returns its output, or throws a typed GitFailed refusal.</summary>
    public static string Git(string repository, params string[] arguments)
    {
        var result = Run("git", ["-C", repository, .. arguments]);
        if (result.ExitCode != 0)
            throw new RunnerException(RunnerError.GitFailed, $"git {string.Join(' ', arguments)} exited {result.ExitCode}: {result.Error.Trim()}");
        return result.Output;
    }
}
