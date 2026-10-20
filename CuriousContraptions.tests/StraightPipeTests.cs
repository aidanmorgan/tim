using System;
using System.Buffers.Binary;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class StraightPipeTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversPipeDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.True(registry.Definitions.ContainsKey(PartId.Pipe));
        var def = registry.Definitions[PartId.Pipe];
        Assert.Equal(PartId.Pipe, def.PartId);
        Assert.Equal("pipe", def.Id);
        Assert.Equal("Clear pipe", def.Title);
        Assert.Equal("Motion", def.Category);
        Assert.Equal("A hollow tube. Gravity carries balls through the open bore; tilt it downhill to guide them. The clear shell transmits light; cream collars are opaque.", def.Description);
        Assert.NotNull(def.Scene);
        Assert.Empty(def.Parameters);

        var part = registry.Create(PartId.Pipe);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.Pipe, part.PartId);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void PipeScene_ExtractsExact24CollidersAndTwoMouthPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://parts/scenes/pipe.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(instance);

            // Exactly 24 colliders: 16 circumference + 8 rim
            Assert.Equal(24, colliders.Count);

            // 16 circumference colliders
            var circumference = colliders.Take(16).ToArray();
            var expectedCircHalfExtents = new MetreVector((Half)1.8, (Half)0.025, (Half)0.14);
            foreach (var col in circumference)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
                Assert.Equal(expectedCircHalfExtents.X, col.HalfExtents.X);
                Assert.Equal(expectedCircHalfExtents.Y, col.HalfExtents.Y);
                Assert.Equal(expectedCircHalfExtents.Z, col.HalfExtents.Z);

                // Origin should be at X = 0, distance in YZ plane should be ~0.675
                Assert.Equal((Half)0, col.LocalPose.Translation.X);
                var py = (float)col.LocalPose.Translation.Y;
                var pz = (float)col.LocalPose.Translation.Z;
                var dist = MathF.Sqrt(py * py + pz * pz);
                Assert.InRange(dist, 0.674f, 0.676f);
            }

            // 8 rim colliders
            var rims = colliders.Skip(16).ToArray();
            Assert.Equal(8, rims.Length);
            foreach (var col in rims)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
                var px = (float)col.LocalPose.Translation.X;
                Assert.True(MathF.Abs(px - 1.8f) < 0.01f || MathF.Abs(px + 1.8f) < 0.01f);
                Assert.Equal(CanonicalRotation.Identity, col.LocalPose.Rotation);
            }

            // 2 port locators: mouth_start and mouth_end
            Assert.Equal(2, ports.Count);
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            var start = ports["mouth_start"];
            Assert.Equal("mouth_start", start.PortId);
            // Story 9R.6: mouths sit at the outer collar faces, ±(1.8 + 0.09) (CAT-048 fact 21), each with the common bore.
            Assert.True(MathF.Abs(start.Position.X - (-1.89f)) < 0.001f);
            Assert.Equal(0.65f, start.BoreRadius);
            Assert.True(MathF.Abs(start.Position.Y) < 0.001f);
            Assert.True(MathF.Abs(start.Position.Z) < 0.001f);

            var end = ports["mouth_end"];
            Assert.Equal("mouth_end", end.PortId);
            Assert.True(MathF.Abs(end.Position.X - 1.89f) < 0.001f);
            Assert.Equal(0.65f, end.BoreRadius);
            Assert.True(MathF.Abs(end.Position.Y) < 0.001f);
            Assert.True(MathF.Abs(end.Position.Z) < 0.001f);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void WorkshopPhysicsCompiler_CompilesPipeIntoStaticBodyWith24Colliders()
    {
        var pipe = WorkshopInput.Pipe(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(pipe));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        Assert.Equal(2, scene.Bodies.Length); // plane + pipe
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        Assert.Equal(new Kilograms((Half)0), body.Mass);
        Assert.Equal(new InverseSeconds((Half)0), body.LinearDrag);
        Assert.Equal(default, body.Gravity);

        var colliders = scene.Colliders.ToArray().Where(c => c.Body == body.Id).ToArray();
        Assert.Equal(24, colliders.Length);

        // All colliders use the body's material (restitution 1.0, bounce threshold 0.1, friction 0.3)
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == colliders[0].Material);
        Assert.Equal(new Restitution((Half)1), material.Restitution);
        Assert.Equal(new LinearSpeed((Half)0.1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half)0.3), material.Friction);
        Assert.Equal(new RollingResistance((Half)0), material.RollingResistance);
    }

    [Fact]
    public void SaveCodec_RoundTripsPipeAndPreservesDeclaredColliders()
    {
        var pipe = WorkshopInput.Pipe(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Basketball(new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, pipe));
        var save = new WorkshopSavedConstruction(construction, new(11));

        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(bytes);

        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));

        var decodedPipe = Assert.IsType<WorkshopPipe>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.Pipe, decodedPipe.PartId);
        Assert.Equal(PipeLength.Default, decodedPipe.Length);
        Assert.Equal(24, decodedPipe.DeclaredColliders.Count);

        // Verify FlatBuffers schema preserves PartId on saved instance record
        var bb = new Google.FlatBuffers.ByteBuffer(bytes);
        var fbSave = CuriousContraptions.Wire.SavedConstruction.GetRootAsSavedConstruction(bb);
        Assert.True(fbSave.Construction.HasValue);
        var fbPipe = fbSave.Construction.Value.Instances(1);
        Assert.True(fbPipe.HasValue);
        Assert.Equal("pipe", fbPipe.Value.PartId);
    }

    [Fact]
    public void WorkbenchCapacity_AdmitsCompoundPipesWithoutColliderLimit()
    {
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.Pipe(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 0, 24 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));

        // 3 pipes = 72 colliders: validates cleanly with unbounded colliders
        var threePipes = new WorkshopInstances(
            WorkshopInput.Pipe(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Pipe(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Pipe(new GpuBodyId(3), 10, 3, 0, 0, 0, 0, 1));
        WorkbenchCapacity.Validate(threePipes);

        // 20 pipes = 480 colliders: validates cleanly
        var twentyPipes = new WorkshopInstances(
            Enumerable.Range(1, 20).Select(i => (IWorkshopInstance)WorkshopInput.Pipe(new GpuBodyId((ulong)i), i * 2, 3, 0, 0, 0, 0, 1)).ToArray());
        WorkbenchCapacity.Validate(twentyPipes);
    }

    [Fact]
    public void BoreGeometry_AdmitsBallRadiusUnder65cm_AndBlocksOversizedBallAtMouth()
    {
        var colliders = WorkshopCanonicalColliders.Pipe(PipeLength.Default);
        Assert.Equal(24, colliders.Count);

        // 16 circumference colliders: inner face distance from X axis is >= 0.65 m
        for (var i = 0; i < 16; i++)
        {
            var c = colliders[i];
            var rCenter = MathF.Sqrt(MathF.Pow((float)c.LocalPose.Translation.Y, 2) + MathF.Pow((float)c.LocalPose.Translation.Z, 2));
            var innerFaceDist = rCenter - (float)c.HalfExtents.Y;
            Assert.True(MathF.Abs(innerFaceDist - 0.65f) < 0.002f, $"Inner face distance {innerFaceDist} must be ~0.65 m");
        }

        // Basketball radius 0.34 m and Bowling ball radius 0.28 m both fit with clearance
        Assert.True((float)BallMaterial.For(PartId.Basketball).Radius.Value < 0.65f);
        Assert.True((float)BallMaterial.For(PartId.BowlingBall).Radius.Value < 0.65f);

        // Oversized ball radius 0.75 m exceeds bore radius 0.65 m
        const float oversizedRadius = 0.75f;
        Assert.True(oversizedRadius > 0.65f);

        // Rim colliders: aperture in Y and Z is exactly [-0.65, 0.65]
        var rims = colliders.Skip(16).ToArray();
        Assert.Equal(8, rims.Length);
        // Top and bottom rims span Y in [0.65, 0.95] and [-0.95, -0.65]
        // Left and right rims span Z in [-0.95, -0.65] and [0.65, 0.95]
        // Oversized sphere centered at (X, 0, 0) has cross section of radius 0.75 m at the mouth entrance,
        // which overlaps all 4 rim colliders by 0.75 - 0.65 = 0.10 m.
        foreach (var rim in rims)
        {
            var y = (float)rim.LocalPose.Translation.Y;
            var z = (float)rim.LocalPose.Translation.Z;
            var innerFace = MathF.Abs(y) > MathF.Abs(z)
                ? MathF.Abs(y) - (float)rim.HalfExtents.Y
                : MathF.Abs(z) - (float)rim.HalfExtents.Z;
            Assert.InRange(innerFace, 0.648f, 0.652f);
            Assert.True(oversizedRadius > innerFace, "Oversized ball overlaps collar rim and is blocked");
        }
    }

    [Fact]
    public void PhysicsGpuAbi_AdmissionAcceptsPipeSceneDeclaration()
    {
        var pipe = WorkshopInput.Pipe(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Basketball(new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, pipe));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), profile);

        Assert.Equal(scene.World.ByteLength, bytes.Length);
        Assert.Equal(profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
    }
}
