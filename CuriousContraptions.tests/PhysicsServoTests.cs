using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsServoTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
    private static (PhysicsWorld World,PhysicsFrameJoint Joint) Fixture(FrameJointKind kind)
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(.1,.1,.1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,
            kind==FrameJointKind.BallSocket?null:new(-1,1),JointTravelDirection.Both);
        return(new([],[Object(a),Object(b)],[joint],new(default)),joint);
    }

    [Theory]
    [InlineData(FrameJointKind.Slider,PhysicsServoMode.Upper)]
    [InlineData(FrameJointKind.Slider,PhysicsServoMode.Lower)]
    [InlineData(FrameJointKind.Hinge,PhysicsServoMode.Upper)]
    [InlineData(FrameJointKind.Hinge,PhysicsServoMode.Lower)]
    public void RampedDriveReversalHoldAndReplayAreWorldOwned(FrameJointKind kind,PhysicsServoMode direction)
    {
        var (world,joint)=Fixture(kind);
        var beforeInstall=world.Capture();
        world.InstallServos([new(joint.Id,2,4,100,100)]);
        var initial=world.Capture();
        Assert.Equal(PhysicsServoMode.Hold,world.Servo(joint.Id).Mode);
        world.Step([],[],.05);Assert.Equal(default,joint.Motion);Assert.Empty(world.MotorUse.ToArray());
        world.Restore(initial);
        var sign=direction==PhysicsServoMode.Upper?1:-1;
        void Run()
        {
            world.SetServoMode(joint.Id,direction);
            var same=world.Joints[0];world.SetServoMode(joint.Id,direction);
            Assert.Same(same,world.Joints[0]);
            world.Step([],[],.05);
            Assert.Equal(sign*.2,world.Servo(joint.Id).CommandSpeed);
            Assert.InRange(Math.Abs(joint.Motion.Speed-sign*.2),0,1e-10);
            Assert.True(Assert.Single(world.MotorUse.ToArray()).SuppliedWork>0);
            world.Step([],[],.05);Assert.Equal(sign*.4,world.Servo(joint.Id).CommandSpeed);
            world.SetServoMode(joint.Id,direction==PhysicsServoMode.Upper?PhysicsServoMode.Lower:PhysicsServoMode.Upper);
            world.Step([],[],.05);Assert.Equal(sign*.2,world.Servo(joint.Id).CommandSpeed);
            world.Step([],[],.05);Assert.Equal(0,world.Servo(joint.Id).CommandSpeed);
            world.Step([],[],.05);Assert.Equal(-sign*.2,world.Servo(joint.Id).CommandSpeed);
            var coordinate=joint.Motion.Coordinate;var work=world.MotorTotal(joint.Id);
            world.SetServoMode(joint.Id,PhysicsServoMode.Hold);
            var held=(PhysicsFrameJoint)world.Joints[0];
            Assert.Equal(new(coordinate,coordinate),held.TravelRange);
            Assert.Equal(0,world.Servo(joint.Id).CommandSpeed);
            world.Step([],[],.05);
            Assert.InRange(Math.Abs(joint.Motion.Speed),0,1e-10);
            Assert.Equal(coordinate,joint.Motion.Coordinate);
            Assert.Empty(world.MotorUse.ToArray());Assert.Equal(work,world.MotorTotal(joint.Id));
        }
        Run();var final=world.Capture();
        world.Restore(initial);Run();
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.Servos.ToArray(),world.Servos.ToArray());
        Assert.Equal(final.MotorTotals.ToArray(),world.MotorTotals.ToArray());
        world.Restore(beforeInstall);Assert.Empty(world.Servos.ToArray());
        Assert.Throws<ArgumentException>(()=>world.Servo(joint.Id));
        Assert.Same(joint,world.Joints[0]);
    }

    [Theory]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.Hinge)]
    public void SupplyIsFiniteAndDirectCommandsCannotBypassTheController(FrameJointKind kind)
    {
        var (world,joint)=Fixture(kind);
        world.InstallServos([new(joint.Id,2,4,1,.1)]);
        foreach(var mode in Enum.GetValues<PhysicsServoMode>())
        {
            world.SetServoMode(joint.Id,mode);
            var before=world.Capture();
            Assert.Throws<ArgumentException>(()=>world.Step([],[new(joint.Id,10,100,100,1000000)],.05));
            Assert.Equal(before.Servos.ToArray(),world.Servos.ToArray());
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        }
        world.Step([],[],.05);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.InRange(use.AbsoluteImpulse,0,.05);
        Assert.InRange(use.SuppliedWork,0,.005);
        Assert.InRange(joint.A.KineticEnergy,0,use.SuppliedWork+1e-12);
        Assert.True(use.SuppliedWork>0);
    }

    [Fact]
    public void InvalidInstallationModesAndPolicyReplacementAreAtomic()
    {
        var (world,joint)=Fixture(FrameJointKind.Slider);
        var declaration=new PhysicsServoDeclaration(joint.Id,2,4,10,10);
        var initial=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.InstallServos([declaration,new(new(99),2,4,10,10)]));
        Assert.Empty(world.Servos.ToArray());Assert.Same(joint,world.Joints[0]);
        Assert.Throws<ArgumentException>(()=>world.InstallServos([declaration,declaration]));
        Assert.Empty(world.Servos.ToArray());
        world.InstallServos([declaration]);var held=world.Joints[0];var state=world.Servo(joint.Id);
        Assert.Throws<ArgumentException>(()=>world.InstallServos([declaration]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.SetServoMode(joint.Id,(PhysicsServoMode)99));
        Assert.Throws<ArgumentException>(()=>world.SetServoMode(new(99),PhysicsServoMode.Hold));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([joint]));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        Assert.Equal(state,world.Servo(joint.Id));Assert.Same(held,world.Joints[0]);
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        var (ball,ballJoint)=Fixture(FrameJointKind.BallSocket);
        Assert.Throws<ArgumentException>(()=>ball.InstallServos([new(ballJoint.Id,1,1,1,1)]));
        foreach(var invalid in new[]{0d,-1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsServoDeclaration(joint.Id,invalid,1,1,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsServoDeclaration(joint.Id,1,invalid,1,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsServoDeclaration(joint.Id,1,1,invalid,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsServoDeclaration(joint.Id,1,1,1,invalid));
        }
    }

    [Fact]
    public void FailedStepRestoresControllerMemoryJointsBodiesAndWork()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(.1,.1,.1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var c=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),new(0,0,2),default,1,new(.1,.1,.1));
        var d=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(new(2,0,0)),default,default);
        var first=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var second=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,c,Origin,d,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b),Object(c),Object(d)],[first,second],
            new(default,maximumStep:1,maximumEvents:1));
        world.InstallServos([new(first.Id,100,10000,100,10000)]);
        world.SetServoMode(first.Id,PhysicsServoMode.Upper);world.Step([],[],.01);
        var before=world.Capture();var joints=world.Joints.ToArray();var reports=world.MotorUse.ToArray();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],1));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Servos.ToArray(),world.Servos.ToArray());
        Assert.Equal(before.MotorTotals.ToArray(),world.MotorTotals.ToArray());
        Assert.Equal(joints,world.Joints.ToArray());Assert.Equal(reports,world.MotorUse.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
    }
    [Theory]
    [InlineData(FrameJointKind.Slider,PhysicsServoEndpoint.Lower)]
    [InlineData(FrameJointKind.Slider,PhysicsServoEndpoint.Upper)]
    [InlineData(FrameJointKind.Hinge,PhysicsServoEndpoint.Lower)]
    [InlineData(FrameJointKind.Hinge,PhysicsServoEndpoint.Upper)]
    public void EndpointReadsFullAuthoredRangeThroughHoldMotionAndRestore(FrameJointKind kind,PhysicsServoEndpoint endpoint)
    {
        var (world,joint)=Fixture(kind);
        world.InstallServos([new(joint.Id,2,4,100,100)]);
        var initial=world.Capture();
        void CheckBoundary(double distance)
        {
            var servo=world.Servo(joint.Id);
            Assert.False(servo.AtEndpoint(endpoint,0));
            Assert.False(servo.AtEndpoint(endpoint,Math.BitDecrement(distance)));
            Assert.True(servo.AtEndpoint(endpoint,distance));
            Assert.True(servo.AtEndpoint(endpoint,Math.BitIncrement(distance)));
        }
        CheckBoundary(1);
        world.SetServoMode(joint.Id,endpoint==PhysicsServoEndpoint.Lower?PhysicsServoMode.Lower:PhysicsServoMode.Upper);
        world.Step([],[],.05);
        var coordinate=joint.Motion.Coordinate;
        Assert.NotEqual(0,coordinate);
        var distance=endpoint==PhysicsServoEndpoint.Lower?coordinate+1:1-coordinate;
        CheckBoundary(distance);
        world.SetServoMode(joint.Id,PhysicsServoMode.Hold);
        Assert.Equal(new(coordinate,coordinate),((PhysicsFrameJoint)world.Joints[0]).TravelRange);
        CheckBoundary(distance); // A held joint's zero-width range is not an endpoint signal.
        world.Restore(initial);CheckBoundary(1);
        var state=world.Servo(joint.Id);
        Assert.Throws<ArgumentOutOfRangeException>(()=>state.AtEndpoint((PhysicsServoEndpoint)999,0));
        foreach(var invalid in new[]{-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>state.AtEndpoint(endpoint,invalid));
        Assert.Throws<InvalidOperationException>(()=>default(PhysicsServoState).AtEndpoint(endpoint,0));
    }

}
