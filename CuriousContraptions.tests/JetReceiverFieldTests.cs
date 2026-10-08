using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JetReceiverFieldTests
{
    private static readonly PhysicsBodyId Source=new(0),Target=new(1),Blocker=new(2);
    private static PhysicsBody Body(PhysicsBodyId id,RigidPose pose)=>new(id,
        id==Target?PhysicsMotionType.Dynamic:PhysicsMotionType.Static,pose,default,default,
        id==Target?1:0,id==Target?new(1,1,1):default);
    private static Dictionary<PhysicsBodyId,PhysicsBody> Bodies(RigidPose source,RigidPose target,RigidPose blocker)=>
        new(){{Source,Body(Source,source)},{Target,Body(Target,target)},{Blocker,Body(Blocker,blocker)}};
    private static Dictionary<PhysicsBodyId,PhysicsColliderUpdate> Colliders(CollisionParticipation blocker)=>
        new[]{Source,Target,Blocker}.ToDictionary(id=>id,id=>new PhysicsColliderUpdate(id,
            new([new(new ConvexSphere(.2),AffineTransform.Identity)]),new(0,0,0),
            id==Blocker?blocker:CollisionParticipation.Enabled));
    private static MechanicalTransferLoad Load(CollisionVector point=default,double force=6)=>
        Load(new AirJetGeometry(Source,Target,default,new(1,0,0),point,5,1,[]),force);
    private static MechanicalTransferLoad Load(AirJetGeometry field,double force=6)=>
        new NozzleCoupledJetReceiver().CreateLoad(new(0),
            new(new StoredFlowSource(new(0),Source,12,new(6,72))),field,new(6,force),1e-8);
    private static BodyWrench Evaluate(MechanicalTransferLoad load,
        Dictionary<PhysicsBodyId,PhysicsBody> bodies,Dictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        var paths=bodies.ToDictionary(pair=>pair.Key,pair=>pair.Value.CreateTrajectory(.01,default));
        var stores=new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>{{Source,new(Source,100,100,100,0,0)}};
        var report=Assert.Single(MechanicalTransferSource.EvaluateAll([load],bodies,[],bodies,paths,.01,
            stores,colliders,new Dictionary<MechanicalSourceId,double>()));
        var term=report.Gradient.Terms.ToArray().Single(value=>value.Body.Id==Target);
        return new(term.Linear*report.Response.Force,term.Angular*report.Response.Force);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.7)]
    [InlineData(-1.2)]
    public void SampledFrameAndApplicationPointProduceCorrectForceAndTorque(double angle)
    {
        var rotation=RigidRotation.FromRotationVector(new(0,0,angle));
        var source=new RigidPose(new(3,4,5),rotation);
        var target=new RigidPose(source.TransformPoint(new(2,0,0)),rotation);
        var bodies=Bodies(source,target,RigidPose.At(new(0,20,0)));
        var result=Evaluate(Load(new CollisionVector(0,.5,0)),bodies,Colliders(CollisionParticipation.Enabled));
        var force=rotation.Apply(new(6,0,0));var torque=rotation.Apply(new(0,0,-3));
        Assert.InRange((result.Force-force).Length,0,1e-12);
        Assert.InRange((result.Torque-torque).Length,0,1e-12);
    }

    [Theory]
    [InlineData(0,0)]
    [InlineData(-1,0)]
    [InlineData(5,0)]
    [InlineData(6,0)]
    [InlineData(2,1)]
    [InlineData(2,2)]
    public void OutsideAndBoundarySamplesReceiveNoForce(double x,double y)
    {
        var bodies=Bodies(RigidPose.Identity,RigidPose.At(new(x,y,0)),RigidPose.At(new(0,20,0)));
        Assert.Equal(default,Evaluate(Load(),bodies,Colliders(CollisionParticipation.Enabled)));
    }

    [Fact]
    public void MovingDisabledReplacedAndExcludedBlockersUseCurrentDeclarations()
    {
        var bodies=Bodies(RigidPose.Identity,RigidPose.At(new(2,0,0)),RigidPose.At(new(1,0,0)));
        var colliders=Colliders(CollisionParticipation.Enabled);var jet=Load();
        Assert.Equal(default,Evaluate(jet,bodies,colliders));
        bodies[Blocker]=Body(Blocker,RigidPose.At(new(1,2,0)));
        Assert.Equal(new CollisionVector(6,0,0),Evaluate(jet,bodies,colliders).Force);
        bodies[Blocker]=Body(Blocker,RigidPose.At(new(1,0,0)));
        colliders=Colliders(CollisionParticipation.Disabled);
        Assert.Equal(new CollisionVector(6,0,0),Evaluate(jet,bodies,colliders).Force);
        colliders=Colliders(CollisionParticipation.Enabled);
        colliders[Blocker]=new(Blocker,new([new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,2,0)))]),
            new(0,0,0),CollisionParticipation.Enabled);
        Assert.Equal(new CollisionVector(6,0,0),Evaluate(jet,bodies,colliders).Force);
        colliders=Colliders(CollisionParticipation.Enabled);
        var excluded=new List<PhysicsBodyId>{Blocker};
        var assemblyJet=Load(new AirJetGeometry(Source,Target,default,new(1,0,0),default,5,1,excluded));
        excluded.Clear();
        Assert.Equal(new CollisionVector(6,0,0),Evaluate(assemblyJet,bodies,colliders).Force);
        foreach(var id in new[]{Source,Target})
        {
            var disabled=Colliders(CollisionParticipation.Disabled);var declaration=disabled[id];
            disabled[id]=new(id,declaration.Geometry,declaration.Material,CollisionParticipation.Disabled);
            Assert.Equal(default,Evaluate(jet,bodies,disabled));
        }
        Assert.Equal(default,Evaluate(Load(force:0),bodies,colliders));
    }

    [Fact]
    public void InvalidGeometryAndOwnershipReject()
    {
        foreach(var value in new[]{0d,-1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentException>(()=>Load(new AirJetGeometry(Source,Target,default,new(1,0,0),default,value,1,[])));
            Assert.Throws<ArgumentException>(()=>Load(new AirJetGeometry(Source,Target,default,new(1,0,0),default,5,value,[])));
        }
        Assert.Throws<ArgumentException>(()=>Load(new AirJetGeometry(Source,Target,default,default,default,5,1,[])));
        Assert.Throws<ArgumentException>(()=>Load(new AirJetGeometry(Source,Source,default,new(1,0,0),default,5,1,[])));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Load(force:-1));
        var bodies=Bodies(RigidPose.Identity,RigidPose.At(new(2,0,0)),RigidPose.At(new(1,0,0)));
        var colliders=Colliders(CollisionParticipation.Enabled);
        colliders.Remove(Blocker);
        Assert.Throws<ArgumentException>(()=>Evaluate(Load(),bodies,colliders));
        colliders=Colliders(CollisionParticipation.Enabled);colliders[Blocker]=default;
        Assert.Throws<ArgumentException>(()=>Evaluate(Load(),bodies,colliders));
        bodies.Remove(Target);
        Assert.Throws<ArgumentException>(()=>Evaluate(Load(),bodies,Colliders(CollisionParticipation.Enabled)));
    }

    [Theory]
    [InlineData(PhysicsMotionType.Static,0)]
    [InlineData(PhysicsMotionType.Static,.7)]
    [InlineData(PhysicsMotionType.Kinematic,0)]
    [InlineData(PhysicsMotionType.Kinematic,.7)]
    [InlineData(PhysicsMotionType.Dynamic,0)]
    [InlineData(PhysicsMotionType.Dynamic,.7)]
    public void PrescribedAndDynamicReceiversShareOneNonMutatingFieldQuery(PhysicsMotionType motion,double angle)
    {
        var rotation=RigidRotation.FromRotationVector(new(0,0,angle));
        var source=new RigidPose(new(3,4,5),rotation);
        var target=new RigidPose(source.TransformPoint(new(2,0,0)),rotation);
        var bodies=Bodies(source,target,RigidPose.At(new(0,20,0)));
        bodies[Target]=new(Target,motion,target,default,default,
            motion==PhysicsMotionType.Dynamic?1:0,motion==PhysicsMotionType.Dynamic?new(1,1,1):default);
        var colliders=Colliders(CollisionParticipation.Enabled);
        var field=new AirJetGeometry(Source,Target,default,new(1,0,0),new(0,.5,0),5,1,[]);
        var load=Load(field);
        var before=bodies.Values.Select(body=>body.Snapshot()).ToArray();
        var signal=Evaluate(load,bodies,colliders);
        Assert.InRange((signal.Force-rotation.Apply(new(6,0,0))).Length,0,1e-12);
        Assert.InRange((signal.Torque-rotation.Apply(new(0,0,-3))).Length,0,1e-12);
        Assert.Same(field,load.Field);
        Assert.Equal(signal,Evaluate(load,bodies,colliders));
        Assert.Equal(before,bodies.Values.Select(body=>body.Snapshot()).ToArray());
        var declaration=colliders[Target];
        colliders[Target]=new(Target,declaration.Geometry,declaration.Material,CollisionParticipation.Disabled);
        Assert.Equal(default,Evaluate(load,bodies,colliders));
        Assert.Equal(before,bodies.Values.Select(body=>body.Snapshot()).ToArray());
    }

    [Fact]
    public void ReceiverRejectsAbsentParticipantsEvenWithNoForce()
    {
        Assert.Throws<ArgumentNullException>(()=>Load((AirJetGeometry)null!));
        var load=Load(force:0);
        var bodies=Bodies(RigidPose.Identity,RigidPose.At(new(2,0,0)),RigidPose.At(new(0,20,0)));
        var colliders=Colliders(CollisionParticipation.Enabled);
        bodies.Remove(Source);colliders.Remove(Source);
        Assert.Throws<ArgumentException>(()=>Evaluate(load,bodies,colliders));
    }
}
