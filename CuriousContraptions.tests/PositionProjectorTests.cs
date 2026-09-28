using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PositionProjectorTests
{
    private static readonly ConvexInstance Sphere=new(new ConvexSphere(.1),Transform3D.Identity);
    private static readonly ConvexInstance Wall=new(new ConvexBox(new(.001,2,2)),Transform3D.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,PhysicsMotionType type=PhysicsMotionType.Dynamic)=>
        type==PhysicsMotionType.Dynamic?
            new(new(id),type,RigidPose.At(center),new(.1,.2,.3),new(.2,.3,.4),1,new(.1,.1,.1)):
            new(new(id),type,RigidPose.At(center),default,default);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-7)=>Assert.InRange((a-b).Length,0,tolerance);

    [Fact]
    public void TranslationCannotCrossAThinWallDespiteClearEndpointsAndCanThenSeparate()
    {
        var a=Body(0,new(-1,0,0)); var b=Body(1,default,PhysicsMotionType.Static);
        var contact=new ContactPositionConstraint(a,Sphere,b,Wall);
        var projector=new PositionProjector([a,b],[contact],1e-6);
        var before=a.Snapshot();
        var fraction=projector.Apply([new(a,new(2,0,0),default)]);
        Assert.InRange(fraction,.449,.451);
        Assert.InRange(a.Center.X,-.1010001,-.1009989);
        Assert.Equal(before.LinearVelocity,a.LinearVelocity); Assert.Equal(before.AngularMomentum,a.AngularMomentum);
        Assert.Equal(1,projector.ClippedCorrections);
        Assert.Equal(1,projector.Apply([new(a,new(-.2,0,0),default)]));
        Assert.True(a.Center.X<-.3);
    }

    [Fact]
    public void SimultaneousCorrectionsStopOnOneSharedClock()
    {
        var a=Body(0,new(-1,0,0)); var b=Body(1,new(1,0,0));
        var projector=new PositionProjector([a,b],[new(a,Sphere,b,Sphere)],1e-6);
        var fraction=projector.Apply([new(a,new(2,0,0),default),new(b,new(-2,0,0),default)]);
        Assert.InRange(fraction,.449,.451);
        Near(default,a.Center+b.Center);
        Assert.True(a.Center.X<0); Assert.True(b.Center.X>0);
        Assert.InRange((a.Center-b.Center).Length,.1999989,.2000001);
    }

    [Fact]
    public void FullTurnCorrectionDoesNotCollapseToItsClearEndpoint()
    {
        var a=Body(0,default);
        var b=Body(1,new(1.5*Math.Cos(.4),1.5*Math.Sin(.4),0),PhysicsMotionType.Static);
        var beam=new ConvexInstance(new ConvexBox(new(2,.02,.02)),Transform3D.Identity);
        var obstacle=new ConvexInstance(new ConvexSphere(.03),Transform3D.Identity);
        var spin=new CollisionVector(0,0,Math.Tau);
        var path=new ConfigurationTrajectory(a.Pose,default,spin);
        var moving=new ConvexMotion(beam,path);
        var fixedMotion=new ConvexMotion(obstacle,new ConfigurationTrajectory(b.Pose,default,default));
        Assert.True(ConvexDistance.Query(moving.At(0),fixedMotion.At(0)).LowerBound>.1);
        Assert.True(ConvexDistance.Query(moving.At(1),fixedMotion.At(1)).LowerBound>.1);
        var projector=new PositionProjector([a,b],[new(a,beam,b,obstacle)],1e-6);
        var fraction=projector.Apply([new(a,default,spin)]);
        Assert.InRange(fraction,.04,.07);
        Assert.Equal(path.At(fraction),a.Pose);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldJointCorrectionCannotTeleportThroughAnObstacle(bool blocked)
    {
        var a=Body(0,new(-1,0,0)); var anchor=Body(1,new(1,0,0),PhysicsMotionType.Static);
        var wall=Body(2,new(0,blocked?0:3,0),PhysicsMotionType.Static);
        var origin=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,origin,anchor,origin,ConnectedBodyCollision.Disabled,null);
        PhysicsObject Object(PhysicsBody body,ConvexInstance shape)=>new(body,new([shape]),new(0,0,0));
        var world=new PhysicsWorld([Object(a,Sphere),Object(anchor,Sphere),Object(wall,Wall)],[joint],new(default));
        var before=world.Capture();
        if(blocked)
        {
            Assert.Throws<InvalidOperationException>(()=>world.Step(.01));
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(0,world.Time); Assert.Equal(0ul,world.StepIndex);
        }
        else
        {
            world.Step(.01);
            Near(anchor.Center,a.Center);
            Assert.InRange(joint.Error(1e-8),0,1e-7);
        }
    }

    [Fact]
    public void InvalidCorrectionBatchesCannotPartiallyMutateBodies()
    {
        var a=Body(0,default); var b=Body(1,new(2,0,0),PhysicsMotionType.Static);
        var foreign=Body(0,default);
        var projector=new PositionProjector([a,b],[],1e-6);
        var before=a.Snapshot();
        Assert.Throws<ArgumentException>(()=>projector.Apply([new(a,new(1,0,0),default),new(b,new(1,0,0),default)]));
        Assert.Throws<ArgumentException>(()=>projector.Apply([new(a,new(1,0,0),default),new(a,default,default)]));
        Assert.Throws<ArgumentException>(()=>projector.Apply([new(foreign,default,default)]));
        Assert.Throws<ArgumentException>(()=>projector.Apply([new(a,new(double.NaN,0,0),default)]));
        Assert.Equal(before,a.Snapshot());
    }

    [Fact]
    public void ChangedObstaclePoseInvalidatesAnExistingProjectionTransaction()
    {
        var a=Body(0,new(-1,0,0)); var b=Body(1,default,PhysicsMotionType.Static);
        var projector=new PositionProjector([a,b],[new(a,Sphere,b,Wall)],1e-6);
        var before=a.Snapshot();
        b.Restore(b.Snapshot() with {Pose=RigidPose.At(new(10,0,0))});
        Assert.Throws<InvalidOperationException>(()=>projector.Apply([new(a,new(2,0,0),default)]));
        Assert.Equal(before,a.Snapshot());
    }

    [Fact]
    public void ConfigurationPathValidatesItsDomainAndPreservesUnwrappedSpin()
    {
        var path=new ConfigurationTrajectory(RigidPose.Identity,new(1,2,3),new(0,0,4*Math.PI));
        Near(new(.25,.5,.75),path.At(.25).Center);
        Near(new(-1,0,0),path.At(.25).Rotation.Apply(new(1,0,0)));
        Assert.Equal(4*Math.PI,path.AngularSpeedBound);
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(-.1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.SegmentEndAfter(double.NaN));
        Assert.Throws<ArgumentException>(()=>new ConfigurationTrajectory(default,default,default));
    }
}
