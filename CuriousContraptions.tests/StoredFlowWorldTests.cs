using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class StoredFlowWorldTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-9,expected+1e-9);

    [Theory]
    [InlineData(0,false)]
    [InlineData(.05,false)]
    [InlineData(.05,true)]
    [InlineData(10,false)]
    public void StoredSourceSharesFiniteWorkWithoutDecorativePhysics(double energy,bool reverse)
    {
        const double duration=.01;
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var receivers=new[]{
            new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1)),
            new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),default,default,1,new(1,1,1))};
        var bodies=new[]{owner}.Concat(receivers).ToArray();
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],bodies.Select(Object).ToArray(),[],new(default,maximumStep:duration));
        world.InstallEnergyStores([new(owner.Id,10,0)]);world.ChargeEnergyStore(owner.Id,energy,1);
        var supply=new MechanicalTransferSource(new StoredFlowSource(new(7),owner.Id,10,new(100,1000)));
        var transfers=receivers.Select((body,i)=>new MechanicalTransferLoad(new(i),supply,
            new PointPowerPort(body.Id,owner.Id,default,Z),new(2,100),1e-10)).ToArray();
        if(reverse)Array.Reverse(transfers);
        var loads=new PhysicsLoadSet{Transfers=transfers};world.ReplaceLoads(loads);
        var initial=world.Capture();var store=world.EnergyStore(owner.Id);
        var prediction=AccelerationSolver.Predict(loads,new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),bodies,[],[],
            bodies.ToDictionary(body=>body.Id,_=>default(BodyWrench)),duration,duration,1e-7,1e-8,1e-9,[],
            world.EnergyStores.ToArray().ToDictionary(value=>value.Owner), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Assert.Equal(store,world.EnergyStore(owner.Id));
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.True(prediction.Transfers.Sum(value=>value.SourceExtraction.Supplied)<=energy);
        var result=world.Step([],[],duration);
        var force=Math.Min(20/(1+duration),energy/(2*10*duration));
        foreach(var body in receivers)Near(force*duration,body.LinearVelocity.Z);
        var used=2*force*10*duration;
        Near(used,world.EnergyStore(owner.Id).ReleasedEnergy);
        Near(energy-used,world.EnergyStore(owner.Id).Energy);
        var report=Assert.Single(world.SourceUse.ToArray());Near(used,report.Extraction.Supplied);
        var gain=receivers.Sum(body=>body.KineticEnergy);
        Near(used-gain,world.TransferUse.ToArray().Sum(value=>value.PairedWork.Dissipated));
        var final=world.Capture();var finalStore=world.EnergyStore(owner.Id);
        world.Restore(initial);
        Assert.Equal(store,world.EnergyStore(owner.Id));
        Assert.Empty(world.SourceTotals.ToArray());
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(finalStore,world.EnergyStore(owner.Id));
        Assert.Equal(final.SourceTotals.ToArray(),world.SourceTotals.ToArray());
    }

    [Fact]
    public void MissingReservoirRejectsBeforeInstallingLoads()
    {
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),default,default,1,new(1,1,1));
        PhysicsObject Object(PhysicsBody value)=>new(value,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(owner),Object(body)],[],new(default));
        var prior=world.Loads;
        var source=new MechanicalTransferSource(new StoredFlowSource(new(0),owner.Id,10,new(10,100)));
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Transfers=[
            new(new(0),source,new PointPowerPort(body.Id,owner.Id,default,Z),new(1,10),1e-10)]}));
        Assert.Same(prior,world.Loads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DistinctSourcesShareReservoirAndRechargeWithoutReusingWork(bool reverse)
    {
        const double duration=.125;
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var receivers=new[]{
            new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1)),
            new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),default,default,1,new(1,1,1))};
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],new[]{owner}.Concat(receivers).Select(Object).ToArray(),[],new(default,maximumStep:duration));
        world.InstallEnergyStores([new(owner.Id,2,0)]);
        world.ChargeEnergyStore(owner.Id,.5,1);
        var loads=receivers.Select((body,i)=>new MechanicalTransferLoad(new(i),
            new(new StoredFlowSource(new(i+10),owner.Id,8,new(100,1000))),
            new PointPowerPort(body.Id,owner.Id,default,Z),new(1,1),1e-10)).ToArray();
        if(reverse)Array.Reverse(loads);
        world.ReplaceLoads(new(){Transfers=loads});
        var initial=world.Capture();
        void DischargeAndRecharge()
        {
            world.Step([],[],duration);
            foreach(var body in receivers)Near(.03125,body.LinearVelocity.Z);
            var first=world.EnergyStore(owner.Id);
            Assert.Equal(0,first.Energy);
            Near(.5,first.ReleasedEnergy);
            Assert.Equal(2,world.SourceUse.Length);
            foreach(var source in world.SourceUse)Near(.25,source.Extraction.Supplied);
            // Exact binary allocation exhausts the balance without erasing residue.
            world.Step([],[],duration);
            foreach(var body in receivers)Near(.03125,body.LinearVelocity.Z);
            var second=world.EnergyStore(owner.Id);
            Assert.Equal(first,second);
            Assert.Equal(0,world.SourceUse.ToArray().Sum(source=>source.Extraction.Supplied));
            Assert.InRange(second.ReleasedEnergy,first.ReleasedEnergy,.5);
            Near(.5,world.ChargeEnergyStore(owner.Id,.5,1));
            world.Step([],[],duration);
            foreach(var body in receivers)Near(.0625,body.LinearVelocity.Z);
            var final=world.EnergyStore(owner.Id);
            Assert.Equal(0,final.Energy);
            Near(1,final.AcceptedEnergy);Near(1,final.ReleasedEnergy);
            Near(final.AcceptedEnergy,final.Energy+final.ReleasedEnergy);
            Near(final.ReleasedEnergy,world.SourceTotals.ToArray().Sum(source=>source.Extraction.Supplied));
            Near(final.ReleasedEnergy-receivers.Sum(body=>body.KineticEnergy),
                world.TransferTotals.ToArray().Sum(branch=>branch.PairedWork.Dissipated));
        }
        DischargeAndRecharge();
        var final=world.Capture();
        Assert.Equal(3UL,world.TransferResidualTotal.Intervals);
        Assert.Equal(1UL,world.TransferResidualUse.Intervals);
        world.Restore(initial);
        Assert.Equal(default,world.TransferResidualTotal);
        DischargeAndRecharge();
        var replay=world.Capture();
        Assert.Equal(final.BodyStates.ToArray(),replay.BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),replay.EnergyStates.ToArray());
        Assert.Equal(final.SourceTotals.ToArray(),replay.SourceTotals.ToArray());
        Assert.Equal(final.TransferResidualUse,replay.TransferResidualUse);
        Assert.Equal(final.TransferResidualTotal,replay.TransferResidualTotal);
        Assert.Equal(final.TransferTotals.ToArray(),replay.TransferTotals.ToArray());
    }

    [Theory]
    [InlineData(true,false)]
    [InlineData(false,false)]
    [InlineData(true,true)]
    public void CollisionCutsDebitAcceptedWorkAndFailureRestoresStore(bool intersecting,bool fail)
    {
        const double duration=.1;
        var owner=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*4,default,1,new(1,1,1));
        var obstacles=new[]{
            new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(intersecting?0:2,0,.1)),default,default),
            new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(new(intersecting?0:2,0,-.1)),default,default)};
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(1,0,0));
        var world=new PhysicsWorld([],new[]{owner,receiver}.Concat(obstacles).Select(Object).ToArray(),[],
            new(default,maximumStep:duration,maximumEvents:fail?1:256));
        world.InstallEnergyStores([new(owner.Id,10,0)]);world.ChargeEnergyStore(owner.Id,10,1);
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),owner.Id,8,new(100,1000))),
            new PointPowerPort(receiver.Id,owner.Id,default,Z),new(1,1),1e-10)]});
        var initial=world.Capture();var energy=receiver.KineticEnergy;
        if(fail)
        {
            Assert.Throws<InvalidOperationException>(()=>world.Step([],[],duration));
            var after=world.Capture();
            Assert.Equal(initial.BodyStates.ToArray(),after.BodyStates.ToArray());
            Assert.Equal(initial.EnergyStates.ToArray(),after.EnergyStates.ToArray());
            Assert.Equal(initial.TransferTotals.ToArray(),after.TransferTotals.ToArray());
            Assert.Equal(initial.SourceTotals.ToArray(),after.SourceTotals.ToArray());
            Assert.Equal(initial.TransferResidualUse,after.TransferResidualUse);
            Assert.Equal(initial.TransferResidualTotal,after.TransferResidualTotal);
            Assert.Equal(0,world.Time);Assert.Equal(0UL,world.StepIndex);
            Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
            return;
        }
        var result=world.Step([],[],duration);
        var report=Assert.Single(world.TransferUse.ToArray());
        Assert.Equal(report.Intervals,world.TransferResidualUse.Intervals);
        Assert.Equal(world.TransferResidualUse,world.TransferResidualTotal);
        if(intersecting)
        {
            Assert.True(world.Impacts.Length>=2);
            Assert.True(report.Intervals>=3);
            Assert.True((ulong)result.PredictionCalls>report.Intervals);
            Assert.True(report.ReceiverDelivery.Dissipated>0);
        }
        else
        {
            Assert.Empty(world.Impacts.ToArray());
            Assert.Equal(1UL,report.Intervals);
            Assert.Equal(0,report.ReceiverDelivery.Dissipated);
        }
        // Saturated one-newton branch at eight metres/second: exactly 0.8 J
        // over the full accepted 0.1 s, regardless of discarded predictions.
        Near(.8,world.EnergyStore(owner.Id).ReleasedEnergy);
        Near(9.2,world.EnergyStore(owner.Id).Energy);
        Near(.8,report.SourceExtraction.Supplied);
        Near(receiver.KineticEnergy-energy,report.ReceiverDelivery.Supplied-report.ReceiverDelivery.Dissipated);
        Near(.8-(receiver.KineticEnergy-energy),report.PairedWork.Dissipated);
        var final=world.Capture();
        world.Restore(initial);
        Assert.Equal(result,world.Step([],[],duration));
        var replay=world.Capture();
        Assert.Equal(final.BodyStates.ToArray(),replay.BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),replay.EnergyStates.ToArray());
        Assert.Equal(final.SourceTotals.ToArray(),replay.SourceTotals.ToArray());
        Assert.Equal(final.TransferResidualUse,replay.TransferResidualUse);
        Assert.Equal(final.TransferResidualTotal,replay.TransferResidualTotal);
        Assert.Equal(final.TransferTotals.ToArray(),replay.TransferTotals.ToArray());
    }
}
