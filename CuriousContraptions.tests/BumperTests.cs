using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BumperTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 0, -1)]
    public void RoundBumperLaunchesAwayFromContactInEveryAxis(int x, int y, int z)
    {
        var world = World();
        try
        {
            var normal = new Vector3(x, y, z);
            var center = new Vector3(0, 6, 0);
            var start = center + normal * 2;
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = "ball", Kind = "ball", Position = [start.X, start.Y, start.Z] },
                    new() { Id = "bumper", Kind = "bumper", Position = [0, 6, 0], Rotation = [30, 40, 20] }
                ]
            });
            world.Start();
            var ball = world.FindPart("ball")!;
            var bumper = (BumperPart)world.FindPart("bumper")!;
            ball.Velocity = -normal * 4;
            for (var tick = 0; tick < 100 && bumper.HitCount == 0; tick++) world.Step();
            Assert.Equal(1, bumper.HitCount);
            Assert.True(ball.Velocity.Dot(normal) > 7.99f);
            Assert.True(ball.Position.DistanceTo(center) >= ball.Radius + .65f);
            Assert.Contains(new MachineEvent(MachineEventKind.Bumped, "bumper", "ball"), world.Events.Keys);
            Assert.Empty(bumper.Boxes);
            Assert.Single(bumper.Spheres);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void GlancingHitPreservesTangentialMotionAndAnotherDepthPlaneMisses()
    {
        var world = World();
        try
        {
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = "ball", Kind = "ball", Position = [0, 4.98f, 0] },
                    new() { Id = "miss", Kind = "ball", Position = [0, 5, 2] },
                    new() { Id = "bumper", Kind = "bumper", Position = [0, 4, 0] }
                ]
            });
            world.Start();
            world.FindPart("ball")!.Velocity = new(2, -4, 0);
            world.FindPart("miss")!.Velocity = new(0, -4, 0);
            world.Step();
            var ball = world.FindPart("ball")!;
            Assert.InRange(ball.Velocity.X, 1.9f, 2.2f);
            Assert.True(ball.Velocity.Y > 7.9f);
            for (var i = 0; i < 90; i++) world.Step();
            Assert.DoesNotContain(new MachineEvent(MachineEventKind.Bumped, "bumper", "miss"), world.Events.Keys);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CooldownIsPerBallAndPulseFinishesAfterPhysicsStops()
    {
        var world = World();
        try
        {
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = "ball", Kind = "ball", Position = [0, 4.99f, 0] },
                    new() { Id = "other", Kind = "ball", Position = [1, 4, 0] },
                    new() { Id = "bumper", Kind = "bumper", Position = [0, 4, 0] }
                ]
            });
            world.Start();
            var bumper = (BumperPart)world.FindPart("bumper")!;
            var ball = world.FindPart("ball")!;
            bumper.OnContact(ball, 2, world);
            bumper.OnContact(ball, 2, world);
            Assert.Equal(1, bumper.HitCount);
            bumper._Process(.08);
            var existingPulse = bumper.PulseAmount;
            bumper.OnContact(world.FindPart("other")!, 2, world);
            bumper._Process(0);
            Assert.Equal(existingPulse, bumper.PulseAmount); // Retrigger has no pose discontinuity.
            Assert.Equal(2, bumper.HitCount);
            var shape = bumper.Spheres.Single();
            world.Running = false;
            var signature = world.StateSignature();
            bumper._Process(.16);
            Assert.InRange(bumper.PulseAmount, .99f, 1);
            Assert.True(bumper.GetNode<Node3D>("Visual/ImpactRing").Scale.X > 1.2f);
            bumper._Process(.2);
            Assert.Equal(0, bumper.PulseAmount);
            Assert.Equal(Vector3.One, bumper.GetNode<Node3D>("Visual/ImpactRing").Scale);
            Assert.Equal(shape, bumper.Spheres.Single());
            Assert.Equal(signature, world.StateSignature()); // Rendering never changes simulation.
            world.Running = true;
            for (var i = 0; i < 18; i++) world.Step();
            bumper.OnContact(ball, 2, world);
            Assert.Equal(3, bumper.HitCount);
            world.Restore();
            var restored = (BumperPart)world.FindPart("bumper")!;
            Assert.Equal(0, restored.HitCount);
            Assert.Equal(0, restored.PulseAmount);
            Assert.False(restored.Active);
            Assert.Equal(Vector3.One, restored.GetNode<Node3D>("Visual/ImpactRing").Scale);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(.45f, true)]
    [InlineData(1f, false)]
    public void PlacementNudgingAloneRescuesTheSameImperfectBumper(float precision, bool expectedWin)
    {
        var world = World();
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json"))
                .Single(p => p.Id == "bumper_sidekick");
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            data.Parts.AddRange(puzzle.Solution);
            data = MachineCodec.Clone(data);
            // Freeze capture and every other assistance rule at strict defaults.
            // Only the bumper's authored placement curve can differ between runs.
            foreach (var part in data.Parts.Where(p => p.Kind != "bumper")) part.Difficulty.Clear();
            var placed = data.Parts.Single(p => p.Kind == "bumper");
            placed.Position = [-3.1104352f, 1.4996231f, .03465762f];
            world.Precision = precision;
            world.LoadMachine(data);
            var initial = world.FindPart("bumper_1")!.Position;
            world.Start();
            Assert.Equal(initial, world.FindPart("bumper_1")!.Position);
            world.Step();
            Assert.InRange(initial.DistanceTo(world.FindPart("bumper_1")!.Position), 0, .001f);
            for (var i = 1; i < 3600 && world.Running; i++) world.Step();
            Assert.Equal(expectedWin, world.Won);
            Assert.Contains(new MachineEvent(MachineEventKind.Bumped, "bumper_1", "ball"), world.Events.Keys);
            Assert.Equal(9.81f, world.Gravity);
            if (precision == 1) Assert.Equal(initial, world.FindPart("bumper_1")!.Position);
            else Assert.True(world.FindPart("bumper_1")!.Position.DistanceTo(new(-3.2f, 1.5f, 0)) < .0001f);
            world.Restore();
            Assert.Equal(initial, world.FindPart("bumper_1")!.Position);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void StationaryOrSeparatingBodiesDoNotTriggerAndReplayIsIdentical()
    {
        var world = World();
        try
        {
            var machine = new MachineData
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = "ball", Kind = "ball", Position = [0, 4.98f, 0] },
                    new() { Id = "bumper", Kind = "bumper", Position = [0, 4, 0] }
                ]
            };
            foreach (var speed in new[] { 0f, 2f })
            {
                world.LoadMachine(machine);
                world.Start();
                world.FindPart("ball")!.Velocity = Vector3.Up * speed;
                world.Step();
                Assert.Equal(0, ((BumperPart)world.FindPart("bumper")!).HitCount);
            }
            world.LoadMachine(machine);
            world.Start();
            world.FindPart("ball")!.Velocity = Vector3.Down * 4;
            var signatures = new List<string>();
            for (var i = 0; i < 120; i++) { world.Step(); signatures.Add(world.StateSignature()); }
            world.Restore();
            world.Start();
            world.FindPart("ball")!.Velocity = Vector3.Down * 4;
            for (var i = 0; i < 120; i++) { world.Step(); Assert.Equal(signatures[i], world.StateSignature()); }
        }
        finally { world.Free(); }
    }
}
