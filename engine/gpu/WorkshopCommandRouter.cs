using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>The codec a command's acknowledgement answers in: the command's own.</summary>
public enum WorkshopResponseCodec : uint { FlatBuffers = 1, Binary = 2 }

/// <summary>Two bounded reliable original requests/results, including one cancellation control.</summary>
public sealed class WorkshopCommandRouter(Func<WorkshopCommand, WorkshopResponseCodec, Task<byte[]>> execute)
{
    private ulong _lastSequence;
    private readonly Dictionary<ulong, (byte[] Request, Task<byte[]> Result)> _retained = new(2);

    public Task<byte[]> Dispatch(byte[] bytes)
    {
        var isFlatBuffer = false;
        WorkshopCommand command;
        try
        {
            command = WorkshopWireFlatBuffers.DecodeCommand(bytes);
            isFlatBuffer = true;
        }
        catch (ArgumentException)
        {
            // Not a FlatBuffers command (its decoder rejects foreign bytes with ArgumentException): fixed binary.
            command = WorkshopWire.DecodeCommand(bytes);
        }
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
        // The binary response's width is its read's populations (Story 9R.5b), so it is encoded when the result exists.
        var codec = isFlatBuffer ? WorkshopResponseCodec.FlatBuffers : WorkshopResponseCodec.Binary;
        if (retired != 0) _retained.Remove(retired);
        _lastSequence = command.Sequence.Value;
        var result = execute(command, codec);
        _retained.Add(command.Sequence.Value, (owned, result));
        return result;
    }
}
