using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private readonly ContinuousWorkComponents _continuousComponents = new();
    private readonly List<ContinuousCoordinateLoad> _continuousLoadPool = [];
    private readonly Dictionary<int, int> _continuousCoordinateIndices = [];
    private bool[] _continuousAvailable = [];
    private float[] _continuousReservations = [];

    private void PrepareContinuousWork(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        var count = layout.AirflowSources;
        Scratch(ref _continuousScales, count).Clear();
        Scratch(ref _continuousWork, count).Clear();
        Scratch(ref _continuousMechanical, count).Clear();
        Scratch(ref _continuousReservations, count).Clear();
        Scratch(ref _continuousAvailable, count);
        Scratch(ref _continuousClosureV, layout.Bodies);
        Scratch(ref _continuousClosureW, layout.Bodies);
        _continuousLoads.Clear(); _continuousCoordinateIndices.Clear();
        _continuousComponents.Begin(layout.Bodies, count);
        foreach (var joint in _joints) _continuousComponents.JoinBodies(joint.A, joint.B);
        foreach (var joint in _revoluteJoints) _continuousComponents.JoinBodies(joint.A, joint.B);
        foreach (var transmission in _transmissions) _continuousComponents.JoinTransmission(transmission);
        foreach (var rope in _tensile) _continuousComponents.JoinRoute(rope);
        foreach (var pair in _candidatePairs) _continuousComponents.JoinBodies(pair.ColA.Body, pair.ColB.Body);
        foreach (var branch in _airflowBranches)
        {
            _continuousComponents.JoinSource(branch.Source, branch.Frame);
            _continuousComponents.JoinSource(branch.Source, branch.Receiver);
        }
        for (var source = 0; source < count; source++)
            PrepareContinuousSource(world, layout, inputs, source);
        JoinPhaseStorage(world, layout, inputs);
    }

    private void PrepareContinuousSource(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs, int source)
    {
        var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
        var kind = (ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset);
        var available = inputs.AirflowEnabled[source] && ReadF32(row, PhysicsGpuAbi.AirflowPressureOffset) > 0 &&
            ReadF32(row, PhysicsGpuAbi.AirflowSpeedOffset) > 0;
        uint slot;
        if (kind == ContinuousWorkInputKind.Coordinate)
        {
            slot = ReadU32(row, PhysicsGpuAbi.AirflowCoordinateOffset);
            var load = CoordinateLoad((int)slot, ReadF32(row, PhysicsGpuAbi.AirflowSenseOffset));
            available &= ReadU32(row, PhysicsGpuAbi.AirflowConnectedOffset) != 0 && load.StartRate > 0;
            _continuousReservations[source] = available ? _airflowDemand[source] : 0;
            load.Demand += _continuousReservations[source];
            _continuousComponents.JoinSource(source, load.Coordinate.A);
            _continuousComponents.JoinSource(source, load.Coordinate.B);
        }
        else
        {
            slot = ReadU32(row, kind == ContinuousWorkInputKind.Store ? PhysicsGpuAbi.AirflowStoreOffset : PhysicsGpuAbi.AirflowElectricalOffset);
            available &= slot != PhysicsGpuAbi.NoBody && (kind == ContinuousWorkInputKind.Store
                ? StoreBalance(world, layout.EnergyStoreOffset((int)slot)).Available > 0
                : PhaseBalance(world, layout.ElectricalSourceOffset((int)slot)).Available > 0);
            _continuousReservations[source] = available ? _airflowGrants[source] : 0;
        }
        _continuousAvailable[source] = available;
        _continuousComponents.Share(source, kind, slot);
    }

    private ContinuousCoordinateLoad CoordinateLoad(int slot, float sense)
    {
        if (_continuousCoordinateIndices.TryGetValue(slot, out var index)) return _continuousLoads[index];
        index = _continuousLoads.Count;
        if (index == _continuousLoadPool.Count) _continuousLoadPool.Add(new());
        var load = _continuousLoadPool[index];
        load.Coordinate.Bind(_revoluteJoints[slot]); load.Coordinate.Prepare();
        load.Slot = slot; load.Sense = sense; load.StartRate = sense * load.Coordinate.Rate;
        load.Demand = load.Impulse = load.Work = load.ClosureRate = load.FinalRate = 0;
        _continuousLoads.Add(load); _continuousCoordinateIndices.Add(slot, index);
        return load;
    }

    private void JoinPhaseStorage(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        for (var route = 0; route < layout.ElectricalBindings; route++)
        {
            if (!inputs.StorageRoutes[route]) continue;
            var binding = layout.ElectricalBindingOffset(route);
            var store = ReadU32(world, binding + PhysicsGpuAbi.ElectricalBindingStorageOffset);
            var supply = ReadU32(world, binding + PhysicsGpuAbi.ElectricalBindingSourceOffset);
            for (var source = 0; source < layout.AirflowSources; source++)
            {
                var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
                if ((ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset) == ContinuousWorkInputKind.Store &&
                    ReadU32(row, PhysicsGpuAbi.AirflowStoreOffset) == store)
                    _continuousComponents.Share(source, ContinuousWorkInputKind.Electrical, supply);
            }
        }
    }
}
