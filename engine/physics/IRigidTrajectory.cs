namespace CuriousContraptions.Physics;

/// <summary>Captured rigid motion consumed by the one continuous collision
/// pipeline. Rates are derivatives with respect to this path's parameter;
/// physical motion uses seconds and configuration corrections use a unit interval.
/// Derivative bounds cover the complete captured horizon; curvature certificates
/// must remain inside SegmentEndAfter when a derivative is discontinuous.</summary>
public interface IRigidTrajectory
{
    double Duration { get; }
    RigidPose StartPose { get; }
    double LinearAccelerationBound { get; }
    double AngularAccelerationBound { get; }
    CollisionVector LinearVelocityAt(double time);
    double AngularSpeedBound { get; }
    RigidPose At(double time);
    double SegmentEndAfter(double time);
}
