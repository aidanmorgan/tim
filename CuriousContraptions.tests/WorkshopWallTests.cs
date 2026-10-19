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
        var saved = Saved(WallDimensions.Default);
        var valid = WorkshopSaveCodec.Encode(saved);
        foreach (var value in new[] { (Half)0, (Half)(-1), (Half)9, Half.NaN, Half.PositiveInfinity })
        {
            foreach (var invalidDimensions in new[]
            {
                new WallDimensions(new(value), WallDimensions.Default.Height, WallDimensions.Default.Thickness),
                new WallDimensions(WallDimensions.Default.Width, new(value), WallDimensions.Default.Thickness),
                new WallDimensions(WallDimensions.Default.Width, WallDimensions.Default.Height, new(value))
            })
            {
                var malformedConstruction = saved.Construction with {
                    Instances = new WorkshopInstances(saved.Construction.Instances[0] switch {
                        WorkshopWall w => w with { Dimensions = invalidDimensions },
                        var other => other
                    })
                };
                var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
                var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, malformedConstruction, validate: false);
                CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
                CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
                CuriousContraptions.Wire.SavedConstruction.AddVersion(builder, WorkshopSaveCodec.Version);
                CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, saved.NextBodyId.Value);
                CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset);
                var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
                builder.Finish(end.Value);
                EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
            }
        }
        Assert.Equal(valid, WorkshopSaveCodec.Encode(Saved(WallDimensions.Default)));
    }

    [Fact]
    public void PreviousSaveAndWireSchemasRejectRatherThanReinterpretNewKind()
    {
        var saved = Saved(WallDimensions.Default);
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, saved.Construction);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddVersion(builder, 3);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, saved.NextBodyId.Value);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset);
        var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
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
        Assert.Empty(WorkshopPorts.For(PartId.Wall).ToArray());
        Assert.False(WorkshopPorts.Has(PartId.Wall, WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        // Free play has no per-kind wall count; only the compiled table capacities limit the population.
        new WorkshopInstances(wall, wall with { Id = new(3) }).Validate();
        var puzzle = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(4), new(5), new((Half)1));
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => puzzle.WithInstance(wall).Validate());
        var construction = Saved(WallDimensions.Default).Construction.WithInstance(
            WorkshopInput.Lamp(new(3), 3, 2, 0, 0, 0, 0, 1)) with
        {
            Connections = new(new WorkshopConnection(wall.Id, WorkshopSocket.ActivationOut,
                new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation))
        };
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => construction.Validate());
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
        Assert.Equal(33, PhysicsSceneDeclaration.MaterialCapacity);
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopWallResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void ActualResourceThreeAxesArtworkAndProjectionFollowCanonicalFullPose()
    {
        var registry = new PartRegistry(); registry.Discover();
        var definition = registry.Definitions[PartId.Wall];
        Assert.Empty(definition.Parameters);
        Assert.Single(definition.Colliders);
        var part = Assert.IsAssignableFrom<MachinePart>(registry.Create(PartId.Wall));
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
                part.ApplyWallDimensions(dimensions);
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
            Assert.Equal(WallDimensions.Maximum, part.WallDimensions);
            Assert.Throws<ArgumentException>(() => part.SetDimensions(new(float.NaN, 2, .25f)));
            Assert.Equal(WallDimensions.Maximum, part.WallDimensions);
            Assert.Equal(position, part.Position); Assert.Equal(rotation, part.Quaternion);
            part.ApplyWallDimensions(WallDimensions.Default);
            Assert.Equal(part.Dimensions.Length() * .5f + .025f, ring.OuterRadius);
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }
    [Fact]
    public void ExistingRampSelectionRingTracksResizeAndCanonicalRestore()
    {
        var registry = new PartRegistry(); registry.Discover();
        var ramp = Assert.IsAssignableFrom<MachinePart>(registry.Create(PartId.Ramp));
        try
        {
            godot.Tree.Root.AddChild(ramp);
            var ring = ramp.GetChildren().OfType<MeshInstance3D>().Select(mesh => mesh.Mesh).OfType<TorusMesh>().Single();
            var original = ring.OuterRadius;
            ramp.ApplyRampDimensions(new(new((Half)2), new((Half)1)));
            Assert.Equal(1.025f, ring.OuterRadius);
            ramp.ApplyRampDimensions(RampDimensions.Default);
            Assert.Equal(original, ring.OuterRadius);
        }
        finally { ramp.GetParent()?.RemoveChild(ramp); ramp.Free(); }
    }

}
