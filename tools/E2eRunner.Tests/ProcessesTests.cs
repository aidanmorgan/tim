using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The short-command boundary: a finished command returns its exit code and both outputs; a program that cannot start is
/// ProcessNotStarted (L1); a command past its limit is stopped once and refused (L4); a command whose output outlives it is refused
/// (L5). The real process handle separates the root's exit from the end of its output (L6).</summary>
public sealed class ProcessesTests
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(5);

    /// <summary>A generous stall guard for a wait on a real process, not a timing assertion.</summary>
    private static readonly TimeSpan StallGuard = TimeSpan.FromSeconds(30);

    public enum ReceiverKind { Captured, Sink }

    [Fact]
    public void FinishedCommandReturnsItsExitCodeAndOutputs()
    {
        var result = Processes.Run("sh", ["-c", "echo out; echo err >&2; exit 3"]);
        Assert.Equal((3, "out\n", "err\n"), (result.ExitCode, result.Output, result.Error));
    }

    // L1: a program absent from PATH, and a program whose working directory is missing, are ProcessNotStarted naming the program,
    // the cause and the remedy. Control: the same program in an existing directory starts.
    [Fact]
    public void AProgramThatCannotStartIsAProcessNotStartedRefusal()
    {
        Assert.Equal(0, Processes.Run("sh", ["-c", "exit 0"]).ExitCode);
        var absent = Assert.Throws<RunnerException>(() => Processes.Run("e2e-runner-absent-program", []));
        var missing = Path.Combine(Path.GetTempPath(), "e2e-missing-" + Guid.NewGuid().ToString("N"));
        var noDirectory = Assert.Throws<RunnerException>(() => Processes.Run("sh", ["-c", "exit 0"], missing));
        Assert.Equal(RunnerError.ProcessNotStarted, absent.Error);
        Assert.Equal(RunnerError.ProcessNotStarted, noDirectory.Error);
        Assert.StartsWith("ProcessNotStarted: e2e-runner-absent-program could not start (", absent.Message, StringComparison.Ordinal);
        Assert.StartsWith("ProcessNotStarted: sh could not start (", noDirectory.Message, StringComparison.Ordinal);
        Assert.Contains("create the working directory", noDirectory.Message, StringComparison.Ordinal);
    }

    // L4 (real): a tree past its limit is killed once; the refusal says so and the grandchild is gone. Elapsed < 30 s and the 5 s
    // reap poll are generous stall guards (1 s limit plus 5 s grace), not timing assertions.
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

    // L4 (fake): a root the kill does not end is ProcessNotStopped after one limit wait, one kill and one grace wait, with no output
    // wait (its captured text is discarded).
    [Fact]
    public void ACommandWhoseRootSurvivesTheKillIsNotStopped()
    {
        using var child = new FakeChild([RootAnswer.AtLimit, RootAnswer.AtLimit]);
        var limit = TimeSpan.FromSeconds(1);
        var refusal = Assert.Throws<RunnerException>(() => Processes.Run(new FakeLauncher(child).Launch, Grace, "stuck", ["--forever"], null, limit));
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Equal(["root wait", "kill tree", "root wait"], child.Events);
        Assert.Equal([limit, Grace], child.RootWaits);
        Assert.Empty(child.OutputWaits);
    }

    // L5 control: a root that exited with its output complete returns its exit code and the exact text of each stream.
    [Fact]
    public void ACommandWhoseOutputEndsReturnsItsResult()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]) { ExitCode = 7 };
        var launcher = new FakeLauncher(child, receiver =>
        {
            receiver.Write(OutputStream.Standard, "out\0");
            receiver.Write(OutputStream.Error, "err");
        });
        var result = Processes.Run(launcher.Launch, Grace, "fake", [], null, Processes.DefaultLimit);
        Assert.Equal(new ProcessResult(7, "out\0", "err"), result);
    }

    // L5: a root that exited while its output stays open is ProcessTimedOut, after one output wait bounded by the time left (at least
    // the grace, at most the limit; here almost the whole limit, as the fake run takes milliseconds), and nothing is killed.
    [Fact]
    public void ACommandWhoseOutputOutlivesItIsRefused()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.StillOpen]);
        var limit = TimeSpan.FromMinutes(10);
        var refusal = Assert.Throws<RunnerException>(() => Processes.Run(new FakeLauncher(child).Launch, Grace, "fake", [], null, limit));
        Assert.Equal(RunnerError.ProcessTimedOut, refusal.Error);
        Assert.EndsWith("it exited but its output stayed open.", refusal.Message, StringComparison.Ordinal);
        Assert.InRange(Assert.Single(child.OutputWaits), limit - StallGuard, limit);
        Assert.Equal(0, child.Kills);
    }

    // L5 boundary: at a zero limit an exited root's complete output is still drained, for exactly the grace.
    [Fact]
    public void AZeroLimitStillDrainsForTheGrace()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]);
        var result = Processes.Run(new FakeLauncher(child).Launch, Grace, "fake", [], null, TimeSpan.Zero);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal([Grace], child.OutputWaits);
    }

    // L6: with either receiver, the root's exit wait returns while a descendant still holds the output, and the output wait does not
    // end until that descendant is killed; then both ends of file are delivered.
    [Theory]
    [InlineData(ReceiverKind.Captured)]
    [InlineData(ReceiverKind.Sink)]
    public async Task TheRealHandleSeparatesExitFromEndOfOutput(ReceiverKind kind)
    {
        var token = TestContext.Current.CancellationToken;
        var scratch = Directory.CreateTempSubdirectory("e2e-handle-");
        var pidFile = Path.Combine(scratch.FullName, "holder.pid");
        try
        {
            var captured = new CapturedOutput();
            using var log = new StringWriter();
            var receiver = new RecordingReceiver(kind == ReceiverKind.Captured ? captured : new OutputSink(log, ""));
            using var child = ChildProcess.Start(Sh($"echo started; sleep 60 & echo $! > '{pidFile}'", scratch.FullName), receiver);
            Assert.True(await child.WaitForRootAsync(StallGuard, token));
            Assert.False(await child.WaitForOutputEndAsync(TimeSpan.FromSeconds(1)));
            Assert.Empty(receiver.Ends);
            KillHolder(pidFile);
            Assert.True(await child.WaitForOutputEndAsync(StallGuard));
            Assert.Equal([OutputStream.Standard, OutputStream.Error], receiver.Ends);
            Assert.Equal("started", (kind == ReceiverKind.Captured ? captured.Output : log.ToString()).Trim());
        }
        finally
        {
            KillHolder(pidFile);
            scratch.Delete(true);
        }
    }

    // L6: with either receiver, every one of 20,000 lines on each stream and both ends of file are delivered by the time the output
    // wait returns.
    [Theory]
    [InlineData(ReceiverKind.Captured)]
    [InlineData(ReceiverKind.Sink)]
    public async Task TheRealHandleDeliversEveryLineAndBothEndsOfFile(ReceiverKind kind)
    {
        var token = TestContext.Current.CancellationToken;
        var captured = new CapturedOutput();
        using var log = new StringWriter();
        var sink = new OutputSink(log, "");
        var lines = new Dictionary<OutputStream, List<string>> { [OutputStream.Standard] = [], [OutputStream.Error] = [] };
        sink.LineWritten += (stream, line) => { lock (lines) lines[stream].Add(line); };
        var receiver = new RecordingReceiver(kind == ReceiverKind.Captured ? captured : sink);
        using var child = ChildProcess.Start(Sh("seq 1 20000; seq 1 20000 >&2", Path.GetTempPath()), receiver);
        Assert.True(await child.WaitForRootAsync(StallGuard, token));
        Assert.True(await child.WaitForOutputEndAsync(StallGuard));
        Assert.Equal([OutputStream.Standard, OutputStream.Error], receiver.Ends);
        var numbers = Enumerable.Range(1, 20000).Select(number => number.ToString(CultureInfo.InvariantCulture)).ToArray();
        if (kind == ReceiverKind.Captured)
        {
            var text = string.Concat(numbers.Select(number => number + "\n"));
            Assert.Equal(text, captured.Output);
            Assert.Equal(text, captured.Error);
        }
        else
        {
            Assert.Equal(numbers, lines[OutputStream.Standard]);
            Assert.Equal(numbers, lines[OutputStream.Error]);
        }
    }

    // L6 boundary: a token cancelled before the root wait throws even though the root has already exited.
    [Fact]
    public async Task ACancelledTokenThrowsAtTheRootWaitAfterTheRootExited()
    {
        using var child = ChildProcess.Start(Sh("exit 0", Path.GetTempPath()), new CapturedOutput());
        Assert.True(await child.WaitForRootAsync(StallGuard, TestContext.Current.CancellationToken));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => child.WaitForRootAsync(StallGuard, cancelled.Token));
    }

    // L6 boundary: disposing the handle while a quiet descendant holds the output ends the output wait, and no end of file is passed.
    [Fact]
    public async Task DisposingTheHandleEndsItsOutputWaitWithoutAnEnd()
    {
        var scratch = Directory.CreateTempSubdirectory("e2e-dispose-");
        var pidFile = Path.Combine(scratch.FullName, "holder.pid");
        var receiver = new RecordingReceiver(new CapturedOutput());
        var child = ChildProcess.Start(Sh($"sleep 60 & echo $! > '{pidFile}'", scratch.FullName), receiver);
        try
        {
            Assert.True(await child.WaitForRootAsync(StallGuard, TestContext.Current.CancellationToken));
            child.Dispose();
            Assert.True(await child.WaitForOutputEndAsync(StallGuard));
            Assert.Empty(receiver.Ends);
        }
        finally
        {
            child.Dispose();
            KillHolder(pidFile);
            scratch.Delete(true);
        }
    }

    // L6 boundary: a receiver that throws on its first write makes the drain a ProcessOutputFailed refusal naming the cause, at once,
    // although a descendant still holds the other stream open.
    [Fact]
    public async Task AReceiverFaultIsAProcessOutputFailedRefusal()
    {
        var scratch = Directory.CreateTempSubdirectory("e2e-fault-");
        var pidFile = Path.Combine(scratch.FullName, "holder.pid");
        try
        {
            using var child = ChildProcess.Start(Sh($"sleep 60 > /dev/null & echo $! > '{pidFile}'; echo hi", scratch.FullName), new ThrowingReceiver());
            Assert.True(await child.WaitForRootAsync(StallGuard, TestContext.Current.CancellationToken));
            var refusal = await Assert.ThrowsAsync<RunnerException>(() => ProcessLifecycle.DrainAsync(child, "sh", StallGuard, Grace));
            Assert.Equal(RunnerError.ProcessOutputFailed, refusal.Error);
            Assert.Contains(ThrowingReceiver.Cause, refusal.Message, StringComparison.Ordinal);
        }
        finally
        {
            KillHolder(pidFile);
            scratch.Delete(true);
        }
    }

    private static ProcessStartInfo Sh(string script, string workingDirectory)
    {
        var start = new ProcessStartInfo("sh") { WorkingDirectory = workingDirectory };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(script);
        return start;
    }

    /// <summary>Kills the descendant whose pid the script recorded, if it recorded one (a pid already gone is ignored).</summary>
    private static void KillHolder(string pidFile)
    {
        if (!File.Exists(pidFile)) return;
        var pid = File.ReadAllText(pidFile).Trim();
        if (pid.Length > 0) Processes.Run("sh", ["-c", $"kill -9 {pid} 2>/dev/null; true"]);
    }

    /// <summary>The process no longer runs. On Linux a killed orphan can stay a zombie until an init without reaping (a container's)
    /// collects it, so /proc's state Z counts as gone; elsewhere `kill -0` must fail.</summary>
    internal static bool Gone(int pid)
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
