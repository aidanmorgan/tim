using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MotorPredictionTests
{
    public enum MotorDirection { Positive, Negative }
    private static readonly CollisionVector Z=new(0,0,1);
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static (PhysicsBody Body,PhysicsBody Anchor,PhysicsFrameJoint Joint) Setup(double speed=0,JointTravelRange? range=null)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*speed,default,2,new(2,2,2));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,range,JointTravelDirection.Both);
        return(body,anchor,joint);
    }
    private static ForcePrediction Predict(PhysicsBody body,PhysicsBody anchor,PhysicsFrameJoint joint,
        MotorPredictionSupply supply,double duration,BodyWrench load=default)=>
        AccelerationSolver.Predict(new(),new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[body,anchor],[joint],[],
            new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,load},{anchor.Id,default}},duration,duration,1e-7,1e-8,1e-9,[supply],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
    private static void Near(double expected,double actual,double tolerance=1e-8)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(MotorDirection.Positive)]
    [InlineData(MotorDirection.Negative)]
    public void RoundedImpulseQuotientStaysInsideTheExactRemainingAllowance(MotorDirection direction)
    {
        var sign=direction switch
        {
            MotorDirection.Positive=>1,MotorDirection.Negative=>-1,
            _=>throw new ArgumentOutOfRangeException(nameof(direction))
        };
        const double duration=.00062077826912692,allowance=.012415565382538398;
        Assert.True((allowance/duration)*duration>allowance); // Exact browser regression boundary.
        var (body,anchor,joint)=Setup();
        var before=body.Snapshot();
        var prediction=Predict(body,anchor,joint,
            new(new(joint.Id,sign*6,20,.25,120),allowance,1e-10),duration);
        var use=Assert.Single(prediction.Motors);
        Assert.Equal(duration,prediction.Duration);
        Assert.InRange(use.AbsoluteImpulse,Math.BitDecrement(allowance),allowance);
        Assert.Equal(Math.Abs(use.Effort)*duration,use.AbsoluteImpulse);
        Assert.Equal(sign,Math.Sign(use.Effort));
        Assert.InRange(use.Work.Supplied+use.Work.SuppliedErrorBound,0,.25);
        Assert.InRange(use.Work.SuppliedPowerUpperBound,0,120);
        Assert.Equal(before,body.Snapshot());
    }


    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void PeakPowerLimitCannotBeReplacedByAnAverageWorkBudget(int sign)
    {
        var (body,anchor,joint)=Setup();
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,sign*10,100,100,1),100,1e-10),1);
        var use=Assert.Single(prediction.Motors);
        // Held force F on mass2: end speed F/2 and peak power F²/2=1W.
        Near(Math.Sqrt(2)*sign,use.Effort);
        Near(.5,use.Work.Supplied);
        Near(Math.Sqrt(.5)*sign,prediction.Trajectories[body.Id].LinearVelocityAt(1).Z);
        Assert.InRange(use.Work.SuppliedPowerUpperBound,1-1e-8,1);
        Assert.True(use.Work.Supplied<1); // A 1J average budget alone would permit 2W at the endpoint.
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ZeroWattsWithAvailableEnergyCannotStartButCanBrake(int sign)
    {
        var (body,anchor,joint)=Setup();
        var idle=Predict(body,anchor,joint,new(new(joint.Id,sign*2,100,100,0),100,1e-10),1);
        Near(0,Assert.Single(idle.Motors).Effort);
        (body,anchor,joint)=Setup(sign*2);
        var stopped=Predict(body,anchor,joint,new(new(joint.Id,0,100,100,0),100,1e-10),1);
        var use=Assert.Single(stopped.Motors);
        Near(0,use.Work.Supplied);Near(0,use.Work.SuppliedPowerUpperBound);
        Near(4,use.Work.Dissipated);Near(0,stopped.Trajectories[body.Id].LinearVelocityAt(1).Z);
    }


    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void HingePowerUsesPhysicalAngularSpeed(int sign)
    {
        var (body,anchor,slider)=Setup();
        var hinge=new PhysicsFrameJoint(slider.Id,FrameJointKind.Hinge,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var prediction=Predict(body,anchor,hinge,new(new(hinge.Id,sign*10,100,100,1),100,1e-10),1);
        var use=Assert.Single(prediction.Motors);
        Near(Math.Sqrt(2)*sign,use.Effort);Near(.5,use.Work.Supplied);
        Near(Math.Sqrt(.5)*sign,prediction.Trajectories[body.Id].PhysicalAngularVelocityAt(1).Z);
        Assert.InRange(use.Work.SuppliedPowerUpperBound,1-1e-8,1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidPowerIsRejected(double power)
        =>Assert.Throws<ArgumentException>(()=>new PhysicsMotorCommand(new(0),1,1,1,power));

    [Fact]
    public void LoadedMotorReachesTargetWithBalancedWorkWithoutMutatingBodies()
    {
        var (body,anchor,joint)=Setup();var before=new[]{body.Snapshot(),anchor.Snapshot()};
        var supply=new MotorPredictionSupply(new(joint.Id,1,100,2,1000000),10,1e-10);
        var prediction=Predict(body,anchor,joint,supply,.1,new(-Z*6,default));
        var use=Assert.Single(prediction.Motors);
        Near(26,use.Effort);Near(1.3,use.Work.Supplied);Near(0,use.Work.Dissipated);
        Near(1,prediction.Trajectories[body.Id].LinearVelocityAt(prediction.Duration).Z);
        Near(1,use.Work.Supplied-6*prediction.Trajectories[body.Id].At(prediction.Duration).Center.Z);
        Assert.Equal(before,new[]{body.Snapshot(),anchor.Snapshot()});
        var repeat=Predict(body,anchor,joint,supply,.1,new(-Z*6,default));
        Assert.Equal(prediction.Motors,repeat.Motors);Assert.Equal(prediction.Wrenches[body.Id],repeat.Wrenches[body.Id]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(.25)]
    public void SupplyExhaustionLimitsKineticEnergy(double available)
    {
        var (body,anchor,joint)=Setup();
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,2,100,available,1000000),100,1e-10),1);
        var use=Assert.Single(prediction.Motors);
        var speed=prediction.Trajectories[body.Id].LinearVelocityAt(prediction.Duration).Z;
        Near(Math.Sqrt(available),speed);Near(available,use.Work.Supplied);
        Assert.True(use.Work.Supplied+use.Work.SuppliedErrorBound<=available);
        Near(speed*speed,use.Work.Supplied);
    }

    [Fact]
    public void EmptySupplyCannotStartMotionButCanBrake()
    {
        var (body,anchor,joint)=Setup();
        var noStart=Predict(body,anchor,joint,new(new(joint.Id,2,100,0,1000000),100,1e-10),1);
        Assert.Equal(0,Assert.Single(noStart.Motors).Effort);
        Near(0,noStart.Trajectories[body.Id].LinearVelocityAt(1).Length);
        (body,anchor,joint)=Setup(2);
        var braking=Predict(body,anchor,joint,new(new(joint.Id,0,10,0,1000000),10,1e-10),1);
        var use=Assert.Single(braking.Motors);
        Near(-4,use.Effort);Near(0,use.Work.Supplied);Near(4,use.Work.Dissipated);
        Near(0,braking.Trajectories[body.Id].LinearVelocityAt(1).Length);
    }

    [Fact]
    public void ZeroWorkHoldingLoadDoesNotRequireAnEnergySource()
    {
        var (body,anchor,joint)=Setup();
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,0,10,0,1000000),10,1e-10),1,new(-Z*6,default));
        Near(6,Assert.Single(prediction.Motors).Effort);
        Near(0,prediction.Motors[0].Work.Supplied);
        Near(0,prediction.Trajectories[body.Id].At(1).Center.Length);
    }

    [Fact]
    public void RemainingImpulseCapsEffort()
    {
        var (body,anchor,joint)=Setup();
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,10,100,100,1000000),.5,1e-10),1);
        var use=Assert.Single(prediction.Motors);
        Near(.5,use.AbsoluteImpulse);Near(.5,use.Effort);
        Near(.25,prediction.Trajectories[body.Id].LinearVelocityAt(1).Z);
    }

    [Fact]
    public void TravelBoundaryRemeasuresTheShortenedPath()
    {
        var (body,anchor,joint)=Setup(range:new(-1,.01));
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,1,10,1,1000000),10,1e-10),1);
        Assert.Equal(ForcePredictionBoundary.Joint,prediction.Boundary);
        Assert.InRange(prediction.Duration,0,.1);
        var use=Assert.Single(prediction.Motors);var endpoint=prediction.Trajectories[body.Id].At(prediction.Duration);
        Near(use.Effort*endpoint.Center.Z,use.Work.Supplied);
        Near(Math.Abs(use.Effort)*prediction.Duration,use.AbsoluteImpulse);
        Assert.True(use.Work.Supplied<=1);
    }


    [Fact]
    public void ReversalSpendsSupplyAfterBrakingWithoutRechargingIt()
    {
        var (body,anchor,joint)=Setup(2);
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,-2,100,1,1000000),100,1e-10),1);
        var use=Assert.Single(prediction.Motors);
        Near(-1,prediction.Trajectories[body.Id].LinearVelocityAt(1).Z);
        Near(1,use.Work.Supplied);Near(4,use.Work.Dissipated);
        Assert.True(use.Work.Supplied+use.Work.SuppliedErrorBound<=1);
    }

    [Fact]
    public void HingeTorqueAccountsForCapturedRotation()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,Z*2,2,new(2,2,2));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,4,100,20,1000000),100,1e-10),.5);
        var use=Assert.Single(prediction.Motors);
        Near(8,use.Effort);Near(12,use.Work.Supplied);Near(0,use.Work.Dissipated);
        Near(4,prediction.Trajectories[body.Id].PhysicalAngularVelocityAt(.5).Z);
    }

    [Fact]
    public void ObstructedDriveSaturatesWithoutCreatingMotionOrWork()
    {
        var (body,anchor,joint)=Setup(range:new(0,0));
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,1,10,1,1000000),10,1e-10),1);
        var use=Assert.Single(prediction.Motors);
        Near(10,use.Effort);Near(0,use.Work.Supplied);Near(0,use.Work.Dissipated);
        Assert.True(prediction.Duration==1,
            $"Locked drive duration={prediction.Duration:R}, boundary={prediction.Boundary}, wrench={prediction.Wrenches[body.Id]}, end={prediction.Trajectories[body.Id].At(prediction.Duration)}, velocity={prediction.Trajectories[body.Id].LinearVelocityAt(prediction.Duration)}");
        Near(0,prediction.Trajectories[body.Id].LinearVelocityAt(1).Length);
    }


    [Fact]
    public void MultipleMotorsKeepIdentityOrderAndIndependentAllowances()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,2,new(2,2,2));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,4,new(4,4,4));
        var anchor=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var first=new PhysicsFrameJoint(new(7),FrameJointKind.Slider,a,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var second=new PhysicsFrameJoint(new(3),FrameJointKind.Slider,b,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var firstSupply=new MotorPredictionSupply(new(first.Id,1,10,10,1000000),10,1e-10);
        var secondSupply=new MotorPredictionSupply(new(second.Id,.5,10,10,1000000),10,1e-10);
        ForcePrediction Run(MotorPredictionSupply[] supplies)=>AccelerationSolver.Predict(new(),
            new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b,anchor],[first,second],[],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default},{b.Id,default},{anchor.Id,default}},
            1,1,1e-7,1e-8,1e-9,supplies,new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        var prediction=Run([firstSupply,secondSupply]);var repeated=Run([secondSupply,firstSupply]);
        Assert.Equal(prediction.Motors,repeated.Motors);
        Assert.Equal(second.Id,prediction.Motors[0].Joint);Assert.Equal(first.Id,prediction.Motors[1].Joint);
        Near(.5,prediction.Motors[0].Work.Supplied);Near(1,prediction.Motors[1].Work.Supplied);
        Near(.5,prediction.Trajectories[b.Id].LinearVelocityAt(1).Z);
        Near(1,prediction.Trajectories[a.Id].LinearVelocityAt(1).Z);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(1000000)]
    public void EmptySupplyStillBrakesAgainstAMovingPrescribedFrame(double maximumPower)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*2,default,2,new(2,2,2));
        var motion=new PrescribedBodyMotion(new QuinticRigidTrajectory(RigidPose.Identity,Z*.1,default,1),RigidPose.Identity,0);
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,motion.At(0),default,default,prescribedMotion:motion);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,0,10,0,maximumPower),10,1e-8),1);
        var use=Assert.Single(prediction.Motors);
        Assert.True(use.Effort<0);
        Near(0,use.Work.Supplied);
        Assert.True(use.Work.Dissipated>0);
    }


    [Theory]
    [InlineData(.49999999999999956)]
    [InlineData(-.49999999999999956)]
    public void ZeroSupplyBrakingRoundsTheEndpointWithoutReversing(double speed)
    {
        var (body,anchor,joint)=Setup(speed);
        var prediction=Predict(body,anchor,joint,new(new(joint.Id,0,1000,0,0),10,1e-10),.005);
        var endpoint=prediction.Trajectories[body.Id].LinearVelocityAt(.005).Z;
        Assert.True(endpoint*speed>=0);Near(0,endpoint,1e-12);
        var use=Assert.Single(prediction.Motors);
        Assert.Equal(0,use.Work.Supplied);Assert.Equal(0,use.Work.SuppliedErrorBound);
        Near(speed*speed,use.Work.Dissipated,1e-12);
    }

    [Fact]
    public void ForeignAndDuplicateMotorDeclarationsReject()
    {
        var (body,anchor,joint)=Setup();var before=body.Snapshot();
        Assert.Throws<ArgumentException>(()=>new MotorPredictionSupply(new(joint.Id,1,1,1,1000000),-1,1e-6));
        Assert.Throws<ArgumentException>(()=>new MotorPredictionSupply(new(joint.Id,1,1,1,1000000),1,0));
        Assert.Throws<ArgumentException>(()=>Predict(body,anchor,joint,new(new(new(9),1,1,1,1000000),1,1e-6),1));
        var supply=new MotorPredictionSupply(new(joint.Id,1,1,1,1000000),1,1e-6);
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Predict(new(),new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),
            [body,anchor],[joint],[],new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,default},{anchor.Id,default}},
            1,1,1e-7,1e-8,1e-9,[supply,supply],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()));
        Assert.Equal(before,body.Snapshot());
    }
}
