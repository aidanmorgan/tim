using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWallTests
{
    private static WorkshopGenericInstance Wall(InstanceParameterCollection dimensions) =>
        WorkshopInput.Generic(PartId.Wall, new(2), .125, 2, -.25, 0, 0, .382683432365, .923879532511, dimensions);
    private static WorkshopSavedConstruction Saved(InstanceParameterCollection dimensions) =>
        new(new(new(1), WorkshopCadenceSettings.Default(), new(Wall(dimensions))), new(3));

    [Fact]
    public void ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits()
    {
        foreach (var dimensions in new[] { WallFixture.Default, WallFixture.Minimum, WallFixture.Maximum })
        {
            var saved = Saved(dimensions);
            var bytes = WorkshopSaveCodec.Encode(saved);
            Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
            Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes)));
            var scene = WorkshopPhysicsCompiler.Compile(saved.Construction, new(1, 2));
            var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(2));
            Assert.Equal(Wall(dimensions).Rotation, body.Rotation);
            Assert.Equal(RigidMotionKind.Static, body.Motion);
            var box = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
            Assert.Equal(ColliderShapeKind.Box, box.Shape);
            Assert.Equal(RigidLocalPose.Identity, box.Pose);
            Assert.Equal(new MetreVector((Half)(dimensions[new("width")] * .5f),
                (Half)(dimensions[new("height")] * .5f), (Half)(dimensions[new("thickness")] * .5f)), box.HalfExtents);
            var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == box.Material);
            Assert.Equal(new Restitution((Half)1), material.Restitution);
            Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
            Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        }
    }

    [Fact]
    public void GenericShapeEnvelopeAcceptsSixteenAndRejectsTheNextHalfValue()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Saved(WallFixture.Maximum).Construction, new(1, 2));
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
    public void InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip()
    {
        var saved = Saved(WallFixture.Values(3.1234567f, 2.2345678f, .3456789f));
        var bytes = WorkshopSaveCodec.Encode(saved);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var wall = Assert.IsType<WorkshopGenericInstance>(saved.Construction.Instances[0]);
        var geometry = CuriousContraptions.Bundle.BundleGeometry.Colliders(DeclaredPhysics.Of(PartId.Wall), wall.Parameters.ToBundleValues());
        Assert.Equal(3.1234567f * .5f, Assert.Single(geometry).HalfExtents.X);
        foreach (var key in new[] { "width", "height", "thickness", "unknown" })
        foreach (var value in new[] { -1f, 0f, 9f, float.NaN, float.PositiveInfinity })
        {
            var values = wall.Parameters.ToDictionary(pair => pair.Key, pair => pair.Value);
            values[new(key)] = value;
            var invalid = wall with { Parameters = new(values) };
            Assert.Throws<ArgumentException>(() => invalid.Validate());
            Assert.Throws<ArgumentException>(() => saved.Construction.WithInstance(invalid).Validate());
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var record = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder,
                saved.Construction with { Instances = new(invalid) }, validate: false);
            Assert.Throws<WorkshopSaveRejectedException>(() =>
                WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Finish(builder, record, saved.NextBodyId.Value)));
            Assert.Equal(bytes, WorkshopSaveCodec.Encode(saved));
        }
        Assert.Throws<ArgumentException>(() => new InstanceParameterCollection([new(new("width"), 1f), new(new("width"), 2f)]));
        Assert.Throws<ArgumentException>(() => (wall with { Parameters = InstanceParameterCollection.Empty }).Validate());
    }


    [Theory]
    [InlineData(0f, 4f)]
    [InlineData(.7853982f, 4f)]
    [InlineData(1.5707964f, 4f)]
    [InlineData(.7853982f, 64f)]
    public void RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot(float yaw, float speed)
    {
        var normal = new System.Numerics.Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw));
        var wall = WorkshopInput.Generic(PartId.Wall, new(2), 0, 3, 0, 0, MathF.Sin(yaw / 2), 0, MathF.Cos(yaw / 2),
            WallFixture.Values(3, 6, .25f));
        var ball = WorkshopInput.Generic(PartId.Basketball,new(1), -2 * normal.X, 3, -2 * normal.Z, 0, 0, 0, 1);
        float Probe(bool includeWall)
        {
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
                includeWall ? new(ball, wall) : new(ball));
            var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            var bytes = PhysicsGpuAbi.Admission(scene, new(1),
                new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
            var offset = scene.Layout.GetBodyOffset(ball.Id);
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.BodyVelocityOffset), normal.X * speed);
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.BodyVelocityOffset + 8), normal.Z * speed);
            var furthest = -2f;
            WorkshopPhysicsEngineTests.StepWorld(bytes, 90, (_, state) =>
            {
                var bodies = PhysicsGpuAbi.ReadDynamicBodies(state);
                Assert.Equal(1, bodies.Count);
                var body = bodies[0].Body;
                var position = new System.Numerics.Vector3((body.Cell.X + body.Local.X) / 16f,
                    (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
                furthest = MathF.Max(furthest, System.Numerics.Vector3.Dot(position, normal));
            });
            return furthest;
        }
        Assert.True(Probe(true) < -.2f, "The ball never crosses the wall face.");
        Assert.True(Probe(false) > .3f, "Without the wall the same ball crosses its plane.");
    }

    [Fact]
    public async Task RealSimulationResetAndSaveLoadRestoreGenericWallAndItsContact()
    {
        var wall = WorkshopInput.Generic(PartId.Wall, new(2), 0, 2, 0, 0, 0, 0, 1,
            WallFixture.Values(3.1234567f, .4f, 2f));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 4, 0, 0, 0, 0, 1), wall));
        var saved = new WorkshopSavedConstruction(construction, new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var (built, reads, reset) = await NoFixedCapacityTests.RunAndReset(construction, 120);
        NoFixedCapacityTests.AssertSameState(built, reset, "Wall Reset");
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(saved));
        Assert.True(reads.All(read => (read.Bodies[0].Body.Cell.Y + read.Bodies[0].Body.Local.Y) / 16f > 2.2f));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, loaded);
        var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(loaded.Construction, 120);
        for (var tick = 0; tick < reads.Count; tick++)
            NoFixedCapacityTests.AssertSameState(reads[tick], replay[tick], "Wall replay");
    }

    [Fact]
    public void WallPopulationPuzzleAndForeignPortsReject()
    {
        var wall = Wall(WallFixture.Default);
        Assert.Empty(DeclaredPorts.Current.Of(PartId.Wall).ToArray());
        Assert.False(DeclaredPorts.Current.Has(PartId.Wall, WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        // Free play has no per-kind wall count; only the compiled table capacities limit the population.
        new WorkshopInstances(wall, wall with { Id = new(3) }).Validate();
        var puzzle = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(4), new((Half)1));
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => puzzle.WithInstance(wall).Validate());
        var construction = Saved(WallFixture.Default).Construction.WithInstance(
            WorkshopInput.Lamp(new(3), 3, 2, 0, 0, 0, 0, 1)) with
        {
            Connections = new(new WorkshopConnection(wall.Id, WorkshopSocket.ActivationOut,
                new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation))
        };
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => construction.Validate());
    }

    [Fact]
    public void AllAdmittedInstancesCompileIntoExactlySizedTablesWithoutNewLaw()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 6, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(2), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Ramp, new(3), -3, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Ramp, new(4), -1, 2, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(5), 0, 1, 2, 0, 0, 0, 1),
            WorkshopInput.Lamp(new(6), 3, 1, 2, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Wall, new(7), -4, 4, -2, 0, 0, 0, 1, WallFixture.Maximum));
        var scene = WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(), instances), new(1, 2));
        Assert.Equal(8, scene.Bodies.Length);
        Assert.Equal(13, scene.Colliders.Length);
        Assert.Equal(8, scene.Materials.Length);
        // Story 9R.5b: no fixed capacity; the world tables are exactly the compiled populations.
        Assert.Equal((8, 13, 8), (scene.World.Bodies, scene.World.Colliders, scene.World.Materials));
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopWallResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void ActualResourceThreeAxesArtworkAndProjectionFollowCanonicalFullPose()
    {
        var registry = new PartRegistry(); registry.Load();
        var definition = registry.Definitions[PartId.Wall];
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
            foreach (var dimensions in new[] { WallFixture.Default, WallFixture.Minimum, WallFixture.Maximum })
            {
                part.SetDimensions(new((float)dimensions[new("width")], (float)dimensions[new("height")], (float)dimensions[new("thickness")]));
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
            Assert.Equal(new Vector3(8, 6, 2), part.Dimensions);
            Assert.Throws<ArgumentException>(() => part.SetDimensions(new(float.NaN, 2, .25f)));
            Assert.Equal(new Vector3(8, 6, 2), part.Dimensions);
            Assert.Equal(position, part.Position); Assert.Equal(rotation, part.Quaternion);
            part.SetDimensions(new(3, 2, .25f));
            Assert.Equal(part.Dimensions.Length() * .5f + .025f, ring.OuterRadius);
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }
    [Fact]
    public void ExistingRampSelectionRingTracksResizeAndCanonicalRestore()
    {
        var registry = new PartRegistry(); registry.Load();
        var ramp = Assert.IsAssignableFrom<MachinePart>(registry.Create(PartId.Ramp));
        try
        {
            godot.Tree.Root.AddChild(ramp);
            var ring = ramp.GetChildren().OfType<MeshInstance3D>().Select(mesh => mesh.Mesh).OfType<TorusMesh>().Single();
            var original = ring.OuterRadius;
            ramp.SetDimensions(new(2, .18f, 1));
            Assert.Equal(1.025f, ring.OuterRadius);
            ramp.SetDimensions(new(3, .18f, 1.3f));
            Assert.Equal(original, ring.OuterRadius);
        }
        finally { ramp.GetParent()?.RemoveChild(ramp); ramp.Free(); }
    }

}


internal static class WallFixture
{
    public static InstanceParameterCollection Values(float width, float height, float thickness) =>
        new([new(new("width"), width), new(new("height"), height), new(new("thickness"), thickness)]);
    public static InstanceParameterCollection Default => Values(3f, 2f, .25f);
    public static InstanceParameterCollection Minimum => Values(.4f, .4f, .12f);
    public static InstanceParameterCollection Maximum => Values(8f, 6f, 2f);
}
