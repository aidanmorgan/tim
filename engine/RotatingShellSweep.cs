using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct RotatingShellHit(SphereSweepStatus Status, double Time, double Margin, int Iterations);

/// <summary>Continuous fixed-axis box flight against analytic hollow tubes and frustums.
/// Scalar expansion clearance bounds point travel, including between clear endpoint
/// poses. This query does not move objects, apply impulses or fill their bores.</summary>
public static class RotatingShellSweep
{
    private const int MaximumIterations = 100000;
    private enum ContactTrend { Unresolved, Approaching, Separating }

    private interface IShell
    {
        Transform3D Pose { get; }
        double Resolution { get; }
        double Margin(Transform3D pose, Vector3 half);
        bool Intersects(Transform3D pose, Vector3 half, double inflation);
    }
    private readonly record struct TubeShell(TubeProxy Shape) : IShell
    {
        public Transform3D Pose => Shape.Pose;
        public double Resolution => TubeBoxIntersection.MarginResolution;
        public double Margin(Transform3D pose, Vector3 half) => TubeBoxIntersection.SignedMargin(pose,half,Shape);
        public bool Intersects(Transform3D pose, Vector3 half, double inflation) =>
            TubeBoxIntersection.Intersects(pose,half,Shape,inflation);
    }
    private readonly record struct FrustumShell(FrustumProxy Shape) : IShell
    {
        public Transform3D Pose => Shape.Pose;
        public double Resolution => FrustumBoxIntersection.MarginResolution;
        public double Margin(Transform3D pose, Vector3 half) => FrustumBoxIntersection.SignedMargin(pose,half,Shape);
        public bool Intersects(Transform3D pose, Vector3 half, double inflation) =>
            FrustumBoxIntersection.Intersects(pose,half,Shape,inflation);
    }

    public static RotatingShellHit Cast(Vector3 pivot, Vector3 axis, Transform3D pose,
        Vector3 half, double angularVelocity, TubeProxy tube, double duration) =>
        Cast(pivot,axis,pose,half,angularVelocity,new TubeShell(tube),duration);

    public static RotatingShellHit Cast(Vector3 pivot, Vector3 axis, Transform3D pose,
        Vector3 half, double angularVelocity, FrustumProxy frustum, double duration) =>
        Cast(pivot,axis,pose,half,angularVelocity,new FrustumShell(frustum),duration);

    private static RotatingShellHit Cast<TShell>(Vector3 pivot, Vector3 axis, Transform3D pose,
        Vector3 half, double angularVelocity, TShell shell, double duration) where TShell : struct, IShell
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
            var margin = shell.Margin(current,half);
            if (time == 0 && margin < -SphereSweep.ContactTolerance &&
                shell.Intersects(current,half,-SphereSweep.ContactTolerance))
                return new(SphereSweepStatus.Overlapping,0,margin,iteration);
            if (angularVelocity == 0 || Coaxial(pivot,axis,shell.Pose))
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
                        throw new InvalidOperationException("Shell contact direction exceeds angular precision.");
                    var nextMargin = shell.Margin(At(angle + probeAngle),half);
                    var change = nextMargin - margin;
                    if (change < -2 * shell.Resolution) trend = ContactTrend.Approaching;
                    else if (change > 2 * shell.Resolution) trend = ContactTrend.Separating;
                    probeAngle *= 2;
                }
                if (trend == ContactTrend.Approaching)
                    return new(SphereSweepStatus.Contact,time,margin,iteration);
                if (margin < -SphereSweep.ContactTolerance - shell.Resolution)
                    throw new InvalidOperationException("Shell contact direction is unresolved inside the shell.");
            }
            if (time >= duration) return new(SphereSweepStatus.Clear,duration,margin,iteration);
            var step = Math.Max(margin,SphereSweep.ContactTolerance * .25) / maximumSpeed;
            var next = Math.Min(duration,time + step);
            if (next <= time)
                throw new InvalidOperationException("Rotating shell sweep cannot make representable progress.");
            time = next;
        }
        throw new InvalidOperationException("Rotating shell sweep did not converge; clearance was not inferred.");
    }

    // Exact coaxial motion preserves every point's axial coordinate and radius.
    // Do not use an approximate parallel test to infer invariant clearance.
    private static bool Coaxial(Vector3 pivot,Vector3 axis,Transform3D shell) =>
        Parallel(axis,shell.Basis.X) && Parallel(pivot - shell.Origin,axis);
    private static bool Parallel(Vector3 a,Vector3 b) =>
        (double)a.Y*b.Z - (double)a.Z*b.Y == 0 &&
        (double)a.Z*b.X - (double)a.X*b.Z == 0 &&
        (double)a.X*b.Y - (double)a.Y*b.X == 0;
    private static double Dot(Vector3 a,Vector3 b) =>
        (double)a.X*b.X + (double)a.Y*b.Y + (double)a.Z*b.Z;
}
