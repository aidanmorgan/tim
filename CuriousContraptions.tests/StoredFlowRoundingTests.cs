using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class StoredFlowRoundingTests
{
    [Theory]
    [InlineData(3,false)]
    [InlineData(3,true)]
    [InlineData(7,false)]
    [InlineData(7,true)]
    [InlineData(11,false)]
    [InlineData(11,true)]
    public void DecimalSharedBudgetDebitsOnlyAvailableWork(int count,bool separateSources)
    {
        const double duration=.03,energy=.1,speed=3.1;
        var z=new CollisionVector(0,0,1);
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-2,0,0)),default,default);
        var receivers=Enumerable.Range(1,count).Select(i=>new PhysicsBody(new(i),PhysicsMotionType.Dynamic,
            RigidPose.At(new(i*2,0,0)),default,default,1,new(1,1,1))).ToArray();
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],new[]{owner}.Concat(receivers).Select(Object).ToArray(),[],new(default,maximumStep:duration));
        world.InstallEnergyStores([new(owner.Id,1,0)]);world.ChargeEnergyStore(owner.Id,energy,1);
        world.ReplaceLoads(new(){Transfers=receivers.Select((body,i)=>new MechanicalTransferLoad(new(i),
            new(new StoredFlowSource(new(separateSources?i:0),owner.Id,speed,new(100,1000))),
            new PointPowerPort(body.Id,owner.Id,default,z),new(1,1),1e-10)).ToArray()});
        var initial=world.Capture();
        var result=world.Step([],[],duration);
        var store=world.EnergyStore(owner.Id);
        Assert.InRange(store.ReleasedEnergy,energy-1e-14,energy);
        Assert.InRange(store.Energy,0,1e-14);
        var supplied=world.SourceUse.ToArray().Sum(source=>source.Extraction.Supplied);
        Assert.InRange(Math.Abs(store.ReleasedEnergy-supplied),0,1e-15);
        foreach(var receiver in receivers)
            Assert.InRange(Math.Abs(receiver.LinearVelocity.Z-energy/(count*speed)),0,1e-12);
        var final=world.Capture();
        world.Restore(initial);
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }
}
