using System.Buffers.Binary;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>Opt-in observation of the real transport boundary. It never solves, edits or substitutes a candidate.</summary>
internal sealed class ObservedWorldTransport(IWorkshopGpuTransport inner) : IWorkshopGpuTransport
{
    private byte[]? _pending;
    private byte[] _committed = [];
    public ReadOnlyMemory<byte> TransportCommitted => _committed;
    public async Task Initialize()
    {
        try { await inner.Initialize(); }
        finally { _pending = null; _committed = []; }
    }
    public Task Stage(ReadOnlyMemory<byte> input, WorkshopGpuOperation operation, PhysicsTickInputs inputs) => inner.Stage(input, operation, inputs);
    public WorkshopTransportCandidate Read()
    {
        var result = inner.Read();
        _pending = result.World.ToArray(); // Exact logical slice, copied before the real transport can reuse its backing.
        return result;
    }
    public void PrepareEvents(byte[] batch) => inner.PrepareEvents(batch);
    public void PublishEvents() => inner.PublishEvents();
    public void Commit()
    {
        var pending = _pending ?? throw new InvalidOperationException("No observed candidate.");
        try { inner.Commit(); _committed = pending; }
        finally { _pending = null; }
    }
    public void Discard()
    {
        try { inner.Discard(); }
        finally { _pending = null; }
    }
    public bool DeviceReady() => inner.DeviceReady();
    public void Dispose()
    {
        try { inner.Dispose(); }
        finally { _pending = null; _committed = []; }
    }
}

public sealed class ObservedWorldTransportTests
{
    private static ulong Tick(ReadOnlyMemory<byte> world) => BinaryPrimitives.ReadUInt64LittleEndian(world.Span[PhysicsGpuAbi.HeaderTickOffset..]);
    [Fact]
    public async Task InactiveOwnedRequestLaneRefusesWithoutPreparingOrAdvancing()
    {
        var transport = new ObservedWorldTransport(new BrowserGpuTransport(new EventRingPublication()));
        try
        {
            await transport.Initialize();
            var source = new LawScene(false);
            source.Box(new(0f, 5f, 0f), new(.1f), 1f, LawMaterial.Domino);
            var admitted = source.Admit().Admitted;
            var inactive = PhysicsTickInputs.Admission with { OwnedFiniteWork = new(new(1), new(99), new(1)) };
            await Assert.ThrowsAsync<ArgumentException>(() => transport.Stage(admitted, WorkshopGpuOperation.Admit, inactive));
            Assert.True(transport.TransportCommitted.IsEmpty);
            await transport.Stage(admitted, WorkshopGpuOperation.Admit, PhysicsTickInputs.Admission);
            transport.Read();
            transport.PrepareEvents(EventRingBuffer.Batch(new(32, EventRingBuffer.SlotBytes, 1, 0), []));
            transport.Commit();
            await Assert.ThrowsAsync<ArgumentException>(() => transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, inactive));
            await Assert.ThrowsAsync<ArgumentException>(() => transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.ApplyFiniteWork, PhysicsTickInputs.Admission));
            Assert.Equal(0UL, Tick(transport.TransportCommitted));
            Assert.Equal(new PhysicalCommitRevision(0), PhysicsGpuAbi.ReadPhysicalRevision(transport.TransportCommitted.Span));
            await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, PhysicsTickInputs.Admission);
            var candidate = transport.Read();
            Assert.Equal(1UL, Tick(candidate.World));
            transport.Discard();
            Assert.Equal(0UL, Tick(transport.TransportCommitted));
        }
        finally { transport.Dispose(); }
    }

    [Fact]
    public async Task PendingWorldsStayInvisibleAndAcceptedCopiesSurviveDiscardAndBackingReuse()
    {
        var transport = new ObservedWorldTransport(new BrowserGpuTransport(new EventRingPublication()));
        try
        {
            await transport.Initialize(); Assert.True(transport.TransportCommitted.IsEmpty);
            var source = new LawScene(false); var body = source.Box(new(0, 5, 0), new(.1f), 1f, LawMaterial.Domino);
            var admitted = source.Admit().Admitted;
            await transport.Stage(admitted, WorkshopGpuOperation.Admit, PhysicsTickInputs.Admission);
            transport.Read(); Assert.True(transport.TransportCommitted.IsEmpty);
            Assert.Throws<InvalidOperationException>(() => transport.Commit());
            Assert.True(transport.TransportCommitted.IsEmpty);
            transport.Read();
            transport.PrepareEvents(EventRingBuffer.Batch(new(32, EventRingBuffer.SlotBytes, 1, 0), []));
            transport.Commit();
            var original = transport.TransportCommitted;
            Assert.Equal(admitted.Length, original.Length); Assert.Equal(0UL, Tick(original));
            var initial = PhysicsGpuAbi.ReadDynamicBodies(original.Span);
            Assert.True(initial.TryGet(body, out var before));
            await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, PhysicsTickInputs.Admission);
            transport.Read(); Assert.Equal(0UL, Tick(transport.TransportCommitted));
            transport.Discard(); Assert.Equal(0UL, Tick(transport.TransportCommitted));
            for (ulong tick = 1; tick <= 3; tick++)
            {
                await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, PhysicsTickInputs.Admission);
                transport.Read(); Assert.Equal(tick - 1, Tick(transport.TransportCommitted));
                transport.PrepareEvents(EventRingBuffer.Batch(new(32, EventRingBuffer.SlotBytes, 1, 0), [])); transport.Commit();
                Assert.Equal(tick, Tick(transport.TransportCommitted));
            }
            Assert.True(PhysicsGpuAbi.ReadDynamicBodies(transport.TransportCommitted.Span).TryGet(body, out var after));
            Assert.True(after.Body.Velocity.Y < before.Body.Velocity.Y);
            Assert.True(PhysicsGpuAbi.ReadDynamicBodies(original.Span).TryGet(body, out var retained));
            Assert.Equal(before.Body.Local, retained.Body.Local); Assert.Equal(before.Body.Velocity, retained.Body.Velocity);
            Assert.Equal(0UL, Tick(original)); // Semantic original state survives pooled backing reuse.
            await transport.Initialize(); Assert.True(transport.TransportCommitted.IsEmpty);
        }
        finally { transport.Dispose(); }
        Assert.True(transport.TransportCommitted.IsEmpty);
    }
}
