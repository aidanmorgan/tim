using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace CuriousContraptions.Abstractions;

/// <summary>One 64-byte little-endian event ring record (Story 9A.22 D2): Sequence u64 at 0 (the seqlock word, written last),
/// Tick u64 at 8, Kind u32 at 16, Domain u32 at 20, Source u64 at 24 (the owner body), Other u64 at 32 (a body or 0), Ordinal u32
/// at 40 and Phase f32 at 44 (the physical time within the run), Magnitude f32 at 48, Count u32 at 52, Level u32 at 56, and 4
/// reserved zero bytes. Each field's meaning per kind is <see cref="SimulationEventKind"/>'s; there is no wall clock.</summary>
public readonly record struct SimulationEventRecord(ulong Sequence, ulong Tick, SimulationEventKind Kind, ulong Source, ulong Other,
    uint Ordinal, float Phase, float Magnitude, uint Count, uint Level)
{
    public const int SequenceOffset = 0, TickOffset = 8, KindOffset = 16, DomainOffset = 20, SourceOffset = 24, OtherOffset = 32,
        OrdinalOffset = 40, PhaseOffset = 44, MagnitudeOffset = 48, CountOffset = 52, LevelOffset = 56, ReservedOffset = 60;

    /// <summary>The kind's engine-owned domain.</summary>
    public SimulationDomain Domain => SimulationEvents.Domain(Kind);

    public void Validate()
    {
        _ = SimulationEvents.Domain(Kind);
        if (Source == 0 || !float.IsFinite(Phase) || !float.IsFinite(Magnitude) || Level > 1)
            throw new ArgumentException("Invalid event record.");
    }

    /// <summary>Encodes the record. Publishing never validates or faults: the producer validates every record while it stages the
    /// candidate tick, before the commit point.</summary>
    public void Write(Span<byte> slot)
    {
        if (slot.Length != EventRingBuffer.SlotBytes) throw new ArgumentException("An event record is exactly one slot.");
        BinaryPrimitives.WriteUInt64LittleEndian(slot[SequenceOffset..], Sequence);
        BinaryPrimitives.WriteUInt64LittleEndian(slot[TickOffset..], Tick);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[KindOffset..], (uint)Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[DomainOffset..], (uint)Domain);
        BinaryPrimitives.WriteUInt64LittleEndian(slot[SourceOffset..], Source);
        BinaryPrimitives.WriteUInt64LittleEndian(slot[OtherOffset..], Other);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[OrdinalOffset..], Ordinal);
        BinaryPrimitives.WriteSingleLittleEndian(slot[PhaseOffset..], Phase);
        BinaryPrimitives.WriteSingleLittleEndian(slot[MagnitudeOffset..], Magnitude);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[CountOffset..], Count);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[LevelOffset..], Level);
        BinaryPrimitives.WriteUInt32LittleEndian(slot[ReservedOffset..], 0);
    }

    /// <summary>Reads one slot, refusing an undefined kind, a domain that is not the kind's, a non-zero reserved word or any other
    /// invalid field.</summary>
    public static SimulationEventRecord Read(ReadOnlySpan<byte> slot)
    {
        if (slot.Length != EventRingBuffer.SlotBytes) throw new ArgumentException("An event record is exactly one slot.");
        var record = new SimulationEventRecord(BinaryPrimitives.ReadUInt64LittleEndian(slot[SequenceOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(slot[TickOffset..]), (SimulationEventKind)BinaryPrimitives.ReadUInt32LittleEndian(slot[KindOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(slot[SourceOffset..]), BinaryPrimitives.ReadUInt64LittleEndian(slot[OtherOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(slot[OrdinalOffset..]), BinaryPrimitives.ReadSingleLittleEndian(slot[PhaseOffset..]),
            BinaryPrimitives.ReadSingleLittleEndian(slot[MagnitudeOffset..]), BinaryPrimitives.ReadUInt32LittleEndian(slot[CountOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(slot[LevelOffset..]));
        record.Validate();
        if (BinaryPrimitives.ReadUInt32LittleEndian(slot[DomainOffset..]) != (uint)record.Domain ||
            BinaryPrimitives.ReadUInt32LittleEndian(slot[ReservedOffset..]) != 0)
            throw new ArgumentException("Event record domain or reserved word is invalid.");
        return record;
    }
}

/// <summary>The 64-byte ring header (Story 9A.22 D6): Capacity u32 at 0 (a power of two), SlotBytes u32 at 4, Epoch u64 at 8 (the
/// admitted world's simulation epoch), WriteSequence u64 at 16 (the last published sequence, stored after its slots) and Lapped u64
/// at 24 (records the producer published past the ring's capacity in one batch, so never written); the rest is zero.</summary>
public readonly record struct EventRingHeader(uint Capacity, uint SlotBytes, ulong Epoch, ulong WriteSequence, ulong Lapped)
{
    public const int CapacityOffset = 0, SlotBytesOffset = 4, EpochOffset = 8, WriteSequenceOffset = 16, LappedOffset = 24;

    public void Write(Span<byte> header)
    {
        if (header.Length < EventRingBuffer.HeaderBytes) throw new ArgumentException("An event ring header is 64 bytes.");
        header[..EventRingBuffer.HeaderBytes].Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(header[CapacityOffset..], Capacity);
        BinaryPrimitives.WriteUInt32LittleEndian(header[SlotBytesOffset..], SlotBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(header[EpochOffset..], Epoch);
        BinaryPrimitives.WriteUInt64LittleEndian(header[WriteSequenceOffset..], WriteSequence);
        BinaryPrimitives.WriteUInt64LittleEndian(header[LappedOffset..], Lapped);
    }

    public static EventRingHeader Read(ReadOnlySpan<byte> header)
    {
        if (header.Length < EventRingBuffer.HeaderBytes) throw new ArgumentException("An event ring header is 64 bytes.");
        var value = new EventRingHeader(BinaryPrimitives.ReadUInt32LittleEndian(header[CapacityOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(header[SlotBytesOffset..]), BinaryPrimitives.ReadUInt64LittleEndian(header[EpochOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(header[WriteSequenceOffset..]), BinaryPrimitives.ReadUInt64LittleEndian(header[LappedOffset..]));
        if (value.Capacity == 0 || (value.Capacity & (value.Capacity - 1)) != 0 || value.SlotBytes != EventRingBuffer.SlotBytes ||
            value.Epoch == 0 || header[32..EventRingBuffer.HeaderBytes].IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid event ring header.");
        return value;
    }
}

/// <summary>The single-producer, multi-consumer event ring layout (Story 9A.22). The ring is sized from the admitted scene (no fixed
/// capacity). The producer composes one batch per publication: the header it will leave in the ring, then each record with its
/// sequence. <see cref="Append"/> is the C# twin of the Simulation worker's <c>writeEventRing</c> and the test reference.</summary>
public static class EventRingBuffer
{
    public const int HeaderBytes = 64;
    public const int SlotBytes = 64;
    /// <summary>The largest ring (coordinator decision, 24 Oct 2026): 2^20 slots, 64 MiB of records, a byte length every reader and
    /// writer represents. A construction whose producers would need more is refused at admission (typed, the session stays Building);
    /// overflow within a run is counted in Lapped, so the ring never has to hold a worst case.</summary>
    public const uint MaximumCapacity = 1u << 20;

    public static int ByteLength(uint capacity) => checked(HeaderBytes + (int)capacity * SlotBytes);

    /// <summary>The power-of-two capacity holding at least <paramref name="slots"/> records (at least one slot).</summary>
    public static uint Capacity(ulong slots)
    {
        if (slots > MaximumCapacity) throw new ArgumentOutOfRangeException(nameof(slots), slots, "The scene's event ring exceeds memory.");
        return System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(1UL, slots));
    }

    /// <summary>Composes one publication: the header the ring will hold after it, then each sequenced record.</summary>
    public static byte[] Batch(EventRingHeader header, ReadOnlySpan<SimulationEventRecord> records)
    {
        if (records.Length > header.Capacity) throw new ArgumentException("A publication never holds more records than its ring.");
        var bytes = new byte[HeaderBytes + records.Length * SlotBytes];
        header.Write(bytes);
        for (var i = 0; i < records.Length; i++) records[i].Write(bytes.AsSpan(HeaderBytes + i * SlotBytes, SlotBytes));
        return bytes;
    }

    /// <summary>Copies one publication into the ring under each slot's seqlock (the slot's sequence is zero while it is written and
    /// set last), then stores Lapped and WriteSequence. A different epoch or capacity replaces the ring with a fresh one first, as
    /// the worker does. Returns the ring that holds the publication.</summary>
    public static byte[] Append(byte[]? ring, ReadOnlySpan<byte> batch)
    {
        var header = EventRingHeader.Read(batch);
        if ((batch.Length - HeaderBytes) % SlotBytes != 0 || (batch.Length - HeaderBytes) / SlotBytes > header.Capacity)
            throw new ArgumentException("A publication is whole records within its ring.");
        if (ring is null || ring.Length != ByteLength(header.Capacity) ||
            BinaryPrimitives.ReadUInt64LittleEndian(ring.AsSpan(EventRingHeader.EpochOffset)) != header.Epoch)
        {
            ring = new byte[ByteLength(header.Capacity)];
            batch[..EventRingHeader.WriteSequenceOffset].CopyTo(ring);
        }
        for (var offset = HeaderBytes; offset < batch.Length; offset += SlotBytes)
        {
            var sequence = BinaryPrimitives.ReadUInt64LittleEndian(batch[offset..]);
            var slot = HeaderBytes + (int)((sequence - 1) & (header.Capacity - 1)) * SlotBytes;
            Volatile.Write(ref Word(ring, slot), 0);
            batch.Slice(offset + 8, SlotBytes - 8).CopyTo(ring.AsSpan(slot + 8));
            Volatile.Write(ref Word(ring, slot), sequence);
        }
        Volatile.Write(ref Word(ring, EventRingHeader.LappedOffset), header.Lapped);
        Volatile.Write(ref Word(ring, EventRingHeader.WriteSequenceOffset), header.WriteSequence);
        return ring;
    }

    internal static ref ulong Word(Span<byte> ring, int offset) => ref Unsafe.As<byte, ulong>(ref ring[offset]);
    internal static ulong ReadWord(ReadOnlySpan<byte> ring, int offset) =>
        Volatile.Read(ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(ring[offset..])));
}

/// <summary>One consumer's cursor over the event ring (the reference reader; Story 9A.23 builds the workers' own). A new epoch in the
/// header restarts the cursor at sequence 0. A slot overwritten before it was read, or a publication past the ring's capacity, is
/// counted in <see cref="Lapped"/> and skipped; it is never read as a record.</summary>
public sealed class EventRingConsumer
{
    public ulong Epoch { get; private set; }
    public ulong Cursor { get; private set; }
    public ulong Lapped { get; private set; }

    public bool TryRead(ReadOnlySpan<byte> ring, SimulationDomain filter, out SimulationEventRecord record)
    {
        record = default;
        var header = EventRingHeader.Read(ring);
        if (ring.Length != EventRingBuffer.ByteLength(header.Capacity)) throw new ArgumentException("The ring is not its header's size.");
        if (header.Epoch != Epoch) { Epoch = header.Epoch; Cursor = 0; }
        var written = EventRingBuffer.ReadWord(ring, EventRingHeader.WriteSequenceOffset);
        while (Cursor < written)
        {
            if (written - Cursor > header.Capacity) { Lapped += written - header.Capacity - Cursor; Cursor = written - header.Capacity; }
            var target = Cursor + 1;
            var slot = EventRingBuffer.HeaderBytes + (int)(Cursor & (header.Capacity - 1)) * EventRingBuffer.SlotBytes;
            var before = EventRingBuffer.ReadWord(ring, slot);
            Span<byte> copy = stackalloc byte[EventRingBuffer.SlotBytes];
            ring.Slice(slot, EventRingBuffer.SlotBytes).CopyTo(copy);
            var after = EventRingBuffer.ReadWord(ring, slot);
            Cursor = target;
            if (before != target || after != target) { Lapped++; continue; }
            BinaryPrimitives.WriteUInt64LittleEndian(copy, target);
            var candidate = SimulationEventRecord.Read(copy);
            if (filter != SimulationDomain.None && (candidate.Domain & filter) == 0) continue;
            record = candidate;
            return true;
        }
        return false;
    }
}
