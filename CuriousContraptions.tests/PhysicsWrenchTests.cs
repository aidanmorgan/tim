using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsWrenchTests
{
    private static PhysicsBody Body(int id=0,double mass=2,CollisionVector velocity=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,default,mass,new(.1,.1,.1));
    private static PhysicsObject Object(PhysicsBody body,ConvexGeometry? geometry=null)=>
        new(body,new([new(geometry??new ConvexSphere(.5),AffineTransform.Identity)]),new(1,0,0));
    private static PhysicsObject Wall(int id,double x)=>Object(new(new(id),PhysicsMotionType.Static,
        RigidPose.At(new(x,0,0)),default,default),new ConvexBox(new(.01,10,10)));
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-7);

    [Theory]
    [InlineData(1)]
    [InlineData(.1)]
    [InlineData(.01)]
    public void ForceAndTorqueIntegrateContinuouslyAcrossSubsteps(double maximumStep)
    {
        var body=Body(); var world=new PhysicsWorld([],[Object(body)],[],new(default,maximumStep:maximumStep));
        world.Step([new(body.Id,new(4,2,0),new(0,0,.6))],[],.5);
        Near(1,body.LinearVelocity.X); Near(.5,body.LinearVelocity.Y);
        Near(.3,body.AngularMomentum.Z);
        Near(0,body.AngularMomentum.X); Near(0,body.AngularMomentum.Y);
        Near(.5,world.Time);
        Near(.25,body.Center.X); Near(.125,body.Center.Y);
        var rotated=body.Pose.Rotation.Apply(new(1,0,0));
        Near(Math.Cos(.75),rotated.X); Near(Math.Sin(.75),rotated.Y);
    }

    [Fact]
    public void ExternalBuoyancyCanBalanceGravityWithoutDuplicatingGravity()
    {
        var body=Body(); var world=new PhysicsWorld([],[Object(body)],[],new(new(0,-9.81,0)));
        world.Step([new(body.Id,new(0,19.62,0),default)],[],.5);
        Near(0,body.Center.Length); Near(0,body.LinearVelocity.Length);
    }

    [Fact]
    public void IndependentSourcesAddAndReplayExactly()
    {
        var body=Body(); var reference=Body();
        var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var other=new PhysicsWorld([],[Object(reference)],[],new(default));
        PhysicsWrenchCommand[] commands=[new(body.Id,new(4,0,0),new(0,0,.2)),new(body.Id,new(2,0,0),new(0,0,.4))];
        var before=world.Capture(); var result=world.Step(commands,[],.5); var after=body.Snapshot();
        other.Step([new(reference.Id,new(6,0,0),new(0,0,.2+.4))],[],.5);
        Assert.Equal(after,reference.Snapshot());
        world.Restore(before); Assert.Equal(result,world.Step(commands,[],.5)); Assert.Equal(after,body.Snapshot());
    }

    [Fact]
    public void CollisionEventsIntegrateOnlyTheElapsedForceDuration()
    {
        var body=Body(mass:1,velocity:new(10,0,0));
        var world=new PhysicsWorld([],[Object(body),Wall(1,-2),Wall(2,2)],[],new(default,maximumStep:1));
        var result=world.Step([new(body.Id,new(0,2,0),default)],[],.8);
        Assert.Equal(3,result.Events);
        Near(1.6,body.LinearVelocity.Y); Near(.64,body.Center.Y);
    }

    [Fact]
    public void FailedLaterContactRestoresForceAndTorqueEffects()
    {
        var body=Body(mass:1,velocity:new(10,0,0));
        var world=new PhysicsWorld([],[Object(body),Wall(1,-2),Wall(2,2)],[],new(default,maximumStep:1,maximumEvents:1));
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([new(body.Id,new(0,2,0),new(0,0,.1))],[],.8));
        Assert.Equal(before.BodyStates[0],body.Snapshot()); Near(0,world.Time);
        Assert.Equal(0ul,world.StepIndex); Assert.Equal(0,world.RetainedContactPairs);
    }

    public enum InvalidLoad { Foreign, Static, SummationOverflow }
    [Theory]
    [InlineData(InvalidLoad.Foreign)]
    [InlineData(InvalidLoad.Static)]
    [InlineData(InvalidLoad.SummationOverflow)]
    public void InvalidLoadBatchRejectsBeforeAnyBodyMutation(InvalidLoad kind)
    {
        var body=Body(); var world=new PhysicsWorld([],[Object(body),Wall(1,10)],[],new(default));
        PhysicsWrenchCommand[] commands=kind switch
        {
            InvalidLoad.Foreign=>[new(body.Id,new(1,0,0),default),new(new(99),default,default)],
            InvalidLoad.Static=>[new(body.Id,new(1,0,0),default),new(new(1),default,default)],
            InvalidLoad.SummationOverflow=>[new(body.Id,new(double.MaxValue,0,0),default),new(body.Id,new(double.MaxValue,0,0),default)],
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var before=body.Snapshot();
        Assert.Throws<ArgumentException>(()=>world.Step(commands,[],.1));
        Assert.Equal(before,body.Snapshot()); Near(0,world.Time); Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }

    [Fact]
    public void NonfiniteLoadDeclarationRejectsExplicitly()
    {
        Assert.Throws<ArgumentException>(()=>new PhysicsWrenchCommand(new(0),new(double.NaN,0,0),default));
        Assert.Throws<ArgumentException>(()=>new PhysicsWrenchCommand(new(0),default,new(0,double.PositiveInfinity,0)));
    }

    [Fact]
    public void AppliedLoadParticipatesInTheSharedRopeConstraintSolve()
    {
        var body=new PhysicsBody(new(7),PhysicsMotionType.Dynamic,RigidPose.At(new(0,-2,0)),default,default,2,new(.1,.1,.1));
        var anchor=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var rope=new PhysicsRopeJoint(new(5),new([new(body,default),new(anchor,default)]),2,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[rope],new(default,maximumStep:.1));
        world.Step([new(body.Id,new(0,-10,0),default)],[],.1);
        Near(-2,body.Center.Y); Near(0,body.LinearVelocity.Length);
        Near(2,rope.Route.CurrentLength);
    }

    [Fact]
    public void ForceStillActsWhenCollisionIsDisabled()
    {
        var body=Body(); var world=new PhysicsWorld([],[Object(body)],[],new(default,maximumStep:1));
        var declaration=world.Collider(body.Id).Declaration;
        world.ApplyColliderUpdates([new(body.Id,declaration.Geometry,declaration.Material,CollisionParticipation.Disabled)]);
        world.Step([new(body.Id,new(4,0,0),default)],[],.5);
        Near(1,body.LinearVelocity.X); Near(.25,body.Center.X);
    }
}
