using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PrescribedCollisionTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static readonly CompoundGeometry Sphere=new([new(new ConvexSphere(.25),AffineTransform.Identity)]);
    private static PhysicsBody Driver(int id,PrescribedBodyMotion motion)=>new(new(id),PhysicsMotionType.Kinematic,
        motion.At(0),motion.LinearVelocityAt(0),motion.AngularVelocityAt(0),prescribedMotion:motion);
    private static PhysicsObject Object(PhysicsBody body,CompoundGeometry shape,CollisionParticipation participation=CollisionParticipation.Enabled)=>
        new(body,shape,new(0,0,0)){InitialParticipation=participation};

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedObstacleCannotBeCrossedAndWholeStepRollsBack(bool eased)
    {
        var mover=eased?Driver(0,new(new(RigidPose.At(-X*2),X*4,default,1),RigidPose.Identity,0)):
            new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(-X*2),X*4,default);
        var obstacle=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(mover,Sphere),Object(obstacle,Sphere)],[],new(default,maximumStep:.1));
        var before=mover.Snapshot();
        var error=Assert.Throws<PrescribedMotionConflictException>(()=>world.Step([],[],1));
        Assert.Equal(mover.Id,error.Conflict.Pair.A);Assert.Equal(obstacle.Id,error.Conflict.Pair.B);
        Assert.InRange(error.Conflict.Time,.3,.5);
        Assert.Equal(before,mover.Snapshot());Assert.Equal(0,world.Time);Assert.Equal((ulong)0,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);Assert.Empty(world.Impacts.ToArray());
        var repeat=Assert.Throws<PrescribedMotionConflictException>(()=>world.Step([],[],1));
        Assert.Equal(error.Conflict,repeat.Conflict);
    }

    [Fact]
    public void RotatingMotionCannotHideAnInteriorCollisionBehindClearEndpoints()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.Identity,default,Z*(Math.PI/2),1);
        var mover=Driver(0,new(profile,RigidPose.Identity,0));
        var obstacle=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(1.2,1.2,0)),default,default);
        var beam=new CompoundGeometry([new(new ConvexBox(new(2,.05,.05)),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[Object(mover,beam),Object(obstacle,Sphere)],[],new(default,maximumStep:1));
        var error=Assert.Throws<PrescribedMotionConflictException>(()=>world.Step([],[],1));
        Assert.InRange(error.Conflict.Time,.1,.5);
        Assert.Equal(0,mover.PrescribedMotion!.Time);Assert.Equal(RigidPose.Identity,mover.Pose);
    }

    [Theory]
    [InlineData(CollisionParticipation.Disabled)]
    [InlineData(CollisionParticipation.Enabled)]
    public void DisabledObstacleAndClearPathControls(CollisionParticipation participation)
    {
        var mover=Driver(0,new(new(RigidPose.At(-X*2),X*4,default,1),RigidPose.Identity,0));
        var center=participation==CollisionParticipation.Disabled?default:Y*2;
        var obstacle=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(center),default,default);
        var world=new PhysicsWorld([],[Object(mover,Sphere),Object(obstacle,Sphere,participation)],[],new(default,maximumStep:.1));
        world.Step([],[],1);
        Assert.Equal(X*2,mover.Center);Assert.Equal(1,world.Time);
    }

    [Fact]
    public void SharedFrameKeepsOverlappingRigidChildrenTogether()
    {
        var path=new QuinticRigidTrajectory(RigidPose.Identity,X,Z,1);
        var a=Driver(0,new(path,RigidPose.Identity,0));
        var b=Driver(1,new(path,RigidPose.At(X*.1),0));
        var world=new PhysicsWorld([],[Object(a,Sphere),Object(b,Sphere)],[],new(default,maximumStep:.1));
        world.Step([],[],1);
        Assert.InRange((b.Center-a.Pose.TransformPoint(X*.1)).Length,0,1e-12);
        Assert.Equal(1,world.Time);
    }

    [Fact]
    public void EnablingAnOverlappingPrescribedColliderRejectsAtomically()
    {
        var mover=Driver(0,new(new(RigidPose.Identity,X*2,default,1),RigidPose.Identity,0));
        var obstacle=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(mover,Sphere,CollisionParticipation.Disabled),Object(obstacle,Sphere)],[],new(default,maximumStep:.1));
        var initial=world.Collider(mover.Id);
        Assert.Throws<InvalidOperationException>(()=>world.ApplyColliderUpdates([
            new(mover.Id,Sphere,initial.Declaration.Material,CollisionParticipation.Enabled)]));
        Assert.Equal(initial,world.Collider(mover.Id));
        world.Step([],[],1);
        world.ApplyColliderUpdates([new(mover.Id,Sphere,initial.Declaration.Material,CollisionParticipation.Enabled)]);
        Assert.Equal(CollisionParticipation.Enabled,world.Collider(mover.Id).Declaration.Participation);
        Assert.Equal(X*2,mover.Center);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TouchingBodiesMayRemainStillOrSeparate(int speed)
    {
        var mover=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(X*.5),X*speed,default);
        var obstacle=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(mover,Sphere),Object(obstacle,Sphere)],[],new(default,maximumStep:.1));
        world.Step([],[],.5);
        Assert.InRange(Math.Abs(mover.Center.X-(.5+.5*speed)),0,1e-12);
    }

    [Fact]
    public void IndependentlyPrescribedBodiesCannotCross()
    {
        var a=Driver(0,new(new(RigidPose.At(-X),X*2,default,1),RigidPose.Identity,0));
        var b=Driver(1,new(new(RigidPose.At(X),-X*2,default,1),RigidPose.Identity,0));
        var world=new PhysicsWorld([],[Object(a,Sphere),Object(b,Sphere)],[],new(default,maximumStep:.1));
        Assert.Throws<PrescribedMotionConflictException>(()=>world.Step([],[],1));
        Assert.Equal(-X,a.Center);Assert.Equal(X,b.Center);
    }
}
