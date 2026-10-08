using CuriousContraptions.Physics;
using Godot;

namespace CuriousContraptions.Tests;

public class RotaryCaptureForcePathTests
{
    public enum SupplyCase { Front, Rear, Opposed, Blocked, Empty, Stopped }
    private static readonly CollisionVector Z=new(0,0,1);
    private sealed record Fixture(PhysicsWorld World,Dictionary<PhysicsBodyId,PhysicsBody> Bodies,
        PhysicsJoint[] Joints,Dictionary<PhysicsBodyId,PhysicsColliderUpdate> Colliders,
        Dictionary<PhysicsBodyId,PhysicsEnergyStoreState> Stores,Dictionary<PhysicsBodyId,BodyTrajectory> Paths,
        RotaryCaptureDeclaration Declaration,PhysicsBody Rotor)
    {
        internal RotaryCaptureForcePath Capture()=>new(Declaration,Bodies,Joints,Colliders,Stores,Paths,1);
    }
    private static Fixture Setup(SupplyCase supply,double flow=12,double response=2.0/3,double maximumSpeed=3,double otherFlow=12,
        CollisionVector? rotorSpin=null,CollisionVector blockerVelocity=default)
    {
        if(!Enum.IsDefined(supply))throw new ArgumentOutOfRangeException(nameof(supply));
        var sign=supply==SupplyCase.Rear?-1:1;
        var nozzle=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var carrier=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(Z*(sign*2)),default,default);
        var rotor=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,carrier.Pose,default,rotorSpin??new(2*Math.PI,0,0),1,new(1,1,1));
        var other=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(Z*4),default,default);
        var blocker=new PhysicsBody(new(4),PhysicsMotionType.Kinematic,
            RigidPose.At(supply==SupplyCase.Blocked?Z:new CollisionVector(5,0,0)),blockerVelocity,default);
        var bodies=new[]{nozzle,carrier,rotor,other,blocker}.ToDictionary(body=>body.Id);
        var frame=new JointFrame(default,RigidRotation.Identity);
        PhysicsJoint[] joints=[new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,rotor,frame,carrier,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both)];
        var shape=new CompoundGeometry([new(new ConvexSphere(.01),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],bodies.Values.Select(body=>new PhysicsObject(body,shape,new(0,0,0))).ToArray(),
            joints,new(default,maximumStep:.01));
        world.InstallEnergyStores([new(nozzle.Id,100,supply==SupplyCase.Empty?0:100),new(other.Id,100,100)]);
        var branches=new List<RotaryCaptureTransfer>
        {
            new(new(0),new(new StoredFlowSource(new(0),nozzle.Id,supply==SupplyCase.Stopped?0:flow,new(9,108))),
                new(nozzle.Id,carrier.Id,default,Z*sign,default,4,1,[rotor.Id]),new(.75,9),1e-10)
        };
        if(supply==SupplyCase.Opposed)
            branches.Add(new(new(1),new(new StoredFlowSource(new(1),other.Id,otherFlow,new(9,108))),
                new(other.Id,carrier.Id,default,-Z,default,4,1,[rotor.Id]),new(.75,9),1e-10));
        var declaration=new RotaryCaptureDeclaration(joints[0].Id,new(.4,response,maximumSpeed,.05),branches,1e-7);
        return new(world,bodies,joints,bodies.Keys.ToDictionary(id=>id,id=>world.Collider(id).Declaration),
            world.EnergyStores.ToArray().ToDictionary(store=>store.Owner),
            bodies.Values.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(1,default)),declaration,rotor);
    }

    [Theory]
    [InlineData(SupplyCase.Front,9)]
    [InlineData(SupplyCase.Rear,-9)]
    [InlineData(SupplyCase.Opposed,0)]
    [InlineData(SupplyCase.Blocked,0)]
    [InlineData(SupplyCase.Empty,0)]
    [InlineData(SupplyCase.Stopped,0)]
    public void SignedCapturedForcesMatchAnalyticRotationWithoutMutatingWorld(SupplyCase supply,double amplitude)
    {
        var fixture=Setup(supply);var before=fixture.World.Capture();var path=fixture.Capture();
        for(var index=0;index<=64;index++)
        {
            var time=index/64.0;
            Assert.InRange(Math.Abs(path.At(time)-amplitude*Math.Cos(2*Math.PI*time)),0,1e-10);
        }
        for(double start=0;start<1;)
        {
            var end=path.SegmentEndAfter(start);var interval=path.Evaluate(start,end);
            Assert.InRange(Math.Abs(interval.End-interval.Start),0,interval.RateBound*(end-start)+1e-10);
            start=end;
        }
        Assert.Equal(before.BodyStates.ToArray(),fixture.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.EnergyStates.ToArray(),fixture.World.Capture().EnergyStates.ToArray());
        var replay=fixture.Capture();
        Assert.Equal(path.Evaluate(0,path.SegmentEndAfter(0)),replay.Evaluate(0,replay.SegmentEndAfter(0)));
    }

    [Theory]
    [InlineData(SupplyCase.Front,RotaryCaptureControlBoundary.PositiveCutIn,.05)]
    [InlineData(SupplyCase.Rear,RotaryCaptureControlBoundary.NegativeCutIn,.05)]
    [InlineData(SupplyCase.Front,RotaryCaptureControlBoundary.PositiveTargetLimit,4.5)]
    [InlineData(SupplyCase.Rear,RotaryCaptureControlBoundary.NegativeTargetLimit,4.5)]
    public void TypedThresholdSweepFindsHiddenCrossing(SupplyCase supply,RotaryCaptureControlBoundary boundary,double force)
    {
        var path=Setup(supply).Capture();
        Assert.InRange(Math.Abs(path.At(1)-path.At(0)),0,1e-10);
        var result=path.Boundary(boundary,1,1e-7,0);
        var expected=Math.Acos(force/9)/(2*Math.PI);
        Assert.Equal(ScalarSweepStatus.Boundary,result.Status);
        Assert.InRange(result.Time,expected-1e-7,expected);
        Assert.Equal(result,path.Boundary(boundary,1,1e-7,0));
    }

    [Fact]
    public void OpposingOrderAndInvalidCaptureBoundariesAreExplicit()
    {
        var fixture=Setup(SupplyCase.Opposed);var path=fixture.Capture();
        var reordered=new RotaryCaptureDeclaration(fixture.Declaration.Joint,fixture.Declaration.Material,
            fixture.Declaration.Branches.Reverse(),fixture.Declaration.ForceTolerance);
        var other=(fixture with {Declaration=reordered}).Capture();
        Assert.Equal(path.Evaluate(0,path.SegmentEndAfter(0)),other.Evaluate(0,other.SegmentEndAfter(0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.Boundary((RotaryCaptureControlBoundary)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.Evaluate(.5,.4));
        fixture.Paths.Remove(fixture.Rotor.Id);
        Assert.Throws<ArgumentException>(()=>fixture.Capture());
    }

    [Theory]
    [InlineData(SupplyCase.Front,1)]
    [InlineData(SupplyCase.Rear,-1)]
    public void TargetSlipDetectsDepartureFromZeroForcePlateau(SupplyCase supply,double sign)
    {
        var fixture=Setup(supply,1,4,12);var path=fixture.Capture();
        Assert.Equal(0,path.AtMaximumPitch(0));
        var id=Assert.Single(path.ActiveBranches);
        var result=path.MaximumPitchSlipBoundary(id,.2,1e-8,0);
        var expected=Math.Acos(Math.Sqrt(1/1.2))/(2*Math.PI);
        Assert.Equal(ScalarSweepStatus.Boundary,result.Status);
        Assert.InRange(result.Time,expected-1e-7,expected);
        var material=fixture.Declaration.Material;
        RotaryCaptureSetting Setting(double time)=>material.Calibrate([
            new(id,new(.75,9),1,0,sign*Math.Cos(2*Math.PI*time))]);
        Assert.True(Setting(0).Pitch<material.MaximumPitch);
        Assert.Equal(material.MaximumPitch,Setting(.125).Pitch);
        for(var index=0;index<=64;index++)
        {
            var time=index/64.0;var alignment=sign*Math.Cos(2*Math.PI*time);
            var force=alignment*.75*Math.Max(0,1-1.2*alignment*alignment);
            Assert.InRange(Math.Abs(force-path.AtMaximumPitch(time)),0,1e-10);
        }
        for(double start=0;start<1;)
        {
            var end=path.SegmentEndAfter(start);var interval=path.EvaluateMaximumPitch(start,end);
            Assert.InRange(Math.Abs(interval.End-interval.Start),0,interval.RateBound*(end-start)+1e-10);
            start=end;
        }
        Assert.Throws<ArgumentException>(()=>path.MaximumPitchSlipBoundary(new(99)));
    }

    [Fact]
    public void OpposingTargetForceDetectsRatioTransitionBeforeEitherBranchStops()
    {
        var fixture=Setup(SupplyCase.Opposed,12,4,100,2);var path=fixture.Capture();
        Assert.True(path.AtMaximumPitch(0)<0);
        var result=path.Boundary(RotaryCaptureControlBoundary.MaximumPitchForce,.2,1e-7,0);
        var expected=Math.Acos(Math.Sqrt(5.0/12))/(2*Math.PI);
        Assert.Equal(ScalarSweepStatus.Boundary,result.Status);
        Assert.InRange(result.Time,expected-1e-7,expected);
        Assert.True(path.AtMaximumPitch(expected+.001)>0);
        Assert.Equal(result,path.Boundary(RotaryCaptureControlBoundary.MaximumPitchForce,.2,1e-7,0));
    }

    [Theory]
    [InlineData(-100,-12)]
    [InlineData(-.01,-.04)]
    [InlineData(0,0)]
    [InlineData(.01,.04)]
    [InlineData(100,12)]
    public void RequestedSpeedIsContinuousBeforeSeparateCutIn(double force,double expected)
    {
        var material=new RotaryCaptureMaterial(.4,4,12,.05);
        Assert.Equal(expected,material.RequestedSpeed(force));
        Assert.Throws<ArgumentOutOfRangeException>(()=>material.RequestedSpeed(double.NaN));
    }

    [Fact]
    public void OverlappingControlBandsAreRejected()
    {
        var fixture=Setup(SupplyCase.Front);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureDeclaration(fixture.Declaration.Joint,
            fixture.Declaration.Material,fixture.Declaration.Branches,1));
    }
}
