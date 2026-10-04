using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>Two bounded reliable original requests/results, including one cancellation control.</summary>
public sealed class WorkshopCommandRouter(Func<WorkshopCommand, byte[], Task<byte[]>> execute)
{
    private ulong _lastSequence;
    private readonly Dictionary<ulong, (byte[] Request, Task<byte[]> Result)> _retained = new(2);

    public Task<byte[]> Dispatch(byte[] bytes)
    {
        var command = WorkshopWire.DecodeCommand(bytes);
        if (_retained.TryGetValue(command.Sequence.Value, out var existing))
        {
            if (!bytes.AsSpan().SequenceEqual(existing.Request))
                throw new ArgumentException("Duplicate sequence carries a different command.");
            return existing.Result;
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
        var reservedResult = new byte[WorkshopWire.ResponseBytes];
        if (retired != 0) _retained.Remove(retired);
        _lastSequence = command.Sequence.Value;
        var result = execute(command, reservedResult);
        _retained.Add(command.Sequence.Value, (owned, result));
        return result;
    }
}
