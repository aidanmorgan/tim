using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ClearPipeLevelTests
{
    private static WorkshopConstruction Lesson(float precision) =>
        PuzzleCatalogue.Create(new("clear_pipe"), new(1), WorkshopCadenceSettings.Default(), new(1), new(precision));

    [Fact]
    public void AuthoredSettingsAndFixturesMatchPuzzlesJson()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "clear_pipe");
        Assert.Equal("Through the looking tube", level.GetProperty("title").GetString());
        Assert.Equal(1, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "pipe").GetProperty("count").GetInt32());

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
        CheckProfile(solution.Single(v => v.GetProperty("kind").GetString() == "pipe").GetProperty("difficulty"),
            construction.Puzzle.PlacementAssistance);

        var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
        Assert.Equal(PartAllowance.Counted(1), inventory[PartId.Pipe]);
    }

    [Fact]
    public void ValidateEnforcesFixturesAndInventory()
    {
        var construction = Lesson(0f);
        construction.Validate();

        var pipe = WorkshopInput.Pipe(new(3), 0, 3, 0, 0.7071068, 0, 0, 0.7071068);
        var placed = construction.WithInstance(pipe);
        placed.Validate();

        // Second pipe exceeds inventory limit
        var pipe2 = WorkshopInput.Pipe(new(4), 1, 3, 0, 0, 0, 0, 1);
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => placed.WithInstance(pipe2).Validate());

        // Locked player pipe is rejected
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => construction.WithInstance(pipe with { Locked = true }).Validate());

        // Unlocking fixed ball or receiver is rejected
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(Payloads.First(construction) with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver)) with { Locked = false }).Validate());

        // Unauthorized parts are rejected
        var ramp = WorkshopInput.Generic(PartId.Ramp, new(5), 0, 1, 0, 0, 0, 0, 1);
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => placed.WithInstance(ramp).Validate());

        // Tampering with assistance profiles is rejected
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { PlacementAssistance = PuzzleCatalogue.Definition(new("first_principles")).Placement } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { InventoryCount = 2 } }).Validate());
    }

    [Theory]
    [InlineData(0, 0.3, 3.0, 0.15, 12)]
    [InlineData(0.45, 0.174, 2.325, 0.24, 6.6)]
    [InlineData(1.0, 0.02, 1.5, 0.35, 0)]
    public void PrecisionEvaluatesAssistanceAndRoundtripsSaveCodec(double precision, double margin, double speed, double dwell, double accel)
    {
        var construction = Lesson((float)precision);
        var receiver = ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver));

        Assert.Equal((float)margin, receiver.Parameters[new("capture_margin")]);
        Assert.Equal((float)speed, receiver.Parameters[new("capture_speed_limit")]);
        Assert.Equal((float)dwell, receiver.Parameters[new("capture_dwell")]);
        Assert.Equal((float)accel, receiver.Parameters[new("force_maximum_acceleration")]);

        var pipe = WorkshopInput.Pipe(new(3), 0, 3, 0, 0, 0, 0, 1);
        var placed = construction.WithInstance(pipe);
        placed.Validate();

        var save = new WorkshopSavedConstruction(placed, new(10));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);

        var updated = PuzzleCatalogue.WithPrecision(placed, new(0.45f));
        Assert.Equal(0.174f, ((WorkshopGenericInstance)updated.Instances.Single(instance => instance.PartId == PartId.Receiver)).Parameters[new("capture_margin")]);
    }

    [Fact]
    public void CompilerEmitsCompoundCollidersAndSensors()
    {
        var construction = Lesson(0.45f);
        var pipe = WorkshopInput.Pipe(new(3), 0, 3, 0, 0.7071068, 0, 0, 0.7071068);
        var placed = construction.WithInstance(pipe);

        var scene = WorkshopPhysicsCompiler.Compile(placed, new(1, 2));

        // Plane (1) + Ball (1) + Receiver (1) + Pipe (1) = 4 bodies
        Assert.Equal(4, scene.Bodies.Length);

        // Plane (1) + Ball sphere and seam (2) + Receiver 5 boxes (5) + Pipe shell tube and 2 collar tori (3) = 11 colliders
        Assert.Equal(11, scene.Colliders.Length);
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
