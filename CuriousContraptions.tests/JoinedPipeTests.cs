using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>Story 9R.7: the joined_pipe lesson ("Meet in the middle"). Fixtures, knots and inventory equal content/puzzles.json; the
/// route solves on the production Simulation only when a 2 m pipe is snapped to the fixed bend.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class JoinedPipeTests
{
    private static readonly GpuBodyId BallId = new(1), BendId = new(2), ReceiverId = new(3), PipeId = new(4);
    private const int RunTicks = 1200;

    private static WorkshopConstruction Lesson(Half precision) =>
        // Revision 2: the production Simulation admits the first Construct after its initial revision 1.
        JoinedPipe.Create(new(2), WorkshopCadenceSettings.Default(), BallId, BendId, ReceiverId, new(precision));

    private static JsonElement Content()
    {
        var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        return source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "joined_pipe").Clone();
    }

    private static IReadOnlyDictionary<PartId, PartDefinition> Definitions()
    {
        var registry = new PartRegistry();
        registry.Load();
        return registry.Definitions;
    }

    private static Quaternion Rz(float degrees) => new(Vector3.Back, Mathf.DegToRad(degrees));

    private static WorkshopPipe PipeAt(Vector3 position, Quaternion rotation, float length, bool locked = false)
    {
        var q = rotation.Normalized();
        return WorkshopInput.Pipe(PipeId, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W, new(length)) with { Locked = locked };
    }

    /// <summary>The granted pipe tilted downhill (−45° about Z), offered 0.1 m off the bend's upper mouth and snapped by the query.</summary>
    private static WorkshopPipe Snapped(WorkshopConstruction lesson, float length)
    {
        var definitions = Definitions();
        var length32 = new PipeLength(length);
        var rotation = Rz(-45);
        var bend = lesson.Instances.Single(instance => instance.PartId == PartId.PipeBend90);
        var bendAt = new Vector3((float)((bend.Cell.X + (double)bend.Local.X) / 16), (float)((bend.Cell.Y + (double)bend.Local.Y) / 16), 0);
        var bendTurn = new Quaternion(bend.Rotation.X, bend.Rotation.Y, bend.Rotation.Z, bend.Rotation.W);
        var mouth = bendAt + bendTurn * TubeMouthSnap.LocalMouth(definitions[PartId.PipeBend90], TubeMouthEnd.Start, null);
        var offered = mouth - rotation * new Vector3(length32.MouthOffset, 0, 0) + new Vector3(-.06f, .08f, 0);
        var snap = TubeMouthSnap.Find(definitions, lesson.Instances, new(PartId.Pipe, PipeId, false, length32), offered, rotation);
        Assert.NotNull(snap);
        Assert.Equal((BendId, TubeMouthEnd.Start, TubeMouthEnd.End), (snap.Value.TargetBody, snap.Value.TargetMouth, snap.Value.MoverMouth));
        return PipeAt(snap.Value.Position, snap.Value.Rotation, length);
    }

    private static bool Solved(WorkshopConstruction construction, IEnumerable<WorkshopRead> run) =>
        run.Any(read => WorkshopGoalEvaluator.Occurrence(construction.Puzzle.Goal, read.Captures, read.Activations) is not null);

    private static Vector3 Ball(WorkshopRead read)
    {
        read.Bodies.TryGet(BallId, out var ball);
        return new((ball.Body.Cell.X + ball.Body.Local.X) / 16, (ball.Body.Cell.Y + ball.Body.Local.Y) / 16, (ball.Body.Cell.Z + ball.Body.Local.Z) / 16);
    }

    // ---------- Fixtures, knots and inventory equal the content ----------

    [Fact]
    public void FixturesKnotsAndInventoryEqualTheContent()
    {
        var level = Content();
        Assert.Equal("Meet in the middle", level.GetProperty("title").GetString());
        Assert.Equal(["pipe"], level.GetProperty("inventory").EnumerateObject().Select(entry => entry.Name).ToArray());
        Assert.Equal(1, level.GetProperty("inventory").GetProperty("pipe").GetInt32());

        var construction = Lesson((Half)0);
        var puzzle = construction.Puzzle;
        Assert.Equal((WorkshopPuzzleId.JoinedPipe, PartId.Pipe, 1u), (puzzle.Id, puzzle.InventoryPart, puzzle.InventoryCount));
        var goal = Assert.Single(level.GetProperty("goals").EnumerateArray());
        Assert.Equal(("captured", "receiver", "ball"), (goal.GetProperty("type").GetString(), goal.GetProperty("target").GetString(), goal.GetProperty("body").GetString()));
        Assert.Equal((WorkshopGoalKind.Captured, BallId, ReceiverId), (puzzle.Goal.Kind, puzzle.Goal.Body, puzzle.Goal.Target));
        Assert.Equal(PartAllowance.Counted(1), Assert.Single(WorkshopInventoryPolicy.Authored(puzzle)).Value);
        Assert.Equal(PartId.Pipe, Assert.Single(WorkshopInventoryPolicy.Authored(puzzle)).Key);

        var parts = level.GetProperty("parts").EnumerateArray().ToDictionary(part => part.GetProperty("id").GetString()!);
        Assert.Equal(["ball", "bend", "receiver"], parts.Keys.ToArray());
        Assert.Equal(3, construction.Instances.Count);
        var expected = new Dictionary<string, (IWorkshopInstance Instance, string Kind, AssistanceProfile Knots)>
        {
            ["ball"] = (construction.Instances.Single(i => i.Id == BallId), "ball", puzzle.BallAssistance),
            ["bend"] = (construction.Instances.Single(i => i.Id == BendId), "pipe_bend_90", JoinedPipe.FixtureAssistance),
            ["receiver"] = (construction.Instances.Single(i => i.Id == ReceiverId), "basket", puzzle.ReceiverAssistance),
        };
        foreach (var (name, (instance, kind, knots)) in expected)
        {
            var source = parts[name];
            Assert.Equal(kind, source.GetProperty("kind").GetString());
            Assert.Equal(kind == "basket" ? PartId.Receiver : kind == "ball" ? PartId.Basketball : PartId.PipeBend90, instance.PartId);
            Assert.True(source.GetProperty("locked").GetBoolean() && instance.Locked, $"{name} is a locked fixture");
            CheckSourcePose(source, instance);
            CheckProfile(source.GetProperty("difficulty"), knots);
        }
        // The fixed bend is the ball's knots exactly (the content repeats them).
        Assert.Equal(puzzle.BallAssistance, JoinedPipe.FixtureAssistance);

        var solution = Assert.Single(level.GetProperty("solution").EnumerateArray());
        Assert.Equal(("pipe", false), (solution.GetProperty("kind").GetString(), solution.GetProperty("locked").GetBoolean()));
        Assert.Equal(2, solution.GetProperty("properties").GetProperty("length").GetDouble());
        CheckProfile(solution.GetProperty("difficulty"), puzzle.RampAssistance);
        Assert.Empty(level.GetProperty("solution_connections").EnumerateArray());
    }

    [Fact]
    public void TheSnappedTwoMetrePipeIsTheContentSolution()
    {
        var level = Content();
        var solution = Assert.Single(level.GetProperty("solution").EnumerateArray());
        var pipe = Snapped(Lesson((Half)1), 2);
        CheckSourcePose(solution, pipe, .0002);
        // Its upper mouth lies on the ball's drop line.
        var at = new Vector3((float)((pipe.Cell.X + (double)pipe.Local.X) / 16), (float)((pipe.Cell.Y + (double)pipe.Local.Y) / 16), 0);
        var upper = at + new Quaternion(pipe.Rotation.X, pipe.Rotation.Y, pipe.Rotation.Z, pipe.Rotation.W) * new Vector3(-pipe.Length.MouthOffset, 0, 0);
        Assert.InRange(upper.X, -1.3081f - .001f, -1.3081f + .001f);
    }

    // ---------- Validation ----------

    [Fact]
    public void ValidateAdmitsTheGrantedPipeOnly()
    {
        var lesson = Lesson((Half)1);
        var placed = lesson.WithInstance(Snapped(lesson, 2));
        placed.Validate();

        // A second pipe exceeds the inventory.
        var second = WorkshopInput.Pipe(new(5), 3, 6, 0, 0, 0, 0, 1, new(2));
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => placed.WithInstance(second).Validate());
        // A locked player pipe, an unlocked or second bend and any other part reject.
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => lesson.WithInstance(Snapped(lesson, 2) with { Locked = true }).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported,
            () => placed.WithInstance(WorkshopInput.PipeBend90(new(5), 3, 3, 0, 0, 0, 0, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported,
            () => placed.WithInstance(WorkshopInput.Ramp(new(5), 3, 1, 0, 0, 0, 0, 1, RampDimensions.Default)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported,
            () => placed.WithInstance(WorkshopInput.PipeBend45(new(5), 3, 3, 0, 0, 0, 0, 1)).Validate());
    }

    [Fact]
    public void ALockedFixedBendIsRequired()
    {
        var lesson = Lesson((Half)0);
        var bend = (WorkshopGenericInstance)lesson.Instances.Single(instance => instance.Id == BendId);
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => (lesson with { Instances = lesson.Instances.Without(BendId) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { Cell = new(bend.Cell.X + 1, bend.Cell.Y, bend.Cell.Z) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { Rotation = CanonicalRotation.Identity }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { DeclaredColliders = WorkshopCanonicalColliders.PipeBend45Colliders }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(lesson.Ball!.Value with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(lesson.Receiver!.Value with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (lesson with { Puzzle = lesson.Puzzle with { InventoryCount = 2 } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (lesson with { Puzzle = lesson.Puzzle with { RampAssistance = FirstPrinciples.RampAssistance } }).Validate());
    }

    [Theory]
    [InlineData(0, .3, 3.0, .15, 12)]
    [InlineData(.45, .174, 2.325, .24, 6.6)]
    [InlineData(1.0, .02, 1.5, .35, 0)]
    public void PrecisionEvaluatesTheReceiverAndTheLessonRoundTripsTheSaveCodec(double precision, double margin, double speed, double dwell, double accel)
    {
        var lesson = Lesson((Half)precision);
        var receiver = lesson.Receiver!.Value;
        Assert.Equal(((Half)margin, (Half)speed, (Half)dwell, (Half)accel),
            (receiver.Capture.Margin.Value, receiver.Capture.SpeedLimit.Value, receiver.Capture.Dwell.Value, receiver.ForceRegion.MaximumAcceleration.Value));
        var placed = lesson.WithInstance(Snapped(lesson, 2));
        var saved = new WorkshopSavedConstruction(placed, new(5));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
        var updated = JoinedPipe.WithPrecision(placed, new((Half).45));
        Assert.Equal((Half).174, updated.Receiver!.Value.Capture.Margin.Value);
        Assert.Equal(placed.Instances.Single(i => i.Id == PipeId), updated.Instances.Single(i => i.Id == PipeId));
    }

    // ---------- The route on the production Simulation, at Precise ----------

    [Fact]
    public async Task TheSnappedTwoMetrePipeDeliversTheBallAndResetRestoresTheBench()
    {
        var lesson = Lesson((Half)1);
        var bench = lesson.WithInstance(Snapped(lesson, 2));
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
        Assert.True(Solved(bench, run), $"the joined route solves (ball last at {Ball(run[^1])})");
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
    }

    [Fact]
    public async Task TheDefaultLengthSnappedLeavesTheBallOnTheOuterShell()
    {
        var lesson = Lesson((Half)1);
        var bench = lesson.WithInstance(Snapped(lesson, 3.6f));
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
        Assert.False(Solved(bench, run), $"a 3.6 m pipe misses the drop line (ball last at {Ball(run[^1])})");
    }

    [Fact]
    public async Task AnUnsnappedPipeLeavesTheGoalUnsolved()
    {
        var lesson = Lesson((Half)1);
        var snapped = Snapped(lesson, 2);
        var unsnapped = PipeAt(UnsnappedPosition(snapped), Rz(-45), 2);
        var definitions = Definitions();
        Assert.Null(TubeMouthSnap.Find(definitions, lesson.Instances, new(PartId.Pipe, PipeId, false, unsnapped.Length),
            UnsnappedPosition(snapped), Rz(-45)));
        var bench = lesson.WithInstance(unsnapped);
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
        Assert.False(Solved(bench, run), $"the unsnapped route does not solve (ball last at {Ball(run[^1])})");
        // The ball lands on the shifted pipe's outer shell: it never enters the bore and leaves the route between the mouths.
        var centre = new Vector3((float)((unsnapped.Cell.X + (double)unsnapped.Local.X) / 16), (float)((unsnapped.Cell.Y + (double)unsnapped.Local.Y) / 16), 0);
        var axis = Rz(-45) * Vector3.Right;
        foreach (var read in run)
        {
            var offset = Ball(read) - centre;
            var along = offset.Dot(axis);
            var radial = (offset - axis * along).Length();
            Assert.False(MathF.Abs(along) <= unsnapped.Length.HalfLength && radial < .65f, $"the ball entered the bore at {Ball(read)}");
        }
    }

    /// <summary>The negative-control pose of cat-050-joined test 4: a refused snap keeps the 0.1 grid, and (−1.3, 6) puts the lower mouth
    /// 0.76 m left of the bend's upper mouth, outside the 0.45 m capture. Poses about 0.72 m up, down or up the axis from the joint still
    /// deliver the ball (native-pose-probe.txt), so the suites use this one.</summary>
    internal static Vector3 UnsnappedPosition(WorkshopPipe snapped)
    {
        var joint = new Vector3((float)((snapped.Cell.X + (double)snapped.Local.X) / 16), (float)((snapped.Cell.Y + (double)snapped.Local.Y) / 16), 0);
        var unsnapped = new Vector3(-1.3f, 6, 0);
        Assert.InRange(joint.DistanceTo(unsnapped), .72f, 1f);
        return unsnapped;
    }

    private static void CheckSourcePose(JsonElement source, IWorkshopInstance instance, double tolerance = .001)
    {
        var position = source.GetProperty("position");
        Assert.InRange(Math.Abs((instance.Cell.X + (double)instance.Local.X) / 16 - position[0].GetDouble()), 0, tolerance);
        Assert.InRange(Math.Abs((instance.Cell.Y + (double)instance.Local.Y) / 16 - position[1].GetDouble()), 0, tolerance);
        Assert.InRange(Math.Abs((instance.Cell.Z + (double)instance.Local.Z) / 16 - position[2].GetDouble()), 0, tolerance);
        var basis = new Basis(new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W).Normalized());
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
        Assert.Equal(3, data.GetArrayLength());
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
