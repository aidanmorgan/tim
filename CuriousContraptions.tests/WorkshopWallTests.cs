using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWallTests
{
    private static WorkshopWall Wall(WallDimensions dimensions) =>
        WorkshopInput.Wall(new(2), .125, 2, -.25, 0, 0, .382683432365, .923879532511, dimensions);
    private static WorkshopSavedConstruction Saved(WallDimensions dimensions) =>
        new(new(new(1), WorkshopCadenceSettings.Default(), new(Wall(dimensions))), new(3));

    [Fact]
    public void ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits()
    {
        foreach (var dimensions in new[] { WallDimensions.Default, WallDimensions.Minimum, WallDimensions.Maximum })
        {
            var saved = Saved(dimensions);
            var bytes = WorkshopSaveCodec.Encode(saved);
            Assert.Equal(saved, WorkshopSaveCodec.Decode(bytes));
            Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
            var scene = WorkshopPhysicsCompiler.Compile(saved.Construction, new(1, 2));
            var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(2));
            Assert.Equal(Wall(dimensions).Rotation, body.Rotation);
            Assert.Equal(RigidMotionKind.Static, body.Motion);
            var box = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
            Assert.Equal(ColliderShapeKind.Box, box.Shape);
            Assert.Equal(RigidLocalPose.Identity, box.Pose);
            Assert.Equal(new MetreVector((Half)(dimensions.Width.Value * (Half).5),
                (Half)(dimensions.Height.Value * (Half).5), (Half)(dimensions.Thickness.Value * (Half).5)), box.HalfExtents);
            var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == box.Material);
            Assert.Equal(new Restitution((Half)1), material.Restitution);
            Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
            Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        }
    }

    [Fact]
    public void GenericShapeEnvelopeAcceptsSixteenAndRejectsTheNextHalfValue()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Saved(WallDimensions.Maximum).Construction, new(1, 2));
        var box = Assert.Single(scene.Colliders.ToArray(), c => c.Shape == ColliderShapeKind.Box);
        box.Validate();
        (box with { HalfExtents = new((Half)16, (Half)16, (Half)16) }).Validate();
        var above = BitConverter.UInt16BitsToHalf((ushort)(BitConverter.HalfToUInt16Bits((Half)16) + 1));
        foreach (var extents in new[] { new MetreVector(above, (Half)1, (Half)1),
            new MetreVector((Half)1, above, (Half)1), new MetreVector((Half)1, (Half)1, above) })
            Assert.Throws<ArgumentException>(() => (box with { HalfExtents = extents }).Validate());
        var sphere = box with { Shape = ColliderShapeKind.Sphere, HalfExtents = default, Radius = new((Half)16) };
        sphere.Validate();
        Assert.Throws<ArgumentException>(() => (sphere with { Radius = new(above) }).Validate());
    }

    [Fact]
    public void FinitePointerInputClampsBeforeHalfConversionButNonfiniteRejects()
    {
        Assert.Equal(WallDimensions.Maximum, WallDimensions.FromInput(float.MaxValue, float.MaxValue, float.MaxValue));
        Assert.Equal(WallDimensions.Minimum, WallDimensions.FromInput(float.MinValue, -1, 0));
        Assert.Equal(WallDimensions.Default, WallDimensions.FromInput(3, 2, .25f));
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.Throws<ArgumentException>(() => WallDimensions.FromInput(invalid, 2, .25f));
            Assert.Throws<ArgumentException>(() => WallDimensions.FromInput(3, invalid, .25f));
            Assert.Throws<ArgumentException>(() => WallDimensions.FromInput(3, 2, invalid));
        }
    }

    [Fact]
    public void SerializedInvalidDimensionsAndPaddingRejectWithoutClamping()
    {
        var valid = WorkshopSaveCodec.Encode(Saved(WallDimensions.Default));
        foreach (var offset in new[] { 104, 106, 108 })
            foreach (var value in new[] { (Half)0, (Half)(-1), (Half)9, Half.NaN, Half.PositiveInfinity })
            {
                var changed = (byte[])valid.Clone();
                BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(24 + WorkshopWire.ConstructionHeaderBytes + offset),
                    BitConverter.HalfToUInt16Bits(value));
                Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(changed));
            }
        var padding = (byte[])valid.Clone(); padding[24 + WorkshopWire.ConstructionHeaderBytes + 110] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        Assert.Equal(valid, WorkshopSaveCodec.Encode(Saved(WallDimensions.Default)));
    }

    [Fact]
    public void PreviousSaveAndWireSchemasRejectRatherThanReinterpretNewKind()
    {
        var save = WorkshopSaveCodec.Encode(Saved(WallDimensions.Default));
        BinaryPrimitives.WriteUInt32LittleEndian(save.AsSpan(4), 3);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(save));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            Saved(WallDimensions.Default).Construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWire.Encode(command);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 10);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(bytes));
    }

    [Fact]
    public void WallPopulationPuzzleAndForeignPortsReject()
    {
        var wall = Wall(WallDimensions.Default);
        Assert.Empty(WorkshopPorts.For(WorkshopPartKind.Wall).ToArray());
        Assert.False(WorkshopPorts.Has(WorkshopPartKind.Wall, WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.Throws<ArgumentException>(() => new WorkshopInstances(wall, wall with { Id = new(3) }).Validate());
        var puzzle = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(4), new(5), new((Half)1));
        Assert.Throws<ArgumentException>(() => puzzle.WithInstance(wall).Validate());
        var construction = Saved(WallDimensions.Default).Construction.WithInstance(
            WorkshopInput.Lamp(new(3), 3, 2, 0, 0, 0, 0, 1)) with
        {
            Connections = new(new WorkshopConnection(wall.Id, WorkshopSocket.ActivationOut,
                new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation))
        };
        Assert.Throws<ArgumentException>(() => construction.Validate());
    }

    [Fact]
    public void AllAdmittedInstancesFitExistingPhysicalCapacitiesWithoutNewLaw()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1),
            WorkshopInput.Receiver(new(2), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Ramp(new(3), -3, 3, 0, 0, 0, 0, 1, RampDimensions.Default),
            WorkshopInput.Ramp(new(4), -1, 2, 0, 0, 0, 0, 1, RampDimensions.Default),
            WorkshopInput.Switch(new(5), 0, 1, 2, 0, 0, 0, 1, ContactTriggerSettings.Default),
            WorkshopInput.Lamp(new(6), 3, 1, 2, 0, 0, 0, 1),
            WorkshopInput.Wall(new(7), -4, 4, -2, 0, 0, 0, 1, WallDimensions.Maximum));
        var scene = WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(), instances), new(1, 2));
        Assert.Equal(8, scene.Bodies.Length);
        Assert.Equal(13, scene.Colliders.Length);
        Assert.Equal(8, scene.Materials.Length);
        Assert.Equal(33, PhysicsSceneDeclaration.BodyCapacity);
        Assert.Equal(64, PhysicsSceneDeclaration.ColliderCapacity);
        Assert.Equal(33, PhysicsSceneDeclaration.MaterialCapacity);
    }
}

[Collection<NativeSceneCollection>]
public sealed class WorkshopWallResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void ActualResourceThreeAxesArtworkAndProjectionFollowCanonicalFullPose()
    {
        var registry = new PartRegistry(); registry.Discover();
        var definition = registry.Definitions[WorkshopPartKind.Wall];
        Assert.Empty(definition.Parameters);
        Assert.Equal(WallDimensions.Default, definition.Wall!.Capture());
        var part = Assert.IsType<WallPart>(registry.Create(WorkshopPartKind.Wall));
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.Equal(ResizeAxes.All, part.ResizableAxes);
            var ring = part.GetChildren().OfType<MeshInstance3D>().Select(mesh => mesh.Mesh).OfType<TorusMesh>().Single();
            part.Position = new(1, 2, -1);
            part.Quaternion = Quaternion.FromEuler(new(.2f, .35f, -.4f));
            var position = part.Position; var rotation = part.Quaternion;
            foreach (var dimensions in new[] { WallDimensions.Default, WallDimensions.Minimum, WallDimensions.Maximum })
            {
                part.ApplyDimensions(dimensions);
                Assert.Equal(position, part.Position); Assert.Equal(rotation, part.Quaternion);
                Assert.Equal(part.Dimensions, part.Visual.Scale);
                Assert.Equal(part.Dimensions.Length() * .5f + .025f, ring.OuterRadius);
                // Independent source-art oracle: the inset slab and cream bands do not fill collider corners.
                var transform = part.GlobalTransform * new Transform3D(Basis.FromScale(part.Dimensions), Vector3.Zero);
                var slab = new Vector3(1, 1, .998f); var band = new Vector3(.035f, .94f, 1);
                var expected = transform * new Aabb(-slab * .5f, slab);
                foreach (var side in new[] { -1, 1 })
                    expected = expected.Merge(transform * new Aabb(new Vector3(side * .46f, 0, 0) - band * .5f, band));
                var collider = part.GlobalTransform * new Aabb(-part.Dimensions * .5f, part.Dimensions);
                var actual = PlacementShadows.ArtworkBounds(part);
                Assert.True(expected.Position.DistanceTo(actual.Position) < .00001f);
                Assert.True(expected.End.DistanceTo(actual.End) < .00001f);
                for (var axis = 0; axis < 3; axis++)
                {
                    Assert.True(actual.Position[axis] >= collider.Position[axis] - .00001f);
                    Assert.True(actual.End[axis] <= collider.End[axis] + .00001f);
                }
            }
            part.SetDimensions(new(float.MaxValue, float.MaxValue, float.MaxValue));
            Assert.Equal(WallDimensions.Maximum, part.CanonicalDimensions);
            Assert.Throws<ArgumentException>(() => part.SetDimensions(new(float.NaN, 2, .25f)));
            Assert.Equal(WallDimensions.Maximum, part.CanonicalDimensions);
            Assert.Equal(position, part.Position); Assert.Equal(rotation, part.Quaternion);
            part.ApplyDimensions(WallDimensions.Default);
            Assert.Equal(part.Dimensions.Length() * .5f + .025f, ring.OuterRadius);
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }
    [Fact]
    public void ExistingRampSelectionRingTracksResizeAndCanonicalRestore()
    {
        var registry = new PartRegistry(); registry.Discover();
        var ramp = Assert.IsType<RampPart>(registry.Create(WorkshopPartKind.Ramp));
        try
        {
            godot.Tree.Root.AddChild(ramp);
            var ring = ramp.GetChildren().OfType<MeshInstance3D>().Select(mesh => mesh.Mesh).OfType<TorusMesh>().Single();
            var original = ring.OuterRadius;
            ramp.ApplyDimensions(new(new((Half)2), new((Half)1)));
            Assert.Equal(1.025f, ring.OuterRadius);
            ramp.ApplyDimensions(RampDimensions.Default);
            Assert.Equal(original, ring.OuterRadius);
        }
        finally { ramp.GetParent()?.RemoveChild(ramp); ramp.Free(); }
    }

}
