using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>Every refusal the runner reports before or instead of a verdict (exit code 2).</summary>
internal enum RunnerError
{
    Usage = 1, UndeclaredSuite, InvalidChangedPath, GitFailed, ToolchainMissing, PreviewUnavailable, ServedIdentityMismatch,
    OutputExists, Uncalibrated, RunnerBusy, NoPrimarySuite, InvalidArtifact, ProcessTimedOut,
}

/// <summary>A typed runner refusal.</summary>
internal sealed class RunnerException : Exception
{
    public RunnerException(RunnerError error, string message) : base($"{error}: {message}") => Error = error;
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

    /// <summary>Runs a short command to completion, capturing its output (git, sysctl, ps, node --version). A command still running
    /// at its limit is killed with its whole process tree and refused as ProcessTimedOut, also when the kill does not complete
    /// (the detail names its cause); so is one whose output stays open past it (a descendant still holding the pipes). A program
    /// that cannot be started (absent, not on PATH, not executable, a missing working directory, or the system unable to start a
    /// process) is a typed ToolchainMissing refusal naming it and the cause.</summary>
    public static ProcessResult Run(string file, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? limit = null)
    {
        var start = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        var wall = limit ?? DefaultLimit;
        var clock = Stopwatch.StartNew();
        using var process = Start(file, start);
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(wall))
            throw TimedOut(file, start, wall, KillTree(process));
        // Output already complete returns normally even at the limit; only output still open past it is refused.
        var remaining = wall - clock.Elapsed;
        if (!Task.WaitAll([output, error], remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero))
            throw TimedOut(file, start, wall, "it exited but its output stayed open");
        return new ProcessResult(process.ExitCode, output.Result, error.Result);
    }

    private static Process Start(string file, ProcessStartInfo start)
    {
        try { return Process.Start(start) ?? throw new InvalidOperationException($"Could not start {file}."); }
        catch (Win32Exception exception)
        {
            throw new RunnerException(RunnerError.ToolchainMissing, $"{file} could not be started ({exception.Message}); check that it is installed and on PATH, that the working directory exists and that the system can start processes.");
        }
    }

    /// <summary>How long a killed command may take to end before its kill is reported as not completed.</summary>
    public static readonly TimeSpan KillGrace = TimeSpan.FromSeconds(5);

    // Kills a process's whole tree (a short command's or a suite's) and returns the detail; a kill that does not complete is named, not thrown:
    // an AggregateException for the processes it could not end, an InvalidOperationException for one that already exited, a
    // Win32Exception or IOException when the process list cannot be read, or a root still running KillGrace after the kill.
    internal static string KillTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            return process.WaitForExit(KillGrace) ? "its process tree was killed"
                : $"killing its process tree did not complete (still running {KillGrace.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s after the kill)";
        }
        catch (Exception exception) when (exception is AggregateException or InvalidOperationException or Win32Exception or IOException)
        {
            return $"killing its process tree did not complete ({exception.Message})";
        }
    }

    private static RunnerException TimedOut(string file, ProcessStartInfo start, TimeSpan wall, string detail) =>
        new(RunnerError.ProcessTimedOut, $"{file} {string.Join(' ', start.ArgumentList)} ran past its "
            + $"{wall.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s limit; {detail}.");

    /// <summary>Runs git in a repository and returns its output, or throws a typed GitFailed refusal.</summary>
    public static string Git(string repository, params string[] arguments)
    {
        var result = Run("git", ["-C", repository, .. arguments]);
        if (result.ExitCode != 0)
            throw new RunnerException(RunnerError.GitFailed, $"git {string.Join(' ', arguments)} exited {result.ExitCode}: {result.Error.Trim()}");
        return result.Output;
    }
}
