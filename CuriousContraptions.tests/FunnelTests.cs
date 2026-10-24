using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class FunnelTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversFunnelDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.Funnel));
        var def = registry.Definitions[PartId.Funnel];
        Assert.Equal(PartId.Funnel, def.PartId);
        Assert.Equal("funnel", def.Id);
        Assert.Equal("Funnel", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Motion, def.Category);
        Assert.Equal("A wide hollow funnel. Gathers falling balls from a wide 2.6-unit mouth into the standard 1.3-unit tube bore by gravity and contact alone.", def.Description);
        Assert.NotNull(def.Scene);

        var part = registry.Create(PartId.Funnel);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.Funnel, part.PartId);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void FunnelScene_ExtractsExact72CollidersAndTwoMouthPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/Funnel/funnel.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

            // Exactly 72 colliders: 64 tapered wall boxes + 8 rim boxes
            Assert.Equal(72, colliders.Count);

            // 64 wall colliders
            var walls = colliders.Take(64).ToArray();
            foreach (var col in walls)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
                Assert.Equal((float)(0.485f * 0.5f), col.HalfExtents.X);
                Assert.Equal((float)(0.05f * 0.5f), col.HalfExtents.Y);
            }

            // 8 rim colliders
            var rims = colliders.Skip(64).ToArray();
            Assert.Equal(8, rims.Length);
            foreach (var col in rims)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
            }

            // 2 port locators: mouth_start (wide inlet) and mouth_end (outlet)
            Assert.Equal(2, ports.Count);
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            var start = ports["mouth_start"];
            Assert.Equal("mouth_start", start.PortId);
            Assert.InRange(start.Position.X, -1.0f, -0.98f);
            Assert.True(MathF.Abs(start.Position.Y) < 0.001f);
            Assert.True(MathF.Abs(start.Position.Z) < 0.001f);

            var end = ports["mouth_end"];
            Assert.Equal("mouth_end", end.PortId);
            Assert.InRange(end.Position.X, 0.98f, 1.0f);
            Assert.True(MathF.Abs(end.Position.Y) < 0.001f);
            Assert.True(MathF.Abs(end.Position.Z) < 0.001f);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void WorkshopCanonicalColliders_MatchesFunnelColliders()
    {
        var colliders = DeclaredPhysics.Colliders(PartId.Funnel);
        Assert.Equal(72, colliders.Count);

        for (var i = 0; i < 72; i++)
        {
            Assert.Equal(ColliderShapeKind.Box, colliders[i].ShapeKind);
        }
    }

    [Fact]
    public void WorkshopPhysicsCompiler_CompilesFunnelIntoStaticBodyWith72Colliders()
    {
        var funnel = WorkshopInput.Funnel(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(funnel));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        Assert.Equal(2, scene.Bodies.Length); // plane + funnel
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        Assert.Equal(new Kilograms(0f), body.Mass);
        Assert.Equal(new InverseSeconds(0f), body.LinearDrag);
        Assert.Equal(default, body.Gravity);

        var colliders = scene.Colliders.ToArray().Where(c => c.Body == body.Id).ToArray();
        Assert.Equal(72, colliders.Length);

        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == colliders[0].Material);
        Assert.Equal(new Restitution(1f), material.Restitution);
        Assert.Equal(new LinearSpeed(0.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(0.3f), material.Friction);
        Assert.Equal(new RollingResistance(0f), material.RollingResistance);
    }

    [Fact]
    public void SaveCodec_RoundTripsFunnelAndPreservesDeclaredColliders()
    {
        var funnel = WorkshopInput.Funnel(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, funnel));
        var save = new WorkshopSavedConstruction(construction, new(12));

        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);

        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));

        var decodedFunnel = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.Funnel, decodedFunnel.PartId);
        Assert.Equal(72, decodedFunnel.DeclaredColliders.Count);

        // Verify FlatBuffers schema preserves PartId on saved instance record
        var fbSave = WorkshopSaveCodec.Open(bytes);
        Assert.True(fbSave.Construction.HasValue);
        var fbFunnel = fbSave.Construction.Value.Instances(1);
        Assert.True(fbFunnel.HasValue);
        Assert.Equal("funnel", fbFunnel.Value.PartId);
    }

    [Fact]
    public void WorkbenchCapacity_AdmitsFunnelsWithoutColliderLimit()
    {
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.Funnel(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 0, 72 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));

        var twoFunnels = new WorkshopInstances(
            WorkshopInput.Funnel(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Funnel(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1));
        WorkbenchCapacity.Validate(twoFunnels);

        var twentyFunnels = new WorkshopInstances(
            Enumerable.Range(1, 20).Select(i => (IWorkshopInstance)WorkshopInput.Funnel(new GpuBodyId((ulong)i), i * 2, 3, 0, 0, 0, 0, 1)).ToArray());
        WorkbenchCapacity.Validate(twentyFunnels);
    }

    [Fact]
    public void BoreGeometry_AdmitsBallRadiusUnder65cm_AndBlocksOversizedBallAtOutlet()
    {
        var colliders = DeclaredPhysics.Colliders(PartId.Funnel);
        Assert.Equal(72, colliders.Count);

        // Basketball radius 0.34 m fits with clearance inside 0.65 m outlet and 1.3 m inlet
        Assert.True(LawBalls.Radius(LawBall.Basketball) < 0.65f);
        Assert.True(LawBalls.Radius(LawBall.Basketball) < 1.30f);

        // Oversized ball radius 0.75 m enters 1.30 m inlet but exceeds 0.65 m outlet
        const float oversizedRadius = 0.75f;
        Assert.True(oversizedRadius < 1.30f);
        Assert.True(oversizedRadius > 0.65f);
    }

    [Fact]
    public void PhysicsGpuAbi_AdmissionAcceptsFunnelSceneDeclaration()
    {
        var funnel = WorkshopInput.Funnel(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, funnel));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), profile);

        Assert.Equal(scene.World.ByteLength, bytes.Length);
        Assert.Equal(profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
    }
}
