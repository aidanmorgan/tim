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

    /// <summary>The lesson's authored `solution` rows (content/puzzles.json), each part placed at its authored pose through the lesson's
    /// production placement. The orientation is the Basis column-major array the content stores (as JoinedPipeTests reads it).</summary>
    private static readonly string[] WallKeys = ["width", "height", "thickness"];

    internal static WorkshopConstruction AuthoredSolution(string lesson, Half precision)
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(value => value.GetProperty("id").GetString() == lesson);
        var construction = PuzzleCatalogue.Create(new PuzzleId(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(precision));
        // Each authored placement slot names the fixture built from it, in placement order; solution parts follow the fixtures.
        var slots = level.GetProperty("placements").EnumerateArray().Select((row, index) =>
        {
            var fixture = construction.Instances[index];
            Assert.Equal(row.GetProperty("instance").GetProperty("part").GetString(), fixture.PartId.Value);
            return KeyValuePair.Create(row.GetProperty("slot").GetString()!, fixture.Id);
        }).ToDictionary();
        // delayed_signal's wiring names a fixture by its part ("switch", "lamp"); each such name is unique among its placements.
        foreach (var named in level.GetProperty("placements").EnumerateArray().Select((row, index) =>
                (Part: row.GetProperty("instance").GetProperty("part").GetString()!, construction.Instances[index].Id))
            .GroupBy(row => row.Part).Where(group => group.Count() == 1))
            slots.TryAdd(named.Key, named.Single().Id);
        var next = construction.Instances.Max(instance => instance.Id.Value) + 1;
        foreach (var row in level.GetProperty("solution").EnumerateArray())
        {
            var p = row.GetProperty("position").EnumerateArray().Select(value => value.GetDouble()).ToArray();
            var o = row.GetProperty("orientation").EnumerateArray().Select(value => (float)value.GetDouble()).ToArray();
            var q = new Godot.Basis(new Godot.Vector3(o[0], o[1], o[2]), new Godot.Vector3(o[3], o[4], o[5]), new Godot.Vector3(o[6], o[7], o[8]))
                .GetRotationQuaternion();
            var id = new GpuBodyId(next++);
            var properties = row.GetProperty("properties");
            IWorkshopInstance part = row.GetProperty("kind").GetString() switch
            {
                "bumper" => PlacedParts.Bumper(id, p[0], p[1], p[2], qx: q.X, qy: q.Y, qz: q.Z, qw: q.W),
                "spring" => PlacedParts.Springboard(id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "wall" => WorkshopInput.Generic(PartId.Wall, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W, new(
                    WallKeys.Select(key => KeyValuePair.Create(new ParameterId(key), (float)properties.GetProperty(key).GetDouble())))),
                "domino" => PlacedParts.Domino(id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "delay" => WorkshopInput.Generic(PartId.Delay, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                var kind => throw new ArgumentException($"No authored-solution placement for '{kind}'.")
            };
            slots.Add(row.GetProperty("id").GetString()!, id);
            construction = construction.WithInstance(part);
        }
        // The authored solution wiring: activation links between named slots.
        var links = new List<WorkshopConnection>();
        foreach (var row in level.GetProperty("solution_connections").EnumerateArray())
        {
            Assert.Equal(("activation", "activation_out", "activation_in"), (row.GetProperty("type").GetString(),
                row.GetProperty("from_port").GetString(), row.GetProperty("to_port").GetString()));
            links.Add(new(slots[row.GetProperty("from").GetString()!], WorkshopSocket.ActivationOut, slots[row.GetProperty("to").GetString()!],
                WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation));
        }
        return links.Count == 0 ? construction : construction with { Connections = new([.. links]) };
    }

    // The authored solution of every placement-launch lesson solves at every precision (the bumper rows were corrected from x/z 3.2 to
    // 3.3 on 22 Oct 2026: at 3.2 the 0.2 m offset under the falling ball misses the Receiver on main 55ee05b9 and here alike,
    // evidence-c-bumper-spring/lesson-pose/). domino_effect's rows were corrected on 23 Oct 2026 (9A.32): the legacy tiles at
    // x = -2.4…0, y = 1 lay 2 m from the falling Basketball and the last overlapped the end tile, so the chain never started; the four
    // tiles now stand at x = -4…-1 on the bench and the end tile is wired to the lamp (evidence-9a-32/candidate/domino-effect-solution/).
    [Theory]
    [InlineData("spring_forward")]
    [InlineData("bumper_sidekick")]
    [InlineData("bumper_depth")]
    [InlineData("wall_and_bumper")]
    [InlineData("domino_effect")]
    public async Task TheAuthoredSolutionSolvesAtEveryPrecision(string lesson)
    {
        foreach (var precision in new[] { (Half)0, (Half).45, (Half)1 })
        {
            var bench = AuthoredSolution(lesson, precision);
            bench.Validate();
            var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
            Assert.True(Solved(bench, run), $"{lesson} at precision {(float)precision}");
            NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        }
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
        var bench = Bench(route);
        bench.Validate();
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
        Assert.Equal(solves, Solved(bench, run));
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
    }
}
