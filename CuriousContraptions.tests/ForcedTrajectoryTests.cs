using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ForcedTrajectoryTests
{
    private static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(CollisionVector center=default,CollisionVector velocity=default)=>
        new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,2,new(2,2,2));
    private static PhysicsBody Fixed(CollisionVector center=default)=>
        new(new(1),PhysicsMotionType.Static,RigidPose.At(center),default,default);
    private static CompoundGeometry Sphere()=>new([new(new ConvexSphere(.1),AffineTransform.Identity)]);
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-9)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Fact]
    public void ForceAndTorqueCommitTheCapturedPrefixAndReplayExactly()
    {
        var body=Body(); var before=body.Snapshot();
        var wrench=new BodyWrench(X*4,Z*4);
        void Run()
        {
            var path=body.CreateTrajectory(1,wrench);
            Near(X*.25,path.At(.5).Center);
            Near(X,path.LinearVelocityAt(.5));
            Near(Z*2,path.AngularMomentumAt(.5));
            Near(RigidRotation.FromRotationVector(Z*.25).Apply(X),path.At(.5).Rotation.Apply(X));
            var pose=path.At(.5);
            body.Advance(path,.5);
            Assert.Equal(pose,body.Pose);
            Near(X,body.LinearVelocity); Near(Z*2,body.AngularMomentum);
            Assert.Throws<InvalidOperationException>(()=>body.Advance(path,.6));
        }
        Run(); var after=body.Snapshot(); body.Restore(before); Run();
        Assert.Equal(after,body.Snapshot());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ForceFromRestCanCreateOrAvoidAnImpact(int sign)
    {
        var body=Body(); var target=Fixed(X);
        var path=body.CreateTrajectory(1,new(X*(sign*4),default));
        var a=new CompoundMotion(Sphere(),path);
        var b=new CompoundMotion(Sphere(),target.CreateTrajectory(1,default));
        var hit=CompoundCollision.Cast(a,b,1,ConvexSweep.ContactDistance);
        if(sign<0) Assert.Equal(ConvexSweepStatus.Clear,hit.Status);
        else
        {
            Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
            Assert.InRange(Math.Abs(hit.Time-Math.Sqrt(.8-ConvexSweep.ContactDistance)),0,1e-6);
            body.Advance(path,hit.Time);
            Near(X*(hit.Time*hit.Time),body.Center);
            Near(X*(2*hit.Time),body.LinearVelocity);
        }
    }

    [Fact]
    public void ReturningPathCannotHideImpactFromHierarchyOrNarrowPhase()
    {
        var body=Body(velocity:X*4); var target=Fixed(X);
        var path=body.CreateTrajectory(1,new(-X*16,default));
        var a=new CompoundMotion(Sphere(),path);
        var b=new CompoundMotion(Sphere(),target.CreateTrajectory(1,default));
        Near(default,path.At(1).Center);
        Assert.True(ConvexDistance.Query(a.Child(new(0)).At(0),b.Child(new(0)).At(0)).LowerBound>.7);
        Assert.True(ConvexDistance.Query(a.Child(new(0)).At(1),b.Child(new(0)).At(1)).LowerBound>.7);
        Assert.Contains(new ColliderChildPair(new(0),new(0)),CompoundCollision.Candidates(a,b,1,0).Pairs);
        var hit=CompoundCollision.Cast(a,b,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(Math.Abs(hit.Time-(1-Math.Sqrt(.2+ConvexSweep.ContactDistance))*.5),0,1e-6);
        var bounds=CollisionBounds.Swept(a.Child(new(0)),1);
        for(var i=0;i<=200;i++)
        {
            var sample=CollisionBounds.Of(a.Child(new(0)).At(i/200.0));
            Assert.True(sample.Minimum.X>=bounds.Minimum.X&&sample.Maximum.X<=bounds.Maximum.X);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void AcceleratingSliderStopsAtItsAnalyticBoundary(int sign)
    {
        var body=Body(); var anchor=Fixed();
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var hit=joint.Sweep([body.CreateTrajectory(1,new(Z*(sign*4),default)),anchor.CreateTrajectory(1,default)],1,1e-7,1e-8);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status);
        Assert.Equal(sign<0?JointBoundary.Lower:JointBoundary.Upper,hit.Boundary);
        Assert.InRange(Math.Abs(hit.Time-Math.Sqrt(.5)),0,1e-7);
    }

    [Fact]
    public void AccelerationCreatesRopeAndDirectionEvents()
    {
        var body=Body(Z*.25); var anchor=Fixed();
        var rope=new PhysicsRopeJoint(new(0),new([new(body,default),new(anchor,default)]),1,ConnectedBodyCollision.Disabled);
        var stop=rope.Sweep([body.CreateTrajectory(1,new(Z*4,default)),anchor.CreateTrajectory(1,default)],1,1e-7,1e-8);
        Assert.Equal(JointBoundary.Upper,stop.Boundary);
        Assert.InRange(Math.Abs(stop.Time-Math.Sqrt(.75)),0,1e-7);
        var moving=Body(velocity:Z);
        var ratchet=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,moving,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Positive);
        var reversal=ratchet.Sweep([moving.CreateTrajectory(1,new(-Z*4,default)),anchor.CreateTrajectory(1,default)],1,1e-7,1e-8);
        Assert.Equal(JointBoundary.Direction,reversal.Boundary);
        Assert.InRange(Math.Abs(reversal.Time-.5),0,1e-8);
    }

    [Fact]
    public void InvalidWrenchesAndPrescribedBodyForcesRejectWithoutMutation()
    {
        Assert.Throws<ArgumentException>(()=>new BodyWrench(new(double.NaN,0,0),default));
        Assert.Throws<ArgumentException>(()=>new BodyWrench(default,new(0,double.PositiveInfinity,0)));
        foreach(var kind in new[]{PhysicsMotionType.Static,PhysicsMotionType.Kinematic})
        {
            var body=new PhysicsBody(new(0),kind,RigidPose.Identity,default,default);
            var before=body.Snapshot();
            Assert.Throws<ArgumentException>(()=>body.CreateTrajectory(1,new(X,default)));
            Assert.Throws<ArgumentException>(()=>body.CreateTrajectory(1,new(default,Z)));
            Assert.Equal(before,body.Snapshot());
        }
        var path=Body().CreateTrajectory(1,new(X,Z));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.LinearVelocityAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.AngularMomentumAt(double.NaN));
    }
}
