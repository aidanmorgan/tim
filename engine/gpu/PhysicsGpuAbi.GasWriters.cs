using System;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    private static void ValidateInventoryMassWrite(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source,
        PhysicsWorldLayout layout, int slot)
    {
        var actual = RF(candidate, layout.BodyOffset(slot) + BodyMassOffset);
        var previous = RF(source, layout.BodyOffset(slot) + BodyMassOffset);
        for (var i = 0; i < layout.InventoryMasses; i++)
        {
            var row = layout.InventoryMassOffset(i);
            if (R32(candidate, row + InventoryMassBodyOffset) != (uint)slot) continue;
            var expected = RF(candidate, row + InventoryMassDryOffset);
            var full = true;
            for (var j = 0; j < layout.GasStores; j++)
            {
                var gas = layout.GasStoreOffset(j);
                if (R32(candidate, gas + GasBodyOffset) != (uint)slot) continue;
                var mass = RF(candidate, gas + GasMassOffset);
                expected += mass; full &= mass == RF(candidate, gas + GasReferenceOffset);
            }
            if (actual != (full ? previous : expected))
                throw new ArgumentException("Body mass disagrees with its sole inventory source.");
            return;
        }
        if (actual != previous) throw new ArgumentException("Undeclared body mass change.");
    }

    private static void ValidateGasGeometryWrite(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source,
        PhysicsWorldLayout layout)
    {
        for (var slot = 0; slot < layout.Colliders; slot++)
        {
            var actual = candidate.Slice(layout.ColliderOffset(slot), ColliderBytes);
            var previous = source.Slice(layout.ColliderOffset(slot), ColliderBytes);
            var binding = -1;
            for (var i = 0; i < layout.GasShapes; i++)
                if (R32(candidate, layout.GasShapeOffset(i) + GasShapeColliderOffset) == (uint)slot) binding = i;
            if (binding < 0)
            {
                if (!actual.SequenceEqual(previous)) throw new ArgumentException("Undeclared physical geometry change.");
                continue;
            }
            if (!actual[..ColliderRadiusOffset].SequenceEqual(previous[..ColliderRadiusOffset]) ||
                !actual[(ColliderRadiusOffset + sizeof(float))..].SequenceEqual(previous[(ColliderRadiusOffset + sizeof(float))..]))
                throw new ArgumentException("Gas shape changed unrelated collider fields.");
            var row = layout.GasShapeOffset(binding);
            var store = (int)R32(candidate, row + GasShapeStoreOffset);
            var radius = IsobaricGas.OuterRadius(RF(candidate, row + GasShapeDryRadiusOffset),
                RF(candidate, layout.GasStoreOffset(store) + GasVolumeOffset));
            if (RF(actual, ColliderRadiusOffset) != radius)
                throw new ArgumentException("Physical gas shape disagrees with committed volume.");
        }
    }
}
