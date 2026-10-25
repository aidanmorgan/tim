using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

// ---- engine-hollow-shapes: the true hollow kinds (owner decision "true hollow shapes", 25 Oct 2026) ----
// A Tube, Frustum or Torus collider is read only through its posed HollowSolid (engine/gpu/HollowGeometry.cs): its exact signed
// distance field, its support point and its rim circles. Hollow colliders are static (admission), so each pair rule here meets a
// dynamic sphere, box or cylinder; one rule per pair.
internal static partial class WorkshopCollisionKernels
{
    internal static bool IsHollow(ShapeKind shape) => shape is ShapeKind.Tube or ShapeKind.Torus or ShapeKind.Frustum;

    /// <summary>The posed solid of a hollow collider (its lanes as admitted).</summary>
    internal static HollowSolid Hollow(SimCollider c) => new(c.Shape switch
    {
        ShapeKind.Tube => ColliderShapeKind.Tube,
        ShapeKind.Torus => ColliderShapeKind.Torus,
        _ => ColliderShapeKind.Frustum
    }, c.Position, c.U0, c.U1, c.U2, c.Radius, c.Hx, c.Hy, c.Hz);

    /// <summary>The radius of a ball about the collider's centre that contains it.</summary>
    internal static float HollowReach(SimCollider c) => Hollow(c).Reach;

    /// <summary>The collider's world bounds (no fattening). A tube or frustum is bounded by its outer cylinder, h |a_i| + r √(1 − a_i²)
    /// per world axis; a torus segment by its centreline arc's exact bounds (its endpoints and any axis extreme inside the sweep)
    /// widened by the tube's outer radius.</summary>
    internal static void HollowBounds(SimCollider c, out Vector3 min, out Vector3 max)
    {
        if (c.Shape != ShapeKind.Torus)
        {
            var r = c.Shape == ShapeKind.Frustum ? MathF.Max(c.Radius, c.Hz) : c.Radius;
            var e = new Vector3(Span(c.U1.X), Span(c.U1.Y), Span(c.U1.Z));
            min = c.Position - e;
            max = c.Position + e;
            return;

            float Span(float a) => c.Hy * MathF.Abs(a) + r * MathF.Sqrt(MathF.Max(0f, 1f - a * a));
        }
        var full = c.Hz == ColliderDeclaration.FullSweep;
        Span<float> lo = stackalloc float[3];
        Span<float> hi = stackalloc float[3];
        for (var i = 0; i < 3; i++)
        {
            var u0 = i == 0 ? c.U0.X : i == 1 ? c.U0.Y : c.U0.Z;
            var u2 = i == 0 ? c.U2.X : i == 1 ? c.U2.Y : c.U2.Z;
            var amplitude = c.Radius * MathF.Sqrt(u0 * u0 + u2 * u2);
            float top, bottom;
            if (full) { top = amplitude; bottom = -amplitude; }
            else
            {
                var start = c.Radius * u0;
                var end = c.Radius * (u0 * MathF.Cos(c.Hz) + u2 * MathF.Sin(c.Hz));
                top = MathF.Max(start, end);
                bottom = MathF.Min(start, end);
                // c_i(a) = R·A·cos(a − β): its maximum at β and its minimum at β + π, when inside the sweep.
                var beta = MathF.Atan2(u2, u0);
                if (HollowSolid.InSweep(beta, c.Hz)) top = amplitude;
                if (HollowSolid.InSweep(beta + MathF.PI, c.Hz)) bottom = -amplitude;
            }
            var centre = i == 0 ? c.Px : i == 1 ? c.Py : c.Pz;
            lo[i] = centre + bottom - c.Hy;
            hi[i] = centre + top + c.Hy;
        }
        min = new(lo[0], lo[1], lo[2]);
        max = new(hi[0], hi[1], hi[2]);
    }


    private static void CollideHollow(SimCollider partner, SimCollider collider, float margin, List<ContactPoint> contacts)
    {
        var kind = partner.Shape switch
        {
            ShapeKind.Sphere => ColliderShapeKind.Sphere,
            ShapeKind.Box => ColliderShapeKind.Box,
            ShapeKind.Cylinder => ColliderShapeKind.Cylinder,
            _ => throw new ArgumentException("Unsupported hollow partner.")
        };
        var shape = new HollowPartner(kind, partner.Position, partner.U0, partner.U1, partner.U2,
            partner.Radius, partner.Hx, partner.Hy, partner.Hz);
        Span<HollowContact> candidates = stackalloc HollowContact[HollowContactQuery.Capacity];
        var count = HollowContactQuery.Measure(shape, Hollow(collider), margin, candidates);
        var first = contacts.Count;
        for (var i = 0; i < count; i++)
        {
            var point = candidates[i];
            contacts.Add(kind == ColliderShapeKind.Sphere
                ? MakeContact(partner.Body, collider.Body, partner, collider, point.Normal, point.Point, point.Gap, point.Feature)
                : CylinderContact(partner, collider, point.Normal, point.Point, point.Gap, point.Feature,
                    point.Support == HollowContactSupport.Axis ? ContactSupport.Axis : ContactSupport.Material, ContactSupport.Material));
        }
        if (kind != ColliderShapeKind.Sphere) ReduceSideManifold(contacts, first);
    }

    /// <summary>A hollow point lying within this distance of another hollow collider's surface lies on their shared seam (m).</summary>
    internal const float SeamTolerance = 1e-3f;

    /// <summary>Internal-feature removal where hollow colliders meet (the enhanced internal-edge rule of modern engines, applied to the
    /// hollow kinds). Where two hollow colliders meet (snapped mouths, a bend's stub and arc, a collar torus whose inner equator lies
    /// on its pipe's bore), a feature of one lies on, or inside, the other's surface. A body rolling along that surface, sunk into it by
    /// its soft contact, meets the buried feature as a bump of that depth with a normal tilted against its motion, which brakes it and
    /// kicks it off the wall. So a contact of a body with one hollow collider is removed when the collider's nearest surface point to it
    /// lies on or inside another hollow collider the same body touches in this substep (within <see cref="SeamTolerance"/>) and its gap
    /// exceeds that collider's smallest gap to the body: the other collider's own nearer contact supports the body there. A rim with no
    /// hollow collider on it (a free pipe end) keeps its contact, so a ball still pivots over a free lip. Manifolds left empty are
    /// dropped and the contact list is rebuilt in manifold order.</summary>
    public static void RemoveInternalHollowEdges(List<ContactManifold> manifolds, List<ContactPoint> contacts)
    {
        var removed = false;
        for (var m = 0; m < manifolds.Count; m++)
        {
            var points = manifolds[m].Points;
            if (points.Count == 0) continue;
            var (hollow, body) = HollowSide(points[0]);
            if (hollow is null) continue;
            var solid = Hollow(hollow);
            for (var i = points.Count - 1; i >= 0; i--)
            {
                solid.Distance(points[i].Point, out var onHollow, out _, out _);
                for (var o = 0; o < manifolds.Count; o++)
                {
                    if (o == m || manifolds[o].Points.Count == 0) continue;
                    var (other, otherBody) = HollowSide(manifolds[o].Points[0]);
                    if (other is null || other == hollow || otherBody != body) continue;
                    var nearest = float.PositiveInfinity;
                    foreach (var point in manifolds[o].Points) nearest = MathF.Min(nearest, point.Gap);
                    if (!(points[i].Gap > nearest + 1e-6f) || Hollow(other).Distance(onHollow) > SeamTolerance) continue;
                    points.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
        }
        if (!removed) return;
        manifolds.RemoveAll(manifold => manifold.Points.Count == 0);
        contacts.Clear();
        foreach (var manifold in manifolds) contacts.AddRange(manifold.Points);

        // The hollow collider of a contact and the body on its other side (null when neither collider is hollow).
        static (SimCollider? Hollow, SimBody? Body) HollowSide(ContactPoint point) =>
            IsHollow(point.ColB.Shape) ? (point.ColB, point.A) : IsHollow(point.ColA.Shape) ? (point.ColA, point.B) : (null, null);
    }

}
