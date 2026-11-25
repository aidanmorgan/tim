using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The short-command boundary: a finished command returns its exit code and both outputs; a command past its wall limit is a
/// typed ProcessTimedOut refusal and its whole process tree is gone; a program that cannot be started is a typed ToolchainMissing
/// refusal naming it.</summary>
public sealed class ProcessesTests
{
    [Fact]
    public void FinishedCommandReturnsItsExitCodeAndOutputs()
    {
        var result = Processes.Run("sh", ["-c", "echo out; echo err >&2; exit 3"]);
        Assert.Equal((3, "out\n", "err\n"), (result.ExitCode, result.Output, result.Error));
    }

    // A program name no PATH holds (PATH itself is left alone, since tests run in parallel): the start failure is a typed
    // ToolchainMissing refusal naming the program, not an untyped Win32Exception. Control: an existing program runs. Boundary: an
    // existing program in a working directory that does not exist is the same refusal.
    [Fact]
    public void AProgramThatCannotStartIsAToolchainMissingRefusalNamingIt()
    {
        Assert.Equal(0, Processes.Run("sh", ["-c", "exit 0"]).ExitCode);

        var refusal = Assert.Throws<RunnerException>(() => Processes.Run("e2e-runner-no-such-program", ["--version"]));
        Assert.Equal(RunnerError.ToolchainMissing, refusal.Error);
        Assert.Contains("e2e-runner-no-such-program could not be started (", refusal.Message, StringComparison.Ordinal);
        Assert.EndsWith(StartRemedy, refusal.Message, StringComparison.Ordinal);

        var scratch = Directory.CreateTempSubdirectory("e2e-processes-");
        try
        {
            var missing = Path.Combine(scratch.FullName, "no-such-directory");
            var directory = Assert.Throws<RunnerException>(() => Processes.Run("sh", ["-c", "true"], missing));
            Assert.Equal(RunnerError.ToolchainMissing, directory.Error);
            Assert.Contains("sh could not be started (", directory.Message, StringComparison.Ordinal);
            Assert.EndsWith(StartRemedy, directory.Message, StringComparison.Ordinal);
        }
        finally { scratch.Delete(true); }
    }

    private const string StartRemedy = "); check that it is installed and on PATH, that the working directory exists and that the system can start processes.";

    [Fact]
    public void CommandPastItsLimitIsRefusedAndItsWholeTreeKilled()
    {
        var scratch = Directory.CreateTempSubdirectory("e2e-processes-");
        try
        {
            var pidFile = Path.Combine(scratch.FullName, "child.pid");
            var clock = Stopwatch.StartNew();
            var refusal = Assert.Throws<RunnerException>(() =>
                Processes.Run("sh", ["-c", $"sleep 60 & echo $! > '{pidFile}'; wait"], scratch.FullName, TimeSpan.FromSeconds(1)));
            Assert.Equal(RunnerError.ProcessTimedOut, refusal.Error);
            Assert.EndsWith("its process tree was killed.", refusal.Message, StringComparison.Ordinal);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(30), $"refused after {clock.Elapsed}");
            var child = int.Parse(File.ReadAllText(pidFile).Trim(), CultureInfo.InvariantCulture);
            Assert.True(Gone(child), $"the grandchild sleep {child} survived the kill");
        }
        finally { scratch.Delete(true); }
    }

    /// <summary>The process no longer runs. On Linux a killed orphan can stay a zombie until an init without reaping (a container's)
    /// collects it, so /proc's state Z counts as gone; elsewhere `kill -0` must fail.</summary>
    private static bool Gone(int pid)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (!Running(pid)) return true;
            Thread.Sleep(100);
        }
        return false;
    }

    private static bool Running(int pid)
    {
        if (Directory.Exists("/proc"))
        {
            var stat = Path.Combine("/proc", pid.ToString(CultureInfo.InvariantCulture), "stat");
            string fields;
            try { fields = File.ReadAllText(stat); }
            catch (IOException) { return false; } // the process (and its /proc entry) is gone
            return fields[(fields.LastIndexOf(')') + 2)..][0] != 'Z';
        }
        return Processes.Run("sh", ["-c", $"kill -0 {pid.ToString(CultureInfo.InvariantCulture)}"]).ExitCode == 0;
    }
}
