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
        Assert.True(HalfBits.IsPositiveZero(body.Velocity));
        Assert.Equal(default, body.AngularVelocity);
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
        var record = bytes.AsSpan(PhysicsGpuAbi.BodiesOffset + slot * PhysicsGpuAbi.BodyBytes, PhysicsGpuAbi.BodyBytes);
        Assert.Equal(BitConverter.HalfToUInt16Bits(properties.X.Mantissa), BinaryPrimitives.ReadUInt16LittleEndian(record[96..]));
        Assert.Equal(properties.X.Exponent, BinaryPrimitives.ReadInt32LittleEndian(record[100..]));
        Assert.Equal(BitConverter.HalfToUInt16Bits(properties.Y.Mantissa), BinaryPrimitives.ReadUInt16LittleEndian(record[104..]));
        Assert.Equal(properties.Y.Exponent, BinaryPrimitives.ReadInt32LittleEndian(record[108..]));
        Assert.Equal(BitConverter.HalfToUInt16Bits(properties.Z.Mantissa), BinaryPrimitives.ReadUInt16LittleEndian(record[112..]));
        Assert.Equal(properties.Z.Exponent, BinaryPrimitives.ReadInt32LittleEndian(record[116..]));
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
        Assert.Equal(WorkshopPartKind.Domino, domino.Kind);
        Assert.False(domino.Cosmetic.IsDeclared);
        var dominoOffset = 24 + WorkshopWire.ConstructionHeaderBytes + WorkshopWire.InstanceBytes;
        foreach (var field in new[] { 104, 106, 108, 110, 112, 114 })
        {
            var forged = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt16LittleEndian(forged.AsSpan(dominoOffset + field), BitConverter.HalfToUInt16Bits((Half)17));
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(forged));
        }
        var padding = (byte[])bytes.Clone(); padding[dominoOffset + 116] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        Assert.Equal((uint)WorkshopSaveVersion.CanonicalConstruction, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
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
        Assert.Equal([new WorkshopPort(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)], WorkshopPorts.For(WorkshopPartKind.Domino).ToArray());
        Assert.True(WorkshopPorts.Has(WorkshopPartKind.Domino, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.False(WorkshopPorts.Has(WorkshopPartKind.Domino, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.Equal(new MetreVector((Half)0, (Half).55, (Half)0), WorkshopPorts.LocalPosition(WorkshopPartKind.Domino, WorkshopSocket.ActivationOut));
        Assert.Equal(new WorkbenchFootprint(1, 1, 1, 0, 0, 0), WorkbenchFootprint.Of(WorkshopPartKind.Domino));
        Assert.Equal(PartAllowance.Unlimited, WorkshopInventoryPolicy.Free[WorkshopPartKind.Domino]);
    }

    [Fact]
    public void SeventeenDynamicBodiesRejectWithTheMovingBodyTableReason()
    {
        var items = new List<IWorkshopInstance>();
        for (var i = 0; i < 16; i++) items.Add(WorkshopInput.Domino(new((ulong)(i + 1)), -6 + i * .8, Upright, 0, 0, 0, 0, 1));
        var sixteen = new WorkshopInstances([.. items]);
        sixteen.Validate();
        items.Add(WorkshopInput.Basketball(new(17), 7, 3, 0, 0, 0, 0, 1));
        var error = Assert.Throws<WorkbenchFullException>(() => new WorkshopInstances([.. items]).Validate());
        Assert.Equal(WorkbenchTable.DynamicBodies, error.Table);
        Assert.Equal("Workbench is full: moving body table", error.Message);
    }
}
