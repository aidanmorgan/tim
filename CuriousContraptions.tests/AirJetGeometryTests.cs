using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AirJetGeometryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExposureAndOcclusionDoNotRequireOrCreateSupply(bool blocked)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),default,default,1,new(1,1,1));
        var blocker=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(1,blocked?0:2,0)),default,default);
        var bodies=new[]{source,body,blocker}.ToDictionary(value=>value.Id);
        var colliders=bodies.Keys.ToDictionary(id=>id,id=>new PhysicsColliderUpdate(id,
            new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0),CollisionParticipation.Enabled));
        var geometry=new AirJetGeometry(source.Id,body.Id,default,new(1,0,0),default,4,1,[]);
        var before=bodies.Values.Select(value=>value.Snapshot()).ToArray();
        Assert.Equal(!blocked,geometry.IsExposed(bodies,colliders));
        var excluded=new List<PhysicsBodyId>{blocker.Id};
        var clear=new AirJetGeometry(source.Id,body.Id,default,new(1,0,0),default,4,1,excluded);
        excluded.Clear();
        Assert.True(clear.IsExposed(bodies,colliders));
        var unpowered=new NozzleCoupledJetReceiver().CreateLoad(new(0),
            new(new StoredFlowSource(new(0),source.Id,0,new(6,72))),
            geometry,new(6,6),1e-8);
        Assert.Equal(!blocked,unpowered.Field!.IsExposed(bodies,colliders));
        Assert.Equal(default,unpowered.EvaluateDemand(bodies,[],colliders).Response);
        Assert.Equal(before,bodies.Values.Select(value=>value.Snapshot()).ToArray());
    }

    [Fact]
    public void GeometryAloneFindsCapturedEntryAndRejectsMissingParticipants()
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(-1,0,0)),new(2,0,0),default,1,new(1,1,1));
        var bodies=new[]{source,body}.ToDictionary(value=>value.Id);
        var colliders=bodies.Keys.ToDictionary(id=>id,id=>new PhysicsColliderUpdate(id,
            new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0),CollisionParticipation.Enabled));
        var geometry=new AirJetGeometry(source.Id,body.Id,default,new(1,0,0),default,4,1,[]);
        var paths=bodies.Values.ToDictionary(value=>value.Id,value=>value.CreateTrajectory(1,default));
        var hit=geometry.Sweep(bodies,colliders,paths,1,1e-7);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.Equal(AirJetBoundary.Inlet,hit.Boundary);
        Assert.InRange(hit.Time,.5-1e-7,.5+1e-7);
        Assert.False(geometry.IsExposed(bodies,colliders));
        bodies.Remove(body.Id);
        Assert.Throws<ArgumentException>(()=>geometry.IsExposed(bodies,colliders));
    }
}
