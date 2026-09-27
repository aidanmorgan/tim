using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BothGateTests(HeadlessFixture godot)
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
            var source=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,4,0]});
            var gate=(BothGatePart)world.AddPart(new(){Id=reverse?"z-first":"a-first",Kind="both_gate",Position=[-4,4,0]});
            var next=(BothGatePart)world.AddPart(new(){Id=reverse?"a-next":"z-next",Kind="both_gate",Position=[0,4,0]});
            var load=world.AddPart(new(){Id="load",Kind="powered_gate",Position=[4,4,0]});
            Assert.Null(world.SuggestedConnection(source,gate));
            Assert.Equal(2,world.ConnectionOptions(source,gate).Count);
            if(first)Assert.True(world.Connect(source,SocketIds.Supply,gate,SocketIds.FirstIn,ConnectionDomain.Electrical));
            if(second)Assert.True(world.Connect(source,SocketIds.Supply,gate,SocketIds.SecondIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(gate,SocketIds.Supply,next,SocketIds.FirstIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(source,SocketIds.Supply,next,SocketIds.SecondIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(next,load));
            world.Start();world.Step();
            var expected=first?second?BothGateState.Both:BothGateState.FirstOnly:second?BothGateState.SecondOnly:BothGateState.Neither;
            Assert.Equal(expected,gate.State);
            Assert.Equal(first&&second,gate.Active);
            Assert.Equal(first&&second,load.HasElectricalPower(SocketIds.PowerIn));
            var wires=world.Connections.Where(c=>c.From==source.Uid).ToArray();
            foreach(var wire in wires)world.Connections.Remove(wire);
            world.Step();
            Assert.Equal(BothGateState.Neither,gate.State);
            Assert.False(load.HasElectricalPower(SocketIds.PowerIn));
            world.Connections.AddRange(wires);world.Step();
            Assert.Equal(first&&second,load.HasElectricalPower(SocketIds.PowerIn));
            world.Restore();
            gate=(BothGatePart)world.FindPart(gate.Uid)!;
            Assert.Equal(BothGateState.Neither,gate.State);
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
            var a=world.AddPart(new(){Id="a",Kind="both_gate",Position=[4,4,0]});
            var b=world.AddPart(new(){Id="b",Kind="both_gate",Position=[8,4,0]});
            foreach(var input in new[]{SocketIds.FirstIn,SocketIds.SecondIn})
            {
                Assert.True(world.Connect(battery,SocketIds.Supply,a,input,ConnectionDomain.Electrical));
                Assert.True(world.Connect(a,SocketIds.Supply,b,input,ConnectionDomain.Electrical));
                Assert.True(world.Connect(b,SocketIds.Supply,a,input,ConnectionDomain.Electrical));
            }
            world.Start();world.Step();
            Assert.True(a.Active);Assert.True(b.Active);
            world.Connections.RemoveAll(c=>c.From==battery.Uid);
            world.Step();
            Assert.False(a.Active);Assert.False(b.Active);
            for(var i=0;i<10;i++)world.Step();
            Assert.False(a.Active);Assert.False(b.Active);
        }
        finally{world.Free();}
    }
}
