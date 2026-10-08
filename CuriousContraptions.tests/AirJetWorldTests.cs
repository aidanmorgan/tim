using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AirJetWorldTests
{
    private static PhysicsObject Object(PhysicsBody body,double radius)=>new(body,
        new([new(new ConvexSphere(radius),AffineTransform.Identity)]),new(0,0,0));

    // These analytical constant-force fixtures stay in the saturated material
    // branch (receiver speed below11); all work is still paid by finite flow.
    private static MechanicalTransferLoad Jet(PhysicsBodyId source,PhysicsBodyId receiver,double reach)=>
        new NozzleCoupledJetReceiver().CreateLoad(new(0),
            new(new StoredFlowSource(new(0),source,12,new(6,72))),
            new(source,receiver,default,new(1,0,0),default,reach,1,[]),new(6,6),1e-8);

    [Theory]
    [InlineData(1,false)]
    [InlineData(4,false)]
    [InlineData(1,true)]
    [InlineData(4,true)]
    public void WorldJetUsesCurrentColliderParticipationAndExactReplay(double mass,bool blocked)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),
            default,default,mass,new(1,1,1));
        var blocker=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(1,0,0)),default,default);
        var world=new PhysicsWorld([],[Object(source,.1),Object(body,.1),Object(blocker,.1)],[],new(default,maximumStep:.01));
        world.InstallEnergyStores([new(source.Id,100,100)]);
        var collider=world.Collider(blocker.Id).Declaration;
        world.ApplyColliderUpdates([new(blocker.Id,collider.Geometry,collider.Material,
            blocked?CollisionParticipation.Enabled:CollisionParticipation.Disabled)]);
        var jet=Jet(source.Id,body.Id,10);
        world.ReplaceLoads(world.Loads with {Transfers=[jet]});var initial=world.Capture();
        void Run()
        {
            world.Step([],[],.1);
            var acceleration=blocked?0:6/mass;
            Assert.InRange(Math.Abs(body.LinearVelocity.X-acceleration*.1),0,1e-10);
            Assert.InRange(Math.Abs(body.Center.X-2-.5*acceleration*.1*.1),0,1e-10);
            Assert.Equal(0,body.AngularMomentum.Length);
            Assert.Equal(!blocked,world.EnergyStore(source.Id).ReleasedEnergy>0);
            Assert.True(body.KineticEnergy<=world.EnergyStore(source.Id).ReleasedEnergy+1e-8);
        }
        Run();var final=world.Capture();
        world.ReplaceLoads(world.Loads with {Transfers=[]});Assert.Empty(world.Loads.Transfers.ToArray());
        world.Restore(initial);Assert.Same(jet,Assert.Single(world.Loads.Transfers.ToArray()));
        Run();Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        world.Restore(initial);
        var movedGeometry=new CompoundGeometry([new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,2,0)))]);
        world.ApplyColliderUpdates([new(blocker.Id,movedGeometry,collider.Material,CollisionParticipation.Enabled)]);
        world.Step([],[],.1);
        Assert.InRange(Math.Abs(body.LinearVelocity.X-.6/mass),0,1e-10);
        var replaced=world.Capture().BodyStates.ToArray();
        world.Restore(initial);
        world.ApplyColliderUpdates([new(blocker.Id,movedGeometry,collider.Material,CollisionParticipation.Enabled)]);
        world.Step([],[],.1);Assert.Equal(replaced,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Transfers=[
            Jet(source.Id,new(99),10)]}));
        Assert.Same(jet,Assert.Single(world.Loads.Transfers.ToArray()));
    }

    [Theory]
    [InlineData(.2)]
    [InlineData(.01)]
    public void CollisionShortenedJetMotionMatchesPiecewiseWorkAndReplays(double maximumStep)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),
            new(10,0,0),default,1,new(1,1,1));
        var wall=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(3,0,0)),default,default);
        PhysicsObject Elastic(PhysicsBody value,double radius)=>new(value,
            new([new(new ConvexSphere(radius),AffineTransform.Identity)]),new(1,0,0));
        var world=new PhysicsWorld([],[Elastic(source,.1),Elastic(body,.1),Elastic(wall,.2)],[],new(default,maximumStep:maximumStep));
        world.InstallEnergyStores([new(source.Id,100,100)]);
        world.ReplaceLoads(world.Loads with {Transfers=[Jet(source.Id,body.Id,10)]});
        var initial=world.Capture();
        // The world declares first contact at its positive skin, not at
        // zero geometric separation. Keep the analytical event at that boundary.
        const double duration=.2,acceleration=6,distance=.7-ConvexSweep.ContactDistance;
        var incoming=Math.Sqrt(100+2*acceleration*distance);
        var arrival=2*distance/(10+incoming);var remaining=duration-arrival;
        var expectedVelocity=-incoming+acceleration*remaining;
        var expectedPosition=2+distance-incoming*remaining+.5*acceleration*remaining*remaining;
        world.Step([],[],duration);
        Assert.NotEmpty(world.Impacts.ToArray());
        Assert.InRange(Math.Abs(body.LinearVelocity.X-expectedVelocity),0,1e-6);
        Assert.InRange(Math.Abs(body.Center.X-expectedPosition),0,1e-6);
        Assert.InRange(Math.Abs(body.KineticEnergy-50-acceleration*(body.Center.X-2)),0,1e-6);
        var work=Assert.Single(world.TransferUse.ToArray()).ReceiverDelivery;
        Assert.InRange(Math.Abs(body.KineticEnergy-50-work.Supplied+work.Dissipated),0,1e-6);
        var final=world.Capture();var impacts=world.Impacts.ToArray();
        world.Restore(initial);world.Step([],[],duration);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Equal(impacts,world.Impacts.ToArray());
    }

    public enum FieldCrossing { DownstreamExit, TransversePass }
    [Theory]
    [InlineData(FieldCrossing.DownstreamExit,.2)]
    [InlineData(FieldCrossing.DownstreamExit,.01)]
    [InlineData(FieldCrossing.TransversePass,.2)]
    [InlineData(FieldCrossing.TransversePass,.01)]
    public void JetBoundaryWorkMatchesActualTimeInsideTheField(FieldCrossing crossing,double maximumStep)
    {
        if(!Enum.IsDefined(crossing))throw new ArgumentOutOfRangeException(nameof(crossing));
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var start=crossing==FieldCrossing.DownstreamExit?new CollisionVector(2.8,0,0):new(2,-2,0);
        var velocity=crossing==FieldCrossing.DownstreamExit?new CollisionVector(2,0,0):new(0,20,0);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(start),velocity,default,1,new(1,1,1));
        var world=new PhysicsWorld([],[Object(source,.1),Object(body,.1)],[],new(default,maximumStep:maximumStep));
        world.InstallEnergyStores([new(source.Id,100,100)]);
        world.ReplaceLoads(world.Loads with {Transfers=[Jet(source.Id,body.Id,3)]});
        const double duration=.2;
        double expectedSpeed,expectedPosition;
        switch(crossing)
        {
            case FieldCrossing.DownstreamExit:
                expectedSpeed=Math.Sqrt(4+2*6*.2);
                var exit=2*.2/(2+expectedSpeed);
                expectedPosition=3+expectedSpeed*(duration-exit);
                break;
            case FieldCrossing.TransversePass:
                // Enters y=-1 at .05s, exits y=1 at .15s; coast after exit.
                expectedSpeed=.6;expectedPosition=2+.5*6*.1*.1+.6*.05;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(crossing));
        }
        var initial=world.Capture();
        world.Step([],[],duration);
        Assert.InRange(Math.Abs(body.LinearVelocity.X-expectedSpeed),0,1e-6);
        Assert.InRange(Math.Abs(body.Center.X-expectedPosition),0,1e-6);
        Assert.True(world.EnergyStore(source.Id).ReleasedEnergy>0);
        var final=world.Capture();
        world.Restore(initial);world.Step([],[],duration);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Theory]
    [InlineData(FanParameter.Powered,"powered")]
    [InlineData(FanParameter.Force,"force")]
    [InlineData(FanParameter.Reach,"reach")]
    [InlineData(FanParameter.Width,"width")]
    public void FanParameterSerializationBoundaryIsCanonical(FanParameter parameter,string serialized)
    {
        Assert.Equal(serialized,PartParameterName.Of(parameter));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((FanParameter)999));
    }
}
