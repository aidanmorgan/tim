using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int GasStoreBytes = 128, GasShapeBytes = 16, InventoryMassBytes = 16, AmbientLoadBytes = 32, IntegrityObserverBytes = 64;
    public const int GasBodyOffset = 8, GasMaterialOffset = 12, GasReferenceOffset = 36;
    public const int GasBoundaryOffset = 48, GasGaugeOffset = 52, GasConductanceOffset = 56;
    public const int GasPreparationWorkOffset = 60, GasPreparationElasticOffset = 64, GasPreparedVolumeOffset = 68, GasPreparedEnergyOffset = 72, GasDeclarationPaddingOffset = 76;
    public const int GasDeclarationBytes = 80, GasMassOffset = 80, GasVolumeOffset = 84, GasEnergyOffset = 88;
    public const int GasOutletOffset = 92, GasEnthalpyOffset = 96, GasBoundaryWorkOffset = 100, GasElasticReleasedOffset = 104;
    public const int GasMomentumOffset = 108, GasKineticOutOffset = 120, GasPaddingOffset = 124;
    public const int GasShapeColliderOffset = 0, GasShapeStoreOffset = 4, GasShapeDryRadiusOffset = 8, GasShapePaddingOffset = 12;
    public const int InventoryMassBodyOffset = 0, InventoryMassDryOffset = 4, InventoryMassPaddingOffset = 8;
    public const int AmbientBodyOffset = 0, AmbientStoreOffset = 4, AmbientAccelerationOffset = 8, AmbientDragOffset = 20, AmbientPaddingOffset = 24;
    public const int IntegrityColliderOffset = 8, IntegrityStoreOffset = 12, IntegrityThresholdOffset = 16, IntegritySpeedOffset = 20;
    public const int IntegrityDeclarationBytes = 24, IntegrityPhaseOffset = 24, IntegrityOrdinalOffset = 28, IntegrityEventPhaseOffset = 32, IntegrityPaddingOffset = 36;

    private static void AdmitGas(Span<byte> world, IPhysicsSceneDeclaration scene)
    {
        foreach (var store in scene.GasStores)
        {
            var row = world.Slice(scene.Layout.GetGasStoreOffset(store.Id), GasStoreBytes);
            U64(row, RecordIdentityOffset, store.Id.Value); U32(row, GasBodyOffset, BodySlot(scene, store.Body));
            var m = store.Material;
            var values = new[] { m.R, m.Cv, m.MinimumTemperature, m.MaximumTemperature, m.MaximumDensity, m.MaximumPressure };
            for (var i = 0; i < values.Length; i++) F32(row, GasMaterialOffset + i * sizeof(float), values[i]);
            WriteGasState(row, GasReferenceOffset, store.Reference);
            F32(row, GasBoundaryOffset, store.BoundaryPressure); F32(row, GasGaugeOffset, store.GaugePressure); F32(row, GasConductanceOffset, store.Conductance);
            var prepared = store.Prepared;
            F32(row, GasPreparationWorkOffset, prepared.BoundaryWorkOut); F32(row, GasPreparationElasticOffset, prepared.ElasticEnergyChange);
            F32(row, GasPreparedVolumeOffset, prepared.State.Volume); F32(row, GasPreparedEnergyOffset, prepared.State.Energy);
            WriteGasState(row, GasMassOffset, prepared.State);
        }
        for (var i = 0; i < scene.GasShapes.Length; i++)
        {
            var shape = scene.GasShapes[i]; var row = world.Slice(scene.World.GasShapeOffset(i), GasShapeBytes);
            U32(row, GasShapeColliderOffset, (uint)scene.Layout.GetColliderSlot(shape.Collider));
            U32(row, GasShapeStoreOffset, (uint)scene.Layout.GetGasStoreSlot(shape.Store));
            F32(row, GasShapeDryRadiusOffset, shape.DryRadius);
        }
        for (var i = 0; i < scene.InventoryMasses.Length; i++)
        {
            var mass = scene.InventoryMasses[i]; var row = world.Slice(scene.World.InventoryMassOffset(i), InventoryMassBytes);
            U32(row, InventoryMassBodyOffset, BodySlot(scene, mass.Body)); F32(row, InventoryMassDryOffset, mass.DryMass);
        }
        for (var i = 0; i < scene.AmbientLoads.Length; i++)
        {
            var load = scene.AmbientLoads[i]; var row = world.Slice(scene.World.AmbientLoadOffset(i), AmbientLoadBytes);
            U32(row, AmbientBodyOffset, BodySlot(scene, load.Body));
            U32(row, AmbientStoreOffset, load.InventoryFraction is { } id ? (uint)scene.Layout.GetGasStoreSlot(id) : NoBody);
            F32Vector(row, AmbientAccelerationOffset, load.Acceleration.X, load.Acceleration.Y, load.Acceleration.Z);
            F32(row, AmbientDragOffset, load.LinearDrag);
        }
        foreach (var observer in scene.IntegrityObservers)
        {
            var row = world.Slice(scene.Layout.GetIntegrityObserverOffset(observer.Id), IntegrityObserverBytes);
            U64(row, RecordIdentityOffset, observer.Id.Value);
            U32(row, IntegrityColliderOffset, (uint)scene.Layout.GetColliderSlot(observer.Collider));
            U32(row, IntegrityStoreOffset, (uint)scene.Layout.GetGasStoreSlot(observer.Store));
            F32(row, IntegrityThresholdOffset, observer.Threshold); F32(row, IntegritySpeedOffset, observer.MinimumApproachSpeed);
        }
    }

    public static GasEnvelopeState ReadPreparedGas(ReadOnlySpan<byte> row) =>
        new(RF(row, GasReferenceOffset), RF(row, GasPreparedVolumeOffset), RF(row, GasPreparedEnergyOffset));

    public static GasMaterial ReadGasMaterial(ReadOnlySpan<byte> row) => new(
        RF(row, GasMaterialOffset), RF(row, GasMaterialOffset + 4), RF(row, GasMaterialOffset + 8),
        RF(row, GasMaterialOffset + 12), RF(row, GasMaterialOffset + 16), RF(row, GasMaterialOffset + 20));

    public static GasEnvelopeState ReadGasState(ReadOnlySpan<byte> row, int offset) =>
        new(RF(row, offset), RF(row, offset + sizeof(float)), RF(row, offset + 2 * sizeof(float)));

    private static void WriteGasState(Span<byte> row, int offset, GasEnvelopeState state)
    {
        F32(row, offset, state.Mass); F32(row, offset + sizeof(float), state.Volume); F32(row, offset + 2 * sizeof(float), state.Energy);
    }

    public static GasStoreRead[] ReadGas(ReadOnlySpan<byte> world)
    {
        var layout = Layout(world); var values = new GasStoreRead[layout.GasStores];
        for (var i = 0; i < values.Length; i++)
        {
            var row = world.Slice(layout.GasStoreOffset(i), GasStoreBytes);
            var bodySlot = R32(row, GasBodyOffset);
            if (bodySlot >= layout.Bodies) throw new ArgumentException("Unknown gas owner body.");
            var value = new GasStoreRead(new(R64(row, RecordIdentityOffset)), new(R64(world, layout.BodyOffset((int)bodySlot))),
                ReadGasState(row, GasMassOffset), (GasOutletPhase)R32(row, GasOutletOffset),
                RF(row, GasEnthalpyOffset), RF(row, GasBoundaryWorkOffset), RF(row, GasElasticReleasedOffset),
                new(RF(row, GasMomentumOffset), RF(row, GasMomentumOffset + 4), RF(row, GasMomentumOffset + 8)),
                RF(row, GasKineticOutOffset), RF(row, GasPreparationWorkOffset), RF(row, GasPreparationElasticOffset));
            value.Validate(); values[i] = value;
        }
        return values;
    }

    public static IntegrityRead[] ReadIntegrity(ReadOnlySpan<byte> world)
    {
        var layout = Layout(world); var values = new IntegrityRead[layout.IntegrityObservers];
        for (var i = 0; i < values.Length; i++)
        {
            var row = world.Slice(layout.IntegrityObserverOffset(i), IntegrityObserverBytes);
            var value = new IntegrityRead(new(R64(row, RecordIdentityOffset)), (IntegrityPhase)R32(row, IntegrityPhaseOffset),
                R32(row, IntegrityOrdinalOffset), RF(row, IntegrityEventPhaseOffset));
            value.Validate(); values[i] = value;
        }
        return values;
    }

    private static void ValidateGasCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(candidate);
        for (var i = 0; i < layout.GasStores; i++)
        {
            var offset = layout.GasStoreOffset(i); var row = candidate.Slice(offset, GasStoreBytes); var previous = source.Slice(offset, GasStoreBytes);
            if (!row[..GasDeclarationBytes].SequenceEqual(previous[..GasDeclarationBytes]) ||
                !AllZero(row[GasPaddingOffset..]) || !Enum.IsDefined((GasOutletPhase)R32(row, GasOutletOffset)) ||
                R32(row, GasOutletOffset) < R32(previous, GasOutletOffset))
                throw new ArgumentException("Gas declaration or outlet changed unlawfully.");
            var state = ReadGasState(row, GasMassOffset);
            IsobaricGas.ValidateRuntime(ReadPreparedGas(row), state);
            if (state.Mass > RF(previous, GasMassOffset) || state.Volume > RF(previous, GasVolumeOffset) ||
                state.Energy > RF(previous, GasEnergyOffset))
                throw new ArgumentException("Finite gas inventory cannot refill.");
            ValidateGasLedger(row, previous);
        }
        if (!candidate[layout.GasShapesOffset..layout.IntegrityObserversOffset].SequenceEqual(
            source[layout.GasShapesOffset..layout.IntegrityObserversOffset]))
            throw new ArgumentException("Gas coupling declaration changed.");
        for (var i = 0; i < layout.IntegrityObservers; i++)
        {
            var offset = layout.IntegrityObserverOffset(i);
            var row = candidate.Slice(offset, IntegrityObserverBytes); var previous = source.Slice(offset, IntegrityObserverBytes);
            if (!row[..IntegrityDeclarationBytes].SequenceEqual(previous[..IntegrityDeclarationBytes]) ||
                !AllZero(row[IntegrityPaddingOffset..]) ||
                (R32(previous, IntegrityPhaseOffset) == (uint)IntegrityPhase.Broken &&
                 !row[IntegrityPhaseOffset..IntegrityPaddingOffset].SequenceEqual(previous[IntegrityPhaseOffset..IntegrityPaddingOffset])))
                throw new ArgumentException("Integrity declaration or committed occurrence changed.");
        }
        _ = ReadIntegrity(candidate);
    }

    private static void ValidateGasLedger(ReadOnlySpan<byte> row, ReadOnlySpan<byte> previous)
    {
        foreach (var offset in new[] { GasEnthalpyOffset, GasBoundaryWorkOffset, GasElasticReleasedOffset, GasKineticOutOffset })
            if (!float.IsFinite(RF(row, offset)) || RF(row, offset) < RF(previous, offset))
                throw new ArgumentException("Invalid gas energy ledger.");
        for (var i = 0; i < 3; i++)
            if (!float.IsFinite(RF(row, GasMomentumOffset + i * sizeof(float))))
                throw new ArgumentException("Invalid gas momentum ledger.");
    }
}
