using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Complete material containment in a finite cylindrical region, with independently authored boundary allowance.</summary>
public sealed class CylindricalContainmentQuery
{
    private readonly Stack<Cell> _pending = new();
    private readonly record struct Cell(float U0, float U1, float V0, float V1);

    public MaterialContainmentResult Contains(HollowPartner region, HollowPartner shape, float allowance)
    {
        if (region.Shape != ColliderShapeKind.Cylinder || !float.IsFinite(allowance) || allowance < 0f)
            throw new ArgumentException("Containment requires a finite cylindrical region and nonnegative allowance.");
        var high = Vector3.Dot(MaterialSupport.Point(shape, region.U1) - region.Position, region.U1);
        var low = Vector3.Dot(MaterialSupport.Point(shape, -region.U1) - region.Position, region.U1);
        if (high > region.Hy + allowance || low < -region.Hy - allowance) return MaterialContainmentResult.Outside;
        var limit = region.Radius + allowance;
        if (shape.Shape == ColliderShapeKind.Sphere)
            return Radial(shape.Position, region) + shape.Radius <= limit ? MaterialContainmentResult.Inside : MaterialContainmentResult.Outside;
        if (shape.Shape == ColliderShapeKind.Box)
        {
            for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                    for (var z = -1; z <= 1; z += 2)
                        if (Radial(shape.Position + x * shape.Hx * shape.U0 + y * shape.Hy * shape.U1 + z * shape.Hz * shape.U2, region) > limit)
                            return MaterialContainmentResult.Outside;
            return MaterialContainmentResult.Inside;
        }
        if (shape.U1 == region.U1 || shape.U1 == -region.U1)
        {
            // A complete coaxial circular section has an exact radial extremum. A
            // derivative interval cannot prove equality along its constant-radius rim.
            var outer = shape.Shape switch
            {
                ColliderShapeKind.Cylinder or ColliderShapeKind.Tube => shape.Radius,
                ColliderShapeKind.Frustum => MathF.Max(shape.Radius, shape.Hz),
                ColliderShapeKind.Torus when shape.Hz == ColliderDeclaration.FullSweep => shape.Radius + shape.Hy,
                _ => -1f
            };
            if (outer >= 0f)
                return Radial(shape.Position, region) + outer <= limit ? MaterialContainmentResult.Inside : MaterialContainmentResult.Outside;
        }
        return CurvedSurfaces(region, shape, limit);
    }

    private MaterialContainmentResult CurvedSurfaces(HollowPartner region, HollowPartner shape, float limit)
    {
        var pending = _pending;
        pending.Clear();
        var unresolved = false;
        for (var face = 0; face < MaterialSurface.Count(shape); face++)
        {
            var patch = new MaterialSurface(shape, face);
            var bound = patch.DerivativeBounds;
            pending.Push(new(0f, 1f, 0f, 1f));
            while (pending.TryPop(out var cell))
            {
                var u = .5f * (cell.U0 + cell.U1); var v = .5f * (cell.V0 + cell.V1);
                var ru = .5f * (cell.U1 - cell.U0) * bound.X; var rv = .5f * (cell.V1 - cell.V0) * bound.Y;
                var radial = Radial(patch.Point(u, v), region);
                if (!float.IsFinite(radial + ru + rv)) { unresolved = true; continue; }
                if (radial > limit) return MaterialContainmentResult.Outside;
                if (radial + ru + rv <= limit) continue;
                if (ru >= rv)
                {
                    if (u == cell.U0 || u == cell.U1) { unresolved = true; continue; }
                    pending.Push(cell with { U0 = u }); pending.Push(cell with { U1 = u });
                }
                else
                {
                    if (v == cell.V0 || v == cell.V1) { unresolved = true; continue; }
                    pending.Push(cell with { V0 = v }); pending.Push(cell with { V1 = v });
                }
            }
        }
        return unresolved ? MaterialContainmentResult.Unresolved : MaterialContainmentResult.Inside;
    }

    private static float Radial(Vector3 point, HollowPartner region)
    {
        var delta = point - region.Position;
        return (delta - Vector3.Dot(delta, region.U1) * region.U1).Length();
    }
}
