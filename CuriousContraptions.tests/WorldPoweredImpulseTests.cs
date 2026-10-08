using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class WorldPoweredImpulseTests
{
    private static readonly CollisionVector Axis=new(1,0,0);
    private static PhysicsBody Body(int id,CollisionVector center,double mass=1,CollisionVector velocity=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,mass,new(mass,mass,mass));
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.5),AffineTransform.Identity)]),new(0,0,0));
    private static double Energy(PhysicsBody body)=>
        .5*body.LinearVelocity.LengthSquared/body.InverseMass+
        .5*CollisionVector.Dot(body.AngularMomentum,body.AngularVelocity);
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-9);

    [Fact]
    public void OffCentreActuationAccountsForLinearAndAngularWorkAndReplays()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(2,3,4)),
            default,default,2,new(.5,1,2));
        var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var initial=world.Capture();
        var axis=new CollisionVector(0,1,0);var point=body.Center+Axis;
        var result=world.ApplyPoweredImpulse(body.Id,axis,point,100,100,8);
        Near(4,result.Impulse);Near(8,result.SuppliedWork);Near(0,result.DissipatedWork);
        Near(2,body.LinearVelocity.Y);Near(2,body.AngularVelocity.Z);Near(8,Energy(body));
        Assert.Equal(initial.BodyStates[0].Pose,body.Pose);
        Assert.Equal(0,world.Time);Assert.Equal(0ul,world.StepIndex);
        var expected=world.Capture();
        world.Restore(initial);
        Assert.Equal(result,world.ApplyPoweredImpulse(body.Id,axis,point,100,100,8));
        Assert.Equal(expected.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoupledJointUsesBothMassesAndIsIndependentOfBodyDeclarationOrder(bool reverse)
    {
        var a=Body(0,-Axis);var b=Body(1,Axis,3);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,new(Axis,RigidRotation.Identity),
            b,new(-Axis,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],reverse?[Object(b),Object(a)]:[Object(a),Object(b)],[joint],new(default));
        var initial=world.Capture();
        var result=world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8);
        Near(8,result.Impulse);Near(8,result.SuppliedWork);
        Near(2,a.LinearVelocity.X);Near(2,b.LinearVelocity.X);
        Near(8,Energy(a)+Energy(b));
        world.Step([],[],.01);
        var final=world.Capture();
        world.Restore(initial);
        Assert.Equal(result,world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8));
        world.Step([],[],.01);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(CollisionParticipation.Enabled)]
    [InlineData(CollisionParticipation.Disabled)]
    public void CurrentContactParticipationControlsTheCoupledResponse(CollisionParticipation participation)
    {
        var a=Body(0,default);var b=Body(1,Axis,3);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[],new(default));
        var collider=world.Collider(b.Id).Declaration;
        world.ApplyColliderUpdates([new(b.Id,collider.Geometry,collider.Material,participation)]);
        var before=world.Capture();
        var use=world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8);
        Near(8,use.SuppliedWork);Near(8,Energy(a)+Energy(b));
        Near(participation==CollisionParticipation.Enabled?2:4,a.LinearVelocity.X);
        Near(participation==CollisionParticipation.Enabled?2:0,b.LinearVelocity.X);
        var after=world.Capture();
        world.Restore(before);
        Assert.Equal(use,world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8));
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void ContactBlocksClosingActuationButAllowsReleaseWithoutSpendingBlockedWork()
    {
        var a=Body(0,default);
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(Axis),default,default);
        var world=new PhysicsWorld([],[Object(a),Object(wall)],[],new(default));
        var before=a.Snapshot();
        Assert.Equal(default,world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8));
        Assert.Equal(before,a.Snapshot());
        var release=world.ApplyPoweredImpulse(a.Id,-Axis,a.Center,100,100,8);
        Near(8,release.SuppliedWork);Near(-4,a.LinearVelocity.X);
    }

    [Fact]
    public void FixedTranslationCannotConsumeLaunchWork()
    {
        var a=Body(0,default);
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var frame=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,frame,anchor,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(anchor)],[joint],new(default));
        Assert.Equal(default,world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8));
        Assert.Equal(default,a.LinearVelocity);Assert.Equal(default,a.AngularMomentum);
    }

    [Fact]
    public void InfeasibleContactRejectsAtomicallyIncludingNewContactCache()
    {
        var a=Body(0,default,velocity:Axis);
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(Axis),default,default);
        var world=new PhysicsWorld([],[Object(a),Object(wall)],[],new(default));
        var before=world.Capture();
        var pairs=world.RetainedContactPairs;
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(a.Id,Axis,a.Center,100,100,8));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(pairs,world.RetainedContactPairs);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
    }

    [Theory]
    [InlineData(PhysicsMotionType.Static)]
    [InlineData(PhysicsMotionType.Kinematic)]
    public void NonDynamicTargetsReject(PhysicsMotionType motion)
    {
        var body=new PhysicsBody(new(0),motion,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var initial=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis,default,1,1,1));
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void InvalidCommandsRejectBeforeMutation()
    {
        var body=Body(0,default);var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(new(99),Axis,default,1,1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,default,default,1,1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis*2,default,1,1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,new(double.NaN,0,0),default,1,1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis,new(0,double.NaN,0),1,1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis,default,double.NaN,1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis,default,1,-1,1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis,default,1,1,-1));
        Assert.Throws<ArgumentException>(()=>world.ApplyPoweredImpulse(body.Id,Axis,default,1,double.PositiveInfinity,1));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(0,world.RetainedContactPairs);
    }

    [Fact]
    public void TargetAndImpulseCapsPreserveUnusedEnergy()
    {
        var body=Body(0,default);var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var initial=world.Capture();
        var capped=world.ApplyPoweredImpulse(body.Id,Axis,body.Center,2,100,100);
        Near(2,body.LinearVelocity.X);Near(2,capped.SuppliedWork);
        world.Restore(initial);
        var limited=world.ApplyPoweredImpulse(body.Id,Axis,body.Center,100,1,100);
        Near(1,body.LinearVelocity.X);Near(.5,limited.SuppliedWork);
        world.Restore(initial);
        Assert.Equal(default,world.ApplyPoweredImpulse(body.Id,Axis,body.Center,100,100,0));
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }
}
