using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ElectricalLogicTests(HeadlessFixture godot)
{
    private partial class Gate : BothGatePart
    {
        public LogicGateKind Operation { get; set; }
        public override IEnumerable<ElectricalGate> ElectricalGates =>
            [new(Operation,SocketIds.FirstIn,SocketIds.SecondIn,SocketIds.PowerIn,SocketIds.Supply)];
    }
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    private static Gate AddGate(MachineWorld world,string id,LogicGateKind operation)
    {
        var gate=new Gate {Operation=operation,Definition=new PartDefinition()};
        gate.Configure(new(){Id=id,Kind="both_gate"});
        world.AddChild(gate);world.Parts.Add(gate);return gate;
    }
    private static void Wire(MachineWorld world,MachinePart source,MachinePart target,string input) =>
        Assert.True(world.Connect(source,SocketIds.Supply,target,input,ConnectionDomain.Electrical));
    public static IEnumerable<object[]> Cases()
    {
        foreach(var row in LogicGateTests.TruthRows())
            foreach(var supply in new[]{false,true})
                yield return [..row,supply];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void SettledTruthRequiresIndependentSupply(LogicGateKind operation,bool first,bool second,bool expected,bool supply)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var gate=AddGate(world,"gate",operation);
            var load=world.AddPart(new(){Id="load",Kind="powered_gate"});
            if(first)Wire(world,battery,gate,SocketIds.FirstIn);
            if(second)Wire(world,battery,gate,SocketIds.SecondIn);
            if(supply)Wire(world,battery,gate,SocketIds.PowerIn);
            Wire(world,gate,load,SocketIds.PowerIn);
            ElectricalNetwork.Solve(world);
            Assert.Equal(supply&&expected,load.HasElectricalPower(SocketIds.PowerIn));
            world.Parts.Reverse();world.Connections.Reverse();
            ElectricalNetwork.Solve(world);
            Assert.Equal(supply&&expected,load.HasElectricalPower(SocketIds.PowerIn));
            world.Connections.RemoveAll(c=>c.From==battery.Uid);
            ElectricalNetwork.Solve(world);
            Assert.False(load.HasElectricalPower(SocketIds.PowerIn));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(LogicGateKind.Xor)]
    [InlineData(LogicGateKind.Nand)]
    public void ReconvergentSecondInputRetractsOutputWithinTheSameSolve(LogicGateKind operation)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var first=AddGate(world,"z-first",LogicGateKind.Or);
            var last=AddGate(world,"a-last",operation);
            var load=world.AddPart(new(){Id="load",Kind="powered_gate"});
            foreach(var gate in new[]{first,last})Wire(world,battery,gate,SocketIds.PowerIn);
            Wire(world,battery,last,SocketIds.FirstIn);
            Wire(world,first,last,SocketIds.SecondIn);
            Wire(world,last,load,SocketIds.PowerIn);
            ElectricalNetwork.Solve(world);
            Assert.True(load.HasElectricalPower(SocketIds.PowerIn));
            Wire(world,battery,first,SocketIds.FirstIn);
            ElectricalNetwork.Solve(world);
            Assert.False(load.HasElectricalPower(SocketIds.PowerIn));
            world.Parts.Reverse();
            ElectricalNetwork.Solve(world);
            Assert.False(load.HasElectricalPower(SocketIds.PowerIn));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(LogicGateKind.Xor)]
    [InlineData(LogicGateKind.Nor)]
    [InlineData(LogicGateKind.Nand)]
    public void ZeroDelayNonmonotoneFeedbackIsRejectedWithoutPartialPowerCommit(LogicGateKind operation)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var a=AddGate(world,"a",operation);var b=AddGate(world,"b",LogicGateKind.Or);
            Wire(world,battery,a,SocketIds.PowerIn);Wire(world,battery,b,SocketIds.PowerIn);
            Wire(world,a,b,SocketIds.FirstIn);Wire(world,b,a,SocketIds.FirstIn);
            Assert.Throws<InvalidOperationException>(()=>ElectricalNetwork.Solve(world));
            Assert.False(a.HasElectricalPower(SocketIds.PowerIn));
            Assert.False(b.HasElectricalPower(SocketIds.PowerIn));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(LogicGateKind.And)]
    [InlineData(LogicGateKind.Or)]
    public void MonotoneFeedbackCannotRememberAConditionAfterItsSourceIsRemoved(LogicGateKind operation)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var a=AddGate(world,"a",operation);var b=AddGate(world,"b",operation);
            foreach(var gate in new[]{a,b})
            {
                Wire(world,battery,gate,SocketIds.PowerIn);
                Wire(world,battery,gate,SocketIds.SecondIn);
            }
            Wire(world,battery,a,SocketIds.FirstIn);
            Wire(world,a,b,SocketIds.FirstIn);Wire(world,b,a,SocketIds.FirstIn);
            ElectricalNetwork.Solve(world);
            Assert.True(b.HasElectricalPower(SocketIds.FirstIn));
            world.Connections.RemoveAll(c=>c.From==battery.Uid);
            ElectricalNetwork.Solve(world);
            Assert.False(a.HasElectricalPower(SocketIds.FirstIn));
            Assert.False(b.HasElectricalPower(SocketIds.FirstIn));
        }
        finally{world.Free();}
    }
}
