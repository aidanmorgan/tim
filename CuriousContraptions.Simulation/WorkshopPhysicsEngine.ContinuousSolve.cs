using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private readonly List<ContinuousCoordinateLoad> _componentLoads = [];
    private float[] _acceptedLoads = [], _correctionLoads = [], _correctionWork = [];
    private float[] _correctionMatrix = [], _correctionRhs = [], _correctionDelta = [], _loadUpper = [];

    private void SolveContinuousComponent(ReadOnlySpan<byte> world, PhysicsWorldLayout layout,
        float dt, SoftCoefficients soft, int root)
    {
        _componentLoads.Clear();
        foreach (var load in _continuousLoads)
            if (load.Demand > 0 && ContinuousLoadRoot(load) == root) _componentLoads.Add(load);
        var accepted = Scratch(ref _acceptedLoads, _componentLoads.Count);
        accepted.Clear();
        var scale = 0f;
        var step = 1f;
        for (var attempt = 0; attempt < 32 && scale < 1; attempt++)
        {
            var proposed = MathF.Min(1, scale + step);
            if (!(proposed > scale)) break;
            SetContinuousScale(layout.AirflowSources, root, proposed);
            foreach (var load in _componentLoads) load.Target = ContinuousLoadDemand(world, layout, load);
            for (var i = 0; i < _componentLoads.Count; i++) _componentLoads[i].Impulse = accepted[i];
            var solved = SolveContinuousLoads(world, layout, dt, soft);
            if (_continuousGraphChanged) return;
            if (solved && ContinuousFeasible(root, layout.AirflowSources, dt) &&
                ContinuousPaymentsFit(world, layout))
            {
                scale = proposed;
                for (var i = 0; i < _componentLoads.Count; i++) accepted[i] = _componentLoads[i].Impulse;
                step = MathF.Min(1 - scale, step * 2);
            }
            else step *= .5f;
        }
        SetContinuousScale(layout.AirflowSources, root, scale);
        for (var i = 0; i < _componentLoads.Count; i++) _componentLoads[i].Impulse = accepted[i];
    }

    private bool SolveContinuousLoads(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, float dt, SoftCoefficients soft)
    {
        if (_componentLoads.Count == 1) return SolveSingleContinuousLoad(world, layout, dt, soft);
        EvaluateContinuousTrial(world, layout, dt, soft);
        if (_componentLoads.Count == 0) return true;
        return SolveCoupledContinuousLoads(world, layout, dt, soft);
    }

    private bool SolveSingleContinuousLoad(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, float dt, SoftCoefficients soft)
    {
        var load = _componentLoads[0];
        var target = load.Target;
        var lower = load.Impulse;
        var upper = MathF.Max(MathF.BitIncrement(lower), 2f * target / load.StartRate);
        if (!float.IsFinite(upper)) return false;
        var bracketed = false;
        var previousWork = 0f;
        for (var correction = 0; correction < 24; correction++)
        {
            load.Impulse = bracketed ? lower + .5f * (upper - lower) : upper;
            EvaluateContinuousTrial(world, layout, dt, soft);
            if (_continuousGraphChanged) return false;
            if (LoadPaid(load, target)) return true;
            var nonreversing = load.ClosureRate >= 0 && load.FinalRate >= 0 && float.IsFinite(load.Work);
            if (!nonreversing || load.Work >= target)
            {
                upper = load.Impulse;
                bracketed = true;
            }
            else
            {
                if (!bracketed && load.Work <= previousWork) return false;
                lower = load.Impulse;
                if (!bracketed)
                {
                    previousWork = load.Work;
                    upper *= 2;
                    if (!float.IsFinite(upper)) return false;
                }
            }
            if (bracketed && !(MathF.BitIncrement(lower) < upper))
            {
                load.Impulse = upper;
                EvaluateContinuousTrial(world, layout, dt, soft);
                return LoadPaid(load, target);
            }
        }
        return false;
    }

    private static bool LoadPaid(ContinuousCoordinateLoad load, float target) =>
        load.ClosureRate >= 0 && load.FinalRate >= 0 && float.IsFinite(load.Work) &&
        load.Work >= target && load.Work - target <= PhysicsReadValidation.Residual(target) + PhysicsReadValidation.Residual(load.Work);


    private float ContinuousLoadDemand(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, ContinuousCoordinateLoad load)
    {
        var total = 0f;
        for (var source = 0; source < layout.AirflowSources; source++)
        {
            var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
            if ((ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset) != ContinuousWorkInputKind.Coordinate ||
                ReadU32(row, PhysicsGpuAbi.AirflowCoordinateOffset) != load.Slot) continue;
            var work = 0f;
            foreach (var branch in _airflowBranches)
                if (branch.Source == source)
                    work += branch.FlowSpeed * (branch.Impulse * _continuousScales[source]);
            total += work;
        }
        return total;
    }

    private bool ContinuousPaymentsFit(ReadOnlySpan<byte> world, PhysicsWorldLayout layout)
    {
        for (var store = 0; store < layout.EnergyStores; store++)
        {
            var balance = StoreBalance(world, layout.EnergyStoreOffset(store));
            if (!ContinuousPaymentsFit(world, layout, ContinuousWorkInputKind.Store, (uint)store, balance)) return false;
        }
        for (var supply = 0; supply < layout.ElectricalSources; supply++)
        {
            var offset = layout.ElectricalSourceOffset(supply);
            if (!ContinuousPaymentsFit(world, layout, ContinuousWorkInputKind.Electrical, (uint)supply,
                SourceBalance(world, offset))) return false;
            if (!ContinuousPaymentsFit(world, layout, ContinuousWorkInputKind.Electrical, (uint)supply,
                PhaseBalance(world, offset))) return false;
        }
        return true;
    }

    private bool ContinuousPaymentsFit(ReadOnlySpan<byte> world, PhysicsWorldLayout layout,
        ContinuousWorkInputKind kind, uint slot, FiniteEnergyBalance balance)
    {
        // Trial the same stable source-ID settlement as commit; summing accepted payments into
        // one rounded scalar could undercharge the shared source.
        for (var source = 0; source < layout.AirflowSources; source++)
        {
            var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
            var reference = kind == ContinuousWorkInputKind.Store ? PhysicsGpuAbi.AirflowStoreOffset : PhysicsGpuAbi.AirflowElectricalOffset;
            if ((ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset) != kind ||
                ReadU32(row, reference) != slot) continue;
            var work = _continuousWork[source];
            if (!float.IsFinite(work) || work < 0 || work > balance.Available) return false;
            balance = balance.Debit(work);
        }
        return true;
    }
}
