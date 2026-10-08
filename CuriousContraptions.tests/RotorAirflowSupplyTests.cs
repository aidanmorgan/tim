using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RotorAirflowSupplyTests(NativeSceneFixture godot)
{
    public enum ReceiverSet { Rotor, TwoRotors, RotorAndBall }
    private enum Fixture { Fan, FirstRotor, SecondRotor, Ball }
    private static string Catalog(Fixture fixture)=>fixture switch
    {
        Fixture.Fan=>"fan",Fixture.FirstRotor or Fixture.SecondRotor=>"windmill",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static string Instance(Fixture fixture)=>fixture switch
    {
        Fixture.Fan=>"supply",Fixture.FirstRotor=>"first_rotor",Fixture.SecondRotor=>"second_rotor",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };

    [Theory]
    [InlineData(ReceiverSet.Rotor,true)]
    [InlineData(ReceiverSet.TwoRotors,true)]
    [InlineData(ReceiverSet.RotorAndBall,true)]
    [InlineData(ReceiverSet.Rotor,false)]
    [InlineData(ReceiverSet.TwoRotors,false)]
    [InlineData(ReceiverSet.RotorAndBall,false)]
    public void FanFundsEveryRotaryBranchAndRestoresConstruction(ReceiverSet receivers,bool powered)
    {
        if(!Enum.IsDefined(receivers))throw new ArgumentOutOfRangeException(nameof(receivers));
        var world=new MachineWorld {Gravity=0,Pressure=1};
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(new(){Id=Instance(Fixture.Fan),Kind=Catalog(Fixture.Fan),Position=[-3,6,0],
                Properties=new(){[PartParameterName.Of(FanParameter.Powered)]=powered?1:0,
                    [PartParameterName.Of(FanParameter.Width)]=3}});
            world.AddPart(new(){Id=Instance(Fixture.FirstRotor),Kind=Catalog(Fixture.FirstRotor),Position=[0,6,-1]});
            if(receivers==ReceiverSet.TwoRotors)
                world.AddPart(new(){Id=Instance(Fixture.SecondRotor),Kind=Catalog(Fixture.SecondRotor),Position=[0,6,1]});
            if(receivers==ReceiverSet.RotorAndBall)
                world.AddPart(new(){Id=Instance(Fixture.Ball),Kind=Catalog(Fixture.Ball),Position=[0,7.8f,0]});
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            PhysicsBodySnapshot[]? expectedBodies=null;
            PhysicsEnergyStoreState[]? expectedStores=null;
            for(var replay=0;replay<2;replay++)
            {
                world.Start();
                var fan=Assert.Single(world.Parts.OfType<FanPart>());
                var owner=world.PhysicsAssembly.Body(new(fan,MachinePart.RootBody));
                var sourceKey=new SceneMechanicalSourceKey(new(fan,MachinePart.RootBody),AirflowNetwork.SourceSlot);
                for(var tick=0;tick<4;tick++)
                {
                    try {world.Step();}
                    catch(TransferStepErrorException failure)
                    {
                        Console.WriteLine($"Transfer budget failure: receivers={receivers}, powered={powered}, replay={replay}, tick={tick}, bound={failure.Error.UpperBound:R}, limit={failure.Error.Limit:R}");
                        throw;
                    }
                }
                var rotors=world.Parts.OfType<WindmillPart>().ToArray();
                var released=world.Physics.EnergyStore(owner.Id).ReleasedEnergy;
                var kinetic=world.PhysicsAssembly.Bodies.ToArray().Sum(body=>body.KineticEnergy);
                Console.WriteLine($"Rotary source accounting: receivers={receivers}, powered={powered}, kinetic={kinetic:R}, released={released:R}, transfers={world.Physics.Loads.Transfers.Count+world.Physics.Loads.Rotary.Sum(load=>load.Branches.Count)}");
                Assert.Equal(rotors.Length,world.Physics.Loads.Rotary.Count);
                foreach(var rotor in rotors)
                {
                    Assert.Equal(powered,Math.Abs(rotor.ShaftSpeed)>1e-6);
                    var joint=world.CurrentJoint(new(rotor,WindmillPart.RotorJoint));
                    var capture=Assert.Single(world.Physics.Loads.Rotary,load=>load.Joint==joint.Id);
                    Assert.Equal(powered?rotor.AirflowSamples.Count:0,capture.Branches.Count);
                    Assert.DoesNotContain(world.Physics.Loads.Efforts,load=>load.Joint==joint.Id);
                }
                Assert.True(kinetic<=released+1e-8);
                Assert.Equal(powered,released>0);
                if(powered)
                {
                    var totals=world.Physics.TransferTotals.ToArray().ToDictionary(value=>value.Transfer);
                    foreach(var rotor in rotors)
                    foreach(var sample in rotor.AirflowSamples)
                    {
                        var receiverKey=new SceneMechanicalReceiverKey(new(rotor,sample.Body),sample.Slot);
                        var branch=world.TransferBindings.Transfer(sourceKey,receiverKey);
                        Assert.True(totals.TryGetValue(branch,out var work));
                        Assert.True(work.ReceiverDelivery.Supplied>0);
                    }
                    var source=Assert.Single(world.Physics.SourceTotals.ToArray());
                    Assert.InRange(Math.Abs(source.Extraction.Supplied+source.Extraction.SuppliedErrorBound-released),0,1e-8);
                }
                var after=world.Physics.Capture();
                if(expectedBodies is null)
                {
                    expectedBodies=after.BodyStates.ToArray();
                    expectedStores=world.Physics.EnergyStores.ToArray();
                }
                else
                {
                    Assert.Equal(expectedBodies,after.BodyStates.ToArray());
                    Assert.Equal(expectedStores,world.Physics.EnergyStores.ToArray());
                }
                world.Restore();
                Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
                world.LoadMachine(JsonSerializer.Deserialize(construction,MachineJson.Default.MachineData)!);
                Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            }
        }
        finally {world.Free();}
    }
}
