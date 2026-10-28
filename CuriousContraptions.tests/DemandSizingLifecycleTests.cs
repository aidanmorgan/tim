using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class DemandSizingLifecycleTests
{
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));

    private static WorkshopConstruction Scene(int count, ulong revision) => new(new(revision), Settings,
        new(Enumerable.Range(0, count).Select(i => (IWorkshopInstance)WorkshopInput.Generic(
            PartId.Basketball, new((ulong)i + 1), (float)(-7 + (i % 8) * 2), 4, (float)(-5 + (i / 8) * 2), 0, 0, 0, 1)).ToArray()));

    [Fact]
    public async Task SmallerLargerSmallerWorldsRunAndResetWithoutRetiredBodyOrContactState()
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), Settings,
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        foreach (var count in new[] { 1, 40, 2, 0 })
        {
            var scene = Scene(count, simulation.Construction.Revision.Value + 1);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(scene)).Outcome);
            var initial = simulation.Committed;
            Assert.Equal(count, initial.Bodies.Count);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            for (var tick = 0; tick < 30; tick++)
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            Assert.Equal(30UL, simulation.Committed.Tick.Value);
            Assert.Equal(count, simulation.Committed.Bodies.Count);
            for (var i = 0; i < count; i++)
            {
                Assert.Equal(new GpuBodyId((ulong)i + 1), simulation.Committed.Bodies[i].Body.Id);
                Assert.True(simulation.Committed.Bodies[i].Body.Velocity.Y < -1f);
                Assert.True(WorldY(simulation.Committed.Bodies[i]) < WorldY(initial.Bodies[i]));
            }
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
            NoFixedCapacityTests.AssertSameState(initial, simulation.Committed, "restored smaller/larger world");
        }
    }

    [Fact]
    public async Task DiscardedLargerAdmissionLeavesOldWorldRunnableAndNextSmallerAdmissionSucceeds()
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), new(101, 206));
        var first = await device.Admit(Scene(2, 1), new(1), Profile, new(1));
        device.Commit(first.Sequence); device.Discard(first.Sequence);
        var moving = await device.Advance(first.Read, new(2), new WorkshopTickControls([]));
        device.Commit(moving.Sequence); device.Discard(moving.Sequence);
        var abandoned = await device.Admit(Scene(40, 2), new(2), Profile, new(3));
        Assert.Equal(40, abandoned.Read.Bodies.Count);
        device.Discard(abandoned.Sequence);
        var continued = await device.Advance(moving.Read, new(4), new WorkshopTickControls([]));
        Assert.Equal(1UL, continued.Read.Epoch.Value);
        Assert.Equal(2UL, continued.Read.Tick.Value);
        Assert.Equal(2, continued.Read.Bodies.Count);
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(moving.Read.Bodies[i].Body.Id, continued.Read.Bodies[i].Body.Id);
            Assert.True(continued.Read.Bodies[i].Body.Velocity.Y < moving.Read.Bodies[i].Body.Velocity.Y);
        }
        device.Commit(continued.Sequence); device.Discard(continued.Sequence);
        var smaller = await device.Admit(Scene(1, 3), new(3), Profile, new(5));
        Assert.Single(Enumerable.Range(0, smaller.Read.Bodies.Count));
        Assert.Equal(new GpuBodyId(1), smaller.Read.Bodies[0].Body.Id);
        device.Commit(smaller.Sequence); device.Discard(smaller.Sequence);
        var next = await device.Advance(smaller.Read, new(6), new WorkshopTickControls([]));
        Assert.Equal(3UL, next.Read.Epoch.Value);
        Assert.Equal(1, next.Read.Bodies.Count);
        Assert.True(next.Read.Bodies[0].Body.Velocity.Y < 0);
        device.Commit(next.Sequence); device.Discard(next.Sequence);
    }

    [Theory]
    [InlineData(WorkshopRejection.Transport)]
    [InlineData(WorkshopRejection.Capacity)]
    public void InstallationRefusalCarriesOnlyItsPermittedTypedReasons(WorkshopRejection reason)
    {
        Assert.Equal(reason, new WorkshopInstallationException(reason).Reason);
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkshopInstallationException(WorkshopRejection.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkshopInstallationException((WorkshopRejection)int.MaxValue));
    }

    [Fact]
    public void PhysicalBackingGrowthUsesCommonDefaultWatermarkAndSaturatesWithoutHugeAllocation()
    {
        const int common = 16 * 1024;
        Assert.Equal(common, BrowserGpuTransport.DefaultWorldCapacity);
        Assert.Equal(common, BrowserGpuTransport.WorldCapacity(common, 12 * 1024));
        Assert.Equal(2 * common, BrowserGpuTransport.WorldCapacity(common, 12 * 1024 + 1));
        Assert.Equal(40_000, BrowserGpuTransport.WorldCapacity(common, 30_000));
        Assert.Equal(Array.MaxLength, BrowserGpuTransport.WorldCapacity(Array.MaxLength - 1, Array.MaxLength));
        Assert.Equal(Array.MaxLength, BrowserGpuTransport.WorldCapacity(Array.MaxLength, Array.MaxLength));
        Assert.Throws<WorkshopStorageException>(() => BrowserGpuTransport.WorldCapacity(common, int.MaxValue));
        Assert.Throws<WorkshopStorageException>(() => PhysicsMotionRead.ByteLength(int.MaxValue));
        var impossible = new PhysicsWorldLayout(int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.Throws<WorkshopStorageException>(() => impossible.ByteLength);
    }

    [Fact]
    public async Task PhysicalMemoryExposesOnlyLogicalRowsAndReusesReservedBuffersAcrossTicks()
    {
        var transport = new BrowserGpuTransport(new EventRingPublication());
        try
        {
            await transport.Initialize();
            foreach (var count in new[] { 1, 40, 2 })
            {
                var epoch = new SimulationEpoch((ulong)count);
                var admitted = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(Scene(count, 1), new(101, 206)), epoch, Profile);
                await transport.Stage(admitted, WorkshopGpuOperation.Admit, PhysicsTickInputs.Admission);
                var first = transport.Read();
                Assert.Equal(admitted.Length, first.World.Length);
                Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(first.World, out var backing));
                var expectedCapacity = admitted.Length <= 12 * 1024 ? 16 * 1024 : Math.Max(32 * 1024, (4 * admitted.Length + 2) / 3);
                Assert.Equal(expectedCapacity, backing.Array!.Length);
                Assert.Equal(count, PhysicsGpuAbi.Layout(first.World.Span).Dynamic);
                transport.PrepareEvents(EventRingBuffer.Batch(new(32, EventRingBuffer.SlotBytes, epoch.Value, 0), []));
                transport.Commit();
                for (ulong tick = 1; tick <= 2; tick++)
                {
                    await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, PhysicsTickInputs.Admission);
                    var next = transport.Read();
                    Assert.Equal(admitted.Length, next.World.Length);
                    Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(next.World, out var nextBacking));
                    Assert.Equal(expectedCapacity, nextBacking.Array!.Length);
                    if (tick == 2) Assert.Same(backing.Array, nextBacking.Array);
                    var validated = PhysicsGpuAbi.ValidateCandidate(next.World.Span, first.World.Span, new(tick), next.Interactions, first.Interactions);
                    Assert.Equal(count, validated.Bodies.Count);
                    Assert.All(Enumerable.Range(0, count), i => Assert.True(validated.Bodies[i].Body.Velocity.Y < 0));
                    transport.PrepareEvents(EventRingBuffer.Batch(new(32, EventRingBuffer.SlotBytes, epoch.Value, 0), []));
                    transport.Commit();
                    first = next;
                }
            }
        }
        finally { transport.Dispose(); }
    }

    private static float WorldY(PhysicsBodyRead body) => body.Body.Cell.Y + body.Body.Local.Y;
}
