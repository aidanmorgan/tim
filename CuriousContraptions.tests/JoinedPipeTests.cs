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

    private static WorkshopConstruction Lesson(float precision) =>
        // Revision 2: the production Simulation admits the first Construct after its initial revision 1.
        PuzzleCatalogue.Create(new("joined_pipe"), new(2), WorkshopCadenceSettings.Default(), BallId, new(precision));

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
        return WorkshopInput.Pipe(PipeId, (float)(position.X), (float)(position.Y), (float)(position.Z), (float)(q.X), (float)(q.Y), (float)(q.Z), (float)(q.W), new(length)) with { Locked = locked };
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
        run.Any(read => WorkshopGoalEvaluator.Occurrence(construction.Puzzle.Goal, read.Captures, read.Activations, construction.Instances.Identities) is not null);

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
        Assert.Equal(["pipe"], level.GetProperty("inventory").EnumerateArray().Select(entry => entry.GetProperty("part").GetString()!).ToArray());
        Assert.Equal(1, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "pipe").GetProperty("count").GetInt32());

        var construction = Lesson(0f);
        var puzzle = construction.Puzzle;
        Assert.Equal((new PuzzleId("joined_pipe"), PartId.Pipe, 1u), (puzzle.Id, puzzle.InventoryPart, puzzle.InventoryCount));
        var goal = level.GetProperty("goal");
        Assert.Equal(("Captured", "fixture_3", "fixture_1"), (goal.GetProperty("kind").GetString(), goal.GetProperty("target").GetString(), goal.GetProperty("source").GetString()));
        Assert.Equal((WorkshopGoalKind.Captured, BallId, ReceiverId), (puzzle.Goal.Kind, puzzle.Goal.Body, puzzle.Goal.Target));
        Assert.Equal(PartAllowance.Counted(1), Assert.Single(WorkshopInventoryPolicy.Authored(puzzle)).Value);
        Assert.Equal(PartId.Pipe, Assert.Single(WorkshopInventoryPolicy.Authored(puzzle)).Key);

        var parts = level.GetProperty("placements").EnumerateArray().ToDictionary(row => row.GetProperty("slot").GetString()!, row => row.GetProperty("instance"));
        Assert.Equal(["fixture_1", "fixture_2", "fixture_3"], parts.Keys.ToArray());
        Assert.Equal(3, construction.Instances.Count);
        var expected = new Dictionary<string, (IWorkshopInstance Instance, string Kind, AssistanceProfile Knots)>
        {
            ["fixture_1"] = (construction.Instances.Single(i => i.Id == BallId), "ball", puzzle.PrimaryAssistance),
            ["fixture_2"] = (construction.Instances.Single(i => i.Id == BendId), "pipe_bend_90", PuzzleCatalogue.Definition(new("joined_pipe")).Primary),
            ["fixture_3"] = (construction.Instances.Single(i => i.Id == ReceiverId), "basket", puzzle.TargetAssistance),
        };
        foreach (var (name, (instance, kind, knots)) in expected)
        {
            var source = parts[name];
            Assert.Equal(kind, source.GetProperty("part").GetString());
            Assert.Equal(kind == "basket" ? PartId.Receiver : kind == "ball" ? PartId.Basketball : PartId.PipeBend90, instance.PartId);
            Assert.True(source.GetProperty("locked").GetBoolean() && instance.Locked, $"{name} is a locked fixture");
            AuthoredContentAssertions.Placement(source, instance);
            AuthoredContentAssertions.Profile(source.GetProperty("assistance"), knots);
        }
        // The fixed bend is the ball's knots exactly (the content repeats them).
        Assert.Equal(puzzle.PrimaryAssistance, PuzzleCatalogue.Definition(new("joined_pipe")).Primary);

        var solution = Assert.Single(level.GetProperty("solution").EnumerateArray()).GetProperty("instance");
        Assert.Equal(("pipe", false), (solution.GetProperty("part").GetString(), solution.GetProperty("locked").GetBoolean()));
        Assert.Equal(2, solution.GetProperty("scalars").GetProperty("Length").GetDouble());
        AuthoredContentAssertions.Profile(solution.GetProperty("assistance"), puzzle.PlacementAssistance);
        Assert.Empty(level.GetProperty("solution_connections").EnumerateArray());
    }

    [Fact]
    public void SnappedAndAuthoredPipesLeaveRoomForTheBallAtBothMouths()
    {
        var lesson = Lesson(1f);
        var authored = PuzzleCatalogue.CreateSolution(new("joined_pipe"), new(2), WorkshopCadenceSettings.Default(), BallId, new(1));
        var definitions = Definitions();
        var ball = lesson.Instances.Single(row => row.Id == BallId);
        var ballShapes = definitions[ball.PartId].Colliders;
        // The bounding radius includes the basketball's raised seam, not just its spherical body.
        var seam = ballShapes.Single(shape => shape.ShapeKind == ColliderShapeKind.Torus);
        var ballRadius = MathF.Max(ballShapes.Single(shape => shape.ShapeKind == ColliderShapeKind.Sphere).Radius,
            seam.Radius + seam.HalfExtents.Y);
        var bend = lesson.Instances.Single(row => row.Id == BendId);
        var bendMouth = definitions[bend.PartId].Mouths(null).Values.Single(row => row.Mouth == TubeMouthEnd.Start);
        static Vector3 Position(IWorkshopInstance part) => new((part.Cell.X + part.Local.X) / 16,
            (part.Cell.Y + part.Local.Y) / 16, (part.Cell.Z + part.Local.Z) / 16);
        static Quaternion Rotation(IWorkshopInstance part) => new(part.Rotation.X, part.Rotation.Y, part.Rotation.Z, part.Rotation.W);
        var joint = Position(bend) + Rotation(bend) * bendMouth.Position;
        var intoBend = -(Rotation(bend) * bendMouth.Rotation * Vector3.Right);
        foreach (var pipe in new[] { Snapped(lesson, 2), Assert.IsType<WorkshopPipe>(authored.Instances.Single(row => row.Id == PipeId)) })
        {
            var mouths = definitions[pipe.PartId].Mouths(pipe.Length).Values;
            var lower = mouths.Single(row => row.Mouth == TubeMouthEnd.End);
            var upper = mouths.Single(row => row.Mouth == TubeMouthEnd.Start);
            var lowerAt = Position(pipe) + Rotation(pipe) * lower.Position;
            var upperAt = Position(pipe) + Rotation(pipe) * upper.Position;
            var direction = Rotation(pipe) * lower.Rotation * Vector3.Right;
            var clearance = MathF.Min(lower.BoreRadius!.Value, bendMouth.BoreRadius!.Value) - ballRadius;
            Assert.True(clearance > 0);
            // Centre mismatch plus axis displacement over one ball diameter must fit inside the available bore clearance.
            Assert.True(lowerAt.DistanceTo(joint) + 2 * ballRadius * (direction - intoBend).Length() < clearance);
            Assert.True(upperAt.Y > lowerAt.Y && Position(ball).Y > upperAt.Y);
            // Intersect the actual vertical drop line with the mouth plane, then allow the ball's projected footprint.
            var entryNormal = Rotation(pipe) * upper.Rotation * Vector3.Right;
            Assert.True(entryNormal.Y > 0);
            var drop = Position(ball);
            drop.Y = upperAt.Y - ((drop.X - upperAt.X) * entryNormal.X + (drop.Z - upperAt.Z) * entryNormal.Z) / entryNormal.Y;
            Assert.True(drop.DistanceTo(upperAt) + ballRadius / entryNormal.Y < upper.BoreRadius!.Value,
                "The falling ball's footprint must fit through the upper mouth.");
        }
    }

    // ---------- Validation ----------

    [Fact]
    public void ValidateAdmitsTheGrantedPipeOnly()
    {
        var lesson = Lesson(1f);
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
            () => placed.WithInstance(WorkshopInput.Generic(PartId.Ramp, new(5), 3, 1, 0, 0, 0, 0, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported,
            () => placed.WithInstance(WorkshopInput.PipeBend45(new(5), 3, 3, 0, 0, 0, 0, 1)).Validate());
    }

    [Fact]
    public void ALockedFixedBendIsRequired()
    {
        var lesson = Lesson(0f);
        var bend = (WorkshopGenericInstance)lesson.Instances.Single(instance => instance.Id == BendId);
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => (lesson with { Instances = lesson.Instances.Without(BendId) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { Cell = new(bend.Cell.X + 1, bend.Cell.Y, bend.Cell.Z) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { Rotation = CanonicalRotation.Identity }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(bend with { PartId = PartId.PipeBend45 }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(Payloads.First(lesson) with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(((WorkshopGenericInstance)lesson.Instances.Single(instance => instance.PartId == PartId.Receiver)) with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (lesson with { Puzzle = lesson.Puzzle with { InventoryCount = 2 } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (lesson with { Puzzle = lesson.Puzzle with { PlacementAssistance = PuzzleCatalogue.Definition(new("first_principles")).Placement } }).Validate());
    }

    // Story 9A.44: between knots the Receiver takes the f32 linear blend of its two bracketing authored knots, each operation rounded
    // once to f32 (t = (p - a)/(b - a), then x + (y - x)·t), and the blend lies strictly between them.
    [Theory]
    [InlineData(.2f)]
    [InlineData(.7f)]
    public void AnInterpolatedPrecisionSetsTheReceiverToTheF32BlendOfItsBracketingKnots(float precision)
    {
        var template = PuzzleCatalogue.Definition(new("joined_pipe")).Placements.Select(placement => placement.Instance)
            .Single(instance => instance.Part == PartId.Receiver);
        var (forgiving, balanced, precise) = (template.Assistance.Forgiving, template.Assistance.Balanced, template.Assistance.Precise);
        var (a, b) = precision < balanced.Precision ? (forgiving, balanced) : (balanced, precise);
        var t = (precision - a.Precision) / (b.Precision - a.Precision);
        float Blend(float x, float y) => x + (y - x) * t;
        var receiver = (WorkshopGenericInstance)Lesson(precision).Instances.Single(instance => instance.PartId == PartId.Receiver);
        var lanes = new (string Parameter, AssistanceValue Knot, float A, float B)[]
        {
            ("capture_margin", AssistanceValue.CaptureMargin, a.CaptureMargin.Value, b.CaptureMargin.Value),
            ("capture_speed_limit", AssistanceValue.CaptureSpeed, a.CaptureSpeed.Value, b.CaptureSpeed.Value),
            ("capture_dwell", AssistanceValue.CaptureDwell, a.CaptureDwell.Value, b.CaptureDwell.Value),
            ("force_support_margin", AssistanceValue.CaptureMargin, a.CaptureMargin.Value, b.CaptureMargin.Value),
            ("force_maximum_acceleration", AssistanceValue.GuideAcceleration, a.GuideAcceleration.Value, b.GuideAcceleration.Value),
        };
        // Each lane is the template's own binding (parameter ← knot lane, force_support_margin ← CaptureMargin among them), and
        // every one of its five bindings takes its blend.
        var bound = template.AssistanceBindings.Select(binding => (binding.Parameter.Value, binding.Value)).ToHashSet();
        Assert.Equal(template.AssistanceBindings.Length, lanes.Length);
        Assert.True(bound.SetEquals(lanes.Select(lane => (lane.Parameter, lane.Knot))), "the lanes are the template's own bindings");
        foreach (var (parameter, _, x, y) in lanes)
        {
            var actual = receiver.Parameters[new(parameter)];
            Assert.Equal(BitConverter.SingleToUInt32Bits(Blend(x, y)), BitConverter.SingleToUInt32Bits(actual));
            Assert.NotEqual(x, y);
            Assert.True(actual > MathF.Min(x, y) && actual < MathF.Max(x, y), $"{parameter} {actual} strictly between {x} and {y}");
        }
    }

    [Theory]
    [InlineData(0, .3, 3.0, .15, 12)]
    [InlineData(.45, .174, 2.325, .24, 6.6)]
    [InlineData(1.0, .02, 1.5, .35, 0)]
    public void PrecisionEvaluatesTheReceiverAndTheLessonRoundTripsTheSaveCodec(double precision, double margin, double speed, double dwell, double accel)
    {
        var lesson = Lesson((float)precision);
        var receiver = ((WorkshopGenericInstance)lesson.Instances.Single(instance => instance.PartId == PartId.Receiver));
        Assert.Equal(((float)margin, (float)speed, (float)dwell, (float)accel),
            (receiver.Parameters[new("capture_margin")], receiver.Parameters[new("capture_speed_limit")], receiver.Parameters[new("capture_dwell")], receiver.Parameters[new("force_maximum_acceleration")]));
        var placed = lesson.WithInstance(Snapped(lesson, 2));
        var saved = new WorkshopSavedConstruction(placed, new(5));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
        var updated = PuzzleCatalogue.WithPrecision(placed, new(.45f));
        Assert.Equal(.174f, ((WorkshopGenericInstance)updated.Instances.Single(instance => instance.PartId == PartId.Receiver)).Parameters[new("capture_margin")]);
        Assert.Equal(placed.Instances.Single(i => i.Id == PipeId), updated.Instances.Single(i => i.Id == PipeId));
    }

    // ---------- The route on the production Simulation, at Precise ----------

    [Fact]
    public async Task TheSnappedTwoMetrePipeDeliversTheBallAndResetRestoresTheBench()
    {
        var lesson = Lesson(1f);
        var bench = lesson.WithInstance(Snapped(lesson, 2));
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, RunTicks);
        Assert.True(Solved(bench, run), $"the joined route solves (ball last at {Ball(run[^1])})");
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
    }

    [Fact]
    public async Task TheDefaultLengthSnappedLeavesTheBallOnTheOuterShell()
    {
        var lesson = Lesson(1f);
        var bench = lesson.WithInstance(Snapped(lesson, 3.6f));
        var (_, run, _) = await WorkshopIntegrationHarness.ObserveReset(bench, RunTicks);
        Assert.False(Solved(bench, run), $"a 3.6 m pipe misses the drop line (ball last at {Ball(run[^1])})");
    }

    [Fact]
    public async Task AnUnsnappedPipeLeavesTheGoalUnsolved()
    {
        var lesson = Lesson(1f);
        var snapped = Snapped(lesson, 2);
        var unsnapped = PipeAt(UnsnappedPosition(snapped), Rz(-45), 2);
        var definitions = Definitions();
        Assert.Null(TubeMouthSnap.Find(definitions, lesson.Instances, new(PartId.Pipe, PipeId, false, unsnapped.Length),
            UnsnappedPosition(snapped), Rz(-45)));
        var bench = lesson.WithInstance(unsnapped);
        var (_, run, _) = await WorkshopIntegrationHarness.ObserveReset(bench, RunTicks);
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




}
