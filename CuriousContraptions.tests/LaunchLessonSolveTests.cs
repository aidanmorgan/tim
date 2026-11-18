using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Native solve-runs of the five launch lessons (Story 9R.8 suite-first guard, C-Bumper+Spring): the player's part is placed
/// through the lesson's production placement (`WithInstance`, which applies the granted row's precision configuration), the
/// construction runs on the production Simulation over the worker transport, and the goal is judged by
/// <see cref="WorkshopGoalEvaluator"/>. Bumper poses follow the solid-cap authored solutions; every route has a matched
/// control that must stay unsolved, and Reset restores the bench exactly. Browser qualification remains pending.</summary>
public sealed class LaunchLessonSolveTests
{
    private const int RunTicks = 1200;
    private static readonly GpuBodyId Player = new(3);
    internal const float SpringPlacementAllowance = .001f; // Existing initial authoring-position allowance, not a motion tolerance.

    public enum Route
    {
        SpringPassive, SpringCharged, SpringZero, SpringAway,
        SidekickPaid, SidekickUnpaid, DepthPaid, DepthMiss, WallPaid, WallMiss, ReadyReceiver, ReadyMissingReceiver
    }

    internal static WorkshopConstruction Bench(Route route)
    {
        IWorkshopInstance ChargedSpring(float compression, bool away = false)
        {
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 4);
            if (away) rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI) * rotation;
            var normal = Vector3.Transform(Vector3.UnitY, rotation);
            // Full payload envelope includes the physical band: .14 anchor + .075 half plate + .35 envelope + .002 clearance.
            // Move the base with compression so the initial plate and payload geometry match.
            var position = new Vector3(-3, 3.6f, 0) - (.567f - compression) * normal;
            return PlacedParts.Springboard(Player, position.X, position.Y, position.Z,
                rotation.X, rotation.Y, rotation.Z, rotation.W, compression, stiffness: 400, damping: .2f);
        }
        WorkshopConstruction Lesson(string id, double precision) => PuzzleCatalogue.Create(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new PuzzleId(id), new(2),
            WorkshopCadenceSettings.Default(), new(1), new((float)precision));
        return route switch
        {
            Route.SpringPassive => Lesson("spring_forward", 1).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Springboard(Player, -3, .8f, 0,
                0, 0, (float)(-Math.Sin(Math.PI / 18)), (float)(Math.Cos(Math.PI / 18)))),
            Route.SpringCharged => Lesson("spring_forward", 1).WithInstance(RuntimeTestServices.Bundle, ChargedSpring(.2f)),
            Route.SpringZero => Lesson("spring_forward", 1).WithInstance(RuntimeTestServices.Bundle, ChargedSpring(0)),
            Route.SpringAway => Lesson("spring_forward", 1).WithInstance(RuntimeTestServices.Bundle, ChargedSpring(.2f, away: true)),
            Route.SidekickPaid => Lesson("bumper_sidekick", .45).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Bumper(Player, -3.35f, 1.5f, 0)),
            Route.SidekickUnpaid => Lesson("bumper_sidekick", .45).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Bumper(Player, -3.35f, 1.5f, 0, strength: 0f)),
            Route.DepthPaid => Lesson("bumper_depth", .45).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Bumper(Player, 0, 1.5f, 3.35f)),
            Route.DepthMiss => Lesson("bumper_depth", .45).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Bumper(Player, 0, 1.5f, 0)),
            Route.WallPaid => Lesson("wall_and_bumper", .45).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Bumper(Player, -3.35f, 1.5f, 0))
                .WithInstance(RuntimeTestServices.Bundle, WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("wall"), new(4), -1.75f, 4, 0, 0, 0, 0, 1,
                    new([new(new("width"), .4f), new(new("height"), 6f), new(new("thickness"), 1.5f)]))),
            Route.WallMiss => Lesson("wall_and_bumper", .45).WithInstance(RuntimeTestServices.Bundle, PlacedParts.Bumper(Player, -3.35f, 1.5f, 0)),
            Route.ReadyReceiver => Lesson("ready_to_rebound", .45).WithInstance(RuntimeTestServices.Bundle, WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("basket"), Player, 1.8f, 1.3f, 0, 0, 0, 0, 1)),
            Route.ReadyMissingReceiver => Lesson("ready_to_rebound", .45),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
    }

    internal static Vector3 AssertSpringLoading(WorkshopConstruction construction, WorkshopRead built)
    {
        var board = Assert.IsType<WorkshopGenericInstance>(construction.Instances.Single(instance => instance.PartId == new PartId("spring")));
        var rotation = board.Rotation;
        var normal = Vector3.Transform(Vector3.UnitY, new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
        var bodies = Enumerable.Range(0, built.Bodies.Count).Select(index => built.Bodies[index].Body).ToArray();
        var ball = Assert.Single(bodies, body => body.Id == new GpuBodyId(1)); // Fixed lesson payload.
        var plate = Assert.Single(bodies, body => body.Id != ball.Id);
        static Vector3 Position(CanonicalBody body) => new((body.Cell.X + body.Local.X) / 16f,
            (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
        var relative = Position(plate) - Position(ball);
        // Plate half-thickness .075 + full payload envelope .350 + authored clearance .002.
        Assert.InRange(Vector3.Distance(relative, -.427f * normal), 0f, SpringPlacementAllowance);
        var gap = -Vector3.Dot(relative, normal) - .075f - .35f;
        // Nonoverlap and contact before the charged stroke ends; the goal assertion proves useful transfer.
        Assert.True(gap >= 0 && gap < .2f, $"initial plate/payload clearance {gap}");
        return relative;
    }

    internal static bool Solved(WorkshopConstruction construction, IEnumerable<WorkshopRead> run) =>
        run.Any(read => WorkshopGoalEvaluator.Occurrence(construction.Puzzle.Goal, read.Captures, read.Activations, construction.Instances.Identities) is not null);

    // The authored solution of every placement-launch lesson solves at every precision (the bumper rows were corrected from x/z 3.2 to
    // 3.3 on 22 Oct 2026: at 3.2 the 0.2 m offset under the falling ball misses the Receiver on main 55ee05b9 and here alike,
    // evidence-c-bumper-spring/lesson-pose/). domino_effect's rows were corrected on 23 Oct 2026 (9A.32): the legacy tiles at
    // x = -2.4…0, y = 1 lay 2 m from the falling Basketball and the last overlapped the end tile, so the chain never started; the four
    // tiles now stand at x = -4…-1 on the bench and the end tile is wired to the lamp (evidence-9a-32/candidate/domino-effect-solution/).
    // The restitution slice (24 Oct 2026) extends the guard to every Playable lesson whose content has an authored solution.
    internal static readonly PuzzleId[] Lessons =
    [
        new("first_principles"), new("spring_forward"), new("domino_effect"), new("bumper_sidekick"),
        new("bumper_depth"), new("wall_and_bumper"), new("delayed_signal"), new("clear_pipe"),
        new("gentle_bend"), new("quarter_bend"), new("joined_pipe"), new("ready_to_rebound")
    ];

    [Fact]
    public void EveryPlayableLessonHasAnExplicitExecutedScenario() =>
        Assert.Equal(PuzzleCatalogue.Playable.OrderBy(id => id.Value), Lessons.OrderBy(id => id.Value));

    public static TheoryData<string, SimulationCadence> LessonCadences()
    {
        var data = new TheoryData<string, SimulationCadence>();
        foreach (var lesson in Lessons)
            foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz120, SimulationCadence.Hz240 })
                data.Add(lesson.Value, cadence);
        return data;
    }

    [Theory]
    [MemberData(nameof(LessonCadences))]
    public async Task AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal(string lesson, SimulationCadence cadence)
    {
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        var positive = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(lesson), new(2), settings, new(1), new(.45f));
        // One connection or one granted part is necessary; fixtures and all other inputs stay.
        var control = positive.Connections.Count > 0
            ? positive with { Connections = positive.Connections.Without(positive.Connections[0]) }
            : positive.WithoutInstance((new PuzzleId(lesson) == new PuzzleId("wall_and_bumper")
                ? positive.Instances.Single(instance => instance.PartId == new PartId("wall"))
                : positive.Instances.First(instance => !instance.Locked)).Id);
        control.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle);
        var limit = 10 * (int)settings.SimulationRate.Numerator;
        foreach (var solves in new[] { true, false })
            await AssertLessonScenario(solves ? positive : control, solves, lesson, limit);
    }

    private static async Task AssertLessonScenario(WorkshopConstruction bench, bool solves, string lesson, int limit)
    {
        await using var harness = await WorkshopIntegrationHarness.Building(bench);
        var built = harness.Committed;
        AssertGoalCue(harness, lit: false);
        await harness.Run();
        await harness.Advance(1);
        Assert.Contains(Enumerable.Range(0, harness.Committed.Bodies.Count).Select(i => harness.Committed.Bodies[i]),
            body => body.Body.Velocity.X != 0 || body.Body.Velocity.Y != 0 || body.Body.Velocity.Z != 0);
        var pausedAt = harness.Committed.Tick;
        await harness.Require(WorkshopCommandKind.Pause);
        Assert.Equal(pausedAt, harness.Committed.Tick);
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(pausedAt.Value + 1, harness.Committed.Tick.Value);
        await harness.Require(WorkshopCommandKind.Resume);
        var solved = Solved(bench, [harness.Committed]);
        for (var tick = 0; tick < limit && !solved; tick++)
        {
            await harness.Advance(1);
            solved = Solved(bench, [harness.Committed]);
        }
        Assert.Equal(solves, solved);
        // Let the declared half-second capture halo or activation bulb reach a plainly visible outcome.
        await harness.Advance((int)bench.Settings.SimulationRate.Numerator);
        AssertGoalCue(harness, lit: solves);
        if (solves && bench.Puzzle.Goal.Kind == WorkshopGoalKind.Captured)
            Assert.Contains(harness.Records, row => row.Kind == SimulationEventKind.Captured &&
                row.Source.Value == bench.Puzzle.Goal.Target.Value && row.Other == bench.Puzzle.Goal.Body.Value);
        if (solves && bench.Puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter)
        {
            var goal = bench.Puzzle.Goal;
            var activations = Enumerable.Range(0, harness.Committed.Activations.Count).Select(i => harness.Committed.Activations[i]).ToArray();
            var source = Assert.Single(activations, row => row.Node == bench.Instances.Identities[goal.Body].Signals[goal.SourceNode].Node);
            var target = Assert.Single(activations, row => row.Node == bench.Instances.Identities[goal.Target].Signals[goal.TargetNode].Node);
            var delayed = new PuzzleId(lesson) == new PuzzleId("delayed_signal");
            Assert.Equal(delayed ? ActivationOccurrenceKind.Contact : ActivationOccurrenceKind.Orientation, source.Kind);
            Assert.Equal(delayed ? ActivationOccurrenceKind.TimerElapsed : ActivationOccurrenceKind.Orientation, target.Kind);
            Assert.Equal(source.Occurrence.Cause, target.Occurrence.Cause);
            Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(source.Occurrence.Time, target.Occurrence.Time, goal.MinimumDuration));
        }
        Assert.Equal(WorkshopRejection.WrongPhase, (await harness.Execute(WorkshopCommandKind.Save)).Result.Reason);
        await harness.Reset();
        NoFixedCapacityTests.AssertSameState(built, harness.Committed, lesson + " Reset");
        AssertGoalCue(harness, lit: false);
        Assert.Equal(bench, harness.Construction);
        if (solves) await AssertSavedLessonReplay(harness, bench, lesson, limit);
    }

    private static async Task AssertSavedLessonReplay(WorkshopIntegrationHarness harness, WorkshopConstruction bench, string lesson, int limit)
    {
        await harness.Require(WorkshopCommandKind.Save);
        var saved = new WorkshopSavedConstruction(bench, new(bench.Instances.Max(row => row.Id.Value) + 1));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, loaded);
        await using var fresh = await WorkshopIntegrationHarness.Running(loaded.Construction);
        var replaySolved = false;
        for (var tick = 0; tick < limit && !replaySolved; tick++)
        {
            await fresh.Advance(1);
            replaySolved = Solved(loaded.Construction, [fresh.Committed]);
        }
        Assert.True(replaySolved, lesson + " fresh Load solves");
    }

    private static void AssertGoalCue(WorkshopIntegrationHarness harness, bool lit)
    {
        var goal = harness.Construction.Puzzle.Goal;
        var capture = goal.Kind == WorkshopGoalKind.Captured;
        var owner = goal.Target;
        var samples = harness.Presentation!.Advance(harness.Schedule, PhysicalTime.FromNanoseconds(harness.Time.Nanoseconds));
        if (!harness.Construction.Instances.Any(part => part.Id == owner))
        {
            Assert.False(lit);
            Assert.DoesNotContain(samples, sample => sample.Owner == owner);
            return;
        }
        // Receiver halo red is binding 1 (neutral mint); lamp bulb red is binding 0 (neutral dark grey).
        var cue = Assert.Single(samples, sample => sample.Owner == owner && sample.Binding == (capture ? 1 : 0));
        var red = cue.Value.ColourChannel.Value;
        if (lit) Assert.True(red > .9f, "The captured receiver halo / activated lamp must visibly brighten.");
        else Assert.InRange(red, capture ? .7f : .3f, capture ? .8f : .4f);
    }

    private static string DescribeLaunch(List<WorkshopRead> run) => string.Join("; ",
        run.Where((_, index) => index < 12 || index % 30 == 0 && index < 240 || index == run.Count - 1).Select(read =>
            $"tick {read.Tick.Value}: " + string.Join(" / ", Enumerable.Range(0, read.Bodies.Count).Select(index =>
            {
                var body = read.Bodies[index].Body;
                return $"{body.Id.Value} p=({(body.Cell.X + body.Local.X) / 16f},{(body.Cell.Y + body.Local.Y) / 16f},{(body.Cell.Z + body.Local.Z) / 16f}) v={body.Velocity}";
            }))));

    [Theory]
    [InlineData(Route.SpringPassive, true)]
    [InlineData(Route.SpringCharged, true)]
    [InlineData(Route.SpringZero, false)]
    [InlineData(Route.SpringAway, false)]
    [InlineData(Route.SidekickPaid, true)]
    [InlineData(Route.SidekickUnpaid, false)]
    [InlineData(Route.DepthPaid, true)]
    [InlineData(Route.DepthMiss, false)]
    [InlineData(Route.WallPaid, true)]
    [InlineData(Route.WallMiss, false)]
    [InlineData(Route.ReadyReceiver, true)]
    [InlineData(Route.ReadyMissingReceiver, false)]
    public async Task EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench(Route route, bool solves)
    {
        var initial = Bench(route);
        var precisions = route is Route.SidekickPaid or Route.SidekickUnpaid or Route.DepthPaid or Route.DepthMiss or Route.WallPaid or Route.WallMiss
            ? new[] { 0f, .45f, 1f } : new[] { initial.Puzzle.Precision.Value };
        foreach (var precision in precisions)
        {
            var bench = PuzzleCatalogue.WithPrecision(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, initial, new(precision));
            bench.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle);
            var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, RunTicks);
            if (route is Route.SpringCharged or Route.SpringZero or Route.SpringAway) AssertSpringLoading(bench, built);
            Assert.True(solves == Solved(bench, run), $"{route} at precision {precision}: expected solved={solves}; {DescribeLaunch(run)}");
            NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        }
    }
}
