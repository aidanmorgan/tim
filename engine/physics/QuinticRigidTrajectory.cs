using System;

namespace CuriousContraptions.Physics;

/// <summary>Prescribed rigid translation and world-axis rotation with quintic
/// easing. Pose, velocity and acceleration are continuous at both endpoints.
/// The complete rotation vector is retained, including multi-turn paths.</summary>
public sealed class QuinticRigidTrajectory : IRigidTrajectory
{
    public RigidPose StartPose { get; }
    public double Duration { get; }
    public CollisionVector Translation { get; }
    public CollisionVector RotationVector { get; }
    public double LinearAccelerationBound { get; }
    public double AngularSpeedBound { get; }
    public double AngularAccelerationBound { get; }
    public double LinearJerkBound { get; }
    public double AngularJerkBound { get; }

    public QuinticRigidTrajectory(RigidPose start,CollisionVector translation,CollisionVector rotation,double duration)
    {
        if(!start.Rotation.IsValid||!start.Center.IsFinite||!translation.IsFinite||!rotation.IsFinite)
            throw new ArgumentException("Prescribed path requires a valid pose and finite increments.");
        if(!double.IsFinite(duration)||duration<=0)throw new ArgumentOutOfRangeException(nameof(duration));
        StartPose=start;Translation=translation;RotationVector=rotation;Duration=duration;
        // max s'=15/8 and max |s''|=10/sqrt(3) on [0,1].
        LinearAccelerationBound=translation.Length*(10/Math.Sqrt(3))/duration/duration;
        AngularSpeedBound=rotation.Length*1.875/duration;
        LinearJerkBound=translation.Length*60/duration/duration/duration;
        AngularJerkBound=rotation.Length*60/duration/duration/duration;
        AngularAccelerationBound=rotation.Length*(10/Math.Sqrt(3))/duration/duration;
        if(!double.IsFinite(LinearJerkBound)||!double.IsFinite(AngularJerkBound)||!double.IsFinite(LinearAccelerationBound)||!double.IsFinite(AngularSpeedBound)||
            !double.IsFinite(AngularAccelerationBound)||!double.IsFinite(translation.Length*1.875/duration))
            throw new ArgumentException("Prescribed derivatives exceed numeric range.");
        At(duration);
    }
    private double UnitTime(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration)throw new ArgumentOutOfRangeException(nameof(time));
        return time/Duration;
    }
    // s(1-u)=1-s(u): evaluate near the closest endpoint to avoid cancellation
    // producing a value above one immediately before the endpoint.
    private static double Blend(double u)=>u>.5?1-Blend(1-u):u*u*u*(10+u*(-15+6*u));
    private double Rate(double time)
    {
        var u=UnitTime(time);
        return 30*u*u*(1-u)*(1-u)/Duration;
    }
    private double Acceleration(double time)
    {
        var u=UnitTime(time);
        return 60*u*(1-u)*(1-2*u)/Duration/Duration;
    }
    public RigidPose At(double time)
    {
        var u=UnitTime(time);
        if(time==0)return StartPose;
        var blend=Blend(u);
        return new(StartPose.Center+Translation*blend,
            RigidRotation.FromRotationVector(RotationVector*blend)*StartPose.Rotation);
    }
    public CollisionVector LinearVelocityAt(double time)=>Translation*Rate(time);
    public CollisionVector LinearAccelerationAt(double time)=>Translation*Acceleration(time);
    public CollisionVector AngularVelocityAt(double time)=>RotationVector*Rate(time);
    public CollisionVector AngularAccelerationAt(double time)=>RotationVector*Acceleration(time);
    public double SegmentEndAfter(double time)
    {
        UnitTime(time);return Duration;
    }
}
