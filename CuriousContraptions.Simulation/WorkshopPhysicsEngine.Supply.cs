using System;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private float[] _airflowDemand = [], _airflowGrants = [];
    private float[] _supplyRequests = [], _supplyGrants = [];

    /// <summary>Validate declared supply lanes, then install this tick's drive targets and fresh debit counters.</summary>
    private void PrepareTickSupply(Span<byte> span, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        var supplyCount = layout.ElectricalSources;
        Span<bool> driven = Scratch(ref _drivenSlots, layout.Prismatics + layout.Revolutes);
        for (var p = 0; p < layout.Prismatics; p++)
            driven[p] = (PrismaticDriveKind)ReadU32(span, layout.PrismaticOffset(p) + PhysicsGpuAbi.PrismaticDriveKindOffset) == PrismaticDriveKind.Supplied;
        var revoluteDrives = 0;
        for (var r = 0; r < layout.Revolutes; r++)
        {
            driven[layout.Prismatics + r] = (RevoluteDriveKind)ReadU32(span, layout.RevoluteOffset(r) + PhysicsGpuAbi.RevoluteDriveKindOffset) == RevoluteDriveKind.Supplied;
            if (driven[layout.Prismatics + r]) revoluteDrives++;
        }
        Span<bool> timed = Scratch(ref _timedTransmissions, layout.MechanicalTransmissions);
        for (var t = 0; t < layout.MechanicalTransmissions; t++)
            timed[t] = ReadF32(span, layout.MechanicalTransmissionOffset(t) + PhysicsGpuAbi.MechanicalTransmissionCloseSecondsOffset) > 0f;
        inputs.Validate(supplyCount, driven, revoluteDrives, timed);
        ValidatePrismaticCommands(span, layout, inputs);
        if (inputs.AirflowEnabled is null || inputs.AirflowEnabled.Length != layout.AirflowSources)
            throw new ArgumentException("Airflow enable table differs from the admitted source population.");
        if (inputs.StorageRoutes is null || inputs.StorageRoutes.Length != layout.ElectricalBindings)
            throw new ArgumentException("Physics storage eligibility differs from admitted routes.");
        // engine-revolute-drive-target: each driven revolute commits this tick's target before its substeps. The world's revolutes and
        // the compiled drive table are both in joint identity order, so a driven revolute's rank among the driven ones is its drive index.
        for (int r = 0, drive = 0; r < layout.Revolutes; r++)
            if (driven[layout.Prismatics + r])
                WriteF32(span, layout.RevoluteOffset(r) + PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset, inputs.DriveTarget[drive++]);
        PrepareEnergyCommit(span, layout);
    }


    private void TransferSupply(Span<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs, uint ordinal, int substep)
    {
        var demand = Scratch(ref _airflowDemand, layout.AirflowSources);
        var grants = Scratch(ref _airflowGrants, layout.AirflowSources);
        demand.Clear(); grants.Clear();
        foreach (var branch in _airflowBranches) demand[branch.Source] += branch.FlowSpeed * branch.Impulse;
        var phase = (ordinal + substep) % 4 == 0;
        for (var supply = 0; supply < layout.ElectricalSources; supply++)
            ReserveElectricalWork(world, layout, inputs, supply, phase, checked(ordinal + (uint)substep));
        for (var store = 0; store < layout.EnergyStores; store++)
        {
            var requests = Scratch(ref _supplyRequests, layout.AirflowSources);
            var allocated = Scratch(ref _supplyGrants, layout.AirflowSources);
            requests.Clear();
            for (var source = 0; source < layout.AirflowSources; source++)
            {
                var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
                if ((ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset) == ContinuousWorkInputKind.Store &&
                    ReadU32(row, PhysicsGpuAbi.AirflowStoreOffset) == store) requests[source] = demand[source];
            }
            var offset = layout.EnergyStoreOffset(store);
            var balance = StoreBalance(world, offset);
            var capacity = ReadF32(world, offset + PhysicsGpuAbi.EnergyStoreCapacityOffset);
            balance.Validate(capacity);
            ProportionalAllocation.Grant(requests, balance.Available, allocated);
            for (var source = 0; source < allocated.Length; source++) grants[source] += allocated[source];
        }
    }

    private void ReserveElectricalWork(Span<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs,
        int supply, bool phase, uint ordinal)
    {
        var offset = layout.ElectricalSourceOffset(supply);
        var enabled = inputs.SourceEnable[supply];
        if (phase)
        {
            WriteU32(world, offset + PhysicsGpuAbi.ElectricalSourceEnableOffset, (uint)enabled);
            var allowance = enabled == ElectricalEnable.Enabled
                ? MathF.Min(SourceBalance(world, offset).Available,
                    ReadF32(world, offset + PhysicsGpuAbi.ElectricalSourceMaximumPowerOffset) * ElectricalSupply.PhaseSeconds)
                : 0f;
            WriteBalance(world, offset + PhysicsGpuAbi.ElectricalPhaseAllowanceOffset,
                offset + PhysicsGpuAbi.ElectricalPhaseRemainderOffset, new(allowance, 0));
            WriteU32(world, offset + PhysicsGpuAbi.ElectricalPhaseOrdinalOffset, ordinal);
        }
        CollectStorageRequests(world, layout, inputs, supply, phase);
        var storageCount = _storeIndices.Count;
        var count = checked(storageCount + layout.AirflowSources);
        var requests = Scratch(ref _electricalRequests, count);
        var grants = Scratch(ref _electricalGrants, count);
        for (var i = 0; i < storageCount; i++)
        {
            var store = _storeIndices[i];
            requests[i] = MathF.Min(StoreBalance(world, store).Headroom(
                ReadF32(world, store + PhysicsGpuAbi.EnergyStoreCapacityOffset)),
                ReadF32(world, store + PhysicsGpuAbi.EnergyStorePowerOffset) * ElectricalSupply.PhaseSeconds);
        }
        for (var source = 0; source < layout.AirflowSources; source++)
        {
            var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
            if ((ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset) == ContinuousWorkInputKind.Electrical &&
                ReadU32(row, PhysicsGpuAbi.AirflowElectricalOffset) == supply)
                requests[storageCount + source] = _airflowDemand[source];
        }
        ElectricalSupply.Reserve(enabled, MathF.Min(SourceBalance(world, offset).Available,
            PhaseBalance(world, offset).Available), requests, grants);
        for (var i = 0; i < storageCount; i++)
        {
            var accepted = MathF.Min(grants[i], MathF.Min(SourceBalance(world, offset).Available,
                PhaseBalance(world, offset).Available));
            StoreCredit(world, _storeIndices[i], accepted);
            SourcePayment(world, offset, accepted);
        }
        // Charge settlement may discard only its bounded low remainder. Direct work retains its entitlement,
        // conservatively reduced to the actual remaining common allowance before any physical trial.
        var direct = grants[storageCount..];
        ProportionalAllocation.Grant(direct, MathF.Min(SourceBalance(world, offset).Available,
            PhaseBalance(world, offset).Available), direct);
        for (var source = 0; source < direct.Length; source++) _airflowGrants[source] += direct[source];
    }

    private void CollectStorageRequests(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs,
        int supply, bool phase)
    {
        _storeIndices.Clear();
        if (!phase) return;
        for (var route = 0; route < layout.ElectricalBindings; route++)
        {
            var binding = layout.ElectricalBindingOffset(route);
            if (!inputs.StorageRoutes[route] || ReadU32(world, binding + PhysicsGpuAbi.ElectricalBindingSourceOffset) != supply) continue;
            _storeIndices.Add(layout.EnergyStoreOffset((int)ReadU32(world, binding + PhysicsGpuAbi.ElectricalBindingStorageOffset)));
        }
        // Store rows are in exact identity order, independent of route declaration order.
        _storeIndices.Sort();
    }
}
