using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopDelayTests
{
    private static WorkshopDelay Delay(Half seconds) => WorkshopInput.Delay(new(3), 1, 2, -.5,
        0, 0, 0, 1, new(new(seconds)));
    [Fact]
    public void ExactCanonicalDurationCeilingNeverUsesRoundedHalfProduct()
    {
        foreach (var (rate, expected) in new[] {
            (SimulationCadence.Hz60, new uint[] {6,60,537,720}),
            (SimulationCadence.Hz120, new uint[] {12,120,1073,1440}),
            (SimulationCadence.Hz240, new uint[] {24,240,2145,2880}) })
        {
            var durations = new Half[] {(Half).1,(Half)1,(Half)8.9375,(Half)12};
            for (var i = 0; i < durations.Length; i++)
                Assert.Equal(expected[i], new DelayDuration(new(durations[i])).Ticks(rate));
        }
        Assert.Equal((Half)1072, (Half)((Half)8.9375 * (Half)120));
    }
    [Fact]
    public void DurationPoseAndConnectionsRoundTripAndCompileOnlySourceBaseBox()
    {
        var trigger = WorkshopInput.Switch(new(1), -2, 2, 0, 0, 0, 0, 1, ContactTriggerSettings.Default);
        var lamp = WorkshopInput.Lamp(new(2), 3, 2, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(trigger, lamp, Delay((Half)8.9375)), Connections: new(
                new(new(1), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation),
                new(new(3), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var saved = new WorkshopSavedConstruction(construction, new(4));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
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
        var saved = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), new(Delay((Half)1))), new(4));
        var bytes = WorkshopSaveCodec.Encode(saved);
        foreach (var invalid in new[] {(Half)0,(Half)(-.1),(Half).09,(Half)13,Half.NaN,Half.PositiveInfinity})
        {
            var changed = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(24 + WorkshopWire.ConstructionHeaderBytes + 104),
                BitConverter.HalfToUInt16Bits(invalid));
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(changed));
        }
        foreach (var bits in new[] {
            (ushort)(BitConverter.HalfToUInt16Bits((Half).1) - 1),
            (ushort)(BitConverter.HalfToUInt16Bits((Half)12) + 1) })
        {
            var changed = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(24 + WorkshopWire.ConstructionHeaderBytes + 104), bits);
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(changed));
        }
        var padding = (byte[])bytes.Clone(); padding[24 + WorkshopWire.ConstructionHeaderBytes + 106] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        var old = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(old.AsSpan(4), 4);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(old));
        foreach (var invalid in new[] {double.NaN,double.PositiveInfinity,.09,12.01})
            Assert.Throws<ArgumentException>(() => DelayDuration.FromInput(invalid));
    }
    [Fact]
    public void FullPopulationAndDelayDirectionUseExistingCapacities()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Basketball(new(1),0,5,0,0,0,0,1),
            WorkshopInput.Receiver(new(2),4,1,0,0,0,0,1),
            WorkshopInput.Ramp(new(3),-4,2,0,0,0,0,1,RampDimensions.Default),
            WorkshopInput.Ramp(new(4),-2,2,0,0,0,0,1,RampDimensions.Default),
            WorkshopInput.Switch(new(5),0,1,0,0,0,0,1,ContactTriggerSettings.Default),
            WorkshopInput.Switch(new(6),2,1,0,0,0,0,1,ContactTriggerSettings.Default),
            WorkshopInput.Lamp(new(7),5,2,0,0,0,0,1), Delay((Half)1) with {Id=new(8)});
        var construction = new WorkshopConstruction(new(1),WorkshopCadenceSettings.Default(),instances);
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(1,2));
        Assert.Equal(9,scene.Bodies.Length);
        Assert.True(scene.Colliders.Length <= PhysicsSceneDeclaration.ColliderCapacity);
        Assert.Throws<ArgumentException>(() => new WorkshopInstances(instances[4],instances[5],
            WorkshopInput.Switch(new(9),3,1,0,0,0,0,1,ContactTriggerSettings.Default)).Validate());
        foreach (var edge in new[] {
            new WorkshopConnection(new(8),WorkshopSocket.ActivationIn,new(7),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation),
            new WorkshopConnection(new(5),WorkshopSocket.ActivationOut,new(8),WorkshopSocket.ActivationOut,WorkshopConnectionDomain.Activation),
            new WorkshopConnection(new(8),WorkshopSocket.ActivationOut,new(7),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Electrical) })
            Assert.Throws<ArgumentException>(() => (construction with {Connections=new(edge)}).Validate());
    }
    [Fact]
    public void PortsAndPuzzlePopulationRemainBounded()
    {
        Assert.True(WorkshopPorts.Has(WorkshopPartKind.Delay, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.True(WorkshopPorts.Has(WorkshopPartKind.Delay, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.False(WorkshopPorts.Has(WorkshopPartKind.Delay, WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output));
        Assert.Throws<ArgumentException>(() => new WorkshopInstances(Delay((Half)1), Delay((Half)2) with {Id=new(4)}).Validate());
        var puzzle = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)0));
        Assert.Throws<ArgumentException>(() => puzzle.WithInstance(Delay((Half)1)).Validate());
    }
}
