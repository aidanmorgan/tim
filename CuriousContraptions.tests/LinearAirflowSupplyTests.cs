using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LinearAirflowSupplyTests(NativeSceneFixture godot)
{
    public enum Fixture { Fan, Pump, FirstBall, SecondBall }
    private static string Catalog(Fixture fixture)=>fixture switch
    {
        Fixture.Fan=>"fan",Fixture.Pump=>"bellows",Fixture.FirstBall or Fixture.SecondBall=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static string Instance(Fixture fixture)=>fixture switch
    {
        Fixture.Fan=>"supply",Fixture.Pump=>"pump",Fixture.FirstBall=>"first_receiver",Fixture.SecondBall=>"second_receiver",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };

    [Theory]
    [InlineData(1,true)]
    [InlineData(2,true)]
    [InlineData(2,false)]
    public void FanSharesFiniteSupplyAndRestoresConstruction(int count,bool powered)
    {
        var world=new MachineWorld {Gravity=0,Pressure=1};
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(new(){Id=Instance(Fixture.Fan),Kind=Catalog(Fixture.Fan),Position=[-3,6,0],
                Properties=new(){[PartParameterName.Of(FanParameter.Powered)]=powered?1:0}});
            for(var i=0;i<count;i++)
            {
                var fixture=i==0?Fixture.FirstBall:Fixture.SecondBall;
                world.AddPart(new(){Id=Instance(fixture),Kind=Catalog(fixture),Position=[0,6,i==0?-.5f:.5f]});
            }
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            PhysicsBodySnapshot[]? expectedBodies=null;
            PhysicsEnergyStoreState[]? expectedStores=null;
            for(var replay=0;replay<2;replay++)
            {
                world.Start();
                var fan=Assert.Single(world.Parts.OfType<FanPart>());
                var owner=world.PhysicsAssembly.Body(new(fan,MachinePart.RootBody));
                var supply=world.Physics.EnergyStore(owner.Id);
                Assert.Equal(FanPart.SupplyEnergy,supply.Energy);
                Assert.Equal(supply.InitialEnergy,supply.Energy);
                for(var i=0;i<4;i++)world.Step();
                Assert.Equal(powered?count:0,world.Physics.Loads.Transfers.Count);
                var after=world.Physics.EnergyStore(owner.Id);
                Assert.Equal(powered,after.ReleasedEnergy>0);
                Assert.Equal(0,after.AcceptedEnergy);
                var kinetic=world.PhysicsAssembly.Bodies.ToArray().Sum(body=>body.KineticEnergy);
                Assert.True(kinetic<=after.ReleasedEnergy+1e-8);
                if(powered)
                {
                    var total=Assert.Single(world.Physics.SourceTotals.ToArray());
                    Assert.True(total.Extraction.SuppliedPowerUpperBound<=
                        fan.ReadParameter(FanParameter.Force)*AirflowNetwork.ReferenceFlowSpeed);
                    Assert.InRange(Math.Abs(total.Extraction.Supplied+total.Extraction.SuppliedErrorBound-after.ReleasedEnergy),0,1e-8);
                }
                var bodies=world.Physics.Capture().BodyStates.ToArray();
                var stores=world.Physics.EnergyStores.ToArray();
                if(expectedBodies is null){expectedBodies=bodies;expectedStores=stores;}
                else {Assert.Equal(expectedBodies,bodies);Assert.Equal(expectedStores,stores);}
                world.Restore();
                Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            }
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BellowsReceiversExtractWorkFromThePlate(bool compressed)
    {
        var world=new MachineWorld {Gravity=0,Pressure=1};
        godot.Tree.Root.AddChild(world);
        try
        {
            var pump=(BellowsPart)world.AddPart(new(){Id=Instance(Fixture.Pump),Kind=Catalog(Fixture.Pump),Position=[0,6,0]});
            foreach(var fixture in new[]{Fixture.FirstBall,Fixture.SecondBall})
                world.AddPart(new(){Id=Instance(fixture),Kind=Catalog(fixture),
                    Position=[2.5f,5.88f,fixture==Fixture.FirstBall?-.35f:.35f]});
            world.Start();
            var plate=world.PhysicsAssembly.Body(new(pump,BellowsPart.PlateBody));
            if(compressed)world.Physics.ApplyImpulse(plate.Id,new(0,-1,0),plate.Center);
            var before=world.Physics.Capture();
            var energy=world.PhysicsAssembly.Bodies.ToArray().Sum(body=>body.KineticEnergy);
            world.Step();
            Assert.Equal(2,world.Physics.Loads.Transfers.Count);
            Assert.All(world.Physics.Loads.Transfers,load=>Assert.Equal(plate.Id,load.Source.ParticipationBody));
            var damping=Assert.Single(world.Physics.Loads.Damping);
            Assert.Equal(0,damping.NegativeCoefficient);Assert.Equal(BellowsPart.RefillDamping,damping.PositiveCoefficient);
            var extraction=Assert.Single(world.Physics.SourceTotals.ToArray()).Extraction.Supplied;
            var receiverEnergy=world.Parts.Where(part=>part is not BellowsPart)
                .Sum(part=>world.PhysicsAssembly.Body(new(part,MachinePart.RootBody)).KineticEnergy);
            if(compressed){Assert.True(extraction>1e-6);Assert.True(receiverEnergy>1e-8);}
            else {Assert.InRange(extraction,0,1e-10);Assert.InRange(receiverEnergy,0,1e-10);}
            Assert.True(receiverEnergy<=extraction+1e-8);
            Assert.True(world.PhysicsAssembly.Bodies.ToArray().Sum(body=>body.KineticEnergy)<=energy+1e-8);
            var after=world.Physics.Capture();
            world.Physics.Restore(before);world.Step();
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(after.SourceTotals.ToArray(),world.Physics.SourceTotals.ToArray());
            world.Physics.Restore(before);
            var collider=world.Physics.Collider(plate.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(plate.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            world.Step();
            Assert.All(world.Physics.SourceTotals.ToArray(),total=>Assert.Equal(0,total.Extraction.Supplied));
            Assert.All(world.Parts.Where(part=>part is not BellowsPart),
                part=>Assert.Equal(0,world.PhysicsAssembly.Body(new(part,MachinePart.RootBody)).KineticEnergy));
        }
        finally {world.Free();}
    }
}
