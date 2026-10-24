using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The short-command boundary: a finished command returns its exit code and both outputs; a command past its wall limit is a
/// typed ProcessTimedOut refusal and its whole process tree is gone.</summary>
public sealed class ProcessesTests
{
    [Fact]
    public void FinishedCommandReturnsItsExitCodeAndOutputs()
    {
        var result = Processes.Run("sh", ["-c", "echo out; echo err >&2; exit 3"]);
        Assert.Equal((3, "out\n", "err\n"), (result.ExitCode, result.Output, result.Error));
    }

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
