using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JetTransferForcePathTests
{
    private sealed class QuadraticSpeed(double constant,double linear,double quadratic,double duration=1) : TransferSpeedPath
    {
        public override double Duration=>duration;
        public override double At(double time)=>constant+linear*time+quadratic*time*time;
        public override double SegmentEndAfter(double time)=>Duration;
        public override ScalarBoundaryInterval Evaluate(double start,double end)=>
            new(new(At(start),linear+2*quadratic*start),new(At(end),linear+2*quadratic*end),
                2*Math.Abs(quadratic),2*Math.Abs(quadratic));
    }

    [Theory]
    [InlineData(0,32,-32,12,-48,48)]
    [InlineData(12,0,0,0,64,-64)]
    [InlineData(-4,8,0,-6,0,0)]
    [InlineData(12,0,0,0,0,0)]
    public void CappedForceHasCertifiedRateAcrossAllCorners(double sc,double sl,double sq,double rc,double rl,double rq)
    {
        var source=new QuadraticSpeed(sc,sl,sq);
        var receiver=new QuadraticSpeed(rc,rl,rq);
        var material=new JetTransferImpedance(.75,9);
        var path=new JetTransferForcePath(source,receiver,material);
        var interval=path.Evaluate(0,1);
        var previous=path.At(0);
        for(var index=1;index<=1000;index++)
        {
            var time=index/1000.0;var current=path.At(time);
            Assert.Equal(material.Evaluate(Math.Max(0,source.At(time)),receiver.At(time)).Force,current);
            Assert.InRange(Math.Abs(current-previous),0,interval.RateBound/1000+1e-12);
            previous=current;
        }
        Assert.Equal(path.At(0),interval.Start);Assert.Equal(path.At(1),interval.End);
    }

    [Fact]
    public void SweepFindsHiddenCutInCrossingBetweenEqualSaturatedEndpoints()
    {
        var force=new JetTransferForcePath(new ConstantTransferSpeedPath(12,1),new QuadraticSpeed(0,64,-64),new(.75,9));
        Assert.Equal(9,force.At(0));Assert.Equal(9,force.At(1));Assert.Equal(0,force.At(.5));
        var expected=(1-Math.Sqrt(1-(12-.05/.75)/16))/2;
        Assert.InRange(force.At(expected),.05-1e-7,.05+1e-7);
    }

    [Theory]
    [InlineData(MechanicalBoundarySide.Nonnegative)]
    [InlineData(MechanicalBoundarySide.Nonpositive)]
    public void ConstantForceThresholdControlsAreClear(MechanicalBoundarySide side)
    {
        var force=new JetTransferForcePath(new ConstantTransferSpeedPath(12,1),new ConstantTransferSpeedPath(0,1),new(.75,9));
        Assert.Equal(0,force.Evaluate(0,1).RateBound);
    }

    [Theory]
    [InlineData(20,2,0,9)]
    [InlineData(-4,2,2,0)]
    [InlineData(2,2,10,0)]
    public void EntireClampPlateauHasZeroForceRate(double source,double slope,double receiver,double expected)
    {
        var force=new JetTransferForcePath(new QuadraticSpeed(source,slope,0),
            new ConstantTransferSpeedPath(receiver,1),new(.75,9));
        Assert.Equal(0,force.Evaluate(0,1).RateBound);
        for(var index=0;index<=100;index++)Assert.Equal(expected,force.At(index/100.0));
    }

    [Fact]
    public void SweepRefinesPlateauBeforeFirstUnsaturatedCrossing()
    {
        var force=new JetTransferForcePath(new QuadraticSpeed(20,-20,0),
            new ConstantTransferSpeedPath(0,1),new(.75,9));
        const double threshold=8.9999995;
        var expected=(20-threshold/.75)/20;
        Assert.InRange(force.At(expected),threshold-1e-7,threshold+1e-7);
    }

    [Fact]
    public void EnclosureTouchingZeroKeepsConservativeRate()
    {
        // Clamped source is zero, but the supplied speed certificate is two.
        // Endpoint slip -1 plus half-duration excursion 1 touches zero;
        // outward rounding cannot certify a strictly enclosed plateau.
        var force=new JetTransferForcePath(new QuadraticSpeed(-4,2,0),
            new ConstantTransferSpeedPath(1,1),new(.75,9));
        Assert.InRange(force.Evaluate(0,1).RateBound,1.5,1.50000000001);
        Assert.Equal(0,force.At(0));Assert.Equal(0,force.At(.5));Assert.Equal(0,force.At(1));
    }

    [Theory]
    [InlineData(0,9)]
    [InlineData(.75,0)]
    public void DisabledMaterialHasZeroRate(double conductance,double cap)
    {
        var force=new JetTransferForcePath(new QuadraticSpeed(0,64,-64),
            new ConstantTransferSpeedPath(0,1),new(conductance,cap));
        Assert.Equal(new TransferForceInterval(0,0,0),force.Evaluate(0,1));
    }

    [Fact]
    public void UnsaturatedAndBoundaryExcursionsRetainAnalyticRate()
    {
        var linear=new JetTransferForcePath(new QuadraticSpeed(2,2,0),
            new ConstantTransferSpeedPath(0,1),new(.75,9));
        Assert.InRange(linear.Evaluate(0,1).RateBound,1.5,1.50000000001);
        Assert.Equal(1.5,linear.At(1)-linear.At(0));
        // Starts exactly on the saturation boundary, then leaves it.
        var leavesCap=new JetTransferForcePath(new QuadraticSpeed(12,-2,0),
            new ConstantTransferSpeedPath(0,1),new(.75,9));
        Assert.True(leavesCap.Evaluate(0,1).RateBound>=1.5);
        Assert.Equal(9,leavesCap.At(0));Assert.Equal(7.5,leavesCap.At(1));
        // Equal zero endpoints hide a positive interior demand.
        var interior=new JetTransferForcePath(new QuadraticSpeed(0,8,-8),
            new ConstantTransferSpeedPath(0,1),new(.75,9));
        Assert.True(interior.Evaluate(0,1).RateBound>0);
        Assert.Equal(0,interior.At(0));Assert.Equal(0,interior.At(1));Assert.Equal(1.5,interior.At(.5));
    }

    [Fact]
    public void DurationAndUnsupportedNumericsReject()
    {
        var force=new JetTransferForcePath(new ConstantTransferSpeedPath(12,2),new ConstantTransferSpeedPath(0,1),new(.75,9));
        Assert.Equal(1,force.Duration);Assert.Equal(1,force.SegmentEndAfter(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>force.At(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>force.Evaluate(.5,.4));
        Assert.Throws<ArgumentOutOfRangeException>(()=>force.Evaluate(0,2));
        Assert.Throws<ArgumentNullException>(()=>new JetTransferForcePath(null!,new ConstantTransferSpeedPath(0,1),new(1,1)));
        var overflow=new JetTransferForcePath(new QuadraticSpeed(0,double.MaxValue,0),new ConstantTransferSpeedPath(0,1),new(2,1));
        Assert.Throws<InvalidOperationException>(()=>overflow.Evaluate(0,0));
    }
}

