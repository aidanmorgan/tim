using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct WrenchPathTerm(PhysicsBody Body,BodyTrajectory Path,BodyWrench Wrench);
public readonly record struct WrenchPathWorkResult(double Supplied,double Dissipated,double SuppliedErrorBound,double DissipatedErrorBound,
    double SuppliedPowerUpperBound,double DissipatedPowerUpperBound);

/// <summary>Positive and negative work of held world-space wrenches on the exact
/// captured paths plus explicit constant algebraic power, using physical angular velocity.</summary>
public static class WrenchPathWork
{
    private const int MaximumIntervals=262144;
    public static WrenchPathWorkResult Measure(IReadOnlyList<WrenchPathTerm> participants,
        double duration,double workTolerance,double constantPower)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if(!double.IsFinite(constantPower))throw new ArgumentOutOfRangeException(nameof(constantPower));
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        if(!double.IsFinite(workTolerance)||workTolerance<=0)throw new ArgumentOutOfRangeException(nameof(workTolerance));
        var terms=participants.ToArray();
        var identities=new HashSet<PhysicsBodyId>();
        foreach(var term in terms)
        {
            ArgumentNullException.ThrowIfNull(term.Body);ArgumentNullException.ThrowIfNull(term.Path);
            if(!identities.Add(term.Body.Id))throw new ArgumentException("Duplicate work participant.");
            term.Path.ValidateSource(term.Body);
            if(duration>term.Path.Duration)throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if(duration==0)return default;
        var intervals=0;
        double Power(double time)
        {
            double power=constantPower;
            foreach(var term in terms)
            {
                // Physical spin can reverse within one geometric rotation
                // segment; its positive and negative work must remain separate.
                var spin=term.Path.PhysicalAngularVelocityAt(time);
                power+=CollisionVector.Dot(term.Wrench.Force,term.Path.LinearVelocityAt(time))+
                    CollisionVector.Dot(term.Wrench.Torque,spin);
            }
            if(!double.IsFinite(power))throw new InvalidOperationException("Path power exceeds numeric range.");
            return power;
        }
        static double PositiveChord(double first,double last,double width)
        {
            if(first<=0&&last<=0)return 0;
            if(first>=0&&last>=0)return (first*.5+last*.5)*width;
            var positive=Math.Max(first,last);var negative=-Math.Min(first,last);
            // Ratio avoids overflow in positive + negative.
            var fraction=positive>=negative?1/(1+negative/positive):
                (positive/negative)/(1+positive/negative);
            return positive*.5*(width*fraction);
        }
        WrenchPathWorkResult Interval(double start,double end)
        {
            if(++intervals>MaximumIntervals)throw new InvalidOperationException("Path work exceeded its interval budget.");
            var width=end-start;var middle=start+width*.5;
            if(middle<=start||middle>=end)throw new InvalidOperationException("Path work reached numeric resolution.");
            double curvature=0;
            foreach(var term in terms)
            {
                var motion=term.Path.PrescribedMotionAt(middle);
                if(motion is not null)
                    curvature+=term.Wrench.Force.Length*motion.LinearJerkBound+
                        term.Wrench.Torque.Length*motion.AngularJerkBound;
                else curvature+=term.Wrench.Torque.Length*term.Path.PhysicalAngularCurvatureBound;
            }
            var error=curvature*width*width*width/12;
            if(!double.IsFinite(error))throw new InvalidOperationException("Path work bound exceeds numeric range.");
            if(error>workTolerance*(width/duration))
            {
                var left=Interval(start,middle);var right=Interval(middle,end);
                return Sum(left,right);
            }
            var first=Power(start);var last=Power(end);
            // The pointwise chord envelope is +/- M*t*(h-t)/2.
            // Its extreme is at an endpoint when the chord slope dominates;
            // otherwise the maximum departure beyond the endpoint range is dip.
            var bend=curvature*width*width;var difference=Math.Abs(last-first);
            var fraction=bend==0||difference>=bend*.5?0:1-2*difference/bend;
            var dip=bend*.125*fraction*fraction;
            var suppliedError=Math.Max(first,last)+dip<=0?0:error;
            var dissipatedError=Math.Min(first,last)-dip>=0?0:error;
            return new(PositiveChord(first,last,width),PositiveChord(-first,-last,width),suppliedError,dissipatedError,
                Math.Max(0,Math.Max(first,last)+dip),Math.Max(0,-Math.Min(first,last)+dip));
        }
        var result=default(WrenchPathWorkResult);double time=0;
        while(time<duration)
        {
            var end=duration;
            foreach(var term in terms)end=Math.Min(end,term.Path.SegmentEndAfter(time));
            if(end<=time)throw new InvalidOperationException("Path work segment must advance.");
            result=Sum(result,Interval(time,end));time=end;
        }
        if(result.SuppliedErrorBound>workTolerance||result.DissipatedErrorBound>workTolerance)throw new InvalidOperationException("Path work tolerance was not met.");
        return result;
    }
    private static WrenchPathWorkResult Sum(WrenchPathWorkResult a,WrenchPathWorkResult b)
    {
        var result=new WrenchPathWorkResult(a.Supplied+b.Supplied,a.Dissipated+b.Dissipated,a.SuppliedErrorBound+b.SuppliedErrorBound,a.DissipatedErrorBound+b.DissipatedErrorBound,
            Math.Max(a.SuppliedPowerUpperBound,b.SuppliedPowerUpperBound),
            Math.Max(a.DissipatedPowerUpperBound,b.DissipatedPowerUpperBound));
        if(!double.IsFinite(result.Supplied)||!double.IsFinite(result.Dissipated)||!double.IsFinite(result.SuppliedErrorBound)||!double.IsFinite(result.DissipatedErrorBound)||
            !double.IsFinite(result.SuppliedPowerUpperBound)||!double.IsFinite(result.DissipatedPowerUpperBound))
            throw new InvalidOperationException("Path work exceeds numeric range.");
        return result;
    }
}
