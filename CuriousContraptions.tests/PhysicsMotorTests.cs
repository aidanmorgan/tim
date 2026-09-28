using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsMotorTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),Transform3D.Identity)]),new(0,0,0));
    private static PhysicsFrameJoint Joint(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointTravelRange? range=null)=>
        new(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,range);
    private static readonly ConstraintJacobian Linear=new(new(0,0,1),default,new(0,0,-1),default);

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void FiniteWorkCapsAccelerationAndDoesNotSpendBrakingEnergy(int sign)
    {
        var a=Body(0,new(0,0,-sign*3)); var b=Fixed(1);
        var result=PoweredImpulse.Apply(a,b,Linear,sign*10,100,.5);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-sign),0,1e-12);
        Assert.InRange(result.SuppliedWork,0,.5);
        Assert.InRange(Math.Abs(result.DissipatedWork-4.5),0,1e-12);
        Assert.InRange(Math.Abs(result.Impulse-sign*4),0,1e-12);
    }

    [Fact]
    public void ZeroSupplyAllowsBrakingButNotReverseAcceleration()
    {
        var a=Body(0,new(0,0,3)); var b=Fixed(1);
        var result=PoweredImpulse.Apply(a,b,Linear,-10,100,0);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z),0,1e-12);
        Assert.Equal(0,result.SuppliedWork);
        Assert.InRange(Math.Abs(result.DissipatedWork-4.5),0,1e-12);
        var before=a.Snapshot();
        Assert.Equal(default,PoweredImpulse.Apply(a,b,Linear,10,0,10));
        Assert.Equal(before,a.Snapshot());
    }

    [Fact]
    public void TwoDynamicBodiesReceiveOppositeMomentumAndAccountForTheirTotalWork()
    {
        var a=Body(0); var b=Body(1);
        var result=PoweredImpulse.Apply(a,b,Linear,10,100,1);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-1),0,1e-12);
        Assert.Equal(-a.LinearVelocity,b.LinearVelocity);
        Assert.InRange(Math.Abs(a.KineticEnergy+b.KineticEnergy-result.SuppliedWork),0,1e-12);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void WorldSharesSupplyAcrossSubstepsAndReplaysExactly(FrameJointKind kind)
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(kind,a,b);
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default));
        PhysicsMotorCommand[] commands=[new(joint.Id,10,2,.5)];
        var before=world.Capture(); world.Step(commands,.5);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.InRange(use.SuppliedWork,0,.5); Assert.InRange(use.AbsoluteImpulse,0,1);
        Assert.InRange(a.KineticEnergy,0,.500000000001);
        var expected=kind==FrameJointKind.Hinge?Math.Sqrt(10):1;
        Assert.InRange(Math.Abs(a.LinearVelocity.Z+a.AngularVelocity.Z-expected),0,1e-10);
        var after=world.Capture(); var report=world.MotorUse.ToArray();
        world.Restore(before); Assert.Empty(world.MotorUse.ToArray());
        world.Step(commands,.5);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(report,world.MotorUse.ToArray());
    }

    [Fact]
    public void StopEventAndTerminalSolveCannotReapplyTheMotorKick()
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b,new(-.02,.02));
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default,maximumStep:.1));
        world.Step([new(joint.Id,1,100,100)],.1);
        Assert.Single(world.JointStops.ToArray());
        Assert.InRange(Math.Abs(a.Center.Z-.02),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length,0,1e-8);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.InRange(Math.Abs(use.AbsoluteImpulse-1),0,1e-12);
        Assert.InRange(Math.Abs(use.SuppliedWork-.5),0,1e-12);
    }

    [Fact]
    public void FailedStepRestoresBodiesClockAndThePreviousMotorReport()
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b,new(-.5,.5));
        var c=Body(2,new(0,0,2)); c.Restore(c.Snapshot() with {Pose=RigidPose.At(new(2,0,0))});
        var d=Fixed(3); d.Restore(d.Snapshot() with {Pose=RigidPose.At(new(2,0,0))});
        var second=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,c,Origin,d,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var world=new PhysicsWorld([Object(a),Object(b),Object(c),Object(d)],[joint,second],new(default,maximumStep:1,maximumEvents:1));
        world.Step([new(joint.Id,1,1,1)],.01);
        var before=world.Capture(); var reports=world.MotorUse.ToArray();
        Assert.Throws<InvalidOperationException>(()=>world.Step([new(joint.Id,100,100,100)],1));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time); Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(reports,world.MotorUse.ToArray());
    }

    [Fact]
    public void InvalidAndDuplicateCommandsFailBeforeMutation()
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b);
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default));
        var command=new PhysicsMotorCommand(joint.Id,1,1,1); var before=a.Snapshot();
        Assert.Throws<ArgumentException>(()=>world.Step([command,command],.01));
        Assert.Throws<ArgumentException>(()=>world.Step([new(new(99),1,1,1)],.01));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(joint.Id,double.NaN,1,1));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(joint.Id,1,-1,1));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(joint.Id,1,1,-1));
        Assert.Throws<ArgumentException>(()=>PoweredImpulse.Apply(a,b,default,1,1,1));
        Assert.Equal(before,a.Snapshot()); Assert.Equal(0,world.Time);
    }

    [Fact]
    public void RandomSignedOffCentreAnisotropicUpdatesRespectBothBudgets()
    {
        var random=new Random(3801);
        double Number()=>2*random.NextDouble()-1;
        CollisionVector Vector()=>new(Number(),Number(),Number());
        for(var i=0;i<500;i++)
        {
            var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,Vector()*4,Vector()*3,1,new(.1,.2,.4));
            var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Vector()*4,Vector()*3,2,new(.4,.3,.2));
            var direction=Vector(); direction/=direction.Length;
            var j=ConstraintJacobian.AtPoint(a,b,Vector(),direction);
            var energy=a.KineticEnergy+b.KineticEnergy;
            var impulse=random.NextDouble()*3; var work=random.NextDouble()*2;
            var result=PoweredImpulse.Apply(a,b,j,Number()*10,impulse,work);
            Assert.InRange(Math.Abs(result.Impulse),0,impulse); Assert.InRange(result.SuppliedWork,0,work);
            var change=a.KineticEnergy+b.KineticEnergy-energy;
            Assert.InRange(Math.Abs(change-(result.SuppliedWork-result.DissipatedWork)),0,1e-11);
        }
    }

    [Fact]
    public void PrescribedCarrierWorkIsSeparateFromMotorSupply()
    {
        var a=Body(0);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,new(0,0,10),default);
        var result=PoweredImpulse.Apply(a,b,Linear,0,100,0);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-10),0,1e-12);
        Assert.Equal(0,result.SuppliedWork);
        Assert.InRange(Math.Abs(result.DissipatedWork-50),0,1e-12);
        Assert.InRange(Math.Abs(a.KineticEnergy-50),0,1e-12);
        Assert.Equal(new CollisionVector(0,0,10),b.LinearVelocity);
    }

    [Fact]
    public void DisabledMotorDoesNotBrakeOrConsumeSupply()
    {
        var a=Body(0,new(0,0,2)); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b);
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default));
        world.Step([new(joint.Id,-10,0,3)],.1);
        Assert.Equal(new CollisionVector(0,0,2),a.LinearVelocity);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.Equal(0,use.AbsoluteImpulse); Assert.Equal(0,use.SuppliedWork); Assert.Equal(0,use.DissipatedWork);
        Assert.Equal(3,use.RemainingWork);
    }
}
