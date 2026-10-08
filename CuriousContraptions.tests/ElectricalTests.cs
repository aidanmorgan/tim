using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ElectricalTests(NativeSceneFixture godot)
{
    public enum Lesson { BatteryMotor, SwitchedMotor }
    private enum Fixture { Battery, Motor, FirstSwitch, SecondSwitch }
    private enum SupplyParameter { Enabled }
    private const string PuzzleResource="res://content/puzzles.json";
    private static string LessonId(Lesson lesson)=>lesson switch
    {
        Lesson.BatteryMotor=>"battery_motor",Lesson.SwitchedMotor=>"switched_motor",
        _=>throw new ArgumentOutOfRangeException(nameof(lesson))
    };
    private static string Id(Fixture fixture)=>fixture switch
    {
        Fixture.Battery=>"battery",Fixture.Motor=>"motor",
        Fixture.FirstSwitch=>"switch_1",Fixture.SecondSwitch=>"switch_2",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Battery=>"battery",Fixture.Motor=>"motor",
        Fixture.FirstSwitch or Fixture.SecondSwitch=>"switch",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 position,bool reverseIds=false)
    {
        var id=reverseIds?fixture switch
        {
            Fixture.Battery=>"z_supply",Fixture.Motor=>"a_motor",
            Fixture.FirstSwitch or Fixture.SecondSwitch=>Id(fixture),
            _=>throw new ArgumentOutOfRangeException(nameof(fixture))
        }:Id(fixture);
        return world.AddPart(new(){Id=id,Kind=Kind(fixture),Position=[position.X,position.Y,position.Z]});
    }

    [Theory]
    [InlineData(Lesson.BatteryMotor, 0f)]
    [InlineData(Lesson.BatteryMotor, .45f)]
    [InlineData(Lesson.BatteryMotor, 1f)]
    [InlineData(Lesson.SwitchedMotor, 0f)]
    [InlineData(Lesson.SwitchedMotor, .45f)]
    [InlineData(Lesson.SwitchedMotor, 1f)]
    public void PowerLessonsRequireTheirWiresAndReplayAfterReset(Lesson lesson, float precision)
    {
        var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(PuzzleResource)).Single(p => p.Id == LessonId(lesson));
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
            Assert.Equal(0, ((MotorPart)world.FindPart(Id(Fixture.Motor))!).ShaftTravel);
            world.Start();
            for (var i = 0; i < 600 && world.Running; i++) world.Step();
            Assert.True(world.Won);
            Assert.Equal(ticks, world.Ticks);
            if (lesson == Lesson.SwitchedMotor)
            {
                var bypass = MachineCodec.Clone(data);
                bypass.Connections = [new() { From = Id(Fixture.Battery), To = Id(Fixture.Motor), Type = ConnectionDomain.Electrical,
                    FromPort = SocketId.Supply, ToPort = SocketId.PowerIn }];
                world.LoadMachine(bypass);
                world.Start();
                for (var i = 0; i < 600; i++) world.Step();
                Assert.True(world.Events.ContainsKey(new MachineEvent(MachineEventKind.Turned, Id(Fixture.Motor))));
                Assert.True(world.Events.ContainsKey(new MachineEvent(MachineEventKind.Activated, Id(Fixture.FirstSwitch))));
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
                Assert.False(world.FindPart(Id(Fixture.Motor))!.HasElectricalPower(SocketId.PowerIn));
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
            var battery=Add(world,Fixture.Battery,new(0,3,0));
            FixtureParts.ConfigureParameter(battery,SupplyParameter.Enabled,enabled?1:0);
            var motor=(MotorPart)Add(world,Fixture.Motor,new(3,3,0));
            if (connected)
            {
                Assert.True(world.Connect(battery, motor));
                var link = Assert.Single(world.Connections);
                Assert.Equal(ConnectionDomain.Electrical, link.Type);
                Assert.Equal(SocketId.Supply, link.FromPort);
                Assert.Equal(SocketId.PowerIn, link.ToPort);
            }
            Assert.False(world.Connect(motor, battery));
            world.Start();
            world.Activate(battery); // A command must not create supply from a disabled source.
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(connected && enabled, motor.Active);
            Assert.Equal(connected && enabled, motor.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(connected && enabled, motor.ShaftSpeed > 0);
            Assert.Equal(connected && enabled, world.Events.ContainsKey(new MachineEvent(MachineEventKind.Powered, Id(Fixture.Motor))));
            world.Restore();
            motor = (MotorPart)world.FindPart(Id(Fixture.Motor))!;
            Assert.False(motor.Active);
            Assert.False(motor.HasElectricalPower(SocketId.PowerIn));
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
            var battery=Add(world,Fixture.Battery,new(-6,3,0),reverseIds);
            var first=Add(world,Fixture.FirstSwitch,new(-3,3,0),reverseIds);
            var second=Add(world,Fixture.SecondSwitch,new(0,3,0),reverseIds);
            var motor=(MotorPart)Add(world,Fixture.Motor,new(3,3,0),reverseIds);
            var supply=new SupplyControl(world,battery);
            (MachinePart Source,MachinePart Target)[] links=[(supply.Output,first),(first,second),(second,first),(second,motor)];
            foreach(var link in reverseIds?links.Reverse():links)
                Assert.True(world.Connect(link.Source,link.Target));
            world.Start();
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(first.HasElectricalPower(SocketId.PowerIn));
            Assert.False(motor.Active);
            world.Activate(first);
            world.Step();
            Assert.True(second.HasElectricalPower(SocketId.PowerIn));
            Assert.False(motor.Active);
            world.Activate(second);
            world.Step();
            Assert.True(motor.Active);
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(motor.Active);
            Assert.False(first.HasElectricalPower(SocketId.PowerIn));
            Assert.False(second.HasElectricalPower(SocketId.PowerIn));
            supply.SetAndSettle(SimulationLatchPhase.On);
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
    public void SupplyLossClearsPowerWithoutDeletingShaftMomentum()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var battery=Add(world,Fixture.Battery,new(-3,3,0));
            var motor=(MotorPart)Add(world,Fixture.Motor,new(3,3,0));
            var trigger=Add(world,Fixture.FirstSwitch,new(0,3,0));
            Assert.True(world.Connect(trigger, motor)); // Closed contact still needs upstream supply.
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,motor));
            world.Start();
            supply.SetAndSettle(SimulationLatchPhase.On);
            for (var i = 0; i < 120; i++) world.Step();
            var speed = motor.ShaftSpeed;
            var work=motor.SuppliedWork;
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(motor.Active);
            Assert.InRange(Math.Abs(motor.ShaftSpeed-speed),0,1e-6);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.InRange(Math.Abs(motor.ShaftSpeed-speed),0,1e-6);
            Assert.Equal(work,motor.SuppliedWork);
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(motor.Active);
            Assert.True(motor.ShaftSpeed > 0);
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(motor.HasElectricalPower(SocketId.PowerIn));
            Assert.False(motor.Active);
        }
        finally { world.Free(); }
    }
}
