using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Exact-shape box/hollow-cylinder intersection, evaluated at finite
/// floating-point precision. Clip by the axial slab, project the resulting
/// convex polytope onto YZ, then compare its radial range with the annulus.
/// No cylinder tessellation or filled-bore approximation is used.</summary>
public static class TubeBoxIntersection
{
    public const double MarginResolution = .000003125;

    private readonly record struct Point(double X, double Y) : IComparable<Point>
    {
        public int CompareTo(Point other)
        {
            var x = X.CompareTo(other.X);
            return x != 0 ? x : Y.CompareTo(other.Y);
        }
        public double SquaredLength => X * X + Y * Y;
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
        public static double Cross(Point a, Point b) => a.X * b.Y - a.Y * b.X;
    }

    public static bool Intersects(Transform3D box, Vector3 half, TubeProxy tube, double inflation = 0)
    {
        if (!double.IsFinite(inflation)) throw new ArgumentOutOfRangeException(nameof(inflation));
        Span<Vector3> corners = stackalloc Vector3[8];
        Prepare(box, half, tube, corners);
        return Intersects(corners, tube.HalfLength + inflation,
            Math.Max(0, tube.InnerRadius - inflation), tube.OuterRadius + inflation);
    }

    /// <summary>Largest known shell expansion that remains disjoint, within
    /// MarginResolution of first intersection. A positive result is a conservative
    /// lower bound on Euclidean clearance because axial/radial expansion contains
    /// the shell's Euclidean dilation. Negative values describe shell erosion,
    /// not a minimum translation vector or a contact normal.</summary>
    public static double SignedMargin(Transform3D box, Vector3 half, TubeProxy tube)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        Prepare(box, half, tube, corners);
        var lower = -Math.Min(tube.HalfLength, ((double)tube.OuterRadius - tube.InnerRadius) * .5);
        double upper = 0;
        if (!Intersects(corners, tube.HalfLength, tube.InnerRadius, tube.OuterRadius))
        {
            lower = 0;
            upper = tube.InnerRadius + 1;
            foreach (var point in corners)
            {
                upper = Math.Max(upper, Math.Abs((double)point.X) - tube.HalfLength + 1);
                upper = Math.Max(upper, Math.Sqrt((double)point.Y * point.Y + (double)point.Z * point.Z) - tube.OuterRadius + 1);
            }
        }
        for (var iteration = 0; iteration < 192; iteration++)
        {
            if (upper - lower <= MarginResolution) return lower;
            var middle = lower + (upper - lower) * .5;
            if (middle == lower || middle == upper)
                throw new InvalidOperationException("Tube clearance cannot make representable progress.");
            if (Intersects(corners, tube.HalfLength + middle,
                Math.Max(0, tube.InnerRadius - middle), tube.OuterRadius + middle)) upper = middle;
            else lower = middle;
        }
        throw new InvalidOperationException("Tube clearance did not converge; clearance was not inferred.");
    }

    private static bool Intersects(ReadOnlySpan<Vector3> corners, double length, double inner, double outer)
    {
        if (length <= 0 || outer <= inner) return false; // Empty erosion, not a replacement geometry.
        Span<Point> points = stackalloc Point[32];
        var count = 0;
        foreach (var p in corners)
            if (Math.Abs((double)p.X) <= length) points[count++] = new(p.Y, p.Z);
        for (var i = 0; i < 8; i++)
        for (var dimension = 0; dimension < 3; dimension++)
        {
            var j = i ^ (1 << dimension);
            if (j <= i) continue;
            var a = corners[i]; var b = corners[j];
            var dx = (double)b.X - a.X;
            if (dx == 0) continue;
            for (var side = -1; side <= 1; side += 2)
            {
                var t = (side * length - a.X) / dx;
                if (t < 0 || t > 1) continue;
                points[count++] = new(a.Y + ((double)b.Y - a.Y) * t,
                    a.Z + ((double)b.Z - a.Z) * t);
            }
        }
        if (count == 0) return false;
        var sorted = points[..count];
        sorted.Sort();
        var unique = 1;
        for (var i = 1; i < count; i++)
            if (sorted[i] != sorted[unique - 1]) sorted[unique++] = sorted[i];
        sorted = sorted[..unique];

        double maximum = 0;
        foreach (var p in sorted) maximum = Math.Max(maximum, p.SquaredLength);
        if (maximum < inner * inner) return false;
        if (unique == 1) return maximum <= outer * outer;
        Span<Point> hull = stackalloc Point[64];
        var size = 0;
        for (var i = 0; i < unique; i++)
        {
            while (size >= 2 && Point.Cross(hull[size - 1] - hull[size - 2], sorted[i] - hull[size - 1]) <= 0) size--;
            hull[size++] = sorted[i];
        }
        var boundary = size + 1;
        for (var i = unique - 2; i >= 0; i--)
        {
            while (size >= boundary && Point.Cross(hull[size - 1] - hull[size - 2], sorted[i] - hull[size - 1]) <= 0) size--;
            hull[size++] = sorted[i];
        }
        size--; // Repeated first vertex.
        var containsOrigin = size >= 3;
        double minimum = double.PositiveInfinity;
        for (var i = 0; i < size; i++)
        {
            var a = hull[i]; var b = hull[(i + 1) % size]; var edge = b - a;
            if (Point.Cross(edge, new(-a.X, -a.Y)) < 0) containsOrigin = false;
            var t = Math.Clamp(-(a.X * edge.X + a.Y * edge.Y) / edge.SquaredLength, 0, 1);
            var closest = new Point(a.X + t * edge.X, a.Y + t * edge.Y);
            minimum = Math.Min(minimum, closest.SquaredLength);
        }
        return containsOrigin || minimum <= outer * outer;
    }

    private static void Prepare(Transform3D box, Vector3 half, TubeProxy tube, Span<Vector3> corners)
    {
        Rigid(box, nameof(box)); Rigid(tube.Pose, nameof(tube));
        if (!half.IsFinite() || half.X <= 0 || half.Y <= 0 || half.Z <= 0)
            throw new ArgumentOutOfRangeException(nameof(half));
        if (!float.IsFinite(tube.HalfLength) || tube.HalfLength <= 0 ||
            !float.IsFinite(tube.InnerRadius) || tube.InnerRadius <= 0 ||
            !float.IsFinite(tube.OuterRadius) || tube.OuterRadius <= tube.InnerRadius)
            throw new ArgumentOutOfRangeException(nameof(tube));
        var relative = tube.Pose.AffineInverse() * box;
        for (var i = 0; i < 8; i++)
        {
            var local = new Vector3((i & 1) == 0 ? -half.X : half.X,
                (i & 2) == 0 ? -half.Y : half.Y, (i & 4) == 0 ? -half.Z : half.Z);
            corners[i] = relative * local;
            if (!corners[i].IsFinite())
                throw new InvalidOperationException("Tube/box geometry exceeds representable world coordinates.");
        }
    }

    private static void Rigid(Transform3D pose, string name)
    {
        var b = pose.Basis;
        if (!pose.Origin.IsFinite() || !b.X.IsFinite() || !b.Y.IsFinite() || !b.Z.IsFinite() ||
            Math.Abs(b.X.LengthSquared() - 1) > .00001f ||
            Math.Abs(b.Y.LengthSquared() - 1) > .00001f ||
            Math.Abs(b.Z.LengthSquared() - 1) > .00001f ||
            Math.Abs(b.X.Dot(b.Y)) > .00001f || Math.Abs(b.X.Dot(b.Z)) > .00001f ||
            Math.Abs(b.Y.Dot(b.Z)) > .00001f || Math.Abs(b.Determinant() - 1) > .0001f)
            throw new ArgumentException("Expected proper rigid transform.", name);
    }
}
