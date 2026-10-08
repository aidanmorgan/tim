using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsPassageSensorTests
{
    public enum Shape { Sphere, Box, RoundedHull, Compound }
    public enum FrameMotion { Fixed, Translation, Rotation }
    private static CompoundGeometry Geometry(Shape shape)=>shape switch
    {
        Shape.Sphere=>new([new(new ConvexSphere(.1),AffineTransform.Identity)]),
        Shape.Box=>new([new(new ConvexBox(new(.1,.1,.1)),AffineTransform.Identity)]),
        Shape.RoundedHull=>new([new(new ConvexRounded(new ConvexHull([new(-.03,-.03,0),new(.03,-.03,0),new(0,.03,.03)]),.07),AffineTransform.Identity)]),
        Shape.Compound=>new([new(new ConvexSphere(.1),AffineTransform.Identity),
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(.2f,0,0)))]),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    };
    private static readonly CompoundGeometry FrameGeometry=new([new(new ConvexSphere(.01),new(AffineBasis.Identity,new(0,5,0)))]);
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Body,PhysicsBody Frame);
    private static Fixture Create(Shape shape=Shape.Sphere,FrameMotion motion=FrameMotion.Fixed,
        CollisionVector? position=null,CollisionVector? velocity=null)
    {
        if(!Enum.IsDefined(motion))throw new ArgumentOutOfRangeException(nameof(motion));
        var frame=new PhysicsBody(new(1),motion==FrameMotion.Fixed?PhysicsMotionType.Static:PhysicsMotionType.Kinematic,
            RigidPose.Identity,motion==FrameMotion.Translation?new(1,0,0):default,motion==FrameMotion.Rotation?new(0,0,1):default);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(position??new(-1,0,0)),
            velocity??new(4,0,0),default,1,new(1,1,1));
        var world=new PhysicsWorld([],[new(body,Geometry(shape),new(0,0,0)),new(frame,FrameGeometry,new(0,0,0))],[],
            new(default,maximumStep:1));
        world.InstallPassageSensors([new(frame.Id,.5,.02)]);
        return new(world,body,frame);
    }
    public static TheoryData<Shape,FrameMotion> ShapesAndFrames()
    {
        var result=new TheoryData<Shape,FrameMotion>();
        foreach(var shape in Enum.GetValues<Shape>())
        foreach(var frame in Enum.GetValues<FrameMotion>())result.Add(shape,frame);
        return result;
    }

    [Theory]
    [MemberData(nameof(ShapesAndFrames))]
    public void OwnedGeometryAndCapturedRigidPathsProduceOnePassAndExactReplay(Shape shape,FrameMotion motion)
    {
        var f=Create(shape,motion);var initial=f.World.Capture();
        var result=f.World.Step([],[],.5);
        var crossing=Assert.Single(f.World.PassageEvents.ToArray());
        Assert.Equal(PhysicsPassageEventKind.Passed,crossing.Kind);
        Assert.Equal(new PhysicsPassageKey(f.Frame.Id,f.Body.Id),crossing.Key);
        Assert.InRange(crossing.Time,motion==FrameMotion.Translation?.33333:.24999,motion==FrameMotion.Translation?.33334:.25001);
        Assert.InRange(crossing.FramePose.InverseTransformPoint(crossing.BodyPose.Center).X,0,2e-7);
        Assert.True(crossing.Extent.RadialUpperBound<.5);
        Assert.Equal(1ul,Assert.Single(f.World.PassageStates.ToArray()).PassedCount);
        Assert.Equal(new CollisionVector(4,0,0),f.Body.LinearVelocity);
        var after=f.World.Capture();
        f.World.Restore(initial);
        Assert.Equal(initial.PassageStates.ToArray(),f.World.PassageStates.ToArray());
        Assert.Empty(f.World.PassageEvents.ToArray());
        Assert.Equal(result,f.World.Step([],[],.5));
        Assert.Equal(after.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(after.PassageStates.ToArray(),f.World.PassageStates.ToArray());
        Assert.Equal(after.PassageEvents.ToArray(),f.World.PassageEvents.ToArray());
        f.World.Step([],[],.1);Assert.Empty(f.World.PassageEvents.ToArray());
    }

    [Fact]
    public void CrossingSplitInsideToleranceCorridorRetainsArmingUntilReported()
    {
        var f=Create();
        f.World.Step([],[],.2500000125);
        Assert.InRange(f.Body.Center.X,0,1e-7);
        Assert.Empty(f.World.PassageEvents.ToArray());
        var state=Assert.Single(f.World.PassageStates.ToArray());
        Assert.Equal(PhysicsPassagePhase.Armed,state.Phase);
        Assert.Equal(0ul,state.PassedCount);
        f.World.Step([],[],.001);
        Assert.Equal(PhysicsPassageEventKind.Passed,Assert.Single(f.World.PassageEvents.ToArray()).Kind);
        Assert.Equal(1ul,Assert.Single(f.World.PassageStates.ToArray()).PassedCount);
        f.World.Step([],[],.001);
        Assert.Empty(f.World.PassageEvents.ToArray());
    }

    [Fact]
    public void CurvedPathCrossingIsFoundEvenWhenBothEndpointsAreUpstream()
    {
        var f=Create(velocity:new(8,0,0));
        f.World.Step([new(f.Body.Id,new(-16,0,0),default)],[],1);
        var passage=Assert.Single(f.World.PassageEvents.ToArray());
        Assert.Equal(PhysicsPassageEventKind.Passed,passage.Kind);
        Assert.InRange(passage.Time,.14644,.14646);
        Assert.InRange(f.Body.Center.X,-1.000001,-.999999);
        Assert.Equal(PhysicsPassagePhase.Armed,Assert.Single(f.World.PassageStates.ToArray()).Phase);
    }

    [Fact]
    public void OutsideApertureDisarmsWithoutPassingAndReverseOrStationaryMotionDoesNotPublish()
    {
        var outside=Create(position:new(-1,.7,0));
        outside.World.Step([],[],.5);
        Assert.Equal(PhysicsPassageEventKind.OutsideAperture,Assert.Single(outside.World.PassageEvents.ToArray()).Kind);
        Assert.Equal(0ul,Assert.Single(outside.World.PassageStates.ToArray()).PassedCount);
        var reverse=Create(position:new(1,0,0),velocity:new(-4,0,0));
        reverse.World.Step([],[],.5);Assert.Empty(reverse.World.PassageEvents.ToArray());
        var stationary=Create(position:new(0,0,0),velocity:default(CollisionVector));
        stationary.World.Step([],[],.5);Assert.Empty(stationary.World.PassageEvents.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledParticipantCannotArmAndReenableRequiresPhysicalUpstreamClearance(bool frame)
    {
        var f=Create();var id=frame?f.Frame.Id:f.Body.Id;var declaration=f.World.Collider(id).Declaration;
        f.World.ApplyColliderUpdates([new(id,declaration.Geometry,declaration.Material,CollisionParticipation.Disabled)]);
        f.World.Step([],[],.5);Assert.Empty(f.World.PassageEvents.ToArray());
        f.World.ApplyColliderUpdates([declaration]);
        f.World.ApplyImpulse(f.Body.Id,new(-8,0,0),f.Body.Center);
        f.World.Step([],[],.5);Assert.Empty(f.World.PassageEvents.ToArray());
        f.World.ApplyImpulse(f.Body.Id,new(8,0,0),f.Body.Center);
        f.World.Step([],[],.5);
        Assert.Equal(PhysicsPassageEventKind.Passed,Assert.Single(f.World.PassageEvents.ToArray()).Kind);
        Assert.Equal(1ul,Assert.Single(f.World.PassageStates.ToArray()).PassedCount);
    }

    [Fact]
    public void GeometryRevisionInvalidatesArmingAndFullCompoundControlsClearance()
    {
        var f=Create(position:new(-.25,0,0),velocity:default(CollisionVector));
        f.World.Step([],[],.001);
        Assert.Equal(PhysicsPassagePhase.Armed,Assert.Single(f.World.PassageStates.ToArray()).Phase);
        var declaration=f.World.Collider(f.Body.Id).Declaration;
        var longer=new CompoundGeometry([new(new ConvexBox(new(.3,.1,.1)),AffineTransform.Identity)]);
        f.World.ApplyColliderUpdates([new(f.Body.Id,longer,declaration.Material,declaration.Participation)]);
        f.World.ApplyImpulse(f.Body.Id,new(1,0,0),f.Body.Center);f.World.Step([],[],.5);
        Assert.Empty(f.World.PassageEvents.ToArray());
        Assert.Equal(0ul,Assert.Single(f.World.PassageStates.ToArray()).PassedCount);
        var wide=Create();var old=wide.World.Collider(wide.Body.Id).Declaration;
        var compound=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity),
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,.6f,0)))]);
        wide.World.ApplyColliderUpdates([new(wide.Body.Id,compound,old.Material,old.Participation)]);
        wide.World.Step([],[],.5);
        Assert.Equal(PhysicsPassageEventKind.OutsideAperture,Assert.Single(wide.World.PassageEvents.ToArray()).Kind);
    }

    [Fact]
    public void DeclarationValidationAndBatchInstallationAreAtomic()
    {
        var f=Create();f.World.Step([],[],.5);var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>f.World.InstallPassageSensors([new(f.Frame.Id,.6,.03)]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallPassageSensors([new(f.Body.Id,.5,.02),new(new(99),.5,.02)]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>f.World.InstallPassageSensors([default]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsPassageSensor(f.Frame.Id,double.NaN,.02));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsPassageSensor(f.Frame.Id,.5,0));
        Assert.Throws<ArgumentNullException>(()=>f.World.InstallPassageSensors(null!));
        Assert.Equal(before.PassageStates.ToArray(),f.World.PassageStates.ToArray());
        Assert.Equal(before.PassageEvents.ToArray(),f.World.PassageEvents.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(()=>Create((Shape)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Create(motion:(FrameMotion)int.MaxValue));
    }

    [Fact]
    public void FailedStepRestoresSensorCountsHistoryEventsBodiesAndClock()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-1,0,0)),new(4,0,0),default,1,new(1,1,1));
        var a=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var b=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[new(body,Geometry(Shape.Sphere),new(0,0,0)),
            new(a,FrameGeometry,new(0,0,0)),new(b,FrameGeometry,new(0,0,0))],[],new(default,maximumStep:1,maximumEvents:1));
        world.InstallPassageSensors([new(a.Id,.5,.02),new(b.Id,.5,.02)]);
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.5));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.PassageStates.ToArray(),world.PassageStates.ToArray());
        Assert.Equal(before.PassageEvents.ToArray(),world.PassageEvents.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        world.Step([],[],.1);Assert.Empty(world.PassageEvents.ToArray());
    }
}
