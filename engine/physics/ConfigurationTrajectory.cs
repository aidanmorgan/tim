using System;

namespace CuriousContraptions.Physics;

/// <summary>Exact configuration-correction path, including the complete declared
/// rotation vector rather than a shortest-arc reconstruction of its endpoint.</summary>
public sealed class ConfigurationTrajectory : IRigidTrajectory
{
    public double Duration=>1;
    public RigidPose StartPose { get; }
    public CollisionVector LinearVelocity { get; }
    public CollisionVector RotationVector { get; }
    public double AngularSpeedBound=>RotationVector.Length;
    public ConfigurationTrajectory(RigidPose start,CollisionVector translation,CollisionVector rotation)
    {
        if(!start.Rotation.IsValid||!translation.IsFinite||!rotation.IsFinite||!double.IsFinite(rotation.Length))
            throw new ArgumentException("Correction path requires a valid pose and finite increments.");
        StartPose=start; LinearVelocity=translation; RotationVector=rotation;
        At(1);
    }
    public RigidPose At(double time)
    {
        if(!double.IsFinite(time)||time<0||time>1) throw new ArgumentOutOfRangeException(nameof(time));
        if(time==0) return StartPose;
        return new(StartPose.Center+LinearVelocity*time,RigidRotation.FromRotationVector(RotationVector*time)*StartPose.Rotation);
    }
    public double SegmentEndAfter(double time)
    {
        if(!double.IsFinite(time)||time<0||time>1) throw new ArgumentOutOfRangeException(nameof(time));
        return 1;
    }
}
