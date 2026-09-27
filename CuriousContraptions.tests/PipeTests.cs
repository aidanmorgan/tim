using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PipeTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(30, 40, 50)]
    [InlineData(0, 90, 90)]
    public void OpenBoreAllowsContinuousTravelAndOuterWallRejectsEntry(float x, float y, float z)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [new() { Id = "pipe", Kind = "pipe", Position = [0, 8, 0], Rotation = [x, y, z] },
                    new() { Id = "ball", Kind = "ball", Position = [0, 12, 0] }]
            });
            var pipe = world.FindPart("pipe")!;
            var ball = world.FindPart("ball")!;
            ball.Position = pipe.Transform * new Vector3(-3, .1f, 0);
            world.Start();
            ball.Velocity = pipe.Basis.X * 4;
            var previous = ball.Position;
            for (var i = 0; i < 180; i++)
            {
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous) < .04f);
                previous = ball.Position;
            }
            var local = pipe.Transform.AffineInverse() * ball.Position;
            Assert.InRange(local.X, 2.99f, 3.01f);
            Assert.InRange(local.Y, .09f, .11f);
            Assert.InRange(ball.Velocity.Length(), 3.99f, 4.01f);
            world.Restore();
            pipe = world.FindPart("pipe")!;
            ball = world.FindPart("ball")!;
            ball.Position = pipe.Transform * new Vector3(0, 2, 0);
            world.Start();
            ball.Velocity = -pipe.Basis.Y * 8;
            for (var i = 0; i < 60; i++) world.Step();
            local = pipe.Transform.AffineInverse() * ball.Position;
            Assert.True(local.Y >= .70f + ball.Radius - .001f);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(.34f, 40f, false)]
    [InlineData(.8f, 4f, true)]
    public void FastBallsPassTheBoreButOversizedBallsHitTheMouth(float radius, float speed, bool blocked)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [new() { Id = "pipe", Kind = "pipe", Position = [0, 6, 0] },
                    new() { Id = "ball", Kind = "ball", Position = [-3, 6, 0], Properties = new() { ["radius"] = radius } }]
            });
            world.Start();
            var ball = world.FindPart("ball")!;
            ball.Velocity = Vector3.Right * speed;
            for (var i = 0; i < 24; i++) world.Step();
            if (blocked) Assert.True(ball.Position.X < -1.89f);
            else Assert.InRange(ball.Position.X, 4.99f, 5.01f);
            Assert.True(ball.Velocity.Length() <= speed + .001f);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CollarsOccludeLightButTheBoreAndClearShellDoNot()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var pipe = world.AddPart(new() { Id = "pipe", Kind = "pipe", Position = [0, 4, 0] });
            var emitter = world.AddPart(new() { Id = "torch", Kind = "flashlight", Position = [-6, 4, 0] });
            Assert.Equal(10, LightNetwork.Trace(world, new(-5, 4, 0), Vector3.Right, 10, emitter));
            Assert.InRange(LightNetwork.Trace(world, new(-5, 4.7f, 0), Vector3.Right, 10, emitter), 3.10f, 3.12f);
            Assert.Equal(5, LightNetwork.Trace(world, new(0, 4, -2), Vector3.Back, 5, emitter));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(.45f, 0f)]
    [InlineData(1f, 0f)]
    [InlineData(0f, .48f)]
    [InlineData(.45f, .48f)]
    [InlineData(1f, .48f)]
    public void AuthoredTubeRouteAndPlacementAssistance(float precision, float depthError)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == "clear_pipe");
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            data.Parts.AddRange(puzzle.Solution);
            data = MachineCodec.Clone(data);
            // Isolate the pipe's placement curve from basket acceptance/guide forces.
            foreach (var part in data.Parts.Where(p => p.Kind != "pipe")) part.Difficulty.Clear();
            data.Parts.Single(p => p.Kind == "pipe").Position[2] += depthError;
            world.Precision = precision;
            world.LoadMachine(data);
            void Run()
            {
                world.Start();
                for (var i = 0; i < 1200 && world.Running; i++) world.Step();
            }
            Run();
            var expected = depthError == 0 || precision < 1;
            Assert.Equal(expected, world.Won);
            var signature = world.StateSignature();
            world.Restore();
            Run();
            Assert.Equal(signature, world.StateSignature());
            data.Parts.RemoveAll(p => p.Kind == "pipe");
            world.LoadMachine(data);
            Run();
            Assert.False(world.Won);
        }
        finally { world.Free(); }
    }
}
