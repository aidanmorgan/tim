using System;
namespace CuriousContraptions.Physics;

/// <summary>Immutable captured pose path with no body/world ownership. Retains full spin segments,
/// including multi-turn motion, rather than reconstructing a shortest arc from endpoints.</summary>
public readonly struct RigidPoseTrajectory : IRigidTrajectory
{
    private readonly RigidRotation[] _rotations;
    private readonly CollisionVector[] _spins;
    private readonly double _step;
    private readonly CollisionVector _velocity,_acceleration;
    private readonly PrescribedBodyMotion? _motion;
    public double Duration { get; }
    public RigidPose StartPose { get; }
    public double AngularSpeedBound { get; }
    public double PhysicalAngularSpeedBound { get; }
    public double LinearAccelerationBound=>_motion?.LinearAccelerationBound??_acceleration.Length;
    public double AngularAccelerationBound=>_motion?.AngularAccelerationBound??0;
    public int SegmentCount=>_motion is { } motion&&motion.Remaining>0&&motion.Remaining<Duration?2:Math.Max(1,_spins?.Length??0);

    // The trajectory builder transfers its freshly constructed arrays; neither it nor this
    // read value exposes or mutates them after this constructor.
    internal RigidPoseTrajectory(RigidPose start,CollisionVector velocity,CollisionVector acceleration,
        double duration,RigidRotation[] rotations,CollisionVector[] spins,double step,
        PrescribedBodyMotion? motion,double angularSpeedBound,double physicalAngularSpeedBound)
    {
        StartPose=start;_velocity=velocity;_acceleration=acceleration;Duration=duration;
        _rotations=rotations;_spins=spins;_step=step;_motion=motion;AngularSpeedBound=angularSpeedBound;PhysicalAngularSpeedBound=physicalAngularSpeedBound;
    }
    private void ValidateTime(double time)
    {
        if(!StartPose.Rotation.IsValid)throw new InvalidOperationException("Uninitialized captured pose path.");
        if(!double.IsFinite(time)||time<0||time>Duration)throw new ArgumentOutOfRangeException(nameof(time));
    }
    public RigidPose At(double time)
    {
        ValidateTime(time);
        if(time==0) return StartPose;
        if(_motion is { } motion)return motion.At(time);
        var center=StartPose.Center+_velocity*time+_acceleration*(time*time*.5);
        if(time==Duration) return new(center,_rotations[^1]);
        var index=Math.Min(_spins.Length-1,(int)(time/_step));
        var local=time-index*_step;
        return new(center,RigidRotation.FromRotationVector(_spins[index]*local)*_rotations[index]);
    }
    public CollisionVector LinearVelocityAt(double time)
    {
        ValidateTime(time);
        var velocity=_motion is { } motion?motion.LinearVelocityAt(time):_velocity+_acceleration*time;
        if(!velocity.IsFinite) throw new InvalidOperationException("Trajectory velocity exceeds numeric range.");
        return velocity;
    }
    public double SegmentEndAfter(double time)
    {
        ValidateTime(time);
        if(time==Duration) return Duration;
        if(_motion is { } motion)return time<motion.Remaining?Math.Min(Duration,motion.Remaining):Duration;
        var index=Math.Min(_spins.Length-1,(int)(time/_step));
        while(index<_spins.Length-1&&(index+1)*_step<=time) index++;
        return index==_spins.Length-1?Duration:(index+1)*_step;
    }
    public CollisionVector AngularVelocityAt(double time)
    {
        ValidateTime(time);
        if(_motion is { } motion)return motion.AngularVelocityAt(time);
        if(time==Duration) return _spins[^1];
        var index=Math.Min(_spins.Length-1,(int)(time/_step));
        while(index<_spins.Length-1&&(index+1)*_step<=time) index++;
        return _spins[index];
    }
}
