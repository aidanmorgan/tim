using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalTransferSaturationTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommittedSaturationExitConservesMomentumAndReplays(bool reverse)
    {
        const double duration=1.25;
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*10,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*4,default,1,new(1,1,1));
        var objects=new[]{frame,source,receiver}.Select(Object).ToArray();
        if(reverse)Array.Reverse(objects);
        var world=new PhysicsWorld([],objects,[],new(default,maximumStep:duration));
        world.ReplaceLoads(new(){Transfers=[new(new(0),new(new(0),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000)),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(1,2),1e-10)]});
        var before=world.Capture();
        var result=world.Step([],[],duration);
        Assert.Equal(duration,world.Time);
        Assert.True(result.Events>0);
        // Initial slip 6 decreases at 4 m/s² under the 2 N cap. At t=1 it
        // reaches 2. The remaining .25 s midpoint step gives slip
        // 2*(1-.25)/(1+.25)=1.2; total momentum remains 14.
        Assert.InRange(source.LinearVelocity.Z,7.6-1e-8,7.6+1e-8);
        Assert.InRange(receiver.LinearVelocity.Z,6.4-1e-8,6.4+1e-8);
        Assert.InRange(source.LinearVelocity.Z+receiver.LinearVelocity.Z,14-1e-10,14+1e-10);
        // KE initially 58; saturated loss 8 and remaining midpoint loss .64.
        Assert.InRange(source.KineticEnergy+receiver.KineticEnergy,49.36-1e-7,49.36+1e-7);
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(before);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(0,2)]
    [InlineData(1,0)]
    [InlineData(double.Epsilon,double.MaxValue)]
    public void DisabledOrUnreachableSaturationDoesNotInventAnEvent(double conductance,double maximumForce)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(2,0,0)),Z*2,default);
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Kinematic,RigidPose.At(new(4,0,0)),Z,default);
        var bodies=new[]{frame,source,receiver}.ToDictionary(b=>b.Id);
        var paths=bodies.ToDictionary(e=>e.Key,e=>e.Value.CreateTrajectory(1,default));
        var load=new MechanicalTransferLoad(new(0),new(new(0),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000)),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(conductance,maximumForce),1e-10);
        var result=load.Sweep(bodies,[],paths,1,1e-8,new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),1e-7,TransferSweepStage.Acceptance);
        Assert.Equal(ScalarSweepStatus.Clear,result.Status);
        Assert.Equal(1,result.Time);
    }

    [Theory]
    [InlineData(1,ScalarSweepStatus.Clear)]
    [InlineData(-1,ScalarSweepStatus.Boundary)]
    public void TangentialSaturationEntryRespectsTheFixedAllowance(int sign,ScalarSweepStatus expected)
    {
        var y=new CollisionVector(0,1,0);
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(2,0,0)),y*(2+sign),Z);
        var bodies=new[]{frame,source}.ToDictionary(b=>b.Id);
        var paths=bodies.ToDictionary(e=>e.Key,e=>e.Value.CreateTrajectory(1,default));
        var sourcePort=MechanicalPortSpeedPath.Capture(
            new PointPowerPort(source.Id,frame.Id,new(-sign,0,0),y),bodies,[],paths);
        var support=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(-2,0,0)),default,default);
        bodies.Add(support.Id,support);paths.Add(support.Id,support.CreateTrajectory(1,default));
        var receiverPort=MechanicalPortSpeedPath.Capture(
            new PointPowerPort(frame.Id,support.Id,default,y),bodies,[],paths);
        var boundary=new MechanicalTransferBoundaryPath(sourcePort,receiverPort,MechanicalTransferBoundary.Saturation,new(1,2),0);
        Assert.Equal(2,sourcePort.At(0));
        var result=ScalarBoundarySweep.Cast(boundary,1,1e-8,4e-8);
        Assert.Equal(expected,result.Status);
        Assert.True(result.Time>0);
        if(expected==ScalarSweepStatus.Clear)Assert.Equal(1,result.Time);
        else Assert.InRange(result.Time,0,.001);
    }
}
