using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalTransferBoundaryTests
{
    private static readonly JetTransferImpedance Impedance=new(2,4);
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static (MechanicalPortSpeedPath Source,MechanicalPortSpeedPath Receiver) Affine(double sourceSpeed,
        double receiverSpeed,double acceleration)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),Z*sourceSpeed,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(X*4),Z*receiverSpeed,default,1,new(1,1,1));
        var bodies=new[]{frame,source,receiver}.ToDictionary(b=>b.Id);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [frame.Id]=frame.CreateTrajectory(1,default),
            [source.Id]=source.CreateTrajectory(1,new(Z*acceleration,default)),
            [receiver.Id]=receiver.CreateTrajectory(1,default)
        };
        return(MechanicalPortSpeedPath.Capture(new PointPowerPort(source.Id,frame.Id,default,Z),bodies,[],paths),
            MechanicalPortSpeedPath.Capture(new PointPowerPort(receiver.Id,frame.Id,default,Z),bodies,[],paths));
    }

    [Theory]
    [InlineData(MechanicalTransferBoundary.SourceFlow,1,.5,MechanicalBoundarySide.Nonnegative)]
    [InlineData(MechanicalTransferBoundary.SourceFlow,-1,.5,MechanicalBoundarySide.Nonpositive)]
    [InlineData(MechanicalTransferBoundary.Slip,1,.25,MechanicalBoundarySide.Nonnegative)]
    [InlineData(MechanicalTransferBoundary.Slip,-1,.25,MechanicalBoundarySide.Nonpositive)]
    public void BothSignedBoundariesFindTheirIndependentCrossing(MechanicalTransferBoundary boundary,
        int sign,double expected,MechanicalBoundarySide side)
    {
        var (source,receiver)=Affine(2*sign,sign,-4*sign);
        var path=new MechanicalTransferBoundaryPath(source,receiver,boundary,Impedance,0);
        Assert.Equal(side,path.Side);
        var hit=ScalarBoundarySweep.Cast(path,1,1e-8,4e-8);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,expected,expected+2e-8);
    }

    [Theory]
    [InlineData(4,ScalarSweepStatus.Clear)]
    [InlineData(-4,ScalarSweepStatus.Boundary)]
    public void ZeroEntryAdvancesWithinTheFixedVelocityAllowance(double acceleration,ScalarSweepStatus status)
    {
        var (source,receiver)=Affine(0,0,acceleration);
        var path=new MechanicalTransferBoundaryPath(source,receiver,MechanicalTransferBoundary.SourceFlow,Impedance,0);
        var hit=ScalarBoundarySweep.Cast(path,1,1e-8,4e-8);
        Assert.Equal(status,hit.Status);Assert.True(hit.Time>0);
        if(status==ScalarSweepStatus.Boundary)Assert.InRange(hit.Time,0,2e-8);
        else Assert.Equal(1,hit.Time);
    }

    [Fact]
    public void IdenticalPortSlipIsExactlyZeroEvenWithAcceleration()
    {
        var (source,_)=Affine(0,0,4);
        var path=new MechanicalTransferBoundaryPath(source,source,MechanicalTransferBoundary.Slip,Impedance,0);
        Assert.Equal(default,path.Evaluate(0,1));
        Assert.Equal(ScalarSweepStatus.Clear,ScalarBoundarySweep.Cast(path,1,1e-8,4e-8).Status);
    }

    [Fact]
    public void UndefinedBoundaryRejects()
    {
        var (source,receiver)=Affine(1,0,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferBoundaryPath(source,receiver,
            (MechanicalTransferBoundary)int.MaxValue,Impedance,0));
    }

    [Fact]
    public void TangentialZeroEntryMustCompleteWithoutBudgetExhaustion()
    {
        // Fixed Y translation and rotating -X offset give speed 1-cos(t).
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,Y,Z);
        var bodies=new[]{frame,source}.ToDictionary(b=>b.Id);
        var paths=bodies.ToDictionary(e=>e.Key,e=>e.Value.CreateTrajectory(1,default));
        var captured=MechanicalPortSpeedPath.Capture(new PointPowerPort(source.Id,frame.Id,-X,Y),bodies,[],paths);
        var path=new MechanicalTransferBoundaryPath(captured,captured,MechanicalTransferBoundary.SourceFlow,Impedance,0);
        Assert.Equal(0,captured.At(0));
        Assert.True(captured.At(.5)>0);
        Assert.Equal(ScalarSweepStatus.Clear,ScalarBoundarySweep.Cast(path,1,1e-8,4e-8).Status);
    }

    [Theory]
    [InlineData(4,-4,MechanicalBoundarySide.Nonnegative)]
    [InlineData(2,4,MechanicalBoundarySide.Nonpositive)]
    public void SaturationCrossingUsesTheMaterialThreshold(double speed,double acceleration,MechanicalBoundarySide side)
    {
        var (source,receiver)=Affine(speed,1,acceleration);
        var path=new MechanicalTransferBoundaryPath(source,receiver,MechanicalTransferBoundary.Saturation,Impedance,0);
        Assert.Equal(side,path.Side);
        var hit=ScalarBoundarySweep.Cast(path,1,1e-8,4e-8);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,.25,.25+2e-8);
        var before=Impedance.Evaluate(source.At(.2),receiver.At(.2));
        var after=Impedance.Evaluate(source.At(.3),receiver.At(.3));
        if(side==MechanicalBoundarySide.Nonnegative)
        {
            Assert.Equal(4,before.Force);Assert.True(after.Force<4);
        }
        else
        {
            Assert.True(before.Force<4);Assert.Equal(4,after.Force);
        }
    }

    [Fact]
    public void IdenticalPortsRemainBelowSaturation()
    {
        var (source,_)=Affine(0,0,4);
        var path=new MechanicalTransferBoundaryPath(source,source,MechanicalTransferBoundary.Saturation,Impedance,0);
        Assert.Equal(MechanicalBoundarySide.Nonpositive,path.Side);
        Assert.Equal(2,path.Evaluate(0,1).Start.Value);
        Assert.Equal(ScalarSweepStatus.Clear,ScalarBoundarySweep.Cast(path,1,1e-8,4e-8).Status);
    }

    [Fact]
    public void UnrepresentableSaturationThresholdRejectsDirectConstruction()
    {
        var (source,receiver)=Affine(1,0,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferBoundaryPath(source,receiver,
            MechanicalTransferBoundary.Saturation,new(double.Epsilon,double.MaxValue),0));
    }

    [Theory]
    [InlineData(0,4)]
    [InlineData(0,-4)]
    [InlineData(-1e-16,4)]
    [InlineData(1e-16,-4)]
    public void NearZeroDepartureUsesTheCapturedDirection(double speed,double acceleration)
    {
        var (source,receiver)=Affine(speed,0,acceleration);
        var path=new MechanicalTransferBoundaryPath(source,receiver,MechanicalTransferBoundary.SourceFlow,Impedance,1e-8);
        Assert.Equal(acceleration>0?MechanicalBoundarySide.Nonnegative:MechanicalBoundarySide.Nonpositive,path.Side);
        var result=ScalarBoundarySweep.Cast(path,1,1e-8,4e-8);
        Assert.Equal(ScalarSweepStatus.Clear,result.Status);
        Assert.Equal(1,result.Time);
    }

    private sealed class ReturningFlow : TransferSpeedPath
    {
        public override double Duration=>2;
        public override double At(double time)=>time*(1-time);
        public override double SegmentEndAfter(double time)=>Duration;
        public override ScalarBoundaryInterval Evaluate(double start,double end)=>
            new(new(At(start),1-2*start),new(At(end),1-2*end),2,2);
    }

    [Fact]
    public void DepartureOrientationStillFindsTheLaterReturn()
    {
        var source=new ReturningFlow();
        var path=new MechanicalTransferBoundaryPath(source,source,MechanicalTransferBoundary.SourceFlow,Impedance,1e-8);
        Assert.Equal(MechanicalBoundarySide.Nonnegative,path.Side);
        var result=ScalarBoundarySweep.Cast(path,2,1e-8,4e-8);
        Assert.Equal(ScalarSweepStatus.Boundary,result.Status);
        Assert.InRange(result.Time,1,1+1e-7);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidDepartureToleranceRejects(double tolerance)
    {
        var (source,receiver)=Affine(0,0,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferBoundaryPath(
            source,receiver,MechanicalTransferBoundary.SourceFlow,Impedance,tolerance));
    }
}
