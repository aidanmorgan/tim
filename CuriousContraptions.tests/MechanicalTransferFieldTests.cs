using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public enum TransferExposureCase { Exposed, Blocked, DisabledSource, DisabledReceiver, DisabledBlocker, Outside }

public class MechanicalTransferFieldTests
{
    private static readonly CollisionVector X=new(1,0,0);
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(.01),AffineTransform.Identity)]);
    private static void Near(double expected,double actual,double tolerance=1e-9)=>
        Assert.InRange(actual,expected-tolerance,expected+tolerance);

    [Theory]
    [InlineData(TransferExposureCase.Exposed)]
    [InlineData(TransferExposureCase.Blocked)]
    [InlineData(TransferExposureCase.DisabledSource)]
    [InlineData(TransferExposureCase.DisabledReceiver)]
    [InlineData(TransferExposureCase.DisabledBlocker)]
    [InlineData(TransferExposureCase.Outside)]
    public void ExposureGatesPairedWorkAndStoreDebitAndRestores(TransferExposureCase exposure)
    {
        const double duration=.01;
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,
            RigidPose.At(new(2,exposure==TransferExposureCase.Outside?2:0,0)),default,default,1,new(1,1,1));
        var blocker=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(1,
            exposure is TransferExposureCase.Blocked or TransferExposureCase.DisabledBlocker?0:2,0)),default,default);
        CollisionParticipation Participation(PhysicsBody body)=>
            (body==source&&exposure==TransferExposureCase.DisabledSource)||
            (body==receiver&&exposure==TransferExposureCase.DisabledReceiver)||
            (body==blocker&&exposure==TransferExposureCase.DisabledBlocker)
                ?CollisionParticipation.Disabled:CollisionParticipation.Enabled;
        var world=new PhysicsWorld([],new[]{source,receiver,blocker}.Select(body=>
            new PhysicsObject(body,Shape,new(0,0,0)){InitialParticipation=Participation(body)}).ToArray(),
            [],new(default,maximumStep:duration));
        world.InstallEnergyStores([new(source.Id,1,0)]);world.ChargeEnergyStore(source.Id,1,1);
        var load=new MechanicalTransferLoad(new(0),
            new(new StoredFlowSource(new(0),source.Id,4,new(10,100))),
            new PointPowerPort(receiver.Id,source.Id,default,X),new(1,1),1e-10)
            {Field=new(source.Id,receiver.Id,default,X,default,4,1,[])};
        world.ReplaceLoads(new(){Transfers=[load]});
        var initial=world.Capture();
        var result=world.Step([],[],duration);
        var active=exposure is TransferExposureCase.Exposed or TransferExposureCase.DisabledBlocker;
        Near(active?.01:0,receiver.LinearVelocity.X);
        Near(active?.04:0,world.EnergyStore(source.Id).ReleasedEnergy);
        var work=Assert.Single(world.TransferUse.ToArray());
        Near(active?.04:0,work.SourceExtraction.Supplied);
        Near(receiver.KineticEnergy,work.ReceiverDelivery.Supplied);
        Near(work.SourceExtraction.Supplied-receiver.KineticEnergy,work.PairedWork.Dissipated);
        var final=world.Capture();
        world.Restore(initial);
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(1,world.EnergyStore(source.Id).Energy);
        Assert.Same(load,Assert.Single(world.Loads.Transfers));
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.SourceTotals.ToArray(),world.SourceTotals.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void EnteringFieldCutsAcceptedIntervalBeforeSupplyBegins()
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        // Offset from the physical nozzle collider but inside its finite cylinder.
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(-.01,.3,0)),
            X*2,default,1,new(1,1,1));
        var world=new PhysicsWorld([],new[]{source,receiver}.Select(body=>
            new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),[],new(default,maximumStep:.01));
        world.InstallEnergyStores([new(source.Id,1,0)]);world.ChargeEnergyStore(source.Id,1,1);
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),source.Id,4,new(10,100))),
            new PointPowerPort(receiver.Id,source.Id,default,X),new(1,1),1e-10)
            {Field=new(source.Id,receiver.Id,default,X,default,4,1,[])}]});
        var initial=world.Capture();
        var result=world.Step([],[],.01);
        Assert.True(result.Events>0);
        Near(.01,world.Time);
        Near(2.005,receiver.LinearVelocity.X,1e-7);
        Near(.02,world.EnergyStore(source.Id).ReleasedEnergy,2e-7);
        var final=world.Capture();
        world.Restore(initial);
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.SourceTotals.ToArray(),world.SourceTotals.ToArray());
    }

    [Fact]
    public void ForeignFieldBindingRejectsAtomically()
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),default,default,1,new(1,1,1));
        var world=new PhysicsWorld([],new[]{source,receiver}.Select(body=>
            new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),[],new(default));
        world.InstallEnergyStores([new(source.Id,1,0)]);
        var prior=world.Loads;
        var invalid=new MechanicalTransferLoad(new(0),
            new(new StoredFlowSource(new(0),source.Id,4,new(10,100))),
            new PointPowerPort(receiver.Id,source.Id,default,X),new(1,1),1e-10)
            {Field=new(source.Id,new(99),default,X,default,4,1,[])};
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Transfers=[invalid]}));
        Assert.Same(prior,world.Loads);
        Assert.Equal(0,world.Time);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnexposedBranchConsumesNoSharedMechanicalAllowance(bool reverse)
    {
        const double duration=.01;
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var drive=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,2,0)),X*4,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(X*2),default,default,1,new(1,1,1));
        var outside=new PhysicsBody(new(3),PhysicsMotionType.Dynamic,RigidPose.At(new(2,2,0)),default,default,1,new(1,1,1));
        var bodies=new[]{frame,drive,receiver,outside};
        var world=new PhysicsWorld([],bodies.Select(body=>new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),
            [],new(default,maximumStep:duration));
        var supply=new MechanicalTransferSource(new(0),new PointPowerPort(drive.Id,frame.Id,default,X),new(1,100));
        var branches=new[]{receiver,outside}.Select(body=>new MechanicalTransferLoad(new(body.Id.Index),supply,
            new PointPowerPort(body.Id,frame.Id,default,X),new(1,1),1e-10)
            {Field=new(frame.Id,body.Id,default,X,default,4,1,[])}).ToArray();
        if(reverse)Array.Reverse(branches);
        world.ReplaceLoads(new(){Transfers=branches});
        var before=world.Capture();
        var result=world.Step([],[],duration);
        Near(3.99,drive.LinearVelocity.X);Near(.01,receiver.LinearVelocity.X);
        Assert.Equal(default,outside.LinearVelocity);
        Near(4,drive.LinearVelocity.X+receiver.LinearVelocity.X);
        Near(.03995,Assert.Single(world.SourceUse.ToArray()).Extraction.Supplied);
        Near(.0399,world.TransferUse.ToArray().Sum(value=>value.PairedWork.Dissipated));
        var inactive=Assert.Single(world.TransferUse.ToArray(),value=>value.Transfer==new MechanicalTransferId(outside.Id.Index));
        Assert.Equal(default,inactive.SourceExtraction);
        var final=world.Capture();
        world.Restore(before);
        Assert.Equal(result,world.Step([],[],duration));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Theory]
    [InlineData(AirJetBoundary.Outlet)]
    [InlineData(AirJetBoundary.Rim)]
    public void LeavingFieldStopsSupplyAtCapturedSurface(AirJetBoundary boundary)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var (position,velocity,exitTime)=boundary switch
        {
            AirJetBoundary.Outlet=>(new CollisionVector(3.99,.3,0),X*2,Math.Sqrt(4.02)-2),
            AirJetBoundary.Rim=>(new CollisionVector(2,.99,0),new CollisionVector(0,2,0),.005),
            _=>throw new ArgumentOutOfRangeException(nameof(boundary))
        };
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(position),velocity,default,1,new(1,1,1));
        var world=new PhysicsWorld([],new[]{source,receiver}.Select(body=>
            new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),[],new(default,maximumStep:.01));
        world.InstallEnergyStores([new(source.Id,1,0)]);world.ChargeEnergyStore(source.Id,1,1);
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),source.Id,4,new(10,100))),
            new PointPowerPort(receiver.Id,source.Id,default,X),new(1,1),1e-10)
            {Field=new(source.Id,receiver.Id,default,X,default,4,1,[])}]});
        var result=world.Step([],[],.01);
        Assert.True(result.Events>0);
        Near(velocity.X+exitTime,receiver.LinearVelocity.X,1e-7);
        Near(velocity.Y,receiver.LinearVelocity.Y);
        Near(4*exitTime,world.EnergyStore(source.Id).ReleasedEnergy,2e-7);
        var released=world.EnergyStore(source.Id).ReleasedEnergy;
        world.Step([],[],.01);
        Assert.Equal(released,world.EnergyStore(source.Id).ReleasedEnergy);
    }

    [Fact]
    public void UnexposedFlowReversalDoesNotCreateAConstitutiveEvent()
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var drive=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,2,0)),X,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,2,0)),default,default,1,new(1,1,1));
        var world=new PhysicsWorld([],new[]{frame,drive,receiver}.Select(body=>
            new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),[],new(default,maximumStep:1));
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new(0),new PointPowerPort(drive.Id,frame.Id,default,X),new(10,100)),
            new PointPowerPort(receiver.Id,frame.Id,default,X),new(1,1),1e-10)
            {Field=new(frame.Id,receiver.Id,default,X,default,4,1,[])}]});
        var before=world.Capture();
        PhysicsWrenchCommand[] force=[new(drive.Id,-X*2,default)];
        var result=world.Step(force,[],1);
        Assert.Equal(0,result.Events);
        Near(-1,drive.LinearVelocity.X);
        Assert.Equal(default,receiver.LinearVelocity);
        var work=Assert.Single(world.TransferTotals.ToArray());
        Assert.Equal(default,work.SourceExtraction);
        Assert.Equal(default,work.ReceiverDelivery);
        var after=world.Capture();
        world.Restore(before);
        Assert.Equal(result,world.Step(force,[],1));
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }
}
