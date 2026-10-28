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

/// <summary>The 64-byte ring header (Story 9A.22 D6): Capacity u32 at 0 (actual demand plus bounded growth spare), SlotBytes u32 at 4, Epoch u64 at 8 (the
/// admitted world's simulation epoch), WriteSequence u64 at 16 (the last published sequence, stored after its slots) ; bytes 24 through 63 are reserved zero.</summary>
public readonly record struct EventRingHeader(uint Capacity, uint SlotBytes, ulong Epoch, ulong WriteSequence)
{
    public const int CapacityOffset = 0, SlotBytesOffset = 4, EpochOffset = 8, WriteSequenceOffset = 16;

    public void Write(Span<byte> header)
    {
        if (header.Length < EventRingBuffer.HeaderBytes) throw new ArgumentException("An event ring header is 64 bytes.");
        header[..EventRingBuffer.HeaderBytes].Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(header[CapacityOffset..], Capacity);
        BinaryPrimitives.WriteUInt32LittleEndian(header[SlotBytesOffset..], SlotBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(header[EpochOffset..], Epoch);
        BinaryPrimitives.WriteUInt64LittleEndian(header[WriteSequenceOffset..], WriteSequence);
    }

    public static EventRingHeader Read(ReadOnlySpan<byte> header)
    {
        if (header.Length < EventRingBuffer.HeaderBytes) throw new ArgumentException("An event ring header is 64 bytes.");
        var value = new EventRingHeader(BinaryPrimitives.ReadUInt32LittleEndian(header[CapacityOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(header[SlotBytesOffset..]), BinaryPrimitives.ReadUInt64LittleEndian(header[EpochOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(header[WriteSequenceOffset..]));
        if (value.Capacity == 0 || value.Capacity > (Array.MaxLength - EventRingBuffer.HeaderBytes) / EventRingBuffer.SlotBytes || value.SlotBytes != EventRingBuffer.SlotBytes ||
            value.Epoch == 0 || header[24..EventRingBuffer.HeaderBytes].IndexOfAnyExcept((byte)0) >= 0)
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
    public static int ByteLength(uint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfZero(capacity);
        if (capacity > (Array.MaxLength - HeaderBytes) / SlotBytes)
            throw new EventPreparationException(EventPublicationPreparation.StorageUnavailable);
        return checked(HeaderBytes + (int)capacity * SlotBytes);
    }

    public const uint DefaultCapacity = 32;

    /// <summary>Reserve once at the prospective 75% watermark, doubling current capacity
    /// or satisfying bulk headroom. Saturate only on demand at the actual storage bound.</summary>
    public static uint Capacity(uint current, ulong required)
    {
        var limit = (ulong)(Array.MaxLength - HeaderBytes) / SlotBytes;
        if (required > limit || current > limit)
            throw new EventPreparationException(EventPublicationPreparation.StorageUnavailable);
        if (required <= 3UL * current / 4) return current;
        return (uint)Math.Min(limit, Math.Max(2UL * current, (4UL * required + 2) / 3));
    }

    /// <summary>Reserves an exact contiguous sequence interval before any publication allocation.</summary>
    public static ulong LastSequence(ulong previous, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if ((ulong)count > ulong.MaxValue - previous)
            throw new EventPreparationException(EventPublicationPreparation.SequenceExhausted);
        return previous + (ulong)count;
    }

    /// <summary>Composes one publication: the header the ring will hold after it, then each sequenced record.</summary>
    public static byte[] Batch(EventRingHeader header, ReadOnlySpan<SimulationEventRecord> records)
    {
        if (records.Length > header.Capacity) throw new ArgumentException("A publication never holds more records than its ring.");
        var bytes = new byte[checked(HeaderBytes + records.Length * SlotBytes)];
        header.Write(bytes);
        for (var i = 0; i < records.Length; i++) records[i].Write(bytes.AsSpan(HeaderBytes + i * SlotBytes, SlotBytes));
        return bytes;
    }

    /// <summary>Prepares all publication storage and validates it without changing an advertised ring.</summary>
    public static PreparedEventRing Prepare(EventRingMemory? ring, ReadOnlySpan<byte> batch)
    {
        var header = EventRingHeader.Read(batch);
        _ = ByteLength(header.Capacity);
        var count = (batch.Length - HeaderBytes) / SlotBytes;
        if ((batch.Length - HeaderBytes) % SlotBytes != 0 || count > header.Capacity || (ulong)count > header.WriteSequence)
            throw new ArgumentException("A publication is whole records within its ring.");
        var sameEpoch = ring is not null && ring.Epoch == header.Epoch;
        var priorSequence = sameEpoch ? ring!.LoadWriteSequence() : 0;
        if (sameEpoch && (header.Capacity < ring!.Capacity || header.WriteSequence < priorSequence))
            throw new ArgumentException("A publication cannot shrink or rewind its current epoch.");
        if (header.WriteSequence - priorSequence != (ulong)count)
            throw new ArgumentException("A publication must include every new sequence.");
        for (var i = 0; i < count; i++)
        {
            var record = SimulationEventRecord.Read(batch.Slice(HeaderBytes + i * SlotBytes, SlotBytes));
            if (record.Sequence != priorSequence + (ulong)i + 1)
                throw new ArgumentException("Publication records must have contiguous sequences.");
        }
        var copy = batch.ToArray();
        var replacement = !sameEpoch || ring!.Capacity != header.Capacity;
        var target = replacement ? new EventRingMemory(header.Capacity, header.Epoch) : ring!;
        if (sameEpoch && replacement)
        {
            var first = priorSequence > ring!.Capacity ? priorSequence - ring.Capacity + 1 : 1;
            for (var sequence = first; sequence <= priorSequence && sequence != 0; sequence++)
            {
                if (ring!.LoadSequence(sequence) != sequence) throw new ArgumentException("Retained ring history is incomplete.");
                for (var word = 1; word < SlotBytes / 8; word++)
                    target.StorePayload(sequence, word, ring.LoadPayload(sequence, word));
                if (ring.LoadSequence(sequence) != sequence) throw new ArgumentException("Ring history changed during preparation.");
                target.PublishSequence(sequence, sequence);
            }
            target.StoreWriteSequence(priorSequence);
        }
        return new(target, header, copy, replacement);
    }

    /// <summary>The native reference uses the same prepare/publish lifecycle as the shipped host.</summary>
    public static EventRingMemory Append(EventRingMemory? ring, ReadOnlySpan<byte> batch) => Prepare(ring, batch).Publish();

}

/// <summary>Validated private publication; Publish performs no allocation or validation.</summary>
public sealed class PreparedEventRing
{
    private readonly EventRingMemory _ring;
    private readonly EventRingHeader _header;
    private readonly byte[] _batch;
    public bool ReplacesRing { get; }
    internal PreparedEventRing(EventRingMemory ring, EventRingHeader header, byte[] batch, bool replacement)
    { _ring = ring; _header = header; _batch = batch; ReplacesRing = replacement; }
    public EventRingMemory Publish()
    {
        for (var offset = EventRingBuffer.HeaderBytes; offset < _batch.Length; offset += EventRingBuffer.SlotBytes)
        {
            var sequence = BinaryPrimitives.ReadUInt64LittleEndian(_batch.AsSpan(offset));
            _ring.PublishSequence(sequence, 0);
            for (var word = 1; word < EventRingBuffer.SlotBytes / 8; word++)
                _ring.StorePayload(sequence, word, BinaryPrimitives.ReadUInt64LittleEndian(_batch.AsSpan(offset + word * 8)));
            _ring.PublishSequence(sequence, sequence);
        }
        _ring.StoreWriteSequence(_header.WriteSequence);
        return _ring;
    }
}

public enum EventPublicationPreparation { Ready = 0, StorageUnavailable = 1, SequenceExhausted = 2 }

/// <summary>A checked precommit resource refusal, distinct from malformed event data.</summary>
public sealed class EventPreparationException : Exception
{
    public EventPublicationPreparation Reason { get; }
    public EventPreparationException(EventPublicationPreparation reason) : base("Event preparation could not reserve its required storage or sequence.")
    {
        if (reason is not (EventPublicationPreparation.StorageUnavailable or EventPublicationPreparation.SequenceExhausted))
            throw new ArgumentOutOfRangeException(nameof(reason));
        Reason = reason;
    }
}

/// <summary>The existing worker host boundary, explicitly bound by native and WASM callers.</summary>
public interface IEventRingPublication
{
    EventPublicationPreparation Prepare(byte[] batch);
    void Commit();
    void Discard();
    void Publish();
    void Dispose();
}

/// <summary>Native shared-memory publication owner. Preparation never modifies Memory;
/// discarded storage cannot be published and committed storage needs no further allocation.</summary>
public sealed class EventRingPublication : IEventRingPublication
{
    public EventRingMemory? Memory { get; private set; }
    private PreparedEventRing? _candidate;
    private PreparedEventRing? _committed;
    private bool _disposed;
    public EventPublicationPreparation Prepare(byte[] batch)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_candidate is not null) throw new InvalidOperationException("Event publication is already prepared.");
        try { _candidate = EventRingBuffer.Prepare(Memory, batch); }
        catch (OutOfMemoryException) { return EventPublicationPreparation.StorageUnavailable; }
        return EventPublicationPreparation.Ready;
    }
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _committed = _candidate ?? throw new InvalidOperationException("No prepared event candidate.");
        _candidate = null;
    }
    public void Publish()
    {
        if (_committed is null) return;
        Memory = _committed.Publish();
        _committed = null;
    }
    public void Discard() => _candidate = null;
    public void Dispose() { _disposed = true; _candidate = null; _committed = null; Memory = null; }
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
        if (capacity == 0 || epoch == 0) throw new ArgumentException("Unsupported event ring layout.");
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

    private int Slot(ulong sequence) => EventRingBuffer.HeaderBytes + (int)((sequence - 1) % Capacity) * EventRingBuffer.SlotBytes;
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
