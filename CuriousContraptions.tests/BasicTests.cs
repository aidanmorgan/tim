using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;
using FileAccess = Godot.FileAccess;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BasicTests(HeadlessFixture godot, ITestOutputHelper output)
{
    private MachineWorld CreateWorld()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static List<PuzzleData> Puzzles() =>
        MachineCodec.ReadPuzzles(FileAccess.GetFileAsString("res://content/puzzles.json"));
    private static MachineData Solution(PuzzleData puzzle)
    {
        var machine = MachineCodec.Clone(puzzle.CreateMachine());
        machine.Parts.AddRange(puzzle.Solution);
        machine.Connections = puzzle.SolutionConnections;
        return MachineCodec.Clone(machine);
    }


    [Fact]
    public void MainSceneCreatesTheCSharpWorkshopAndControls()
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        try
        {
            Assert.Equal(2, scene.World.Parts.Count);
            var controls = scene.FindChildren("*", "Button", true, false);
            Assert.True(controls.Count >= 10);
            Assert.Contains(controls, node => node is Button button && button.HasMeta("action_label") && button.GetMeta("action_label").AsString().Contains("Run machine") && button.Icon != null);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void ForgivingPrecisionExpandsSuccessfulReceiverPlacementRegion()
    {
        var world = CreateWorld();
        var puzzle = Puzzles().Single(p => p.Id == "spring_forward");
        var precise = new HashSet<int>();
        var forgiving = new HashSet<int>();
        try
        {
            for (var offset = -16; offset <= 16; offset++)
            foreach (var precision in new[] { 0f, 1f })
            {
                world.Precision = precision;
                var data = Solution(puzzle);
                data.Parts.Single(p => p.Id == "receiver").Position[0] += offset * .1f;
                world.LoadMachine(data);
                world.Start();
                for (var tick = 0; tick < 1200 && world.Running; tick++) world.Step();
                if (world.Won) (precision == 0 ? forgiving : precise).Add(offset);
            }
            output.WriteLine($"Successful x-offsets: precise={string.Join(",", precise)}; forgiving={string.Join(",", forgiving)}");
            Assert.NotEmpty(precise);
            Assert.True(precise.IsSubsetOf(forgiving), "Assistance must preserve known precise solutions in this sweep.");
            Assert.True(forgiving.Count > precise.Count, "Lower precision must accept more placements in this sweep.");
        }
        finally { world.Free(); }
    }


    [Fact]
    public void CampaignContainsTheAuthoredExpansionStageAndLegalNonTrivialPuzzles()
    {
        var world = CreateWorld();
        try
        {
            var puzzles = Puzzles();
            Assert.Equal(52, puzzles.Count); // Expansion stage; the final target remains 75.
            Assert.Equal(puzzles.Count, puzzles.Select(p => p.Id).Distinct().Count());
            foreach (var puzzle in puzzles)
            {
                Assert.NotEmpty(puzzle.Title);
                Assert.NotEmpty(puzzle.Hint);
                Assert.NotEmpty(puzzle.Goals);
                Assert.NotEmpty(puzzle.Solution);
                Assert.All(puzzle.Parts, p => Assert.True(p.Locked, puzzle.Id + ": fixed parts must be locked"));
                Assert.All(puzzle.Solution, p =>
                {
                    Assert.False(p.Locked);
                    Assert.InRange(p.Position[0], -7, 7);
                    Assert.InRange(p.Position[1], 0, 9);
                    Assert.InRange(p.Position[2], -4, 4);
                });
                foreach (var group in puzzle.Solution.GroupBy(p => p.Kind))
                    Assert.True(puzzle.Inventory.GetValueOrDefault(group.Key) >= group.Count(), puzzle.Id + ": inventory exceeded");
                var data = Solution(puzzle);
                Assert.Equal(data.Parts.Count, data.Parts.Select(p => p.Id).Distinct().Count());
                world.LoadMachine(data);
                foreach (var link in data.Connections)
                {
                    Assert.True(world.IsValidConnection(link), puzzle.Id + ": invalid typed sockets");
                }
                world.LoadMachine(puzzle.CreateMachine());
                world.Start();
                for (var tick = 0; tick < 1200 && world.Running; tick++) world.Step();
                Assert.False(world.Won, puzzle.Id + ": untouched puzzle solves itself");
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CatalogInstantiatesEveryPartScene()
    {
        var world = CreateWorld();
        try
        {
            Assert.True(world.Registry.Definitions.Count >= 11);
            foreach (var key in world.Registry.Definitions.Keys)
            {
                var part = world.AddPart(new() { Id = key, Kind = key, Position = [0, 2, 0] });
                Assert.NotEmpty(part.GetChildren());
                Assert.True(part.Mass > 0);
            }
            Assert.Equal(world.Registry.Definitions.Count, world.Parts.Count);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.45f)]
    [InlineData(1f)]
    public void CampaignSolutionsReachTheirGoals(float precision)
    {
        var world = CreateWorld();
        var failures = new List<string>();
        try
        {
            world.Precision = precision;
            foreach (var puzzle in Puzzles())
            {
                world.LoadMachine(Solution(puzzle));
                world.Start();
                for (var i = 0; i < 3600 && world.Running; i++) world.Step();
                var body = world.Bodies.FirstOrDefault();
                output.WriteLine($"{puzzle.Id} precision={precision}: won={world.Won}; ticks={world.Ticks}; ball={body?.Position}; events={string.Join(",", world.Events.Keys)}");
                if (!world.Won) failures.Add(puzzle.Id);
            }
            Assert.True(failures.Count == 0, "Unsolved: " + string.Join(", ", failures));
        }
        finally { world.Free(); }
    }


    [Theory]
    [InlineData(0f)]
    [InlineData(.45f)]
    public void AirMailToleratesSmallFanPlacementErrorsBeforeAirflow(float precision)
    {
        var world = CreateWorld();
        var puzzle = Puzzles().Single(p => p.Id == "air_mail");
        try
        {
            world.Precision = precision;
            foreach (var heightError in new[] { -.05f, -.0158873f, -.004805f, 0f, .005f, .05f })
            {
                var data = Solution(puzzle);
                data.Parts.Single(p => p.Id == "fan_1").Position[1] += heightError;
                world.LoadMachine(data);
                world.Start();
                for (var tick = 0; tick < 1200 && world.Running; tick++) world.Step();
                Assert.True(world.Won, $"precision={precision}, fan height error={heightError}");
                world.Restore();
                Assert.InRange(Mathf.Abs(world.FindPart("fan_1")!.Position.Y - (3.8f + heightError)), 0, .00001f);
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RestoreReplaysAnIdenticalTrace()
    {
        var world = CreateWorld();
        try
        {
            world.LoadMachine(Solution(Puzzles()[0]));
            world.Start();
            var hashes = new List<string>();
            for (var tick = 0; tick < 600; tick++) { world.Step(); hashes.Add(world.StateSignature()); }
            world.Restore();
            world.Start();
            for (var tick = 0; tick < 600; tick++)
            {
                world.Step();
                Assert.Equal(hashes[tick], world.StateSignature());
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void DepthSeparatesCollisions()
    {
        var world = CreateWorld();
        var reference = CreateWorld();
        try
        {
            world.LoadMachine(new()
            {
                Parts =
                [
                    new() { Id = "ball", Kind = "ball", Position = [0, 3, 2] },
                    new() { Id = "ramp", Kind = "ramp", Position = [0, 1, -2] }
                ]
            });
            reference.LoadMachine(new()
            {
                Parts = [new() { Id = "ball", Kind = "ball", Position = [0, 3, 2] }]
            });
            world.Start();
            reference.Start();
            for (var tick = 0; tick < 120; tick++)
            {
                world.Step();
                reference.Step();
                Assert.Equal(reference.FindPart("ball")!.Position, world.FindPart("ball")!.Position);
                Assert.Equal(reference.FindPart("ball")!.Velocity, world.FindPart("ball")!.Velocity);
            }
        }
        finally { world.Free(); reference.Free(); }
    }

    [Fact]
    public void WrongBallDoesNotSatisfyGoal()
    {
        var world = CreateWorld();
        try
        {
            world.LoadMachine(new()
            {
                Parts =
                [
                    new() { Id = "decoy", Kind = "ball", Position = [0, 2, 0] },
                    new() { Id = "required", Kind = "ball", Position = [4, 2, 0] },
                    new() { Id = "receiver", Kind = "basket", Position = [0, .8f, 0] }
                ],
                Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "required" }]
            });
            world.Start();
            for (var tick = 0; tick < 600; tick++) world.Step();
            Assert.Contains(new MachineEvent(MachineEventKind.Captured, "receiver", "decoy"), world.Events.Keys);
            Assert.False(world.Won);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void PowerCyclesTerminateAndInvalidPortsAreRejected()
    {
        var world = CreateWorld();
        try
        {
            var a = world.AddPart(new() { Id = "a", Kind = "domino" });
            var b = world.AddPart(new() { Id = "b", Kind = "domino" });
            var ball = world.AddPart(new() { Id = "ball", Kind = "ball" });
            Assert.True(world.Connect(a, b));
            Assert.True(world.Connect(b, a));
            Assert.False(world.Connect(ball, a));
            world.Activate(a);
            Assert.True(a.Active && b.Active);
            Assert.False(ball.Active);
        }
        finally { world.Free(); }
    }
}
