using System;
namespace CuriousContraptions.Physics;

/// <summary>Immutable constant-velocity translation with fixed orientation.
/// Parameter units are supplied by the caller, as for every IRigidTrajectory.</summary>
public sealed class LinearRigidTrajectory : IRigidTrajectory
{
    public double Duration { get; }
    public RigidPose StartPose { get; }
    private CollisionVector Velocity { get; }
    public double LinearAccelerationBound=>0;
    public double AngularAccelerationBound=>0;
    public double AngularSpeedBound=>0;
    public LinearRigidTrajectory(RigidPose start,CollisionVector velocity,double duration)
    {
        if(!start.Center.IsFinite||!start.Rotation.IsValid||!velocity.IsFinite)
            throw new ArgumentException("Linear trajectory requires a valid pose and finite velocity.");
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        StartPose=start;Velocity=velocity;Duration=duration;
        _=At(duration);
    }
    private void ValidateTime(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration)throw new ArgumentOutOfRangeException(nameof(time));
    }
    public RigidPose At(double time)
    {
        ValidateTime(time);
        return time==0?StartPose:new(StartPose.Center+Velocity*time,StartPose.Rotation);
    }
    public CollisionVector LinearVelocityAt(double time) {ValidateTime(time);return Velocity;}
    public double SegmentEndAfter(double time) {ValidateTime(time);return Duration;}
}
