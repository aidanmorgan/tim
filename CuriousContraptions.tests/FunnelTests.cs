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

    /// <summary>The Funnel's true hollow colliders (CAT-030, engine-hollow-shapes): the Frustum wall along local X from the 1.35 m outer
    /// radius at the inlet (x −0.9) to 0.7 m at the outlet (x +0.9), radial wall 0.05 m (bores 1.3 and 0.65 m); a 9 cm Tube stub from
    /// each collar to its mouth (outer 1.35 / bore 1.3 at the inlet, outer 0.7 / bore 0.65 at the outlet); the drawn collar tori (inlet
    /// inner 1.3, outer 1.43; outlet inner 0.65, outer 0.78) at x ∓0.9.</summary>
    private static void AssertFunnelColliders(IReadOnlyList<ExtractedCollider> colliders)
    {
        Assert.Equal(5, colliders.Count);
        var x = System.Numerics.Vector3.UnitX;
        TestBundle.AssertHollow(colliders[0], ColliderShapeKind.Frustum, 1.35f, new(.05f, .9f, .7f), System.Numerics.Vector3.Zero, x);
        // The frustum's local +Y (its 0.7 m end) points to the outlet at +X.
        var q = colliders[0].LocalPose.Rotation;
        Assert.True(System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W)).X > .99999f,
            "the frustum narrows toward the outlet");
        TestBundle.AssertHollow(colliders[1], ColliderShapeKind.Tube, 1.35f, new(1.3f, .045f, 0f), new(-.945f, 0, 0), x);
        TestBundle.AssertHollow(colliders[2], ColliderShapeKind.Tube, .7f, new(.65f, .045f, 0f), new(.945f, 0, 0), x);
        TestBundle.AssertHollow(colliders[3], ColliderShapeKind.Torus, 1.365f, new(0f, .065f, ColliderDeclaration.FullSweep), new(-.9f, 0, 0), x);
        TestBundle.AssertHollow(colliders[4], ColliderShapeKind.Torus, .715f, new(0f, .065f, ColliderDeclaration.FullSweep), new(.9f, 0, 0), x);
    }

    [Fact]
    public void FunnelScene_ExtractsItsFrustumStubsCollarsAndTwoMouthPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/Funnel/funnel.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);
            AssertFunnelColliders(colliders);

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
        AssertFunnelColliders(DeclaredPhysics.Colliders(PartId.Funnel));
    }

    [Fact]
    public void WorkshopPhysicsCompiler_CompilesFunnelIntoStaticBodyWithItsHollowColliders()
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
        Assert.Equal([ColliderShapeKind.Frustum, ColliderShapeKind.Tube, ColliderShapeKind.Tube, ColliderShapeKind.Torus, ColliderShapeKind.Torus],
            colliders.Select(c => c.Shape).ToArray());

        // The pipe family's documented restitution 0.15 (CAT-030).
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == colliders[0].Material);
        Assert.Equal(new Restitution(.15f), material.Restitution);
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
        Assert.Equal(save, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(decoded)));

        var decodedFunnel = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.Funnel, decodedFunnel.PartId);
        AssertFunnelColliders(decodedFunnel.DeclaredColliders);

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
        Assert.Equal((1 + 1, 0, 5 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));

        var twoFunnels = new WorkshopInstances(
            WorkshopInput.Funnel(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Funnel(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1));
        new WorkshopInstances([.. twoFunnels]).Validate();

        var twentyFunnels = new WorkshopInstances(
            Enumerable.Range(1, 20).Select(i => (IWorkshopInstance)WorkshopInput.Funnel(new GpuBodyId((ulong)i), i * 2, 3, 0, 0, 0, 0, 1)).ToArray());
        new WorkshopInstances([.. twentyFunnels]).Validate();
    }

    [Fact]
    public void BoreGeometry_AdmitsBallRadiusUnder65cm_AndBlocksOversizedBallAtOutlet()
    {
        // The inlet stub's bore and the frustum's wide end are the inlet mouth's 1.3 m; its narrow end and the outlet stub's bore are the
        // outlet mouth's 0.65 m.
        var colliders = DeclaredPhysics.Colliders(PartId.Funnel);
        var mouths = TestBundle.Entry(PartId.Funnel).TubeMouths;
        var (inlet, outlet) = (mouths.Single(m => m.End == CuriousContraptions.Abstractions.TubeEnd.Start).BoreRadius,
            mouths.Single(m => m.End == CuriousContraptions.Abstractions.TubeEnd.End).BoreRadius);
        Assert.Equal(inlet, colliders[0].Radius - colliders[0].HalfExtents.X, 1e-6f);
        Assert.Equal(inlet, colliders[1].HalfExtents.X, 1e-6f);
        Assert.Equal(outlet, colliders[0].HalfExtents.Z - colliders[0].HalfExtents.X, 1e-6f);
        Assert.Equal(outlet, colliders[2].HalfExtents.X, 1e-6f);

        // Actual fitting and oversized passage is exercised by
        // WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload.
    }

    [Fact]
    public void PhysicsGpuAbi_AdmissionAcceptsFunnelSceneDeclaration()
    {
        var funnel = WorkshopInput.Funnel(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 5, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, funnel));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), profile);

        Assert.Equal(scene.World.ByteLength, bytes.Length);
        Assert.Equal(profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
    }
}
