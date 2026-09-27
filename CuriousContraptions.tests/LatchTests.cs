using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class LatchTests(HeadlessFixture godot)
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
            Assert.True(world.Connect(set,SocketIds.ActivationOut,latch,SocketIds.SetIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(reset,SocketIds.ActivationOut,latch,SocketIds.ResetIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(world.FindPart("battery")!,latch));
            Assert.True(world.Connect(latch,gate));
            var wire=world.Connections.Single(c=>c.From=="battery");
            if(!supply)world.Connections.Remove(wire);
            world.Start();
            world.Activate(set);
            Assert.Equal(LatchState.Off,latch.State);
            world.Step(); // Current-tick requests cannot settle before all senders have delivered.
            Assert.Equal(LatchState.Off,latch.State);
            world.Step();
            Assert.Equal(LatchState.On,latch.State);
            Assert.Equal(supply,gate.HasElectricalPower(SocketIds.PowerIn));
            for(var tick=0;tick<20;tick++)world.Step();
            Assert.Equal(LatchState.On,latch.State);
            // Memory survives source removal/restoration; only a real source powers the output.
            world.Connections.Remove(wire);world.Step();
            Assert.False(gate.HasElectricalPower(SocketIds.PowerIn));
            world.Connections.Add(wire);world.Step();
            Assert.True(gate.HasElectricalPower(SocketIds.PowerIn));
            if(reverse){world.Activate(reset);world.Activate(set);}
            else{world.Activate(set);world.Activate(reset);}
            world.Step();world.Step();
            Assert.Equal(LatchState.Off,latch.State);
            Assert.False(gate.HasElectricalPower(SocketIds.PowerIn));
            world.Activate(set);world.Step();world.Step();
            Assert.Equal(LatchState.On,latch.State);
            world.Activate(reset);world.Step();world.Step();
            Assert.Equal(LatchState.Off,latch.State);
            world.Activate(set); // Pending requests are also discarded by workshop Reset.
            world.Restore();
            latch=(LatchPart)world.FindPart("latch")!;
            world.Start();world.Step();world.Step();
            Assert.Equal(LatchState.Off,latch.State);
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
            Assert.True(world.Connect(source,SocketIds.ActivationOut,latch,SocketIds.SetIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(source,SocketIds.ActivationOut,latch,SocketIds.ResetIn,ConnectionDomain.Activation));
            world.Start();world.Activate(source);world.Step();world.Step();
            Assert.Equal(LatchState.Off,latch.State);
        }
        finally{world.Free();}
    }
}
