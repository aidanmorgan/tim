using System;

namespace CuriousContraptions.Physics;

public readonly record struct TransferForceInterval(double Start,double End,double RateBound);

/// <summary>Continuous unilateral transfer demand on captured speed paths.
/// Source flow is clamped at zero as in mechanical transfer demand. Exposure,
/// store availability and shared supply allocation are separate boundaries.
/// Rate bounds remain valid across zero-slip and saturation corners.</summary>
public sealed class JetTransferForcePath
{
    private readonly TransferSpeedPath _source,_receiver;
    private readonly JetTransferImpedance _impedance;
    public double Duration=>Math.Min(_source.Duration,_receiver.Duration);

    public JetTransferForcePath(TransferSpeedPath source,TransferSpeedPath receiver,JetTransferImpedance impedance)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(impedance);
        if(!double.IsFinite(source.Duration)||source.Duration<0||
            !double.IsFinite(receiver.Duration)||receiver.Duration<0)
            throw new ArgumentException("Transfer force requires finite captured durations.");
        _source=source;_receiver=receiver;_impedance=impedance;
    }

    public double At(double time)
    {
        ValidateTime(time);
        return _impedance.Evaluate(Math.Max(0,_source.At(time)),_receiver.At(time)).Force;
    }

    public double SegmentEndAfter(double time)
    {
        ValidateTime(time);
        var end=Math.Min(_source.SegmentEndAfter(time),_receiver.SegmentEndAfter(time));
        if(!double.IsFinite(end)||end<time||end>Duration||time<Duration&&end==time)
            throw new InvalidOperationException("Transfer force has an invalid captured segment.");
        return end;
    }

    public TransferForceInterval Evaluate(double start,double end)
    {
        ValidateTime(start);ValidateTime(end);
        if(end<start||end>SegmentEndAfter(start))throw new ArgumentOutOfRangeException(nameof(end));
        var first=At(start);var last=At(end);
        if(_impedance.Conductance==0||_impedance.MaximumForce==0)return new(first,last,0);
        // Both scalar clamps are nonexpansive, including at their corners.
        var speedRate=_source.AbsoluteRateBound(start,end)+_receiver.AbsoluteRateBound(start,end);
        var rate=_impedance.Conductance*speedRate;
        if(rate>0)rate=Math.BitIncrement(rate*(1+1e-12));
        if(!double.IsFinite(rate)||rate<0)
            throw new InvalidOperationException("Transfer force rate exceeds numeric range.");
        // Every point is within half the interval of an endpoint. Enclose raw
        // slip, not clamped endpoint force: equal capped endpoints can hide
        // an interior excursion. The material parameters are immutable.
        if(speedRate>0&&end>start)
        {
            var firstSlip=Math.Max(0,_source.At(start))-_receiver.At(start);
            var lastSlip=Math.Max(0,_source.At(end))-_receiver.At(end);
            var halfDuration=Math.BitIncrement(Math.BitIncrement(end-start)*.5);
            var excursion=Math.BitIncrement(Math.BitIncrement(speedRate)*halfDuration);
            var lower=Math.BitDecrement(Math.BitDecrement(Math.Min(firstSlip,lastSlip))-excursion);
            var upper=Math.BitIncrement(Math.BitIncrement(Math.Max(firstSlip,lastSlip))+excursion);
            if(upper<=0||lower>=Math.BitIncrement(_impedance.MaximumForce/_impedance.Conductance))
                return new(first,last,0);
        }
        return new(first,last,rate);
    }

    private void ValidateTime(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration)throw new ArgumentOutOfRangeException(nameof(time));
    }
}
