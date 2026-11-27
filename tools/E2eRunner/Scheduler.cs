using System.Collections.Concurrent;

namespace CuriousContraptions.E2eRunner;

/// <summary>One file to schedule.</summary>
internal sealed record ScheduledFile(string Label, SuiteTiming Timing);

/// <summary>Runs one file in a phase (on the given worker in Phase P) and returns its record(s): a contended SerialAlone observation
/// and its quiet re-run are both kept.</summary>
internal delegate Task<IReadOnlyList<FileResult>> FileExecution(ScheduledFile file, RunPhase phase, int? worker, CancellationToken cancellation);

internal static class Scheduler
{
    /// <summary>Phase S: the SerialAlone files in catalogue (input) order.</summary>
    public static IReadOnlyList<ScheduledFile> SerialPhase(IReadOnlyList<ScheduledFile> files) =>
        [.. files.Where(file => file.Timing == SuiteTiming.SerialAlone)];

    /// <summary>Phase P: the Parallel files longest first by the previous summary's durations (files it lacks first, alphabetically),
    /// or alphabetically without one.</summary>
    public static IReadOnlyList<ScheduledFile> ParallelPhase(IReadOnlyList<ScheduledFile> files, IReadOnlyDictionary<string, double>? durations) =>
        [.. files.Where(file => file.Timing == SuiteTiming.Parallel)
            .OrderBy(file => durations is not null && durations.ContainsKey(file.Label) ? 1 : 0)
            .ThenByDescending(file => durations is not null && durations.TryGetValue(file.Label, out var duration) ? duration : 0)
            .ThenBy(file => file.Label, StringComparer.Ordinal)];

    /// <summary>Phase S one file at a time, then Phase P on <paramref name="workers"/> workers. The first failure of any file, of any
    /// exception type, stops the run: no file starts after it and files in flight receive the cancellation. Once every worker has
    /// returned, the first failure in time is rethrown, except that a later ProcessNotStopped replaces a first failure of another
    /// kind, and a later ProcessNotStopped after a first one is joined with it into one ProcessNotStopped naming each. The caller's cancellation (an abort) with no failure returns the results; a cancellation a file sees after the stop or
    /// the abort is not a failure.</summary>
    public static async Task<IReadOnlyList<FileResult>> RunAsync(IReadOnlyList<ScheduledFile> files, int workers,
        IReadOnlyDictionary<string, double>? durations, FileExecution execute, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(execute);
        if (workers < 1 || workers > RunnerLock.MaxWorkers)
            throw new ArgumentOutOfRangeException(nameof(workers), workers, $"Workers must be 1..{RunnerLock.MaxWorkers}.");
        var results = new ConcurrentQueue<FileResult>();
        var failure = new FirstFailure();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        // Every throw of a file, synchronous or not, becomes the outcome of this task.
        async Task<IReadOnlyList<FileResult>> RunFileAsync(ScheduledFile file, RunPhase phase, int? worker) =>
            await execute(file, phase, worker, stop.Token).ConfigureAwait(false);
        async Task Execute(ScheduledFile file, RunPhase phase, int? worker)
        {
            var run = RunFileAsync(file, phase, worker);
            await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            if (run.IsCompletedSuccessfully)
            {
                foreach (var result in await run.ConfigureAwait(false)) results.Enqueue(result);
                return;
            }
            // A cancellation the file saw after the stop or the abort is not a failure; every other outcome is.
            if (run.IsCanceled && stop.IsCancellationRequested) return;
            failure.Record(run);
            await stop.CancelAsync().ConfigureAwait(false);
        }
        foreach (var file in SerialPhase(files))
        {
            if (stop.IsCancellationRequested) break;
            await Execute(file, RunPhase.SerialAlone, null).ConfigureAwait(false);
        }
        var queue = new ConcurrentQueue<ScheduledFile>(ParallelPhase(files, durations));
        await Task.WhenAll(Enumerable.Range(0, workers).Select(async worker =>
        {
            while (!stop.IsCancellationRequested && queue.TryDequeue(out var file))
                await Execute(file, RunPhase.Parallel, worker).ConfigureAwait(false);
        })).ConfigureAwait(false);
        failure.ThrowIfRecorded();
        return [.. results];
    }

    /// <summary>The run's failure (a failed file's task): the first in time, replaced by a later ProcessNotStopped when the first is
    /// not one, and joined with it into one ProcessNotStopped naming each when it is. Rethrowing it rethrows that exception.</summary>
    private sealed class FirstFailure
    {
        private readonly Lock _gate = new();
        private Task? _first;

        public void Record(Task failed)
        {
            var later = NotStopped(failed);
            lock (_gate)
            {
                if (_first is null) _first = failed;
                else if (later is not null)
                    _first = NotStopped(_first) is { } earlier ? Task.FromException(ProcessLifecycle.BothNotStopped(earlier, later)) : failed;
            }
        }

        public void ThrowIfRecorded()
        {
            Task? first;
            lock (_gate) first = _first;
            first?.GetAwaiter().GetResult();
        }

        private static RunnerException? NotStopped(Task failed) =>
            failed.Exception?.InnerException is RunnerException { Error: RunnerError.ProcessNotStopped } refusal ? refusal : null;
    }
}
