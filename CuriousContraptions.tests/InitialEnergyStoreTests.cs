using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class InitialEnergyStoreTests
{
    private static readonly CollisionVector X=new(1,0,0);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(0)]
    [InlineData(.25)]
    [InlineData(1)]
    public void InitialSupplyIsDistinctFromChargingAndRestoresExactly(double initial)
    {
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),default,default,1,new(1,1,1));
        var world=new PhysicsWorld([],[Object(owner),Object(body)],[],new(default,maximumStep:.125));
        world.InstallEnergyStores([new(owner.Id,1,initial)]);
        var store=world.EnergyStore(owner.Id);
        Assert.Equal(initial,store.InitialEnergy);Assert.Equal(initial,store.Energy);
        Assert.Equal(0,store.AcceptedEnergy);Assert.Equal(0,store.ReleasedEnergy);
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),owner.Id,4,new(1,4))),
            new PointPowerPort(body.Id,owner.Id,default,X),new(1,1),1e-10)]});
        var before=world.Capture();var result=world.Step([],[],.125);
        var released=Math.Min(initial,.5);
        var after=world.EnergyStore(owner.Id);
        Assert.Equal(released,after.ReleasedEnergy);
        Assert.Equal(initial-released,after.Energy);
        Assert.Equal(initial,after.InitialEnergy);
        Assert.Equal(0,after.AcceptedEnergy);
        Assert.Equal(released/4,body.LinearVelocity.X);
        Assert.Throws<ArgumentException>(()=>world.InstallEnergyStores([new(owner.Id,1,1)]));
        Assert.Equal(after,world.EnergyStore(owner.Id));
        Assert.Equal(.25,world.ChargeEnergyStore(owner.Id,1,.25));
        var charged=world.EnergyStore(owner.Id);
        Assert.Equal(initial,charged.InitialEnergy);
        Assert.Equal(.25,charged.AcceptedEnergy);
        Assert.Equal(charged.InitialEnergy+charged.AcceptedEnergy,charged.Energy+charged.ReleasedEnergy);
        var finalBody=world.Capture().BodyStates.ToArray();
        world.Restore(before);
        Assert.Equal(store,world.EnergyStore(owner.Id));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(result,world.Step([],[],.125));
        Assert.Equal(after,world.EnergyStore(owner.Id));
        world.ChargeEnergyStore(owner.Id,1,.25);
        Assert.Equal(charged,world.EnergyStore(owner.Id));
        Assert.Equal(finalBody,world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UnsupportedInitialBalancesReject(double initial)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsEnergyStoreDeclaration(new(0),1,initial));

    [Fact]
    public void RejectedBatchCannotPartiallySeedAndRestoreRemovesLaterMembership()
    {
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(owner)],[],new(default));
        var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.InstallEnergyStores([new(owner.Id,1,.5),new(new(99),1,.5)]));
        Assert.Empty(world.EnergyStores.ToArray());
        world.InstallEnergyStores([new(owner.Id,1,.5)]);
        Assert.Equal(.5,world.EnergyStore(owner.Id).InitialEnergy);
        world.Restore(before);
        Assert.Empty(world.EnergyStores.ToArray());
        world.InstallEnergyStores([new(owner.Id,1,.25)]);
        Assert.Equal(.25,world.EnergyStore(owner.Id).Energy);
        Assert.Equal(0,world.EnergyStore(owner.Id).AcceptedEnergy);
    }

    [Fact]
    public void RunningWorldCannotIntroduceConstructionEnergy()
    {
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(owner)],[],new(default));
        var construction=world.Capture();
        world.Step([],[],.01);
        Assert.Throws<InvalidOperationException>(()=>world.InstallEnergyStores([new(owner.Id,1,.5)]));
        Assert.Empty(world.EnergyStores.ToArray());
        world.InstallEnergyStores([new(owner.Id,1,0)]);
        world.ChargeEnergyStore(owner.Id,1,.5);
        Assert.Equal(0,world.EnergyStore(owner.Id).InitialEnergy);
        Assert.Equal(.5,world.EnergyStore(owner.Id).AcceptedEnergy);
        world.Restore(construction);
        world.InstallEnergyStores([new(owner.Id,1,.5)]);
        Assert.Equal(.5,world.EnergyStore(owner.Id).InitialEnergy);
        Assert.Equal(0,world.EnergyStore(owner.Id).AcceptedEnergy);
    }
}
