using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text.Json;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class PipeBend90Tests(NativeSceneFixture godot)
{
    private static WorkshopConstruction Lesson(float precision) =>
        PuzzleCatalogue.Create(new("quarter_bend"), new(1), WorkshopCadenceSettings.Default(), new(1), new(precision));

    [Fact]
    public void PartRegistry_DiscoversPipeBend90DefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.PipeBend90));
        var def = registry.Definitions[PartId.PipeBend90];
        Assert.Equal(PartId.PipeBend90, def.PartId);
        Assert.Equal("pipe_bend_90", def.Id);
        Assert.Equal("90° pipe bend", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Motion, def.Category);
        Assert.Equal("A 90-degree hollow curve. Turns falling balls through a right angle with a smooth hollow bend.", def.Description);
        Assert.NotNull(def.Scene);

        var part = registry.Create(PartId.PipeBend90);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.PipeBend90, part.PartId);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void PipeBend90Scene_ExtractsItsTorusArcStubsCollarsAndTwoMouthPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/PipeBend90/pipe_bend_90.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

            BendColliders.Assert(colliders, 90);

            // 2 port locators: mouth_start and mouth_end
            Assert.Equal(2, ports.Count);
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            var start = ports["mouth_start"];
            Assert.Equal("mouth_start", start.PortId);
            Assert.InRange(start.Position.X, -1.79f, -1.78f);
            Assert.InRange(start.Position.Y, 0.70f, 0.71f);
            Assert.True(MathF.Abs(start.Position.Z) < 0.001f);

            var end = ports["mouth_end"];
            Assert.Equal("mouth_end", end.PortId);
            Assert.InRange(end.Position.X, 0.70f, 0.71f);
            Assert.InRange(end.Position.Y, -1.79f, -1.78f);
            Assert.True(MathF.Abs(end.Position.Z) < 0.001f);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void WorkshopCanonicalColliders_MatchesPipeBend90Colliders()
    {
        BendColliders.Assert(DeclaredPhysics.Colliders(PartId.PipeBend90), 90);
    }

    [Fact]
    public void WorkshopPhysicsCompiler_CompilesPipeBend90IntoStaticBodyWithItsHollowColliders()
    {
        var bend = WorkshopInput.PipeBend90(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(bend));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        Assert.Equal(2, scene.Bodies.Length); // plane + pipe_bend_90
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        Assert.Equal(new Kilograms(0f), body.Mass);
        Assert.Equal(new InverseSeconds(0f), body.LinearDrag);
        Assert.Equal(default, body.Gravity);

        var colliders = scene.Colliders.ToArray().Where(c => c.Body == body.Id).ToArray();
        Assert.Equal(BendColliders.Kinds, colliders.Select(c => c.Shape).ToArray());

        // All colliders use the body's material: the pipe family's documented restitution 0.15 (CAT-050).
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == colliders[0].Material);
        Assert.Equal(new Restitution(.15f), material.Restitution);
        Assert.Equal(new LinearSpeed(0.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(0.3f), material.Friction);
        Assert.Equal(new RollingResistance(0f), material.RollingResistance);
    }

    [Fact]
    public void SaveCodec_RoundTripsPipeBend90AndPreservesDeclaredColliders()
    {
        var bend = WorkshopInput.PipeBend90(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, bend));
        var save = new WorkshopSavedConstruction(construction, new(11));

        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);

        Assert.Equal(save, decoded);
        Assert.Equal(save, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(decoded)));

        var decodedBend = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.PipeBend90, decodedBend.PartId);
        BendColliders.Assert(decodedBend.DeclaredColliders, 90);

        // Verify FlatBuffers schema preserves PartId on saved instance record
        var fbSave = WorkshopSaveCodec.Open(bytes);
        Assert.True(fbSave.Construction.HasValue);
        var fbBend = fbSave.Construction.Value.Instances(1);
        Assert.True(fbBend.HasValue);
        Assert.Equal("pipe_bend_90", fbBend.Value.PartId);
    }

    [Fact]
    public void WorkbenchCapacity_AdmitsPipeBendsWithoutColliderLimit()
    {
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.PipeBend90(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 0, 5 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));

        // 2 pipe_bend_90: validates cleanly with unbounded colliders
        var twoBends = new WorkshopInstances(
            WorkshopInput.PipeBend90(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.PipeBend90(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1));
        new WorkshopInstances([.. twoBends]).Validate();

        // 20 pipe_bend_90: validates cleanly
        var twentyBends = new WorkshopInstances(
            Enumerable.Range(1, 20).Select(i => (IWorkshopInstance)WorkshopInput.PipeBend90(new GpuBodyId((ulong)i), (float)(i * 2), 3, 0, 0, 0, 0, 1)).ToArray());
        new WorkshopInstances([.. twentyBends]).Validate();
    }

    [Fact]
    public void BoreGeometry_MatchesTheDeclaredMouths()
    {
        BendColliders.AssertBore(DeclaredPhysics.Colliders(PartId.PipeBend90), TestBundle.Entry(PartId.PipeBend90));

        // Actual fitting and oversized passage is exercised by
        // WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload.
    }

    [Fact]
    public void PhysicsGpuAbi_AdmissionAcceptsPipeBend90SceneDeclaration()
    {
        var bend = WorkshopInput.PipeBend90(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 5, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, bend));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), profile);

        Assert.Equal(scene.World.ByteLength, bytes.Length);
        Assert.Equal(profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
    }

    [Fact]
    public void QuarterBend_AuthoredSettingsAndFixturesMatchPuzzlesJson()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "quarter_bend");
        Assert.Equal("Around the corner", level.GetProperty("title").GetString());
        Assert.Equal(1, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "pipe_bend_90").GetProperty("count").GetInt64());

        var construction = Lesson(0f);
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        Assert.Equal(new GpuBodyId(1), construction.Puzzle.Goal.Body);
        Assert.Equal(new GpuBodyId(2), construction.Puzzle.Goal.Target);

        var fixtures = level.GetProperty("placements").EnumerateArray().Select(row => row.GetProperty("instance")).ToArray();
        AuthoredContentAssertions.Placement(fixtures[0], Payloads.First(construction));
        AuthoredContentAssertions.Placement(fixtures[1], ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver)));
        Assert.True(Payloads.First(construction).Locked && ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver)).Locked);

        AuthoredContentAssertions.Profile(fixtures[0].GetProperty("assistance"), construction.Puzzle.PrimaryAssistance);
        AuthoredContentAssertions.Profile(fixtures[1].GetProperty("assistance"), construction.Puzzle.TargetAssistance);

        var solution = level.GetProperty("solution").EnumerateArray().ToArray();
        CheckProfile(solution.Single(v => v.GetProperty("kind").GetString() == "pipe_bend_90").GetProperty("difficulty"),
            construction.Puzzle.PlacementAssistance);

        var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
        Assert.Equal(PartAllowance.Counted(1), inventory[PartId.PipeBend90]);
    }

    [Fact]
    public void QuarterBend_ValidateEnforcesFixturesAndInventory()
    {
        var construction = Lesson(0f);
        construction.Validate();

        var bend = WorkshopInput.PipeBend90(new(3), 0, 4, 0, 0, 0, -0.7071068f, 0.7071068f);
        var placed = construction.WithInstance(bend);
        placed.Validate();

        // Second pipe bend 90 exceeds inventory limit
        var bend2 = WorkshopInput.PipeBend90(new(4), 1, 3, 0, 0, 0, 0, 1);
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => placed.WithInstance(bend2).Validate());

        // Locked player pipe bend is rejected
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => construction.WithInstance(bend with { Locked = true }).Validate());

        // Unlocking fixed ball or receiver is rejected
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(Payloads.First(construction) with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver)) with { Locked = false }).Validate());

        // Unauthorized parts are rejected
        var pipe = WorkshopInput.Pipe(new(5), 0, 1, 0, 0, 0, 0, 1);
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => placed.WithInstance(pipe).Validate());

        // Tampering with assistance profiles is rejected
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { PlacementAssistance = PuzzleCatalogue.Definition(new("first_principles")).Placement } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { InventoryCount = 2 } }).Validate());
    }

    [Theory]
    [InlineData(0, 0.3, 3.0, 0.15, 12)]
    [InlineData(0.45, 0.174, 2.325, 0.24, 6.6)]
    [InlineData(1.0, 0.02, 1.5, 0.35, 0)]
    public void QuarterBend_PrecisionEvaluatesAssistanceAndRoundtripsSaveCodec(double precision, double margin, double speed, double dwell, double accel)
    {
        var construction = Lesson((float)precision);
        var receiver = ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver));

        Assert.Equal((float)margin, receiver.Parameters[new("capture_margin")]);
        Assert.Equal((float)speed, receiver.Parameters[new("capture_speed_limit")]);
        Assert.Equal((float)dwell, receiver.Parameters[new("capture_dwell")]);
        Assert.Equal((float)accel, receiver.Parameters[new("force_maximum_acceleration")]);

        var bend = WorkshopInput.PipeBend90(new(3), 0, 4, 0, 0, 0, -0.7071068f, 0.7071068f);
        var placed = construction.WithInstance(bend);
        placed.Validate();

        var save = new WorkshopSavedConstruction(placed, new(10));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);

        var updated = PuzzleCatalogue.WithPrecision(placed, new(0.45f));
        Assert.Equal(0.174f, ((WorkshopGenericInstance)updated.Instances.Single(instance => instance.PartId == PartId.Receiver)).Parameters[new("capture_margin")]);
    }

    [Fact]
    public void QuarterBend_CompilerEmitsCompoundCollidersAndSensors()
    {
        var construction = Lesson(0.45f);
        var bend = WorkshopInput.PipeBend90(new(3), 0, 4, 0, 0, 0, -0.7071068f, 0.7071068f);
        var placed = construction.WithInstance(bend);

        var scene = WorkshopPhysicsCompiler.Compile(placed, new(1, 2));

        // Plane (1) + Ball (1) + Receiver (1) + PipeBend90 (1) = 4 bodies
        Assert.Equal(4, scene.Bodies.Length);

        // Plane (1) + Ball sphere and seam (2) + Receiver 5 boxes (5) + PipeBend90 arc, 2 stubs and 2 collars (5) = 13 colliders
        Assert.Equal(13, scene.Colliders.Length);
        Assert.Equal(new[] { ColliderShapeKind.Sphere, ColliderShapeKind.Torus },
            scene.Colliders.ToArray().Where(c => c.Body == new GpuBodyId(1)).Select(c => c.Shape));

        // Capture sensor and guide present
        Assert.Single(scene.Sensors.ToArray());
        Assert.Single(scene.Guides.ToArray());
        Assert.Equal(placed.Puzzle.Goal.Target, scene.Sensors[0].Frame);
        var payload = scene.Bodies.ToArray().Single(body => body.Id == placed.Puzzle.Goal.Body);
        Assert.True(scene.Sensors[0].Targets.Contains(payload.Id, payload.Payload));
    }



    private static void CheckProfile(JsonElement data, AssistanceProfile profile)
    {
        var values = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
        var names = new[] { "precision", "position_window", "rotation_window", "max_position_correction", "max_rotation_correction",
            "blend_seconds", "capture_margin", "capture_speed", "capture_dwell", "guide_acceleration", "trigger_threshold" };
        for (var i = 0; i < 3; i++)
        {
            var k = values[i];
            float[] actual = [k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value,
                k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value,
                k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold];
            for (var field = 0; field < names.Length; field++)
                Assert.Equal((float)data[i].GetProperty(names[field]).GetDouble(), actual[field]);
        }
    }
}
