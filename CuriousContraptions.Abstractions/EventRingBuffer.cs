using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
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
    /// published last), then stores Lapped and WriteSequence, all through <see cref="EventRingMemory"/>'s word accessors. A different
    /// epoch or capacity replaces the ring with a fresh one first, as the worker does. Returns the ring that holds the publication.</summary>
    public static EventRingMemory Append(EventRingMemory? ring, ReadOnlySpan<byte> batch)
    {
        var header = EventRingHeader.Read(batch);
        if ((batch.Length - HeaderBytes) % SlotBytes != 0 || (batch.Length - HeaderBytes) / SlotBytes > header.Capacity)
            throw new ArgumentException("A publication is whole records within its ring.");
        if (ring is null || ring.Capacity != header.Capacity || ring.Epoch != header.Epoch) ring = new EventRingMemory(header.Capacity, header.Epoch);
        for (var offset = HeaderBytes; offset < batch.Length; offset += SlotBytes)
        {
            // The writer's seqlock, as the shipped worker's: the sequence word is 0 while every payload word is stored, then published.
            var sequence = BinaryPrimitives.ReadUInt64LittleEndian(batch[offset..]);
            ring.PublishSequence(sequence, 0);
            for (var word = 1; word < SlotBytes / 8; word++)
                ring.StorePayload(sequence, word, BinaryPrimitives.ReadUInt64LittleEndian(batch[(offset + word * 8)..]));
            ring.PublishSequence(sequence, sequence);
        }
        ring.StoreLapped(header.Lapped);
        ring.StoreWriteSequence(header.WriteSequence);
        return ring;
    }
}

/// <summary>The C# reference ring's memory (owner request, review R2-F2), the twin of <c>event-ring-cursor.js</c>'s
/// <c>EventRingMemory</c>: it alone holds the ring's bytes, and every accessor is one whole-word <see cref="Volatile"/> load or store.
/// The writer (<see cref="EventRingBuffer"/>'s <c>Append</c>) and the reader (<see cref="EventRingConsumer.TryRead"/>) implement the slot seqlock
/// against these accessors only.</summary>
public sealed class EventRingMemory
{
    private readonly byte[] _memory;

    /// <summary>A fresh ring: its fixed header words (capacity and slot size, then epoch) are stored before any slot.</summary>
    public EventRingMemory(uint capacity, ulong epoch)
    {
        if (capacity == 0 || (capacity & (capacity - 1)) != 0 || epoch == 0) throw new ArgumentException("Unsupported event ring layout.");
        _memory = new byte[EventRingBuffer.ByteLength(capacity)];
        Capacity = capacity; Epoch = epoch;
        Volatile.Write(ref Word(EventRingHeader.CapacityOffset), capacity | (ulong)EventRingBuffer.SlotBytes << (8 * EventRingHeader.SlotBytesOffset));
        Volatile.Write(ref Word(EventRingHeader.EpochOffset), epoch);
    }

    /// <summary>A copy of a ring another writer produced (the shipped worker's bytes): its header is validated and every word stored.</summary>
    public static EventRingMemory Load(ReadOnlySpan<byte> ring)
    {
        var header = EventRingHeader.Read(ring);
        if (ring.Length != EventRingBuffer.ByteLength(header.Capacity)) throw new ArgumentException("The ring is not its header's size.");
        var memory = new EventRingMemory(header.Capacity, header.Epoch);
        for (var offset = 0; offset < ring.Length; offset += 8)
            Volatile.Write(ref memory.Word(offset), BinaryPrimitives.ReadUInt64LittleEndian(ring[offset..]));
        return memory;
    }

    public uint Capacity { get; }
    public ulong Epoch { get; }

    public ulong LoadWriteSequence() => Volatile.Read(ref Word(EventRingHeader.WriteSequenceOffset));
    public void StoreWriteSequence(ulong value) => Volatile.Write(ref Word(EventRingHeader.WriteSequenceOffset), value);
    public void StoreLapped(ulong value) => Volatile.Write(ref Word(EventRingHeader.LappedOffset), value);
    /// <summary>The seqlock word of the slot that holds record <paramref name="sequence"/>.</summary>
    public ulong LoadSequence(ulong sequence) => Volatile.Read(ref Word(Slot(sequence)));
    public void PublishSequence(ulong sequence, ulong value) => Volatile.Write(ref Word(Slot(sequence)), value);
    /// <summary>Payload word <paramref name="word"/> (1 to 7; word 0 is the sequence) of the slot that holds record <paramref name="sequence"/>.</summary>
    public ulong LoadPayload(ulong sequence, int word) => Volatile.Read(ref Word(Slot(sequence) + Payload(word)));
    public void StorePayload(ulong sequence, int word, ulong value) => Volatile.Write(ref Word(Slot(sequence) + Payload(word)), value);

    /// <summary>The ring's header, read word by word.</summary>
    public EventRingHeader Header => EventRingHeader.Read(Snapshot(EventRingBuffer.HeaderBytes));

    /// <summary>A copy of the whole ring, read word by word (little-endian), to compare it with another reader's view.</summary>
    public byte[] Snapshot() => Snapshot(_memory.Length);

    private byte[] Snapshot(int length)
    {
        var copy = new byte[length];
        for (var offset = 0; offset < length; offset += 8) BinaryPrimitives.WriteUInt64LittleEndian(copy.AsSpan(offset), Volatile.Read(ref Word(offset)));
        return copy;
    }

    private int Slot(ulong sequence) => EventRingBuffer.HeaderBytes + (int)((sequence - 1) & (Capacity - 1)) * EventRingBuffer.SlotBytes;
    private static int Payload(int word) => word is >= 1 and < EventRingBuffer.SlotBytes / 8
        ? word * 8 : throw new ArgumentOutOfRangeException(nameof(word), word, "A payload word is 1 to 7.");
    private ref ulong Word(int offset) => ref Unsafe.As<byte, ulong>(ref _memory[offset]);
}

/// <summary>One consumer's cursor over the event ring (the C# reference reader; the workers' own is the shared <c>EventRingCursor</c> of
/// <c>event-ring-cursor.js</c>, which reproduces these semantics across threads and is proven equal to it). A new epoch in the
/// header restarts the cursor at sequence 0. A slot overwritten before it was read, or a publication past the ring's capacity, is
/// counted in <see cref="Lapped"/> and skipped; it is never read as a record.</summary>
public sealed class EventRingConsumer
{
    public ulong Epoch { get; private set; }
    public ulong Cursor { get; private set; }
    public ulong Lapped { get; private set; }

    public bool TryRead(EventRingMemory ring, SimulationDomain filter, out SimulationEventRecord record)
    {
        record = default;
        if (ring.Epoch != Epoch) { Epoch = ring.Epoch; Cursor = 0; }
        var written = ring.LoadWriteSequence();
        while (Cursor < written)
        {
            if (written - Cursor > ring.Capacity) { Lapped += written - ring.Capacity - Cursor; Cursor = written - ring.Capacity; }
            var target = Cursor + 1;
            // The reader's seqlock, as the shared JS reader's (review F1): the sequence, every payload word, then the sequence again.
            var before = ring.LoadSequence(target);
            Span<byte> copy = stackalloc byte[EventRingBuffer.SlotBytes];
            for (var word = 1; word < EventRingBuffer.SlotBytes / 8; word++)
                BinaryPrimitives.WriteUInt64LittleEndian(copy[(word * 8)..], ring.LoadPayload(target, word));
            var after = ring.LoadSequence(target);
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

/// <summary>One read of the shared event ring reader (Story 9A.23, <c>event-ring-cursor.js</c> <c>eventDelivery</c>): the ring's Epoch
/// u64 at 0, Skipped u64 at 8 (the records this read passed without delivering: lapped by the writer, or reused while copied), then the
/// delivered whole records in strictly ascending sequence. Both consumers (the Animation worker and the main thread) decode it here.</summary>
public sealed record EventRingDelivery(ulong Epoch, ulong Skipped, SimulationEventRecord[] Records)
{
    public const int HeaderBytes = 16;

    public static EventRingDelivery Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderBytes || (bytes.Length - HeaderBytes) % EventRingBuffer.SlotBytes != 0)
            throw new ArgumentException("An event delivery is its header and whole records.");
        var epoch = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        var skipped = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]);
        var records = new SimulationEventRecord[(bytes.Length - HeaderBytes) / EventRingBuffer.SlotBytes];
        for (var i = 0; i < records.Length; i++)
        {
            records[i] = SimulationEventRecord.Read(bytes.Slice(HeaderBytes + i * EventRingBuffer.SlotBytes, EventRingBuffer.SlotBytes));
            if (records[i].Sequence == 0 || (i > 0 && records[i].Sequence <= records[i - 1].Sequence))
                throw new ArgumentException("Delivered event records are not in strictly ascending sequence.");
        }
        if (epoch == 0 && (records.Length != 0 || skipped != 0)) throw new ArgumentException("An event delivery without a ring carries nothing.");
        return new(epoch, skipped, records);
    }

    /// <summary>The C# twin of <c>eventDelivery</c>, for the reference reader in tests.</summary>
    public static byte[] Write(ulong epoch, ulong skipped, ReadOnlySpan<SimulationEventRecord> records)
    {
        var bytes = new byte[HeaderBytes + records.Length * EventRingBuffer.SlotBytes];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, epoch);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(8), skipped);
        for (var i = 0; i < records.Length; i++) records[i].Write(bytes.AsSpan(HeaderBytes + i * EventRingBuffer.SlotBytes, EventRingBuffer.SlotBytes));
        return bytes;
    }
}
