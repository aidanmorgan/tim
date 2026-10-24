using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int ElectricalSourceBytes = 64;
    // Source row: identity U64 at 0, owner body slot, declared enable, f32 capacity, maximum power and initial energy, zero
    // padding; then the state: f32 remaining energy, enable, f32 debit and zero padding to the end.
    public const int ElectricalSourceOwnerOffset = 8;
    public const int ElectricalSourceDeclaredEnableOffset = 12;
    public const int ElectricalSourceCapacityOffset = 16;
    public const int ElectricalSourceMaximumPowerOffset = 20;
    public const int ElectricalSourceInitialEnergyOffset = 24;
    private const int ElectricalSourceDeclarationPaddingOffset = 28;
    public const int ElectricalSourceDeclarationBytes = 32;
    public const int ElectricalSourceRemainingOffset = 32;
    public const int ElectricalSourceEnableOffset = 36;
    public const int ElectricalSourceDebitOffset = 40;
    private const int ElectricalSourcePaddingOffset = 44;
    public const int ElectricalBindingBytes = 16;
    // Binding row: source slot, contact-work storage slot, zero padding.
    public const int ElectricalBindingSourceOffset = 0;
    public const int ElectricalBindingStorageOffset = 4;
    private const int ElectricalBindingPaddingOffset = 8;

    private static void WriteEnergy(Span<byte> bytes, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value);

    private static void WriteElectricalAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.Electrical.Sources.Length; i++)
        {
            var source = scene.Electrical.Sources[i];
            var record = data.Slice(scene.Layout.GetElectricalSourceOffset(source.Id), ElectricalSourceBytes);
            U64(record, RecordIdentityOffset, source.Id.Value);
            U32(record, ElectricalSourceOwnerOffset, (uint)scene.Layout.GetBodySlot(source.Owner));
            U32(record, ElectricalSourceDeclaredEnableOffset, (uint)source.Enabled);
            WriteEnergy(record, ElectricalSourceCapacityOffset, source.Capacity.Value);
            WriteEnergy(record, ElectricalSourceMaximumPowerOffset, source.MaximumPower.Value);
            WriteEnergy(record, ElectricalSourceInitialEnergyOffset, source.InitialEnergy.Value);
            WriteEnergy(record, ElectricalSourceRemainingOffset, source.InitialEnergy.Value);
            U32(record, ElectricalSourceEnableOffset, (uint)source.Enabled);
        }
        for (var i = 0; i < scene.Electrical.Bindings.Length; i++)
        {
            var binding = scene.Electrical.Bindings[i];
            var record = data.Slice(scene.World.ElectricalBindingOffset(i), ElectricalBindingBytes);
            U32(record, ElectricalBindingSourceOffset, (uint)scene.Layout.GetElectricalSourceSlot(binding.Source));
            U32(record, ElectricalBindingStorageOffset, (uint)scene.Layout.GetContactWorkSlot(binding.Storage));
        }
    }

    public static PhysicsElectricalRead ReadElectrical(ReadOnlySpan<byte> data)
    {
        Header(data);
        var layout = Layout(data);
        var count = layout.ElectricalSources;
        var values = new ElectricalSourceRead[count];
        for (var i = 0; i < count; i++)
        {
            var record = data.Slice(layout.ElectricalSourceOffset(i), ElectricalSourceBytes);
            var owner = R32(record, ElectricalSourceOwnerOffset);
            if (owner >= R32(data, HeaderBodyCountOffset) ||
                !AllZero(record[ElectricalSourceDeclarationPaddingOffset..ElectricalSourceDeclarationBytes]) ||
                !AllZero(record[ElectricalSourcePaddingOffset..]) ||
                R32(record, ElectricalSourceDeclaredEnableOffset) > (uint)ElectricalEnable.Enabled ||
                R32(record, ElectricalSourceEnableOffset) > (uint)ElectricalEnable.Enabled)
                throw new ArgumentException("Invalid electrical source record.");
            ElectricalSourceDeclaration.ValidateStore(new(RF(record, ElectricalSourceCapacityOffset)),
                new(RF(record, ElectricalSourceMaximumPowerOffset)), 1f, (ElectricalEnable)R32(record, ElectricalSourceDeclaredEnableOffset));
            var initial = RF(record, ElectricalSourceInitialEnergyOffset);
            if (!float.IsFinite(initial) || initial < 0 || initial > RF(record, ElectricalSourceCapacityOffset) ||
                RF(record, ElectricalSourceRemainingOffset) > initial)
                throw new ArgumentException("Electrical source exceeds its initial finite charge.");
            values[i] = new(new(R64(record, RecordIdentityOffset)), new(R64(data, layout.BodyOffset((int)owner))),
                new(RF(record, ElectricalSourceRemainingOffset)), new(RF(record, ElectricalSourceDebitOffset)),
                (ElectricalEnable)R32(record, ElectricalSourceEnableOffset));
        }
        var seen = new bool[layout.ContactWorks];
        for (var i = 0; i < layout.ElectricalBindings; i++)
        {
            var record = data.Slice(layout.ElectricalBindingOffset(i), ElectricalBindingBytes);
            var source = R32(record, ElectricalBindingSourceOffset); var storage = R32(record, ElectricalBindingStorageOffset);
            if (source >= count || storage >= (uint)layout.ContactWorks || seen[(int)storage] ||
                !AllZero(record[ElectricalBindingPaddingOffset..]))
                throw new ArgumentException("Invalid or ambiguous electrical storage route.");
            seen[(int)storage] = true;
        }
        return PhysicsElectricalRead.Adopt(values);
    }

    private static void ValidateElectricalCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> previous)
    {
        var before = ReadElectrical(previous); var after = ReadElectrical(candidate);
        after.ValidateAdvance(before);
        var layout = Layout(previous);
        if (!candidate[layout.ElectricalBindingsOffset..layout.PrismaticsOffset].SequenceEqual(previous[layout.ElectricalBindingsOffset..layout.PrismaticsOffset]))
            throw new ArgumentException("Electrical routes changed during a Run.");
        var phaseCount = (R32(candidate,HeaderSourceOrdinalOffset) + 3) / 4 - (R32(previous,HeaderSourceOrdinalOffset) + 3) / 4;
        var works = ReadContactWorks(candidate);
        var supplied = new bool[works.Count];
        for (var i = 0; i < after.Count; i++)
        {
            var offset = layout.ElectricalSourceOffset(i);
            if (!candidate.Slice(offset, ElectricalSourceDeclarationBytes).SequenceEqual(previous.Slice(offset, ElectricalSourceDeclarationBytes)))
                throw new ArgumentException("Electrical source declaration changed.");
            var limit = RF(candidate, offset + ElectricalSourceMaximumPowerOffset) * ElectricalSupply.PhaseSeconds * phaseCount;
            if (after[i].Debit.Value > limit + PhysicsElectricalRead.Residual(limit))
                throw new ArgumentException("Electrical source exceeded its phase power allowance.");
            double credit = 0, tolerance = PhysicsElectricalRead.Residual(before[i].Remaining.Value);
            for (var route = 0; route < layout.ElectricalBindings; route++)
            {
                var binding = layout.ElectricalBindingOffset(route);
                if (R32(candidate, binding + ElectricalBindingSourceOffset) != i) continue;
                var slot = (int)R32(candidate, binding + ElectricalBindingStorageOffset);
                supplied[slot] = true; credit += works[slot].SuppliedEnergy.Value;
                tolerance += PhysicsElectricalRead.Residual(works[slot].RemainingEnergy.Value);
            }
            if ((after[i].Debit.Value == 0 && credit != 0) || Math.Abs(after[i].Debit.Value - credit) > tolerance)
                throw new ArgumentException("Electrical debit does not fund its connected storage credits.");
        }
        for (var i = 0; i < works.Count; i++)
            if (!supplied[i] && works[i].SuppliedEnergy.Value != 0)
                throw new ArgumentException("Unconnected storage received electrical work.");
    }
}
