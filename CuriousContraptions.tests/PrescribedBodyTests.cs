using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PrescribedBodyTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Driver(PrescribedBodyMotion motion)=>new(new(0),PhysicsMotionType.Kinematic,
        motion.At(0),motion.LinearVelocityAt(0),motion.AngularVelocityAt(0),prescribedMotion:motion);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>Assert.InRange((a-b).Length,0,tolerance);

    private sealed record EffectState:PhysicsImpactEffectState;
    private sealed class RejectImpact(PhysicsBodyId id):PhysicsImpactEffect(id)
    {
        public override PhysicsImpactEffectState Capture()=>new EffectState();
        public override void Restore(PhysicsImpactEffectState state)
        {
            if(state is not EffectState)throw new ArgumentException("Foreign effect state.");
        }
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)=>
            throw new InvalidOperationException("Injected impact failure.");
    }

    [Fact]
    public void FailedImpactRollsBackMotionCursorAndBodies()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.At(-X*2),X*2,default,1);
        var driver=Driver(new(profile,RigidPose.Identity,0));
        var ball=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(.1,.1,.1));
        var shape=new CompoundGeometry([new(new ConvexSphere(.25),AffineTransform.Identity)]);
        var world=new PhysicsWorld([new RejectImpact(driver.Id)],[new(driver,shape,new(1,0,0)),new(ball,shape,new(1,0,0))],[],new(default,maximumStep:.1));
        var first=driver.Snapshot();var second=ball.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],1));
        Assert.Equal(first,driver.Snapshot());Assert.Equal(second,ball.Snapshot());
        Assert.Equal(0,world.Time);Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }

    [Fact]
    public void OffsetMotionUsesExactSweptPrefixAndSnapshotCursor()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.Identity,X,Z*2,.8);
        var motion=new PrescribedBodyMotion(profile,new(X,RigidRotation.FromRotationVector(Y*.3)),0);
        var body=Driver(motion);var original=body.Snapshot();var path=body.CreateTrajectory(1,default);
        Assert.Equal(.8,path.SegmentEndAfter(0));Assert.Equal(1,path.SegmentEndAfter(.8));
        var target=motion.At(.23);
        body.Advance(path,.23);
        Assert.Equal(target,body.Pose);Assert.Equal(.23,body.PrescribedMotion!.Time);
        Near(motion.LinearVelocityAt(.23),body.LinearVelocity);
        Near(motion.AngularVelocityAt(.23),body.AngularVelocity);
        Assert.Throws<InvalidOperationException>(()=>body.Advance(path,.1));
        var saved=body.Snapshot();
        body.Advance(body.CreateTrajectory(2,default),2);
        Assert.Equal(profile.Duration,body.PrescribedMotion!.Time);
        Near(default,body.LinearVelocity);Near(default,body.AngularVelocity);
        Assert.Equal(motion.At(2),body.Pose);
        body.Restore(saved);Assert.Equal(saved,body.Snapshot());
        body.Restore(original);Assert.Equal(original,body.Snapshot());
        Assert.Throws<InvalidOperationException>(()=>body.SetKinematicVelocity(X,default));
        Assert.Throws<ArgumentException>(()=>body.Restore(original with {PrescribedMotion=null}));
        Assert.Throws<ArgumentException>(()=>body.Restore(original with {Pose=RigidPose.At(Y)}));
        Assert.Equal(original,body.Snapshot());
    }

    [Fact]
    public void OffsetAccelerationMatchesDerivativeAndSupportReactions()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.Identity,X,Z,1);
        var motion=new PrescribedBodyMotion(profile,new(X,RigidRotation.Identity),.2);
        var driver=Driver(motion);var path=driver.CreateTrajectory(.1,default);
        const double t=.04,h=1e-6;
        Near(path.LinearAccelerationAt(t),(path.LinearVelocityAt(t+h)-path.LinearVelocityAt(t-h))/(2*h),1e-6);
        var follower=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,driver.Pose,driver.LinearVelocity,default,2,new(1,1,1));
        var frame=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,follower,frame,driver,frame,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{driver.Id,default},{follower.Id,default}};
        var solved=AccelerationSolver.Solve([driver,follower],joint.AccelerationConstraints(joint.Bodies.ToArray().ToDictionary(body=>body.Id),1e-7,1e-8),[],loads,1e-9,out _,[],out _,out _,out _);
        Near(motion.LinearAccelerationAt(0)*2,solved[follower.Id].Force);
        Assert.Equal(default,solved[driver.Id]);
    }

    [Fact]
    public void SharedWorldCollidesAgainstProfileAndReplaysItsCursor()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.At(-X*2),X*2,default,1);
        var driver=Driver(new(profile,RigidPose.Identity,0));
        var ball=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(.1,.1,.1));
        var shape=new CompoundGeometry([new(new ConvexSphere(.25),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(driver,shape,new(1,0,0)),new(ball,shape,new(1,0,0))],[],new(default,maximumStep:.1));
        var saved=world.Capture();
        world.Step([],[],1.2);
        Assert.True(ball.LinearVelocity.X>0);
        Assert.Equal(1,driver.PrescribedMotion!.Time);
        var endDriver=driver.Snapshot();var endBall=ball.Snapshot();
        world.Restore(saved);world.Step([],[],1.2);
        Assert.Equal(endDriver,driver.Snapshot());Assert.Equal(endBall,ball.Snapshot());
    }
}
