using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int ElectricalSourceBytes = 64;
    public const int ElectricalBindingBytes = 16;

    private static void WriteEnergy(Span<byte> bytes, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value);

    private static void WriteElectricalAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.Electrical.Sources.Length; i++)
        {
            var source = scene.Electrical.Sources[i];
            var record = data.Slice(scene.Layout.GetElectricalSourceOffset(source.Id), ElectricalSourceBytes);
            U64(record, 0, source.Id.Value);
            U32(record, 8, (uint)scene.Layout.GetBodySlot(source.Owner)); U32(record, 12, (uint)source.Enabled);
            WriteEnergy(record, 16, source.Capacity.Value); WriteEnergy(record, 20, source.MaximumPower.Value);
            WriteEnergy(record, 24, source.InitialEnergy.Value); WriteEnergy(record, 32, source.InitialEnergy.Value);
            U32(record, 36, (uint)source.Enabled);
        }
        for (var i = 0; i < scene.Electrical.Bindings.Length; i++)
        {
            var binding = scene.Electrical.Bindings[i];
            var record = data.Slice(scene.World.ElectricalBindingOffset(i), ElectricalBindingBytes);
            U32(record, 0, (uint)scene.Layout.GetElectricalSourceSlot(binding.Source)); U32(record, 4, (uint)scene.Layout.GetContactWorkSlot(binding.Storage));
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
            var owner = R32(record, 8);
            if (owner >= R32(data, HeaderBodyCountOffset) || !AllZero(record[28..32]) || !AllZero(record[44..]) ||
                R32(record, 12) > (uint)ElectricalEnable.Enabled || R32(record, 36) > (uint)ElectricalEnable.Enabled)
                throw new ArgumentException("Invalid electrical source record.");
            new ElectricalSourceSettings(new(RF(record, 16)), new(RF(record, 20)),
                1f, (ElectricalEnable)R32(record, 12)).Validate();
            if (!float.IsFinite(RF(record,24)) || RF(record,24) < 0 || RF(record,24) > RF(record,16) ||
                RF(record,32) > RF(record,24))
                throw new ArgumentException("Electrical source exceeds its initial finite charge.");
            values[i] = new(new(R64(record, 0)), new(R64(data, layout.BodyOffset((int)owner))),
                new(RF(record, 32)), new(RF(record, 40)), (ElectricalEnable)R32(record, 36));
        }
        var seen = new bool[layout.ContactWorks];
        for (var i = 0; i < layout.ElectricalBindings; i++)
        {
            var record = data.Slice(layout.ElectricalBindingOffset(i), ElectricalBindingBytes);
            var source = R32(record, 0); var storage = R32(record, 4);
            if (source >= count || storage >= (uint)layout.ContactWorks || seen[(int)storage] || !AllZero(record[8..]))
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
            if (!candidate.Slice(offset,32).SequenceEqual(previous.Slice(offset,32)))
                throw new ArgumentException("Electrical source declaration changed.");
            var limit = RF(candidate, offset + 20) * ElectricalSupply.PhaseSeconds * phaseCount;
            if (after[i].Debit.Value > limit + PhysicsElectricalRead.Residual(limit))
                throw new ArgumentException("Electrical source exceeded its phase power allowance.");
            double credit = 0, tolerance = PhysicsElectricalRead.Residual(before[i].Remaining.Value);
            for (var route = 0; route < layout.ElectricalBindings; route++)
            {
                var binding = layout.ElectricalBindingOffset(route);
                if (R32(candidate,binding) != i) continue;
                var slot = (int)R32(candidate,binding + 4);
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
