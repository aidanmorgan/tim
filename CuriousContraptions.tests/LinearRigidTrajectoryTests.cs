using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class LinearRigidTrajectoryTests
{
    [Theory]
    [InlineData(0,0)]
    [InlineData(0,2)]
    [InlineData(.125,2)]
    [InlineData(10,-2)]
    public void MatchesUnforcedFixedOrientationBodyTrajectory(double duration,double speed)
    {
        var start=new RigidPose(new(1,2,3),new(0,Math.Sin(.3),0,Math.Cos(.3)));
        var velocity=new CollisionVector(speed,speed*.5,-speed);
        var expected=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,start,velocity,default).CreateTrajectory(duration,default);
        var actual=new LinearRigidTrajectory(start,velocity,duration);
        foreach(var time in new[]{0,duration*.5,duration})
        {
            Assert.Equal(expected.At(time).Center,actual.At(time).Center);
            var a=actual.At(time).Rotation.Apply(new(1,2,3));
            var b=expected.At(time).Rotation.Apply(new(1,2,3));
            Assert.InRange((a-b).Length,0,1e-14);
            Assert.Equal(expected.LinearVelocityAt(time),actual.LinearVelocityAt(time));
            Assert.Equal(expected.SegmentEndAfter(time),actual.SegmentEndAfter(time));
        }
        Assert.Equal(expected.LinearAccelerationBound,actual.LinearAccelerationBound);
        Assert.Equal(expected.AngularAccelerationBound,actual.AngularAccelerationBound);
        Assert.Equal(expected.AngularSpeedBound,actual.AngularSpeedBound);
    }

    [Fact]
    public void InvalidConstructionAndOutOfHorizonQueriesReject()
    {
        Assert.Throws<ArgumentException>(()=>new LinearRigidTrajectory(default,default,1));
        Assert.Throws<ArgumentException>(()=>new LinearRigidTrajectory(RigidPose.Identity,new(double.NaN,0,0),1));
        Assert.Throws<ArgumentException>(()=>new LinearRigidTrajectory(RigidPose.At(new(double.MaxValue,0,0)),new(double.MaxValue,0,0),2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new LinearRigidTrajectory(RigidPose.Identity,default,-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new LinearRigidTrajectory(RigidPose.Identity,default,double.PositiveInfinity));
        var path=new LinearRigidTrajectory(RigidPose.Identity,default,1);
        foreach(var time in new[]{-1,1.01,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.LinearVelocityAt(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>path.SegmentEndAfter(time));
        }
    }
}
