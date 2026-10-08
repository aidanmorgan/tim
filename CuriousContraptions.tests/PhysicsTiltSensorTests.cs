using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsTiltSensorTests
{
    private static readonly CompoundGeometry Geometry=new([new(new ConvexSphere(.01),AffineTransform.Identity)]);
    private const double Threshold=.7071067811865476;
    private static PhysicsBody Body(int index,PhysicsMotionType motion,CollisionVector spin,RigidRotation rotation)=>
        new(new(index),motion,new(new(index*3,0,0),rotation),default,spin,
            motion==PhysicsMotionType.Dynamic?1:0,motion==PhysicsMotionType.Dynamic?new(1,1,1):default);

    [Theory]
    [InlineData(PhysicsMotionType.Dynamic,true)]
    [InlineData(PhysicsMotionType.Dynamic,false)]
    [InlineData(PhysicsMotionType.Kinematic,true)]
    [InlineData(PhysicsMotionType.Kinematic,false)]
    public void CapturedPathDetectsTipAndReturnButRejectsAxialSpin(PhysicsMotionType motion,bool tip)
    {
        var rotation=RigidRotation.FromRotationVector(new(.2,.3,-.4));
        var body=Body(0,motion,rotation.Apply(tip?new(8,0,0):new(0,8,0)),rotation);
        var world=new PhysicsWorld([],[new(body,Geometry,new(0,0,0))],[],new(default,maximumStep:1));
        world.InstallTiltSensors([new(body.Id,new(0,2,0),Threshold)]);
        var initial=world.Capture();var duration=Math.Tau/8;
        var result=world.Step([],[],duration);
        var state=world.TiltState(body.Id);
        Assert.Equal(tip?PhysicsTiltPhase.Triggered:PhysicsTiltPhase.Waiting,state.Phase);
        // Both endpoints are upright; endpoint-only sampling would miss the tip.
        Assert.InRange((body.Pose.Rotation.Apply(new(0,1,0))-state.ReferenceDirection).Length,0,1e-10);
        if(tip)Assert.InRange(Math.Abs(state.TriggerTime!.Value-Math.PI/32),0,1e-8);
        else Assert.Null(state.TriggerTime);
        Assert.Equal(tip?1:0,result.Events);
        var after=world.Capture();
        world.Restore(initial);
        Assert.Equal(initial.TiltStates.ToArray(),world.TiltStates.ToArray());
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(after.TiltStates.ToArray(),world.TiltStates.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        world.Step([],[],duration);
        Assert.Equal(state,world.TiltState(body.Id)); // One-shot, no duplicate pulse.
    }

    [Fact]
    public void DisabledParticipationDoesNotTriggerAndReenableUsesOriginalReference()
    {
        var body=Body(0,PhysicsMotionType.Kinematic,new(2,0,0),RigidRotation.Identity);
        var world=new PhysicsWorld([],[new(body,Geometry,new(0,0,0))],[],new(default,maximumStep:1));
        world.InstallTiltSensors([new(body.Id,new(0,1,0),Threshold)]);
        world.ApplyColliderUpdates([new(body.Id,Geometry,new(0,0,0),CollisionParticipation.Disabled)]);
        world.Step([],[],.5);
        Assert.Equal(PhysicsTiltPhase.Waiting,world.TiltState(body.Id).Phase);
        world.ApplyColliderUpdates([new(body.Id,Geometry,new(0,0,0),CollisionParticipation.Enabled)]);
        world.Step([],[],.1);
        Assert.Equal(PhysicsTiltPhase.Triggered,world.TiltState(body.Id).Phase);
        Assert.Equal(.5,world.TiltState(body.Id).TriggerTime);
    }

    [Fact]
    public void InstallationAndSnapshotMembershipAreAtomic()
    {
        var a=Body(0,PhysicsMotionType.Dynamic,default,RigidRotation.Identity);
        var b=Body(1,PhysicsMotionType.Dynamic,default,RigidRotation.Identity);
        var fixedBody=Body(2,PhysicsMotionType.Static,default,RigidRotation.Identity);
        var world=new PhysicsWorld([],[new(a,Geometry,new(0,0,0)),new(b,Geometry,new(0,0,0)),new(fixedBody,Geometry,new(0,0,0))],[],new(default));
        var empty=world.Capture();
        world.InstallTiltSensors([new(a.Id,new(0,1,0),Threshold)]);
        var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.InstallTiltSensors([new(b.Id,new(0,1,0),Threshold),new(a.Id,new(0,1,0),Threshold)]));
        Assert.Throws<ArgumentException>(()=>world.InstallTiltSensors([new(new(99),new(0,1,0),Threshold)]));
        Assert.Throws<ArgumentException>(()=>world.InstallTiltSensors([new(fixedBody.Id,new(0,1,0),Threshold)]));
        Assert.Throws<ArgumentNullException>(()=>world.InstallTiltSensors([null!]));
        Assert.Throws<ArgumentNullException>(()=>world.InstallTiltSensors(null!));
        Assert.Equal(before.TiltStates.ToArray(),world.TiltStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.TiltState(b.Id));
        world.Restore(empty);Assert.Empty(world.TiltStates.ToArray());
        world.Restore(before);Assert.Equal(before.TiltStates.ToArray(),world.TiltStates.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidThresholdsAreRejected(double threshold)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsTiltSensor(new(0),new(0,1,0),threshold));

    [Fact]
    public void InvalidDirectionsAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>new PhysicsTiltSensor(new(0),default,Threshold));
        Assert.Throws<ArgumentException>(()=>new PhysicsTiltSensor(new(0),new(double.NaN,0,0),Threshold));
    }

    [Fact]
    public void LaterEventBudgetFailureRestoresEarlierCommittedMotionAndSensorState()
    {
        var a=Body(0,PhysicsMotionType.Kinematic,new(2,0,0),RigidRotation.Identity);
        var b=Body(1,PhysicsMotionType.Kinematic,new(2,0,0),RigidRotation.Identity);
        var world=new PhysicsWorld([],[new(a,Geometry,new(0,0,0)),new(b,Geometry,new(0,0,0))],[],new(default,maximumStep:.05,maximumEvents:1));
        world.InstallTiltSensors([new(a.Id,new(0,1,0),Threshold),new(b.Id,new(0,1,0),Threshold)]);
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.5));
        Assert.Equal(before.TiltStates.ToArray(),world.TiltStates.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        world.Step([],[],.1);Assert.True(world.Time>0);
    }
}
