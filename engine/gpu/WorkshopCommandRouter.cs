using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>A dispatched command's sequence (its acknowledgement's key, handed to the browser with the response so no script
/// parses a buffer) and its encoded response.</summary>
public readonly record struct WorkshopRoutedCommand(CommandSequence Sequence, Task<byte[]> Response);

/// <summary>Two bounded reliable original requests/results, including one cancellation control. Each command is decoded once by
/// the one codec; a refused buffer is its <see cref="WireFaultException"/> and never reaches the executor.</summary>
public sealed class WorkshopCommandRouter(Func<WorkshopCommand, Task<byte[]>> execute)
{
    private ulong _lastSequence;
    private readonly Dictionary<ulong, (byte[] Request, Task<byte[]> Result)> _retained = new(2);

    public WorkshopRoutedCommand Dispatch(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var command = WorkshopWireFlatBuffers.DecodeCommand(bytes);
        if (_retained.TryGetValue(command.Sequence.Value, out var existing))
        {
            if (!bytes.AsSpan().SequenceEqual(existing.Request))
                throw new ArgumentException("Duplicate sequence carries a different command.");
            return new(command.Sequence, existing.Result);
        }
        if (_lastSequence == ulong.MaxValue || command.Sequence.Value != _lastSequence + 1)
            throw new ArgumentException("Command sequence is not the next reliable request.");
        ulong retired = 0;
        if (_retained.Count == 2)
        {
            foreach (var entry in _retained)
                if (entry.Value.Result.IsCompleted && (retired == 0 || entry.Key < retired)) retired = entry.Key;
            if (retired == 0) throw new InvalidOperationException("Reliable command result capacity is full.");
        }
        var owned = (byte[])bytes.Clone();
        if (retired != 0) _retained.Remove(retired);
        _lastSequence = command.Sequence.Value;
        var result = execute(command);
        _retained.Add(command.Sequence.Value, (owned, result));
        return new(command.Sequence, result);
    }
}
