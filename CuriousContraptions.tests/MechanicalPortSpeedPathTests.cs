using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalPortSpeedPathTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,PhysicsMotionType motion,CollisionVector velocity,CollisionVector spin)=>
        motion==PhysicsMotionType.Dynamic?
            new(new(id),motion,RigidPose.Identity,velocity,spin,1,new(1,1,1)):
            new(new(id),motion,RigidPose.Identity,velocity,spin);
    private static Dictionary<PhysicsBodyId,PhysicsBody> Bodies(PhysicsBody a,PhysicsBody b)=>new(){[a.Id]=a,[b.Id]=b};
    private static Dictionary<PhysicsBodyId,BodyTrajectory> Paths(PhysicsBody a,PhysicsBody b,double duration,BodyWrench wrench)=>
        new(){[a.Id]=a.CreateTrajectory(duration,wrench),[b.Id]=b.CreateTrajectory(duration,default)};
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-10,expected+1e-10);
    private static void CheckDerivatives(MechanicalPortSpeedPath path)
    {
        for(double start=0;start<path.Duration;)
        {
            var end=path.SegmentEndAfter(start);var interval=path.Evaluate(start,end);
            for(var i=0;i<8;i++)
            {
                var lo=start+(end-start)*(i+.1)/8;var hi=start+(end-start)*(i+.9)/8;
                var change=path.Sample(hi).Rate-path.Sample(lo).Rate;
                Assert.InRange(change,-interval.Curvature*(hi-lo)-1e-10,interval.Curvature*(hi-lo)+1e-10);
                var middle=(lo+hi)*.5;var delta=Math.Min(1e-5,(hi-lo)*.1);
                var finiteDifference=(path.At(middle+delta)-path.At(middle-delta))/(2*delta);
                Assert.InRange(path.Sample(middle).Rate,finiteDifference-1e-6,finiteDifference+1e-6);
            }
            start=end;
        }
    }

    [Fact]
    public void AffinePortFindsKnownReversal()
    {
        var a=Body(1,PhysicsMotionType.Dynamic,Z*3,default);
        var b=Body(0,PhysicsMotionType.Static,default,default);
        var path=MechanicalPortSpeedPath.Capture(new PointPowerPort(a.Id,b.Id,default,Z),
            Bodies(a,b),[],Paths(a,b,1,new(-Z*4,default)));
        Near(3,path.At(0));Near(-1,path.At(1));
        Near(-4,path.Sample(.4).Rate);Assert.Equal(0,path.Evaluate(0,1).Curvature);
        var hit=ScalarBoundarySweep.Cast(path,1,1e-8,0);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,.75-1e-8,.75);
        CheckDerivatives(path);
    }

    [Fact]
    public void PositiveEndpointsCannotHideRotatingPointReversal()
    {
        var a=Body(1,PhysicsMotionType.Kinematic,default,Z);
        var b=Body(0,PhysicsMotionType.Static,default,default);
        var path=MechanicalPortSpeedPath.Capture(new PointPowerPort(a.Id,b.Id,X,Y),
            Bodies(a,b),[],Paths(a,b,2*Math.PI,default));
        Near(1,path.At(0));Near(1,path.At(path.Duration));Near(-1,path.At(Math.PI));
        Near(-Math.Sin(.5),path.Sample(.5).Rate);
        var hit=ScalarBoundarySweep.Cast(path,path.Duration,1e-8,0);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,Math.PI/2-2e-8,Math.PI/2);
        CheckDerivatives(path);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge,2)]
    [InlineData(FrameJointKind.Slider,-3)]
    public void AxialSpeedUsesPhysicalStateAndSignedConversion(FrameJointKind kind,double scale)
    {
        var a=Body(1,PhysicsMotionType.Dynamic,Z*3,Z*2);
        var b=Body(0,PhysicsMotionType.Static,default,default);
        var joint=new PhysicsFrameJoint(new(0),kind,a,new(default,RigidRotation.Identity),
            b,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var paths=Paths(a,b,1,new(-Z*4,-Z*4));
        var path=MechanicalPortSpeedPath.Capture(new AxialPowerPort(joint.Id,kind,scale),Bodies(a,b),[joint],paths);
        foreach(var t in new[]{0,.125,.5,.875,1})
            {
                Near(scale*((kind==FrameJointKind.Hinge?2:3)-4*t),path.At(t));
                Near(-4*scale,path.Sample(t).Rate);
            }
        Assert.NotEqual(paths[a.Id].AngularVelocityAt(0).Z,paths[a.Id].PhysicalAngularVelocityAt(0).Z);
        Assert.True(path.SegmentEndAfter(0)<path.Duration);
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.Evaluate(0,path.Duration));
        CheckDerivatives(path);
    }

    [Fact]
    public void MissingForeignAndStalePathsReject()
    {
        var a=Body(1,PhysicsMotionType.Dynamic,Z,default);var b=Body(0,PhysicsMotionType.Static,default,default);
        var port=new PointPowerPort(a.Id,b.Id,default,Z);
        var bodies=Bodies(a,b);var paths=Paths(a,b,1,default);
        var missing=new Dictionary<PhysicsBodyId,BodyTrajectory>{[a.Id]=paths[a.Id]};
        Assert.Throws<ArgumentException>(()=>MechanicalPortSpeedPath.Capture(port,bodies,[],missing));
        var foreign=Paths(a,b,1,default);foreign[a.Id]=b.CreateTrajectory(1,default);
        Assert.Throws<InvalidOperationException>(()=>MechanicalPortSpeedPath.Capture(port,bodies,[],foreign));
        var path=MechanicalPortSpeedPath.Capture(port,bodies,[],paths);
        a.ApplyImpulse(Z,a.Center);
        Assert.Throws<InvalidOperationException>(()=>path.At(0));
        Assert.Throws<InvalidOperationException>(()=>path.Evaluate(0,1));
    }

    [Theory]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.Hinge)]
    public void MovingAnisotropicParticipantsRetainCurvatureBounds(FrameJointKind kind)
    {
        var a=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,
            new(new(2,1,-1),RigidRotation.FromRotationVector(new(.1,.2,.3))),
            new(1,2,3),new(.3,.5,-.1),1,new(1,2,3));
        var b=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,
            new(new(-1,0,2),RigidRotation.FromRotationVector(new(-.2,.4,.1))),
            new(-.2,.3,.4),new(-.2,.4,.1),2,new(2,3,4));
        var joint=new PhysicsFrameJoint(new(0),kind,a,
            new(new(.4,.5,-.2),RigidRotation.FromRotationVector(new(.2,0,0))),b,
            new(new(-.2,.1,.3),RigidRotation.FromRotationVector(new(0,.1,0))),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [a.Id]=a.CreateTrajectory(.2,new(new(1,-2,3),new(3,-2,1))),
            [b.Id]=b.CreateTrajectory(.2,new(new(-3,1,2),new(-1,2,-3)))
        };
        var path=MechanicalPortSpeedPath.Capture(new AxialPowerPort(joint.Id,kind,-.7),Bodies(a,b),[joint],paths);
        CheckDerivatives(path);
    }

    [Fact]
    public void UncertifiableHingeIntervalRejectsRatherThanHidingSingularity()
    {
        var a=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,
            new(default,RigidRotation.FromRotationVector(X*(Math.PI-1e-4))),default,Y,1,new(1,1,1));
        var b=Body(0,PhysicsMotionType.Static,default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,a,new(default,RigidRotation.Identity),
            b,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var path=MechanicalPortSpeedPath.Capture(new AxialPowerPort(joint.Id,FrameJointKind.Hinge,1),
            Bodies(a,b),[joint],Paths(a,b,.01,default));
        Assert.True(double.IsFinite(path.At(0)));
        Assert.Throws<InvalidOperationException>(()=>path.Evaluate(0,.01));
    }
}
