using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class StoredFlowSourceTests
{
    private static PhysicsWorld World(double energy)
    {
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[new(owner,new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0))],[],new(default));
        world.InstallEnergyStores([new(owner.Id,10,0)]);
        world.ChargeEnergyStore(owner.Id,energy,1);
        return world;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedStoreCannotBeAllocatedTwiceAndPredictionDoesNotDebit(bool reverse)
    {
        var world=World(4);var before=world.EnergyStore(new(0));
        StoredFlowRequest[] requests=[
            new(new(new(0),new(0),2,new(100,100)),4),
            new(new(new(1),new(0),4,new(100,100)),2)];
        if(reverse)Array.Reverse(requests);
        var stores=world.EnergyStores.ToArray().ToDictionary(value=>value.Owner);
        var result=StoredFlowAllocation.Prepare(requests,stores,1);
        Assert.Equal(new MechanicalSourceId(0),result[0].Source);
        Assert.Equal(new MechanicalSourceId(1),result[1].Source);
        Assert.InRange(result[0].Force,1-1e-12,1);
        Assert.InRange(result[1].Force,.5-1e-12,.5);
        Assert.All(result,value=>Assert.InRange(value.Work,2-1e-12,2));
        Assert.True(result.Sum(value=>value.Work)<=before.Energy);
        Assert.Equal(before,world.EnergyStore(new(0)));
        Assert.Equal(result,StoredFlowAllocation.Prepare(requests,stores,1));
        var collection=Assert.IsAssignableFrom<IList<StoredFlowAllowance>>(result);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(()=>collection[0]=default);
    }

    [Theory]
    [InlineData(0,2,100,100,0)]
    [InlineData(10,0,100,100,0)]
    [InlineData(10,2,1,100,1)]
    [InlineData(10,2,100,2,1)]
    public void EmptyStoreStationaryFlowAndIndependentRatingsApply(double energy,double speed,double force,double power,double expected)
    {
        var world=World(energy);
        var result=StoredFlowAllocation.Prepare([new(new(new(0),new(0),speed,new(force,power)),4)],
            world.EnergyStores.ToArray().ToDictionary(value=>value.Owner),1);
        Assert.InRange(result[0].Force,Math.Max(0,expected-1e-12),expected);
        Assert.InRange(result[0].Work,0,energy);
    }

    [Fact]
    public void DuplicateMissingAndInvalidDemandsRejectWithoutDebit()
    {
        var world=World(4);
        var stores=world.EnergyStores.ToArray().ToDictionary(value=>value.Owner);
        var request=new StoredFlowRequest(new(new(0),new(0),2,new(10,10)),1);
        Assert.Throws<ArgumentException>(()=>StoredFlowAllocation.Prepare([request,request],stores,1));
        Assert.Throws<ArgumentException>(()=>StoredFlowAllocation.Prepare(
            [new(new(new(0),new(99),2,new(10,10)),1)],stores,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>StoredFlowAllocation.Prepare([request with {ForceDemand=-1}],stores,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>StoredFlowAllocation.Prepare([request],stores,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new StoredFlowSource(new(0),new(0),double.NaN,new(1,1)));
        Assert.Equal(4,world.EnergyStore(new(0)).Energy);
    }
}
