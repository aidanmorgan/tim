using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ElectricalTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData("battery_motor", 0f)]
    [InlineData("battery_motor", .45f)]
    [InlineData("battery_motor", 1f)]
    [InlineData("switched_motor", 0f)]
    [InlineData("switched_motor", .45f)]
    [InlineData("switched_motor", 1f)]
    public void PowerLessonsRequireTheirWiresAndReplayAfterReset(string id, float precision)
    {
        var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
        var data = MachineCodec.Clone(puzzle.CreateMachine());
        data.Parts.AddRange(puzzle.Solution);
        data.Connections = puzzle.SolutionConnections;
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(data);
            world.Start();
            for (var i = 0; i < 600 && world.Running; i++) world.Step();
            Assert.True(world.Won);
            var ticks = world.Ticks;
            world.Restore();
            Assert.Equal(0, ((MotorPart)world.FindPart("motor")!).ShaftTravel);
            world.Start();
            for (var i = 0; i < 600 && world.Running; i++) world.Step();
            Assert.True(world.Won);
            Assert.Equal(ticks, world.Ticks);
            if (id == "switched_motor")
            {
                var bypass = MachineCodec.Clone(data);
                bypass.Connections = [new() { From = "battery", To = "motor", Type = ConnectionDomain.Electrical,
                    FromPort = SocketIds.Supply, ToPort = SocketIds.PowerIn }];
                world.LoadMachine(bypass);
                world.Start();
                for (var i = 0; i < 600; i++) world.Step();
                Assert.True(world.Events.ContainsKey(new MachineEvent(MachineEventKind.Turned, "motor")));
                Assert.True(world.Events.ContainsKey(new MachineEvent(MachineEventKind.Activated, "switch_1")));
                Assert.False(world.Won); // Activation and turning alone do not prove correct sequencing.
            }
            for (var omitted = 0; omitted < data.Connections.Count; omitted++)
            {
                var broken = MachineCodec.Clone(data);
                broken.Connections.RemoveAt(omitted);
                world.LoadMachine(broken);
                world.Start();
                for (var i = 0; i < 600 && world.Running; i++) world.Step();
                Assert.False(world.Won);
                Assert.False(world.FindPart("motor")!.HasElectricalPower(SocketIds.PowerIn));
            }
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MotorNeedsConnectedEnabledSupply(bool connected, bool enabled)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var battery = world.AddPart(new() { Id = "battery", Kind = "battery",
                Properties = new() { ["enabled"] = enabled ? 1 : 0 } });
            var motor = (MotorPart)world.AddPart(new() { Id = "motor", Kind = "motor", Position = [3, 1, 0] });
            if (connected)
            {
                Assert.True(world.Connect(battery, motor));
                var link = Assert.Single(world.Connections);
                Assert.Equal(ConnectionDomain.Electrical, link.Type);
                Assert.Equal(SocketIds.Supply, link.FromPort);
                Assert.Equal(SocketIds.PowerIn, link.ToPort);
            }
            Assert.False(world.Connect(motor, battery));
            world.Start();
            world.Activate(battery); // A command must not create supply from a disabled source.
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(connected && enabled, motor.Active);
            Assert.Equal(connected && enabled, motor.HasElectricalPower(SocketIds.PowerIn));
            Assert.Equal(connected && enabled, motor.ShaftSpeed > 0);
            Assert.Equal(connected && enabled, world.Events.ContainsKey(new MachineEvent(MachineEventKind.Powered, "motor")));
            world.Restore();
            motor = (MotorPart)world.FindPart("motor")!;
            Assert.False(motor.Active);
            Assert.False(motor.HasElectricalPower(SocketIds.PowerIn));
            Assert.Equal(0, motor.ShaftAngle);
            Assert.Equal(0, motor.ShaftSpeed);
            Assert.Equal(connected ? 1 : 0, world.Connections.Count);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosedRelayChainsPropagateWithoutOrderDependenceAndLoopsNeedSupply(bool reverseIds)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var battery = world.AddPart(new() { Id = reverseIds ? "z" : "a", Kind = "battery" });
            var first = world.AddPart(new() { Id = "c", Kind = "switch" });
            var second = world.AddPart(new() { Id = "d", Kind = "switch" });
            var motor = (MotorPart)world.AddPart(new() { Id = reverseIds ? "a" : "z", Kind = "motor" });
            Assert.True(world.Connect(battery, first));
            Assert.True(world.Connect(first, second));
            Assert.True(world.Connect(second, first)); // A cycle must terminate without generating supply.
            Assert.True(world.Connect(second, motor));
            if (reverseIds) world.Connections.Reverse();
            world.Start();
            world.Step();
            Assert.True(first.HasElectricalPower(SocketIds.PowerIn));
            Assert.False(motor.Active);
            world.Activate(first);
            world.Step();
            Assert.True(second.HasElectricalPower(SocketIds.PowerIn));
            Assert.False(motor.Active);
            world.Activate(second);
            world.Step();
            Assert.True(motor.Active);
            battery.Properties["enabled"] = 0;
            world.Step();
            Assert.False(motor.Active);
            Assert.False(first.HasElectricalPower(SocketIds.PowerIn));
            Assert.False(second.HasElectricalPower(SocketIds.PowerIn));
            battery.Properties["enabled"] = 1;
            world.Step();
            Assert.True(motor.Active);
            second.Active = false;
            world.Step();
            Assert.False(motor.Active);
            world.Restore();
            Assert.All(world.Parts, p => Assert.False(p.Active));
            world.Start();
            world.Step();
            Assert.False(world.FindPart(motor.Uid)!.Active);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void SupplyLossClearsPowerAndShaftCoastsToRest()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var battery = world.AddPart(new() { Id = "battery", Kind = "battery" });
            var motor = (MotorPart)world.AddPart(new() { Id = "motor", Kind = "motor" });
            var trigger = world.AddPart(new() { Id = "switch", Kind = "switch" });
            Assert.True(world.Connect(trigger, motor)); // Closed contact still needs upstream supply.
            Assert.True(world.Connect(battery, motor));
            world.Start();
            for (var i = 0; i < 120; i++) world.Step();
            var speed = motor.ShaftSpeed;
            battery.Properties["enabled"] = 0; // Native source-loss fixture, not a browser driver.
            world.Step();
            Assert.False(motor.Active);
            Assert.InRange(motor.ShaftSpeed, .01f, speed);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(0, motor.ShaftSpeed);
            battery.Properties["enabled"] = 1;
            world.Step();
            Assert.True(motor.Active);
            Assert.True(motor.ShaftSpeed > 0);
            world.Connections.Clear();
            world.Step();
            Assert.False(motor.HasElectricalPower(SocketIds.PowerIn));
            Assert.False(motor.Active);
        }
        finally { world.Free(); }
    }
}
