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
                var buried = IsInternalFeature(manifolds, m, current);
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

    private static bool IsInternalFeature(List<ContactManifold> manifolds, int m, ContactPoint current)
    {
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
        return buried;
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

    internal static HollowPartner Partner(SimCollider collider) => new(collider.Shape switch
    {
        ShapeKind.Plane => ColliderShapeKind.Plane,
        ShapeKind.Sphere => ColliderShapeKind.Sphere,
        ShapeKind.Box => ColliderShapeKind.Box,
        ShapeKind.Cylinder => ColliderShapeKind.Cylinder,
        ShapeKind.Tube => ColliderShapeKind.Tube,
        ShapeKind.Frustum => ColliderShapeKind.Frustum,
        ShapeKind.Torus => ColliderShapeKind.Torus,
        _ => throw new ArgumentException("A material query requires an admitted collider surface.")
    }, collider.Position, collider.U0, collider.U1, collider.U2, collider.Radius, collider.Hx, collider.Hy, collider.Hz);

}
