using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>AC1 scheduling: SerialAlone intervals overlap nothing and concurrency never exceeds N; one runner at a time per repository.</summary>
public sealed class SchedulerTests : IDisposable
{
    private readonly DirectoryInfo _locks = Directory.CreateTempSubdirectory("e2e-lock-");

    public void Dispose() => _locks.Delete(true);

    private sealed record Interval(string Label, RunPhase Phase, long Start, long End);

    [Fact]
    public async Task SerialAloneRunsAloneAndConcurrencyNeverExceedsN()
    {
        var files = new List<ScheduledFile>();
        for (var index = 0; index < 3; index++) files.Add(new ScheduledFile($"serial-{index}", SuiteTiming.SerialAlone));
        for (var index = 0; index < 12; index++) files.Add(new ScheduledFile($"parallel-{index:D2}", SuiteTiming.Parallel));
        var clock = 0L;
        var running = 0;
        var peak = 0;
        var intervals = new List<Interval>();
        var gate = new Lock();
        var firstParallelBatch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredParallel = 0;
        async Task<IReadOnlyList<FileResult>> Execute(ScheduledFile file, RunPhase phase, int? worker, CancellationToken token)
        {
            var start = Interlocked.Increment(ref clock);
            var now = Interlocked.Increment(ref running);
            lock (gate) peak = Math.Max(peak, now);
            if (phase == RunPhase.Parallel && Interlocked.Increment(ref enteredParallel) <= 3)
            {
                if (Volatile.Read(ref enteredParallel) == 3) firstParallelBatch.TrySetResult();
                await firstParallelBatch.Task.WaitAsync(TimeSpan.FromSeconds(30), token);
            }
            await Task.Yield();
            Interlocked.Decrement(ref running);
            var end = Interlocked.Increment(ref clock);
            lock (gate) intervals.Add(new Interval(file.Label, phase, start, end));
            return [];
        }
        await Scheduler.RunAsync(files, 3, null, Execute, TestContext.Current.CancellationToken);

        Assert.Equal(files.Count, intervals.Count);
        Assert.Equal(3, peak);
        foreach (var serial in intervals.Where(interval => interval.Label.StartsWith("serial", StringComparison.Ordinal)))
        {
            Assert.Equal(RunPhase.SerialAlone, serial.Phase);
            Assert.DoesNotContain(intervals, other => other != serial && other.Start < serial.End && serial.Start < other.End);
        }
        var lastSerialEnd = intervals.Where(interval => interval.Phase == RunPhase.SerialAlone).Max(interval => interval.End);
        Assert.All(intervals.Where(interval => interval.Phase == RunPhase.Parallel), interval => Assert.True(interval.Start > lastSerialEnd));
    }

    [Fact]
    public void ParallelPhaseRunsLongestFirstAndUnknownFilesFirst()
    {
        var files = new[] { new ScheduledFile("b", SuiteTiming.Parallel), new ScheduledFile("a", SuiteTiming.Parallel),
            new ScheduledFile("c", SuiteTiming.Parallel), new ScheduledFile("new", SuiteTiming.Parallel), new ScheduledFile("s", SuiteTiming.SerialAlone) };
        var durations = new Dictionary<string, double> { ["a"] = 10, ["b"] = 30, ["c"] = 20 };
        Assert.Equal(["new", "b", "c", "a"], Scheduler.ParallelPhase(files, durations).Select(file => file.Label));
        Assert.Equal(["a", "b", "c", "new"], Scheduler.ParallelPhase(files, null).Select(file => file.Label));
        Assert.Equal(["s"], Scheduler.SerialPhase(files).Select(file => file.Label));
    }

    /// <summary>A generous stall guard: a scheduler that failed to stop its files would hang, not pass.</summary>
    private static readonly TimeSpan StallGuard = TimeSpan.FromSeconds(30);

    private static ScheduledFile[] Files(SuiteTiming timing, params string[] labels) => [.. labels.Select(label => new ScheduledFile(label, timing))];

    /// <summary>Parallel files a, b, c, d on two workers: a (worker 0) waits in flight; b (worker 1) fails first, after a has started;
    /// a then sees the cancellation and fails later. Returns what was rethrown, the files started and whether a saw the cancellation.</summary>
    private static async Task<(Exception Thrown, string[] Started, bool ASawCancellation)> FailInOrderAsync(Exception first, Exception later)
    {
        var started = new List<string>();
        var gate = new Lock();
        var aStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aSawCancellation = false;
        async Task<IReadOnlyList<FileResult>> Execute(ScheduledFile file, RunPhase phase, int? worker, CancellationToken token)
        {
            lock (gate) started.Add(file.Label);
            if (file.Label == "a")
            {
                aStarted.SetResult();
                try { await Task.Delay(Timeout.Infinite, token); }
                catch (OperationCanceledException) { aSawCancellation = true; }
                throw later;
            }
            await aStarted.Task.WaitAsync(StallGuard, token);
            throw first;
        }
        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => Scheduler.RunAsync(Files(SuiteTiming.Parallel, "a", "b", "c", "d"), 2, null,
            Execute, TestContext.Current.CancellationToken).WaitAsync(StallGuard, TestContext.Current.CancellationToken));
        lock (gate) return (thrown, [.. started], aSawCancellation);
    }

    // L11: the first failure in time (from worker 1, not the lowest worker), of any exception type, cancels the file in flight, starts
    // no later file and is rethrown once every worker has returned; the in-flight file's later failure does not replace it.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AFailureStopsEveryOtherFileAndTheFirstIsRethrown(bool runnerFailure)
    {
        Exception first = runnerFailure ? new RunnerException(RunnerError.ProcessTimedOut, "b ran past its limit") : new InvalidOperationException("b broke");
        var (thrown, started, aSawCancellation) = await FailInOrderAsync(first, new InvalidOperationException("a failed after the stop"));
        Assert.Same(first, thrown);
        Assert.Equal(["a", "b"], started);
        Assert.True(aSawCancellation);
    }

    // L11 boundary: a later ProcessNotStopped (a process the run owns may still run) replaces a first failure of another kind.
    [Fact]
    public async Task AProcessNotStoppedAfterAnotherFailureIsRethrown()
    {
        var later = new RunnerException(RunnerError.ProcessNotStopped, "a could not be stopped");
        var (thrown, _, _) = await FailInOrderAsync(new InvalidOperationException("b broke"), later);
        Assert.Same(later, thrown);
    }

    // L11 boundary: two ProcessNotStopped failures (each a process the run owns that may still run) become one refusal naming each.
    [Fact]
    public async Task TwoProcessNotStoppedFailuresAreBothNamed()
    {
        var first = new RunnerException(RunnerError.ProcessNotStopped, "b could not be stopped");
        var later = new RunnerException(RunnerError.ProcessNotStopped, "a could not be stopped");
        var (thrown, _, _) = await FailInOrderAsync(first, later);
        var refusal = Assert.IsType<RunnerException>(thrown);
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Contains("b could not be stopped", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("a could not be stopped", refusal.Message, StringComparison.Ordinal);
        Assert.Equal([first, later], Assert.IsType<AggregateException>(refusal.InnerException).InnerExceptions);
    }

    // L11 boundary: a Phase S failure starts no later Phase S file and no Phase P file.
    [Fact]
    public async Task APhaseSFailureStartsNoPhasePFile()
    {
        var started = new List<string>();
        var failure = new InvalidOperationException("s broke");
        Task<IReadOnlyList<FileResult>> Execute(ScheduledFile file, RunPhase phase, int? worker, CancellationToken token)
        {
            started.Add(file.Label);
            return Task.FromException<IReadOnlyList<FileResult>>(failure);
        }
        ScheduledFile[] files = [.. Files(SuiteTiming.SerialAlone, "s", "t"), .. Files(SuiteTiming.Parallel, "p")];
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Scheduler.RunAsync(files, 2, null, Execute, TestContext.Current.CancellationToken).WaitAsync(StallGuard, TestContext.Current.CancellationToken));
        Assert.Same(failure, thrown);
        Assert.Equal(["s"], started);
    }

    // L11: the caller's cancellation (an abort) is not a failure: the file it cancels in flight is not one, no later file starts and
    // the results already recorded are returned.
    [Fact]
    public async Task AnAbortIsNotAFailure()
    {
        using var abort = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var finished = new FileResult("a", RunPhase.Parallel, 0, FileOutcome.Pass, [], 0, null, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [],
            Observation.Clean, null, null);
        var started = new List<string>();
        async Task<IReadOnlyList<FileResult>> Execute(ScheduledFile file, RunPhase phase, int? worker, CancellationToken token)
        {
            started.Add(file.Label);
            if (file.Label == "a") return [finished];
            await abort.CancelAsync();
            await Task.Delay(Timeout.Infinite, token);
            return [];
        }
        var results = await Scheduler.RunAsync(Files(SuiteTiming.Parallel, "a", "b", "c"), 1, null, Execute, abort.Token)
            .WaitAsync(StallGuard, TestContext.Current.CancellationToken);
        Assert.Equal([finished], results);
        Assert.Equal(["a", "b"], started);
    }

    [Fact]
    public void OnlyOneRunnerHoldsTheLock()
    {
        var path = Path.Combine(_locks.FullName, "e2e-runner.lock");
        using (RunnerLock.Acquire(path))
            Assert.Equal(RunnerError.RunnerBusy, Assert.Throws<RunnerException>(() => RunnerLock.Acquire(path)).Error);
        using var again = RunnerLock.Acquire(path);
        Assert.Equal(path, again.Path);
    }
}
