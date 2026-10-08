using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class StoredFlowBoundaryTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    [Theory]
    [InlineData(MechanicalTransferBoundary.Slip,0,4,.75)]
    [InlineData(MechanicalTransferBoundary.Saturation,0,4,.25)]
    [InlineData(MechanicalTransferBoundary.Slip,4,-4,.25)]
    [InlineData(MechanicalTransferBoundary.Saturation,4,-4,.75)]
    public void AlgebraicSourceUsesSharedBoundarySweepAgainstAcceleratingReceiver(
        MechanicalTransferBoundary boundary,double velocity,double acceleration,double expected)
    {
        var source=new StoredFlowSource(new(0),new(0),3,new(100,100));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*velocity,default,1,new(1,1,1));
        var bodies=new[]{frame,receiver}.ToDictionary(body=>body.Id);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{
            [frame.Id]=frame.CreateTrajectory(1,default),
            [receiver.Id]=receiver.CreateTrajectory(1,new(Z*acceleration,default))};
        var receiving=MechanicalPortSpeedPath.Capture(new PointPowerPort(receiver.Id,frame.Id,default,Z),bodies,[],paths);
        var path=new MechanicalTransferBoundaryPath(source.CaptureSpeed(1),receiving,boundary,new(2,4),0);
        var hit=ScalarBoundarySweep.Cast(path,1,1e-8,4e-8);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,expected,expected+2e-8);
        Assert.Equal(3,source.Speed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void ConstantSourceHasExactZeroDerivativeAndNoSourceFlowEvent(double speed)
    {
        var source=new StoredFlowSource(new(0),new(0),speed,new(1,1));
        var path=source.CaptureSpeed(1);
        Assert.Equal(speed,path.At(0));Assert.Equal(speed,path.At(1));
        Assert.Equal(new ScalarBoundaryInterval(new(speed,0),new(speed,0),0,0),path.Evaluate(0,1));
        var boundary=new MechanicalTransferBoundaryPath(path,path,MechanicalTransferBoundary.SourceFlow,new(1,1),0);
        Assert.Equal(ScalarSweepStatus.Clear,ScalarBoundarySweep.Cast(boundary,1,1e-8,4e-8).Status);
        var empty=source.CaptureSpeed(0);
        Assert.Equal(speed,empty.At(0));Assert.Equal(0,empty.SegmentEndAfter(0));
        Assert.Equal(speed,empty.Evaluate(0,0).Start.Value);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonfiniteSpeedAndDurationReject(double invalid)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConstantTransferSpeedPath(invalid,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConstantTransferSpeedPath(1,invalid));
    }

    [Fact]
    public void OutsideCapturedIntervalRejects()
    {
        var path=new ConstantTransferSpeedPath(1,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConstantTransferSpeedPath(1,-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.At(2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.SegmentEndAfter(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.Evaluate(.5,.25));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.Evaluate(0,2));
    }
}
