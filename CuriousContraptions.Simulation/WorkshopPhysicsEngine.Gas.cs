using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private readonly ContactIntegrity _integrityMeasure = new();
    private readonly List<ContactImpulseSample> _integritySamples = [];
    private readonly List<ContactFeatureIdentity> _gasContactKeys = [];
    private readonly List<ContactFeatureSetKey> _gasFeatureKeys = [];

    // Allocated once outside the motor-budget trial. Trials reuse this candidate inventory and cannot debit it again.
    private void AdvanceGas(Span<byte> world, PhysicsWorldLayout layout, float dt)
    {
        for (var i = 0; i < layout.GasStores; i++)
        {
            var row = world.Slice(layout.GasStoreOffset(i), PhysicsGpuAbi.GasStoreBytes);
            if ((GasOutletPhase)ReadU32(row, PhysicsGpuAbi.GasOutletOffset) != GasOutletPhase.Open) continue;
            var body = _allBodies[(int)ReadU32(row, PhysicsGpuAbi.GasBodyOffset)];
            var discharge = IsobaricGas.Discharge(PhysicsGpuAbi.ReadPreparedGas(row),
                PhysicsGpuAbi.ReadGasState(row, PhysicsGpuAbi.GasMassOffset),
                ReadF32(row, PhysicsGpuAbi.GasBoundaryOffset), ReadF32(row, PhysicsGpuAbi.GasGaugeOffset),
                ReadF32(row, PhysicsGpuAbi.GasBoundaryOffset), ReadF32(row, PhysicsGpuAbi.GasConductanceOffset),
                dt, body.LinearVelocity);
            WriteF32(row, PhysicsGpuAbi.GasMassOffset, discharge.After.Mass);
            WriteF32(row, PhysicsGpuAbi.GasVolumeOffset, discharge.After.Volume);
            WriteF32(row, PhysicsGpuAbi.GasEnergyOffset, discharge.After.Energy);
            var thermal = IsobaricGas.Released(PhysicsGpuAbi.ReadPreparedGas(row), discharge.After,
                ReadF32(row, PhysicsGpuAbi.GasBoundaryOffset), ReadF32(row, PhysicsGpuAbi.GasGaugeOffset));
            WriteF32(row, PhysicsGpuAbi.GasEnthalpyOffset, thermal.EnthalpyOut);
            WriteF32(row, PhysicsGpuAbi.GasBoundaryWorkOffset, thermal.BoundaryWork);
            WriteF32(row, PhysicsGpuAbi.GasElasticReleasedOffset, thermal.ElasticReleased);
            AddGasLedger(row, PhysicsGpuAbi.GasKineticOutOffset, discharge.KineticEnergyOut);
            var momentum = ReadVector(row, PhysicsGpuAbi.GasMomentumOffset) + discharge.MomentumOut;
            WriteVector(row, PhysicsGpuAbi.GasMomentumOffset, momentum.X, momentum.Y, momentum.Z);
        }
        RefreshInventoryMasses(world, layout);
        RefreshGasShapes(world, layout);
        ApplyAmbientLoads(world, layout);
    }

    private static void AddGasLedger(Span<byte> row, int offset, float value) =>
        WriteF32(row, offset, ReadF32(row, offset) + value);

    private void RefreshInventoryMasses(Span<byte> world, PhysicsWorldLayout layout)
    {
        for (var i = 0; i < layout.InventoryMasses; i++)
        {
            var row = world.Slice(layout.InventoryMassOffset(i), PhysicsGpuAbi.InventoryMassBytes);
            var slot = (int)ReadU32(row, PhysicsGpuAbi.InventoryMassBodyOffset);
            var body = _allBodies[slot];
            var mass = ReadF32(row, PhysicsGpuAbi.InventoryMassDryOffset);
            var full = true;
            for (var j = 0; j < layout.GasStores; j++)
            {
                var gas = layout.GasStoreOffset(j);
                if (ReadU32(world, gas + PhysicsGpuAbi.GasBodyOffset) == (uint)slot)
                {
                    mass += ReadF32(world, gas + PhysicsGpuAbi.GasMassOffset);
                    full &= ReadF32(world, gas + PhysicsGpuAbi.GasMassOffset) == ReadF32(world, gas + PhysicsGpuAbi.GasReferenceOffset);
                }
            }
            if (full) continue;
            if (body.Mass != mass)
                foreach (var collider in _colliders)
                    if (collider.Body == body) InvalidateGasContacts(collider.Index);
            body.Mass = mass; body.InvMass = 1f / mass;
            WriteF32(world, layout.BodyOffset(slot) + PhysicsGpuAbi.BodyMassOffset, mass);
        }
    }

    private void RefreshGasShapes(Span<byte> world, PhysicsWorldLayout layout)
    {
        for (var i = 0; i < layout.GasShapes; i++)
        {
            var row = world.Slice(layout.GasShapeOffset(i), PhysicsGpuAbi.GasShapeBytes);
            var colliderSlot = (int)ReadU32(row, PhysicsGpuAbi.GasShapeColliderOffset);
            var storeSlot = (int)ReadU32(row, PhysicsGpuAbi.GasShapeStoreOffset);
            var radius = IsobaricGas.OuterRadius(ReadF32(row, PhysicsGpuAbi.GasShapeDryRadiusOffset),
                ReadF32(world, layout.GasStoreOffset(storeSlot) + PhysicsGpuAbi.GasVolumeOffset));
            var collider = _colliders[colliderSlot];
            if (collider.Radius == radius) continue;
            InvalidateGasContacts(colliderSlot);
            collider.Radius = radius;
            WorkshopCollisionKernels.RefreshCollider(collider);
            WriteF32(world, layout.ColliderOffset(colliderSlot) + PhysicsGpuAbi.ColliderRadiusOffset, radius);
        }
        // PrepareBroadphase rebuilds current bounds before every accepted/trial contact query.
    }

    private void ApplyAmbientLoads(ReadOnlySpan<byte> world, PhysicsWorldLayout layout)
    {
        for (var i = 0; i < layout.AmbientLoads; i++)
        {
            var row = world.Slice(layout.AmbientLoadOffset(i), PhysicsGpuAbi.AmbientLoadBytes);
            var slot = (int)ReadU32(row, PhysicsGpuAbi.AmbientBodyOffset);
            var store = ReadU32(row, PhysicsGpuAbi.AmbientStoreOffset);
            var fraction = 1f;
            if (store != PhysicsGpuAbi.NoBody)
            {
                var gas = layout.GasStoreOffset((int)store);
                fraction = ReadF32(world, gas + PhysicsGpuAbi.GasMassOffset) / ReadF32(world, gas + PhysicsGpuAbi.GasReferenceOffset);
            }
            var baseOffset = layout.BodyOffset(slot);
            var acceleration = ReadVector(world, baseOffset + PhysicsGpuAbi.BodyGravityOffset) +
                ReadVector(row, PhysicsGpuAbi.AmbientAccelerationOffset) * fraction;
            var body = _allBodies[slot];
            body.Gx = acceleration.X; body.Gy = acceleration.Y; body.Gz = acceleration.Z;
            body.Drag = ReadF32(world, baseOffset + PhysicsGpuAbi.BodyDragOffset) + ReadF32(row, PhysicsGpuAbi.AmbientDragOffset);
        }
    }

    private void CommitIntegrity(Span<byte> world, PhysicsWorldLayout layout, uint ordinal)
    {
        if (layout.IntegrityObservers == 0) return;
        _integritySamples.Clear();
        foreach (var contact in _contacts)
            _integritySamples.Add(new(new(contact.ColA.Id), new(contact.ColB.Id), -contact.RelVel, contact.LambdaN));
        for (var i = 0; i < layout.IntegrityObservers; i++)
        {
            var row = world.Slice(layout.IntegrityObserverOffset(i), PhysicsGpuAbi.IntegrityObserverBytes);
            if ((IntegrityPhase)ReadU32(row, PhysicsGpuAbi.IntegrityPhaseOffset) == IntegrityPhase.Broken) continue;
            var collider = _colliders[(int)ReadU32(row, PhysicsGpuAbi.IntegrityColliderOffset)];
            var impulse = _integrityMeasure.Measure(CollectionsMarshal.AsSpan(_integritySamples), new(collider.Id),
                ReadF32(row, PhysicsGpuAbi.IntegritySpeedOffset));
            if (!ContactIntegrity.Ruptures(impulse, ReadF32(row, PhysicsGpuAbi.IntegrityThresholdOffset))) continue;
            WriteU32(row, PhysicsGpuAbi.IntegrityPhaseOffset, (uint)IntegrityPhase.Broken);
            WriteU32(row, PhysicsGpuAbi.IntegrityOrdinalOffset, ordinal);
            WriteF32(row, PhysicsGpuAbi.IntegrityEventPhaseOffset, 0);
            var gas = layout.GasStoreOffset((int)ReadU32(row, PhysicsGpuAbi.IntegrityStoreOffset));
            WriteU32(world, gas + PhysicsGpuAbi.GasOutletOffset, (uint)GasOutletPhase.Open);
        }
    }

    private void InvalidateGasContacts(int collider)
    {
        _gasContactKeys.Clear();
        foreach (var key in _contactCache.Keys)
            if (key.A == collider || key.B == collider) _gasContactKeys.Add(key);
        foreach (var key in _gasContactKeys) _contactCache.Remove(key);
        _normalContactSets.RemoveWhere(key => key.UsesCollider(collider));
        _gasFeatureKeys.Clear();
        foreach (var key in _frictionCache.Keys)
            if (key.UsesCollider(collider)) _gasFeatureKeys.Add(key);
        foreach (var key in _gasFeatureKeys) _frictionCache.Remove(key);
    }
}
