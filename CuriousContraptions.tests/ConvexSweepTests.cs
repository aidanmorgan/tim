using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvexSweepTests
{
    private static ConvexMotion Motion(ConvexGeometry geometry,RigidPose pose,CollisionVector velocity,
        CollisionVector spin,double duration)=>new(new(geometry,Transform3D.Identity),
            new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,velocity,spin).CreateTrajectory(duration));

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(20000)]
    public void PureRotationFindsTheObstacleMissedByPackagedBackendCcd(double spin)
    {
        var position=new CollisionVector(1.5*Math.Cos(.4),1.5*Math.Sin(.4),0);
        var rotating=Motion(new ConvexBox(new(2,.05,.05)),RigidPose.Identity,default,new(0,0,spin),1/spin);
        var fixedBody=Motion(new ConvexBox(new(.05,.05,.05)),RigidPose.At(position),default,default,1/spin);
        var hit=ConvexSweep.Cast(rotating,fixedBody,1/spin,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        // Independent side/box projection equation for this particular fixture.
        // No removed production solver is retained as a test dependency.
        double low=0,high=.4;
        for(var i=0;i<80;i++)
        {
            var angle=(low+high)/2;
            var gap=position.Y*Math.Cos(angle)-position.X*Math.Sin(angle)-
                .05-.05*(Math.Sin(angle)+Math.Cos(angle))-ConvexSweep.ContactDistance;
            if(gap>0) low=angle; else high=angle;
        }
        Assert.InRange(Math.Abs(hit.Time*spin-(low+high)/2),0,1e-6);
        Assert.InRange(hit.Time*spin,.3,.4);
        Assert.InRange(hit.Separation.UpperBound,0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance);
        Assert.InRange(hit.Iterations,1,100);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(400)]
    public void BothMovingBodiesContributeToTheSameTimeOfImpact(double speed)
    {
        var geometry=new ConvexSphere(.5);
        var a=Motion(geometry,RigidPose.At(new(-2,0,0)),new(speed,0,0),default,2/speed);
        var b=Motion(geometry,RigidPose.At(new(2,0,0)),new(-speed,0,0),default,2/speed);
        var hit=ConvexSweep.Cast(a,b,2/speed,ConvexSweep.ContactDistance);
        var expected=(3-ConvexSweep.ContactDistance)/(2*speed);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(Math.Abs(hit.Time-expected)*speed,0,.000001);
        var swapped=ConvexSweep.Cast(b,a,2/speed,ConvexSweep.ContactDistance);
        Assert.InRange(Math.Abs(hit.Time-swapped.Time),0,1e-10);
    }

    [Fact]
    public void MovingApartAndDepthMissAreClearWithoutEndpointAssumptions()
    {
        var beam=Motion(new ConvexBox(new(2,.05,.05)),RigidPose.Identity,default,new(0,0,10),.2);
        var missed=Motion(new ConvexBox(new(.05,.05,.05)),RigidPose.At(new(1,1,2)),default,default,.2);
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(beam,missed,.2,ConvexSweep.ContactDistance).Status);
        var a=Motion(new ConvexSphere(.5),RigidPose.Identity,new(-2,0,0),default,2);
        var b=Motion(new ConvexSphere(.5),RigidPose.At(new(2,0,0)),new(2,0,0),default,2);
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(a,b,2,ConvexSweep.ContactDistance).Status);
    }

    [Fact]
    public void InitialContactNowIncludesCertifiedSignedPenetration()
    {
        var a=Motion(new ConvexBox(new(1,1,1)),RigidPose.Identity,default,default,1);
        var b=Motion(new ConvexSphere(.5),RigidPose.Identity,default,default,1);
        var result=ConvexSweep.Cast(a,b,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.InitialContact,result.Status);
        Assert.Equal(0,result.Time);
        Assert.Equal(ConvexSeparationStatus.Penetrating,result.Separation.Status);
        Assert.InRange(result.Separation.LowerBound,-1.5000001,-1.4999999);
    }

    [Fact]
    public void RotatedHullUsesTheSameSweepWithoutAddingAnAlgorithm()
    {
        var hull=new ConvexHull([new(-1,0,0),new(1,0,0),new(0,1,0),new(0,0,1)]);
        var moving=Motion(hull,new(default,RigidRotation.FromRotationVector(new(.3,.2,.1))),new(8,0,0),new(.2,.4,.6),1);
        var stationary=Motion(new ConvexBox(new(.1,5,5)),RigidPose.At(new(4,0,0)),default,default,1);
        var hit=ConvexSweep.Cast(moving,stationary,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,.3,.6);
    }

    [Fact]
    public void InvalidMotionCannotReturnClear()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        Assert.Throws<ArgumentException>(()=>new ConvexMotion(default,body.CreateTrajectory(1)));
        Assert.Throws<ArgumentNullException>(()=>new ConvexMotion(new(new ConvexSphere(1),Transform3D.Identity),null!));
        var motion=Motion(new ConvexSphere(1),RigidPose.Identity,default,default,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexSweep.Cast(motion,motion,-1,ConvexSweep.ContactDistance));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexSweep.Cast(motion,motion,2,ConvexSweep.ContactDistance));
    }
}
