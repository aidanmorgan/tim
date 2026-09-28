using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BodyTrajectoryTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body()=>new(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
        new(.2,.1,-.1),new(.7,1.1,8),2,new InertiaTensor(1,2,3));
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-9)=>Assert.InRange((a-b).Length,0,tolerance);

    [Fact]
    public void SweepPoseAndCommittedPrefixAreExactlyTheSameCapturedTrajectory()
    {
        var body=Body(); var path=body.CreateTrajectory(.4);
        var local=new ConvexInstance(new ConvexBox(new(2,.05,.05)),new(Basis.Identity,new(.3f,.2f,-.1f)));
        var motion=new ConvexMotion(local,path);
        Assert.True(path.SegmentCount>1);
        var expected=path.At(.173);
        foreach(var direction in new[]{X,Y,Z,-X,-Y,-Z})
            Assert.Equal(expected.TransformPoint(local.Support(expected.Rotation.Inverse().Apply(direction))),motion.At(.173).Support(direction));
        body.Advance(path,.173);
        Assert.Equal(expected,body.Pose);
        Assert.Throws<InvalidOperationException>(()=>body.Advance(path,.2));
    }

    [Fact]
    public void CurvatureIntervalsEndAtEveryConstantSpinBoundary()
    {
        var path=Body().CreateTrajectory(.4);
        double time=0; var count=0;
        while(time<path.Duration)
        {
            var end=path.SegmentEndAfter(time);
            Assert.True(end>time);
            var middle=(time+end)*.5;
            var first=path.At(middle).Rotation*path.At(time).Rotation.Inverse();
            var second=path.At(end).Rotation*path.At(middle).Rotation.Inverse();
            Near(first.Apply(X),second.Apply(X),1e-10);
            Near(first.Apply(Y),second.Apply(Y),1e-10);
            Assert.Equal(end,path.SegmentEndAfter(middle));
            time=end; count++;
            Assert.True(count<=path.SegmentCount);
        }
        Assert.Equal(path.SegmentCount,count);
        Assert.Equal(path.Duration,path.SegmentEndAfter(path.Duration));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.SegmentEndAfter(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.SegmentEndAfter(double.NaN));
        Assert.Equal(0,Body().CreateTrajectory(0).SegmentEndAfter(0));
    }

    [Fact]
    public void ChangingVelocityInvalidatesPreviouslySweptMotion()
    {
        var body=Body(); var path=body.CreateTrajectory(.1);
        body.ApplyImpulse(X,body.Center);
        var before=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>body.Advance(path,.1));
        Assert.Equal(before,body.Snapshot());
        var other=Body();
        Assert.Throws<InvalidOperationException>(()=>other.Advance(path,.1));
    }

    [Fact]
    public void AngularBoundCoversEverySegmentIncludingBoundaries()
    {
        var body=Body(); var path=body.CreateTrajectory(.4);
        var point=new CollisionVector(1.5,.2,.1);
        var bound=path.LinearVelocity.Length+path.AngularSpeedBound*point.Length;
        const int samples=2000; const double step=.4/samples;
        for(var i=0;i<samples;i++)
        {
            var from=path.At(i*step).TransformPoint(point);
            var to=path.At((i+1)*step).TransformPoint(point);
            Assert.InRange((to-from).Length,0,bound*step+1e-12);
        }
    }

    [Fact]
    public void AnisotropicIntermediateCollisionIsDetectedAndAdvancedToTheReportedContact()
    {
        var body=Body(); var path=body.CreateTrajectory(.24);
        var beam=new ConvexMotion(new(new ConvexBox(new(2,.05,.05)),Transform3D.Identity),path);
        var target=path.At(.12).TransformPoint(X*1.5);
        var fixedBody=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(target),default,default);
        var obstacle=new ConvexMotion(new(new ConvexSphere(.03),Transform3D.Identity),fixedBody.CreateTrajectory(.24));
        Assert.True(ConvexDistance.Query(beam.At(0),obstacle.At(0)).LowerBound>.01);
        Assert.True(ConvexDistance.Query(beam.At(.24),obstacle.At(.24)).LowerBound>.01);
        var hit=ConvexSweep.Cast(beam,obstacle,.24,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,.02,.12);
        var expected=path.At(hit.Time);
        body.Advance(path,hit.Time);
        Assert.Equal(expected,body.Pose);
        var committedSupport=beam.At(hit.Time);
        var separation=ConvexDistance.Query(committedSupport,obstacle.At(hit.Time));
        Assert.InRange(separation.UpperBound,0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance);
    }

    [Fact]
    public void CompoundChildrenUseTheSameBodyPathAndKeepStableChildIdentity()
    {
        var body=Body(); var path=body.CreateTrajectory(.24);
        var geometry=new CompoundGeometry([
            new(new ConvexBox(new(.1,.1,.1)),new(Basis.Identity,new(0,0,5))),
            new(new ConvexBox(new(2,.05,.05)),Transform3D.Identity)]);
        var moving=new CompoundMotion(geometry,path);
        var target=path.At(.12).TransformPoint(X*1.5);
        var fixedBody=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(target),default,default);
        var obstacle=new CompoundMotion(new([new(new ConvexSphere(.03),Transform3D.Identity)]),fixedBody.CreateTrajectory(.24));
        var hit=CompoundCollision.Cast(moving,obstacle,.24,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.Equal(new ColliderChildId(1),hit.ChildA);
        Assert.Equal(new ColliderChildId(0),hit.ChildB);
    }

    [Fact]
    public void KinematicMultiTurnMotionIsNotReducedToEndpointOrientation()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,Z*(8*Math.PI));
        var path=body.CreateTrajectory(1);
        Near(-X,path.At(.125).TransformPoint(X));
        Near(X,path.At(1).TransformPoint(X));
        Assert.InRange(path.AngularSpeedBound,8*Math.PI-1e-12,8*Math.PI+1e-12);
    }

    [Fact]
    public void InvalidHorizonAndOutOfRangeTimeFailWithoutChangingState()
    {
        var body=Body(); var before=body.Snapshot(); var path=body.CreateTrajectory(.1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(.2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>body.Advance(path,.2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>body.CreateTrajectory(double.NaN));
        Assert.Equal(before,body.Snapshot());
    }
}
