using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>
/// The Simulation run loop's outstanding scheduler turns (Program.NextTurn / ResumeTurn, Story 9.4b). A turn identity
/// only has to be unique among the outstanding turns, so it wraps from <see cref="int.MaxValue"/> back to 1 instead of
/// overflowing after 2^31 turns (edge-case E2), skipping any identity still outstanding.
/// </summary>
public sealed class WorkshopTurnRegistry
{
    private readonly Dictionary<int, TaskCompletionSource> _turns = [];
    private int _last;

    public WorkshopTurnRegistry() : this(0) { }

    /// <summary>Starts after <paramref name="last"/>, so tests can begin at the wrap.</summary>
    public WorkshopTurnRegistry(int last)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(last);
        _last = last;
    }

    public int Outstanding => _turns.Count;

    public (int Turn, Task Resumed) Begin()
    {
        if (_turns.Count == int.MaxValue) throw new InvalidOperationException("Every scheduler turn identity is outstanding.");
        do _last = _last == int.MaxValue ? 1 : _last + 1;
        while (_turns.ContainsKey(_last));
        var completion = new TaskCompletionSource();
        _turns.Add(_last, completion);
        return (_last, completion.Task);
    }

    public void Resume(int turn)
    {
        if (!_turns.Remove(turn, out var completion))
            throw new InvalidOperationException($"Scheduler turn {turn} was never requested or already resumed.");
        completion.SetResult();
    }
}
