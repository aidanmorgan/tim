using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;


/// <summary>Continuous translation of actual material, including hollow surfaces and nested solids.
/// Interval radii use analytic patch derivative bounds and the signed distance field's unit Lipschitz bound.
/// An interval is discarded only when every point is separated by the shared contact margin.</summary>
public sealed class MaterialSweepQuery
{
    private readonly Stack<Cell> _pending = new();
    private readonly record struct Cell(float U0, float U1, float V0, float V1, float T0, float T1);

    public MaterialQueryResult Sweep(HollowPartner moving, HollowPartner obstacle, Vector3 translation)
    {
        if (!Finite(translation)) throw new ArgumentException("Translation must be finite.", nameof(translation));
        if (moving.Shape == ColliderShapeKind.Plane) throw new ArgumentException("A swept body must be finite.", nameof(moving));
        if (obstacle.Shape == ColliderShapeKind.Plane)
        {
            var support = MaterialSupport.Point(moving, -obstacle.U1);
            var gap = Vector3.Dot(support - obstacle.Position, obstacle.U1) + MathF.Min(0f, Vector3.Dot(translation, obstacle.U1));
            return gap > PhysicalTolerance.ContactSeparationMetres ? MaterialQueryResult.Clear : MaterialQueryResult.Intersecting;
        }
        var forward = Boundaries(moving, obstacle, translation);
        if (forward == MaterialQueryResult.Intersecting) return forward;
        var reverse = Boundaries(obstacle, moving, -translation);
        return reverse == MaterialQueryResult.Intersecting ? reverse :
            forward == MaterialQueryResult.Unresolved || reverse == MaterialQueryResult.Unresolved ? MaterialQueryResult.Unresolved : MaterialQueryResult.Clear;
    }

    private MaterialQueryResult Boundaries(HollowPartner source, HollowPartner target, Vector3 translation)
    {
        var unresolved = false;
        var timeBound = translation.Length();
        for (var face = 0; face < MaterialSurface.Count(source); face++)
        {
            var patch = new MaterialSurface(source, face);
            var derivatives = patch.DerivativeBounds;
            _pending.Clear();
            _pending.Push(new(0f, 1f, 0f, 1f, 0f, timeBound > 0f ? 1f : 0f));
            while (_pending.TryPop(out var cell))
            {
                var u = .5f * (cell.U0 + cell.U1);
                var v = .5f * (cell.V0 + cell.V1);
                var t = .5f * (cell.T0 + cell.T1);
                var ru = .5f * (cell.U1 - cell.U0) * derivatives.X;
                var rv = .5f * (cell.V1 - cell.V0) * derivatives.Y;
                var rt = .5f * (cell.T1 - cell.T0) * timeBound;
                var radius = ru + rv + rt;
                var point = patch.Point(u, v) + t * translation;
                var distance = HollowContactQuery.SurfaceDistance(target, point, out _, out _);
                if (!float.IsFinite(distance + radius)) { unresolved = true; continue; }
                if (distance <= PhysicalTolerance.ContactSeparationMetres) return MaterialQueryResult.Intersecting;
                if (distance - radius > PhysicalTolerance.ContactSeparationMetres) continue;
                if (ru >= rv && ru >= rt)
                {
                    if (u == cell.U0 || u == cell.U1) { unresolved = true; continue; }
                    _pending.Push(cell with { U0 = u }); _pending.Push(cell with { U1 = u });
                }
                else if (rv >= rt)
                {
                    if (v == cell.V0 || v == cell.V1) { unresolved = true; continue; }
                    _pending.Push(cell with { V0 = v }); _pending.Push(cell with { V1 = v });
                }
                else
                {
                    if (t == cell.T0 || t == cell.T1) { unresolved = true; continue; }
                    _pending.Push(cell with { T0 = t }); _pending.Push(cell with { T1 = t });
                }
            }
        }
        return unresolved ? MaterialQueryResult.Unresolved : MaterialQueryResult.Clear;
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
