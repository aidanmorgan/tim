using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int ElectricalSourceBytes = 64;
    public const int ElectricalSourcesOffset = CacheOffset + CacheCapacity * CacheBytes;
    public const int ElectricalBindingBytes = 16;
    public const int ElectricalBindingsOffset = ElectricalSourcesOffset + ElectricalSupplyPlan.SourceCapacity * ElectricalSourceBytes;

    private static void WriteEnergy(Span<byte> bytes, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value);

    private static void WriteElectricalAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        U32(data, 120, (uint)scene.Electrical.Sources.Length);
        U32(data, 124, (uint)scene.Electrical.Bindings.Length);
        for (var i = 0; i < scene.Electrical.Sources.Length; i++)
        {
            var source = scene.Electrical.Sources[i];
            var record = data.Slice(ElectricalSourcesOffset + i * ElectricalSourceBytes, ElectricalSourceBytes);
            U64(record, 0, source.Id.Value);
            var body = 0;
            while (scene.Bodies[body].Id != source.Owner) body++;
            U32(record, 8, (uint)body); U32(record, 12, (uint)source.Enabled);
            WriteEnergy(record, 16, source.Capacity.Value); WriteEnergy(record, 20, source.MaximumPower.Value);
            WriteEnergy(record, 24, source.InitialEnergy.Value); WriteEnergy(record, 32, source.InitialEnergy.Value);
            U32(record, 36, (uint)source.Enabled);
        }
        for (var i = 0; i < scene.Electrical.Bindings.Length; i++)
        {
            var binding = scene.Electrical.Bindings[i]; var source = 0; var storage = 0;
            while (scene.Electrical.Sources[source].Id != binding.Source) source++;
            while (scene.ContactWorks[storage].Id != binding.Storage) storage++;
            var record = data.Slice(ElectricalBindingsOffset + i * ElectricalBindingBytes, ElectricalBindingBytes);
            U32(record, 0, (uint)source); U32(record, 4, (uint)storage);
        }
    }

    public static PhysicsElectricalRead ReadElectrical(ReadOnlySpan<byte> data)
    {
        Header(data);
        var count = (int)R32(data, 120);
        Span<ElectricalSourceRead> values = stackalloc ElectricalSourceRead[ElectricalSupplyPlan.SourceCapacity];
        for (var i = 0; i < count; i++)
        {
            var record = data.Slice(ElectricalSourcesOffset + i * ElectricalSourceBytes, ElectricalSourceBytes);
            var owner = R32(record, 8);
            if (owner >= R32(data, 12) || !AllZero(record[28..32]) || !AllZero(record[44..]) ||
                R32(record, 12) > (uint)ElectricalEnable.Enabled || R32(record, 36) > (uint)ElectricalEnable.Enabled)
                throw new ArgumentException("Invalid electrical source record.");
            new ElectricalSourceSettings(new(RF(record, 16)), new(RF(record, 20)),
                1f, (ElectricalEnable)R32(record, 12)).Validate();
            if (!float.IsFinite(RF(record,24)) || RF(record,24) < 0 || RF(record,24) > RF(record,16) ||
                RF(record,32) > RF(record,24))
                throw new ArgumentException("Electrical source exceeds its initial finite charge.");
            values[i] = new(new(R64(record, 0)), new(R64(data, BodiesOffset + (int)owner * BodyBytes)),
                new(RF(record, 32)), new(RF(record, 40)), (ElectricalEnable)R32(record, 36));
        }
        if (!AllZero(data[(ElectricalSourcesOffset + count * ElectricalSourceBytes)..ElectricalBindingsOffset]))
            throw new ArgumentException("Unused electrical source slot changed.");
        Span<bool> seen = stackalloc bool[PhysicsSceneDeclaration.ContactWorkCapacity];
        var routes = (int)R32(data, 124);
        for (var i = 0; i < routes; i++)
        {
            var record = data.Slice(ElectricalBindingsOffset + i * ElectricalBindingBytes, ElectricalBindingBytes);
            var source = R32(record, 0); var storage = R32(record, 4);
            if (source >= count || storage >= R32(data, 104) || seen[(int)storage] || !AllZero(record[8..]))
                throw new ArgumentException("Invalid or ambiguous electrical storage route.");
            seen[(int)storage] = true;
        }
        if (!AllZero(data[(ElectricalBindingsOffset + routes * ElectricalBindingBytes)..]))
            throw new ArgumentException("Unused electrical route slot changed.");
        return new(values[..count]);
    }

    private static void ValidateElectricalCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> previous)
    {
        var before = ReadElectrical(previous); var after = ReadElectrical(candidate);
        after.ValidateAdvance(before);
        if (!candidate[ElectricalBindingsOffset..].SequenceEqual(previous[ElectricalBindingsOffset..]))
            throw new ArgumentException("Electrical routes changed during a Run.");
        var phaseCount = (R32(candidate,88) + 3) / 4 - (R32(previous,88) + 3) / 4;
        var works = ReadContactWorks(candidate);
        Span<bool> supplied = stackalloc bool[PhysicsSceneDeclaration.ContactWorkCapacity];
        for (var i = 0; i < after.Count; i++)
        {
            var offset = ElectricalSourcesOffset + i * ElectricalSourceBytes;
            if (!candidate.Slice(offset,32).SequenceEqual(previous.Slice(offset,32)))
                throw new ArgumentException("Electrical source declaration changed.");
            var limit = RF(candidate, offset + 20) * ElectricalSupply.PhaseSeconds * phaseCount;
            if (after[i].Debit.Value > limit + PhysicsElectricalRead.Residual(limit))
                throw new ArgumentException("Electrical source exceeded its phase power allowance.");
            double credit = 0, tolerance = PhysicsElectricalRead.Residual(before[i].Remaining.Value);
            for (var route = 0; route < R32(candidate,124); route++)
            {
                var binding = ElectricalBindingsOffset + route * ElectricalBindingBytes;
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
