using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ElectricalLogicTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Load=new("powered_gate"),Contact=new("switch");
    private static CatalogueId Gate(LogicGateKind operation)=>operation switch
    {
        LogicGateKind.And=>new("both_gate"),LogicGateKind.Or=>new("electrical_or"),
        LogicGateKind.Xor=>new("electrical_xor"),LogicGateKind.Nor=>new("electrical_nor"),
        LogicGateKind.Nand=>new("electrical_nand"),_=>throw new ArgumentOutOfRangeException(nameof(operation))
    };
    private static ElectricalLogicPart AddGate(MachineWorld world,FixturePartId id,LogicGateKind operation) =>
        (ElectricalLogicPart)world.AddPart(new(){Id=FixtureParts.Id(id),Kind=Gate(operation).Value});
    private static void Wire(MachineWorld world,MachinePart source,MachinePart target,SocketId input) =>
        Assert.True(world.Connect(source,SocketId.Supply,target,input,ConnectionDomain.Electrical));
    public static IEnumerable<object[]> Cases()
    {
        foreach(var row in LogicGateTests.TruthRows())
            foreach(var supply in new[]{false,true})
                foreach(var reverse in new[]{false,true})
                    yield return [..row,supply,reverse];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void SettledTruthRequiresIndependentSupply(LogicGateKind operation,bool first,bool second,bool expected,bool supply,bool reverse)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value});
            var gate=AddGate(world,reverse?FixturePartId.Second:FixturePartId.First,operation);
            var load=world.AddPart(new(){Id=FixtureParts.Id(reverse?FixturePartId.First:FixturePartId.Second),Kind=Load.Value});
            if(first)Wire(world,battery,gate,SocketId.FirstIn);
            if(second)Wire(world,battery,gate,SocketId.SecondIn);
            if(supply)Wire(world,battery,gate,SocketId.PowerIn);
            Wire(world,gate,load,SocketId.PowerIn);
            new ElectricalNetwork(world).Solve();
            Assert.Equal(supply&&expected,load.HasElectricalPower(SocketId.PowerIn));
            var links=world.Connections.ToArray();
            foreach(var link in links)Assert.True(world.Disconnect(link));
            foreach(var link in links.Reverse())
                Assert.True(world.Connect(world.FindPart(link.From)!,link.FromPort!.Value,
                    world.FindPart(link.To)!,link.ToPort!.Value,link.Type));
            new ElectricalNetwork(world).Solve();
            Assert.Equal(supply&&expected,load.HasElectricalPower(SocketId.PowerIn));
            foreach(var link in world.Connections.Where(c=>c.From==battery.Uid).ToArray())
                Assert.True(world.Disconnect(link));
            new ElectricalNetwork(world).Solve();
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(LogicGateKind.Xor,false)]
    [InlineData(LogicGateKind.Xor,true)]
    [InlineData(LogicGateKind.Nand,false)]
    [InlineData(LogicGateKind.Nand,true)]
    public void ReconvergentSecondInputRetractsOutputWithinTheSameSolve(LogicGateKind operation,bool reverse)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value});
            var first=AddGate(world,reverse?FixturePartId.First:FixturePartId.Second,LogicGateKind.Or);
            var last=AddGate(world,reverse?FixturePartId.Second:FixturePartId.First,operation);
            var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind=Load.Value});
            foreach(var gate in new[]{first,last})Wire(world,battery,gate,SocketId.PowerIn);
            Wire(world,battery,last,SocketId.FirstIn);
            Wire(world,first,last,SocketId.SecondIn);
            Wire(world,last,load,SocketId.PowerIn);
            new ElectricalNetwork(world).Solve();
            Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            Wire(world,battery,first,SocketId.FirstIn);
            new ElectricalNetwork(world).Solve();
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            new ElectricalNetwork(world).Solve();
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
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value});
            var a=AddGate(world,FixturePartId.First,operation);var b=AddGate(world,FixturePartId.Second,LogicGateKind.Or);
            Wire(world,battery,a,SocketId.PowerIn);Wire(world,battery,b,SocketId.PowerIn);
            Wire(world,a,b,SocketId.FirstIn);Wire(world,b,a,SocketId.FirstIn);
            Assert.Throws<ElectricalFeedbackException>(()=>new ElectricalNetwork(world).Solve());
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
            var gate=AddGate(world,FixturePartId.First,LogicGateKind.Xor);
            var contact=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Contact.Value});
            Wire(world,gate,contact,SocketId.PowerIn);
            Wire(world,contact,gate,SocketId.FirstIn);
            Assert.False(contact.Active);
            var before=System.Text.Json.JsonSerializer.Serialize(world.Snapshot());
            var error=Assert.Throws<ElectricalFeedbackException>(()=>world.Start());
            Assert.Contains(FixtureParts.Id(FixturePartId.First),error.Message);
            Assert.Contains(FixtureParts.Id(FixturePartId.Second),error.Message);
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
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value});
            var a=AddGate(world,FixturePartId.First,operation);var b=AddGate(world,FixturePartId.Second,operation);
            foreach(var gate in new[]{a,b})
            {
                Wire(world,battery,gate,SocketId.PowerIn);
                Wire(world,battery,gate,SocketId.SecondIn);
            }
            Wire(world,battery,a,SocketId.FirstIn);
            Wire(world,a,b,SocketId.FirstIn);Wire(world,b,a,SocketId.FirstIn);
            new ElectricalNetwork(world).Solve();
            Assert.True(b.HasElectricalPower(SocketId.FirstIn));
            foreach(var link in world.Connections.Where(c=>c.From==battery.Uid).ToArray())
                Assert.True(world.Disconnect(link));
            new ElectricalNetwork(world).Solve();
            Assert.False(a.HasElectricalPower(SocketId.FirstIn));
            Assert.False(b.HasElectricalPower(SocketId.FirstIn));
        }
        finally{world.Free();}
    }
}
