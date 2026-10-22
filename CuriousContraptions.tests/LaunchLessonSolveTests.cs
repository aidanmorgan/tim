using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Native solve-runs of the five launch lessons (Story 9R.8 suite-first guard, C-Bumper+Spring): the player's part is placed
/// through the lesson's production placement (`WithInstance`, which applies the granted row's precision configuration), the
/// construction runs on the production Simulation over the worker transport, and the goal is judged by
/// <see cref="WorkshopGoalEvaluator"/>. Poses and precisions are the ones the Chrome suites proved (cat-062b, cat-015a-sidekick,
/// cat-015b, cat-062b Ready); every route has a matched control that must stay unsolved, and Reset restores the bench exactly.</summary>
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
            WorkshopCadenceSettings.Default(), new(1), new((Half)precision));
        return route switch
        {
            Route.SpringPassive => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player, -3, .8, 0,
                0, 0, -Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18))),
            Route.SpringCharged => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player,
                -3.315333366394043, 3.1000001430511475, 0, 0, 0, Qz, Qw, compression: .2f)),
            Route.SpringZero => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player,
                -3.315333366394043, 3.1000001430511475, 0, 0, 0, Qz, Qw, compression: 0f)),
            Route.SpringAway => Lesson("spring_forward", 1).WithInstance(PlacedParts.Springboard(Player,
                -3.315333366394043, 3.1000001430511475, 0, -.11166255176067352, .9937461614608765, 7.4505797087454084e-9,
                2.980232594040899e-8, compression: .2f)),
            Route.SidekickPaid => Lesson("bumper_sidekick", .45).WithInstance(PlacedParts.Bumper(Player, -3.3, 1, 0)),
            Route.SidekickUnpaid => Lesson("bumper_sidekick", .45).WithInstance(PlacedParts.Bumper(Player, -3.3, 1, 0, strength: 0f)),
            Route.DepthPaid => Lesson("bumper_depth", .45).WithInstance(PlacedParts.Bumper(Player, 0, 1, 3.3)),
            Route.DepthMiss => Lesson("bumper_depth", .45).WithInstance(PlacedParts.Bumper(Player, 0, 1, 0)),
            Route.WallPaid => Lesson("wall_and_bumper", .45).WithInstance(PlacedParts.Bumper(Player, -3.3, 1, 0))
                .WithInstance(WorkshopInput.Generic(PartId.Wall, new(4), -1, 4, 0, 0, 0, 0, 1,
                    new([new(new("width"), .4f), new(new("height"), 6f), new(new("thickness"), 1.5f)]))),
            Route.WallMiss => Lesson("wall_and_bumper", .45).WithInstance(PlacedParts.Bumper(Player, -3.3, 1, 0)),
            Route.ReadyReceiver => Lesson("ready_to_rebound", .45).WithInstance(WorkshopInput.Generic(PartId.Receiver, Player, 1.8, 1.3, 0, 0, 0, 0, 1)),
            Route.ReadyMissingReceiver => Lesson("ready_to_rebound", .45),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
    }

    internal static bool Solved(WorkshopConstruction construction, IEnumerable<WorkshopRead> run) =>
        run.Any(read => WorkshopGoalEvaluator.Occurrence(construction.Puzzle.Goal, read.Captures, read.Activations) is not null);

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
        var bench = Bench(route);
        bench.Validate();
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
        Assert.Equal(solves, Solved(bench, run));
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
    }
}
