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

    /// <summary>Phase S one file at a time, then Phase P on <paramref name="workers"/> workers. Stops scheduling new files once cancelled.</summary>
    public static async Task<IReadOnlyList<FileResult>> RunAsync(IReadOnlyList<ScheduledFile> files, int workers,
        IReadOnlyDictionary<string, double>? durations, FileExecution execute, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(execute);
        if (workers < 1 || workers > RunnerLock.MaxWorkers)
            throw new ArgumentOutOfRangeException(nameof(workers), workers, $"Workers must be 1..{RunnerLock.MaxWorkers}.");
        var results = new ConcurrentQueue<FileResult>();
        async Task Execute(ScheduledFile file, RunPhase phase, int? worker)
        {
            foreach (var result in await execute(file, phase, worker, cancellation).ConfigureAwait(false)) results.Enqueue(result);
        }
        try
        {
            foreach (var file in SerialPhase(files))
            {
                if (cancellation.IsCancellationRequested) break;
                await Execute(file, RunPhase.SerialAlone, null).ConfigureAwait(false);
            }
            var queue = new ConcurrentQueue<ScheduledFile>(ParallelPhase(files, durations));
            await Task.WhenAll(Enumerable.Range(0, workers).Select(async worker =>
            {
                while (!cancellation.IsCancellationRequested && queue.TryDequeue(out var file))
                    await Execute(file, RunPhase.Parallel, worker).ConfigureAwait(false);
            })).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return [.. results]; } // the caller reports the abort
        return [.. results];
    }
}
