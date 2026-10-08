using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalSourceBindingTests
{
    private static readonly CollisionVector Z=new(0,0,1);

    [Theory]
    [InlineData(TransferSupplyKind.StoredFlow,false)]
    [InlineData(TransferSupplyKind.StoredFlow,true)]
    [InlineData(TransferSupplyKind.Mechanical,false)]
    [InlineData(TransferSupplyKind.Mechanical,true)]
    public void BindingSurvivesRemovalAndRestoresWithConstruction(TransferSupplyKind kind,bool run)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var otherFrame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(-2,0,0)),default,default);
        var body=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.Identity,Z,default,1,new(1,1,1));
        PhysicsObject Object(PhysicsBody value)=>new(value,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(frame),Object(otherFrame),Object(body)],[],new(default));
        world.InstallEnergyStores([new(frame.Id,10,0),new(otherFrame.Id,10,0)]);
        world.ChargeEnergyStore(frame.Id,10,1);world.ChargeEnergyStore(otherFrame.Id,10,1);
        var construction=world.Capture();
        MechanicalTransferSource Source(TransferSupplyKind mode,PhysicsBody owner,double rating)=>mode switch
        {
            TransferSupplyKind.StoredFlow=>new(new StoredFlowSource(new(7),owner.Id,8,new(rating,rating*8))),
            TransferSupplyKind.Mechanical=>new(new(7),new PointPowerPort(body.Id,owner.Id,default,Z),new(rating,rating*8)),
            _=>throw new ArgumentOutOfRangeException(nameof(mode))
        };
        PhysicsLoadSet Loads(MechanicalTransferSource source)=>new(){Transfers=[
            new(new(0),source,new PointPowerPort(body.Id,frame.Id,default,Z),new(1,1),1e-10)]};
        world.ReplaceLoads(Loads(Source(kind,frame,100)));
        if(run)world.Step([],[],.01);
        world.ReplaceLoads(new());
        var removed=world.Capture();
        var empty=world.Loads;
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(Loads(Source(kind,otherFrame,100))));
        Assert.Same(empty,world.Loads);
        var differentKind=kind==TransferSupplyKind.Mechanical?TransferSupplyKind.StoredFlow:TransferSupplyKind.Mechanical;
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(Loads(Source(differentKind,frame,100))));
        Assert.Same(empty,world.Loads);
        Assert.Equal(removed.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(removed.SourceTotals.ToArray(),world.SourceTotals.ToArray());
        // Rating is a declaration parameter, not supply ownership.
        world.ReplaceLoads(Loads(Source(kind,frame,50)));
        world.Restore(removed);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(Loads(Source(kind,otherFrame,50))));
        // Restoring before first installation must remove the later binding.
        world.Restore(construction);
        world.ReplaceLoads(Loads(Source(kind,otherFrame,100)));
        Assert.Single(world.Loads.Transfers);
        Assert.Equal(construction.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }
}
