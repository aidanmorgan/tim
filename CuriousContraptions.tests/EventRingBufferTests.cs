using System;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class EventRingBufferTests
{
    [Fact]
    public void EventRingBuffer_InitializesCorrectly()
    {
        Span<byte> buffer = stackalloc byte[EventRingBuffer.TotalByteLength];
        EventRingBuffer.Initialize(buffer);

        var consumer = new EventRingConsumer();
        var success = consumer.TryRead(buffer, SimulationDomain.None, out _, out _);
        Assert.False(success);
        Assert.Equal(0UL, consumer.ReadCursor);
    }

    [Fact]
    public void EventRingBuffer_PublishesAndReadsWithIndependentConsumers()
    {
        byte[] buffer = new byte[EventRingBuffer.TotalByteLength];
        EventRingBuffer.Initialize(buffer);

        var consumerAll = new EventRingConsumer();
        var consumerMech = new EventRingConsumer();
        var consumerElec = new EventRingConsumer();

        // Publish Mechanical Event
        var ev1 = new SimulationEventRecord(0, 1000, SimulationDomain.Mechanical, SimulationEventKind.Impact, 1, 2, P0: 5.5f);
        EventRingBuffer.Publish(buffer, ev1);

        // Publish Electrical Event
        var ev2 = new SimulationEventRecord(0, 2000, SimulationDomain.Electrical, SimulationEventKind.ElectricalPowered, 3, 4, P0: 12f);
        EventRingBuffer.Publish(buffer, ev2);

        // consumerAll reads both
        Assert.True(consumerAll.TryRead(buffer, SimulationDomain.None, out var r1, out var o1));
        Assert.False(o1);
        Assert.Equal(SimulationEventKind.Impact, r1.EventKind);
        Assert.Equal(5.5f, r1.Impulse);

        Assert.True(consumerAll.TryRead(buffer, SimulationDomain.None, out var r2, out var o2));
        Assert.False(o2);
        Assert.Equal(SimulationEventKind.ElectricalPowered, r2.EventKind);
        Assert.Equal(12f, r2.Impulse);

        Assert.False(consumerAll.TryRead(buffer, SimulationDomain.None, out _, out _));

        // consumerMech reads only mechanical
        Assert.True(consumerMech.TryRead(buffer, SimulationDomain.Mechanical, out var rm1, out var om1));
        Assert.False(om1);
        Assert.Equal(SimulationEventKind.Impact, rm1.EventKind);
        Assert.False(consumerMech.TryRead(buffer, SimulationDomain.Mechanical, out _, out _));

        // consumerElec reads only electrical
        Assert.True(consumerElec.TryRead(buffer, SimulationDomain.Electrical, out var re1, out var oe1));
        Assert.False(oe1);
        Assert.Equal(SimulationEventKind.ElectricalPowered, re1.EventKind);
        Assert.False(consumerElec.TryRead(buffer, SimulationDomain.Electrical, out _, out _));
    }

    [Fact]
    public void EventRingBuffer_OverrunDetectedWhenCapacityExceeded()
    {
        const int smallCap = 16;
        byte[] buffer = new byte[EventRingBuffer.HeaderBytes + smallCap * EventRingBuffer.SlotBytes];
        EventRingBuffer.Initialize(buffer, smallCap);

        var consumer = new EventRingConsumer();

        // Publish 20 events (exceeds capacity 16)
        for (ulong i = 1; i <= 20; i++)
        {
            var ev = new SimulationEventRecord(0, i * 100, SimulationDomain.Mechanical, SimulationEventKind.Impact, i, 0, P0: (float)i);
            EventRingBuffer.Publish(buffer, ev);
        }

        Assert.True(consumer.TryRead(buffer, SimulationDomain.Mechanical, out var record, out var overrun));
        Assert.True(overrun);
        // Should have skipped to oldest available (seq 5 or 6)
        Assert.True(record.Sequence >= 5);
    }
}
