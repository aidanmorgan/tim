using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The Domino is declaration data: one dynamic box, its mass, material and ActivationOut; the orientation sensor compiles only when wired.</summary>
public sealed class WorkshopDominoTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopConstruction Construction() => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Generic(PartId.Basketball,new(1), .4, 3, 0, 0, 0, 0, 1),
            PlacedParts.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1)));

    [Fact]
    public void DominoCompilesAsOneDynamicBoxWithDeclaredMassMaterialAndGravity()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(2));
        Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
        Assert.Equal(new Kilograms(.4f), body.Mass);
        Assert.Equal(new AccelerationVector(0f, (float)(-9.81), 0f), body.Gravity);
        Assert.Equal(new InverseSeconds(0f), body.LinearDrag);
        Assert.True(body.Velocity.IsPositiveZero);
        Assert.True(body.AngularVelocity.IsPositiveZero);
        var box = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
        Assert.Equal(ColliderShapeKind.Box, box.Shape);
        Assert.Equal(RigidLocalPose.Identity, box.Pose);
        Assert.Equal(new MetreVector(.125f, .55f, .325f), box.HalfExtents);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == box.Material);
        Assert.Equal(new Restitution(.05f), material.Restitution);
        Assert.Equal(new LinearSpeed(.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(.6f), material.Friction);
        Assert.Equal(new RollingResistance(0f), material.RollingResistance); // a box declares no rolling resistance
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
        // The exact moments of the declared f32 inputs, within the five f32 roundings of the closed form (6 · 2^-24 relative).
        double mass = .4f, hx = .125f, hy = .55f, hz = .325f;
        Near(properties.X, mass * (hy * hy + hz * hz) / 3);
        Near(properties.Y, mass * (hx * hx + hz * hz) / 3);
        Near(properties.Z, mass * (hx * hx + hy * hy) / 3);
        // The admission record carries exactly these f32 moments for the worker to read.
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var slot = Array.FindIndex(scene.Bodies.ToArray(), b => b.Id == body.Id);
        var offset = scene.World.BodyOffset(slot) + PhysicsGpuAbi.BodyPrincipalInertiaOffset;
        uint Moment(int axis) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + axis * PhysicsGpuAbi.PrincipalInertiaBytes));
        Assert.Equal(BitConverter.SingleToUInt32Bits(properties.X.Moment), Moment(0));
        Assert.Equal(BitConverter.SingleToUInt32Bits(properties.Y.Moment), Moment(1));
        Assert.Equal(BitConverter.SingleToUInt32Bits(properties.Z.Moment), Moment(2));
        static void Near(PrincipalInertia value, double expected) =>
            Assert.InRange(Math.Abs(value.Moment - expected) / expected, 0, 6.0 / (1 << 24));
    }

    // 9A.32: the tile is a zero-parameter generic instance of its package; its material is the package's and no record field or
    // parameter can carry another. A forged material is a typed field or a parameter, refused at the save and command boundaries.
    [Fact]
    public void SaveRoundTripsTheGenericDominoAndRefusesAnyTypedFieldOrParameter()
    {
        var save = new WorkshopSavedConstruction(Construction(), new(3));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        var domino = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.Domino, domino.PartId);
        Assert.Empty(domino.Parameters);
        Assert.DoesNotContain(DeclaredPhysics.Of(PartId.Domino).Presentation.Bindings,
            binding => binding.Driver == CuriousContraptions.Abstractions.PresentationDriver.Event);
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(1)!.Value;
        Assert.Equal((false, 0), (WorkshopWireFlatBuffers.CarriesTypedField(record), record.ParametersLength));
        foreach (var forged in Enum.GetValues<TypedField>().Select(field => PayloadInstanceTests.GenericSave("domino", field))
                     .Append(PayloadInstanceTests.GenericSave("domino", null, [("mass", .5f)])))
        {
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, forged));
            var forgedRecord = WorkshopSaveCodec.Open(forged).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(forgedRecord)).Reason);
        }
        // Control: the bare current-layout record loads as the same generic tile.
        Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements,
            PayloadInstanceTests.GenericSave("domino", null)).Construction.Instances));
    }

    [Fact]
    public void TheDominoHasNoAuthorableParameterAndDeclaresActivationOutOnly()
    {
        var domino = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1));
        domino.Validate();
        Assert.Empty(DeclaredPhysics.Of(PartId.Domino).Metadata.Parameters);
        foreach (var forged in new[] { "mass", "bounce", "friction", "half_extents", "threshold_degrees" })
            Assert.Throws<ArgumentException>(() => (domino with { Parameters = new([new(new(forged), 1f)]) }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => PlacedParts.Domino(new(2), 0, 65, 0, 0, 0, 0, 1));
        Assert.Throws<ArgumentException>(() => PlacedParts.Domino(new(2), 0, 0, 0, 0, 0, 0, 0));
        Assert.Equal([new WorkshopPort(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output, WorkshopSignalKind.Pulse,
            new(0f, .55f, 0f))], DeclaredPorts.Current.Of(PartId.Domino).ToArray());
        Assert.True(DeclaredPorts.Current.Has(PartId.Domino, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.False(DeclaredPorts.Current.Has(PartId.Domino, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.Equal(new System.Numerics.Vector3(0f, .55f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Domino, WorkshopSocket.ActivationOut));
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(PlacedParts.Domino(new(1), 0, Upright, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 1, 1 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));
        Assert.Equal(PartAllowance.Unlimited, WorkbenchCapacityTests.FreePalette[PartId.Domino]);
    }

    [Fact]
    public void SeventeenDynamicBodiesBuildAndCompileWithoutAMovingBodyLimit()
    {
        var items = new List<IWorkshopInstance>();
        for (var i = 0; i < 16; i++) items.Add(PlacedParts.Domino(new((ulong)(i + 1)), -6 + i * .8, Upright, 0, 0, 0, 0, 1));
        var sixteen = new WorkshopInstances([.. items]);
        sixteen.Validate();
        // Story 9R.5b: the retired 16-row moving-body table no longer refuses the seventeenth moving body.
        items.Add(WorkshopInput.Generic(PartId.Basketball,new(17), 7, 3, 0, 0, 0, 0, 1));
        var seventeen = new WorkshopInstances([.. items]);
        seventeen.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), seventeen), new(1, 2));
        Assert.Equal(17, scene.World.Dynamic);
    }
}
