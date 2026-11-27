using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>L10: the process table read with its one limit and typed refusals, the sampler that skips a refused read, and the
/// survivor kill of a pid already gone.</summary>
public sealed class ProcessSamplerTests
{
    // A sample whose table read is refused is skipped and counted, the first reason kept, nothing thrown (its timer has no caller).
    // Control: the accepted sample before it is counted in the peak memory.
    [Fact]
    public void ARefusedSampleIsSkippedCountedAndItsFirstReasonKept()
    {
        var table = new FakeTable([], [new ProcessRow(100, 1, 10, "node suite")]) { RefuseFromRead = 2 };
        using var sampler = new ProcessSampler(table, Timeout.InfiniteTimeSpan);
        sampler.Register(100);
        sampler.Sample();
        sampler.Sample();
        Assert.Equal(2, sampler.SkippedSamples);
        Assert.Equal("ProcessTableUnreadable: ps refused read 2", sampler.FirstSkipReason);
        Assert.Equal(10 * 1024, sampler.PeakRssBytes);
    }

    // The real table lists this test process.
    [Fact]
    public void TheRealSnapshotListsThisProcess()
    {
        var rows = new ProcessTable(ChildProcess.Start, ProcessTable.SnapshotLimit).Snapshot();
        Assert.Contains(rows, row => row.Pid == Environment.ProcessId);
    }

    // A ps that never exits is stopped at the table's one limit (it ends at the kill) and refused as ProcessTimedOut naming 10 s.
    [Fact]
    public void AProcessTableReadThatNeverEndsIsRefusedAtItsLimit()
    {
        using var ps = new FakeChild([RootAnswer.AtLimit, RootAnswer.Exited]);
        var refusal = Assert.Throws<RunnerException>(() => new ProcessTable(new FakeLauncher(ps).Launch, ProcessTable.SnapshotLimit).Snapshot());
        Assert.Equal(RunnerError.ProcessTimedOut, refusal.Error);
        Assert.Contains("ran past its 10 s limit", refusal.Message, StringComparison.Ordinal);
        Assert.Equal([TimeSpan.FromSeconds(10), ProcessLifecycle.Grace], ps.RootWaits);
    }

    // A ps that exits non-zero, or prints a row that does not parse, is a typed ProcessTableUnreadable refusal.
    [Theory]
    [InlineData(1, "", "ps: cannot list processes\n")]
    [InlineData(0, "  12     1    40 /bin/zsh -l\nnot a row\n", "")]
    public void AProcessTableThatCannotBeReadIsATypedRefusal(int exitCode, string output, string error)
    {
        using var ps = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]) { ExitCode = exitCode };
        var launcher = new FakeLauncher(ps, receiver => Deliver(receiver, output, error));
        var refusal = Assert.Throws<RunnerException>(() => new ProcessTable(launcher.Launch, ProcessTable.SnapshotLimit).Snapshot());
        Assert.Equal(RunnerError.ProcessTableUnreadable, refusal.Error);
    }

    // Control: readable rows parse, including a row without a command.
    [Fact]
    public void AReadableProcessTableReturnsItsRows()
    {
        using var ps = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]);
        var launcher = new FakeLauncher(ps, receiver => Deliver(receiver, "  12     1    40 /bin/zsh -l\n   13    12     0\n", ""));
        var rows = new ProcessTable(launcher.Launch, ProcessTable.SnapshotLimit).Snapshot();
        Assert.Equal([new ProcessRow(12, 1, 40, "/bin/zsh -l"), new ProcessRow(13, 12, 0, "")], rows);
    }

    // A pid that cannot exist (macOS pids stay far below int.MaxValue) is Gone, not a failure.
    [Fact]
    public void KillingAPidThatCannotExistIsGone()
    {
        var kill = new ProcessTable(ChildProcess.Start, ProcessTable.SnapshotLimit).Kill(int.MaxValue);
        Assert.Equal(SurvivorKillResult.Gone, kill.Result);
    }

    /// <summary>A generous stall guard for the two-thread tick test, not a timing assertion.</summary>
    private static readonly TimeSpan StallGuard = TimeSpan.FromSeconds(30);

    // A timer tick that finds the previous tick's sample still reading the table returns without reading; once that sample ends the
    // next tick reads again.
    [Fact]
    public async Task ATimerTickDuringASampleIsSkipped()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var table = new FakeTable([], [new ProcessRow(100, 1, 10, "node suite")])
        {
            DuringRead = read =>
            {
                if (read != 1) return;
                entered.Set();
                release.Wait(StallGuard);
            },
        };
        using var sampler = new ProcessSampler(table, Timeout.InfiniteTimeSpan);
        var first = Task.Run(sampler.SampleOnTick, TestContext.Current.CancellationToken);
        Assert.True(entered.Wait(StallGuard, TestContext.Current.CancellationToken));
        sampler.SampleOnTick();
        Assert.Equal(1, table.Reads);
        release.Set();
        await first.WaitAsync(StallGuard, TestContext.Current.CancellationToken);
        sampler.SampleOnTick();
        Assert.Equal(2, table.Reads);
    }

    // VG4-1: a sample after Sweep(root) no longer walks that root: a process that reused the root's pid, with a child and a large
    // resident memory, neither raises the peak nor gives a second sweep anything to kill. Control: the first sweep kills the child seen
    // while the root ran (101).
    [Fact]
    public void ASampleAfterTheSweepNoLongerWalksTheTree()
    {
        var table = new FakeTable([], [new ProcessRow(100, 1, 10, "node suite"), new ProcessRow(101, 100, 10, "chrome first")])
        {
            LaterFromRead = 4, // reads: register, sample, the sweep's own read; then the timer's sample and the second sweep
            LaterRows = [new ProcessRow(100, 1, 999_999, "another program"), new ProcessRow(103, 100, 999_999, "its child")],
        };
        using var sampler = new ProcessSampler(table, Timeout.InfiniteTimeSpan);
        sampler.Register(100);
        sampler.Sample();
        Assert.Equal([101], sampler.Sweep(100).Killed.Select(row => row.Pid));
        var peak = sampler.PeakRssBytes;
        sampler.SampleOnTick();
        Assert.Equal(peak, sampler.PeakRssBytes);
        Assert.Empty(sampler.Sweep(100).Killed);
        Assert.Equal([101], table.Killed);
    }

    // The run header's `sampler` line reports the skipped samples with the first reason, or that none was skipped.
    [Theory]
    [InlineData(false, "no sample skipped")]
    [InlineData(true, "2 sample(s) skipped; first: ProcessTableUnreadable: ps refused read 1")]
    public void TheRunHeaderReportsSkippedSamples(bool refused, string expected)
    {
        var table = new FakeTable([], [new ProcessRow(100, 1, 10, "node suite")]) { RefuseFromRead = refused ? 1 : int.MaxValue };
        using var sampler = new ProcessSampler(table, Timeout.InfiniteTimeSpan);
        sampler.Sample();
        sampler.Sample();
        Assert.Equal(expected, RunEngine.SamplerHeader(sampler));
    }

    public enum SystemQuery { LoadAverage, MachineMemory }

    // A system query (sysctl) that exits non-zero (even with output that would parse) or prints what does not parse is a typed
    // SystemQueryFailed refusal.
    [Theory]
    [InlineData(SystemQuery.LoadAverage, 1, "")]
    [InlineData(SystemQuery.LoadAverage, 1, "{ 1.50 1.20 1.00 }\n")]
    [InlineData(SystemQuery.LoadAverage, 0, "{ high }\n")]
    [InlineData(SystemQuery.MachineMemory, 1, "")]
    [InlineData(SystemQuery.MachineMemory, 1, "68719476736\n")]
    [InlineData(SystemQuery.MachineMemory, 0, "lots\n")]
    public void ASystemQueryThatFailsIsATypedRefusal(SystemQuery query, int exitCode, string output)
    {
        using var sysctl = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]) { ExitCode = exitCode };
        LaunchChild launch = new FakeLauncher(sysctl, receiver => Deliver(receiver, output, "")).Launch;
        var refusal = Assert.Throws<RunnerException>(() => Query(query, launch));
        Assert.Equal(RunnerError.SystemQueryFailed, refusal.Error);
    }

    // Control: readable answers parse.
    [Theory]
    [InlineData(SystemQuery.LoadAverage, "{ 1.50 1.20 1.00 }\n", 1.5)]
    [InlineData(SystemQuery.MachineMemory, "68719476736\n", 68719476736d)]
    public void AReadableSystemQueryReturnsItsValue(SystemQuery query, string output, double expected)
    {
        using var sysctl = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]);
        LaunchChild launch = new FakeLauncher(sysctl, receiver => Deliver(receiver, output, "")).Launch;
        Assert.Equal(expected, Query(query, launch));
    }

    private static double Query(SystemQuery query, LaunchChild launch) =>
        query == SystemQuery.LoadAverage ? ProcessTable.LoadAverage(launch) : MeasuredWorkers.MachineMemory(launch);

    private static void Deliver(IOutputReceiver receiver, string output, string error)
    {
        receiver.Write(OutputStream.Standard, output);
        receiver.Write(OutputStream.Error, error);
        receiver.End(OutputStream.Standard);
        receiver.End(OutputStream.Error);
    }
}
