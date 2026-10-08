using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LatchTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void SocketCommandsRetainMemoryAndResetWinsEitherOrder(bool reverse,bool supply)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var specs=new List<PartSpec>{
                new(){Id="set",Kind="switch",Position=[-6,6,0]},
                new(){Id="reset",Kind="switch",Position=[-3,6,0]},
                new(){Id="latch",Kind="latch",Position=[0,6,0]},
                new(){Id="battery",Kind="battery",Position=[3,6,0]},
                new(){Id="gate",Kind="powered_gate",Position=[6,6,0]}};
            if(reverse)specs.Reverse();
            foreach(var spec in specs)world.AddPart(spec);
            var latch=(LatchPart)world.FindPart("latch")!;
            var set=world.FindPart("set")!;var reset=world.FindPart("reset")!;
            var gate=world.FindPart("gate")!;
            Assert.Null(world.SuggestedConnection(set,latch));
            Assert.True(world.Connect(set,SocketId.ActivationOut,latch,SocketId.SetIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(reset,SocketId.ActivationOut,latch,SocketId.ResetIn,ConnectionDomain.Activation));
            var power=new SupplyControl(world,world.FindPart("battery")!);
            Assert.True(world.Connect(power.Output,latch));
            Assert.True(world.Connect(latch,gate));
            world.Start();
            if(supply)power.SetAndSettle(SimulationLatchPhase.On);
            world.Activate(set);
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            world.Step(); // Current-tick requests cannot settle before all senders have delivered.
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            world.Step();
            Assert.Equal(SimulationLatchPhase.On,latch.State);
            Assert.Equal(supply,gate.HasElectricalPower(SocketId.PowerIn));
            for(var tick=0;tick<20;tick++)world.Step();
            Assert.Equal(SimulationLatchPhase.On,latch.State);
            // Memory survives an upstream contact opening; wiring stays fixed.
            power.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            power.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(gate.HasElectricalPower(SocketId.PowerIn));
            if(reverse){world.Activate(reset);world.Activate(set);}
            else{world.Activate(set);world.Activate(reset);}
            world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            world.Activate(set);world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.On,latch.State);
            world.Activate(reset);world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            world.Activate(set); // Pending requests are also discarded by workshop Reset.
            world.Restore();
            latch=(LatchPart)world.FindPart("latch")!;
            world.Start();world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            Assert.Throws<ArgumentException>(()=>world.Activate(latch));
        }
        finally{world.Free();}
    }

    [Fact]
    public void BothSocketsInOneFanoutAreDeliveredRatherThanDeduplicatingThePart()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var source=world.AddPart(new(){Id="source",Kind="switch"});
            var latch=(LatchPart)world.AddPart(new(){Id="latch",Kind="latch",Position=[4,4,0]});
            Assert.True(world.Connect(source,SocketId.ActivationOut,latch,SocketId.SetIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(source,SocketId.ActivationOut,latch,SocketId.ResetIn,ConnectionDomain.Activation));
            world.Start();world.Activate(source);world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
        }
        finally{world.Free();}
    }

    [Fact]
    public void OwnedSnapshotsRestorePendingCommandsAndSceneReadings()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var latch=(LatchPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind="latch"});
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            Assert.Throws<InvalidOperationException>(()=>latch.HandleActivation(world,ActivationCommand.Set));
            world.Start();
            var empty=world.Latches.Capture();
            latch.HandleActivation(world,ActivationCommand.Set);
            var pending=world.Latches.Capture();
            world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.On,latch.State);
            world.Latches.Restore(empty);
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
            world.Latches.Restore(pending);
            var key=new SceneLatchKey(latch,LatchPart.Memory);
            Assert.Equal(SimulationLatchRequests.Set,world.ReadLatch(key).NextRequests);
            // Exercise the owned boundaries directly; the scene reads the same authority.
            world.Latches.Advance(0);world.Latches.Advance(1);
            Assert.Equal(SimulationLatchPhase.On,latch.State);
            Assert.Throws<ArgumentException>(()=>latch.HandleActivation(world,ActivationCommand.Trigger));
            world.Restore();
            latch=(LatchPart)Assert.Single(world.Parts);
            Assert.Equal(SimulationLatchPhase.Off,latch.State);
        }
        finally{world.Free();}
    }
}
