using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PoweredGateTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        world.AddPart(new() { Id = "gate", Kind = "powered_gate", Position = [0, 8, 0] });
        world.AddPart(new() { Id = "battery", Kind = "battery", Position = [5, 8, 0] });
        return world;
    }

    [Fact]
    public void ElectricalSupplyOpensSmoothlyAndResetCloses()
    {
        var world = World();
        try
        {
            var gate = (PoweredGatePart)world.FindPart("gate")!;
            var timer = world.AddPart(new() { Id = "timer", Kind = "delay", Position = [8, 8, 0] });
            Assert.False(world.Connect(timer, gate));
            Assert.True(world.Connect(world.FindPart("battery")!, gate));
            Assert.Equal(ConnectionDomain.Electrical, Assert.Single(world.Connections).Type);
            world.Start();
            var previous = 0f;
            for (var i = 0; i < 180; i++)
            {
                world.Step();
                Assert.InRange(gate.Opening - previous, 0, 2.8f * MachineWorld.Tick + .00001f);
                Assert.InRange(gate.BladeSpeed, 0, 2.8f);
                previous = gate.Opening;
            }
            Assert.Equal(GateState.Open, gate.State);
            Assert.Equal(PoweredGatePart.Stroke, gate.Opening);
            world.Restore();
            gate = (PoweredGatePart)world.FindPart("gate")!;
            Assert.Equal(GateState.Closed, gate.State);
            Assert.Equal(0, gate.Opening);
            Assert.Equal(0, gate.BladeSpeed);
            Assert.Single(world.Connections);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false, 0, 0, 0)]
    [InlineData(true, 0, 0, 0)]
    [InlineData(false, 20, 30, 40)]
    [InlineData(true, 20, 30, 40)]
    public void OnlyOpenGateAllowsBallThrough(bool powered, float x, float y, float z)
    {
        var world = World();
        try
        {
            var gate = (PoweredGatePart)world.FindPart("gate")!;
            gate.RotationDegrees = new(x, y, z);
            if (powered) Assert.True(world.Connect(world.FindPart("battery")!, gate));
            world.Start();
            for (var i = 0; i < 180; i++) world.Step();
            var ball = world.AddPart(new() { Id = "ball", Kind = "ball", Position = [0, 12, 0] });
            ball.Position = gate.Transform * new Vector3(-2, 0, 0);
            ball.Velocity = gate.Basis.X * 4;
            for (var i = 0; i < 120; i++) world.Step();
            var local = gate.Transform.AffineInverse() * ball.Position;
            if (powered) Assert.InRange(local.X, 1.99f, 2.01f);
            else Assert.True(local.X < -.39f);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ClosingStopsAtObstructionAndResumesWithoutMovingBall()
    {
        var world = World();
        try
        {
            var gate = (PoweredGatePart)world.FindPart("gate")!;
            Assert.True(world.Connect(world.FindPart("battery")!, gate));
            world.Start();
            for (var i = 0; i < 180; i++) world.Step();
            var ball = world.AddPart(new() { Id = "ball", Kind = "ball", Position = [0, 8, 0] });
            var origin = ball.Position;
            world.Connections.Clear();
            for (var i = 0; i < 180; i++) world.Step();
            Assert.Equal(GateState.Blocked, gate.State);
            Assert.True(gate.Opening > 0);
            Assert.True(ball.Position.DistanceTo(origin) < .0001f);
            Assert.Equal(0, gate.BladeSpeed);
            ball.Position = new(-4, 8, 0);
            for (var i = 0; i < 180; i++) world.Step();
            Assert.Equal(GateState.Closed, gate.State);
            Assert.Equal(0, gate.Opening);
        }
        finally { world.Free(); }
    }
}
