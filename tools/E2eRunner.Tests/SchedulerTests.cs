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
        async Task<IReadOnlyList<FileResult>> Execute(ScheduledFile file, RunPhase phase, int? worker, CancellationToken token)
        {
            var start = Interlocked.Increment(ref clock);
            var now = Interlocked.Increment(ref running);
            lock (gate) peak = Math.Max(peak, now);
            await Task.Delay(10 + (file.Label.Length * 7 % 23), token);
            Interlocked.Decrement(ref running);
            var end = Interlocked.Increment(ref clock);
            lock (gate) intervals.Add(new Interval(file.Label, phase, start, end));
            return [];
        }
        await Scheduler.RunAsync(files, 3, null, Execute, TestContext.Current.CancellationToken);

        Assert.Equal(files.Count, intervals.Count);
        Assert.InRange(peak, 1, 3);
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
