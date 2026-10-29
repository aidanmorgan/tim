using System;
using System.Buffers.Binary;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;
using NumVector3 = System.Numerics.Vector3;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class StraightPipeTests(NativeSceneFixture godot)
{
    /// <summary>The Clear pipe's true hollow colliders at the default 3.6 m (CAT-048, engine-hollow-shapes): the shell Tube about local
    /// X between the 0.65 m bore and the drawn 0.7 m wall, reaching both mouths (half-length 1.8 + 0.09); the two collars the drawn
    /// TorusMesh (inner 0.65, outer 0.78: centreline 0.715, solid tube 0.065, a full turn) at x ±1.8.</summary>
    private static void AssertPipeColliders(IReadOnlyList<ExtractedCollider> colliders)
    {
        Assert.Equal(3, colliders.Count);
        TestBundle.AssertHollow(colliders[0], ColliderShapeKind.Tube, .7f, new(.65f, 1.89f, 0f), NumVector3.Zero, NumVector3.UnitX);
        TestBundle.AssertHollow(colliders[1], ColliderShapeKind.Torus, .715f, new(0f, .065f, ColliderDeclaration.FullSweep),
            new(-1.8f, 0, 0), NumVector3.UnitX);
        TestBundle.AssertHollow(colliders[2], ColliderShapeKind.Torus, .715f, new(0f, .065f, ColliderDeclaration.FullSweep),
            new(1.8f, 0, 0), NumVector3.UnitX);
    }

    [Fact]
    public void PartRegistry_DiscoversPipeDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(new PartId("pipe")));
        var def = registry.Definitions[new PartId("pipe")];
        Assert.Equal(new PartId("pipe"), def.PartId);
        Assert.Equal("pipe", def.Id);
        Assert.Equal("Clear pipe", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Motion, def.Category);
        Assert.Equal("A hollow tube. Gravity carries balls through the open bore; tilt it downhill to guide them. The clear shell transmits light; cream collars are opaque.", def.Description);
        Assert.NotNull(def.Scene);

        var part = registry.Create(new PartId("pipe"));
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(new PartId("pipe"), part.PartId);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void PipeScene_ExtractsItsShellTubeTwoCollarToriAndTwoMouthPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/Pipe/pipe.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);
            AssertPipeColliders(colliders);

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
    public void WorkshopPhysicsCompiler_CompilesPipeIntoStaticBodyWithItsThreeHollowColliders()
    {
        var pipe = WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(pipe));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        Assert.Equal(2, scene.Bodies.Length); // plane + pipe
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        Assert.Equal(new Kilograms(0f), body.Mass);
        Assert.Equal(new InverseSeconds(0f), body.LinearDrag);
        Assert.Equal(default, body.Gravity);

        var colliders = scene.Colliders.ToArray().Where(c => c.Body == body.Id).ToArray();
        Assert.Equal([ColliderShapeKind.Tube, ColliderShapeKind.Torus, ColliderShapeKind.Torus], colliders.Select(c => c.Shape).ToArray());

        // All colliders use the body's material: the pipe family's documented restitution 0.15, bounce threshold 0.1, friction 0.3.
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == colliders[0].Material);
        Assert.Equal(new Restitution(.15f), material.Restitution);
        Assert.Equal(new LinearSpeed(0.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(0.3f), material.Friction);
        Assert.Equal(new RollingResistance(0f), material.RollingResistance);
    }

    [Fact]
    public void SaveCodec_RoundTripsPipeAndPreservesDeclaredColliders()
    {
        var pipe = WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(new PartId("ball"),new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, pipe));
        var save = new WorkshopSavedConstruction(construction, new(11));

        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);

        Assert.Equal(save, decoded);
        Assert.Equal(save, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(decoded)));

        var decodedPipe = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(new PartId("pipe"), decodedPipe.PartId);
        Assert.Equal(3.6f, decodedPipe.Parameters[new("length")]);
        AssertPipeColliders(decodedPipe.DeclaredColliders);

        // Verify FlatBuffers schema preserves PartId on saved instance record
        var fbSave = WorkshopSaveCodec.Open(bytes);
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
            new WorkshopInstances(WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 0, 3 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));

        var threePipes = new WorkshopInstances(
            WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(3), 10, 3, 0, 0, 0, 0, 1));
        new WorkshopInstances([.. threePipes]).Validate();

        var twentyPipes = new WorkshopInstances(
            Enumerable.Range(1, 20).Select(i => (IWorkshopInstance)WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId((ulong)i), (float)(i * 2), 3, 0, 0, 0, 0, 1)).ToArray());
        new WorkshopInstances([.. twentyPipes]).Validate();
    }

    /// <summary>The bore admits a ball of radius under 0.65 m end to end: the shell Tube's inner radius and each collar's innermost
    /// reach (centreline less tube, R − r) are both the mouths' 0.65 m bore; a 0.75 m ball overlaps both by 0.1 m.</summary>
    [Fact]
    public void BoreGeometry_MatchesTheDeclaredMouths()
    {
        var colliders = DeclaredPhysics.Colliders(new PartId("pipe"));
        var bore = TestBundle.Entry(new PartId("pipe")).TubeMouths.Select(m => m.BoreRadius).Distinct().Single();
        Assert.True(MathF.Abs(colliders[0].HalfExtents.X - bore) <= 1e-6f, $"the shell's bore {colliders[0].HalfExtents.X} is not {bore}");
        foreach (var collar in colliders.Skip(1))
            Assert.True(MathF.Abs(collar.Radius - collar.HalfExtents.Y - bore) <= 1e-6f, $"a collar reaches {collar.Radius - collar.HalfExtents.Y}");

        // Actual fitting and oversized passage is exercised by
        // WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload.
    }

    [Fact]
    public void PhysicsGpuAbi_AdmissionAcceptsPipeSceneDeclaration()
    {
        var pipe = WorkshopInput.Generic(new PartId("pipe"),new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(new PartId("ball"),new GpuBodyId(2), 0, 5, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, pipe));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), profile);

        Assert.Equal(scene.World.ByteLength, bytes.Length);
        Assert.Equal(profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
    }
}
