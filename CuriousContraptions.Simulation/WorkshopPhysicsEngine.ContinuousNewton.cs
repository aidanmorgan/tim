using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private bool SolveCoupledContinuousLoads(ReadOnlySpan<byte> world, PhysicsWorldLayout layout,
        float dt, SoftCoefficients soft)
    {
        var count = _componentLoads.Count;
        var point = Scratch(ref _correctionLoads, count);
        var work = Scratch(ref _correctionWork, count);
        var upper = Scratch(ref _loadUpper, count);
        var matrix = Scratch(ref _correctionMatrix, checked(count * count));
        var rhs = Scratch(ref _correctionRhs, count);
        var delta = Scratch(ref _correctionDelta, count);
        for (var i = 0; i < count; i++)
        {
            var load = _componentLoads[i];
            upper[i] = MathF.Max(MathF.BitIncrement(load.Impulse), 2f * load.Target / load.StartRate);
            if (!float.IsFinite(upper[i])) return false;
        }
        for (var correction = 0; correction < 24; correction++)
        {
            var paid = true;
            for (var i = 0; i < count; i++)
            {
                var load = _componentLoads[i];
                paid &= LoadPaid(load, load.Target);
                point[i] = load.Impulse; work[i] = load.Work;
                rhs[i] = load.Target - load.Work;
            }
            if (paid) return true;
            var error = ContinuousResidual();
            if (!float.IsFinite(error)) return false;
            if (!ContinuousJacobian(world, layout, dt, soft, point, work, upper, matrix)) return false;
            if (!DenseLinearSystem.Solve(matrix, rhs, delta)) return false;
            var accepted = false;
            var fraction = 1f;
            for (var backtrack = 0; backtrack < 24; backtrack++)
            {
                var distinct = false;
                for (var i = 0; i < count; i++)
                {
                    var proposed = Math.Clamp(point[i] + fraction * delta[i], 0, upper[i]);
                    distinct |= proposed != point[i];
                    _componentLoads[i].Impulse = proposed;
                }
                if (!distinct) break;
                EvaluateContinuousTrial(world, layout, dt, soft);
                if (_continuousGraphChanged) return false;
                if (ContinuousResidual() < error)
                {
                    accepted = true;
                    for (var i = 0; i < count; i++)
                        if (_componentLoads[i].Impulse == upper[i]) upper[i] *= 2;
                    break;
                }
                fraction *= .5f;
            }
            if (!accepted) return false;
        }
        foreach (var load in _componentLoads)
            if (!LoadPaid(load, load.Target)) return false;
        return true;
    }

    private bool ContinuousJacobian(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, float dt, SoftCoefficients soft,
        ReadOnlySpan<float> point, ReadOnlySpan<float> work, ReadOnlySpan<float> upper, Span<float> matrix)
    {
        var count = _componentLoads.Count;
        for (var column = 0; column < count; column++)
        {
            for (var i = 0; i < count; i++) _componentLoads[i].Impulse = point[i];
            var distance = .25f * (upper[column] - point[column]);
            var probe = point[column] + distance;
            if (!(probe > point[column]))
            {
                distance = -.25f * point[column];
                probe = point[column] + distance;
            }
            if (probe == point[column] || !float.IsFinite(probe)) return false;
            _componentLoads[column].Impulse = probe;
            EvaluateContinuousTrial(world, layout, dt, soft);
            if (_continuousGraphChanged) return false;
            for (var row = 0; row < count; row++)
            {
                var load = _componentLoads[row];
                if (load.ClosureRate < 0 || load.FinalRate < 0 || !float.IsFinite(load.Work)) return false;
                matrix[row * count + column] = (load.Work - work[row]) / (probe - point[column]);
            }
            // The accepted branch is the first rising-work branch; no Newton correction may select its descending root.
            if (!(matrix[column * count + column] > 0)) return false;
        }
        return true;
    }

    private float ContinuousResidual()
    {
        var result = 0f;
        foreach (var load in _componentLoads)
        {
            var target = load.Target;
            if (!(target > 0) || load.ClosureRate < 0 || load.FinalRate < 0 || !float.IsFinite(load.Work))
                return float.PositiveInfinity;
            result += MathF.Abs(load.Work - target) / target;
        }
        return result;
    }
}
