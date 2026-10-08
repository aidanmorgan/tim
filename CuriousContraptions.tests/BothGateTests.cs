using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BothGateTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(false,false,false)]
    [InlineData(false,true,false)]
    [InlineData(true,false,false)]
    [InlineData(true,true,false)]
    [InlineData(false,false,true)]
    [InlineData(false,true,true)]
    [InlineData(true,false,true)]
    [InlineData(true,true,true)]
    public void ContinuousTruthTableAndChainedGatesHaveNoOrderDelay(bool first,bool second,bool reverse)
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,4,0]});
            var power=new SupplyControl(world,battery);
            var source=power.Output;
            var gate=(ElectricalLogicPart)world.AddPart(new(){Id=reverse?"z-first":"a-first",Kind="both_gate",Position=[-4,4,0]});
            var next=(ElectricalLogicPart)world.AddPart(new(){Id=reverse?"a-next":"z-next",Kind="both_gate",Position=[0,4,0]});
            var load=world.AddPart(new(){Id="load",Kind="powered_gate",Position=[4,4,0]});
            Assert.Null(world.SuggestedConnection(source,gate));
            Assert.Equal(3,world.ConnectionOptions(source,gate).Count);
            Assert.True(world.Connect(source,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(source,SocketId.Supply,next,SocketId.PowerIn,ConnectionDomain.Electrical));
            if(first)Assert.True(world.Connect(source,SocketId.Supply,gate,SocketId.FirstIn,ConnectionDomain.Electrical));
            if(second)Assert.True(world.Connect(source,SocketId.Supply,gate,SocketId.SecondIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(gate,SocketId.Supply,next,SocketId.FirstIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(source,SocketId.Supply,next,SocketId.SecondIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(next,load));
            world.Start();power.SetAndSettle(SimulationLatchPhase.On);
            var expected=first?second?LogicInputState.Both:LogicInputState.FirstOnly:second?LogicInputState.SecondOnly:LogicInputState.Neither;
            Assert.Equal(expected,gate.State);
            Assert.Equal(first&&second,gate.Active);
            Assert.Equal(first&&second,load.HasElectricalPower(SocketId.PowerIn));
            power.SetAndSettle(SimulationLatchPhase.Off);
            Assert.Equal(LogicInputState.Neither,gate.State);
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            power.SetAndSettle(SimulationLatchPhase.On);
            Assert.Equal(first&&second,load.HasElectricalPower(SocketId.PowerIn));
            world.Restore();
            gate=(ElectricalLogicPart)world.FindPart(gate.Uid)!;
            Assert.Equal(LogicInputState.Neither,gate.State);
            Assert.False(gate.Active);
        }
        finally{world.Free();}
    }

    [Fact]
    public void PreviouslyPoweredFeedbackCannotRetainEnergyWithoutARealSource()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var power=new SupplyControl(world,battery);
            var a=world.AddPart(new(){Id="a",Kind="both_gate",Position=[4,4,0]});
            var b=world.AddPart(new(){Id="b",Kind="both_gate",Position=[8,4,0]});
            foreach(var input in new[]{SocketId.FirstIn,SocketId.SecondIn,SocketId.PowerIn})
            {
                Assert.True(world.Connect(power.Output,SocketId.Supply,a,input,ConnectionDomain.Electrical));
                Assert.True(world.Connect(a,SocketId.Supply,b,input,ConnectionDomain.Electrical));
                Assert.True(world.Connect(b,SocketId.Supply,a,input,ConnectionDomain.Electrical));
            }
            world.Start();power.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(a.Active);Assert.True(b.Active);
            power.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(a.Active);Assert.False(b.Active);
            for(var i=0;i<10;i++)world.Step();
            Assert.False(a.Active);Assert.False(b.Active);
        }
        finally{world.Free();}
    }
}
