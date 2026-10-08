using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public enum TransferRatingLimit { Force, Power }

public class TransferRatingRoundingTests
{
    [Theory]
    [InlineData(3,TransferSupplyKind.Mechanical,TransferRatingLimit.Force)]
    [InlineData(7,TransferSupplyKind.Mechanical,TransferRatingLimit.Force)]
    [InlineData(11,TransferSupplyKind.Mechanical,TransferRatingLimit.Force)]
    [InlineData(3,TransferSupplyKind.Mechanical,TransferRatingLimit.Power)]
    [InlineData(7,TransferSupplyKind.Mechanical,TransferRatingLimit.Power)]
    [InlineData(11,TransferSupplyKind.Mechanical,TransferRatingLimit.Power)]
    [InlineData(3,TransferSupplyKind.StoredFlow,TransferRatingLimit.Force)]
    [InlineData(7,TransferSupplyKind.StoredFlow,TransferRatingLimit.Force)]
    [InlineData(11,TransferSupplyKind.StoredFlow,TransferRatingLimit.Force)]
    [InlineData(3,TransferSupplyKind.StoredFlow,TransferRatingLimit.Power)]
    [InlineData(7,TransferSupplyKind.StoredFlow,TransferRatingLimit.Power)]
    [InlineData(11,TransferSupplyKind.StoredFlow,TransferRatingLimit.Power)]
    public void RoundedBranchesRespectIndependentRating(int count,TransferSupplyKind kind,TransferRatingLimit limit)
    {
        const double speed=3.1,duration=.03;
        var z=new CollisionVector(0,0,1);
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var drive=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,0,0)),z*speed,default,1,new(1,1,1));
        var receivers=Enumerable.Range(2,count).Select(i=>new PhysicsBody(new(i),PhysicsMotionType.Dynamic,
            RigidPose.At(new(i*2,0,0)),default,default,1,new(1,1,1))).ToArray();
        var bodies=new[]{frame,drive}.Concat(receivers).ToArray();
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],bodies.Select(Object).ToArray(),[],new(default));
        world.InstallEnergyStores([new(frame.Id,100,0)]);world.ChargeEnergyStore(frame.Id,100,1);
        var rating=limit switch
        {
            TransferRatingLimit.Force=>new MechanicalSourceRating(.1,1000),
            TransferRatingLimit.Power=>new MechanicalSourceRating(1000,.1),
            _=>throw new ArgumentOutOfRangeException(nameof(limit))
        };
        var supply=kind switch
        {
            TransferSupplyKind.Mechanical=>new MechanicalTransferSource(new(0),new PointPowerPort(drive.Id,frame.Id,default,z),rating),
            TransferSupplyKind.StoredFlow=>new MechanicalTransferSource(new StoredFlowSource(new(0),frame.Id,speed,rating)),
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var branches=receivers.Select((body,i)=>new MechanicalTransferLoad(new(i),supply,
            new PointPowerPort(body.Id,frame.Id,default,z),new(1,1),1e-10)).ToArray();
        var owned=bodies.ToDictionary(body=>body.Id);
        var paths=bodies.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(duration,default));
        var stores=world.EnergyStores.ToArray().ToDictionary(store=>store.Owner);
        MechanicalTransferEvaluation[] Evaluate()=>MechanicalTransferSource.EvaluateAll(branches,owned,[],owned,paths,duration,stores,new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),
            kind==TransferSupplyKind.Mechanical?new Dictionary<MechanicalSourceId,double>{{supply.Id,1}}:new Dictionary<MechanicalSourceId,double>());
        var result=Evaluate();
        var force=result.Sum(value=>value.Response.Force);
        var power=result.Sum(value=>value.Response.SourcePower);
        Assert.InRange(force,0,rating.MaximumForce);
        Assert.InRange(power,0,rating.MaximumPower);
        var expected=Math.Min(rating.MaximumForce,rating.MaximumPower/speed);
        Assert.InRange(force,expected-1e-14,expected);
        foreach(var branch in result)Assert.Equal(branch.Response.Force*speed,branch.Response.SourcePower);
        Array.Reverse(branches);
        Assert.Equal(result.Select(value=>value.Response),Evaluate().Select(value=>value.Response));
        Assert.Equal(100,world.EnergyStore(frame.Id).Energy);
    }
}
