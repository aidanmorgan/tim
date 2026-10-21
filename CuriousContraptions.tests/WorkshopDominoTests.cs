using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The Domino is declaration data: one dynamic box, its mass, material and ActivationOut; the orientation sensor compiles only when wired.</summary>
public sealed class WorkshopDominoTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopConstruction Construction() => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), .4, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1)));

    [Fact]
    public void DominoCompilesAsOneDynamicBoxWithDeclaredMassMaterialAndGravity()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(2));
        Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
        Assert.Equal(new Kilograms((Half).4), body.Mass);
        Assert.Equal(new AccelerationVector((Half)0, (Half)(-9.81), (Half)0), body.Gravity);
        Assert.Equal(new InverseSeconds((Half)0), body.LinearDrag);
        Assert.True(body.Velocity.IsPositiveZero);
        Assert.True(body.AngularVelocity.IsPositiveZero);
        var box = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
        Assert.Equal(ColliderShapeKind.Box, box.Shape);
        Assert.Equal(RigidLocalPose.Identity, box.Pose);
        Assert.Equal(new MetreVector((Half).125, (Half).55, (Half).325), box.HalfExtents);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == box.Material);
        Assert.Equal(new Restitution((Half).05), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).6), material.Friction);
        Assert.Equal(new RollingResistance((Half)0), material.RollingResistance); // a box declares no rolling resistance
        Assert.Equal(2, scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic));
        Assert.Empty(scene.Sensors.ToArray()); Assert.Empty(scene.Triggers.ToArray()); Assert.Empty(scene.ContactWorks.ToArray());
    }

    [Fact]
    public void CompiledInertiaIsTheHomogeneousBoxTensorAboutTheBoxCentre()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(2));
        var box = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
        var properties = RigidMassProperties.Compile(body, box);
        Assert.Equal(default, properties.LocalCentreOfMass);
        Assert.Equal(CanonicalRotation.Identity, properties.PrincipalFrame);
        const double mass = .4, hx = .125, hy = .55, hz = .325;
        Near(properties.X, mass * (hy * hy + hz * hz) / 3);
        Near(properties.Y, mass * (hx * hx + hz * hz) / 3);
        Near(properties.Z, mass * (hx * hx + hy * hy) / 3);
        // The admission record carries exactly these moments (Half mantissa + int32 exponent) for the worker to read.
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var slot = Array.FindIndex(scene.Bodies.ToArray(), b => b.Id == body.Id);
        var record = bytes.AsSpan(scene.World.BodyOffset(slot), PhysicsGpuAbi.BodyBytes);
        Assert.Equal(BitConverter.HalfToUInt16Bits(properties.X.Mantissa), BinaryPrimitives.ReadUInt16LittleEndian(record[120..]));
        Assert.Equal(properties.X.Exponent, BinaryPrimitives.ReadInt32LittleEndian(record[124..]));
        Assert.Equal(BitConverter.HalfToUInt16Bits(properties.Y.Mantissa), BinaryPrimitives.ReadUInt16LittleEndian(record[128..]));
        Assert.Equal(properties.Y.Exponent, BinaryPrimitives.ReadInt32LittleEndian(record[132..]));
        Assert.Equal(BitConverter.HalfToUInt16Bits(properties.Z.Mantissa), BinaryPrimitives.ReadUInt16LittleEndian(record[136..]));
        Assert.Equal(properties.Z.Exponent, BinaryPrimitives.ReadInt32LittleEndian(record[140..]));
        static void Near(PrincipalInertia value, double expected) =>
            Assert.InRange(Math.Abs(Math.ScaleB((double)value.Mantissa, value.Exponent) - expected) / expected, 0, .003);
    }

    [Fact]
    public void SaveRoundTripsTheDominoAndRejectsForgedMaterialOrPadding()
    {
        var save = new WorkshopSavedConstruction(Construction(), new(3));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        var domino = Assert.IsType<WorkshopDomino>(decoded.Construction.Instances[1]);
        Assert.Equal(DominoMaterial.Default, domino.Material);
        Assert.Equal(PartId.Domino, domino.PartId);
        Assert.False(domino.Cosmetic.IsDeclared);
        foreach (var forgedMaterial in new[]
        {
            domino.Material with { Mass = new((Half)17) },
            domino.Material with { Bounce = new((Half)17) },
            domino.Material with { Friction = new((Half)17) },
            domino.Material with { HalfExtents = new((Half)17, domino.Material.HalfExtents.Y, domino.Material.HalfExtents.Z) },
            domino.Material with { HalfExtents = new(domino.Material.HalfExtents.X, (Half)17, domino.Material.HalfExtents.Z) },
            domino.Material with { HalfExtents = new(domino.Material.HalfExtents.X, domino.Material.HalfExtents.Y, (Half)17) }
        })
        {
            var forgedConstruction = save.Construction with { Instances = new WorkshopInstances(save.Construction.Instances[0], domino with { Material = forgedMaterial }) };
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, forgedConstruction, validate: false);
            var savedBytes = SaveFixture.Finish(builder, crOffset, 3);
            var forgedBytes = savedBytes;
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(forgedBytes));
        }
    }

    [Fact]
    public void OnlyTheDefaultMaterialIsAdmittedAndTheDominoDeclaresActivationOutOnly()
    {
        var domino = WorkshopInput.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1);
        domino.Validate();
        foreach (var invalid in new[]
        {
            domino with { Material = DominoMaterial.Default with { Mass = new((Half).5) } },
            domino with { Material = DominoMaterial.Default with { Bounce = new((Half)0) } },
            domino with { Material = DominoMaterial.Default with { Friction = new((Half).3) } },
            domino with { Material = DominoMaterial.Default with { HalfExtents = new((Half).125, (Half).55, (Half).3) } }
        })
            Assert.Throws<ArgumentException>(() => invalid.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopInput.Domino(new(2), 0, 65, 0, 0, 0, 0, 1));
        Assert.Throws<ArgumentException>(() => WorkshopInput.Domino(new(2), 0, 0, 0, 0, 0, 0, 0));
        Assert.Equal([new WorkshopPort(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output, WorkshopSignalKind.Pulse)], WorkshopPorts.For(PartId.Domino).ToArray());
        Assert.True(WorkshopPorts.Has(PartId.Domino, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.False(WorkshopPorts.Has(PartId.Domino, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.Equal(new System.Numerics.Vector3(0f, .55f, 0f), WorkshopPorts.LocalPosition(PartId.Domino, WorkshopSocket.ActivationOut));
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.Domino(new(1), 0, Upright, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 1, 1 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));
        Assert.Equal(PartAllowance.Unlimited, WorkbenchCapacityTests.FreePalette[PartId.Domino]);
    }

    [Fact]
    public void SeventeenDynamicBodiesBuildAndCompileWithoutAMovingBodyLimit()
    {
        var items = new List<IWorkshopInstance>();
        for (var i = 0; i < 16; i++) items.Add(WorkshopInput.Domino(new((ulong)(i + 1)), -6 + i * .8, Upright, 0, 0, 0, 0, 1));
        var sixteen = new WorkshopInstances([.. items]);
        sixteen.Validate();
        // Story 9R.5b: the retired 16-row moving-body table no longer refuses the seventeenth moving body.
        items.Add(WorkshopInput.Basketball(new(17), 7, 3, 0, 0, 0, 0, 1));
        var seventeen = new WorkshopInstances([.. items]);
        seventeen.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), seventeen), new(1, 2));
        Assert.Equal(17, scene.World.Dynamic);
    }
}
