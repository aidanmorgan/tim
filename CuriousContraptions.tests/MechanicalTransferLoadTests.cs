using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalTransferLoadTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
    private static (PhysicsWorld World,PhysicsBody Source,PhysicsBody[] Receivers) Setup(int count,double step,bool reverse,double sourceSpeed,double receiverSpeed,int maximumEvents=256)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*sourceSpeed,default,1,new(1,1,1));
        var receivers=Enumerable.Range(0,count).Select(i=>new PhysicsBody(new(i+2),PhysicsMotionType.Dynamic,
            RigidPose.At(new(i+2,0,0)),Z*receiverSpeed,default,1,new(1,1,1))).ToArray();
        var objects=new[]{frame,source}.Concat(receivers).Select(Object).ToArray();
        var input=new PointPowerPort(source.Id,frame.Id,default,Z);
        var transfers=receivers.Select(body=>new MechanicalTransferLoad(new(body.Id.Index),new(new(0),input,new(1000,100000)),
            new PointPowerPort(body.Id,frame.Id,default,Z),new(2,100),1e-10)).ToArray();
        if(reverse){Array.Reverse(objects);Array.Reverse(transfers);}
        var world=new PhysicsWorld([],objects,[],new(default,maximumStep:step,maximumEvents:maximumEvents));
        world.ReplaceLoads(new(){Transfers=transfers});
        Array.Clear(transfers); // installed declarations must own their collection
        return(world,source,receivers);
    }
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-9,expected+1e-9);

    [Theory]
    [InlineData(1,false)]
    [InlineData(2,false)]
    [InlineData(4,false)]
    [InlineData(4,true)]
    public void SharedSourceReceivesEveryReactionDuringSamePrediction(int count,bool reverse)
    {
        const double step=.01;
        var (world,source,receivers)=Setup(count,step,reverse,10,4);
        var before=world.Capture();
        var energyBefore=source.KineticEnergy+receivers.Sum(b=>b.KineticEnergy);
        world.Step([],[],step);
        // Independent implicit-midpoint solution for equal unit masses:
        // relative midpoint speed = 6 - (count+1)*force*step/2.
        var force=12/(1+(count+1)*step);
        Near(10-count*force*step,source.LinearVelocity.Z);
        foreach(var body in receivers)Near(4+force*step,body.LinearVelocity.Z);
        Near(10+4*count,source.LinearVelocity.Z+receivers.Sum(b=>b.LinearVelocity.Z));
        var loss=count*force*(force/2)*step;
        Near(energyBefore-loss,source.KineticEnergy+receivers.Sum(b=>b.KineticEnergy));
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(before);world.Step([],[],step);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Equal(count,world.Loads.Transfers.Count);
    }

    [Fact]
    public void LongTransferAdvancesAllTimeWithoutOvershootAndReplays()
    {
        var (world,source,receivers)=Setup(1,1,false,10,4);
        var before=world.Capture();
        var result=world.Step([],[],1);
        Assert.True(result.Events>0);
        Assert.Equal(1,world.Time);
        Assert.InRange(source.LinearVelocity.Z,7,10);
        Assert.InRange(receivers[0].LinearVelocity.Z,4,7);
        Near(14,source.LinearVelocity.Z+receivers[0].LinearVelocity.Z);
        Assert.InRange(source.KineticEnergy+receivers[0].KineticEnergy,49,58);
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(before);
        Assert.Equal(result,world.Step([],[],1));
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void ExhaustedBoundaryBudgetRestoresTheEntireStep()
    {
        var (world,_,_)=Setup(1,1,false,10,4,1);
        var before=world.Capture();
        var failure=Assert.Throws<InvalidOperationException>(()=>world.Step([],[],1));
        Console.WriteLine(failure);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(0,world.Time);Assert.Equal(0UL,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        Assert.Single(world.Loads.Transfers);
    }

    [Fact]
    public void BadCapturedPathStillRejectsActiveWork()
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*10,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(4,0,0)),Z*4,default,1,new(1,1,1));
        var bodies=new[]{frame,source,receiver}.ToDictionary(b=>b.Id);
        var load=new MechanicalTransferLoad(new(0),new(new(0),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000)),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(2,100),1e-10);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [frame.Id]=frame.CreateTrajectory(1,default),
            [source.Id]=source.CreateTrajectory(1,new(-Z*12,default)),
            [receiver.Id]=receiver.CreateTrajectory(1,new(Z*12,default))
        };
        var evaluation=MechanicalTransferSource.EvaluateAll([load],bodies,[],bodies,paths,1,new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(),new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),new Dictionary<MechanicalSourceId,double>{{load.Source.Id,1}}).Single();
        var failure=Assert.Throws<MechanicalTransferWorkException>(()=>evaluation.Certify(bodies,paths,1,load.WorkTolerance));
        Near(81,failure.Work.Supplied);Near(9,failure.Work.Dissipated);
    }

    [Fact]
    public void ForeignReplacementIsAtomic()
    {
        var (world,source,_)=Setup(1,.01,false,10,4);
        var before=world.Loads;
        var bad=new MechanicalTransferLoad(new(0),new(new(0),new PointPowerPort(source.Id,new(100),default,Z),new(1000,100000)),
            new PointPowerPort(new(2),new(0),default,Z),new(2,100),1e-10);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Transfers=[bad]}));
        Assert.Same(before,world.Loads);
        world.Step([],[],.01);
        Assert.True(source.LinearVelocity.Z<10);
    }


    [Fact]
    public void ExternalForceStartsTransferWithinTheSameInterval()
    {
        const double step=.01;
        var (world,source,receivers)=Setup(1,step,false,0,0);
        world.Step([new(source.Id,Z*12,default)],[],step);
        var force=12*step/(1+2*step);
        Near((12-force)*step,source.LinearVelocity.Z);
        Near(force*step,receivers[0].LinearVelocity.Z);
        Assert.True(receivers[0].LinearVelocity.Z>0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(2)]
    public void StationaryRefillAndOutrunningControlsDoNotDrive(double sourceSpeed)
    {
        var (world,source,receivers)=Setup(1,.01,false,sourceSpeed,4);
        world.Step([],[],.01);
        Assert.Equal(sourceSpeed,source.LinearVelocity.Z);
        Assert.Equal(4,receivers[0].LinearVelocity.Z);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidWorkToleranceRejects(double tolerance)
    {
        var port=new PointPowerPort(new(1),new(0),default,Z);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferLoad(new(0),new(new(0),port,new(1000,100000)),port,new(1,1),tolerance));
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    [InlineData(true,true)]
    public void OneCompressionPortFeedsBodyAndRotorWithoutDuplicatingWork(bool reverse,bool saturated)
    {
        const double step=.01,ratio=3,arm=.5,inertia=.25;
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var plate=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,-Z*2,default,1,new(1,1,1));
        var cargo=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z,default,1,new(1,1,1));
        var carrier=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(new(4,0,0)),default,default);
        var rotor=new PhysicsBody(new(4),PhysicsMotionType.Dynamic,carrier.Pose,default,Z*2,1,new(inertia,inertia,inertia));
        var guide=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,plate,new(default,RigidRotation.Identity),
            frame,new(new(4,0,0),RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var hinge=new PhysicsFrameJoint(new(1),FrameJointKind.Hinge,rotor,new(default,RigidRotation.Identity),
            carrier,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var source=new AxialPowerPort(guide.Id,FrameJointKind.Slider,-ratio);
        var impedance=new JetTransferImpedance(2,saturated?1:100);
        var transfers=new MechanicalTransferLoad[]{
            new(new(0),new(new(0),source,new(1000,100000)),new PointPowerPort(cargo.Id,frame.Id,default,Z),impedance,1e-10),
            new(new(1),new(new(0),source,new(1000,100000)),new AxialPowerPort(hinge.Id,FrameJointKind.Hinge,arm),impedance,1e-10)};
        var objects=new[]{frame,plate,cargo,carrier,rotor}.Select(Object).ToArray();
        var joints=new PhysicsJoint[]{guide,hinge};
        if(reverse){Array.Reverse(objects);Array.Reverse(joints);Array.Reverse(transfers);}
        var world=new PhysicsWorld([],objects,joints,new(default,maximumStep:step));
        world.ReplaceLoads(new(){Transfers=transfers});
        var initial=world.Capture();
        var initialEnergy=plate.KineticEnergy+cargo.KineticEnergy+rotor.KineticEnergy;
        world.Step([],[],step);
        // Equal output admittances: arm^2 / inertia = 1 / cargo mass.
        // Both branches have slip 5 - (2*ratio^2 + 1)*force*step/2.
        var force=saturated?1:10/(1+19*step);
        Near(-2+2*ratio*force*step,plate.LinearVelocity.Z);
        Near(1+force*step,cargo.LinearVelocity.Z);
        Near(2+arm/inertia*force*step,rotor.AngularVelocity.Z);
        var loss=2*force*(5-19*force*step/2)*step;
        Near(initialEnergy-loss,plate.KineticEnergy+cargo.KineticEnergy+rotor.KineticEnergy);
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(initial);world.Step([],[],step);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
        world.Restore(initial);
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([hinge]));
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        world.Step([],[],step);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }

    public enum SourceLimit { Force, Power }

    [Theory]
    [InlineData(SourceLimit.Force,2,false)]
    [InlineData(SourceLimit.Force,4,true)]
    [InlineData(SourceLimit.Power,2,false)]
    [InlineData(SourceLimit.Power,4,true)]
    public void SharedRatingLimitsAllBranchesInsidePrediction(SourceLimit limit,int count,bool reverse)
    {
        const double step=.01;
        var (world,source,receivers)=Setup(count,step,reverse,10,4);
        var rating=limit switch
        {
            SourceLimit.Force=>new MechanicalSourceRating(4,1000),
            SourceLimit.Power=>new MechanicalSourceRating(1000,20),
            _=>throw new ArgumentOutOfRangeException(nameof(limit))
        };
        world.ReplaceLoads(new(){Transfers=world.Loads.Transfers.Select(load=>new MechanicalTransferLoad(load.Id,
            new(load.Source.Id,load.Source.MechanicalPort,rating),load.Receiver,load.Impedance,load.WorkTolerance)).ToArray()});
        var before=world.Capture();
        var energy=source.KineticEnergy+receivers.Sum(body=>body.KineticEnergy);
        var result=world.Step([],[],step);
        // Source slows throughout this interval, so peak speed is the initial 10.
        var force=limit==SourceLimit.Force?4:20.0/10;
        Near(10-force*step,source.LinearVelocity.Z);
        foreach(var body in receivers)Near(4+force*step/count,body.LinearVelocity.Z);
        Near(10+4*count,source.LinearVelocity.Z+receivers.Sum(body=>body.LinearVelocity.Z));
        var loss=force*(6-(1+1.0/count)*force*step/2)*step;
        Near(energy-loss,source.KineticEnergy+receivers.Sum(body=>body.KineticEnergy));
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(before);
        Assert.Equal(result,world.Step([],[],step));
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void DuplicateBranchAndConflictingSourceRejectAtomically()
    {
        var (world,_,_)=Setup(2,.01,false,10,4);
        var prior=world.Loads;
        var a=prior.Transfers[0];var b=prior.Transfers[1];
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Transfers=[a,a]}));
        Assert.Same(prior,world.Loads);
        var conflict=new MechanicalTransferLoad(b.Id,new(a.Source.Id,a.Source.MechanicalPort,new(1,1)),
            b.Receiver,b.Impedance,b.WorkTolerance);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Transfers=[a,conflict]}));
        Assert.Same(prior,world.Loads);
        world.Step([],[],.01);
    }

    [Fact]
    public void NegativeSourceAndBranchIdentitiesReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalSourceId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferId(-1));
    }

    [Fact]
    public void AcceleratingSourceRespectsEndpointPeakRatherThanMidpointPower()
    {
        const double step=.01;
        var (world,source,receivers)=Setup(2,step,false,10,4);
        world.ReplaceLoads(new(){Transfers=world.Loads.Transfers.Select(load=>new MechanicalTransferLoad(load.Id,
            new(load.Source.Id,load.Source.MechanicalPort,new(1000,20)),load.Receiver,load.Impedance,load.WorkTolerance)).ToArray()});
        var before=world.Capture();
        var result=world.Step([new(source.Id,Z*12,default)],[],step);
        // F*(10+(12-F)*h)=20; endpoint is the largest source speed.
        var freeEnd=10+12*step;
        var force=40/(freeEnd+Math.Sqrt(freeEnd*freeEnd-80*step));
        Near(freeEnd-force*step,source.LinearVelocity.Z);
        foreach(var body in receivers)Near(4+force*step/2,body.LinearVelocity.Z);
        Near(20,force*source.LinearVelocity.Z);
        Near(18+12*step,source.LinearVelocity.Z+receivers.Sum(body=>body.LinearVelocity.Z));
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(before);
        Assert.Equal(result,world.Step([new(source.Id,Z*12,default)],[],step));
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }
}
