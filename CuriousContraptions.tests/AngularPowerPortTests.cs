using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AngularPowerPortTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-9,expected+1e-9);

    [Theory]
    [InlineData(0)]
    [InlineData(.7)]
    [InlineData(2)]
    public void ExplicitReactionCoupleBalancesMomentumAndWork(double angle)
    {
        var rotation=RigidRotation.FromRotationVector(new(0,angle,0));
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,new(default,rotation),new(1,2,3),new(1,2,3),1,new(1,1,1));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-4,0,0)),new(-2,1,4),new(-2,.4,1),1,new(1,1,1));
        var port=new AngularPowerPort(body.Id,frame.Id,Z,.4);
        var bodies=new[]{body,frame}.ToDictionary(value=>value.Id);
        var gradient=port.Bind(bodies,[]);
        Near(.4*CollisionVector.Dot(rotation.Apply(Z),body.AngularVelocity-frame.AngularVelocity),gradient.Speed);
        var force=default(CollisionVector);var torque=default(CollisionVector);
        foreach(var term in gradient.Terms){force+=term.Linear;torque+=term.Angular;}
        Near(0,force.Length);Near(0,torque.Length);
        var common=new PhysicsBody(frame.Id,PhysicsMotionType.Dynamic,frame.Pose,frame.LinearVelocity,body.AngularVelocity,1,new(1,1,1));
        bodies[frame.Id]=common;Near(0,port.Bind(bodies,[]).Speed);
    }

    [Fact]
    public void CapturedPhysicalSpinHasCertifiedDerivativeAndCurvature()
    {
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,new(1,2,3),1,new(1,1,1));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-4,0,0)),default,new(-1,.5,2),1,new(1,1,1));
        var bodies=new[]{body,frame}.ToDictionary(value=>value.Id);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{
            [body.Id]=body.CreateTrajectory(.5,new(default,new(2,-1,-4))),
            [frame.Id]=frame.CreateTrajectory(.5,new(default,new(1,1,2)))};
        var path=MechanicalPortSpeedPath.Capture(new AngularPowerPort(body.Id,frame.Id,Z,-.4),bodies,[],paths);
        for(double start=0;start<path.Duration;)
        {
            var end=path.SegmentEndAfter(start);var interval=path.Evaluate(start,end);
            var a=start+(end-start)*.25;var b=start+(end-start)*.75;var middle=(a+b)*.5;
            var delta=Math.Min(1e-5,(b-a)*.1);
            var finite=(path.At(middle+delta)-path.At(middle-delta))/(2*delta);
            Assert.InRange(path.Sample(middle).Rate,finite-1e-6,finite+1e-6);
            Assert.InRange(path.Sample(b).Rate-path.Sample(a).Rate,-interval.Curvature*(b-a)-1e-9,interval.Curvature*(b-a)+1e-9);
            start=end;
        }
    }

    [Fact]
    public void StoredFlowSpinsRotorAndReactionFrameWithConservedAngularMomentum()
    {
        const double h=.01,radius=.4;
        var rotor=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(2,2,2));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-4,0,0)),default,default,1,new(1,1,1));
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(frame),Object(rotor)],[],new(default,maximumStep:h));
        world.InstallEnergyStores([new(frame.Id,10,0)]);world.ChargeEnergyStore(frame.Id,10,1);
        world.ReplaceLoads(new(){Transfers=[new(new(0),new(new StoredFlowSource(new(0),frame.Id,4,new(100,1000))),
            new AngularPowerPort(rotor.Id,frame.Id,Z,radius),new(1,100),1e-10)]});
        var initial=world.Capture();var result=world.Step([],[],h);
        var force=4/(1+.5*h*radius*radius*1.5);
        Near(radius*force*h/2,rotor.AngularVelocity.Z);
        Near(-radius*force*h,frame.AngularVelocity.Z);
        Near(0,2*rotor.AngularVelocity.Z+frame.AngularVelocity.Z);
        Near(0,rotor.LinearVelocity.Length+frame.LinearVelocity.Length);
        Near(4*force*h,world.EnergyStore(frame.Id).ReleasedEnergy);
        Near(world.EnergyStore(frame.Id).ReleasedEnergy-rotor.KineticEnergy-frame.KineticEnergy,
            Assert.Single(world.TransferUse.ToArray()).PairedWork.Dissipated);
        var final=world.Capture();world.Restore(initial);
        Assert.Equal(result,world.Step([],[],h));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidScaleRejects(double scale)=>Assert.Throws<ArgumentException>(()=>new AngularPowerPort(new(1),new(0),Z,scale));
}
