using CuriousContraptions.Physics;
using Godot;

namespace CuriousContraptions.Tests;

public class RotaryCaptureDeclarationTests
{
    public enum SupplyCase { Front, Rear, Blocked, Empty, Stopped, Refill }
    private static readonly CollisionVector Z=new(0,0,1);
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(.01),AffineTransform.Identity)]);
    private static (PhysicsWorld World,PhysicsBody Nozzle,PhysicsBody Rotor,RotaryCaptureDeclaration Declaration,PhysicsBody[] Bodies) Setup(SupplyCase supply,bool mixed=false,double flow=12,double initialEnergy=100,
        CollisionVector blockerVelocity=default,double maximumStep=.01)
    {
        if(!Enum.IsDefined(supply))throw new ArgumentOutOfRangeException(nameof(supply));
        var sign=supply==SupplyCase.Rear?-1:1;
        var nozzle=new PhysicsBody(new(0),supply==SupplyCase.Refill?PhysicsMotionType.Kinematic:PhysicsMotionType.Static,
            RigidPose.Identity,supply==SupplyCase.Refill?-Z:default,default);
        var carrier=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(Z*(2*sign)),default,default);
        var rotor=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,carrier.Pose,default,default,1,new(1,1,1));
        var blocker=new PhysicsBody(new(3),blockerVelocity==default?PhysicsMotionType.Static:PhysicsMotionType.Kinematic,
            RigidPose.At(supply==SupplyCase.Blocked?Z:new CollisionVector(3,0,0)),blockerVelocity,default);
        var frame=new JointFrame(default,RigidRotation.Identity);
        var hinge=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,rotor,frame,carrier,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var owned=new List<PhysicsBody>{nozzle,carrier,rotor,blocker};
        if(mixed)owned.Add(new(new(4),PhysicsMotionType.Dynamic,RigidPose.At(new(.5,0,2)),default,default,1,new(1,1,1)));
        var world=new PhysicsWorld([],owned.Select(body=>
            new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),[hinge],new(default,maximumStep:maximumStep));
        world.InstallEnergyStores([new(nozzle.Id,100,supply==SupplyCase.Empty?0:initialEnergy)]);
        var source=supply==SupplyCase.Refill?
            new MechanicalTransferSource(new(0),new PointPowerPort(nozzle.Id,carrier.Id,default,Z),new(9,108)):
            new MechanicalTransferSource(new StoredFlowSource(new(0),nozzle.Id,supply==SupplyCase.Stopped?0:flow,new(9,108)));
        var field=new AirJetGeometry(nozzle.Id,carrier.Id,default,Z*sign,default,4,1,[rotor.Id]);
        RotaryCaptureTransfer[] branches=[new(new(0),source,field,new(.75,9),1e-10)];
        var declaration=new RotaryCaptureDeclaration(hinge.Id,new(.4,2.0/3,12,.05),branches,1e-7);
        Array.Clear(branches);
        return(world,nozzle,rotor,declaration,owned.ToArray());
    }

    private static RotaryCapturePreparation Prepare(PhysicsWorld world,RotaryCaptureDeclaration declaration,
        PhysicsBody[] bodies)
    {
        var participants=bodies.ToDictionary(body=>body.Id);
        return declaration.Prepare(participants,world.Joints.ToArray(),
            participants.Keys.ToDictionary(id=>id,id=>world.Collider(id).Declaration),
            world.EnergyStores.ToArray().ToDictionary(store=>store.Owner));
    }

    [Theory]
    [InlineData(SupplyCase.Front)]
    [InlineData(SupplyCase.Rear)]
    [InlineData(SupplyCase.Blocked)]
    [InlineData(SupplyCase.Empty)]
    [InlineData(SupplyCase.Stopped)]
    [InlineData(SupplyCase.Refill)]
    public void TypedPreparationPreservesIdentityPaidWorkAndReplay(SupplyCase supply)
    {
        var (world,nozzle,rotor,declaration,bodies)=Setup(supply);
        var beforePreparation=world.Capture();
        var prepared=Prepare(world,declaration,bodies);
        Assert.Equal(beforePreparation.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(beforePreparation.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Single(declaration.Branches);
        Assert.Single(prepared.Transfers);
        Assert.Equal(new MechanicalTransferId(0),prepared.Transfers[0].Id);
        var active=supply is SupplyCase.Front or SupplyCase.Rear;
        Assert.Equal(active?RotaryCaptureState.Driven:RotaryCaptureState.Passive,prepared.Setting.State);
        Assert.Equal(active?(supply==SupplyCase.Rear?-6:6):0,prepared.Setting.TargetSpeed);
        world.ReplaceLoads(new(){Transfers=prepared.Transfers,Damping=[prepared.Damping]});
        var before=world.Capture();
        var result=world.Step([],[],.01);
        var released=world.EnergyStore(nozzle.Id).ReleasedEnergy;
        Assert.Equal(active,released>0);
        Assert.Equal(active,Math.Abs(rotor.AngularVelocity.Z)>0);
        Assert.True(rotor.KineticEnergy<=released+1e-8);
        var work=Assert.Single(world.TransferTotals.ToArray());
        Assert.InRange(Math.Abs(work.SourceExtraction.Supplied+work.SourceExtraction.SuppliedErrorBound-released),0,1e-8);
        var after=world.Capture();
        world.Restore(before);
        var renewed=Prepare(world,declaration,bodies);
        Assert.Equal(prepared.Setting,renewed.Setting);
        world.ReplaceLoads(new(){Transfers=renewed.Transfers,Damping=[renewed.Damping]});
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void ParticipationBodyDisablesSupplyWhilePreservingCoastAndReactivation()
    {
        var (world,nozzle,rotor,original,bodies)=Setup(SupplyCase.Front);
        var branch=Assert.Single(original.Branches);
        var owner=bodies.Single(body=>body.Id==new PhysicsBodyId(3));
        var source=branch.Source with {ParticipationBody=owner.Id};
        var declaration=new RotaryCaptureDeclaration(original.Joint,original.Material,
            [new(branch.Id,source,branch.Field,branch.Impedance,branch.WorkTolerance)],original.ForceTolerance);
        world.ReplaceLoads(new(){Rotary=[declaration]});
        var initial=world.Capture();
        void Run()
        {
            world.Step([],[],.01);
            var spin=rotor.AngularVelocity.Z;
            var paid=world.EnergyStore(nozzle.Id).ReleasedEnergy;
            Assert.True(spin>0);Assert.True(paid>0);
            var collider=world.Collider(owner.Id).Declaration;
            world.ApplyColliderUpdates([new(owner.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            Assert.Equal(RotaryCaptureState.Passive,Prepare(world,declaration,bodies).Setting.State);
            world.Step([],[],.01);
            Assert.Equal(paid,world.EnergyStore(nozzle.Id).ReleasedEnergy);
            Assert.InRange(rotor.AngularVelocity.Z,double.Epsilon,Math.BitDecrement(spin));
            world.ApplyColliderUpdates([collider]);
            Assert.Equal(RotaryCaptureState.Driven,Prepare(world,declaration,bodies).Setting.State);
            world.Step([],[],.01);
            Assert.True(world.EnergyStore(nozzle.Id).ReleasedEnergy>paid);
            Assert.True(rotor.KineticEnergy<=world.EnergyStore(nozzle.Id).ReleasedEnergy+1e-8);
        }
        Run();var final=world.Capture();
        world.Restore(initial);Run();
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void MissingHingeAndDuplicateBranchesReject()
    {
        var (world,_,_,declaration,bodies)=Setup(SupplyCase.Front);
        var branch=Assert.Single(declaration.Branches);
        Assert.Throws<ArgumentException>(()=>new RotaryCaptureDeclaration(declaration.Joint,declaration.Material,[branch,branch],declaration.ForceTolerance));
        Assert.Throws<ArgumentException>(()=>new RotaryCaptureDeclaration(declaration.Joint,declaration.Material,[null!],declaration.ForceTolerance));
        Assert.Throws<ArgumentException>(()=>Prepare(world,new(new(99),declaration.Material,[branch],declaration.ForceTolerance),bodies));
    }

    [Fact]
    public void PartialExposureRepreparesWithoutLosingBranchIdentity()
    {
        var (world,_,rotor,original,bodies)=Setup(SupplyCase.Blocked);
        var first=Assert.Single(original.Branches);
        var secondField=new AirJetGeometry(first.Field.Source,first.Field.Body,default,Z,
            new(.5,0,0),4,1,[rotor.Id]);
        var declaration=new RotaryCaptureDeclaration(original.Joint,original.Material,[
            new(new(1),first.Source,secondField,new(.375,4.5),1e-10),
            new(new(0),first.Source,first.Field,new(.375,4.5),1e-10)],original.ForceTolerance);
        var before=world.Capture();
        var partial=Prepare(world,declaration,bodies);
        Assert.Equal(3,partial.Setting.TargetSpeed);
        Assert.Equal(new[]{new MechanicalTransferId(0),new MechanicalTransferId(1)},
            partial.Transfers.Select(load=>load.Id).ToArray());
        var blocker=world.Collider(new(3)).Declaration;
        world.ApplyColliderUpdates([new(blocker.Body,blocker.Geometry,blocker.Material,CollisionParticipation.Disabled)]);
        var full=Prepare(world,declaration,bodies);
        Assert.Equal(6,full.Setting.TargetSpeed);
        Assert.Equal(partial.Transfers.Select(load=>load.Id),full.Transfers.Select(load=>load.Id));
        world.Restore(before);
        Assert.Equal(partial.Setting,Prepare(world,declaration,bodies).Setting);
    }

    [Theory]
    [InlineData(SupplyCase.Front)]
    [InlineData(SupplyCase.Rear)]
    [InlineData(SupplyCase.Blocked)]
    [InlineData(SupplyCase.Empty)]
    [InlineData(SupplyCase.Stopped)]
    [InlineData(SupplyCase.Refill)]
    public void OwnedRotaryLoadsPayAcceptedWorkAndReplay(SupplyCase supply)
    {
        var (world,nozzle,rotor,declaration,_)=Setup(supply);
        var inputs=new[]{declaration};
        var loads=new PhysicsLoadSet{Rotary=inputs};Array.Clear(inputs);
        Assert.False(loads.IsEmpty);Assert.Single(loads.Rotary);
        world.ReplaceLoads(loads);var before=world.Capture();
        var results=Enumerable.Range(0,4).Select(_=>world.Step([],[],.01)).ToArray();
        var active=supply is SupplyCase.Front or SupplyCase.Rear;
        var released=world.EnergyStore(nozzle.Id).ReleasedEnergy;
        Assert.Equal(active,released>0);Assert.Equal(active,Math.Abs(rotor.AngularVelocity.Z)>0);
        Assert.True(rotor.KineticEnergy<=released+1e-8);
        var transfer=Assert.Single(world.TransferTotals.ToArray());
        Assert.Equal(active,transfer.ReceiverDelivery.Supplied>0);
        Assert.InRange(Math.Abs(transfer.SourceExtraction.Supplied+transfer.SourceExtraction.SuppliedErrorBound-released),0,1e-8);
        var after=world.Capture();world.Restore(before);
        Assert.Same(loads,world.Loads);
        for(var index=0;index<results.Length;index++)Assert.Equal(results[index],world.Step([],[],.01));
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Theory]
    [InlineData(.01)]
    [InlineData(.002)]
    [InlineData(.0005)]
    public void MixedLinearRotaryBranchesUseOneFiniteSourceAllocation(double maximumStep)
    {
        var (world,nozzle,rotor,declaration,bodies)=Setup(SupplyCase.Front,mixed:true,maximumStep:maximumStep);
        var branch=Assert.Single(declaration.Branches);var ball=bodies.Single(body=>body.Id==new PhysicsBodyId(4));
        var field=new AirJetGeometry(nozzle.Id,ball.Id,default,Z,default,4,1,[rotor.Id]);
        var linear=new NozzleCoupledJetReceiver().CreateLoad(new(1),branch.Source,field,branch.Impedance,branch.WorkTolerance);
        world.ReplaceLoads(new(){Rotary=[declaration],Transfers=[linear]});
        var before=world.Capture();
        for(var index=0;index<4;index++)world.Step([],[],.01);
        Assert.Equal(declaration.Branches[0].WorkTolerance,world.TransferError.Limit);
        Assert.InRange(world.TransferError.UpperBound,0,world.TransferError.Limit);
        var released=world.EnergyStore(nozzle.Id).ReleasedEnergy;
        Assert.True(ball.LinearVelocity.Z>0);Assert.True(rotor.AngularVelocity.Z>0);
        Assert.InRange(released,0,108*.04+1e-8);
        Assert.True(ball.KineticEnergy+rotor.KineticEnergy<=released+1e-8);
        var totals=world.TransferTotals.ToArray();Assert.Equal(2,totals.Length);
        Assert.All(totals,total=>{Assert.Equal(branch.Source.Id,total.Source);Assert.True(total.ReceiverDelivery.Supplied>0);});
        var after=world.Capture();world.Restore(before);
        for(var index=0;index<4;index++)world.Step([],[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void WorldRejectsDuplicateRotaryAndTransferIdentitiesAtomically()
    {
        var (world,_,_,declaration,bodies)=Setup(SupplyCase.Front);
        var loads=new PhysicsLoadSet{Rotary=[declaration]};world.ReplaceLoads(loads);
        var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Rotary=[declaration,declaration]}));
        var prepared=Prepare(world,declaration,bodies);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(new(){Rotary=[declaration],Transfers=prepared.Transfers}));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        Assert.Same(loads,world.Loads);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }

    [Fact]
    public void ReducedRatioIsRecalibratedInAuthoritativePrediction()
    {
        var (world,nozzle,rotor,declaration,bodies)=Setup(SupplyCase.Front,flow:1);
        declaration=new(declaration.Joint,new(.4,4,12,.05),declaration.Branches,declaration.ForceTolerance);
        Assert.True(Prepare(world,declaration,bodies).Setting.Pitch<declaration.Material.MaximumPitch);
        world.ReplaceLoads(new(){Rotary=[declaration]});
        world.Step([],[],.01);
        Assert.True(rotor.AngularVelocity.Z>0);
        Assert.True(world.EnergyStore(nozzle.Id).ReleasedEnergy>=rotor.KineticEnergy);
    }

    [Fact]
    public void MovingBlockerBoundaryActivatesOwnedRotaryWithoutResubmission()
    {
        var (world,nozzle,rotor,declaration,_)=Setup(SupplyCase.Blocked,blockerVelocity:new(2,0,0));
        world.ReplaceLoads(new(){Rotary=[declaration]});
        var before=world.Capture();world.Step([],[],.01);
        Assert.True(rotor.AngularVelocity.Z>0);
        Assert.True(world.EnergyStore(nozzle.Id).ReleasedEnergy>0);
        var after=world.Capture();world.Restore(before);world.Step([],[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }

    [Fact]
    public void DepletionCoastRechargeAndResetUseOwnedStoreState()
    {
        const double duration=.125;
        var (world,nozzle,rotor,original,bodies)=Setup(SupplyCase.Front,flow:8,initialEnergy:.5,maximumStep:duration);
        var branch=Assert.Single(original.Branches);
        var declaration=new RotaryCaptureDeclaration(original.Joint,original.Material,
            [new(branch.Id,branch.Source,branch.Field,new(100,1),branch.WorkTolerance)],original.ForceTolerance);
        world.ReplaceLoads(new(){Rotary=[declaration]});var before=world.Capture();
        void Cycle()
        {
            world.Step([],[],duration);
            Assert.Equal(0,world.EnergyStore(nozzle.Id).Energy);
            Assert.Equal(RotaryCaptureState.Passive,Prepare(world,declaration,bodies).Setting.State);
            var spin=rotor.AngularVelocity.Z;var released=world.EnergyStore(nozzle.Id).ReleasedEnergy;
            Assert.True(spin>0);
            world.Step([],[],duration);
            var resistance=declaration.Material.MaximumPitch/declaration.Material.SpeedPerForce;
            var expected=spin*(1-resistance*duration*.5)/(1+resistance*duration*.5);
            Assert.InRange(Math.Abs(rotor.AngularVelocity.Z-expected),0,1e-10);
            Assert.Equal(released,world.EnergyStore(nozzle.Id).ReleasedEnergy);
            Assert.Equal(.5,world.ChargeEnergyStore(nozzle.Id,.5,1));
            Assert.Equal(RotaryCaptureState.Driven,Prepare(world,declaration,bodies).Setting.State);
            world.Step([],[],duration);
            Assert.Equal(0,world.EnergyStore(nozzle.Id).Energy);
            var store=world.EnergyStore(nozzle.Id);
            Assert.Equal(store.InitialEnergy+store.AcceptedEnergy,store.ReleasedEnergy);
        }
        Cycle();var after=world.Capture();world.Restore(before);Cycle();
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void NoBranchesStillProvidesPassiveOwnedDamping()
    {
        var (world,_,rotor,original,_)=Setup(SupplyCase.Stopped);
        var declaration=new RotaryCaptureDeclaration(original.Joint,original.Material,[],original.ForceTolerance);
        world.ReplaceLoads(new(){Rotary=[declaration]});
        world.ApplyAngularImpulse(rotor.Id,Z*.3);
        var before=world.Capture();world.Step([],[],.01);
        var expected=.3*(1-.6*.005)/(1+.6*.005);
        Assert.InRange(Math.Abs(rotor.AngularVelocity.Z-expected),0,1e-10);
        Assert.Empty(world.TransferTotals.ToArray());
        var after=world.Capture();world.Restore(before);world.Step([],[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.Epsilon)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(.1)]
    public void OwnedForceToleranceRejectsUnsupportedBands(double tolerance)
    {
        var (_,_,_,declaration,_)=Setup(SupplyCase.Front);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureDeclaration(declaration.Joint,
            declaration.Material,declaration.Branches,tolerance));
    }

    [Fact]
    public void MechanicalSupplyPaysNozzleReactionAndRotorWorkWithExactReplay()
    {
        var nozzle=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*12,default,1,new(1,1,1));
        var carrier=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(Z*2),default,default);
        var rotor=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,carrier.Pose,default,default,1,new(1,1,1));
        var frame=new JointFrame(default,RigidRotation.Identity);
        var hinge=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,rotor,frame,carrier,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],new[]{nozzle,carrier,rotor}.Select(body=>new PhysicsObject(body,Shape,new(0,0,0))).ToArray(),
            [hinge],new(default,maximumStep:.01));
        var source=new MechanicalTransferSource(new(0),new PointPowerPort(nozzle.Id,carrier.Id,default,Z),new(9,108));
        var field=new AirJetGeometry(nozzle.Id,carrier.Id,default,Z,default,4,1,[rotor.Id]);
        var declaration=new RotaryCaptureDeclaration(hinge.Id,new(.4,2.0/3,12,.05),
            [new(new(0),source,field,new(.75,9),1e-10)],1e-7);
        world.ReplaceLoads(new(){Rotary=[declaration]});var initialEnergy=nozzle.KineticEnergy+rotor.KineticEnergy;
        var before=world.Capture();
        for(var index=0;index<4;index++)world.Step([],[],.01);
        Assert.True(nozzle.LinearVelocity.Z<12);Assert.True(rotor.AngularVelocity.Z>0);
        var loss=initialEnergy-nozzle.KineticEnergy-rotor.KineticEnergy;
        var report=Assert.Single(world.TransferTotals.ToArray());
        Assert.True(report.SourceExtraction.Supplied>0);
        Assert.InRange(report.PairedWork.Dissipated,0,loss+1e-8);
        var after=world.Capture();world.Restore(before);
        for(var index=0;index<4;index++)world.Step([],[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }
}
