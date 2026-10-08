using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompressionTransferSupplyTests
{
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void PlateStartingAtRestSuppliesTheSamePredictionIntervalAndReplays(double appliedForce)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var plate=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(0,2,0)),default,default,1,new(1,1,1));
        var target=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),default,default,1,new(1,1,1));
        var world=new PhysicsWorld([],[Object(source),Object(plate),Object(target)],[],new(default,maximumStep:.1));
        MechanicalTransferLoad Load(PhysicsBodyId pump,PhysicsBodyId participation)=>
            new NozzleCoupledJetReceiver().CreateLoad(new(0),
                new(new(0),new PointPowerPort(pump,source.Id,default,new(0,-1,0)),new(6,6))
                    {ParticipationBody=participation},
                new(source.Id,target.Id,default,new(1,0,0),default,10,1,[plate.Id]),new(6,6),1e-8);
        var load=Load(plate.Id,plate.Id);
        world.ReplaceLoads(world.Loads with {Transfers=[load]});var initial=world.Capture();
        PhysicsWrenchCommand[] force=[new(plate.Id,new(0,appliedForce,0),default)];
        world.Step(force,[],.1);
        // Coupled midpoint slip = .05 - .1*f, so f=6*slip=.1875.
        // The plate pays the reaction in this same interval.
        var expected=appliedForce<0?.01875:0;
        Assert.InRange(Math.Abs(target.LinearVelocity.X-expected),0,1e-10);
        Assert.InRange(Math.Abs(plate.LinearVelocity.Y-appliedForce*.1-expected),0,1e-10);
        var externalWork=appliedForce*(plate.Center.Y-initial.BodyStates.ToArray().Single(body=>body.Id==plate.Id).Pose.Center.Y);
        Assert.True(plate.KineticEnergy+target.KineticEnergy<=externalWork+1e-8);
        var final=world.Capture();
        world.Restore(initial);world.Step(force,[],.1);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        foreach(var disabled in new[]{plate.Id,source.Id})
        {
            world.Restore(initial);
            var collider=world.Collider(disabled).Declaration;
            world.ApplyColliderUpdates([new(disabled,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            world.Step(force,[],.1);
            Assert.Equal(0,target.LinearVelocity.X);
            Assert.InRange(Math.Abs(plate.LinearVelocity.Y-appliedForce*.1),0,1e-10);
        }
        world.Restore(initial);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Transfers=[Load(new(99),plate.Id)]}));
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Transfers=[Load(plate.Id,new(99))]}));
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Transfers=[Load(plate.Id,source.Id)]}));
        Assert.Same(load,Assert.Single(world.Loads.Transfers));

    }

    [Theory]
    [InlineData(-2,false)]
    [InlineData(-.4,false)]
    [InlineData(0,false)]
    [InlineData(.4,false)]
    [InlineData(2,false)]
    [InlineData(-2,true)]
    [InlineData(-.4,true)]
    [InlineData(0,true)]
    [InlineData(.4,true)]
    [InlineData(2,true)]
    public void RelativeCompressionRejectsCarrierMotionAndRefill(double speed,bool rotated)
    {
        var rotation=rotated?RigidRotation.FromRotationVector(new(.4,.7,-.3)):RigidRotation.Identity;
        var source=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,new(new(3,4,5),rotation),
            new(2,-3,1),new(.3,.5,.7));
        var center=source.Pose.TransformPoint(new(1,2,3));
        var plate=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,new(center,rotation),
            source.PointVelocity(center)+rotation.Apply(new(0,speed,0)),default,1,new(1,1,1));
        var bodies=new[]{source,plate}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(pair=>pair.Key,pair=>new PhysicsColliderUpdate(pair.Key,
            Object(pair.Value).Geometry,new(0,0,0),CollisionParticipation.Enabled));
        var port=new PointPowerPort(plate.Id,source.Id,default,new(0,-2,0));
        var supply=new MechanicalTransferSource(new(0),port,new(6,72)){ParticipationBody=plate.Id};
        var before=bodies.Values.Select(body=>body.Snapshot()).ToArray();
        var compression=port.Bind(bodies,[]).Speed;
        Assert.InRange(Math.Abs(compression+speed),0,1e-12);
        Assert.True(supply.Participates(bodies,colliders));
        var response=new JetTransferImpedance(6/.8,6).Evaluate(Math.Max(0,compression),0);
        Assert.InRange(Math.Abs(response.Force-6*Math.Clamp(-speed/.8,0,1)),0,1e-12);
        Assert.Equal(before,bodies.Values.Select(body=>body.Snapshot()).ToArray());
        Assert.Throws<ArgumentException>(()=>new PointPowerPort(plate.Id,plate.Id,default,new(0,-1,0)));
    }

    [Fact]
    public void InvalidSupplyMaterialAndWorkLimitsAreRejected()
    {
        var port=new PointPowerPort(new(1),new(0),default,new(0,-1,0));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,-1d})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalSourceRating(value,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalSourceRating(1,value));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new JetTransferImpedance(value,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new JetTransferImpedance(1,value));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new StoredFlowSource(new(0),new(0),value,new(1,1)));
        }
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,-1d,0})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferLoad(new(0),
                new(new(0),port,new(1,1)),port,new(1,1),value));
        Assert.Throws<ArgumentException>(()=>new PointPowerPort(new(1),new(0),default,default));
        Assert.Throws<ArgumentNullException>(()=>new MechanicalTransferSource(new(0),null!,new(1,1)));
        Assert.Throws<ArgumentNullException>(()=>new MechanicalTransferLoad(new(0),
            new(new(0),port,new(1,1)),port,null!,1e-8));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidColliderDeclarationsRejectAtTheSupplyBoundary(bool sourceInvalid)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var plate=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(0,2,0)),new(0,-1,0),default,1,new(1,1,1));
        var bodies=new[]{source,plate}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(pair=>pair.Key,pair=>new PhysicsColliderUpdate(pair.Key,
            Object(pair.Value).Geometry,new(0,0,0),CollisionParticipation.Enabled));
        var id=sourceInvalid?source.Id:plate.Id;
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsColliderUpdate(id,colliders[id].Geometry,
            new(0,0,0),(CollisionParticipation)99));
        colliders[id]=default;
        var supply=new MechanicalTransferSource(new(0),
            new PointPowerPort(plate.Id,source.Id,default,new(0,-1,0)),new(6,72)){ParticipationBody=plate.Id};
        var load=new NozzleCoupledJetReceiver().CreateLoad(new(0),supply,
            new(source.Id,plate.Id,default,new(1,0,0),default,5,1,[]),new(6/.8,6),1e-8);
        Assert.Throws<ArgumentException>(()=>load.Validate(bodies,[],colliders));
        if(sourceInvalid)
        {
            var stored=new MechanicalTransferSource(new StoredFlowSource(new(0),source.Id,12,new(6,72)))
                {ParticipationBody=source.Id};
            Assert.Throws<ArgumentException>(()=>stored.Participates(bodies,colliders));
        }
    }

}
