using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable cursor on a finite prescribed path, held at its endpoint
/// thereafter. LocalPose binds an offset body/center of mass to the moving frame.</summary>
public sealed record PrescribedBodyMotion
{
    public QuinticRigidTrajectory Path { get; }
    public RigidPose LocalPose { get; }
    public double Time { get; }
    public double Remaining=>Path.Duration-Time;
    public double AngularSpeedBound=>Remaining==0?0:Path.AngularSpeedBound;
    public double AngularAccelerationBound=>Remaining==0?0:Path.AngularAccelerationBound;
    public double AngularJerkBound=>Remaining==0?0:Path.AngularJerkBound;
    public double LinearAccelerationBound=>Remaining==0?0:Path.LinearAccelerationBound+
        (AngularAccelerationBound+AngularSpeedBound*AngularSpeedBound)*LocalPose.Center.Length;
    public double LinearJerkBound=>Remaining==0?0:Path.LinearJerkBound+
        (AngularJerkBound+3*AngularSpeedBound*AngularAccelerationBound+Math.Pow(AngularSpeedBound,3))*LocalPose.Center.Length;
    public PrescribedBodyMotion(QuinticRigidTrajectory path,RigidPose localPose,double time)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(!localPose.Center.IsFinite||!localPose.Rotation.IsValid)throw new ArgumentException("Invalid local body pose.");
        if(!double.IsFinite(time)||time<0||time>path.Duration)throw new ArgumentOutOfRangeException(nameof(time));
        Path=path;LocalPose=localPose;Time=time;
        if(!double.IsFinite(LinearAccelerationBound)||!double.IsFinite(LinearJerkBound))
            throw new ArgumentException("Offset motion derivatives exceed numeric range.");
    }
    private double SampleTime(double elapsed)
    {
        if(!double.IsFinite(elapsed)||elapsed<0)throw new ArgumentOutOfRangeException(nameof(elapsed));
        return elapsed>=Remaining?Path.Duration:Time+elapsed;
    }
    public PrescribedBodyMotion Advance(double elapsed)=>new(Path,LocalPose,SampleTime(elapsed));
    public RigidPose At(double elapsed)
    {
        var pose=Path.At(SampleTime(elapsed));
        return new(pose.TransformPoint(LocalPose.Center),pose.Rotation*LocalPose.Rotation);
    }
    public CollisionVector AngularVelocityAt(double elapsed)=>Path.AngularVelocityAt(SampleTime(elapsed));
    public CollisionVector AngularAccelerationAt(double elapsed)=>Path.AngularAccelerationAt(SampleTime(elapsed));
    public CollisionVector LinearVelocityAt(double elapsed)
    {
        var time=SampleTime(elapsed);var arm=Path.At(time).Rotation.Apply(LocalPose.Center);
        return Path.LinearVelocityAt(time)+CollisionVector.Cross(Path.AngularVelocityAt(time),arm);
    }
    public CollisionVector LinearAccelerationAt(double elapsed)
    {
        var time=SampleTime(elapsed);var arm=Path.At(time).Rotation.Apply(LocalPose.Center);
        var spin=Path.AngularVelocityAt(time);
        return Path.LinearAccelerationAt(time)+CollisionVector.Cross(Path.AngularAccelerationAt(time),arm)+
            CollisionVector.Cross(spin,CollisionVector.Cross(spin,arm));
    }
}
