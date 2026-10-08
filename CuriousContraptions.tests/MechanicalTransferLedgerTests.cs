using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalTransferLedgerTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static (PhysicsWorld World,PhysicsBody Source,PhysicsBody Receiver) Setup(double maximumStep,int maximumEvents=256)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*10,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*4,default,1,new(1,1,1));
        PhysicsObject Object(PhysicsBody body)=>new(body,
            new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],new[]{frame,source,receiver}.Select(Object).ToArray(),[],
            new(default,maximumStep:maximumStep,maximumEvents:maximumEvents));
        world.ReplaceLoads(new(){Transfers=[new(new(3),
            new(new(5),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000)),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(2,100),1e-10)]});
        return(world,source,receiver);
    }
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-8,expected+1e-8);

    [Fact]
    public void SubintervalReportsCommitOnceAndRestoreExactly()
    {
        var (world,source,receiver)=Setup(.005);
        var initial=world.Capture();
        var sourceEnergy=source.KineticEnergy;var receiverEnergy=receiver.KineticEnergy;
        var result=world.Step([],[],.01);
        var use=Assert.Single(world.TransferUse.ToArray());
        Assert.Equal(new MechanicalTransferId(3),use.Transfer);
        Assert.Equal(new MechanicalSourceId(5),use.Source);
        Assert.Equal(2UL,use.Intervals);
        Assert.Equal(1e-10,world.TransferError.Limit);
        Assert.InRange(world.TransferError.UpperBound,0,world.TransferError.Limit);
        Assert.Equal(2UL,world.TransferResidualUse.Intervals);
        Assert.Equal(world.TransferResidualUse,world.TransferResidualTotal);
        Near(sourceEnergy-source.KineticEnergy,use.SourceExtraction.Supplied);
        Near(10-source.LinearVelocity.Z,use.Impulse);
        Near(receiver.LinearVelocity.Z-4,use.Impulse);
        Near(receiver.KineticEnergy-receiverEnergy,use.ReceiverDelivery.Supplied);
        Near(use.SourceExtraction.Supplied-use.ReceiverDelivery.Supplied,use.PairedWork.Dissipated);
        Assert.Equal(use,Assert.Single(world.TransferTotals.ToArray()));
        var saved=world.Capture();
        world.Step([],[],.01);
        var total=Assert.Single(world.TransferTotals.ToArray());
        Assert.Equal(4UL,total.Intervals);
        Assert.Equal(4UL,world.TransferResidualTotal.Intervals);
        var firstResidual=saved.TransferResidualUse.Work;
        var secondResidual=world.TransferResidualUse.Work;
        var accumulated=world.TransferResidualTotal.Work;
        Assert.Equal(firstResidual.Supplied+secondResidual.Supplied,accumulated.Supplied);
        Assert.Equal(firstResidual.Dissipated+secondResidual.Dissipated,accumulated.Dissipated);
        Assert.Equal(firstResidual.SuppliedErrorBound+secondResidual.SuppliedErrorBound,accumulated.SuppliedErrorBound);
        Assert.Equal(firstResidual.DissipatedErrorBound+secondResidual.DissipatedErrorBound,accumulated.DissipatedErrorBound);
        Assert.Equal(Math.Max(firstResidual.SuppliedPowerUpperBound,secondResidual.SuppliedPowerUpperBound),accumulated.SuppliedPowerUpperBound);
        Assert.Equal(Math.Max(firstResidual.DissipatedPowerUpperBound,secondResidual.DissipatedPowerUpperBound),accumulated.DissipatedPowerUpperBound);
        Near(sourceEnergy-source.KineticEnergy,total.SourceExtraction.Supplied);
        Near(10-source.LinearVelocity.Z,total.Impulse);
        Near(receiver.LinearVelocity.Z-4,total.Impulse);
        Near(use.Impulse+world.TransferUse[0].Impulse,total.Impulse);
        Near(receiver.KineticEnergy-receiverEnergy,total.ReceiverDelivery.Supplied);
        Assert.Equal(use,saved.TransferTotals[0]);
        world.Restore(saved);
        Assert.Equal(saved.TransferError,world.TransferError);
        Assert.Equal(saved.TransferResidualUse,world.TransferResidualUse);
        Assert.Equal(saved.TransferResidualTotal,world.TransferResidualTotal);
        Assert.Equal(saved.TransferUse.ToArray(),world.TransferUse.ToArray());
        Assert.Equal(saved.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Equal(saved.TransferBodyImpulses.ToArray(),world.TransferBodyImpulses.ToArray());
        world.Restore(initial);
        Assert.Equal(default,world.TransferError);
        Assert.Equal(default,world.TransferResidualUse);Assert.Equal(default,world.TransferResidualTotal);
        Assert.Empty(world.TransferUse.ToArray());Assert.Empty(world.TransferTotals.ToArray());
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(use,Assert.Single(world.TransferUse.ToArray()));
        Assert.Equal(saved.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(.125)]
    [InlineData(.03125)]
    public void StationaryReceiverHasAcceptedImpulseWithoutReceiverWork(double maximumStep)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(2,0,0)),default,default);
        PhysicsObject Object(PhysicsBody body)=>new(body,
            new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(frame),Object(receiver)],[],new(default,maximumStep:maximumStep));
        world.InstallEnergyStores([new(frame.Id,100,100)]);
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),frame.Id,12,new(3,36))),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(3,3),1e-10)]});
        var initial=world.Capture();
        world.Step([],[],.125);
        var use=Assert.Single(world.TransferUse.ToArray());
        Assert.Equal(.375,use.Impulse);
        Assert.Equal(0,use.ReceiverDelivery.Supplied);
        Assert.Equal(0,use.ReceiverDelivery.Dissipated);
        Assert.Equal(4.5,world.EnergyStore(frame.Id).ReleasedEnergy);
        Assert.Equal(use.Impulse,Assert.Single(world.SourceUse.ToArray()).Impulse);
        var final=world.Capture();
        world.Restore(initial);world.Step([],[],.125);
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Equal(final.TransferBodyImpulses.ToArray(),world.TransferBodyImpulses.ToArray());
        Assert.Equal(final.SourceTotals.ToArray(),world.SourceTotals.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.MaxValue)]
    public void InvalidOrOverflowingImpulseCannotEnterAccumulatedTotals(double impulse)
    {
        var branch=new MechanicalTransferTotals(new(0),new(0),1,default,default,default,impulse);
        var source=new MechanicalSourceTotals(new(0),1,default,impulse);
        Assert.Throws<InvalidOperationException>(()=>branch.Add(branch));
        Assert.Throws<InvalidOperationException>(()=>source.Add(source));
    }

    [Fact]
    public void FailedStepPreservesPreviouslyCommittedLedger()
    {
        var (world,_,_)=Setup(1,1);
        world.Step([],[],.01);
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],1));
        Assert.Equal(before.TransferUse.ToArray(),world.TransferUse.ToArray());
        Assert.Equal(before.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Equal(before.TransferBodyImpulses.ToArray(),world.TransferBodyImpulses.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.TransferError,world.TransferError);
        Assert.Equal(before.TransferResidualUse,world.TransferResidualUse);
        Assert.Equal(before.TransferResidualTotal,world.TransferResidualTotal);
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }

    [Fact]
    public void RemovingLoadsRetainsHistoryButClearsLastStepUse()
    {
        var (world,_,_)=Setup(.01);
        world.Step([],[],.01);var before=world.Capture();
        world.ReplaceLoads(new());
        world.Step([],[],.01);
        Assert.Empty(world.TransferUse.ToArray());
        Assert.Equal(default,world.TransferResidualUse);
        Assert.Equal(before.TransferResidualTotal,world.TransferResidualTotal);
        Assert.Equal(before.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Empty(world.TransferBodyImpulses.ToArray());
        world.Restore(before);
        Assert.Single(world.Loads.Transfers);
        Assert.Equal(before.TransferUse.ToArray(),world.TransferUse.ToArray());
    }

    [Fact]
    public void AccountedBranchCannotBeReassignedToAnotherSource()
    {
        var (world,_,_)=Setup(.01);
        world.Step([],[],.01);
        var prior=world.Loads;var load=prior.Transfers[0];
        var replacement=new MechanicalTransferLoad(load.Id,new(new(99),load.Source.MechanicalPort,load.Source.Rating),
            load.Receiver,load.Impedance,load.WorkTolerance);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Transfers=[replacement]}));
        Assert.Same(prior,world.Loads);
        Assert.Equal(new MechanicalSourceId(5),world.TransferTotals[0].Source);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CollisionCutsChargeOnlyAcceptedTransferIntervals(bool intersecting)
    {
        const double duration=.04;
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*10,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*4,default,1,new(1,1,1));
        var obstacle=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(new(intersecting?2:3,0,.1)),default,default);
        PhysicsObject Object(PhysicsBody body)=>new(body,
            new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(1,0,0));
        var world=new PhysicsWorld([],new[]{frame,source,receiver,obstacle}.Select(Object).ToArray(),[],
            new(default,maximumStep:duration));
        world.ReplaceLoads(new(){Transfers=[new(new(3),
            new(new(5),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000)),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(2,100),1e-10)]});
        var initial=world.Capture();
        var sourceEnergy=source.KineticEnergy;var receiverEnergy=receiver.KineticEnergy;
        var result=world.Step([],[],duration);
        var use=Assert.Single(world.TransferUse.ToArray());
        Assert.Equal(duration,world.Time);
        var sourceImpulse=Assert.Single(world.TransferBodyImpulses.ToArray(),value=>
            value.Key.Body==source.Id&&value.Key.Role==TransferPortRole.Source);
        Near(source.LinearVelocity.Z-10,sourceImpulse.Linear.Z);
        if(intersecting)
        {
            Assert.NotEmpty(world.Impacts.ToArray());
            Assert.True(receiver.LinearVelocity.Z<0);
            Assert.True(use.Intervals>=2);
            Assert.True((ulong)result.PredictionCalls>use.Intervals);
            Assert.True(use.ReceiverDelivery.Dissipated>0);
        }
        else
        {
            Assert.Empty(world.Impacts.ToArray());
            Assert.True(receiver.LinearVelocity.Z>0);
            Assert.Equal(1UL,use.Intervals);
            Assert.Equal(0,use.ReceiverDelivery.Dissipated);
        }
        // Elastic, frictionless contact preserves KE at impact; all net work
        // here must come from accepted transfer intervals, including counterflow.
        Near(sourceEnergy-source.KineticEnergy,use.SourceExtraction.Supplied-use.SourceExtraction.Dissipated);
        Near(receiver.KineticEnergy-receiverEnergy,use.ReceiverDelivery.Supplied-use.ReceiverDelivery.Dissipated);
        Near(sourceEnergy+receiverEnergy-source.KineticEnergy-receiver.KineticEnergy,
            use.PairedWork.Dissipated-use.PairedWork.Supplied);
        var final=world.Capture();
        world.Restore(initial);
        Assert.Empty(world.TransferTotals.ToArray());
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Equal(final.TransferBodyImpulses.ToArray(),world.TransferBodyImpulses.ToArray());
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void SubintervalsShareWorkBudgetAndReplayPreviouslyCommittedStep()
    {
        var (world,_,_)=Setup(.001);
        var load=Assert.Single(world.Loads.Transfers);
        world.ReplaceLoads(new(){Transfers=[new(load.Id,load.Source,load.Receiver,load.Impedance,1e-15)]});
        world.Step([],[],.001);
        var before=world.Capture();
        world.Step([],[],.3);
        Assert.Equal(1e-15,world.TransferError.Limit);
        Assert.InRange(world.TransferError.UpperBound,0,world.TransferError.Limit);
        Assert.True(Assert.Single(world.TransferUse.ToArray()).Intervals>=300);
        var after=world.Capture();
        world.Restore(before);
        world.Step([],[],.3);
        var replay=world.Capture();
        Assert.Equal(after.BodyStates.ToArray(),replay.BodyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),replay.TransferTotals.ToArray());
        Assert.Equal(after.SourceTotals.ToArray(),replay.SourceTotals.ToArray());
        Assert.Equal(after.TransferError,replay.TransferError);
        Assert.Equal(after.TransferResidualTotal,replay.TransferResidualTotal);
        Assert.Equal(after.Time,replay.Time);Assert.Equal(after.StepIndex,replay.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }
}
