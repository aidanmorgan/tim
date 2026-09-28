namespace CuriousContraptions.Physics;

/// <summary>Captured rigid motion consumed by the one continuous collision
/// pipeline. Rates are derivatives with respect to this path's parameter;
/// free flight uses seconds and configuration corrections use a unit interval.</summary>
public interface IRigidTrajectory
{
    double Duration { get; }
    RigidPose StartPose { get; }
    CollisionVector LinearVelocity { get; }
    double AngularSpeedBound { get; }
    RigidPose At(double time);
    double SegmentEndAfter(double time);
}
