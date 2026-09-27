using Godot;
using System;

namespace CuriousContraptions;

public enum SphereSweepStatus { Clear, Contact, Overlapping }

public readonly record struct SphereSweepResult(SphereSweepStatus Status, float Distance, Vector3 Normal, float Penetration);

/// <summary>Conservative finite-radius sweep against a signed-distance surface in the same coordinate space.
/// Requires a distance bound that never overestimates clearance and an outward unit normal.
/// Rigid transforms preserve this contract; non-uniform scaling does not.</summary>
public static class SphereSweep
{
    public const float ContactTolerance = .0001f;

    public static SphereSweepResult Cast(Vector3 origin, float radius, Vector3 displacement,
        Func<Vector3, (Vector3 Normal, float Distance)> surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        if (!origin.IsFinite()) throw new ArgumentOutOfRangeException(nameof(origin));
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!displacement.IsFinite()) throw new ArgumentOutOfRangeException(nameof(displacement));
        var length = displacement.Length();
        if (!float.IsFinite(length)) throw new ArgumentOutOfRangeException(nameof(displacement));
        var direction = length > 0 ? displacement / length : Vector3.Zero;
        // Double accumulation guarantees progress even where float travel + tolerance would round back.
        double travel = 0;
        while (true)
        {
            var sample = surface(origin + direction * (float)travel);
            if (!float.IsFinite(sample.Distance) || !sample.Normal.IsFinite()
                || Mathf.Abs(sample.Normal.LengthSquared() - 1) > .001f)
                throw new ArgumentException("Surface must return a finite distance and unit normal.", nameof(surface));
            var gap = sample.Distance - radius;
            if (travel == 0 && gap < -ContactTolerance)
                return new(SphereSweepStatus.Overlapping, 0, sample.Normal, -gap);
            if (gap <= ContactTolerance && (gap < -ContactTolerance || direction.Dot(sample.Normal) < -1e-6f))
                return new(SphereSweepStatus.Contact, (float)travel, sample.Normal, Mathf.Max(0, -gap));
            if (travel >= length) return new(SphereSweepStatus.Clear, length, Vector3.Zero, 0);
            // Re-sample tangent/separating contacts rather than ignoring that surface:
            // a straight path tangent to the inside of a curved bore can enter its wall.
            travel = Math.Min(length, travel + Math.Max(ContactTolerance, gap));
        }
    }

    public static (Vector3 Normal, float Distance) BoxSurface(Vector3 point, Vector3 half)
    {
        var closest = point.Clamp(-half, half);
        var offset = point - closest;
        var distance = offset.Length();
        if (distance > 0) return (offset / distance, distance);
        var clearance = half - point.Abs();
        var axis = clearance.X <= clearance.Y && clearance.X <= clearance.Z ? 0
            : clearance.Y <= clearance.Z ? 1 : 2;
        var normal = Vector3.Zero;
        normal[axis] = point[axis] >= 0 ? 1 : -1;
        return (normal, -clearance[axis]);
    }

    public static (Vector3 Normal, float Distance) SphereSurface(Vector3 point, float radius)
    {
        var distance = point.Length();
        return (distance > 0 ? point / distance : Vector3.Up, distance - radius);
    }
}
