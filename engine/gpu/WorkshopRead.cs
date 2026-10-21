using System;

namespace CuriousContraptions.Gpu;

/// <summary>Exact committed read content; inline storage and decoded motion wrappers have no reference authority.</summary>
public readonly partial record struct WorkshopRead
{
    // Per-commit balances cannot be coalesced: consumers reconcile each credit and debit.
    public bool RequiresReliableRead(WorkshopRead previous)
    {
        for (var i = 0; i < ContactWorks.Count; i++)
            if (ContactWorks[i].SuppliedEnergy.Value > 0 || i >= previous.ContactWorks.Count ||
                ContactWorks[i].OccurrenceCount > previous.ContactWorks[i].OccurrenceCount) return true;
        for (var i = 0; i < Electrical.Count; i++)
            if (Electrical[i].Debit.Value > 0 || i >= previous.Electrical.Count ||
                Electrical[i].Enabled != previous.Electrical[i].Enabled) return true;
        return false;
    }

    public bool HasSameContent(WorkshopRead other) => Epoch == other.Epoch && Tick == other.Tick &&
        Revision == other.Revision && Capture == other.Capture && SamePhysicalContent(this, other);
    public bool Equals(WorkshopRead other) => HasSameContent(other);
    // Identity hashes avoid rehashing a full motion record. Equal content always shares these identities.
    public override int GetHashCode() => HashCode.Combine(Epoch, Tick, Revision, Capture);

    internal static bool SamePhysicalContent(WorkshopRead first, WorkshopRead second)
    {
        if (!first.Bodies.HasSameBits(second.Bodies) ||
            first.Captures.Count != second.Captures.Count || first.Activations.Count != second.Activations.Count ||
            first.Timers.Count != second.Timers.Count || !first.ContactWorks.HasSameBits(second.ContactWorks) || !first.Electrical.HasSameBits(second.Electrical) ||
            !first.Memory.HasSameBits(second.Memory) || !first.Presentation.HasSameBits(second.Presentation)) return false;
        if (first.Motion is { } m1 && second.Motion is { } m2)
        {
            if (!m1.Bytes.SequenceEqual(m2.Bytes)) return false;
        }
        for (var i = 0; i < first.Captures.Count; i++)
        {
            var left = first.Captures[i]; var right = second.Captures[i];
            if (left.Sensor != right.Sensor || left.Phase != right.Phase || left.EventOrdinal != right.EventOrdinal ||
                !HalfBits.Equal(left.EventPhase, right.EventPhase)) return false;
        }
        for (var i = 0; i < first.Activations.Count; i++)
        {
            var left = first.Activations[i]; var right = second.Activations[i];
            if (left != right || !F32Bits.Equal(left.EventPhase, right.EventPhase) ||
                !F32Bits.Equal(left.ApproachSpeed.Value, right.ApproachSpeed.Value)) return false;
        }
        for (var i = 0; i < first.Timers.Count; i++) if (first.Timers[i] != second.Timers[i]) return false;
        return true;
    }

}
