using System.Buffers.Binary;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.22: the event ring's 64-byte record and header layout, the reference writer (the twin of worker.js
/// writeEventRing) and the epoch-aware reference consumer.</summary>
public sealed class EventRingBufferTests
{
    private static SimulationEventRecord Record(ulong sequence, SimulationEventKind kind = SimulationEventKind.Impact, ulong source = 1) =>
        new(sequence, 7, kind, source, 2, 28, .25f, 1.5f, 3, 0);

    private static byte[] Publication(ulong epoch, uint capacity, ulong lapped, params SimulationEventRecord[] records) =>
        EventRingBuffer.Batch(new(capacity, EventRingBuffer.SlotBytes, epoch, records.Length == 0 ? 0 : records[^1].Sequence, lapped), records);

    [Fact]
    public void ARecordWritesEveryFieldAtItsOffsetAndReadsBackExactly()
    {
        var record = new SimulationEventRecord(0x0102030405060708, 9, SimulationEventKind.ContactLoad, 11, 12, 13, -.5f, 2.25f, 14, 1);
        var slot = new byte[EventRingBuffer.SlotBytes];
        record.Write(slot);
        Assert.Equal((0x0102030405060708UL, 9UL, 5u, (uint)SimulationDomain.Mechanical, 11UL, 12UL, 13u, -.5f, 2.25f, 14u, 1u, 0u),
            (BinaryPrimitives.ReadUInt64LittleEndian(slot), BinaryPrimitives.ReadUInt64LittleEndian(slot.AsSpan(8)),
                BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(16)), BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(20)),
                BinaryPrimitives.ReadUInt64LittleEndian(slot.AsSpan(24)), BinaryPrimitives.ReadUInt64LittleEndian(slot.AsSpan(32)),
                BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(40)), BinaryPrimitives.ReadSingleLittleEndian(slot.AsSpan(44)),
                BinaryPrimitives.ReadSingleLittleEndian(slot.AsSpan(48)), BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(52)),
                BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(56)), BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(60))));
        Assert.Equal(record, SimulationEventRecord.Read(slot));
    }

    // Every kind has exactly one engine-owned domain; None and an undefined kind are no record.
    [Theory]
    [InlineData(SimulationEventKind.Impact, SimulationDomain.Mechanical)]
    [InlineData(SimulationEventKind.Passage, SimulationDomain.Mechanical)]
    [InlineData(SimulationEventKind.ContactLoad, SimulationDomain.Mechanical)]
    [InlineData(SimulationEventKind.Captured, SimulationDomain.Mechanical)]
    [InlineData(SimulationEventKind.ThresholdReached, SimulationDomain.Signal)]
    [InlineData(SimulationEventKind.MemoryChanged, SimulationDomain.Signal)]
    [InlineData(SimulationEventKind.TriggerActivated, SimulationDomain.Signal)]
    [InlineData(SimulationEventKind.ElectricalPowered, SimulationDomain.Electrical)]
    public void EachKindCarriesItsOneDomain(SimulationEventKind kind, SimulationDomain domain) => Assert.Equal(domain, SimulationEvents.Domain(kind));

    [Fact]
    public void TheClosedKindSetHasNoElementNamedOrUnproducedKind()
    {
        Assert.Equal(["None", "Impact", "Passage", "ThresholdReached", "MemoryChanged", "ContactLoad", "Captured", "ElectricalPowered", "TriggerActivated"],
            Enum.GetNames<SimulationEventKind>());
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEvents.Domain(SimulationEventKind.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEvents.Domain((SimulationEventKind)9));
    }

    [Theory]
    [InlineData(16, 0u)]  // kind None
    [InlineData(16, 9u)]  // undefined kind
    [InlineData(20, 4u)]  // a domain other than the kind's
    [InlineData(56, 2u)]  // a level other than 0 or 1
    [InlineData(60, 1u)]  // the reserved word
    [InlineData(24, 0u)]  // no source
    public void AnInvalidRecordIsRefused(int offset, uint value)
    {
        var slot = new byte[EventRingBuffer.SlotBytes];
        Record(1).Write(slot);
        if (offset == 24) BinaryPrimitives.WriteUInt64LittleEndian(slot.AsSpan(offset), value);
        else BinaryPrimitives.WriteUInt32LittleEndian(slot.AsSpan(offset), value);
        Assert.ThrowsAny<ArgumentException>(() => SimulationEventRecord.Read(slot));
    }

    [Theory]
    [InlineData(0UL, 1UL)]
    [InlineData(1UL, 1UL)]
    [InlineData(180UL, 256UL)]
    [InlineData(256UL, 256UL)]
    [InlineData(257UL, 512UL)]
    public void TheCapacityIsTheNextPowerOfTwoOfTheSlotsAtLeastOne(ulong slots, ulong capacity) =>
        Assert.Equal((uint)capacity, EventRingBuffer.Capacity(slots));

    [Fact]
    public void AHeaderWithoutAPowerOfTwoCapacityOrEpochOrWithStrayBytesIsRefused()
    {
        foreach (var header in new EventRingHeader[] { new(0, 64, 1, 0, 0), new(3, 64, 1, 0, 0), new(4, 32, 1, 0, 0), new(4, 64, 0, 0, 0) })
        {
            var bytes = new byte[EventRingBuffer.HeaderBytes];
            header.Write(bytes);
            Assert.Throws<ArgumentException>(() => EventRingHeader.Read(bytes));
        }
        var stray = new byte[EventRingBuffer.HeaderBytes];
        new EventRingHeader(4, 64, 1, 0, 0).Write(stray);
        stray[40] = 1;
        Assert.Throws<ArgumentException>(() => EventRingHeader.Read(stray));
    }

    // Independent consumers each hold their own cursor and filter by domain; a second publication continues the same ring.
    [Fact]
    public void IndependentConsumersReadEachRecordOnceThroughTheirOwnFilters()
    {
        var ring = EventRingBuffer.Append(null, Publication(3, 8, 0, Record(1), Record(2, SimulationEventKind.ElectricalPowered)));
        ring = EventRingBuffer.Append(ring, Publication(3, 8, 0, Record(3, SimulationEventKind.MemoryChanged)));
        List<ulong> Read(SimulationDomain filter)
        {
            var consumer = new EventRingConsumer();
            var sequences = new List<ulong>();
            while (consumer.TryRead(ring, filter, out var record)) sequences.Add(record.Sequence);
            Assert.False(consumer.TryRead(ring, filter, out _));
            Assert.Equal(0UL, consumer.Lapped);
            return sequences;
        }
        Assert.Equal([1UL, 2UL, 3UL], Read(SimulationDomain.None));
        Assert.Equal([1UL], Read(SimulationDomain.Mechanical));
        Assert.Equal([2UL], Read(SimulationDomain.Electrical));
        Assert.Equal([2UL, 3UL], Read(SimulationDomain.Electrical | SimulationDomain.Signal));
    }

    // A slot whose sequence word is not its own (zero while the writer copies it) is never read as a record: it is skipped and counted.
    [Fact]
    public void ASlotBeingWrittenIsSkippedAndCountedNeverRead()
    {
        var ring = EventRingBuffer.Append(null, Publication(3, 4, 0, Record(1), Record(2), Record(3)));
        ring.PublishSequence(2, 0); // the writer's first step on record 2's slot: its sequence word is 0 while the slot is written
        var consumer = new EventRingConsumer();
        var read = new List<ulong>();
        while (consumer.TryRead(ring, SimulationDomain.None, out var record)) read.Add(record.Sequence);
        Assert.Equal([1UL, 3UL], read);
        Assert.Equal(1UL, consumer.Lapped);
    }

    // A new epoch is a fresh ring: the writer replaces it and every consumer restarts its cursor at sequence 0.
    [Fact]
    public void ANewEpochReplacesTheRingAndRestartsEveryCursor()
    {
        var first = EventRingBuffer.Append(null, Publication(3, 4, 0, Record(1), Record(2)));
        var consumer = new EventRingConsumer();
        while (consumer.TryRead(first, SimulationDomain.None, out _)) { }
        Assert.Equal((3UL, 2UL), (consumer.Epoch, consumer.Cursor));
        var second = EventRingBuffer.Append(first, Publication(4, 4, 0, Record(1, SimulationEventKind.Passage)));
        Assert.NotSame(first, second);
        Assert.True(consumer.TryRead(second, SimulationDomain.None, out var record));
        Assert.Equal((4UL, 1UL, SimulationEventKind.Passage), (consumer.Epoch, record.Sequence, record.Kind));
        Assert.Same(second, EventRingBuffer.Append(second, Publication(4, 4, 0, Record(2))));
        Assert.NotSame(second, EventRingBuffer.Append(second, Publication(4, 8, 0, Record(3))));
    }

    [Fact]
    public void APublicationOfMoreRecordsThanItsRingOrOfPartialRecordsIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Publication(3, 2, 0, Record(1), Record(2), Record(3)));
        var partial = Publication(3, 4, 0, Record(1));
        Assert.Throws<ArgumentException>(() => EventRingBuffer.Append(null, partial.AsSpan(0, partial.Length - 1)));
    }
}
