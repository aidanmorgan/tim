using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ConveyorTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(90f)]
    public void BeltTransportsAlongItsLocalAxis(float yaw)
    {
        var world = World();
        try
        {
            var direction = new Basis(Vector3.Up, Mathf.DegToRad(yaw)).X;
            var start = Vector3.Up * 2.5f - direction * .8f;
            world.LoadMachine(new()
            {
                Parts =
                [
                    new() { Id = "ball", Kind = "ball", Position = [start.X, start.Y, start.Z] },
                    new() { Id = "belt", Kind = "conveyor", Position = [0, 1, 0], Rotation = [0, yaw, 0] }
                ]
            });
            world.Start();
            for (var tick = 0; tick < 180; tick++) world.Step();
            Assert.Contains("transported:belt:ball", world.Events.Keys);
            Assert.True((world.FindPart("ball")!.Position - start).Dot(direction) > 1);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void UnpoweredBeltWaitsForActivation()
    {
        var world = World();
        try
        {
            world.LoadMachine(new()
            {
                Parts =
                [
                    new() { Id = "ball", Kind = "ball", Position = [-.8f, 2.5f, 0] },
                    new() { Id = "belt", Kind = "conveyor", Position = [0, 1, 0], Properties = new() { ["powered"] = 0 } },
                    new() { Id = "switch", Kind = "switch", Position = [5, 1, 0] }
                ],
                Connections = [new() { From = "switch", To = "belt" }]
            });
            world.Start();
            for (var tick = 0; tick < 180; tick++) world.Step();
            Assert.Equal(-.8f, world.FindPart("ball")!.Position.X, 3);
            Assert.DoesNotContain("transported:belt:ball", world.Events.Keys);
            world.Activate(world.FindPart("switch")!);
            for (var tick = 0; tick < 180; tick++) world.Step();
            Assert.Contains("transported:belt:ball", world.Events.Keys);
            Assert.True(world.FindPart("ball")!.Position.X > .5f);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ABodyInAnotherDepthPlaneIsNotTransported()
    {
        var world = World();
        try
        {
            world.LoadMachine(new()
            {
                Parts =
                [
                    new() { Id = "ball", Kind = "ball", Position = [0, 2.5f, 2] },
                    new() { Id = "belt", Kind = "conveyor", Position = [0, 1, 0] }
                ]
            });
            world.Start();
            for (var tick = 0; tick < 120; tick++) world.Step();
            Assert.DoesNotContain("transported:belt:ball", world.Events.Keys);
            Assert.Equal(0, world.FindPart("ball")!.Position.X);
        }
        finally { world.Free(); }
    }
}

