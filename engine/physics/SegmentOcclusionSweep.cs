using System;

namespace CuriousContraptions.Physics;

/// <summary>A finite, zero-thickness visibility segment; no collision-only body.</summary>
public readonly record struct CollisionSegment : IConvexFeatureSupport
{
    public CollisionVector Start { get; }
    public CollisionVector End { get; }
    public CollisionSegment(CollisionVector start,CollisionVector end)
    {
        if(!start.IsFinite||!end.IsFinite)throw new ArgumentException("Segment endpoints must be finite.");
        Start=start;End=end;
    }
    public double RoundingRadius=>0;
    public InteriorBall InteriorBall=>new(Start*.5+End*.5,0);
    public CollisionVector Support(CollisionVector direction)=>
        CollisionVector.Dot(Start,direction)>=CollisionVector.Dot(End,direction)?Start:End;
    public CollisionVector CoreSupport(CollisionVector direction)=>Support(direction);
    public SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)=>
        SupportFeature.FromPoints([Start,End],direction,planeTolerance);
}

/// <summary>Captured deforming segment. SpeedBound bounds the speed of either
/// endpoint throughout one common geometric segment, hence its Hausdorff speed.</summary>
public abstract class SegmentTrajectory
{
    public abstract double Duration { get; }
    public abstract CollisionSegment At(double time);
    public abstract double SegmentEndAfter(double time);
    public abstract double SpeedBound(double start,double end);
}

/// <summary>Both entry and exit of a convex blocker across a deforming segment.
/// Signed separation is Lipschitz under bounded endpoint/support motion.
/// The side-crossing allowance is positional, never a skipped time interval.</summary>
public static class SegmentOcclusionSweep
{
    private const int MaximumIterations=4096;
    public static ScalarSweepResult Cast(SegmentTrajectory segment,ConvexMotion obstacle,double duration,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if(!double.IsFinite(duration)||duration<0||duration>segment.Duration||duration>obstacle.Trajectory.Duration||
            !double.IsFinite(tolerance)||tolerance<=0||tolerance/64==0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        segment.At(duration);obstacle.At(duration);
        ConvexSeparationResult Query(double time)
        {
            var result=ConvexSeparation.Query(segment.At(time),obstacle.At(time),tolerance/64);
            if(!double.IsFinite(result.LowerBound)||!double.IsFinite(result.UpperBound)||
                result.UpperBound<result.LowerBound||result.UpperBound-result.LowerBound>tolerance/16)
                throw new InvalidOperationException("Occlusion separation did not resolve its positional allowance.");
            return result;
        }
        var initial=Query(0);var blocked=initial.LowerBound<=0;
        double time=0;
        var current=initial;
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var lower=blocked?-current.UpperBound:current.LowerBound;
            var upper=blocked?-current.LowerBound:current.UpperBound;
            if(upper<=-tolerance*.25)
                return new(ScalarSweepStatus.Boundary,time,iteration);
            if(time==duration)return new(ScalarSweepStatus.Clear,duration,iteration);
            var end=Math.Min(duration,Math.Min(segment.SegmentEndAfter(time),obstacle.Trajectory.SegmentEndAfter(time)));
            if(!double.IsFinite(end)||end<=time)
                throw new InvalidOperationException("Occlusion path has an invalid segment end.");
            var h=end-time;
            var segmentSpeed=segment.SpeedBound(time,end);
            if(!double.IsFinite(segmentSpeed)||segmentSpeed<0)
                throw new InvalidOperationException("Segment speed bound exceeds numeric range.");
            if(segmentSpeed==0&&ParallelSlideKeepsOcclusion(segment.At(time),obstacle,time,end,tolerance))
            {
                time=end;current=Query(time);continue;
            }
            var speed=segmentSpeed+
                Math.Max(obstacle.LinearVelocityAt(time).Length,obstacle.LinearVelocityAt(end).Length)+
                obstacle.LinearAccelerationBound*h*.5+obstacle.AngularSpeedBound*obstacle.RotationalReach;
            if(speed!=0)speed=Math.BitIncrement(speed*(1+1e-12));
            if(!double.IsFinite(speed)||speed<0)
                throw new InvalidOperationException("Occlusion speed bound exceeds numeric range.");
            // Every point of this advance remains within 3/8 tolerance of the
            // initial side. At a reported crossing the new side is certified.
            var advance=speed==0?h:Math.Min(h,(lower+tolerance*.375)/speed);
            var next=time+advance;
            if(!double.IsFinite(next)||next<=time)
                throw new InvalidOperationException("Occlusion crossing cannot advance within its positional allowance.");
            time=Math.Min(next,end);current=Query(time);
        }
        throw new InvalidOperationException("Occlusion sweep iteration budget exceeded.");
    }

    private static bool ParallelSlideKeepsOcclusion(CollisionSegment segment,ConvexMotion obstacle,
        double start,double end,double tolerance)
    {
        // Translation along a stationary segment cannot change intersection
        // while the entire convex body remains strictly between its endpoint
        // planes. This support certificate applies to every convex geometry.
        if(obstacle.LinearAccelerationBound!=0||obstacle.AngularSpeedBound*obstacle.RotationalReach!=0)return false;
        var delta=segment.End-segment.Start;var length=delta.Length;
        if(!double.IsFinite(length)||length<=0)return false;
        var axis=delta/length;
        if(CollisionVector.Cross(axis,obstacle.LinearVelocityAt(start))!=default)return false;
        bool Inside(double time)
        {
            var shape=obstacle.At(time);
            var lower=CollisionVector.Dot(shape.Support(-axis)-segment.Start,axis);
            var upper=CollisionVector.Dot(shape.Support(axis)-segment.Start,axis);
            return double.IsFinite(lower)&&double.IsFinite(upper)&&lower>tolerance&&upper<length-tolerance;
        }
        return Inside(start)&&Inside(end);
    }
}
