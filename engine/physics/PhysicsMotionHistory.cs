using System;
namespace CuriousContraptions.Physics;

/// <summary>Only the accepted prefix of a captured path is visible to readers.</summary>
public readonly struct PhysicsMotionInterval
{
    private readonly RigidPoseTrajectory _path;
    public PhysicsBodyId Body { get; }
    public double StartTime { get; }
    public double Duration { get; }
    public double EndTime { get; }
    internal PhysicsMotionInterval(PhysicsBodyId body,double start,double end,double duration,RigidPoseTrajectory path)
    {
        if(!double.IsFinite(start)||start<0||!double.IsFinite(duration)||duration<=0||
            duration>path.Duration||!double.IsFinite(end)||end<=start)
            throw new ArgumentException("Accepted motion requires a representable positive prefix.");
        // Absolute clock arithmetic can round differently from local duration arithmetic.
        // Admit only a bounded roundoff discrepancy, never a different physical duration.
        var roundoff=4*((start-Math.BitDecrement(start))+(end-Math.BitDecrement(end))+
            (duration-Math.BitDecrement(duration)));
        if(Math.Abs((end-start)-duration)>roundoff)
            throw new ArgumentException("Accepted motion timestamps disagree with the solver duration.");
        _=path.At(0);_=path.At(duration);
        Body=body;StartTime=start;EndTime=end;Duration=duration;_path=path;
    }
    public RigidPose At(double simulationTime)=>_path.At(LocalTime(simulationTime));
    public CollisionVector LinearVelocityAt(double simulationTime)=>_path.LinearVelocityAt(LocalTime(simulationTime));
    /// <summary>Conservative physical angular speed bound from the immutable captured path;
    /// it may include motion beyond this accepted prefix, never underestimate it.</summary>
    public double PhysicalAngularSpeedBound
    {
        get { _=LocalTime(StartTime);return _path.PhysicalAngularSpeedBound; }
    }
    private double LocalTime(double simulationTime)
    {
        if(Duration<=0)throw new InvalidOperationException("Uninitialized accepted motion.");
        if(!double.IsFinite(simulationTime)||simulationTime<StartTime||simulationTime>EndTime)
            throw new ArgumentOutOfRangeException(nameof(simulationTime));
        return simulationTime==EndTime?Duration:
            (simulationTime-StartTime)/(EndTime-StartTime)*Duration;
    }
}
public readonly record struct PhysicsMotionEndpoint(PhysicsBodyId Body,RigidPose Pose);

/// <summary>Owned immutable history for one successful physics Step, not an entire gameplay tick.
/// Boundary poses are right-continuous; final corrected poses supersede the last path endpoint.</summary>
public sealed class PhysicsMotionHistory
{
    private readonly PhysicsMotionInterval[] _intervals;
    private readonly PhysicsMotionEndpoint[] _final;
    public double StartTime { get; }
    public double EndTime { get; }
    public ulong StepIndex { get; }
    public ReadOnlySpan<PhysicsMotionInterval> Intervals=>_intervals;
    public ReadOnlySpan<PhysicsMotionEndpoint> FinalPoses=>_final;
    // Arrays are freshly produced by the world and transferred; never exposed mutably.
    internal PhysicsMotionHistory(double start,double end,ulong step,PhysicsMotionInterval[] intervals,PhysicsMotionEndpoint[] final)
    {
        StartTime=start;EndTime=end;StepIndex=step;_intervals=intervals;_final=final;
    }
    public RigidPose Sample(PhysicsBodyId body,double time)
    {
        if(!double.IsFinite(time)||time<StartTime||time>EndTime)throw new ArgumentOutOfRangeException(nameof(time));
        if(time==EndTime)
        {
            foreach(var pose in _final)if(pose.Body==body)return pose.Pose;
        }
        else
        {
            for(var i=_intervals.Length-1;i>=0;i--)
            {
                var interval=_intervals[i];
                if(interval.Body==body&&time>=interval.StartTime&&time<=interval.EndTime)return interval.At(time);
            }
        }
        throw new ArgumentException("No accepted motion covers this body and time.");
    }
}
