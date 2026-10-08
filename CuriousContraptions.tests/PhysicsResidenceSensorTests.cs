using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsResidenceSensorTests
{
    public enum Motion { Fixed, Translation, Rotation }
    private static readonly CompoundGeometry Payload=new([new(new ConvexSphere(.1),AffineTransform.Identity)]);
    private static readonly CompoundGeometry FrameGeometry=new([new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,5,0)))]);
    private static readonly CollisionBounds Region=new(new(-1,-1,-1),new(1,1,1));
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Body,PhysicsBody Frame,PhysicsResidenceSensor Sensor);
    private static Fixture Create(Motion motion=Motion.Fixed,bool comoving=true)
    {
        if(!Enum.IsDefined(motion))throw new ArgumentOutOfRangeException(nameof(motion));
        var linear=motion==Motion.Translation?new CollisionVector(.2,0,0):default;
        var spin=motion==Motion.Rotation?new CollisionVector(0,0,.2):default;
        var position=new CollisionVector(.2,0,0);
        var velocity=comoving?linear+CollisionVector.Cross(spin,position):default;
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(position),velocity,default,1,new(1,1,1));
        var frame=new PhysicsBody(new(1),motion==Motion.Fixed?PhysicsMotionType.Static:PhysicsMotionType.Kinematic,
            RigidPose.Identity,linear,spin);
        var world=new PhysicsWorld([],[new(body,Payload,new(0,0,0)),new(frame,FrameGeometry,new(0,0,0))],[],
            new(default,maximumStep:.01));
        var sensor=new PhysicsResidenceSensor(new(frame.Id,body.Id),Region,.01,.1);
        world.InstallResidenceSensors([sensor]);return new(world,body,frame,sensor);
    }
    [Theory]
    [InlineData(Motion.Fixed,true)]
    [InlineData(Motion.Translation,true)]
    [InlineData(Motion.Translation,false)]
    [InlineData(Motion.Rotation,true)]
    [InlineData(Motion.Rotation,false)]
    public void PhysicalTimeRelativeMotionAndSnapshotReplayOwnCapture(Motion motion,bool comoving)
    {
        var f=Create(motion,comoving);var initial=f.World.Capture();
        Assert.Equal(PhysicsResidencePhase.Outside,Assert.Single(f.World.ResidenceStates.ToArray()).Phase);
        f.World.Step([],[],.05);
        var midway=Assert.Single(f.World.ResidenceStates.ToArray());
        Assert.Equal(comoving?PhysicsResidencePhase.Dwelling:PhysicsResidencePhase.Outside,midway.Phase);
        Assert.InRange(midway.Elapsed,0,.05);
        var snapshot=f.World.Capture();
        f.World.Step([],[],.06);
        var state=Assert.Single(f.World.ResidenceStates.ToArray());
        Assert.Equal(comoving?PhysicsResidencePhase.Captured:PhysicsResidencePhase.Outside,state.Phase);
        var after=f.World.Capture();
        f.World.Restore(snapshot);f.World.Step([],[],.06);
        Assert.Equal(after.ResidenceStates.ToArray(),f.World.ResidenceStates.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        f.World.Restore(initial);
        Assert.Equal(initial.ResidenceStates.ToArray(),f.World.ResidenceStates.ToArray());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledParticipationBreaksDwellAndCaptureLatchesOnlyAfterNewDwell(bool frame)
    {
        var f=Create();f.World.Step([],[],.06);
        var id=frame?f.Frame.Id:f.Body.Id;var declaration=f.World.Collider(id).Declaration;
        f.World.ApplyColliderUpdates([new(id,declaration.Geometry,declaration.Material,CollisionParticipation.Disabled)]);
        f.World.Step([],[],.01);
        Assert.Equal(new(f.Sensor.Key,PhysicsResidencePhase.Outside,0),Assert.Single(f.World.ResidenceStates.ToArray()));
        f.World.ApplyColliderUpdates([declaration]);f.World.Step([],[],.06);
        Assert.Equal(PhysicsResidencePhase.Dwelling,Assert.Single(f.World.ResidenceStates.ToArray()).Phase);
        f.World.Step([],[],.05);
        Assert.Equal(PhysicsResidencePhase.Captured,Assert.Single(f.World.ResidenceStates.ToArray()).Phase);
        f.World.ApplyImpulse(f.Body.Id,new(3,0,0),f.Body.Center);f.World.Step([],[],1);
        Assert.Equal(PhysicsResidencePhase.Captured,Assert.Single(f.World.ResidenceStates.ToArray()).Phase);
    }
    [Fact]
    public void HighSpeedAndLeavingTheRegionResetPartialResidence()
    {
        var f=Create();f.World.Step([],[],.06);
        f.World.ApplyImpulse(f.Body.Id,new(3,0,0),f.Body.Center);f.World.Step([],[],.5);
        Assert.True(f.Body.Center.X>1);
        Assert.Equal(PhysicsResidencePhase.Outside,Assert.Single(f.World.ResidenceStates.ToArray()).Phase);
        Assert.Equal(0,Assert.Single(f.World.ResidenceStates.ToArray()).Elapsed);
        f.World.ApplyImpulse(f.Body.Id,new(-3,0,0),f.Body.Center);f.World.Step([],[],.2);
        Assert.Equal(PhysicsResidencePhase.Outside,Assert.Single(f.World.ResidenceStates.ToArray()).Phase);
    }
    [Fact]
    public void BoundariesValidationAndInstallationAreExplicitAndAtomic()
    {
        var f=Create();var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>new PhysicsResidenceSensor(new(f.Body.Id,f.Body.Id),Region,1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsResidenceSensor(f.Sensor.Key,default,1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsResidenceSensor(f.Sensor.Key,Region,0,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsResidenceSensor(f.Sensor.Key,Region,1,double.NaN));
        Assert.Throws<ArgumentException>(()=>f.World.InstallResidenceSensors([f.Sensor]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallResidenceSensors([
            new(new(f.Body.Id,f.Frame.Id),Region,1,1)]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallResidenceSensors([
            new(new(new(999),f.Body.Id),Region,1,1)]));
        Assert.Throws<ArgumentNullException>(()=>f.World.InstallResidenceSensors([null!]));
        Assert.Equal(before.ResidenceStates.ToArray(),f.World.ResidenceStates.ToArray());
    }

    [Fact]
    public void FailureAfterEarlierCommittedSubstepsRestoresResidenceAndClock()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-1,0,0)),new(4,0,0),default,1,new(1,1,1));
        var a=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var b=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[new(body,Payload,new(0,0,0)),new(a,FrameGeometry,new(0,0,0)),
            new(b,FrameGeometry,new(0,0,0))],[],new(default,maximumStep:.1,maximumEvents:1));
        world.InstallPassageSensors([new(a.Id,.5,.02),new(b.Id,.5,.02)]);
        world.InstallResidenceSensors([new(new(a.Id,body.Id),new(new(-2,-1,-1),new(2,1,1)),5,1)]);
        world.Step([],[],.05);
        Assert.InRange(Assert.Single(world.ResidenceStates.ToArray()).Elapsed,.049,.051);
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.5));
        Assert.Equal(before.ResidenceStates.ToArray(),world.ResidenceStates.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }
}
