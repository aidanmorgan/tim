using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct RotatingObstacleHit(SphereSweepStatus Status, double Time,
    Vector3 Normal, double Gap, int Iterations);

/// <summary>
/// Fixed-axis rotating box against a stationary oriented box. Separating axes
/// determine overlap; conservative supporting-plane bounds determine safe time
/// advancement. This is a query, not a contact response or world integration.
/// </summary>
public static class RotatingBoxObstacleSweep
{
    private const int MaximumIterations = 100000;
    private const double FloatResolution = 1.1920928955078125e-7;

    public static RotatingObstacleHit Cast(Vector3 pivot, Vector3 axis,
        Transform3D pose, Vector3 half, double angularVelocity,
        Transform3D obstacle, Vector3 obstacleHalf, double duration)
    {
        Finite(pivot, nameof(pivot)); Unit(axis, nameof(axis));
        Rigid(pose, nameof(pose)); Rigid(obstacle, nameof(obstacle));
        Extent(half, nameof(half)); Extent(obstacleHalf, nameof(obstacleHalf));
        var squaredSpeed = angularVelocity * angularVelocity;
        if (!double.IsFinite(angularVelocity) || !double.IsFinite(squaredSpeed) ||
            (angularVelocity != 0 && squaredSpeed == 0))
            throw new ArgumentOutOfRangeException(nameof(angularVelocity));
        if (!double.IsFinite(duration) || duration < 0 || !double.IsFinite(angularVelocity * duration))
            throw new ArgumentOutOfRangeException(nameof(duration));
        var offset = pose.Origin - pivot;
        Finite(offset, nameof(pose));
        var reach = Length(offset - axis * axis.Dot(offset)) + Length(half);
        // Rotation leaves projection on its own axis invariant. Check this
        // plane even when another SAT axis currently has a larger clearance:
        // touching axial faces must not become a false radial impact later.
        var centerDelta = pose.Origin - obstacle.Origin;
        Finite(centerDelta, nameof(obstacle));
        var axialGap = Math.Abs(Dot(centerDelta, axis));
        for (var i = 0; i < 3; i++)
            axialGap -= Math.Abs(Dot(pose.Basis[i], axis)) * half[i] +
                Math.Abs(Dot(obstacle.Basis[i], axis)) * obstacleHalf[i];
        if (axialGap >= -SphereSweep.ContactTolerance)
            return new(SphereSweepStatus.Clear, duration, axis, axialGap, 1);
        double time = 0;
        for (var iteration = 1; iteration <= MaximumIterations; iteration++)
        {
            var rotation = new Basis(axis, (float)Math.IEEERemainder(angularVelocity * time, Math.Tau));
            var current = new Transform3D(rotation * pose.Basis, pivot + rotation * offset);
            var sample = Separation(current, half, obstacle, obstacleHalf, pivot, axis, angularVelocity);
            var normal = sample.Normal; // From obstacle towards rotating box.
            var gap = sample.Gap;
            if (time == 0 && gap < -SphereSweep.ContactTolerance)
                return new(SphereSweepStatus.Overlapping, 0, normal, gap, iteration);

            // Bound every vertex's motion towards this fixed supporting plane.
            var centerRate = angularVelocity * Dot(axis.Cross(current.Origin - pivot), normal);
            double maximumClosing = -centerRate;
            for (var dimension = 0; dimension < 3; dimension++)
            {
                var direction = current.Basis[dimension];
                var rate = angularVelocity * Dot(axis.Cross(direction), normal);
                maximumClosing += half[dimension] * Math.Abs(rate);
            }
            var perpendicular = normal - axis * axis.Dot(normal);
            var acceleration = squaredSpeed * reach * Length(perpendicular);
            if (!double.IsFinite(maximumClosing) || !double.IsFinite(acceleration))
                throw new InvalidOperationException("Rotating obstacle sweep exceeds scalar precision.");
            // An invariant separating plane also handles axial tangent motion.
            if (gap >= -SphereSweep.ContactTolerance && acceleration == 0 && maximumClosing <= 0)
                return new(SphereSweepStatus.Clear, duration, normal, gap, iteration);
            var roundoff = 8 * FloatResolution * Math.Abs(angularVelocity) * reach;
            if (gap <= SphereSweep.ContactTolerance &&
                (gap < -SphereSweep.ContactTolerance || sample.Rate < -roundoff))
                return new(SphereSweepStatus.Contact, time, normal, gap, iteration);
            if (time >= duration)
                return new(SphereSweepStatus.Clear, duration, normal, gap, iteration);

            // Every vertex has normal acceleration bounded by omega² * radial
            // reach * normal's perpendicular component. No endpoint-only test.
            var distance = Math.Max(gap, SphereSweep.ContactTolerance * .25);
            double step;
            if (acceleration == 0)
            {
                if (maximumClosing <= 0)
                    return new(SphereSweepStatus.Clear, duration, normal, gap, iteration);
                step = distance / maximumClosing;
            }
            else
            {
                var discriminant = maximumClosing * maximumClosing + 2 * acceleration * distance;
                if (!double.IsFinite(discriminant))
                    throw new InvalidOperationException("Rotating obstacle sweep root exceeds scalar precision.");
                var root = Math.Sqrt(discriminant);
                step = maximumClosing >= 0 ? 2 * distance / (maximumClosing + root) :
                    (root - maximumClosing) / acceleration;
            }
            var next = Math.Min(duration, time + step);
            if (next <= time)
                throw new InvalidOperationException("Rotating obstacle sweep cannot make representable progress.");
            time = next;
        }
        throw new InvalidOperationException("Rotating obstacle sweep did not converge; clearance was not inferred.");
    }

    // Static OBB SAT: six face normals and nine edge cross products.
    // The largest signed projection gap is a lower bound on Euclidean clearance,
    // not a closest-points distance. Negative means penetration on every axis.
    private static (double Gap, Vector3 Normal, double Rate) Separation(Transform3D a, Vector3 ah,
        Transform3D b, Vector3 bh, Vector3 pivot, Vector3 axis, double speed)
    {
        var delta = a.Origin - b.Origin;
        Finite(delta, nameof(b));
        double largest = double.NegativeInfinity;
        var normal = Vector3.Zero;
        double derivative = double.NegativeInfinity;
        var centerVelocity = axis.Cross(a.Origin - pivot);
        var tie = 8 * FloatResolution * (Length(delta) + Length(ah) + Length(bh));
        void Test(Vector3 candidate, Vector3 candidateRate)
        {
            var length = Length(candidate);
            if (length == 0) return;
            candidate /= (float)length;
            var normalRate = (candidateRate - candidate * (float)Dot(candidate, candidateRate)) / (float)length;
            var center = Dot(delta, candidate);
            var centerDerivative = speed * (Dot(centerVelocity, candidate) + Dot(delta, normalRate));
            static double AbsoluteDerivative(double value, double rate) =>
                Math.Abs(value) <= 8 * FloatResolution ? Math.Abs(rate) : Math.Sign(value) * rate;
            var gap = Math.Abs(center);
            var rate = AbsoluteDerivative(center, centerDerivative);
            for (var i = 0; i < 3; i++)
            {
                var ap = Dot(a.Basis[i], candidate);
                var bp = Dot(b.Basis[i], candidate);
                gap -= Math.Abs(ap) * ah[i] + Math.Abs(bp) * bh[i];
                rate -= AbsoluteDerivative(ap, speed * (Dot(axis.Cross(a.Basis[i]), candidate) +
                    Dot(a.Basis[i], normalRate))) * ah[i] +
                    AbsoluteDerivative(bp, speed * Dot(b.Basis[i], normalRate)) * bh[i];
            }
            if (gap > largest + tie) derivative = rate;
            else if (gap >= largest - tie) derivative = Math.Max(derivative, rate);
            if (gap <= largest) return;
            largest = gap;
            normal = center >= 0 ? candidate : -candidate;
        }
        for (var i = 0; i < 3; i++)
        {
            Test(a.Basis[i], axis.Cross(a.Basis[i]));
            Test(b.Basis[i], Vector3.Zero);
        }
        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++)
            Test(a.Basis[i].Cross(b.Basis[j]), axis.Cross(a.Basis[i]).Cross(b.Basis[j]));
        if (!double.IsFinite(largest) || !normal.IsFinite())
            throw new InvalidOperationException("Box separation exceeds representable geometry.");
        return (largest, normal, derivative);
    }

    private static double Dot(Vector3 a, Vector3 b) =>
        (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    private static double Length(Vector3 value) => Math.Sqrt(Dot(value, value));
    private static void Finite(Vector3 value, string name)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(name);
    }
    private static void Unit(Vector3 value, string name)
    {
        Finite(value, name);
        if (Math.Abs(Dot(value, value) - 1) > .00001)
            throw new ArgumentException("Expected unit vector.", name);
    }
    private static void Extent(Vector3 value, string name)
    {
        Finite(value, name);
        if (value.X <= 0 || value.Y <= 0 || value.Z <= 0)
            throw new ArgumentOutOfRangeException(name);
    }
    private static void Rigid(Transform3D pose, string name)
    {
        Finite(pose.Origin, name);
        Unit(pose.Basis.X, name); Unit(pose.Basis.Y, name); Unit(pose.Basis.Z, name);
        if (Math.Abs(Dot(pose.Basis.X, pose.Basis.Y)) > .00001 ||
            Math.Abs(Dot(pose.Basis.X, pose.Basis.Z)) > .00001 ||
            Math.Abs(Dot(pose.Basis.Y, pose.Basis.Z)) > .00001 ||
            Math.Abs(pose.Basis.Determinant() - 1) > .0001)
            throw new ArgumentException("Expected proper rigid box transform.", name);
    }
}
