using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ElectricalLogicTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    private static ElectricalLogicPart AddGate(MachineWorld world,string id,LogicGateKind operation) =>
        (ElectricalLogicPart)world.AddPart(new(){Id=id,Kind=operation switch
        {
            LogicGateKind.And=>"both_gate",LogicGateKind.Or=>"electrical_or",
            LogicGateKind.Xor=>"electrical_xor",LogicGateKind.Nor=>"electrical_nor",
            LogicGateKind.Nand=>"electrical_nand",_=>throw new ArgumentOutOfRangeException(nameof(operation))
        }});
    private static void Wire(MachineWorld world,MachinePart source,MachinePart target,SocketId input) =>
        Assert.True(world.Connect(source,SocketId.Supply,target,input,ConnectionDomain.Electrical));
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
            if(first)Wire(world,battery,gate,SocketId.FirstIn);
            if(second)Wire(world,battery,gate,SocketId.SecondIn);
            if(supply)Wire(world,battery,gate,SocketId.PowerIn);
            Wire(world,gate,load,SocketId.PowerIn);
            ElectricalNetwork.Solve(world);
            Assert.Equal(supply&&expected,load.HasElectricalPower(SocketId.PowerIn));
            world.Parts.Reverse();world.Connections.Reverse();
            ElectricalNetwork.Solve(world);
            Assert.Equal(supply&&expected,load.HasElectricalPower(SocketId.PowerIn));
            world.Connections.RemoveAll(c=>c.From==battery.Uid);
            ElectricalNetwork.Solve(world);
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
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
            foreach(var gate in new[]{first,last})Wire(world,battery,gate,SocketId.PowerIn);
            Wire(world,battery,last,SocketId.FirstIn);
            Wire(world,first,last,SocketId.SecondIn);
            Wire(world,last,load,SocketId.PowerIn);
            ElectricalNetwork.Solve(world);
            Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            Wire(world,battery,first,SocketId.FirstIn);
            ElectricalNetwork.Solve(world);
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            world.Parts.Reverse();
            ElectricalNetwork.Solve(world);
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
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
            Wire(world,battery,a,SocketId.PowerIn);Wire(world,battery,b,SocketId.PowerIn);
            Wire(world,a,b,SocketId.FirstIn);Wire(world,b,a,SocketId.FirstIn);
            Assert.Throws<ElectricalFeedbackException>(()=>ElectricalNetwork.Solve(world));
            Assert.False(a.HasElectricalPower(SocketId.PowerIn));
            Assert.False(b.HasElectricalPower(SocketId.PowerIn));
        }
        finally{world.Free();}
    }
    [Fact]
    public void OpenSwitchCannotHideInvalidFeedbackUntilAfterRunStarts()
    {
        var world=World();
        try
        {
            var gate=AddGate(world,"exclusive",LogicGateKind.Xor);
            var contact=world.AddPart(new(){Id="switch",Kind="switch"});
            Wire(world,gate,contact,SocketId.PowerIn);
            Wire(world,contact,gate,SocketId.FirstIn);
            Assert.False(contact.Active);
            var before=System.Text.Json.JsonSerializer.Serialize(world.Snapshot());
            var error=Assert.Throws<ElectricalFeedbackException>(()=>world.Start());
            Assert.Contains("exclusive",error.Message);
            Assert.Contains("switch",error.Message);
            Assert.False(world.Running);
            Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(world.Snapshot()));
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
                Wire(world,battery,gate,SocketId.PowerIn);
                Wire(world,battery,gate,SocketId.SecondIn);
            }
            Wire(world,battery,a,SocketId.FirstIn);
            Wire(world,a,b,SocketId.FirstIn);Wire(world,b,a,SocketId.FirstIn);
            ElectricalNetwork.Solve(world);
            Assert.True(b.HasElectricalPower(SocketId.FirstIn));
            world.Connections.RemoveAll(c=>c.From==battery.Uid);
            ElectricalNetwork.Solve(world);
            Assert.False(a.HasElectricalPower(SocketId.FirstIn));
            Assert.False(b.HasElectricalPower(SocketId.FirstIn));
        }
        finally{world.Free();}
    }
}
