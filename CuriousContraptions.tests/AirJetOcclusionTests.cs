using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AirJetOcclusionTests
{
    private static readonly CollisionVector X=new(1,0,0);
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(.1),AffineTransform.Identity)]);
    private static void Near(double expected,double actual,double tolerance=3e-7)=>
        Assert.InRange(actual,expected-tolerance,expected+tolerance);

    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    [InlineData(true,true)]
    public void BlockerTransitCutsBothSidesAndCommitsOnlyExposedWork(bool reverse,bool compound)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),default,default,1,new(1,1,1));
        var blocker=new PhysicsBody(new(2),PhysicsMotionType.Kinematic,RigidPose.At(new(1,.3,0)),new(0,-1,0),default);
        var blockerShape=compound?new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity),
            new(new ConvexSphere(.1),SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new(.5f,.1f,0))))]):Shape;
        var objects=new[]{source,receiver,blocker}.Select(body=>new PhysicsObject(body,body==blocker?blockerShape:Shape,new(0,0,0))).ToArray();
        if(reverse)Array.Reverse(objects);
        var world=new PhysicsWorld([],objects,[],new(default,maximumStep:.6));
        world.InstallEnergyStores([new(source.Id,10,0)]);world.ChargeEnergyStore(source.Id,10,1);
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),source.Id,4,new(10,100))),
            new PointPowerPort(receiver.Id,source.Id,default,X),new(1,1),1e-10)
            {Field=new(source.Id,receiver.Id,default,X,default,4,1,[])}]});
        var initial=world.Capture();var result=world.Step([],[],.6);
        Assert.True(result.Events>=2);Near(.6,world.Time);
        // The second child blocks from .3 through .5; the union stays blocked
        // while the first child exits at .4.
        var exposed=compound?.3:.4;
        Near(exposed,receiver.LinearVelocity.X);
        Near(4*exposed,world.EnergyStore(source.Id).ReleasedEnergy,1e-6);
        Near(4*exposed-receiver.KineticEnergy,Assert.Single(world.TransferUse.ToArray()).PairedWork.Dissipated,1e-6);
        var final=world.Capture();world.Restore(initial);
        Assert.Equal(result,world.Step([],[],.6));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Theory]
    [InlineData(CollisionParticipation.Enabled)]
    [InlineData(CollisionParticipation.Disabled)]
    public void ReceiverMotionChangesOcclusionAgainstStationaryBlocker(CollisionParticipation participation)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(2,.3,0)),new(0,-1,0),default);
        var blocker=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(X),default,default);
        var bodies=new[]{source,receiver,blocker}.ToDictionary(body=>body.Id);
        var colliders=bodies.Values.ToDictionary(body=>body.Id,body=>new PhysicsColliderUpdate(body.Id,Shape,new(0,0,0),
            body==blocker?participation:CollisionParticipation.Enabled));
        var paths=bodies.Values.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(.6,default));
        var geometry=new AirJetGeometry(source.Id,receiver.Id,default,X,default,4,1,[]);
        Assert.True(geometry.IsExposed(bodies,colliders));
        var hit=geometry.Sweep(bodies,colliders,paths,.6,1e-7);
        if(participation==CollisionParticipation.Enabled)
        {
            Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
            Assert.Equal(AirJetBoundary.Occlusion,hit.Boundary);Near(.2,hit.Time);
        }
        else {Assert.Equal(ScalarSweepStatus.Clear,hit.Status);Assert.Equal(.6,hit.Time);}
        paths.Remove(blocker.Id);
        if(participation==CollisionParticipation.Enabled)
            Assert.Throws<ArgumentException>(()=>geometry.Sweep(bodies,colliders,paths,.6,1e-7));
    }

    [Fact]
    public void RotatingNozzlePlaneFindsOcclusionExitWithStationaryReceiver()
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,1));
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(2,.3,0)),default,default);
        var blocker=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(1,.3,0)),default,default);
        var bodies=new[]{source,receiver,blocker}.ToDictionary(body=>body.Id);
        var colliders=bodies.Keys.ToDictionary(id=>id,id=>new PhysicsColliderUpdate(id,Shape,new(0,0,0),CollisionParticipation.Enabled));
        var paths=bodies.Values.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(.3,default));
        var geometry=new AirJetGeometry(source.Id,receiver.Id,default,X,default,4,1,[]);
        Assert.False(geometry.IsExposed(bodies,colliders));
        var hit=geometry.Sweep(bodies,colliders,paths,.3,1e-7);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.Equal(AirJetBoundary.Occlusion,hit.Boundary);Near(Math.Asin(.1),hit.Time);
        var at=bodies.Keys.ToDictionary(id=>id,id=>paths[id].SampleBody(hit.Time));
        Assert.True(geometry.IsExposed(at,colliders));
    }
}
