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
            WorkshopCadenceSettings.Default(), new(1), new((float)precision));
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

    internal static WorkshopConstruction AuthoredSolution(string lesson, float precision)
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
        // Content wiring may name a fixture by its part ("switch", "lamp") when that part is unique among the fixtures.
        foreach (var group in construction.Instances.GroupBy(instance => instance.PartId.Value).Where(group => group.Count() == 1))
            slots.TryAdd(group.Key, group.Single().Id);
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
                "ramp" => WorkshopInput.Generic(PartId.Ramp, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "delay" => WorkshopInput.Generic(PartId.Delay, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "basket" => WorkshopInput.Generic(PartId.Receiver, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "pipe_bend_45" => WorkshopInput.Generic(PartId.PipeBend45, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "pipe_bend_90" => WorkshopInput.Generic(PartId.PipeBend90, id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W),
                "pipe" => WorkshopInput.Pipe(id, p[0], p[1], p[2], q.X, q.Y, q.Z, q.W, new((float)properties.GetProperty("length").GetDouble())),
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
    // The restitution slice (24 Oct 2026) extends the guard to every Playable lesson whose content has an authored solution.
    public static TheoryData<string> AuthoredLessons()
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var authored = source.RootElement.EnumerateArray()
            .Where(level => level.TryGetProperty("solution", out var rows) && rows.GetArrayLength() > 0)
            .Select(level => level.GetProperty("id").GetString()!).ToHashSet();
        var data = new TheoryData<string>();
        foreach (var lesson in PuzzleCatalogue.Playable.Where(id => authored.Contains(id.Value))) data.Add(lesson.Value);
        return data;
    }

    [Fact]
    public void EveryPlayableLessonHasAnAuthoredSolutionUnderTheGuard() =>
        Assert.Equal(PuzzleCatalogue.Playable.Count, AuthoredLessons().Count);

    [Theory]
    [MemberData(nameof(AuthoredLessons))]
    public async Task TheAuthoredSolutionSolvesAtEveryPrecision(string lesson)
    {
        foreach (var precision in new[] { 0f, .45f, 1f })
        {
            var bench = AuthoredSolution(lesson, precision);
            bench.Validate();
            var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, RunTicks);
            Assert.True(Solved(bench, run), $"{lesson} at precision {precision}");
            NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
            // Save/load (Story 9A.44): the lesson's f32 puzzle record and its nine f32 assistance knots round-trip exactly, and the
            // reloaded bench replays the same run to the same committed state and solves.
            var saved = new WorkshopSavedConstruction(bench, new(bench.Instances.Max(instance => instance.Id.Value) + 1));
            var bytes = WorkshopSaveCodec.Encode(saved);
            var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
            Assert.Equal(saved, reloaded);
            var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(reloaded.Construction, RunTicks);
            Assert.True(Solved(reloaded.Construction, replay), $"{lesson} reloaded at precision {precision}");
            NoFixedCapacityTests.AssertSameState(run[^1], replay[^1], $"{lesson} reloaded at precision {precision}");
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
