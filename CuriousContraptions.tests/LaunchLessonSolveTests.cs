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

    public enum Route
    {
        SpringPassive, SpringCharged, SpringZero, SpringAway,
        SidekickPaid, SidekickUnpaid, DepthPaid, DepthMiss, WallPaid, WallMiss, ReadyReceiver, ReadyMissingReceiver
    }

    internal static WorkshopConstruction Bench(Route route)
    {
        // cat-062b: Precise; passive board −20° about Z; charged/zero/away board under the ball, −12.82° about Z, away turned
        // world-Y 180°.
        const double Qz = -.11166252195835114, Qw = .9937461614608765;
        WorkshopConstruction Lesson(string id, double precision) => PuzzleCatalogue.Create(new PuzzleId(id), new(2),
            WorkshopCadenceSettings.Default(), new(1), new((float)precision));
        return route switch
        {
            Route.SpringPassive => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player, -3, .8f, 0,
                0, 0, (float)(-Math.Sin(Math.PI / 18)), (float)(Math.Cos(Math.PI / 18)))),
            Route.SpringCharged => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player,
                -3.315333366394043f, 3.1000001430511475f, 0, 0, 0, (float)(Qz), (float)(Qw), compression: .2f)),
            Route.SpringZero => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player,
                -3.315333366394043f, 3.1000001430511475f, 0, 0, 0, (float)(Qz), (float)(Qw), compression: 0f)),
            Route.SpringAway => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player,
                -3.315333366394043f, 3.1000001430511475f, 0, -.11166255176067352f, .9937461614608765f, 7.4505797087454084e-9f,
                2.980232594040899e-8f, compression: .2f)),
            Route.SidekickPaid => Lesson("bumper_sidekick", .45).WithInstance(PlacedParts.Bumper(Player, -3.35f, 1.5f, 0)),
            Route.SidekickUnpaid => Lesson("bumper_sidekick", .45).WithInstance(PlacedParts.Bumper(Player, -3.35f, 1.5f, 0, strength: 0f)),
            Route.DepthPaid => Lesson("bumper_depth", .45).WithInstance(PlacedParts.Bumper(Player, 0, 1.5f, 3.35f)),
            Route.DepthMiss => Lesson("bumper_depth", .45).WithInstance(PlacedParts.Bumper(Player, 0, 1.5f, 0)),
            Route.WallPaid => Lesson("wall_and_bumper", .45).WithInstance(PlacedParts.Bumper(Player, -3.35f, 1.5f, 0))
                .WithInstance(WorkshopInput.Generic(new PartId("wall"), new(4), -1.75f, 4, 0, 0, 0, 0, 1,
                    new([new(new("width"), .4f), new(new("height"), 6f), new(new("thickness"), 1.5f)]))),
            Route.WallMiss => Lesson("wall_and_bumper", .45).WithInstance(PlacedParts.Bumper(Player, -3.35f, 1.5f, 0)),
            Route.ReadyReceiver => Lesson("ready_to_rebound", .45).WithInstance(WorkshopInput.Generic(new PartId("basket"), Player, 1.8f, 1.3f, 0, 0, 0, 0, 1)),
            Route.ReadyMissingReceiver => Lesson("ready_to_rebound", .45),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
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
        var positive = PuzzleCatalogue.CreateSolution(new(lesson), new(2), settings, new(1), new(.45f));
        // One connection or one granted part is necessary; fixtures and all other inputs stay.
        var control = positive.Connections.Count > 0
            ? positive with { Connections = positive.Connections.Without(positive.Connections[0]) }
            : positive.WithoutInstance((new PuzzleId(lesson) == new PuzzleId("wall_and_bumper")
                ? positive.Instances.Single(instance => instance.PartId == new PartId("wall"))
                : positive.Instances.First(instance => !instance.Locked)).Id);
        control.Validate();
        var limit = 10 * (int)settings.SimulationRate.Numerator;
        foreach (var solves in new[] { true, false })
        {
            var bench = solves ? positive : control;
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
            await harness.Advance((int)settings.SimulationRate.Numerator);
            AssertGoalCue(harness, lit: solves);
            if (solves && bench.Puzzle.Goal.Kind == WorkshopGoalKind.Captured)
                Assert.Contains(harness.Records, row => row.Kind == SimulationEventKind.Captured &&
                    row.Source == bench.Puzzle.Goal.Target.Value && row.Other == bench.Puzzle.Goal.Body.Value);
            if (solves && bench.Puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter)
            {
                var goal = bench.Puzzle.Goal;
                var activations = Enumerable.Range(0, harness.Committed.Activations.Count).Select(i => harness.Committed.Activations[i]).ToArray();
                var source = Assert.Single(activations, row => row.Node == goal.SourceNode);
                var target = Assert.Single(activations, row => row.Node == goal.TargetNode);
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
            if (!solves) continue;
            await harness.Require(WorkshopCommandKind.Save);
            var saved = new WorkshopSavedConstruction(bench, new(bench.Instances.Max(row => row.Id.Value) + 1));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
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
    }

    private static void AssertGoalCue(WorkshopIntegrationHarness harness, bool lit)
    {
        var goal = harness.Construction.Puzzle.Goal;
        var capture = goal.Kind == WorkshopGoalKind.Captured;
        var owner = capture ? goal.Target : new GpuBodyId(goal.TargetNode.Value);
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
            var bench = PuzzleCatalogue.WithPrecision(initial, new(precision));
            bench.Validate();
            var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, RunTicks);
            Assert.True(solves == Solved(bench, run), $"{route} at precision {precision}: expected solved={solves}");
            NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        }
    }
}
