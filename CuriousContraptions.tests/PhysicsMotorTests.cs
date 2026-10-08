using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsMotorTests
{

    [Theory]
    [InlineData(-1,1,.01,false)]
    [InlineData(1,1,.01,true)]
    [InlineData(-1,3,.01,true)]
    [InlineData(1,3,.01,false)]
    [InlineData(-1,1,10,true)]
    [InlineData(1,1,10,false)]
    [InlineData(-1,3,10,false)]
    [InlineData(1,3,10,true)]
    public void ContactLoadSharesMotorMomentumAndWorkWithExactReplay(int sign,double mass,double work,bool reverseOrder)
    {
        var driven=Body(0);var anchor=Fixed(1);
        var load=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,
            RigidPose.At(new(0,0,sign*.02)),default,default,mass,new(.1,.1,.1));
        var joint=Joint(FrameJointKind.Slider,driven,anchor);
        PhysicsObject[] objects=[Object(driven),Object(anchor),Object(load)];
        if(reverseOrder) Array.Reverse(objects);
        var world=new PhysicsWorld([],objects,[joint],new(default,maximumStep:.01));
        var before=world.Capture();
        void Run()
        {
            world.Step([],[new(joint.Id,sign,100,work,1000000)],.01);
            var report=Assert.Single(world.MotorUse.ToArray());
            var speed=Math.Min(1/(1+mass),Math.Sqrt(2*work/(1+mass)));
            Assert.InRange(Math.Abs(driven.LinearVelocity.Z-sign*speed),0,1e-10);
            Assert.InRange(Math.Abs(load.LinearVelocity.Z-sign*speed),0,1e-10);
            Assert.InRange(Math.Abs(report.AbsoluteImpulse-(1+mass)*speed),0,1e-10);
            Assert.InRange(Math.Abs(report.SuppliedWork-.5*(1+mass)*speed*speed),0,1e-10);
            Assert.InRange(Math.Abs(driven.KineticEnergy+load.KineticEnergy-report.SuppliedWork),0,1e-10);
            Assert.InRange(report.SuppliedWork,0,work);
            Assert.InRange(Math.Abs((load.Center-driven.Center).Z-sign*.02),0,1e-10);
            var coast=load.Snapshot();
            world.Step([],[new(joint.Id,-sign,100,10,1000000)],.01);
            Assert.True(driven.LinearVelocity.Z*sign<0);
            Assert.Equal(coast.LinearVelocity,load.LinearVelocity);
            Assert.True((load.Center-driven.Center).Z*sign>.02);
        }
        Run();var after=world.Capture();var use=world.MotorUse.ToArray();
        world.Restore(before);Run();
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(use,world.MotorUse.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ContactBlocksMotorWorkAndPermitsReleaseWithExactReplay(int sign)
    {
        var a=Body(0);var anchor=Fixed(1);
        var obstacle=new PhysicsBody(new(2),PhysicsMotionType.Static,
            RigidPose.At(new(0,0,sign*.02)),default,default);
        var joint=Joint(FrameJointKind.Slider,a,anchor);
        var world=new PhysicsWorld([],[Object(a),Object(anchor),Object(obstacle)],[joint],new(default));
        var before=world.Capture();
        void Run()
        {
            world.Step([],[new(joint.Id,sign,100,100,1000000)],.01);
            Assert.InRange(a.LinearVelocity.Length,0,1e-10);
            Assert.InRange(a.Center.Length,0,1e-10);
            Assert.InRange(Assert.Single(world.MotorUse.ToArray()).SuppliedWork,0,1e-10);
            world.Step([],[new(joint.Id,-sign,100,100,1000000)],.01);
            Assert.True(a.LinearVelocity.Z*sign<0);
            Assert.True(a.Center.Z*sign<0);
            Assert.True(Assert.Single(world.MotorUse.ToArray()).SuppliedWork>0);
        }
        Run();var after=world.Capture();var use=world.MotorUse.ToArray();
        world.Restore(before);Run();
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(use,world.MotorUse.ToArray());
    }

    [Theory]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.Hinge)]
    public void CommittedMotorTotalsOwnAccelerationBrakingCoastingAndReplay(FrameJointKind kind)
    {
        var a=Body(0);var b=Fixed(1);var joint=Joint(kind,a,b);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.005));
        var zero=new PhysicsMotorTotals(joint.Id,0,0,0,0,0,0);
        Assert.Equal(zero,world.MotorTotal(joint.Id));
        Assert.Throws<ArgumentException>(()=>world.MotorTotal(new(99)));
        var initial=world.Capture();
        void Run()
        {
            world.Step([],[new(joint.Id,2,100,10,1000000)],.02);
            var first=Assert.Single(world.MotorUse.ToArray());
            Assert.True(first.SuppliedWork>0);
            Assert.Equal(new(joint.Id,first.AbsoluteImpulse,first.SuppliedWork,first.DissipatedWork,
                first.SuppliedWorkError,first.DissipatedWorkError,first.ReservedWork),
                world.MotorTotal(joint.Id));
            world.Step([],[new(joint.Id,0,100,0,1000000)],.02);
            var second=Assert.Single(world.MotorUse.ToArray());
            Assert.True(second.DissipatedWork>0);
            Assert.Equal(0,second.SuppliedWork);
            var total=world.MotorTotal(joint.Id);
            Assert.Equal(first.SuppliedWork,total.SuppliedWork);
            Assert.Equal(first.AbsoluteImpulse+second.AbsoluteImpulse,total.AbsoluteImpulse);
            Assert.Equal(first.DissipatedWork+second.DissipatedWork,total.DissipatedWork);
            world.Step([],[],.02);
            Assert.Empty(world.MotorUse.ToArray());
            Assert.Equal(total,world.MotorTotal(joint.Id));
            world.Step([],[new(joint.Id,3,0,10,1000000)],.02);
            Assert.Equal(total,world.MotorTotal(joint.Id));
        }
        Run();var after=world.Capture();
        var copy=world.MotorTotals.ToArray();copy[0]=zero;
        Assert.NotEqual(zero,world.MotorTotal(joint.Id));
        world.Restore(initial);
        Assert.Empty(world.MotorTotals.ToArray());Assert.Equal(zero,world.MotorTotal(joint.Id));
        Run();
        Assert.Equal(after.MotorTotals.ToArray(),world.MotorTotals.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        var replacement=new PhysicsFrameJoint(joint.Id,joint.Kind,joint.A,joint.LocalA,
            joint.B,joint.LocalB,joint.Collision,new(-1,1),joint.Direction);
        world.ReplaceJoints([replacement]);
        Assert.Equal(after.MotorTotals.ToArray(),world.MotorTotals.ToArray());
        // Removing the hinge also removes its collision suppression. Explicitly
        // disable the overlapping rotor before disassembling that joint.
        var collider=world.Collider(a.Id).Declaration;
        world.ApplyColliderUpdates([new(a.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
        world.ReplaceJoints([]);
        Assert.Equal(after.MotorTotals.ToArray(),world.MotorTotals.ToArray());
        Assert.Equal(after.MotorTotals[0],world.MotorTotal(joint.Id));
        world.Restore(initial);Assert.Equal(zero,world.MotorTotal(joint.Id));
    }


    [Theory]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.Hinge)]
    public void WorldCommitsPeakLimitedWorkAndRestoresExactReplay(FrameJointKind kind)
    {
        var a=Body(0);var b=Fixed(1);var joint=Joint(kind,a,b);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:1));
        PhysicsMotorCommand command=new(joint.Id,10,100,100,1);
        var before=world.Capture();
        world.Step([],[command],.1);
        var inertia=kind==FrameJointKind.Slider?1:.1;
        Assert.InRange(Math.Abs(Math.Sqrt(.1/inertia)-joint.Motion.Speed),0,1e-7);
        Assert.InRange(Math.Abs(.05-world.MotorUse[0].SuppliedWork),0,1e-8);
        Assert.True(world.MotorUse[0].ReservedWork<=100);
        var after=world.Capture();var use=world.MotorUse[0];
        world.Restore(before);
        world.Step([],[command],.1);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(use,world.MotorUse[0]);
        Assert.Equal(after.MotorTotals.ToArray(),world.MotorTotals.ToArray());
        world.Restore(before);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
    private static PhysicsFrameJoint Joint(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointTravelRange? range=null)=>
        new(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,range,JointTravelDirection.Both);
    private static readonly ConstraintJacobian Linear=new(new(0,0,1),default,new(0,0,-1),default);

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void FiniteWorkCapsAccelerationAndDoesNotSpendBrakingEnergy(int sign)
    {
        var a=Body(0,new(0,0,-sign*3)); var b=Fixed(1);
        var result=PoweredImpulse.Apply(Linear.Bind(a,b),sign*10,100,.5);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-sign),0,1e-12);
        Assert.InRange(result.SuppliedWork,0,.5);
        Assert.InRange(Math.Abs(result.DissipatedWork-4.5),0,1e-12);
        Assert.InRange(Math.Abs(result.Impulse-sign*4),0,1e-12);
    }

    [Fact]
    public void ZeroSupplyAllowsBrakingButNotReverseAcceleration()
    {
        var a=Body(0,new(0,0,3)); var b=Fixed(1);
        var result=PoweredImpulse.Apply(Linear.Bind(a,b),-10,100,0);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z),0,1e-12);
        Assert.Equal(0,result.SuppliedWork);
        Assert.InRange(Math.Abs(result.DissipatedWork-4.5),0,1e-12);
        var before=a.Snapshot();
        Assert.Equal(default,PoweredImpulse.Apply(Linear.Bind(a,b),10,0,10));
        Assert.Equal(before,a.Snapshot());
    }

    [Fact]
    public void TwoDynamicBodiesReceiveOppositeMomentumAndAccountForTheirTotalWork()
    {
        var a=Body(0); var b=Body(1);
        var result=PoweredImpulse.Apply(Linear.Bind(a,b),10,100,1);
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
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default));
        PhysicsMotorCommand[] commands=[new(joint.Id,10,2,.5,1000000)];
        var before=world.Capture(); world.Step([],commands,.5);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.InRange(use.SuppliedWork,0,.5); Assert.InRange(use.AbsoluteImpulse,0,1);
        Assert.InRange(a.KineticEnergy,0,.500000000001);
        var expected=kind==FrameJointKind.Hinge?Math.Sqrt(10):1;
        Assert.InRange(Math.Abs(a.LinearVelocity.Z+a.AngularVelocity.Z-expected),0,1e-10);
        var after=world.Capture(); var report=world.MotorUse.ToArray();
        world.Restore(before); Assert.Empty(world.MotorUse.ToArray());
        world.Step([],commands,.5);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(report,world.MotorUse.ToArray());
    }

    [Fact]
    public void StopEventAccountsForContinuedBoundedEffortWithoutAdditionalWork()
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b,new(-.02,.02));
        var settings=new PhysicsWorldSettings(default,maximumStep:.1);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],settings);
        world.Step([],[new(joint.Id,1,100,100,1000000)],.1);
        var stop=Assert.Single(world.JointStops.ToArray());
        Assert.InRange(Math.Abs(a.Center.Z-.02),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length,0,1e-8);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.InRange(Math.Abs(use.AbsoluteImpulse-(1+100*(.1-stop.Time))),0,1e-10);
        var speedError=settings.AccelerationTolerance*stop.Time;
        var energyError=speedError+.5*speedError*speedError;
        Assert.InRange(Math.Abs(use.SuppliedWork-.5),0,energyError);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ContinuedSupplyAtTravelStopDoesNotSpendWork(int sign)
    {
        var a=Body(0); var b=Fixed(1);
        var joint=Joint(FrameJointKind.Slider,a,b,new(-.02,.02));
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.1));
        PhysicsMotorCommand[] commands=[new(joint.Id,sign,100,100,1000000)];
        world.Step([],commands,.1);
        Assert.InRange(Math.Abs(a.Center.Z-sign*.02),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length,0,1e-8);
        var stopped=world.Capture();
        double supplied=0;
        for(var i=0;i<10;i++)
        {
            world.Step([],commands,.1);
            supplied+=Assert.Single(world.MotorUse.ToArray()).SuppliedWork;
            Assert.InRange(Math.Abs(a.Center.Z-sign*.02),0,1e-7);
            Assert.InRange(a.LinearVelocity.Length,0,1e-8);
        }
        var after=world.Capture();
        world.Restore(stopped);
        for(var i=0;i<10;i++) world.Step([],commands,.1);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.InRange(supplied,0,1e-10);
    }

    [Theory]
    [InlineData(FrameJointKind.Slider,-1)]
    [InlineData(FrameJointKind.Slider,1)]
    [InlineData(FrameJointKind.Hinge,-1)]
    [InlineData(FrameJointKind.Hinge,1)]
    public void WorldMotorAtAStopBrakesInwardMotionAndThenReleases(FrameJointKind kind,int sign)
    {
        var a=Body(0);var b=Fixed(1);var joint=Joint(kind,a,b,new(-.02,.02));
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.1));
        PhysicsMotorCommand[] outward=[new(joint.Id,sign,1000,100,1000000)];
        world.Step([],outward,.1);
        Assert.InRange(Math.Abs(joint.Travel.Error-sign*.02),0,1e-7);
        if(kind==FrameJointKind.Slider)
            world.ApplyImpulse(a.Id,new(0,0,-sign),a.Center);
        else
            world.ApplyImpulse(a.Id,new(0,-sign*.1,0),a.Center+new CollisionVector(1,0,0));
        var incomingEnergy=.5*(kind==FrameJointKind.Hinge?.1:1)*joint.Motion.Speed*joint.Motion.Speed;
        var before=world.Capture();
        world.Step([],outward,.005);
        var use=Assert.Single(world.MotorUse.ToArray());
        // Continuous force brakes the inward motion and drives it back to the
        // stop; the terminal impact removes the restored outgoing kinetic energy.
        Assert.InRange(Math.Abs(use.SuppliedWork-incomingEnergy),0,1e-10);
        Assert.InRange(Math.Abs(use.DissipatedWork-incomingEnergy),0,1e-10);
        Assert.InRange(Math.Abs(joint.Travel.Jacobian.Bind(a,b).Speed),0,1e-8);
        var stopped=world.Capture();
        world.Restore(before);
        world.Step([],outward,.005);
        Assert.Equal(stopped.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(use,Assert.Single(world.MotorUse.ToArray()));
        world.Step([],[new(joint.Id,-sign,100,100,1000000)],.005);
        Assert.True(sign*joint.Travel.Jacobian.Bind(a,b).Speed<0);
        Assert.True(sign*joint.Travel.Error<.02);
        Assert.True(Assert.Single(world.MotorUse.ToArray()).SuppliedWork>0);
    }

    public enum TransmissionTopology { SingleLink, RedundantLoop, LockedLoop }

    public static TheoryData<FrameJointKind,int,double,TransmissionEngagement,TransmissionTopology> TransmissionMotorCases()
    {
        var cases=new TheoryData<FrameJointKind,int,double,TransmissionEngagement,TransmissionTopology>();
        foreach(var kind in new[]{FrameJointKind.Slider,FrameJointKind.Hinge})
        foreach(var sign in new[]{-1,1})
        foreach(var ratio in new[]{-2d,1d,2d})
        foreach(var engagement in Enum.GetValues<TransmissionEngagement>())
        foreach(var topology in Enum.GetValues<TransmissionTopology>())
            cases.Add(kind,sign,ratio,engagement,topology);
        return cases;
    }

    [Theory]
    [MemberData(nameof(TransmissionMotorCases))]
    public void TransmissionLoadParticipatesInMotorTargetAndWork(
        FrameJointKind kind,int sign,double ratio,TransmissionEngagement engagement,TransmissionTopology topology)
    {
        var a=Body(0); var b=Fixed(1);
        var c=Body(2); var d=Fixed(3);
        c.Restore(c.Snapshot() with {Pose=RigidPose.At(new(3,0,0))});
        d.Restore(d.Snapshot() with {Pose=RigidPose.At(new(3,0,0))});
        var first=Joint(kind,a,b);
        var second=new PhysicsFrameJoint(new(1),kind,c,Origin,d,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var transmission=new PhysicsTransmissionJoint(new(2),first,second,ratio,engagement);
        var returnTransmission=new PhysicsTransmissionJoint(new(3),second,first,(topology==TransmissionTopology.LockedLoop?2:1)/ratio,engagement);
        PhysicsJoint[] joints=topology!=TransmissionTopology.SingleLink?[first,second,transmission,returnTransmission]:[first,second,transmission];
        var world=new PhysicsWorld([],[Object(a),Object(b),Object(c),Object(d)],
            joints,new(default,maximumStep:.1));
        var initial=world.Capture();
        var locked=engagement==TransmissionEngagement.Engaged&&topology==TransmissionTopology.LockedLoop;
        var expectedInputSpeed=locked?0:sign;
        var outputSpeed=engagement==TransmissionEngagement.Engaged?expectedInputSpeed*ratio:0;
        var inertia=kind==FrameJointKind.Hinge?.1:1;
        var requiredWork=.5*inertia*(expectedInputSpeed*expectedInputSpeed+outputSpeed*outputSpeed);
        PhysicsMotorCommand[] commands=[new(first.Id,sign,100,locked?1:requiredWork,1000000)];
        world.Step([],commands,.1);
        var after=world.Capture();
        var use=Assert.Single(world.MotorUse.ToArray());
        world.Restore(initial);
        world.Step([],commands,.1);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(use,Assert.Single(world.MotorUse.ToArray()));
        if(engagement==TransmissionEngagement.Engaged)
        {
            Assert.InRange(Math.Abs(transmission.SpeedError),0,1e-10);
            if(topology!=TransmissionTopology.SingleLink) Assert.InRange(Math.Abs(returnTransmission.SpeedError),0,1e-10);
        }
        var inputSpeed=first.Travel.Jacobian.Bind(a,b).Speed;
        var actualOutputSpeed=second.Travel.Jacobian.Bind(c,d).Speed;
        Assert.InRange(Math.Abs(inputSpeed-expectedInputSpeed),0,1e-10);
        Assert.InRange(Math.Abs(actualOutputSpeed-outputSpeed),0,1e-10);
        Assert.InRange(Math.Abs(use.SuppliedWork-requiredWork),0,1e-10);
        Assert.InRange(Math.Abs(a.KineticEnergy+c.KineticEnergy-use.SuppliedWork),0,1e-10);
    }

    [Fact]
    public void FailedStepRestoresBodiesClockAndThePreviousMotorReport()
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b,new(-.5,.5));
        var c=Body(2,new(0,0,2)); c.Restore(c.Snapshot() with {Pose=RigidPose.At(new(2,0,0))});
        var d=Fixed(3); d.Restore(d.Snapshot() with {Pose=RigidPose.At(new(2,0,0))});
        var second=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,c,Origin,d,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b),Object(c),Object(d)],[joint,second],new(default,maximumStep:1,maximumEvents:1));
        world.Step([],[new(joint.Id,1,1,1,1000000)],.01);
        var before=world.Capture(); var reports=world.MotorUse.ToArray();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[new(joint.Id,100,100,100,1000000)],1));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time); Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(reports,world.MotorUse.ToArray());
        Assert.Equal(before.MotorTotals.ToArray(),world.MotorTotals.ToArray());
    }

    [Fact]
    public void InvalidAndDuplicateCommandsFailBeforeMutation()
    {
        var a=Body(0); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default));
        var command=new PhysicsMotorCommand(joint.Id,1,1,1,1000000); var before=a.Snapshot();
        Assert.Throws<ArgumentException>(()=>world.Step([],[command,command],.01));
        Assert.Throws<ArgumentException>(()=>world.Step([],[new(new(99),1,1,1,1000000)],.01));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(joint.Id,double.NaN,1,1,1000000));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(joint.Id,1,-1,1,1000000));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(joint.Id,1,1,-1,1000000));
        Assert.Throws<ArgumentException>(()=>PoweredImpulse.Apply(new ConstraintGradient([new(a,default,default),new(b,default,default)]),1,1,1));
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
            var result=PoweredImpulse.Apply(j.Bind(a,b),Number()*10,impulse,work);
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
        var result=PoweredImpulse.Apply(Linear.Bind(a,b),0,100,0);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-10),0,1e-12);
        Assert.Equal(0,result.SuppliedWork);
        Assert.InRange(Math.Abs(result.DissipatedWork-50),0,1e-12);
        Assert.InRange(Math.Abs(a.KineticEnergy-50),0,1e-12);
        Assert.Equal(new CollisionVector(0,0,10),b.LinearVelocity);
    }

    [Theory]
    [InlineData(-1,false)]
    [InlineData(1,false)]
    [InlineData(-1,true)]
    [InlineData(1,true)]
    public void ArbitraryBodyGradientAccountsForEveryParticipantAndBraking(int sign,bool splitTerms)
    {
        var a=Body(0);var b=Body(1);var c=Body(2);
        var terms=new List<ConstraintTerm>
        {
            new(b,new(0,0,.5),default),
            new(c,new(0,0,-1),default)
        };
        if(splitTerms)
        {
            terms.Add(new(a,new(0,0,.25),default));
            terms.Add(new(a,new(0,0,.25),default));
        }
        else terms.Add(new(a,new(0,0,.5),default));
        var gradient=new ConstraintGradient(terms.ToArray());
        var before=new[]{a.Snapshot(),b.Snapshot(),c.Snapshot()};
        var use=PoweredImpulse.Apply(gradient,sign*10,100,1);
        var after=new[]{a.Snapshot(),b.Snapshot(),c.Snapshot()};
        Assert.InRange(use.SuppliedWork,.999999999999,1);
        Assert.InRange(Math.Abs(a.KineticEnergy+b.KineticEnergy+c.KineticEnergy-use.SuppliedWork),0,1e-12);
        Assert.InRange((a.LinearVelocity+b.LinearVelocity+c.LinearVelocity).Length,0,1e-12);
        Assert.Equal(a.LinearVelocity,b.LinearVelocity);
        Assert.InRange(Math.Abs(c.LinearVelocity.Z+2*a.LinearVelocity.Z),0,1e-12);
        Assert.True(sign*a.LinearVelocity.Z>0);
        var braking=PoweredImpulse.Apply(gradient,-sign*10,100,0);
        Assert.Equal(0,braking.SuppliedWork);
        Assert.InRange(Math.Abs(braking.DissipatedWork-use.SuppliedWork),0,1e-12);
        Assert.InRange(a.KineticEnergy+b.KineticEnergy+c.KineticEnergy,0,1e-12);
        a.Restore(before[0]);b.Restore(before[1]);c.Restore(before[2]);
        Assert.Equal(use,PoweredImpulse.Apply(gradient,sign*10,100,1));
        Assert.Equal(after,new[]{a.Snapshot(),b.Snapshot(),c.Snapshot()});
    }

    [Fact]
    public void DisabledMotorDoesNotBrakeOrConsumeSupply()
    {
        var a=Body(0,new(0,0,2)); var b=Fixed(1); var joint=Joint(FrameJointKind.Slider,a,b);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default));
        world.Step([],[new(joint.Id,-10,0,3,1000000)],.1);
        Assert.Equal(new CollisionVector(0,0,2),a.LinearVelocity);
        var use=Assert.Single(world.MotorUse.ToArray());
        Assert.Equal(0,use.AbsoluteImpulse); Assert.Equal(0,use.SuppliedWork); Assert.Equal(0,use.DissipatedWork);
        Assert.Equal(3,use.RemainingWork);
    }
}
