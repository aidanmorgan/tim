using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvexPoseTests
{
    private static readonly CollisionVector Direction=new(1,2,-3);
    private static ConvexInstance Shape()=>new(new ConvexBox(new(.4,.7,.2)),
        new(SceneGeometryAdapter.CaptureBasis(Basis.FromEuler(new(.2f,-.1f,.3f))),new(.3f,.1f,-.4f)));

    [Theory]
    [InlineData(PhysicsMotionType.Static)]
    [InlineData(PhysicsMotionType.Kinematic)]
    [InlineData(PhysicsMotionType.Dynamic)]
    public void CurrentAndSweptQueriesShareExactSupportWithoutDependingOnVelocity(PhysicsMotionType mode)
    {
        var pose=new RigidPose(new(2,-3,1),RigidRotation.FromRotationVector(new(.2,-.4,.3)));
        var body=new PhysicsBody(new(0),mode,pose,mode==PhysicsMotionType.Static?default:new(2,3,4),
            mode==PhysicsMotionType.Static?default:new(.4,.3,.2),
            mode==PhysicsMotionType.Dynamic?1:0,
            mode==PhysicsMotionType.Dynamic?new InertiaTensor(1,1,1):default);
        var shape=Shape();
        var current=new ConvexPose(shape,body.Pose);
        var moving=new ConvexMotion(shape,body.CreateTrajectory(.01,default));
        Assert.Equal(pose.TransformPoint(shape.Support(pose.Rotation.Inverse().Apply(Direction))),current.Support(Direction));
        Assert.Equal(current.Support(Direction),moving.At(0).Support(Direction));
        Assert.Equal(current.CoreSupport(Direction),moving.At(0).CoreSupport(Direction));
        Assert.Equal(current.InteriorBall,moving.At(0).InteriorBall);
        Assert.Equal(current.RoundingRadius,moving.At(0).RoundingRadius);
        Assert.Equal(current.SupportingFeature(Direction,1e-7).Vertices.ToArray(),
            moving.At(0).SupportingFeature(Direction,1e-7).Vertices.ToArray());
    }

    [Fact]
    public void CapturedPoseDoesNotFollowLaterBodyChanges()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new InertiaTensor(1,1,1));
        var shape=Shape();var captured=new ConvexPose(shape,body.Pose);var before=captured.Support(Direction);
        body.CorrectPose(new(2,0,0),new(0,.2,0));
        Assert.Equal(before,captured.Support(Direction));
        Assert.NotEqual(before,new ConvexPose(shape,body.Pose).Support(Direction));
    }

    [Fact]
    public void InvalidDeclarationsAndUninitializedValuesReject()
    {
        Assert.Throws<ArgumentException>(()=>new ConvexPose(default,RigidPose.Identity));
        Assert.Throws<ArgumentException>(()=>new ConvexPose(Shape(),default));
        var uninitialized=default(ConvexPose);
        Assert.Throws<InvalidOperationException>(()=>uninitialized.Support(Direction));
        Assert.Throws<InvalidOperationException>(()=>uninitialized.CoreSupport(Direction));
        Assert.Throws<InvalidOperationException>(()=>uninitialized.SupportingFeature(Direction,1e-7));
        Assert.Throws<InvalidOperationException>(()=>uninitialized.InteriorBall);
        Assert.Throws<InvalidOperationException>(()=>uninitialized.RoundingRadius);
    }

    [Fact]
    public void RepeatedInstantaneousSupportDoesNotAllocate()
    {
        var shape=Shape();var pose=RigidPose.Identity;var result=default(CollisionVector);
        for(var i=0;i<100;i++)result=new ConvexPose(shape,pose).Support(Direction);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++)result=new ConvexPose(shape,pose).Support(Direction);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        Assert.True(result.IsFinite);
    }
}
