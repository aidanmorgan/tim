using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopBumperTests
{
    private static WorkshopConstruction Construction(Half strength) => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1),
            WorkshopInput.Bumper(new(2), 0, 4, 0, 0, 0, 0, 1, BumperWork.FromCanonicalStrength(strength))));

    [Fact]
    public void SourceSphereAndExplicitPreloadCompileAsGenericDeclarations()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction((Half)8), new(1, 2));
        var sphere = Assert.Single(scene.Colliders.ToArray(), x => x.Body == new GpuBodyId(2));
        Assert.Equal(ColliderShapeKind.Sphere, sphere.Shape);
        Assert.Equal(new Metres((Half).65), sphere.Radius);
        Assert.Equal(RigidLocalPose.Identity, sphere.Pose);
        var material = Assert.Single(scene.Materials.ToArray(), x => x.Id == sphere.Material);
        Assert.Equal(new Restitution((Half)1), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        var work = Assert.Single(scene.ContactWorks.ToArray());
        Assert.Equal(new GpuBodyId(2), work.Owner);
        Assert.Equal(new GpuBodyId(1), work.Target);
        Assert.Equal(new LinearSpeed((Half)8), work.TargetSpeed);
        Assert.Equal(new Joules((Half)32), work.InitialEnergy);
        Assert.Equal(new LinearSpeed((Half).05), work.Threshold);
        Assert.Equal(72u, work.CooldownPhysicalSteps);
    }

    [Fact]
    public void ConstructionRoundTripPreservesCalibrationAndRejectsPriorSchemasOrForgedEnergy()
    {
        foreach (var strength in new Half[] { (Half)0, (Half)8, (Half)20 })
        {
            var save = new WorkshopSavedConstruction(Construction(strength), new(3));
            var bytes = WorkshopSaveCodec.Encode(save);
            Assert.Equal(save, WorkshopSaveCodec.Decode(bytes));
            var bumper = Assert.IsType<WorkshopBumper>(save.Construction.Instances[1]);
            Assert.Equal(new Kilograms((Half)1), bumper.Work.ReferenceMass);
            Assert.Equal((Half)((Half).5 * (Half)(strength * strength)), bumper.Work.Preload.Value);
            var forged = (byte[])bytes.Clone();
            var bumperOffset = 24 + WorkshopWire.ConstructionHeaderBytes + WorkshopWire.InstanceBytes;
            BinaryPrimitives.WriteUInt16LittleEndian(forged.AsSpan(bumperOffset + 108),
                BitConverter.HalfToUInt16Bits((Half)17));
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(forged));
            var prior = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(prior.AsSpan(4), 6);
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(prior));
            var padding = (byte[])bytes.Clone(); padding[bumperOffset + 110] = 1;
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        }
    }

    [Fact]
    public void WorkRequiresUniquePhysicalStaticOwnerAndDynamicTarget()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction((Half)8), new(1, 2));
        var work = scene.ContactWorks[0];
        foreach (var invalid in new[] {
            work with { Owner = work.Target }, work with { Target = work.Owner },
            work with { Owner = new(99) }, work with { CooldownPhysicalSteps = 0 },
            work with { InitialEnergy = new((Half)201) } })
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity,
                scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers, [invalid]));
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity + 1,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers,
            [work, work with { Id = new(scene.NextIdentity) }]));
    }

    [Fact]
    public void StrengthBoundariesAndPopulationRejectAtomically()
    {
        foreach (var invalid in new[] { Half.NaN, Half.PositiveInfinity, (Half)(-.01),
            BitConverter.UInt16BitsToHalf((ushort)(BitConverter.HalfToUInt16Bits((Half)20) + 1)) })
            Assert.Throws<ArgumentException>(() => BumperWork.FromCanonicalStrength(invalid));
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, -.001, 20.001 })
            Assert.Throws<ArgumentException>(() => BumperWork.FromInput(invalid));
        var construction = Construction((Half)8);
        var bumper = Assert.IsType<WorkshopBumper>(construction.Instances[1]);
        Assert.Throws<ArgumentException>(() => construction.WithInstance(bumper with { Id = new(3) }).Validate());
        Assert.Throws<ArgumentException>(() => (bumper.Work with { ReferenceMass = new((Half)2) }).Validate());
        Assert.False(WorkshopPorts.Has(WorkshopPartKind.PinballBumper, WorkshopSocket.ActivationOut,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
    }
}
