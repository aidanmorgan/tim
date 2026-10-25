using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>A posed convex partner for the shared hollow contact query.</summary>
public readonly record struct HollowPartner(ColliderShapeKind Shape, Vector3 Position, Vector3 U0, Vector3 U1, Vector3 U2,
    float Radius, float Hx, float Hy, float Hz);
public enum HollowContactSupport : byte { Material, Axis }
public readonly record struct HollowContact(Vector3 Normal, Vector3 Point, float Gap, int Feature, HollowContactSupport Support);

/// <summary>Scene-independent f32 hollow feature measurements. Simulation, placement
/// and admission all consume these same distances and normals.</summary>
public static class HollowContactQuery
{
    // Box: 8 vertices + 12 edge minima + 6 faces * (4 crest + 16 rim + 2 generator) = 152.
    // Cylinder: 2 rims + 32 cap samples + 1 axis + 2 * (4 crest + 16 rim) = 75.
    // Sphere: 1 closest surface + 4 end rims * 4 points = 17 (full revolutions: 4).
    public const int Capacity = 152;
    public const float PlacementSlop = 1e-4f;
    private const float EdgeFaceAlignment = .98f, SideAlignment = .19899749f;
    private const int RimSamples = 16, RimSteps = 16;
    private const float GoldenSection = .618034f;
    private interface IDistanceCurve { float Distance(float s); }
    private ref struct CandidateBuffer
    {
        private readonly Span<HollowContact> data;
        public CandidateBuffer(Span<HollowContact> storage) { data = storage; Count = 0; }
        public int Count { get; private set; }
        public ref HollowContact this[int index] => ref data[index];
        public void Add(HollowContact contact) { data[Count++] = contact; }
    }

    public static int Measure(HollowPartner partner, HollowSolid hollow, float margin, Span<HollowContact> output)
    {
        if (output.Length < Capacity) throw new ArgumentException("Hollow query scratch is too small.", nameof(output));
        var points = new CandidateBuffer(output);
        switch (partner.Shape)
        {
            case ColliderShapeKind.Sphere: Sphere(partner, hollow, margin, ref points); break;
            case ColliderShapeKind.Box: CollideBoxHollow(partner, hollow, margin, ref points); break;
            case ColliderShapeKind.Cylinder: CollideCylinderHollow(partner, hollow, margin, ref points); break;
            default: throw new ArgumentException("A hollow partner must be a sphere, box or cylinder.", nameof(partner));
        }
        return points.Count;
    }

    public static bool Overlaps(HollowPartner partner, HollowSolid hollow, float slop)
    {
        Span<HollowContact> contacts = stackalloc HollowContact[Capacity];
        var count = Measure(partner, hollow, 0f, contacts);
        for (var i = 0; i < count; i++) if (contacts[i].Gap < -slop) return true;
        return false;
    }

    private static void Sphere(HollowPartner sphere, HollowSolid hollow, float margin, ref CandidateBuffer points)
    {
        var distance = hollow.Distance(sphere.Position, out var surface, out var outward, out var on);
        var gap = distance - sphere.Radius;

        if (hollow.Kind == ColliderShapeKind.Torus && !hollow.Full)
        {
            // A bare segment mouth is an annulus. At its axis an oversized sphere
            // meets a circle of equally close points, not one arbitrary meridian.
            Span<(Vector3 Centre, Vector3 Normal, float Radius)> rims = stackalloc (Vector3, Vector3, float)[4];
            var count = hollow.Rims(rims);
            for (var r = 0; r < count; r++)
            {
                var (centre, axis, radius) = rims[r];
                var ownRim = RadialUnit(hollow.Y, axis, hollow.X);
                var across = Vector3.Cross(axis, ownRim);
                for (var k = 0; k < 4; k++)
                {
                    var e = k switch { 0 => ownRim, 1 => across, 2 => -ownRim, _ => -across };
                    var point = centre + radius * e;
                    var delta = sphere.Position - point;
                    var length = delta.Length();
                    if (length <= 1e-9f) continue;
                    AddHollowCandidate(ref points, 0, delta / length, point, length - sphere.Radius,
                        Feature(HollowFeature.RimPoint, 4 * r + k), HollowContactSupport.Material, margin);
                }
            }
            AddHollowCandidate(ref points, 0, outward, surface, gap,
                Feature(HollowFeature.Surface, (int)on), HollowContactSupport.Material, margin);
            return;
        }
        if (gap <= margin) points.Add(new(outward, surface, gap, Feature(HollowFeature.Surface, (int)on), HollowContactSupport.Material));
        var own = hollow.RadialUnit(sphere.Position - hollow.Position, hollow.X);
        var quarter = Vector3.Cross(hollow.Y, own);
        for (var k = 1; k < 4; k++)
        {
            var e = k switch { 1 => quarter, 2 => -own, _ => -quarter };
            var meridian = hollow.MeridianDistance(sphere.Position, e, out var point, out var normal) - sphere.Radius;
            if (meridian <= margin) points.Add(new(normal, point, meridian, Feature(HollowFeature.RimPoint, k), HollowContactSupport.Material));
        }
    }

    private static Vector3 BoxVertex(HollowPartner box, float x, float y, float z) =>
        box.Position + x * box.Hx * box.U0 + y * box.Hy * box.U1 + z * box.Hz * box.U2;

    private static Vector3 RadialUnit(Vector3 v, Vector3 axis, Vector3 reference)
    {
        var radial = v - Vector3.Dot(v, axis) * axis;
        if (!(radial.Length() > 1e-3f * v.Length())) radial = reference - Vector3.Dot(reference, axis) * axis;
        radial -= Vector3.Dot(radial, axis) * axis;
        var length = radial.Length();
        return length > 1e-6f ? radial / length : reference;
    }

    private static void ClipSlab(float p, float d, float limit, ref float t0, ref float t1)
    {
        if (MathF.Abs(d) < 1e-12f)
        {
            if (MathF.Abs(p) > limit) { t0 = 1f; t1 = 0f; }
            return;
        }
        var ta = (-limit - p) / d;
        var tb = (limit - p) / d;
        if (ta > tb) (ta, tb) = (tb, ta);
        t0 = MathF.Max(t0, ta);
        t1 = MathF.Min(t1, tb);
    }


    private enum HollowFeature
    {
        Surface = 17,
        PartnerVertex = 18,
        PartnerEdge = 19,
        PartnerRim = 20,
        PartnerSide = 21,
        Support = 22,
        Generator = 23,
        RimPoint = 24
    }

    private static int Feature(HollowFeature kind, int detail) => ((int)kind << 12) | detail;

    private readonly struct HollowCurve(HollowSolid hollow, Vector3 start, Vector3 length) : IDistanceCurve
    {
        public float Distance(float s) => hollow.Distance(start + s * length);
    }

    private readonly struct HollowRimCurve(HollowSolid hollow, Vector3 centre, Vector3 u, Vector3 v, float radius) : IDistanceCurve
    {
        public float Distance(float s) => hollow.Distance(centre + radius * (MathF.Cos(s) * u + MathF.Sin(s) * v));
    }

    private static float CylinderDistance(HollowPartner c, Vector3 p, out Vector3 surface, out Vector3 outward)
    {
        var a = c.U1;
        var d = p - c.Position;
        var y = Vector3.Dot(d, a);
        var q = d - y * a;
        var rho = q.Length();
        var radial = rho > 1e-6f ? q / rho : c.U0;
        var dy = MathF.Abs(y) - c.Hy;
        var dr = rho - c.Radius;
        var cap = y >= 0f ? a : -a;
        if (dy > 0f || dr > 0f)
        {
            surface = c.Position + Math.Clamp(y, -c.Hy, c.Hy) * a + MathF.Min(rho, c.Radius) * radial;
            var diff = p - surface;
            var dist = diff.Length();
            outward = dist > 1e-9f ? diff / dist : dr > dy ? radial : cap;
            return dist;
        }
        if (dr > dy)
        {
            surface = c.Position + y * a + c.Radius * radial;
            outward = radial;
            return dr;
        }
        surface = p + (-dy) * cap;
        outward = cap;
        return dy;
    }

    private static float GoldenMinimum<T>(in T curve, float lo, float hi, int steps) where T : struct, IDistanceCurve
    {
        var x1 = hi - GoldenSection * (hi - lo);
        var x2 = lo + GoldenSection * (hi - lo);
        var f1 = curve.Distance(x1);
        var f2 = curve.Distance(x2);
        for (var step = 0; step < steps; step++)
        {
            if (f1 <= f2)
            {
                hi = x2; x2 = x1; f2 = f1;
                x1 = hi - GoldenSection * (hi - lo);
                f1 = curve.Distance(x1);
            }
            else
            {
                lo = x1; x1 = x2; f1 = f2;
                x2 = lo + GoldenSection * (hi - lo);
                f2 = curve.Distance(x2);
            }
        }
        return f1 <= f2 ? x1 : x2;
    }

    // ---- a box or cylinder (A) against a hollow collider (B) ----
    // The contact set is assembled from two kinds of candidate, each measured exactly:
    // - the partner's features against the hollow's signed distance: a box's vertices and its edges' minima, a cylinder's two rims'
    //   minima and its axis's interior minimum less its radius (there the distance's gradient is perpendicular to the axis, so the
    //   nearest hollow point lies radially off it and the side's generator meets it);
    // - the hollow's features against the partner's signed distance: the hollow's support point toward each partner face (a box face, a
    //   cylinder cap), a tube's outer generator clipped to a box face it lies along, and each rim circle's deepest point toward a face
    //   (four points of a circle lying in the face's plane).
    // Candidates within the margin are kept once each (points within 1 mm merge to the deeper) and reduced to four by the side rule: the
    // deepest, the farthest-apart pair, then the deepest of the rest. Every count is fixed: no convergence test.

    private const int HollowSegmentSamples = 8;
    private const int HollowSegmentSteps = 12;
    private const float HollowMerge = 1e-3f;

    /// <summary>The parameter in [lo, hi] minimising a curve that need not be unimodal (a segment may cross a bore): the best of fixed
    /// samples, refined in its bracket by golden-section steps.</summary>
    private static float BracketedMinimum<T>(in T curve, int samples, int steps, float lo, float hi) where T : struct, IDistanceCurve
    {
        var step = (hi - lo) / samples;
        var best = lo;
        var bestDistance = float.PositiveInfinity;
        for (var k = 0; k <= samples; k++)
        {
            var s = lo + k * step;
            var distance = curve.Distance(s);
            if (distance < bestDistance) { bestDistance = distance; best = s; }
        }
        return GoldenMinimum(in curve, MathF.Max(lo, best - step), MathF.Min(hi, best + step), steps);
    }

    private static void AddHollowCandidate(ref CandidateBuffer points, int first, Vector3 n,
        Vector3 p, float gap, int feature, HollowContactSupport supportA, float margin)
    {
        if (!(gap <= margin) || !float.IsFinite(gap)) return;
        for (var i = first; i < points.Count; i++)
        {
            if ((points[i].Point - p).LengthSquared() > HollowMerge * HollowMerge) continue;
            if (gap < points[i].Gap) points[i] = new HollowContact(n, p, gap, feature, supportA);
            return;
        }
        points.Add(new HollowContact(n, p, gap, feature, supportA));
    }

    /// <summary>The signed distance from p to the box, its closest surface point and the unit direction leaving the box there.</summary>
    private static float BoxDistance(HollowPartner box, Vector3 p, out Vector3 surface, out Vector3 outward)
    {
        var d = p - box.Position;
        ReadOnlySpan<Vector3> axes = [box.U0, box.U1, box.U2];
        ReadOnlySpan<float> extents = [box.Hx, box.Hy, box.Hz];
        var clamped = box.Position;
        var deepest = float.NegativeInfinity;
        var exit = box.U0;
        var outside = false;
        for (var i = 0; i < 3; i++)
        {
            var l = Vector3.Dot(d, axes[i]);
            clamped += Math.Clamp(l, -extents[i], extents[i]) * axes[i];
            var q = MathF.Abs(l) - extents[i];
            if (q > 0f) outside = true;
            if (q > deepest) { deepest = q; exit = l >= 0f ? axes[i] : -axes[i]; }
        }
        if (outside)
        {
            surface = clamped;
            var diff = p - clamped;
            var distance = diff.Length();
            outward = distance > 1e-9f ? diff / distance : exit;
            return distance;
        }
        outward = exit;
        surface = p - deepest * exit;
        return deepest;
    }

    /// <summary>A hollow feature point measured against the partner's own signed distance: the contact normal leaves the hollow into
    /// the partner (minus the partner's outward direction there); a point on a cylinder's curved side rolls on its axis support.</summary>
    private static void AddPartnerMeasured(ref CandidateBuffer points, int first, HollowPartner partner, Vector3 onHollow,
        int feature, float margin)
    {
        float gap;
        Vector3 surface, outward;
        var support = HollowContactSupport.Material;
        if (partner.Shape == ColliderShapeKind.Box) gap = BoxDistance(partner, onHollow, out surface, out outward);
        else
        {
            gap = CylinderDistance(partner, onHollow, out surface, out outward);
            if (MathF.Abs(Vector3.Dot(outward, partner.U1)) <= SideAlignment) support = HollowContactSupport.Axis;
        }
        AddHollowCandidate(ref points, first, -outward, (onHollow + surface) * 0.5f, gap, feature, support, margin);
    }

    /// <summary>A partner point (an axis point when <paramref name="lessRadius"/> is the cylinder's radius) measured against the
    /// hollow's signed distance.</summary>
    private static void AddHollowMeasured(ref CandidateBuffer points, int first, in HollowSolid hollow, Vector3 onPartner, float lessRadius, int feature, HollowContactSupport supportA, float margin)
    {
        var distance = hollow.Distance(onPartner, out var surface, out var outward, out _);
        AddHollowCandidate(ref points, first, outward, (onPartner - lessRadius * outward + surface) * 0.5f,
            distance - lessRadius, feature, supportA, margin);
    }

    /// <summary>A partner face (centre, outward unit normal) against the hollow's support point toward it and each rim circle's deepest
    /// point toward it (four points of a circle lying in the face's plane), all measured by the partner's signed distance.</summary>
    private static void HollowFeaturesOnFace(ref CandidateBuffer points, int first, HollowPartner partner, HollowSolid hollowCollider,
        in HollowSolid hollow, Vector3 faceCentre, Vector3 faceNormal, int faceIndex,
        scoped ReadOnlySpan<(Vector3 Centre, Vector3 Normal, float Radius)> rims, float margin)
    {
        if (hollowCollider.Kind == ColliderShapeKind.Torus && MathF.Abs(Vector3.Dot(faceNormal, hollow.Y)) >= EdgeFaceAlignment)
        {
            // A face lying along a torus's plane meets its whole crest circle: four points of it (a quarter turn apart from the point
            // under the face's centre, or spread over a segment's sweep), each the centreline point offset by r_o toward the face.
            var offset = faceCentre - hollow.Position;
            var under = MathF.Atan2(Vector3.Dot(offset, hollow.Z), Vector3.Dot(offset, hollow.X));
            for (var k = 0; k < 4; k++)
            {
                var angle = hollow.Full ? under + k * MathF.PI / 2 : k * hollow.LaneZ / 3;
                var radial = MathF.Cos(angle) * hollow.X + MathF.Sin(angle) * hollow.Z;
                var projected = Vector3.Dot(faceNormal, radial) * radial + Vector3.Dot(faceNormal, hollow.Y) * hollow.Y;
                AddPartnerMeasured(ref points, first, partner,
                    hollow.Position + hollow.Radius * radial - hollow.LaneY * Vector3.Normalize(projected),
                    Feature(HollowFeature.Support, (faceIndex << 2) | k), margin);
            }
        }
        else
            AddPartnerMeasured(ref points, first, partner, hollow.SupportNear(-faceNormal, faceCentre),
                Feature(HollowFeature.Support, faceIndex << 2), margin);
        for (var r = 0; r < rims.Length; r++)
        {
            var (centre, normal, radius) = rims[r];
            if (MathF.Abs(Vector3.Dot(normal, faceNormal)) >= EdgeFaceAlignment)
            {
                var reference = RadialUnit(faceCentre - centre, normal, RadialUnit(Vector3.UnitX, normal, Vector3.UnitZ));
                var across = Vector3.Cross(normal, reference);
                for (var k = 0; k < 4; k++)
                    AddPartnerMeasured(ref points, first, partner,
                        centre + radius * (k switch { 0 => reference, 1 => across, 2 => -reference, _ => -across }),
                        Feature(HollowFeature.RimPoint, (faceIndex << 6) | (r << 2) | k), margin);
            }
            else
                AddPartnerMeasured(ref points, first, partner, centre + radius * RadialUnit(-faceNormal, normal, Vector3.Zero),
                    Feature(HollowFeature.RimPoint, (faceIndex << 6) | (r << 2)), margin);
        }
    }

    /// <summary>A box (A) against a hollow collider (B): the box's vertices and edge minima against the hollow's signed distance, and
    /// the hollow's support, generator and rim points toward each box face against the box's.</summary>
    private static void CollideBoxHollow(HollowPartner box, HollowSolid hollowCollider, float margin, ref CandidateBuffer outList)
    {
        var hollow = hollowCollider;
        var reach = hollow.Reach + MathF.Sqrt(box.Hx * box.Hx + box.Hy * box.Hy + box.Hz * box.Hz) + margin;
        if ((box.Position - hollow.Position).LengthSquared() > reach * reach) return;
        var first = outList.Count;

        Span<Vector3> vertices = stackalloc Vector3[8];
        for (var v = 0; v < 8; v++)
        {
            vertices[v] = BoxVertex(box, (v & 1) != 0 ? 1f : -1f, (v & 2) != 0 ? 1f : -1f, (v & 4) != 0 ? 1f : -1f);
            AddHollowMeasured(ref outList, first, hollow, vertices[v], 0f, Feature(HollowFeature.PartnerVertex, v),
                HollowContactSupport.Material, margin);
        }
        var edge = 0;
        for (var v = 0; v < 8; v++)
            for (var bit = 1; bit < 8; bit <<= 1)
            {
                if ((v & bit) != 0) continue;
                var start = vertices[v];
                var length = vertices[v | bit] - start;
                var s = BracketedMinimum(new HollowCurve(hollow, start, length), HollowSegmentSamples, HollowSegmentSteps, 0f, 1f);
                if (s > 1e-3f && s < 1f - 1e-3f)
                    AddHollowMeasured(ref outList, first, hollow, start + s * length, 0f,
                        Feature(HollowFeature.PartnerEdge, edge), HollowContactSupport.Material, margin);
                edge++;
            }

        Span<(Vector3, Vector3, float)> rims = stackalloc (Vector3, Vector3, float)[4];
        var rimCount = hollow.Rims(rims);
        ReadOnlySpan<Vector3> axes = [box.U0, box.U1, box.U2];
        ReadOnlySpan<float> extents = [box.Hx, box.Hy, box.Hz];
        for (var i = 0; i < 3; i++)
            for (var side = 0; side < 2; side++)
            {
                var faceNormal = (side == 0 ? 1f : -1f) * axes[i];
                var faceCentre = box.Position + extents[i] * faceNormal;
                var faceIndex = 2 * i + side;
                HollowFeaturesOnFace(ref outList, first, box, hollowCollider, hollow, faceCentre, faceNormal, faceIndex, rims[..rimCount], margin);
                // A tube lying along the face meets it on a generator: its two ends clipped to the face.
                if (hollowCollider.Kind != ColliderShapeKind.Tube || MathF.Abs(Vector3.Dot(faceNormal, hollow.Y)) > SideAlignment) continue;
                var radial = RadialUnit(-faceNormal, hollow.Y, hollow.X);
                var start = hollow.Position - hollow.LaneY * hollow.Y + hollow.Radius * radial;
                var length = 2f * hollow.LaneY * hollow.Y;
                float t0 = 0f, t1 = 1f;
                for (var k = 1; k < 3; k++)
                {
                    var axis = axes[(i + k) % 3];
                    ClipSlab(Vector3.Dot(start - faceCentre, axis), Vector3.Dot(length, axis), extents[(i + k) % 3], ref t0, ref t1);
                }
                if (t0 > t1) continue;
                AddPartnerMeasured(ref outList, first, box, start + t0 * length, Feature(HollowFeature.Generator, 2 * faceIndex), margin);
                AddPartnerMeasured(ref outList, first, box, start + t1 * length, Feature(HollowFeature.Generator, 2 * faceIndex + 1), margin);
            }

    }

    /// <summary>A cylinder (A) against a hollow collider (B): each rim's nearest point and the axis's interior minimum (a side contact
    /// on the axis support) against the hollow's signed distance, and the hollow's support and rim points toward each cap against the
    /// cylinder's.</summary>
    private static void CollideCylinderHollow(HollowPartner cyl, HollowSolid hollowCollider, float margin, ref CandidateBuffer outList)
    {
        var hollow = hollowCollider;
        var reach = hollow.Reach + MathF.Sqrt(cyl.Radius * cyl.Radius + cyl.Hy * cyl.Hy) + margin;
        if ((cyl.Position - hollow.Position).LengthSquared() > reach * reach) return;
        var first = outList.Count;
        var a = cyl.U1;

        const float step = 2f * MathF.PI / RimSamples;
        Span<float> rimGap = stackalloc float[2];
        for (var s = 0; s < 2; s++)
        {
            var centre = cyl.Position + (s == 0 ? cyl.Hy : -cyl.Hy) * a;
            var curve = new HollowRimCurve(hollow, centre, cyl.U0, cyl.U2, cyl.Radius);
            var bestAngle = 0f;
            var bestDistance = float.PositiveInfinity;
            for (var k = 0; k < RimSamples; k++)
            {
                var distance = curve.Distance(k * step);
                if (distance < bestDistance) { bestDistance = distance; bestAngle = k * step; }
            }
            var angle = GoldenMinimum(in curve, bestAngle - step, bestAngle + step, RimSteps);
            var onRim = centre + cyl.Radius * (MathF.Cos(angle) * cyl.U0 + MathF.Sin(angle) * cyl.U2);
            rimGap[s] = hollow.Distance(onRim, out _, out var normal, out _);
            // A rim point whose normal lies within the side tolerance of the axis's perpendicular plane is an end of the side's
            // generator (as on the bench): it rolls on the axis support.
            // A cap facing a flat hollow end needs an area of support. Its fixed
            // features also retain their identities under tiny resting tilts.
            var capSamples = 0;
            for (var k = 0; k < RimSamples; k++)
            {
                var sample = centre + cyl.Radius * (MathF.Cos(k * step) * cyl.U0 + MathF.Sin(k * step) * cyl.U2);
                var sampleGap = hollow.Distance(sample, out _, out var sampleNormal, out _);
                if (sampleGap > margin || MathF.Abs(Vector3.Dot(sampleNormal, a)) < EdgeFaceAlignment) continue;
                AddHollowMeasured(ref outList, first, hollow, sample, 0f,
                    Feature(HollowFeature.PartnerRim, 2 + s * RimSamples + k), HollowContactSupport.Material, margin);
                capSamples++;
            }
            // When the whole cap rim is present the minimum is redundant; on a
            // flat face its arbitrary angle moves while its cached identity does
            // not. Partial rims and side generators still need their minimum.
            if (capSamples < RimSamples)
                AddHollowMeasured(ref outList, first, hollow, onRim, 0f, Feature(HollowFeature.PartnerRim, s),
                    MathF.Abs(Vector3.Dot(normal, a)) <= SideAlignment ? HollowContactSupport.Axis : HollowContactSupport.Material, margin);

        }

        var axisStart = cyl.Position - cyl.Hy * a;
        var axisLength = 2f * cyl.Hy * a;
        var along = BracketedMinimum(new HollowCurve(hollow, axisStart, axisLength), HollowSegmentSamples, HollowSegmentSteps, 0f, 1f);
        if (along > 1e-3f && along < 1f - 1e-3f)
        {
            var onAxis = axisStart + along * axisLength;
            var middle = hollow.Distance(onAxis, out _, out var outward, out _) - cyl.Radius;
            // The side meets the hollow between its rims when the nearest hollow point lies radially off the axis (within the side
            // tolerance) and nearer than both rims' points by more than the merge distance; along a side lying parallel to the
            // hollow's wall the rims' points already hold the line (its interior minimum is then flat and arbitrary).
            if (MathF.Abs(Vector3.Dot(outward, a)) <= SideAlignment && middle < MathF.Min(rimGap[0], rimGap[1]) - HollowMerge)
                AddHollowMeasured(ref outList, first, hollow, onAxis, cyl.Radius, Feature(HollowFeature.PartnerSide, 0),
                    HollowContactSupport.Axis, margin);
        }

        Span<(Vector3, Vector3, float)> rims = stackalloc (Vector3, Vector3, float)[4];
        var rimCount = hollow.Rims(rims);
        for (var s = 0; s < 2; s++)
        {
            var capNormal = (s == 0 ? 1f : -1f) * a;
            HollowFeaturesOnFace(ref outList, first, cyl, hollowCollider, hollow, cyl.Position + cyl.Hy * capNormal, capNormal, s,
                rims[..rimCount], margin);
        }

    }
}
