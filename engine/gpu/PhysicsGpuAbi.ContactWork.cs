using System;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public static ContactWorkRead ReadContactWork(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, 104))
            throw new ArgumentException("Invalid contact-work read.");
        var record = data.Slice(ContactWorksOffset + slot * ContactWorkBytes, ContactWorkBytes);
        var owner = R32(record, 8); var target = R32(record, 12); var count = R32(record, 32);
        if (R64(record, 0) == 0 || owner >= R32(data, 12) || target != R32(data, 28) || target >= R32(data, 12) ||
            owner == target || (RigidMotionKind)R32(data, BodiesOffset + checked((int)owner) * BodyBytes + 8) != RigidMotionKind.Static ||
            R32(record, 24) is < 9 or > 14400 || count > 1601 ||
            !AllZero(record[22..24]) || !AllZero(record[28..32]) || !AllZero(record[52..]))
            throw new ArgumentException("Invalid contact-work identity, capacity or padding.");
        PhysicsDeclarationBounds.Range(RH(record, 16), (Half)0, (Half)20);
        PhysicsDeclarationBounds.Range(RH(record, 18), (Half)0, (Half)200);
        PhysicsDeclarationBounds.Range(RH(record, 20), (Half)0, (Half)64);
        PhysicsDeclarationBounds.Range(RH(record, 48), (Half)0, RH(record, 18));
        PhysicsDeclarationBounds.Range(RH(record, 50), (Half)0, RH(record, 18));
        var collider = default(GpuColliderId);
        if (count == 0)
        {
            if (!AllZero(record[36..48]) || !AllZero(record[50..52]) || !record[18..20].SequenceEqual(record[48..50]))
                throw new ArgumentException("Unused contact work retained an event or spent its preload.");
        }
        else
        {
            var colliderSlot = R32(record, 36);
            if (colliderSlot >= R32(data, 16) ||
                R32(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes + 8) != owner ||
                !AdmittedTime(R32(record, 40), RH(record, 44), R32(data, 88)))
                throw new ArgumentException("Invalid work event collider or timestamp.");
            PhysicsDeclarationBounds.Range(RH(record, 46), RH(record, 20), (Half)128);
            collider = new(R64(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes));
        }
        return new(new(R64(record, 0)), new(R64(data, BodiesOffset + checked((int)owner) * BodyBytes)),
            new(R64(data, BodiesOffset + checked((int)target) * BodyBytes)), count, collider,
            R32(record, 40), RH(record, 44), new(RH(record, 46)), new(RH(record, 48)), new(RH(record, 50)));
    }

    private static void ValidateContactWorkCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source,
        SimulationTick expectedTick)
    {
        var count = checked((int)R32(source, 104));
        for (var i = 0; i < count; i++)
        {
            var offset = ContactWorksOffset + i * ContactWorkBytes;
            var before = source.Slice(offset, ContactWorkBytes); var after = candidate.Slice(offset, ContactWorkBytes);
            if (!before[..32].SequenceEqual(after[..32])) throw new ArgumentException("Contact-work declaration changed.");
            var previous = ReadContactWork(source, i); var current = ReadContactWork(candidate, i);
            if (current.OccurrenceCount == previous.OccurrenceCount)
            {
                if (!before.SequenceEqual(after)) throw new ArgumentException("Work state changed without an accepted impact.");
                continue;
            }
            if (expectedTick.Value == 0 || current.OccurrenceCount != previous.OccurrenceCount + 1 ||
                Before(current.EventOrdinal, current.EventPhase, R32(source, 88), (Half)0) ||
                current.RemainingEnergy.Value > previous.RemainingEnergy.Value ||
                current.LastDebit.Value > previous.RemainingEnergy.Value)
                throw new ArgumentException("Work event or debit does not belong to this candidate.");
            if (previous.OccurrenceCount != 0 &&
                Before(current.EventOrdinal, current.EventPhase, checked(previous.EventOrdinal + R32(before, 24)), previous.EventPhase))
                throw new ArgumentException("Contact work occurred before its physical cooldown.");
            // Wider arithmetic validates already-canonical storage; it never computes a response.
            var removed = (double)previous.RemainingEnergy.Value - (double)current.RemainingEnergy.Value;
            if (removed == 0 && current.LastDebit.Value != (Half)0 ||
                removed > 0 && current.LastDebit.Value <= (Half)0 ||
                current.LastDebit.Value != (Half)removed)
                throw new ArgumentException("Reported canonical debit differs from the committed reservoir change.");
        }
        if (!AllZero(candidate[(ContactWorksOffset + count * ContactWorkBytes)..MotionOffset]))
            throw new ArgumentException("Unused work slots changed.");
    }
}
