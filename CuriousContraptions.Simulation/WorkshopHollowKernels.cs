using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

// ---- engine-hollow-shapes: the true hollow kinds (owner decision "true hollow shapes", 25 Oct 2026) ----
// A Tube, Frustum or Torus collider is read only through its posed HollowSolid (engine/gpu/HollowGeometry.cs): its exact signed
// distance field, its support point and its rim circles. The query is independent of either body's motion mode.
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
            ShapeKind.Plane => ColliderShapeKind.Plane,
            ShapeKind.Tube => ColliderShapeKind.Tube,
            ShapeKind.Frustum => ColliderShapeKind.Frustum,
            ShapeKind.Torus => ColliderShapeKind.Torus,
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
            if (point.Support == HollowContactSupport.DistanceOnly) continue;
            contacts.Add(kind == ColliderShapeKind.Sphere
                ? MakeContact(partner.Body, collider.Body, partner, collider, point.Normal, point.Point, point.Gap, point.Feature)
                : CylinderContact(partner, collider, point.Normal, point.Point, point.Gap, point.Feature,
                    point.Support == HollowContactSupport.Axis ? ContactSupport.Axis : ContactSupport.Material, ContactSupport.Material));
        }
        if (kind != ColliderShapeKind.Sphere) ReduceSideManifold(contacts, first);
    }

    /// <summary>A hollow point lying within this distance of another hollow collider's surface lies on their shared seam (m).</summary>
    internal const float SeamTolerance = 1e-3f;


    /// <summary>A surface feature buried in another rigidly co-owned surface is not a boundary of their union.
    /// Static surfaces share the world frame, including snapped parts with different authored owners.
    /// Independent moving bodies never share this union. The nearer supporting contact must remain.</summary>
    public static void RemoveInternalFeatures(List<ContactManifold> manifolds, List<ContactPoint> contacts)
    {
        var removed = false;
        for (var m = 0; m < manifolds.Count; m++)
        {
            var points = manifolds[m].Points;
            for (var i = points.Count - 1; i >= 0; i--)
            {
                var current = points[i];
                var buried = false;
                for (var side = 0; side < 2 && !buried; side++)
                {
                    var surface = side == 0 ? current.ColA : current.ColB;
                    var partner = side == 0 ? current.B : current.A;
                    if (surface.Shape == ShapeKind.Plane) continue;
                    HollowContactQuery.SurfaceDistance(Partner(surface), current.Point, out var witness, out _);
                    for (var o = 0; o < manifolds.Count && !buried; o++)
                    {
                        if (o == m || manifolds[o].Points.Count == 0) continue;
                        var otherPoints = manifolds[o].Points;
                        var first = otherPoints[0];
                        var other = first.A == partner ? first.ColB : first.B == partner ? first.ColA : null;
                        if (other is null || other == surface || other.Shape == ShapeKind.Plane) continue;
                        if (other.Body != surface.Body &&
                            !(other.Body.Motion == RigidMotionKind.Static && surface.Body.Motion == RigidMotionKind.Static)) continue;
                        var nearest = float.PositiveInfinity;
                        foreach (var point in otherPoints) nearest = MathF.Min(nearest, point.Gap);
                        if (!(current.Gap > nearest + 1e-6f)) continue;
                        if (HollowContactQuery.SurfaceDistance(Partner(other), witness, out _, out _) > SeamTolerance) continue;
                        buried = true;
                    }
                }
                if (!buried) continue;
                points.RemoveAt(i);
                removed = true;
            }
        }
        if (!removed) return;
        manifolds.RemoveAll(manifold => manifold.Points.Count == 0);
        contacts.Clear();
        foreach (var manifold in manifolds) contacts.AddRange(manifold.Points);
    }

    /// <summary>One rigid pair's same-material witnesses share a coupled normal support set.</summary>
    public static void GroupRigidSupports(List<ContactManifold> manifolds, List<ContactPoint> contacts, List<ContactManifold> pool)
    {
        foreach (var point in contacts)
        {
            if (point.A.Id <= point.B.Id) continue;
            (point.A, point.B) = (point.B, point.A);
            (point.ColA, point.ColB) = (point.ColB, point.ColA);
            (point.SupportA, point.SupportB) = (point.SupportB, point.SupportA);
            point.Nx = -point.Nx; point.Ny = -point.Ny; point.Nz = -point.Nz;
        }
        manifolds.Clear();
        foreach (var point in contacts)
        {
            ContactManifold? match = null;
            foreach (var manifold in manifolds)
            {
                var first = manifold.Points[0];
                if (point.A != first.A || point.B != first.B ||
                    point.ColA.MaterialSlot != first.ColA.MaterialSlot || point.ColB.MaterialSlot != first.ColB.MaterialSlot) continue;
                match = manifold;
                break;
            }
            if (match is null)
            {
                while (pool.Count <= manifolds.Count) pool.Add(new ContactManifold());
                match = pool[manifolds.Count];
                match.Points.Clear();
                manifolds.Add(match);
            }
            match.Points.Add(point);
        }
        foreach (var manifold in manifolds)
        {
            var group = manifold.Points;
            group.Sort((a, b) =>
            {
                var x = a.Px.CompareTo(b.Px); if (x != 0) return x;
                var y = a.Py.CompareTo(b.Py); if (y != 0) return y;
                return a.Pz.CompareTo(b.Pz);
            });
        }
        contacts.Clear();
        foreach (var manifold in manifolds) contacts.AddRange(manifold.Points);
    }

    private static HollowPartner Partner(SimCollider collider) => new(collider.Shape switch
    {
        ShapeKind.Sphere => ColliderShapeKind.Sphere,
        ShapeKind.Box => ColliderShapeKind.Box,
        ShapeKind.Cylinder => ColliderShapeKind.Cylinder,
        ShapeKind.Tube => ColliderShapeKind.Tube,
        ShapeKind.Frustum => ColliderShapeKind.Frustum,
        ShapeKind.Torus => ColliderShapeKind.Torus,
        _ => throw new ArgumentException("A rigid union requires finite collider surfaces.")
    }, collider.Position, collider.U0, collider.U1, collider.U2, collider.Radius, collider.Hx, collider.Hy, collider.Hz);

}
