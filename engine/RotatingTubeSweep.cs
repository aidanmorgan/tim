using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct RotatingTubeHit(SphereSweepStatus Status, double Time, double Margin, int Iterations);

/// <summary>Continuous fixed-axis box flight against an analytic hollow tube.
/// Scalar expansion clearance bounds point travel, including between clear endpoint
/// poses. This query does not move objects, apply impulses or fill the tube bore.</summary>
public static class RotatingTubeSweep
{
    private const int MaximumIterations = 100000;
    private enum ContactTrend { Unresolved, Approaching, Separating }

    public static RotatingTubeHit Cast(Vector3 pivot, Vector3 axis, Transform3D pose,
        Vector3 half, double angularVelocity, TubeProxy tube, double duration)
    {
        if (!pivot.IsFinite() || !axis.IsFinite() || Math.Abs(Dot(axis,axis) - 1) > .00001)
            throw new ArgumentException("Expected finite pivot and unit rotation axis.");
        if (!double.IsFinite(angularVelocity)) throw new ArgumentOutOfRangeException(nameof(angularVelocity));
        if (!double.IsFinite(duration) || duration < 0 || !double.IsFinite(angularVelocity * duration))
            throw new ArgumentOutOfRangeException(nameof(duration));
        var offset = pose.Origin - pivot;
        if (!offset.IsFinite()) throw new ArgumentOutOfRangeException(nameof(pose));
        var reach = Math.Sqrt(Dot(offset,offset)) + Math.Sqrt(Dot(half,half));
        var maximumSpeed = Math.Abs(angularVelocity) * reach;
        if (!double.IsFinite(maximumSpeed) || (angularVelocity != 0 && maximumSpeed == 0))
            throw new ArgumentOutOfRangeException(nameof(angularVelocity),"Unrepresentable rotational speed bound.");
        Transform3D At(double angle)
        {
            var rotation = new Basis(axis,(float)Math.IEEERemainder(angle,Math.Tau));
            return new(rotation * pose.Basis,pivot + rotation * offset);
        }
        double time = 0;
        for (var iteration = 1; iteration <= MaximumIterations; iteration++)
        {
            var angle = angularVelocity * time;
            var current = At(angle);
            var margin = TubeBoxIntersection.SignedMargin(current,half,tube);
            if (time == 0 && margin < -SphereSweep.ContactTolerance &&
                TubeBoxIntersection.Intersects(current,half,tube,-SphereSweep.ContactTolerance))
                return new(SphereSweepStatus.Overlapping,0,margin,iteration);
            if (angularVelocity == 0 || Coaxial(pivot,axis,tube.Pose))
                return new(SphereSweepStatus.Clear,duration,margin,iteration);
            if (margin <= SphereSweep.ContactTolerance)
            {
                // A tangent face can curve inward with zero first-order normal
                // speed. Increase a direction probe only while its clearance
                // change is below the two measurements' resolution. This probe
                // never advances simulation time or changes the contact skin.
                var trend = ContactTrend.Unresolved;
                var probeAngle = Math.Sign(angularVelocity) * SphereSweep.ContactTolerance * .25 / reach;
                for (var probe = 0; probe < 9 && trend == ContactTrend.Unresolved; probe++)
                {
                    if (probeAngle == 0 || angle + probeAngle == angle)
                        throw new InvalidOperationException("Tube contact direction exceeds angular precision.");
                    var nextMargin = TubeBoxIntersection.SignedMargin(At(angle + probeAngle),half,tube);
                    var change = nextMargin - margin;
                    if (change < -2 * TubeBoxIntersection.MarginResolution) trend = ContactTrend.Approaching;
                    else if (change > 2 * TubeBoxIntersection.MarginResolution) trend = ContactTrend.Separating;
                    probeAngle *= 2;
                }
                if (trend == ContactTrend.Approaching)
                    return new(SphereSweepStatus.Contact,time,margin,iteration);
                if (margin < -SphereSweep.ContactTolerance - TubeBoxIntersection.MarginResolution)
                    throw new InvalidOperationException("Tube contact direction is unresolved inside the shell.");
            }
            if (time >= duration) return new(SphereSweepStatus.Clear,duration,margin,iteration);
            var step = Math.Max(margin,SphereSweep.ContactTolerance * .25) / maximumSpeed;
            var next = Math.Min(duration,time + step);
            if (next <= time)
                throw new InvalidOperationException("Rotating tube sweep cannot make representable progress.");
            time = next;
        }
        throw new InvalidOperationException("Rotating tube sweep did not converge; clearance was not inferred.");
    }

    // Exact coaxial motion preserves every point's axial coordinate and radius.
    // Do not use an approximate parallel test to infer invariant clearance.
    private static bool Coaxial(Vector3 pivot,Vector3 axis,Transform3D tube) =>
        Parallel(axis,tube.Basis.X) && Parallel(pivot - tube.Origin,axis);
    private static bool Parallel(Vector3 a,Vector3 b) =>
        (double)a.Y*b.Z - (double)a.Z*b.Y == 0 &&
        (double)a.Z*b.X - (double)a.X*b.Z == 0 &&
        (double)a.X*b.Y - (double)a.Y*b.X == 0;
    private static double Dot(Vector3 a,Vector3 b) =>
        (double)a.X*b.X + (double)a.Y*b.Y + (double)a.Z*b.Z;
}
