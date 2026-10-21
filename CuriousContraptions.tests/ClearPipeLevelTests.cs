using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ClearPipeLevelTests
{
    private static WorkshopConstruction Lesson(Half precision) =>
        ClearPipe.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new(precision));

    [Fact]
    public void AuthoredSettingsAndFixturesMatchPuzzlesJson()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "clear_pipe");
        Assert.Equal("Through the looking tube", level.GetProperty("title").GetString());
        Assert.Equal(1, level.GetProperty("inventory").GetProperty("pipe").GetInt32());

        var construction = Lesson((Half)0);
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        Assert.Equal(new GpuBodyId(1), construction.Puzzle.Goal.Body);
        Assert.Equal(new GpuBodyId(2), construction.Puzzle.Goal.Target);

        var fixtures = level.GetProperty("parts");
        CheckSourcePose(fixtures[0], construction.Ball!.Value.Cell, construction.Ball!.Value.Local, construction.Ball!.Value.Rotation);
        CheckSourcePose(fixtures[1], construction.Receiver!.Value.Cell, construction.Receiver!.Value.Local, construction.Receiver!.Value.Rotation);
        Assert.True(construction.Ball!.Value.Locked && construction.Receiver!.Value.Locked);

        CheckProfile(fixtures[0].GetProperty("difficulty"), construction.Puzzle.BallAssistance);
        CheckProfile(fixtures[1].GetProperty("difficulty"), construction.Puzzle.ReceiverAssistance);

        var solution = level.GetProperty("solution").EnumerateArray().ToArray();
        CheckProfile(solution.Single(v => v.GetProperty("kind").GetString() == "pipe").GetProperty("difficulty"),
            construction.Puzzle.RampAssistance);

        var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
        Assert.Equal(PartAllowance.Counted(1), inventory[PartId.Pipe]);
    }

    [Fact]
    public void ValidateEnforcesFixturesAndInventory()
    {
        var construction = Lesson((Half)0);
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
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(construction.Ball!.Value with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(construction.Receiver!.Value with { Locked = false }).Validate());

        // Unauthorized parts are rejected
        var ramp = WorkshopInput.Ramp(new(5), 0, 1, 0, 0, 0, 0, 1, RampDimensions.Default);
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => placed.WithInstance(ramp).Validate());

        // Tampering with assistance profiles is rejected
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { RampAssistance = FirstPrinciples.RampAssistance } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { InventoryCount = 2 } }).Validate());
    }

    [Theory]
    [InlineData(0, 0.3, 3.0, 0.15, 12)]
    [InlineData(0.45, 0.174, 2.325, 0.24, 6.6)]
    [InlineData(1.0, 0.02, 1.5, 0.35, 0)]
    public void PrecisionEvaluatesAssistanceAndRoundtripsSaveCodec(double precision, double margin, double speed, double dwell, double accel)
    {
        var construction = Lesson((Half)precision);
        var receiver = construction.Receiver!.Value;

        Assert.Equal((Half)margin, receiver.Capture.Margin.Value);
        Assert.Equal((Half)speed, receiver.Capture.SpeedLimit.Value);
        Assert.Equal((Half)dwell, receiver.Capture.Dwell.Value);
        Assert.Equal((Half)accel, receiver.ForceRegion.MaximumAcceleration.Value);

        var pipe = WorkshopInput.Pipe(new(3), 0, 3, 0, 0, 0, 0, 1);
        var placed = construction.WithInstance(pipe);
        placed.Validate();

        var save = new WorkshopSavedConstruction(placed, new(10));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);

        var updated = ClearPipe.WithPrecision(placed, new((Half)0.45));
        Assert.Equal((Half)0.174, updated.Receiver!.Value.Capture.Margin.Value);
    }

    [Fact]
    public void CompilerEmitsCompoundCollidersAndSensors()
    {
        var construction = Lesson((Half)0.45);
        var pipe = WorkshopInput.Pipe(new(3), 0, 3, 0, 0.7071068, 0, 0, 0.7071068);
        var placed = construction.WithInstance(pipe);

        var scene = WorkshopPhysicsCompiler.Compile(placed, new(1, 2));

        // Plane (1) + Ball (1) + Receiver (1) + Pipe (1) = 4 bodies
        Assert.Equal(4, scene.Bodies.Length);

        // Plane (1) + Ball (1) + Receiver 5 boxes (5) + Pipe 24 compound boxes (24) = 31 colliders
        Assert.Equal(31, scene.Colliders.Length);

        // Capture sensor and guide present
        Assert.Single(scene.Sensors.ToArray());
        Assert.Single(scene.Guides.ToArray());
        Assert.Equal(scene.Sensors[0].Id, placed.Puzzle.Goal.EventSource);
    }

    private static void CheckSourcePose(JsonElement source, CellOrigin cell,
        LocalPosition local, CanonicalRotation rotation)
    {
        var position = source.GetProperty("position");
        Assert.InRange(Math.Abs((cell.X + (double)local.X) / 16 - position[0].GetDouble()), 0, .001);
        Assert.InRange(Math.Abs((cell.Y + (double)local.Y) / 16 - position[1].GetDouble()), 0, .001);
        Assert.InRange(Math.Abs((cell.Z + (double)local.Z) / 16 - position[2].GetDouble()), 0, .001);
        var q = new Godot.Quaternion((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)rotation.W).Normalized();
        var basis = new Godot.Basis(q);
        var orientation = source.GetProperty("orientation");
        for (var column = 0; column < 3; column++)
            for (var row = 0; row < 3; row++)
                Assert.InRange(Math.Abs(basis[column][row] - orientation[column * 3 + row].GetDouble()), 0, .002);
    }

    private static void CheckProfile(JsonElement data, AssistanceProfile profile)
    {
        var values = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
        var names = new[] { "precision", "position_window", "rotation_window", "max_position_correction", "max_rotation_correction",
            "blend_seconds", "capture_margin", "capture_speed", "capture_dwell", "guide_acceleration", "trigger_threshold" };
        for (var i = 0; i < 3; i++)
        {
            var k = values[i];
            Half[] actual = [k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value,
                k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value,
                k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold];
            for (var field = 0; field < names.Length; field++)
                Assert.Equal((Half)data[i].GetProperty(names[field]).GetDouble(), actual[field]);
        }
    }
}
