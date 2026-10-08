using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class QuinticTrajectoryTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-8)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Fact]
    public void EasingHasExactEndpointsAndBoundedContinuousDerivatives()
    {
        var start=new RigidPose(new(2,3,4),RigidRotation.FromRotationVector(Y*.7));
        var path=new QuinticRigidTrajectory(start,X*3,Z*7,.6);
        Assert.Equal(start,path.At(0));
        Near(start.Center+X*3,path.At(.6).Center);
        Near((RigidRotation.FromRotationVector(Z*7)*start.Rotation).Apply(X),path.At(.6).Rotation.Apply(X));
        foreach(var t in new[]{0,.6})
        {
            Assert.Equal(default,path.LinearVelocityAt(t));Assert.Equal(default,path.LinearAccelerationAt(t));
            Assert.Equal(default,path.AngularVelocityAt(t));Assert.Equal(default,path.AngularAccelerationAt(t));
        }
        const double h=1e-6;
        for(var i=1;i<100;i++)
        {
            var t=.6*i/100;
            Near(path.LinearVelocityAt(t),(path.At(t+h).Center-path.At(t-h).Center)/(2*h),1e-7);
            Near(path.LinearAccelerationAt(t),(path.LinearVelocityAt(t+h)-path.LinearVelocityAt(t-h))/(2*h),1e-6);
            var turn=(path.At(t+h).Rotation*path.At(t-h).Rotation.Inverse()).RotationVector()/(2*h);
            Near(path.AngularVelocityAt(t),turn,1e-7);
            Assert.True(path.LinearAccelerationAt(t).Length<=path.LinearAccelerationBound*(1+1e-12));
            Assert.True(path.AngularVelocityAt(t).Length<=path.AngularSpeedBound*(1+1e-12));
            Assert.True(path.AngularAccelerationAt(t).Length<=path.AngularAccelerationBound*(1+1e-12));
            Assert.Equal(.6,path.SegmentEndAfter(t));
        }
    }

    [Fact]
    public void EndpointNeighbourhoodNeverOvershootsTheAuthoredTranslation()
    {
        var path=new QuinticRigidTrajectory(RigidPose.At(-X*2),X*4,default,1);
        var time=1d;
        for(var i=0;i<1000;i++)
        {
            time=Math.BitDecrement(time);
            Assert.InRange(path.At(time).Center.X,-2,2);
            Assert.InRange(path.At(1-time).Center.X,-2,2);
        }
    }

    [Fact]
    public void InitiallyStationaryEasedTranslationHitsThroughSharedSweep()
    {
        var shape=new ConvexInstance(new ConvexSphere(.1),AffineTransform.Identity);
        var path=new QuinticRigidTrajectory(RigidPose.At(-X*2),X*4,default,1);
        var moving=new ConvexMotion(shape,path);
        var stationary=new ConvexMotion(shape,new ConfigurationTrajectory(RigidPose.Identity,default,default));
        var result=ConvexSweep.Cast(moving,stationary,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,result.Status);
        Assert.InRange(result.Time,0,.5);
        Assert.InRange(Math.Abs(path.At(result.Time).Center.X+.2+ConvexSweep.ContactDistance),0,1e-6);
        var clear=new ConvexMotion(shape,new ConfigurationTrajectory(RigidPose.At(Y),default,default));
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(moving,clear,1,ConvexSweep.ContactDistance).Status);
    }

    [Fact]
    public void VariableSpinFindsInteriorImpactWithBothEndpointPosesClear()
    {
        var bar=new ConvexInstance(new ConvexBox(new(2,.05,.05)),AffineTransform.Identity);
        var path=new QuinticRigidTrajectory(RigidPose.Identity,default,Z*(Math.PI/2),1);
        var moving=new ConvexMotion(bar,path);
        var sphere=new ConvexInstance(new ConvexSphere(.1),AffineTransform.Identity);
        var obstacle=new ConvexMotion(sphere,new ConfigurationTrajectory(RigidPose.At(new(1.2,1.2,0)),default,default));
        Assert.True(ConvexSeparation.Query(moving.At(0),obstacle.At(0)).LowerBound>.5);
        Assert.True(ConvexSeparation.Query(moving.At(1),obstacle.At(1)).LowerBound>.5);
        var hit=ConvexSweep.Cast(moving,obstacle,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,0,.5);
        var bounds=CollisionBounds.Swept(moving,1);
        for(var i=0;i<=100;i++)
        foreach(var direction in new[]{X,-X,Y,-Y,Z,-Z})
        {
            var point=moving.At(i/100d).Support(direction);
            Assert.InRange(point.X,bounds.Minimum.X,bounds.Maximum.X);
            Assert.InRange(point.Y,bounds.Minimum.Y,bounds.Maximum.Y);
            Assert.InRange(point.Z,bounds.Minimum.Z,bounds.Maximum.Z);
        }
    }

    [Fact]
    public void InvalidPathsAndTimesRejectExplicitly()
    {
        foreach(var duration in new[]{0,-1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new QuinticRigidTrajectory(RigidPose.Identity,X,Z,duration));
        Assert.Throws<ArgumentException>(()=>new QuinticRigidTrajectory(default,X,Z,1));
        Assert.Throws<ArgumentException>(()=>new QuinticRigidTrajectory(RigidPose.Identity,X*double.MaxValue,Z,double.Epsilon));
        var path=new QuinticRigidTrajectory(RigidPose.Identity,X,Z,1);
        foreach(var time in new[]{-1,2,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.LinearVelocityAt(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.AngularAccelerationAt(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.SegmentEndAfter(time));
        }
    }
}
