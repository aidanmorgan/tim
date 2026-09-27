using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Numerical convergence of one sphere pair at one simulation instant.
/// Never carries across time advancement. The first impact is always resolved;
/// only repeated, unchanged contact normals can converge relative to their
/// already-resolved approach speed. No absolute low-speed cutoff is used.</summary>
public readonly record struct BodyContactConvergence(Vector3 Normal, double PeakApproachSpeed)
{
    private const double FloatRelativeResolution = 1.1920928955078125e-7;

    private static double Approach(Vector3 firstVelocity, Vector3 secondVelocity, Vector3 normal) =>
        -(((double)firstVelocity.X - secondVelocity.X) * normal.X +
          ((double)firstVelocity.Y - secondVelocity.Y) * normal.Y +
          ((double)firstVelocity.Z - secondVelocity.Z) * normal.Z);

    public BodyContactConvergence Observe(Vector3 firstVelocity, Vector3 secondVelocity, Vector3 normal)
    {
        var speed = Math.Max(0, Approach(firstVelocity, secondVelocity, normal));
        return new(normal, Normal == normal ? Math.Max(PeakApproachSpeed, speed) : speed);
    }

    public bool HasConverged(Vector3 firstVelocity, Vector3 secondVelocity, MovingSphereHit hit, float remainingDuration)
    {
        if (PeakApproachSpeed <= 0 || hit.Status != SphereSweepStatus.Contact ||
            hit.Time != 0 || hit.Penetration != 0 || hit.Normal != Normal)
            return false;
        var approach = Approach(firstVelocity, secondVelocity, hit.Normal);
        // Resolve down to the float precision of the impact, not down to float
        // underflow. Also bound any residual closing displacement by the query's
        // geometric tolerance while the entire remaining interval is advanced.
        return approach >= 0 && approach <= 4 * FloatRelativeResolution * PeakApproachSpeed &&
            approach * remainingDuration <= SphereSweep.ContactTolerance;
    }
}
