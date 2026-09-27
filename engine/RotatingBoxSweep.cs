using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct RotatingBoxHit(SphereSweepStatus Status, double Time,
    Vector3 Normal, Vector3 Point, float Penetration, int Iterations);

/// <summary>
/// Continuous sphere/rotating-box query for fixed-axis, constant angular velocity
/// and linear sphere flight. Caller splits the interval at force changes/end stops.
/// Supporting-plane velocity and acceleration bounds prevent stepping across a
/// collision. The query neither advances state nor applies an impulse.
/// </summary>
public static class RotatingBoxSweep
{
    private const int MaximumIterations = 100000;
    private const double FloatResolution = 1.1920928955078125e-7;

    public static RotatingBoxHit Cast(Vector3 origin, float radius, Vector3 velocity,
        Vector3 pivot, Vector3 axis, Transform3D initialPose, Vector3 half,
        double angularVelocity, double duration)
    {
        Finite(origin, nameof(origin)); Finite(velocity, nameof(velocity));
        Finite(pivot, nameof(pivot)); Unit(axis, nameof(axis));
        Finite(half, nameof(half));
        if (half.X <= 0 || half.Y <= 0 || half.Z <= 0) throw new ArgumentOutOfRangeException(nameof(half));
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        var squaredAngularVelocity = angularVelocity * angularVelocity;
        if (!double.IsFinite(angularVelocity) || !double.IsFinite(squaredAngularVelocity) ||
            (angularVelocity != 0 && squaredAngularVelocity == 0))
            throw new ArgumentOutOfRangeException(nameof(angularVelocity), "Angular acceleration bound must be representable.");
        if (!double.IsFinite(duration) || duration < 0 || !double.IsFinite(angularVelocity * duration))
            throw new ArgumentOutOfRangeException(nameof(duration));
        Finite(initialPose.Origin, nameof(initialPose));
        var basis = initialPose.Basis;
        Unit(basis.X, nameof(initialPose)); Unit(basis.Y, nameof(initialPose)); Unit(basis.Z, nameof(initialPose));
        if (Math.Abs(basis.X.Dot(basis.Y)) > .00001f || Math.Abs(basis.X.Dot(basis.Z)) > .00001f ||
            Math.Abs(basis.Y.Dot(basis.Z)) > .00001f || Math.Abs(basis.Determinant() - 1) > .0001f)
            throw new ArgumentException("Rotating box requires a proper rigid transform.", nameof(initialPose));
        var centerOffset = initialPose.Origin - pivot;
        Finite(centerOffset, nameof(initialPose));
        var radialOffset = centerOffset - axis * axis.Dot(centerOffset);
        var maximumRadius = Length(radialOffset) + Length(half);
        double time = 0;
        for (var iteration = 1; iteration <= MaximumIterations; iteration++)
        {
            // Reduce in double before converting to the float rotation API.
            var rotation = new Basis(axis, (float)Math.IEEERemainder(angularVelocity * time, Math.Tau));
            var currentBasis = rotation * basis;
            var center = pivot + rotation * centerOffset;
            var sphere = new Vector3((float)(origin.X + velocity.X * time),
                (float)(origin.Y + velocity.Y * time), (float)(origin.Z + velocity.Z * time));
            Finite(center, nameof(initialPose)); Finite(sphere, nameof(duration));
            var sample = SphereSweep.BoxSurface(currentBasis.Transposed() * (sphere - center), half);
            var normal = currentBasis * sample.Normal;
            var point = sphere - normal * sample.Distance;
            var gap = sample.Distance - radius;
            if (!float.IsFinite(gap) || !point.IsFinite())
                throw new InvalidOperationException("Rotating contact exceeds representable world distance.");
            var normalAngularVelocity = Dot(axis.Cross(point - pivot), normal) * angularVelocity;
            var sphereNormalVelocity = Dot(velocity, normal);
            var approach = sphereNormalVelocity - normalAngularVelocity;
            var roundoff = 8 * FloatResolution * (Math.Abs(sphereNormalVelocity) + Math.Abs(normalAngularVelocity));
            if (time == 0 && gap < -SphereSweep.ContactTolerance)
                return new(SphereSweepStatus.Overlapping, 0, normal, point, -gap, iteration);
            if (gap <= SphereSweep.ContactTolerance && (gap < -SphereSweep.ContactTolerance || approach < -roundoff))
                return new(SphereSweepStatus.Contact, time, normal, point, Math.Max(0, -gap), iteration);
            if (time >= duration) return Clear(duration, iteration);

            // Every vertex's projection on this fixed plane has acceleration at
            // most omega² * radial reach * the normal's perpendicular component.
            // Bound the largest initial projected vertex velocity analytically.
            var maximumNormalVelocity = angularVelocity * Dot(axis.Cross(center - pivot), normal);
            maximumNormalVelocity += Math.Abs(angularVelocity * Dot(axis.Cross(currentBasis.X), normal)) * half.X;
            maximumNormalVelocity += Math.Abs(angularVelocity * Dot(axis.Cross(currentBasis.Y), normal)) * half.Y;
            maximumNormalVelocity += Math.Abs(angularVelocity * Dot(axis.Cross(currentBasis.Z), normal)) * half.Z;
            var closing = maximumNormalVelocity - sphereNormalVelocity;
            var perpendicular = normal - axis * axis.Dot(normal);
            var acceleration = squaredAngularVelocity * maximumRadius * Length(perpendicular);
            if (!double.IsFinite(acceleration) || !double.IsFinite(closing))
                throw new InvalidOperationException("Rotating sweep speed bound exceeds scalar precision.");
            // Within the contact skin, allow only a fraction of its width of
            // unresolved travel when tangential/separating; never skip the surface.
            var distance = Math.Max(gap, SphereSweep.ContactTolerance * .25);
            double step;
            if (acceleration == 0)
            {
                if (closing <= 0) return Clear(duration, iteration);
                step = distance / closing;
            }
            else
            {
                var discriminant = closing * closing + 2 * acceleration * distance;
                if (!double.IsFinite(discriminant))
                    throw new InvalidOperationException("Rotating sweep root exceeds scalar precision.");
                var root = Math.Sqrt(discriminant);
                step = closing >= 0 ? 2 * distance / (closing + root) : (root - closing) / acceleration;
            }
            var next = Math.Min(duration, time + step);
            if (next <= time) throw new InvalidOperationException("Rotating sweep cannot make representable progress.");
            time = next;
        }
        throw new InvalidOperationException("Rotating sweep did not converge; no collision-free result was inferred.");
    }

    private static RotatingBoxHit Clear(double duration, int iterations) =>
        new(SphereSweepStatus.Clear, duration, Vector3.Zero, Vector3.Zero, 0, iterations);
    private static double Dot(Vector3 a, Vector3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    private static double Length(Vector3 value) => Math.Sqrt(Dot(value, value));
    private static void Finite(Vector3 value, string name)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(name);
    }
    private static void Unit(Vector3 value, string name)
    {
        Finite(value, name);
        if (Math.Abs(Dot(value, value) - 1) > .00001) throw new ArgumentException("Expected unit vector.", name);
    }
}
