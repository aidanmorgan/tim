using System;

namespace CuriousContraptions.Physics;

/// <summary>Signed transfer-coordinate speed in m/s with certified path derivatives.
/// Evaluate.Curvature must bound the absolute second derivative on each segment;
/// this stronger contract also certifies first-derivative bounds for capped laws.
/// A physical port and an algebraic source share this contract without sharing bodies.</summary>
public abstract class TransferSpeedPath : ScalarBoundaryPath
{
    public abstract double At(double time);

    public double AbsoluteRateBound(double start,double end)
    {
        if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||
            end>SegmentEndAfter(start))throw new ArgumentOutOfRangeException(nameof(end));
        var interval=Evaluate(start,end);
        if(!double.IsFinite(interval.Start.Value)||!double.IsFinite(interval.End.Value)||
            !double.IsFinite(interval.Start.Rate)||!double.IsFinite(interval.End.Rate)||
            !double.IsFinite(interval.Curvature)||interval.Curvature<0||
            !double.IsFinite(interval.ChordCurvature)||interval.ChordCurvature<0)
            throw new InvalidOperationException("Transfer speed has an invalid absolute curvature certificate.");
        // Each interior point is within half the interval of an endpoint.
        var rate=Math.Max(Math.Abs(interval.Start.Rate),Math.Abs(interval.End.Rate))+
            interval.Curvature*((end-start)*.5);
        if(rate>0)rate=Math.BitIncrement(rate*(1+1e-12));
        if(!double.IsFinite(rate))throw new InvalidOperationException("Transfer speed rate exceeds numeric range.");
        return rate;
    }
}

/// <summary>A held algebraic flow speed; no simulated mass or decorative rotor.</summary>
public sealed class ConstantTransferSpeedPath : TransferSpeedPath
{
    public double Speed { get; }
    public override double Duration { get; }
    public ConstantTransferSpeedPath(double speed,double duration)
    {
        if(!double.IsFinite(speed))throw new ArgumentOutOfRangeException(nameof(speed));
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        Speed=speed;Duration=duration;
    }
    public override double At(double time)
    {
        ValidateTime(time);return Speed;
    }
    public override double SegmentEndAfter(double time)
    {
        ValidateTime(time);return Duration;
    }
    public override ScalarBoundaryInterval Evaluate(double start,double end)
    {
        ValidateTime(start);ValidateTime(end);
        if(end<start)throw new ArgumentOutOfRangeException(nameof(end));
        return new(new(Speed,0),new(Speed,0),0,0);
    }
    private void ValidateTime(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration)throw new ArgumentOutOfRangeException(nameof(time));
    }
}
