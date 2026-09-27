using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class HoldTimerTests(HeadlessFixture godot)
{
    private MachineWorld World(float seconds = 2, bool reverse = false)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        var parts = new List<PartSpec>
        {
            new() { Id = "battery", Kind = "battery", Position = [-4,4,0] },
            new() { Id = "timer", Kind = "hold_timer", Position = [0,4,0],
                Properties = new() { [HoldTimerParameters.Seconds] = seconds } },
            new() { Id = "gate", Kind = "powered_gate", Position = [4,4,0] }
        };
        if (reverse) parts.Reverse();
        foreach (var part in parts) world.AddPart(part);
        Assert.True(world.Connect(world.FindPart("battery")!, world.FindPart("timer")!));
        Assert.True(world.Connect(world.FindPart("timer")!, world.FindPart("gate")!));
        return world;
    }

    [Theory]
    [InlineData(.1f, false)]
    [InlineData(.1f, true)]
    [InlineData(1.13f, false)]
    [InlineData(1.13f, true)]
    [InlineData(2f, false)]
    [InlineData(2f, true)]
    public void ContactExpiresBeforeNetworkSnapshotAndCanBeRetriggered(float seconds, bool reverse)
    {
        var world = World(seconds, reverse);
        try
        {
            var timer = (HoldTimerPart)world.FindPart("timer")!;
            var gate = (PoweredGatePart)world.FindPart("gate")!;
            world.Start();
            world.Step();
            Assert.False(gate.Active);
            world.Activate(timer);
            Assert.Equal(HoldTimerState.Holding, timer.State);
            var due = timer.DueTick;
            var remaining = timer.Remaining;
            while (world.Ticks < due)
            {
                world.Activate(timer);
                Assert.Equal(due, timer.DueTick);
                world.Step();
                Assert.True(gate.HasElectricalPower(SocketId.PowerIn));
                Assert.InRange(timer.Remaining, 0, remaining);
                remaining = timer.Remaining;
            }
            world.Step();
            Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            Assert.False(timer.Active);
            Assert.Equal(HoldTimerState.Ready, timer.State);
            world.Activate(timer);
            Assert.True(timer.DueTick > due);
            world.Step();
            Assert.True(gate.Active);
            world.Restore();
            timer = (HoldTimerPart)world.FindPart("timer")!;
            Assert.Equal(HoldTimerState.Ready, timer.State);
            Assert.Equal(-1, timer.DueTick);
            Assert.Equal(-1, timer.StartedTick);
            Assert.Equal(0, timer.Remaining);
            Assert.False(timer.Active);
            Assert.Equal(2, world.Connections.Count);
            Assert.Equal(seconds, timer.Duration);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void PowerLossDoesNotPauseCountdownOrCreateSupply()
    {
        var world = World();
        try
        {
            var timer = (HoldTimerPart)world.FindPart("timer")!;
            var gate = (PoweredGatePart)world.FindPart("gate")!;
            var battery = world.FindPart("battery")!;
            var sourceWire = world.Connections.Single(c => c.From == battery.Uid);
            world.Connections.Remove(sourceWire);
            world.Start();
            world.Activate(timer);
            var due = timer.DueTick;
            for (var i = 0; i < 30; i++) world.Step();
            Assert.False(gate.Active);
            Assert.Equal(HoldTimerState.Holding, timer.State);
            world.Connections.Add(sourceWire); // Native supply-restoration fixture; editor wiring remains locked during Run.
            world.Step();
            Assert.True(gate.Active);
            Assert.Equal(due,timer.DueTick);
            world.Connections.RemoveAll(c => c.From == battery.Uid);
            world.Step();
            Assert.False(gate.Active);
            while (world.Ticks <= due) world.Step();
            Assert.Equal(HoldTimerState.Ready,timer.State);
            world.Connections.Add(sourceWire); // Native supply-restoration fixture; editor wiring remains locked during Run.
            world.Step();
            Assert.False(gate.Active); // Restoring power does not replay a spent trigger.
        }
        finally { world.Free(); }
    }

    [Fact]
    public void DelayCanTriggerHoldAndHoldDrivesGateOnlyWithSupply()
    {
        var world = World(.5f);
        try
        {
            var timer = (HoldTimerPart)world.FindPart("timer")!;
            var delay = world.AddPart(new() { Id="delay", Kind="delay", Position=[-4,8,0],
                Properties=new() { [DelayParameters.Seconds]=.1f } });
            Assert.True(world.Connect(delay,timer));
            Assert.Equal(ConnectionDomain.Activation,world.Connections.Last().Type);
            Assert.False(world.Connect(timer,delay));
            world.Start();
            world.Activate(delay);
            for (var i=0;i<12;i++) world.Step();
            Assert.Equal(HoldTimerState.Ready,timer.State);
            world.Step();
            Assert.Equal(HoldTimerState.Holding,timer.State);
            var gate = (PoweredGatePart)world.FindPart("gate")!;
            for (var i=0;i<50;i++) world.Step();
            Assert.True(gate.Opening>0);
            for (var i=0;i<240;i++) world.Step();
            Assert.Equal(GateState.Closed,gate.State);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void AmbiguousSwitchConnectionRequiresExplicitDomain()
    {
        var world = World();
        try
        {
            var trigger = world.AddPart(new() { Id="switch",Kind="switch",Position=[-3,8,0] });
            var timer = world.FindPart("timer")!;
            var options = world.ConnectionOptions(trigger,timer);
            Assert.Equal(2,options.Count);
            Assert.Contains(options,c=>c.Type==ConnectionDomain.Activation);
            Assert.Contains(options,c=>c.Type==ConnectionDomain.Electrical);
            Assert.Null(world.SuggestedConnection(trigger,timer));
            Assert.False(world.Connect(trigger,timer));
            Assert.True(world.Connect(trigger,SocketId.ActivationOut,timer,SocketId.ActivationIn,ConnectionDomain.Activation));
            var saved = MachineCodec.Clone(world.Snapshot());
            world.LoadMachine(saved);
            Assert.Equal(ConnectionDomain.Activation,world.Connections.Last().Type);
            Assert.Equal(SocketId.ActivationIn,world.Connections.Last().ToPort);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(13f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void UnsupportedDurationIsRejected(float seconds)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            Assert.Throws<ArgumentException>(() => world.AddPart(new() { Id="timer",Kind="hold_timer",
                Properties=new() { [HoldTimerParameters.Seconds]=seconds } }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }
}
