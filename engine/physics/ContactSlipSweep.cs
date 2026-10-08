using System;

namespace CuriousContraptions.Physics;

public readonly record struct ContactSlipSample(CollisionVector Slip,CollisionVector Derivative);
public readonly record struct ContactSlipBounds(double Rate,double Curvature,double NormalRate,double NormalCurvature);
public enum ContactSlipStatus { Clear, Boundary }
public readonly record struct ContactSlipResult(ContactSlipStatus Status,double Time,int Iterations);

/// <summary>Find the first boundary of the initial sliding direction along
/// captured paths. Positive derivative bounds certify clear intervals; endpoint
/// samples alone cannot hide a reversal and return inside the interval.</summary>
public static class ContactSlipSweep
{
    private const int MaximumIntervals=4096;
    public static ContactSlipResult Cast(ContactSlipPath path,double duration,double velocityTolerance)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!double.IsFinite(duration)||duration<0||duration>path.Duration||
            !double.IsFinite(velocityTolerance)||velocityTolerance<=0) throw new ArgumentOutOfRangeException(nameof(duration));
        var first=path.At(0).Slip;
        if(first.Length<=velocityTolerance) throw new ArgumentException("A sliding sweep requires nonzero source slip.");
        var direction=first/first.Length;
        double Value(double time)=>CollisionVector.Dot(path.At(time).Slip,direction);
        var visits=0;
        double? Search(double lo,double hi,double left,double right)
        {
            if(++visits>MaximumIntervals) throw new InvalidOperationException($"Contact slip sweep exceeded its interval budget: interval [{lo:R}, {hi:R}], projected slip [{left:R}, {right:R}], source {first.Length:R}, duration {duration:R}, bounds {path.Bounds(lo,hi)}.");
            if(left<=velocityTolerance*.5)
            {
                if(left< -velocityTolerance) throw new InvalidOperationException("Contact slip boundary was crossed before certification.");
                return lo;
            }
            var bound=path.Bounds(lo,hi);
            if(bound is { } limits)
            {
                var h=hi-lo;
                var derivative=CollisionVector.Dot(path.At(lo).Derivative,direction);
                // Taylor lower bound is concave: its minimum is at an endpoint.
                var taylor=Math.Min(left,left+derivative*h-limits.Curvature*h*h*.5);
                var lipschitz=Math.Min(left,right)-limits.Rate*h*.5;
                if(Math.Max(taylor,lipschitz)>velocityTolerance*.5) return null;
            }
            var mid=lo+(hi-lo)*.5;
            if(mid<=lo||mid>=hi)
            {
                if(right<=velocityTolerance&&right>=-velocityTolerance) return hi;
                throw new InvalidOperationException("Contact slip interval cannot be resolved at the requested precision.");
            }
            var middle=Value(mid);
            var before=Search(lo,mid,left,middle);
            return before??Search(mid,hi,middle,right);
        }
        if(duration==0) return new(ContactSlipStatus.Clear,0,0);
        for(double start=0;start<duration;)
        {
            var end=Math.Min(duration,path.SegmentEndAfter(start));
            if(end<=start) throw new InvalidOperationException("Slip segment made no temporal progress.");
            var hit=Search(start,end,Value(start),Value(end));
            if(hit is double time) return new(ContactSlipStatus.Boundary,time,visits);
            start=end;
        }
        return new(ContactSlipStatus.Clear,duration,visits);
    }
}
