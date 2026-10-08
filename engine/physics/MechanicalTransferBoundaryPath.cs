using System;

namespace CuriousContraptions.Physics;

public enum MechanicalTransferBoundary { SourceFlow, Slip, Saturation }
public enum MechanicalBoundarySide { Nonnegative, Nonpositive }

/// <summary>Signed source/receiver boundary on captured transfer-coordinate speeds.
/// Orientation is fixed at capture; allowance belongs to the sweep caller.</summary>
public sealed class MechanicalTransferBoundaryPath : ScalarBoundaryPath
{
    private readonly TransferSpeedPath _source,_receiver;
    private readonly double _offset;
    public MechanicalTransferBoundary Boundary { get; }
    public MechanicalBoundarySide Side { get; }
    public override double Duration=>Math.Min(_source.Duration,_receiver.Duration);
    public MechanicalTransferBoundaryPath(TransferSpeedPath source,TransferSpeedPath receiver,
        MechanicalTransferBoundary boundary,JetTransferImpedance impedance,double departureTolerance)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(impedance);
        if(!Enum.IsDefined(boundary))throw new ArgumentOutOfRangeException(nameof(boundary));
        if(!double.IsFinite(departureTolerance)||departureTolerance<0)
            throw new ArgumentOutOfRangeException(nameof(departureTolerance));
        _source=source;_receiver=receiver;Boundary=boundary;
        _offset=boundary==MechanicalTransferBoundary.Saturation?impedance.MaximumForce/impedance.Conductance:0;
        if(!double.IsFinite(_offset)||_offset<0)
            throw new ArgumentOutOfRangeException(nameof(impedance),"Saturation requires a representable slip threshold.");
        var first=boundary==MechanicalTransferBoundary.SourceFlow?source.At(0):
            (ReferenceEquals(source,receiver)?0:source.At(0)-receiver.At(0))-_offset;
        if(!double.IsFinite(first))throw new InvalidOperationException("Transfer boundary exceeds numeric range.");
        var orientation=first;
        if(boundary==MechanicalTransferBoundary.SourceFlow&&departureTolerance>0&&Math.Abs(first)<=departureTolerance)
        {
            // At the switching surface either departure is admissible. Orient
            // toward the initial derivative; the full path certificate still
            // detects a subsequent return across the fixed allowance.
            var rate=source.Evaluate(0,0).Start.Rate;
            orientation=rate!=0?rate:source.At(Duration);
            if(!double.IsFinite(orientation))throw new InvalidOperationException("Source departure exceeds numeric range.");
        }
        Side=orientation<0?MechanicalBoundarySide.Nonpositive:MechanicalBoundarySide.Nonnegative;
    }

    public override double SegmentEndAfter(double time)=>
        Math.Min(_source.SegmentEndAfter(time),_receiver.SegmentEndAfter(time));

    public override ScalarBoundaryInterval Evaluate(double start,double end)
    {
        if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||
            end>SegmentEndAfter(start))throw new ArgumentOutOfRangeException(nameof(end));
        if(Boundary!=MechanicalTransferBoundary.SourceFlow&&ReferenceEquals(_source,_receiver))
            return new(new(_offset,0),new(_offset,0),0,0);
        var source=_source.Evaluate(start,end);
        var first=source.Start;var last=source.End;var curvature=Math.Max(source.Curvature,source.ChordCurvature);
        if(Boundary!=MechanicalTransferBoundary.SourceFlow)
        {
            var receiver=_receiver.Evaluate(start,end);
            first=new(first.Value-receiver.Start.Value-_offset,first.Rate-receiver.Start.Rate);
            last=new(last.Value-receiver.End.Value-_offset,last.Rate-receiver.End.Rate);
            curvature+=Math.Max(receiver.Curvature,receiver.ChordCurvature);
        }
        if(!double.IsFinite(first.Value)||!double.IsFinite(first.Rate)||!double.IsFinite(last.Value)||
            !double.IsFinite(last.Rate)||!double.IsFinite(curvature))
            throw new InvalidOperationException("Transfer boundary exceeds numeric range.");
        return Side switch
        {
            MechanicalBoundarySide.Nonnegative=>new(first,last,curvature,curvature),
            MechanicalBoundarySide.Nonpositive=>new(new(-first.Value,-first.Rate),new(-last.Value,-last.Rate),curvature,curvature),
            _=>throw new InvalidOperationException("Undefined transfer boundary orientation.")
        };
    }
}
