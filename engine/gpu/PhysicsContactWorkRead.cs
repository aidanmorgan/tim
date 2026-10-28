using System;

namespace CuriousContraptions.Gpu;

/// <summary>The committed contact-work owners of a read: each owner's identity, occurrence count and reservoir (stores only). Story 9A.23
/// D9: the per-target occurrence rows are device-private (<see cref="ContactWorkLedger"/>); a consumer learns of each hit from its
/// event ring Impact record, never from the read.</summary>
public readonly struct PhysicsContactWorkRead
{
    // Story 9R.5b: exactly the compiled work population; no fixed table capacity.
    private readonly ContactWorkRead[]? _values;
    public int Count => _values?.Length ?? 0;
    public PhysicsContactWorkRead(ReadOnlySpan<ContactWorkRead> values) : this(values.ToArray()) { }

    /// <summary>Takes ownership of a freshly decoded array (no copy); the caller never touches it again.</summary>
    internal static PhysicsContactWorkRead Adopt(ContactWorkRead[] values) => new(values);

    private PhysicsContactWorkRead(ContactWorkRead[] values)
    {
        _values = values;
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Id.Value >= values[i].Id.Value)
                throw new ArgumentException("Contact-work identities must be unique and ordered.");
        }
    }
    public ContactWorkRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool HasSameBits(PhysicsContactWorkRead other)
    {
        if (Count != other.Count) return false;
        for (var i=0; i<Count; i++)
            if (_values![i].Id != other._values![i].Id || _values![i].Owner != other._values![i].Owner ||
                _values![i].OccurrenceCount != other._values![i].OccurrenceCount ||
                !_values![i].RemainingEnergy.HasSameBits(other._values![i].RemainingEnergy) ||
                !_values![i].SuppliedEnergy.HasSameBits(other._values![i].SuppliedEnergy)) return false;
        return true;
    }

    /// <summary>The owners keep their identities and their occurrence counts never reverse. The reservoir accounting of each count
    /// increment against its debits is the device's, over the private occurrence rows (<see cref="ContactWorkLedger.ValidateAdvance"/>).</summary>
    public void ValidateAdvance(PhysicsContactWorkRead previous)
    {
        if (Count != previous.Count) throw new ArgumentException("Work population changed within an epoch.");
        for (var owner=0; owner<Count; owner++)
        {
            var before=previous[owner]; var after=this[owner];
            if (before.Id != after.Id || before.Owner != after.Owner || after.OccurrenceCount < before.OccurrenceCount)
                throw new ArgumentException("Work owner state reversed or changed identity.");
        }
    }

    public void ValidateScene(PhysicsSceneDeclaration scene)
    {
        if (Count != scene.ContactWorks.Length) throw new ArgumentException("Work-owner population differs from scene.");
        for (var i=0; i<Count; i++)
        {
            var declared=scene.ContactWorks[i];
            if (_values![i].Id != declared.Id || _values![i].Owner != declared.Owner ||
                _values![i].RemainingEnergy.Value > declared.InitialEnergy.Value)
                throw new ArgumentException("Work owner or reservoir differs from declaration.");
        }
    }
}

/// <summary>Story 9A.23 D9: the device-private contact-work ledger of one candidate or committed world state: the owners' stores and actual
/// occurrence rows, the last qualified hit of each encountered pair. It is read from the world and its interaction snapshot and
/// is the only input of the Impact records (<see cref="EventRecordProducer.Stage"/>) and of the reservoir accounting
/// (<see cref="ValidateAdvance"/>); it never leaves the Simulation worker.</summary>
public readonly struct ContactWorkLedger
{
    private readonly ContactWorkOccurrence[]? _occurrences;
    public PhysicsContactWorkRead Stores { get; }
    public int Count => Stores.Count;
    public int OccurrenceCount => _occurrences?.Length ?? 0;

    /// <summary>Takes ownership of freshly decoded arrays (no copy); the caller never touches them again.</summary>
    internal static ContactWorkLedger Adopt(ContactWorkRead[] values, ContactWorkOccurrence[] occurrences) =>
        new(PhysicsContactWorkRead.Adopt(values), occurrences);

    public ContactWorkLedger(ReadOnlySpan<ContactWorkRead> values, ReadOnlySpan<ContactWorkOccurrence> occurrences)
        : this(new PhysicsContactWorkRead(values), occurrences.ToArray()) { }

    private ContactWorkLedger(PhysicsContactWorkRead stores, ContactWorkOccurrence[] occurrences)
    {
        Stores = stores; _occurrences = occurrences;
        for (var i = 0; i < occurrences.Length; i++)
        {
            var value = occurrences[i]; value.Validate();
            if (value.Work.Value >= Count || value.Sequence > stores[(int)value.Work.Value].OccurrenceCount ||
                (i > 0 && (occurrences[i-1].Work.Value > value.Work.Value ||
                    (occurrences[i-1].Work == value.Work && occurrences[i-1].Target.Value >= value.Target.Value))))
                throw new ArgumentException("Contact targets require ordered unique identities and owned sequences.");
            // Occurrences are ordered by owner, so only this owner's earlier rows can share its sequence.
            for (var j=i-1; j>=0 && occurrences[j].Work == value.Work; j--)
                if (value.Sequence != 0 && occurrences[j].Sequence == value.Sequence)
                    throw new ArgumentException("A contact-work owner sequence was duplicated.");
        }
    }
    public ContactWorkRead this[int index] => Stores[index];
    public ContactWorkOccurrence Occurrence(int index) => index >= 0 && index < OccurrenceCount ? _occurrences![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool TryOccurrence(ContactWorkSlot work,GpuBodyId target,out ContactWorkOccurrence occurrence)
    {
        for(var i=0;i<OccurrenceCount;i++)
            if(_occurrences![i].Work==work && _occurrences[i].Target==target) { occurrence=_occurrences[i]; return true; }
        occurrence=default; return false;
    }

    /// <summary>Every owner's count increment is exactly its new occurrence rows, each later than the row it replaces, and the reservoir
    /// moved by exactly their debits (and any recharge in the same commit).</summary>
    public void ValidateAdvance(ContactWorkLedger previous)
    {
        Stores.ValidateAdvance(previous.Stores);
        for(var i=0;i<previous.OccurrenceCount;i++)
        {
            var prior=previous.Occurrence(i);
            if(!TryOccurrence(prior.Work,prior.Target,out _)) throw new ArgumentException("Earned work state was lost.");
        }
        for (var owner=0; owner<Count; owner++)
        {
            var before=previous[owner]; var after=this[owner];
            uint changed=0; float debit=0, sumRounding=0;
            for (var i=0; i<OccurrenceCount; i++)
            {
                var last=Occurrence(i);
                previous.TryOccurrence(last.Work,last.Target,out var first);
                if (last.Work.Value != owner) continue;
                if (last.Sequence == first.Sequence)
                {
                    if (!last.HasSameBits(first)) throw new ArgumentException("Unchanged occurrence mutated.");
                    continue;
                }
                if (last.Sequence <= before.OccurrenceCount || last.Sequence > after.OccurrenceCount ||
                    last.EventOrdinal < first.EventOrdinal ||
                    (last.EventOrdinal == first.EventOrdinal && last.EventPhase < first.EventPhase))
                    throw new ArgumentException("Contact occurrence was skipped or reversed.");
                changed++; debit+=last.Debit.Value;
                sumRounding += PhysicsElectricalRead.Residual(debit);
            }
            // Unique event sequences plus this cardinality prove the complete contiguous owner interval.
            if (after.OccurrenceCount-before.OccurrenceCount != changed)
                throw new ArgumentException("Committed contact occurrences were lost.");
            var removed=before.RemainingEnergy.Value + after.SuppliedEnergy.Value - after.RemainingEnergy.Value;
            // Recharge and impacts are separate finite transfers in the same commit.
            var tolerance = after.SuppliedEnergy.Value > 0
                ? PhysicsElectricalRead.Residual(before.RemainingEnergy.Value) + PhysicsElectricalRead.Residual(after.RemainingEnergy.Value) +
                    PhysicsElectricalRead.Residual(after.SuppliedEnergy.Value) + debit / 8388608
                : debit / 8388608 + changed * float.Epsilon;
            if (MathF.Abs(removed-debit)>tolerance + sumRounding + PhysicsElectricalRead.Residual(removed) ||
                (debit==0 && after.SuppliedEnergy.Value==0 && !before.RemainingEnergy.HasSameBits(after.RemainingEnergy)))
                throw new ArgumentException("Shared reservoir does not account for every emitted debit.");
        }
    }

    /// <summary>The ledger is exactly the scene's owners and one row per (owner, eligible dynamic target) of the read's bodies, each hit
    /// on an owner collider at or above its declared threshold.</summary>
    public void ValidateScene(PhysicsSceneDeclaration scene, PhysicsBodyReadSet bodies)
    {
        Stores.ValidateScene(scene);
        for(var i=0;i<OccurrenceCount;i++)
        {
            var value=Occurrence(i);
            var declared=scene.ContactWorks[(int)value.Work.Value];
            if(!bodies.TryGet(value.Target,out _)) throw new ArgumentException("Work target is absent from the read.");
            var admitted=false;
            foreach(var body in scene.Bodies)
                if(body.Id==value.Target && body.Motion==RigidMotionKind.Dynamic && declared.Targets.Contains(body.Id,body.Payload)) admitted=true;
            if(!admitted || value.Collider.Value>=(uint)scene.Colliders.Length ||
                scene.Colliders[(int)value.Collider.Value].Body!=declared.Owner || value.ApproachSpeed.Value<declared.Threshold.Value)
                throw new ArgumentException("Contact occurrence violates its declared ownership or threshold.");
        }
    }

    /// <summary>An admitted world has hit nothing yet.</summary>
    public void ValidateAdmission()
    {
        if(OccurrenceCount!=0) throw new ArgumentException("Admission retained contact occurrence.");
    }
}
