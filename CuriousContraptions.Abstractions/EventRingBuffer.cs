using System;
using System.Buffers.Binary;
using System.Threading;

namespace CuriousContraptions.Abstractions;

public readonly record struct SimulationEventRecord(
    ulong Sequence,
    ulong TimestampNs,
    SimulationDomain Domain,
    SimulationEventKind EventKind,
    ulong SourceBodyId,
    ulong TargetBodyId,
    float P0 = 0f,
    float P1 = 0f,
    float P2 = 0f,
    float P3 = 0f,
    float P4 = 0f,
    float P5 = 0f)
{
    public float Impulse => P0;
    public float ContactX => P1;
    public float ContactY => P2;
    public float ContactZ => P3;
    public float Value => P4;
}

public static class EventRingBuffer
{
    public const int HeaderBytes = 64;
    public const int DefaultCapacity = 1024;
    public const int SlotBytes = 64;
    public const int TotalByteLength = HeaderBytes + DefaultCapacity * SlotBytes; // 65600

    public static void Initialize(Span<byte> buffer, uint capacity = DefaultCapacity)
    {
        if (buffer.Length < HeaderBytes + (int)capacity * SlotBytes)
            throw new ArgumentException("Buffer is too small for event ring buffer.");
        if ((capacity & (capacity - 1)) != 0 || capacity == 0)
            throw new ArgumentException("Capacity must be a power of two.");

        buffer[..HeaderBytes].Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[0..4], capacity);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[4..8], (uint)SlotBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[8..16], 0);
    }

    public static ulong Publish(Span<byte> buffer, in SimulationEventRecord record)
    {
        if (buffer.Length < HeaderBytes)
            throw new ArgumentException("Invalid event ring buffer header.");

        var capacity = BinaryPrimitives.ReadUInt32LittleEndian(buffer[0..4]);
        if (capacity == 0) capacity = DefaultCapacity;

        ref byte head = ref buffer[0];
        ref long writeSeqRef = ref System.Runtime.CompilerServices.Unsafe.As<byte, long>(ref System.Runtime.CompilerServices.Unsafe.Add(ref head, 8));
        var nextSeq = (ulong)Interlocked.Increment(ref writeSeqRef);

        var slotIndex = (int)((nextSeq - 1) & (capacity - 1));
        var slotOffset = HeaderBytes + slotIndex * SlotBytes;
        var slot = buffer.Slice(slotOffset, SlotBytes);

        BinaryPrimitives.WriteUInt64LittleEndian(slot[0..8], 0); // Invalid while writing
        BinaryPrimitives.WriteUInt64LittleEndian(slot[8..16], record.TimestampNs);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[16..20], (uint)record.Domain);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[20..24], (uint)record.EventKind);
        BinaryPrimitives.WriteUInt64LittleEndian(slot[24..32], record.SourceBodyId);
        BinaryPrimitives.WriteUInt64LittleEndian(slot[32..40], record.TargetBodyId);
        BinaryPrimitives.WriteSingleLittleEndian(slot[40..44], record.P0);
        BinaryPrimitives.WriteSingleLittleEndian(slot[44..48], record.P1);
        BinaryPrimitives.WriteSingleLittleEndian(slot[48..52], record.P2);
        BinaryPrimitives.WriteSingleLittleEndian(slot[52..56], record.P3);
        BinaryPrimitives.WriteSingleLittleEndian(slot[56..60], record.P4);
        BinaryPrimitives.WriteSingleLittleEndian(slot[60..64], record.P5);

        // Commit sequence
        ref byte slotHead = ref slot[0];
        ref long seqRef = ref System.Runtime.CompilerServices.Unsafe.As<byte, long>(ref slotHead);
        Volatile.Write(ref seqRef, (long)nextSeq);

        return nextSeq;
    }
}

public sealed class EventRingConsumer
{
    public ulong ReadCursor { get; private set; }

    public EventRingConsumer(ulong initialCursor = 0)
    {
        ReadCursor = initialCursor;
    }

    public bool TryRead(ReadOnlySpan<byte> buffer, SimulationDomain domainFilter, out SimulationEventRecord record, out bool overrun)
    {
        record = default;
        overrun = false;

        if (buffer.Length < EventRingBuffer.HeaderBytes) return false;

        var capacity = BinaryPrimitives.ReadUInt32LittleEndian(buffer[0..4]);
        if (capacity == 0) capacity = EventRingBuffer.DefaultCapacity;

        ref byte head = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(buffer);
        ref long writeSeqRef = ref System.Runtime.CompilerServices.Unsafe.As<byte, long>(ref System.Runtime.CompilerServices.Unsafe.Add(ref head, 8));
        var writeSeq = (ulong)Volatile.Read(ref writeSeqRef);

        if (ReadCursor >= writeSeq) return false;

        if (writeSeq - ReadCursor > capacity)
        {
            overrun = true;
            ReadCursor = writeSeq - capacity;
        }

        while (ReadCursor < writeSeq)
        {
            var targetSeq = ReadCursor + 1;
            var slotIndex = (int)(ReadCursor & (capacity - 1));
            var slotOffset = EventRingBuffer.HeaderBytes + slotIndex * EventRingBuffer.SlotBytes;
            var slot = buffer.Slice(slotOffset, EventRingBuffer.SlotBytes);

            ref byte slotHead = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(slot);
            ref long seqRef = ref System.Runtime.CompilerServices.Unsafe.As<byte, long>(ref slotHead);
            var committedSeq = (ulong)Volatile.Read(ref seqRef);

            if (committedSeq != targetSeq)
            {
                // Slot is being overwritten or partially written
                ReadCursor++;
                continue;
            }

            var domain = (SimulationDomain)BinaryPrimitives.ReadUInt32LittleEndian(slot[16..20]);
            ReadCursor++;

            if ((domain & domainFilter) != 0 || domainFilter == SimulationDomain.None)
            {
                record = new SimulationEventRecord(
                    Sequence: targetSeq,
                    TimestampNs: BinaryPrimitives.ReadUInt64LittleEndian(slot[8..16]),
                    Domain: domain,
                    EventKind: (SimulationEventKind)BinaryPrimitives.ReadUInt32LittleEndian(slot[20..24]),
                    SourceBodyId: BinaryPrimitives.ReadUInt64LittleEndian(slot[24..32]),
                    TargetBodyId: BinaryPrimitives.ReadUInt64LittleEndian(slot[32..40]),
                    P0: BinaryPrimitives.ReadSingleLittleEndian(slot[40..44]),
                    P1: BinaryPrimitives.ReadSingleLittleEndian(slot[44..48]),
                    P2: BinaryPrimitives.ReadSingleLittleEndian(slot[48..52]),
                    P3: BinaryPrimitives.ReadSingleLittleEndian(slot[52..56]),
                    P4: BinaryPrimitives.ReadSingleLittleEndian(slot[56..60]),
                    P5: BinaryPrimitives.ReadSingleLittleEndian(slot[60..64]));
                return true;
            }
        }

        return false;
    }
}
