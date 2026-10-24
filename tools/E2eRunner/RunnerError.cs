using System.Diagnostics;
using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>Every refusal the runner reports before or instead of a verdict (exit code 2).</summary>
internal enum RunnerError
{
    Usage = 1, UndeclaredSuite, InvalidChangedPath, GitFailed, ToolchainMissing, PreviewUnavailable, ServedIdentityMismatch,
    OutputExists, Uncalibrated, RunnerBusy, NoPrimarySuite, PublishFailed, ProcessTimedOut,
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
    /// at its limit is killed with its whole process tree and refused as ProcessTimedOut; so is one whose output stays open past it
    /// (a descendant still holding the pipes).</summary>
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
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {file}.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(wall))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw TimedOut(file, start, wall, "its process tree was killed");
        }
        var remaining = wall - clock.Elapsed;
        if (remaining <= TimeSpan.Zero || !Task.WaitAll([output, error], remaining))
            throw TimedOut(file, start, wall, "it exited but its output stayed open");
        return new ProcessResult(process.ExitCode, output.Result, error.Result);
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
