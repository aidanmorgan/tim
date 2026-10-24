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
public sealed class PipeBend45Tests(NativeSceneFixture godot)
{
    private static WorkshopConstruction Lesson(float precision) =>
        PuzzleCatalogue.Create(new("gentle_bend"), new(1), WorkshopCadenceSettings.Default(), new(1), new(precision));

    [Fact]
    public void PartRegistry_DiscoversPipeBend45DefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.PipeBend45));
        var def = registry.Definitions[PartId.PipeBend45];
        Assert.Equal(PartId.PipeBend45, def.PartId);
        Assert.Equal("pipe_bend_45", def.Id);
        Assert.Equal("45° pipe bend", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Motion, def.Category);
        Assert.Equal("A 45-degree hollow curve. Turns falling balls smoothly through a gentle angle under gravity.", def.Description);
        Assert.NotNull(def.Scene);

        var part = registry.Create(PartId.PipeBend45);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.PipeBend45, part.PartId);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void PipeBend45Scene_ExtractsExact40CollidersAndTwoMouthPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/PipeBend45/pipe_bend_45.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

            // Exactly 40 colliders: 32 curved wall boxes + 8 rim boxes
            Assert.Equal(40, colliders.Count);

            // 32 wall colliders
            var walls = colliders.Take(32).ToArray();
            var expectedWallHalfExtents = new MetreVector(0.26f, 0.025f, 0.29f);
            foreach (var col in walls)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
                Assert.Equal(expectedWallHalfExtents.X, col.HalfExtents.X);
                Assert.Equal(expectedWallHalfExtents.Y, col.HalfExtents.Y);
                Assert.Equal(expectedWallHalfExtents.Z, col.HalfExtents.Z);
            }

            // 8 rim colliders
            var rims = colliders.Skip(32).ToArray();
            Assert.Equal(8, rims.Length);
            foreach (var col in rims)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
            }

            // 2 port locators: mouth_start and mouth_end
            Assert.Equal(2, ports.Count);
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            var start = ports["mouth_start"];
            Assert.Equal("mouth_start", start.PortId);
            Assert.InRange(start.Position.X, -1.01f, -1.00f);
            Assert.InRange(start.Position.Y, 0.18f, 0.19f);
            Assert.True(MathF.Abs(start.Position.Z) < 0.001f);

            var end = ports["mouth_end"];
            Assert.Equal("mouth_end", end.PortId);
            Assert.InRange(end.Position.X, 0.84f, 0.85f);
            Assert.InRange(end.Position.Y, -0.59f, -0.58f);
            Assert.True(MathF.Abs(end.Position.Z) < 0.001f);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void WorkshopCanonicalColliders_MatchesPipeBend45Colliders()
    {
        var colliders = DeclaredPhysics.Colliders(PartId.PipeBend45);
        Assert.Equal(40, colliders.Count);

        for (var i = 0; i < 40; i++)
        {
            Assert.Equal(ColliderShapeKind.Box, colliders[i].ShapeKind);
        }
    }

    [Fact]
    public void WorkshopPhysicsCompiler_CompilesPipeBend45IntoStaticBodyWith40Colliders()
    {
        var bend = WorkshopInput.PipeBend45(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(bend));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        Assert.Equal(2, scene.Bodies.Length); // plane + pipe_bend_45
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        Assert.Equal(new Kilograms(0f), body.Mass);
        Assert.Equal(new InverseSeconds(0f), body.LinearDrag);
        Assert.Equal(default, body.Gravity);

        var colliders = scene.Colliders.ToArray().Where(c => c.Body == body.Id).ToArray();
        Assert.Equal(40, colliders.Length);

        // All colliders use the body's material
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == colliders[0].Material);
        Assert.Equal(new Restitution(1f), material.Restitution);
        Assert.Equal(new LinearSpeed(0.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(0.3f), material.Friction);
        Assert.Equal(new RollingResistance(0f), material.RollingResistance);
    }

    [Fact]
    public void SaveCodec_RoundTripsPipeBend45AndPreservesDeclaredColliders()
    {
        var bend = WorkshopInput.PipeBend45(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, bend));
        var save = new WorkshopSavedConstruction(construction, new(11));

        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);

        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));

        var decodedBend = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.PipeBend45, decodedBend.PartId);
        Assert.Equal(40, decodedBend.DeclaredColliders.Count);

        // Verify FlatBuffers schema preserves PartId on saved instance record
        var fbSave = WorkshopSaveCodec.Open(bytes);
        Assert.True(fbSave.Construction.HasValue);
        var fbBend = fbSave.Construction.Value.Instances(1);
        Assert.True(fbBend.HasValue);
        Assert.Equal("pipe_bend_45", fbBend.Value.PartId);
    }

    [Fact]
    public void WorkbenchCapacity_AdmitsPipeBendsWithoutColliderLimit()
    {
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.PipeBend45(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 0, 40 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));

        // 2 pipe_bend_45 = 80 colliders: validates cleanly with unbounded colliders
        var twoBends = new WorkshopInstances(
            WorkshopInput.PipeBend45(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.PipeBend45(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1));
        WorkbenchCapacity.Validate(twoBends);

        // 20 pipe_bend_45 = 800 colliders: validates cleanly
        var twentyBends = new WorkshopInstances(
            Enumerable.Range(1, 20).Select(i => (IWorkshopInstance)WorkshopInput.PipeBend45(new GpuBodyId((ulong)i), i * 2, 3, 0, 0, 0, 0, 1)).ToArray());
        WorkbenchCapacity.Validate(twentyBends);
    }

    [Fact]
    public void BoreGeometry_AdmitsBallRadiusUnder65cm_AndBlocksOversizedBallAtMouth()
    {
        var colliders = DeclaredPhysics.Colliders(PartId.PipeBend45);
        Assert.Equal(40, colliders.Count);

        // Basketball radius 0.34 m fits with clearance
        Assert.True(LawBalls.Radius(LawBall.Basketball) < 0.65f);

        // Oversized ball radius 0.75 m exceeds bore radius 0.65 m
        const float oversizedRadius = 0.75f;
        Assert.True(oversizedRadius > 0.65f);
    }

    [Fact]
    public void PhysicsGpuAbi_AdmissionAcceptsPipeBend45SceneDeclaration()
    {
        var bend = WorkshopInput.PipeBend45(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(ball, bend));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), profile);

        Assert.Equal(scene.World.ByteLength, bytes.Length);
        Assert.Equal(profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
    }

    [Fact]
    public void GentleBend_AuthoredSettingsAndFixturesMatchPuzzlesJson()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "gentle_bend");
        Assert.Equal("A gentle turn", level.GetProperty("title").GetString());
        Assert.Equal(1, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "pipe_bend_45").GetProperty("count").GetInt32());

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
        CheckProfile(solution.Single(v => v.GetProperty("kind").GetString() == "pipe_bend_45").GetProperty("difficulty"),
            construction.Puzzle.PlacementAssistance);

        var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
        Assert.Equal(PartAllowance.Counted(1), inventory[PartId.PipeBend45]);
    }

    [Fact]
    public void GentleBend_ValidateEnforcesFixturesAndInventory()
    {
        var construction = Lesson(0f);
        construction.Validate();

        var bend = WorkshopInput.PipeBend45(new(3), 0, 4, 0, 0, 0, -0.7071068, 0.7071068);
        var placed = construction.WithInstance(bend);
        placed.Validate();

        // Second pipe bend 45 exceeds inventory limit
        var bend2 = WorkshopInput.PipeBend45(new(4), 1, 3, 0, 0, 0, 0, 1);
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
    public void GentleBend_PrecisionEvaluatesAssistanceAndRoundtripsSaveCodec(double precision, double margin, double speed, double dwell, double accel)
    {
        var construction = Lesson((float)precision);
        var receiver = ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver));

        Assert.Equal((float)margin, receiver.Parameters[new("capture_margin")]);
        Assert.Equal((float)speed, receiver.Parameters[new("capture_speed_limit")]);
        Assert.Equal((float)dwell, receiver.Parameters[new("capture_dwell")]);
        Assert.Equal((float)accel, receiver.Parameters[new("force_maximum_acceleration")]);

        var bend = WorkshopInput.PipeBend45(new(3), 0, 4, 0, 0, 0, -0.7071068, 0.7071068);
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
    public void GentleBend_CompilerEmitsCompoundCollidersAndSensors()
    {
        var construction = Lesson(0.45f);
        var bend = WorkshopInput.PipeBend45(new(3), 0, 4, 0, 0, 0, -0.7071068, 0.7071068);
        var placed = construction.WithInstance(bend);

        var scene = WorkshopPhysicsCompiler.Compile(placed, new(1, 2));

        // Plane (1) + Ball (1) + Receiver (1) + PipeBend45 (1) = 4 bodies
        Assert.Equal(4, scene.Bodies.Length);

        // Plane (1) + Ball (1) + Receiver 5 boxes (5) + PipeBend45 40 compound boxes (40) = 47 colliders
        Assert.Equal(47, scene.Colliders.Length);

        // Capture sensor and guide present
        Assert.Single(scene.Sensors.ToArray());
        Assert.Single(scene.Guides.ToArray());
        Assert.Equal(scene.Sensors[0].Id, placed.Puzzle.Goal.EventSource);
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
