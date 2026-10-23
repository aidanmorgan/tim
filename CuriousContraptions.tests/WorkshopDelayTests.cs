using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopDelayTests
{
    private static WorkshopGenericInstance Delay(float seconds) => WorkshopInput.Generic(PartId.Delay, new(3), 1, 2, -.5, 0, 0, 0, 1, new InstanceParameterCollection([new(new ParameterId("delay_seconds"), seconds)]));
    [Fact]
    public void CanonicalF32ProductCeilingPreservesDeadlineBoundaries()
    {
        foreach (var (rate, expected) in new[] {
            (SimulationCadence.Hz60, new uint[] {6,60,537,720}),
            (SimulationCadence.Hz120, new uint[] {12,120,1073,1440}),
            (SimulationCadence.Hz240, new uint[] {24,240,2145,2880}) })
        {
            var durations = new float[] {.1f,1f,8.9375f,12f};
            for (var i = 0; i < durations.Length; i++)
                Assert.Equal(expected[i], ActivationTimerDeclaration.Duration(durations[i], rate));
        }
        // Fixed boundary expectations: the primitive accepts positive values below the package minimum.
        foreach (var (seconds, ticks) in new[] { (MathF.BitDecrement(.1f), 12u), (.1f, 12u), (MathF.BitIncrement(.1f), 13u),
            (MathF.BitDecrement(1f), 120u), (1f, 120u), (MathF.BitIncrement(1f), 121u), (12f, 1440u) })
            Assert.Equal(ticks, ActivationTimerDeclaration.Duration(seconds, SimulationCadence.Hz120));
        Assert.Equal(12u, ActivationTimerDeclaration.Duration(.1f, SimulationCadence.Hz120));
        Assert.Equal(24u, ActivationTimerDeclaration.Duration(.2f, SimulationCadence.Hz120));
    }
    [Fact]
    public void DurationPoseAndConnectionsRoundTripAndCompileOnlySourceBaseBox()
    {
        var trigger = WorkshopInput.Generic(PartId.ImpactSwitch, new(1), -2, 2, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(2), 3, 2, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(trigger, lamp, Delay((float)8.9375)), Connections: new(
                new(new(1), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation),
                new(new(3), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var saved = new WorkshopSavedConstruction(construction, new(4));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1,2));
        var box = Assert.Single(scene.Colliders.ToArray(), c => c.Body == new GpuBodyId(3));
        Assert.Equal(ColliderShapeKind.Box, box.Shape);
        Assert.Equal(new MetreVector((Half)0,(Half)(-.7),(Half)0), box.Pose.Translation);
        Assert.Equal(new MetreVector((Half).675,(Half).075,(Half).4), box.HalfExtents);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == box.Material);
        Assert.Equal(new Restitution((Half)1), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
    }
    [Fact]
    public void InvalidCanonicalDurationPaddingAndPreviousSchemasReject()
    {
        var saved = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), new(Delay((float)1))), new(4));
        var bytes = WorkshopSaveCodec.Encode(saved);
        foreach (var invalid in new[] { 0f, -.1f, .09f, 13f, float.NaN, float.PositiveInfinity, MathF.BitDecrement(.1f), MathF.BitIncrement(12f) })
        {
            var malformedConstruction = saved.Construction with {
                Instances = new WorkshopInstances(new WorkshopGenericInstance(PartId.Delay, new(2), new(0, 0, 0), new(0, 0, 0), CanonicalRotation.Identity) { Parameters = new([new(new ParameterId("delay_seconds"), invalid)]) })
            };
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, malformedConstruction, validate: false);
            var savedBytes = SaveFixture.Finish(builder, crOffset, saved.NextBodyId.Value);
            SaveFixture.Refused(float.IsFinite(invalid) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,savedBytes));
        }
        foreach (var invalid in new[] {float.NaN,float.PositiveInfinity,.09f,12.01f})
            Assert.Throws<ArgumentException>(() => Delay(invalid));
    }
    [Fact]
    public void FullPopulationAndDelayDirectionUseExistingCapacities()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Generic(PartId.Basketball,new(1),0,5,0,0,0,0,1),
            WorkshopInput.Generic(PartId.Receiver, new(2),4,1,0,0,0,0,1),
            WorkshopInput.Generic(PartId.Ramp, new(3),-4,2,0,0,0,0,1),
            WorkshopInput.Generic(PartId.Ramp, new(4),-2,2,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(5),0,1,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(6),2,1,0,0,0,0,1),
            PlacedParts.Lamp(new(7),5,2,0,0,0,0,1), Delay((float)1) with {Id=new(8)});
        var construction = new WorkshopConstruction(new(1),WorkshopCadenceSettings.Default(),instances);
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(1,2));
        Assert.Equal(9,scene.Bodies.Length);
        // Free play has no per-kind count: a third switch is admitted within the compiled tables.
        new WorkshopInstances(instances[4],instances[5],
            WorkshopInput.Generic(PartId.ImpactSwitch, new(9),3,1,0,0,0,0,1)).Validate();
        foreach (var edge in new[] {
            new WorkshopConnection(new(8),WorkshopSocket.ActivationIn,new(7),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation),
            new WorkshopConnection(new(5),WorkshopSocket.ActivationOut,new(8),WorkshopSocket.ActivationOut,WorkshopConnectionDomain.Activation),
            new WorkshopConnection(new(8),WorkshopSocket.ActivationOut,new(7),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Electrical) })
            EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (construction with {Connections=new(edge)}).Validate());
    }
    [Fact]
    public void PortsAndPuzzlePopulationRemainBounded()
    {
        Assert.True(DeclaredPorts.Current.Has(PartId.Delay, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.True(DeclaredPorts.Current.Has(PartId.Delay, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.False(DeclaredPorts.Current.Has(PartId.Delay, WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output));
        new WorkshopInstances(Delay((float)1), Delay((float)2) with {Id=new(4)}).Validate(); // No per-kind delay count in free play.
        var puzzle = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(1), new((Half)0));
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => puzzle.WithInstance(Delay((float)1)).Validate());
    }

    private static WorkshopConnection Signal(ulong source, ulong target) =>
        new(new(source), WorkshopSocket.ActivationOut, new(target), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static ActivationLatch Lamp(WorkshopRead read) =>
        Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]).Single(value => value.Owner.Value == 4);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealContactChainedTimersFireAtThirtySixTicksRegardlessOfEntityOrder(bool reverse)
    {
        var first = Delay(.1f) with { Id = new(3) };
        var second = WorkshopInput.Generic(PartId.Delay, new(5), 2, 1, 0, 0, 0, 0, 1,
            new InstanceParameterCollection([new(new ParameterId("delay_seconds"), .2f)]));
        IWorkshopInstance[] parts = [
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1),
            first, PlacedParts.Lamp(new(4), 4, 1, 0, 0, 0, 0, 1), second];
        if (reverse) Array.Reverse(parts);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts),
            Connections: new(Signal(2, 3), Signal(3, 5), Signal(5, 4)));
        var (built, reads, reset) = await NoFixedCapacityTests.RunAndReset(construction, 180);
        var started = reads.First(read => read.Timers[0].Phase == ActivationTimerPhase.Counting).Timers[0].StartedTick;
        var lit = reads.First(read => Lamp(read).Phase == ActivationPhase.Latched);
        Assert.Equal(started + 36, lit.Tick.Value);
        Assert.All(reads.Where(read => read.Tick.Value < started + 36), read => Assert.Equal(ActivationPhase.Clear, Lamp(read).Phase));
        Assert.Equal(ActivationOccurrenceKind.TimerElapsed, Lamp(lit).Kind);
        Assert.Equal((uint)((started + 36) * 4), Lamp(lit).EventOrdinal);
        NoFixedCapacityTests.AssertSameState(built, reset, "chained timer Reset");
        var disconnected = construction with { Connections = new(Signal(3, 5), Signal(5, 4)) };
        var (_, control, _) = await NoFixedCapacityTests.RunAndReset(disconnected, 180);
        Assert.All(control, read => Assert.Equal(ActivationPhase.Clear, Lamp(read).Phase));
    }

    [Fact]
    public async Task RealContactNonHalfDurationSurvivesSaveLoadAndResetExactly()
    {
        const float seconds = .1234567f;
        Assert.NotEqual(seconds, (float)(Half)seconds);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 3, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1),
                Delay(seconds), PlacedParts.Lamp(new(4), 4, 1, 0, 0, 0, 0, 1)),
            Connections: new(Signal(2, 3), Signal(3, 4)));
        var saved = new WorkshopSavedConstruction(construction, new(5));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, loaded);
        var restored = Assert.IsType<WorkshopGenericInstance>(loaded.Construction.Instances.Single(part => part.Id.Value == 3));
        Assert.True(F32Bits.Equal(seconds, restored.Parameters[new("delay_seconds")]));
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(construction, 180);
        var counting = run.First(read => read.Timers[0].Phase == ActivationTimerPhase.Counting).Timers[0];
        Assert.Equal(15UL, counting.DueTick - counting.StartedTick);
        Assert.Equal(counting.DueTick, run.First(read => Lamp(read).Phase == ActivationPhase.Latched).Tick.Value);
        NoFixedCapacityTests.AssertSameState(built, reset, "timer Reset");
        var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(loaded.Construction, 180);
        NoFixedCapacityTests.AssertSameState(run[^1], replay[^1], "saved timer replay");
    }

    [Fact]
    public void UndeclaredOldDurationKeyRejectsAtomically()
    {
        var current = Delay(.1234567f);
        var invalid = current with { Parameters = new([new(new ParameterId("duration_seconds"), 1f)]) };
        Assert.Throws<ArgumentException>(invalid.Validate);
        Assert.True(F32Bits.Equal(.1234567f, current.Parameters[new("delay_seconds")]));
        var saved = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), new(current)), new(4));
        var malformed = saved.Construction with { Instances = new(invalid) };
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var record = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, malformed, validate: false);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () =>
            WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Finish(builder, record, 4)));
        Assert.Equal(current, saved.Construction.Instances[0]);
    }

}
