using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>L8: one suite process through the lifecycle over a scripted handle and process table (the operating-system boundary),
/// the real sampler and the real suite log, in every state: wall limit, abort, stuck root, refused sweep, open output, output fault and
/// a refused start.</summary>
public sealed class SuiteLifecycleTests : IDisposable
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Wall = TimeSpan.FromMinutes(15);
    /// <summary>A generous allowance for the time a fake run takes between its start and its drain (it takes milliseconds).</summary>
    private static readonly TimeSpan StallGuard = TimeSpan.FromSeconds(30);
    private static readonly ProcessRow Root = new(100, 1, 10, "node suite");
    private static readonly ProcessRow First = new(101, 100, 10, "chrome first");
    private static readonly ProcessRow Second = new(102, 100, 10, "chrome second");
    private const string LimitLine = "E2E_RUNNER stop (wall limit 00:15:00 reached): its process tree was killed";

    private readonly DirectoryInfo _output = Directory.CreateTempSubdirectory("e2e-suite-lifecycle-");
    private readonly List<string> _events = [];

    public void Dispose() => _output.Delete(true);

    private SuiteLaunch Launch() =>
        new("fake", "tools/e2e/fake.test.ts", _output.FullName, _output.FullName, new Dictionary<string, string>(), Wall, "loader.mjs");

    private string[] Log() => File.ReadAllLines(Path.Combine(_output.FullName, "fake.log"));

    private async Task<SuiteRun> RunAsync(FakeTable table, LaunchChild launch, CancellationToken abort)
    {
        using var sampler = new ProcessSampler(table, Timeout.InfiniteTimeSpan);
        return await SuiteProcess.RunAsync(Launch(), sampler, launch, Grace, abort);
    }

    // Limit: the root is stopped, the survivor swept and only then the output drained, for the grace (no time is left after a stop);
    // the log ends with the stop line and the survivor line; the root's pid is never killed by the sweep.
    [Fact]
    public async Task ASuiteAtItsWallLimitIsStoppedSweptThenDrained()
    {
        using var child = new FakeChild([RootAnswer.AtLimit, RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var table = new FakeTable(_events, [Root, First]);
        var run = await RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.True(run.TimedOut);
        Assert.Equal(["101 chrome first"], run.Survivors);
        Assert.Equal([LimitLine, "E2E_RUNNER survivor 101 killed: chrome first"], Log());
        var kill = _events.IndexOf("kill tree");
        var sweep = _events.IndexOf("kill 101");
        var drain = _events.IndexOf("output wait");
        Assert.True(kill >= 0 && kill < sweep && sweep < drain, string.Join(", ", _events));
        Assert.Equal([101], table.Killed);
        Assert.Equal([Grace], child.OutputWaits);
    }

    // Abort: the same stop with "stop requested", and the run is not TimedOut.
    [Fact]
    public async Task AnAbortedSuiteIsStoppedAndNotTimedOut()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        using var child = new FakeChild([RootAnswer.EndsAtCancel, RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var run = await RunAsync(new FakeTable(_events, [Root]), new FakeLauncher(child).Launch, aborted.Token);
        Assert.False(run.TimedOut);
        Assert.Equal(["E2E_RUNNER stop (stop requested): its process tree was killed"], Log());
    }

    // Stuck: the sweep still runs, nothing is drained, the log is written and closed with the still-running line last, a later
    // line is dropped, and the refusal names the suite, the stop detail and the survivor killed.
    [Fact]
    public async Task ASuiteWhoseRootSurvivesTheKillIsNotStopped()
    {
        using var child = new FakeChild([RootAnswer.AtLimit, RootAnswer.AtLimit], [], _events);
        var launcher = new FakeLauncher(child);
        var refusal = await Assert.ThrowsAsync<RunnerException>(() =>
            RunAsync(new FakeTable(_events, [Root, First]), launcher.Launch, CancellationToken.None));
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Contains("Suite fake could not be stopped: its root was still running after the kill (its process tree was killed); "
            + "survivors killed: 101 chrome first", refusal.Message, StringComparison.Ordinal);
        Assert.Empty(child.OutputWaits);
        string[] closed = [LimitLine, "E2E_RUNNER survivor 101 killed: chrome first", ProcessLifecycle.RootStillRunningLine];
        Assert.Equal(closed, Log());
        var sink = Assert.IsType<OutputSink>(launcher.Receiver);
        sink.Write(OutputStream.Standard, "late\n");
        Assert.Equal(1, sink.Dropped);
        Assert.Equal(closed, Log());
    }

    // A root the kill did not end still owns its pid: the sample taken after the stop finds the child it spawned during the grace
    // (104), and the sweep kills it with the rest.
    [Fact]
    public async Task AChildSpawnedDuringTheGraceOfAStuckRootIsSwept()
    {
        using var child = new FakeChild([RootAnswer.AtLimit, RootAnswer.AtLimit], [], _events);
        var late = new ProcessRow(104, 100, 10, "chrome late");
        // Reads: the register sample, the sample before the stop, then (later rows) the sample after it and the sweep's read.
        var table = new FakeTable(_events, [Root, First]) { LaterFromRead = 3, LaterRows = [Root, First, late] };
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None));
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Equal([101, 104], table.Killed);
    }

    // Control for the post-stop sample: a root that ended at its stop is not sampled again (its pid may be reused), so the children of
    // a process that reused it (103) are not swept; only what was seen while it ran (101) is.
    [Fact]
    public async Task AStoppedRootThatEndedIsNotSampledAgain()
    {
        using var child = new FakeChild([RootAnswer.AtLimit, RootAnswer.Exited], [OutputAnswer.Ended], _events);
        // Reads: the register sample, the sample before the stop, then (later rows) the sweep's read.
        var table = new FakeTable(_events, [Root, First])
        {
            LaterFromRead = 3,
            LaterRows = [new ProcessRow(100, 1, 10, "another program"), First, new ProcessRow(103, 100, 10, "its child")],
        };
        await RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.Equal([101], table.Killed);
    }

    // Sweep refused by the table: the root ended and its output drained, but a refused table read leaves owned processes unknown.
    [Fact]
    public async Task ASuiteWhoseSweepCannotReadTheTableIsNotStopped()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var table = new FakeTable(_events, [Root, First]) { RefuseFromRead = 2 }; // the register sample; the sweep's own read refused
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None));
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Contains("survivor sweep failed: ProcessTableUnreadable: ps refused read 2", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(["E2E_RUNNER survivor sweep failed: ProcessTableUnreadable: ps refused read 2"], Log());
        Assert.Empty(table.Killed);
    }

    // Sweep refused by a kill: with two survivors, the first refused and the second killed, the sweep goes on over both and the
    // refusal names the refused pid and its cause; with a stuck root too, it also carries the stuck detail.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ASuiteWithASurvivorThatCannotBeKilledIsNotStopped(bool rootStuck)
    {
        using var child = new FakeChild(rootStuck ? [RootAnswer.AtLimit, RootAnswer.AtLimit] : [RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var table = new FakeTable(_events, [Root, First, Second]);
        table.KillAnswers[101] = new SurvivorKill(SurvivorKillResult.Refused, "operation not permitted");
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None));
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Contains("survivors killed: 102 chrome second; survivor 101 could not be killed: operation not permitted", refusal.Message,
            StringComparison.Ordinal);
        Assert.Equal(rootStuck, refusal.Message.Contains("its root was still running after the kill", StringComparison.Ordinal));
        Assert.Equal([101, 102], table.Killed);
        string[] sweepLines = ["E2E_RUNNER survivor 102 killed: chrome second", "E2E_RUNNER survivor 101 could not be killed: operation not permitted"];
        Assert.Equal(rootStuck ? [LimitLine, .. sweepLines, ProcessLifecycle.RootStillRunningLine] : sweepLines, Log());
    }

    // Control: a survivor already gone at its kill is neither a failure nor listed as killed.
    [Fact]
    public async Task ASurvivorAlreadyGoneAtItsKillIsNoFailure()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var table = new FakeTable(_events, [Root, First]);
        table.KillAnswers[101] = new SurvivorKill(SurvivorKillResult.Gone, "no such process");
        var run = await RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.Empty(run.Survivors);
        Assert.False(run.TimedOut);
        Assert.Empty(Log());
    }

    // After the root has ended its pid may be reused: children of the new holder of that pid are not swept. Only what was seen
    // while the root ran (101) is matched against the fresh table.
    [Fact]
    public async Task ASweepDoesNotKillChildrenOfAReusedRootPid()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var table = new FakeTable(_events, [Root, First])
        {
            LaterFromRead = 2,
            LaterRows = [new ProcessRow(100, 1, 10, "another program"), First, new ProcessRow(103, 100, 10, "its child")],
        };
        var run = await RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.Equal([101], table.Killed);
        Assert.Equal(["101 chrome first"], run.Survivors);
    }

    // A seen pid now running another command is a reused pid, not a survivor: it is not killed.
    [Fact]
    public async Task ASeenPidNowRunningAnotherCommandIsNotKilled()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended], _events);
        var table = new FakeTable(_events, [Root, First]) { LaterFromRead = 2, LaterRows = [Root, new ProcessRow(101, 100, 10, "reused pid")] };
        var run = await RunAsync(table, new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.Empty(table.Killed);
        Assert.Empty(run.Survivors);
        Assert.Empty(Log());
    }

    // A suite whose root exited reports the root's own exit code.
    [Fact]
    public async Task ASuiteRunReportsItsRootsExitCode()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended], _events) { ExitCode = 3 };
        var run = await RunAsync(new FakeTable(_events, [Root]), new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.Equal(3, run.ExitCode);
    }

    // Open output: the root exited but its output stays open past the drain bound (the time left before the wall limit, at least the
    // grace; here almost the whole wall limit); the log records it and the run is returned.
    [Fact]
    public async Task ASuiteWhoseOutputOutlivesItsRootIsRecordedAndReturned()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.StillOpen], _events);
        var run = await RunAsync(new FakeTable(_events, [Root]), new FakeLauncher(child).Launch, CancellationToken.None);
        Assert.False(run.TimedOut);
        Assert.Equal([ProcessLifecycle.OutputStillOpenLine], Log());
        var bound = Assert.Single(child.OutputWaits);
        Assert.InRange(bound, Wall - StallGuard, Wall);
    }

    // Output fault: the sink is still closed with the runner's lines, then the fault is a typed ProcessOutputFailed refusal.
    [Fact]
    public async Task ASuiteWhoseOutputCannotBeReadIsAnOutputFailure()
    {
        using var child = new FakeChild([RootAnswer.AtLimit, RootAnswer.Exited], [OutputAnswer.Faults], _events);
        var refusal = await Assert.ThrowsAsync<RunnerException>(() =>
            RunAsync(new FakeTable(_events, [Root]), new FakeLauncher(child).Launch, CancellationToken.None));
        Assert.Equal(RunnerError.ProcessOutputFailed, refusal.Error);
        Assert.Contains("the pipe broke", refusal.Message, StringComparison.Ordinal);
        Assert.Equal([LimitLine], Log());
    }

    // Not starting: the refusal is ProcessNotStarted, the log holds no line and the table is never read.
    [Fact]
    public async Task ASuiteThatCannotStartIsAProcessNotStartedRefusal()
    {
        LaunchChild refusing = (_, _) => throw new RunnerException(RunnerError.ProcessNotStarted, "node could not start");
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => RunAsync(new FakeTable(_events, [Root]), refusing, CancellationToken.None));
        Assert.Equal(RunnerError.ProcessNotStarted, refusal.Error);
        Assert.Empty(Log());
        Assert.Empty(_events);
    }
}
