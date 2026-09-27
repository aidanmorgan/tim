using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Oriented box versus a hollow, finite frustum, without tessellation.
/// After axial clipping, the continuous function r - slope*x has an interval
/// image on the convex clipped box. Intersection requires that interval to
/// overlap the shell's radial-intercept interval.</summary>
public static class FrustumBoxIntersection
{
    public const double MarginResolution = .000003125;

    private readonly record struct Point(double X, double Y, double Z)
    {
        public double Value(double slope) => Math.Sqrt(Y * Y + Z * Z) - slope * X;
        public static Point operator +(Point a, Point b) => new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static Point operator -(Point a, Point b) => new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static Point operator *(Point a, double t) => new(a.X*t,a.Y*t,a.Z*t);
    }

    public static bool Intersects(Transform3D box, Vector3 half, FrustumProxy frustum, double inflation = 0)
    {
        if (!double.IsFinite(inflation)) throw new ArgumentOutOfRangeException(nameof(inflation));
        Span<Point> corners = stackalloc Point[8];
        var relative = Prepare(box,half,frustum,corners);
        return Intersects(corners,relative,half,frustum,inflation);
    }

    /// <summary>Known-disjoint expansion, within MarginResolution of first
    /// intersection. Positive values bound Euclidean clearance from below:
    /// axial bounds expand by delta, while r-slope*x bounds expand by
    /// delta*sqrt(1+slope²), their Lipschitz constant. Negative values describe
    /// erosion, not a contact normal or minimum translation vector.</summary>
    public static double SignedMargin(Transform3D box, Vector3 half, FrustumProxy frustum)
    {
        Span<Point> corners = stackalloc Point[8];
        var relative = Prepare(box,half,frustum,corners);
        var slope = ((double)frustum.OutletRadius-frustum.InletRadius)/(2d*frustum.HalfLength);
        var scale = Math.Sqrt(1+slope*slope);
        var intercept = ((double)frustum.InletRadius+frustum.OutletRadius)*.5;
        var lower = -Math.Min(frustum.HalfLength,frustum.Thickness/(2*scale));
        double upper = 0;
        if (!Intersects(corners,relative,half,frustum,0))
        {
            lower = 0;
            upper = 1;
            foreach (var p in corners)
            {
                upper = Math.Max(upper,Math.Abs(p.X)-frustum.HalfLength+1);
                upper = Math.Max(upper,Math.Abs(p.Value(slope)-intercept)/scale+1);
            }
        }
        for (var iteration=0;iteration<192;iteration++)
        {
            if (upper-lower <= MarginResolution) return lower;
            var middle = lower+(upper-lower)*.5;
            if (middle == lower || middle == upper)
                throw new InvalidOperationException("Frustum clearance cannot make representable progress.");
            if (Intersects(corners,relative,half,frustum,middle)) upper = middle;
            else lower = middle;
        }
        throw new InvalidOperationException("Frustum clearance did not converge; clearance was not inferred.");
    }

    private static bool Intersects(ReadOnlySpan<Point> corners, Transform3D relative,
        Vector3 half, FrustumProxy frustum, double inflation)
    {
        var slope = ((double)frustum.OutletRadius-frustum.InletRadius)/(2d*frustum.HalfLength);
        var radialExpansion = inflation*Math.Sqrt(1+slope*slope);
        var length = frustum.HalfLength+inflation;
        var inner = ((double)frustum.InletRadius+frustum.OutletRadius)*.5-radialExpansion;
        var outer = ((double)frustum.InletRadius+frustum.OutletRadius)*.5+frustum.Thickness+radialExpansion;
        if (length <= 0 || outer <= inner) return false; // Empty erosion.

        Span<Point> clipped = stackalloc Point[32];
        var count = 0;
        foreach (var p in corners)
            if (Math.Abs(p.X) <= length) clipped[count++] = p;
        for (var i=0;i<8;i++)
        for (var dimension=0;dimension<3;dimension++)
        {
            var j = i^(1<<dimension);
            if (j<=i) continue;
            var a = corners[i]; var edge = corners[j]-a;
            if (edge.X == 0) continue;
            for (var side=-1;side<=1;side+=2)
            {
                var t = (side*length-a.X)/edge.X;
                if (t>=0 && t<=1) clipped[count++] = a+edge*t;
            }
        }
        if (count == 0) return false;
        double maximum = double.NegativeInfinity, minimum = double.PositiveInfinity;
        for (var i=0;i<count;i++)
        {
            var value = clipped[i].Value(slope);
            maximum = Math.Max(maximum,value);
            minimum = Math.Min(minimum,value);
        }
        // A convex function attains its maximum at a polytope vertex.
        if (maximum < inner) return false;
        if (minimum <= outer) return true;

        // A smooth face-interior minimum extends along the cone generator
        // (its Hessian has that null direction) to an edge or the axis.
        // All vertex pairs include every clipped edge. Extra chords lie
        // inside the polytope and therefore cannot produce a false minimum.
        for (var i=0;i<count;i++)
        for (var j=i+1;j<count;j++)
            if (SegmentMinimum(clipped[i],clipped[j],slope) <= outer) return true;

        // The nonsmooth radial-axis minimum can lie inside a face or volume.
        // Clip that axis against the original box and the frustum axial slab.
        var inverse = relative.AffineInverse();
        var origin = inverse.Origin;
        var direction = inverse.Basis.X;
        var lo = -length; var hi = length;
        for (var dimension=0;dimension<3;dimension++)
        {
            var o = (double)origin[dimension]; var d = (double)direction[dimension];
            var h = (double)half[dimension];
            if (d == 0)
            {
                if (Math.Abs(o)>h) return false;
                continue;
            }
            var a = (-h-o)/d; var b = (h-o)/d;
            lo = Math.Max(lo,Math.Min(a,b)); hi = Math.Min(hi,Math.Max(a,b));
            if (lo>hi) return false;
        }
        return Math.Min(-slope*lo,-slope*hi) <= outer;
    }

    private static double SegmentMinimum(Point a, Point b, double slope)
    {
        var edge = b-a;
        var radialSpeedSquared = edge.Y*edge.Y+edge.Z*edge.Z;
        var axialSlope = slope*edge.X;
        var minimum = Math.Min(a.Value(slope),b.Value(slope));
        if (radialSpeedSquared == 0 || axialSlope*axialSlope >= radialSpeedSquared) return minimum;
        var t0 = -(a.Y*edge.Y+a.Z*edge.Z)/radialSpeedSquared;
        var closest = a+edge*t0;
        var perpendicularSquared = closest.Y*closest.Y+closest.Z*closest.Z;
        var t = Math.Clamp(t0+axialSlope*Math.Sqrt(perpendicularSquared/
            (radialSpeedSquared*(radialSpeedSquared-axialSlope*axialSlope))),0,1);
        return Math.Min(minimum,(a+edge*t).Value(slope));
    }

    private static Transform3D Prepare(Transform3D box, Vector3 half, FrustumProxy frustum, Span<Point> corners)
    {
        Rigid(box,nameof(box)); Rigid(frustum.Pose,nameof(frustum));
        if (!half.IsFinite() || half.X<=0 || half.Y<=0 || half.Z<=0)
            throw new ArgumentOutOfRangeException(nameof(half));
        if (!float.IsFinite(frustum.HalfLength) || frustum.HalfLength<=0 ||
            !float.IsFinite(frustum.InletRadius) || frustum.InletRadius<=0 ||
            !float.IsFinite(frustum.OutletRadius) || frustum.OutletRadius<=0 ||
            !float.IsFinite(frustum.Thickness) || frustum.Thickness<=0)
            throw new ArgumentOutOfRangeException(nameof(frustum));
        var relative = frustum.Pose.AffineInverse()*box;
        for (var i=0;i<8;i++)
        {
            var p = relative*new Vector3((i&1)==0?-half.X:half.X,
                (i&2)==0?-half.Y:half.Y,(i&4)==0?-half.Z:half.Z);
            if (!p.IsFinite()) throw new InvalidOperationException("Frustum/box coordinates exceed representable geometry.");
            corners[i] = new(p.X,p.Y,p.Z);
        }
        return relative;
    }

    private static void Rigid(Transform3D pose, string name)
    {
        var b = pose.Basis;
        if (!pose.Origin.IsFinite() || !b.X.IsFinite() || !b.Y.IsFinite() || !b.Z.IsFinite() ||
            Math.Abs(b.X.LengthSquared()-1)>.00001f || Math.Abs(b.Y.LengthSquared()-1)>.00001f ||
            Math.Abs(b.Z.LengthSquared()-1)>.00001f || Math.Abs(b.X.Dot(b.Y))>.00001f ||
            Math.Abs(b.X.Dot(b.Z))>.00001f || Math.Abs(b.Y.Dot(b.Z))>.00001f ||
            Math.Abs(b.Determinant()-1)>.0001f)
            throw new ArgumentException("Expected proper rigid transform.",name);
    }
}
