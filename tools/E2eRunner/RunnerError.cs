using System.Diagnostics;

namespace CuriousContraptions.E2eRunner;

/// <summary>Every refusal the runner reports before or instead of a verdict (exit code 2).</summary>
internal enum RunnerError
{
    Usage = 1, UndeclaredSuite, InvalidChangedPath, GitFailed, ToolchainMissing, PreviewUnavailable, ServedIdentityMismatch,
    OutputExists, Uncalibrated, RunnerBusy, NoPrimarySuite, PublishFailed,
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
    /// <summary>Runs a short command to completion, capturing its output (git, sysctl, ps, node --version).</summary>
    public static ProcessResult Run(string file, IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var start = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {file}.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output, error.GetAwaiter().GetResult());
    }

    /// <summary>Runs git in a repository and returns its output, or throws a typed GitFailed refusal.</summary>
    public static string Git(string repository, params string[] arguments)
    {
        var result = Run("git", ["-C", repository, .. arguments]);
        if (result.ExitCode != 0)
            throw new RunnerException(RunnerError.GitFailed, $"git {string.Join(' ', arguments)} exited {result.ExitCode}: {result.Error.Trim()}");
        return result.Output;
    }
}
