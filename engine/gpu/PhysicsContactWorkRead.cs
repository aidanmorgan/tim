using System;

namespace CuriousContraptions.Gpu;


public readonly struct PhysicsContactWorkRead
{
    public const int OccurrenceCapacity = PhysicsSceneDeclaration.ContactWorkCapacity * PhysicsBodyReadSet.Capacity;
    private readonly ContactWorkRead[]? _values;
    private readonly ContactWorkOccurrence[]? _occurrences;
    public byte Count { get; }
    public byte OccurrenceCount { get; }
    public PhysicsContactWorkRead(ReadOnlySpan<ContactWorkRead> values, ReadOnlySpan<ContactWorkOccurrence> occurrences)
    {
        if (values.Length > PhysicsSceneDeclaration.ContactWorkCapacity || occurrences.Length > OccurrenceCapacity)
            throw new ArgumentException("Contact-work read capacity exceeded.");
        Count = checked((byte)values.Length); OccurrenceCount = checked((byte)occurrences.Length);
        _values = new ContactWorkRead[values.Length]; _occurrences = new ContactWorkOccurrence[occurrences.Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Id.Value >= values[i].Id.Value)
                throw new ArgumentException("Contact-work identities must be unique and ordered.");
            _values![i] = values[i];
        }
        for (var i = 0; i < occurrences.Length; i++)
        {
            var value = occurrences[i]; value.Validate();
            if (value.Work.Value >= Count || value.Sequence > values[value.Work.Value].OccurrenceCount ||
                (i > 0 && (occurrences[i-1].Work.Value > value.Work.Value ||
                    (occurrences[i-1].Work == value.Work && occurrences[i-1].Target.Value >= value.Target.Value))))
                throw new ArgumentException("Contact targets require ordered unique identities and owned sequences.");
            for (var j=0; j<i; j++)
                if (value.Sequence != 0 && occurrences[j].Work == value.Work && occurrences[j].Sequence == value.Sequence)
                    throw new ArgumentException("A contact-work owner sequence was duplicated.");
            _occurrences![i] = value;
        }
    }
    public ContactWorkRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));
    public ContactWorkOccurrence Occurrence(int index) => index >= 0 && index < OccurrenceCount ? _occurrences![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool HasSameBits(PhysicsContactWorkRead other)
    {
        if (Count != other.Count || OccurrenceCount != other.OccurrenceCount) return false;
        for (var i=0; i<Count; i++)
            if (_values![i].Id != other._values![i].Id || _values![i].Owner != other._values![i].Owner ||
                _values![i].OccurrenceCount != other._values![i].OccurrenceCount ||
                !HalfBits.Equal(_values![i].RemainingEnergy.Value, other._values![i].RemainingEnergy.Value)) return false;
        for (var i=0; i<OccurrenceCount; i++) if (!_occurrences![i].HasSameBits(other._occurrences![i])) return false;
        return true;
    }


    public void ValidateAdvance(PhysicsContactWorkRead previous)
    {
        if (Count != previous.Count || OccurrenceCount != previous.OccurrenceCount)
            throw new ArgumentException("Work population changed within an epoch.");
        for (var owner=0; owner<Count; owner++)
        {
            var before=previous[owner]; var after=this[owner];
            if (before.Id != after.Id || before.Owner != after.Owner ||
                after.OccurrenceCount < before.OccurrenceCount || after.RemainingEnergy.Value > before.RemainingEnergy.Value)
                throw new ArgumentException("Work owner state reversed or changed identity.");
            uint changed=0; double debit=0;
            for (var i=0; i<OccurrenceCount; i++)
            {
                var first=previous.Occurrence(i); var last=Occurrence(i);
                if (first.Work != last.Work || first.Target != last.Target)
                    throw new ArgumentException("Work target identity changed.");
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
                changed++; debit+=(double)last.Debit.Value;
            }
            // Unique event sequences plus this cardinality prove the complete contiguous owner interval.
            if (after.OccurrenceCount-before.OccurrenceCount != changed)
                throw new ArgumentException("Committed contact occurrences were lost.");
            var removed=(double)before.RemainingEnergy.Value-(double)after.RemainingEnergy.Value;
            // Each stored event debit is the Half projection of an actual quantized reservoir decrease.
            if ((debit>0 && removed<=0) || Math.Abs(removed-debit)>debit/1024+changed*Math.ScaleB(1,-25) ||
                (debit==0 && !HalfBits.Equal(before.RemainingEnergy.Value,after.RemainingEnergy.Value)))
                throw new ArgumentException("Shared reservoir does not account for every emitted debit.");
        }
    }

    public void ValidateScene(PhysicsSceneDeclaration scene, PhysicsBodyReadSet bodies)
    {
        if (Count != scene.ContactWorks.Length) throw new ArgumentException("Work-owner population differs from scene.");
        var occurrence=0;
        for (var i=0; i<Count; i++)
        {
            var declared=scene.ContactWorks[i];
            if (_values![i].Id != declared.Id || _values![i].Owner != declared.Owner ||
                _values![i].RemainingEnergy.Value > declared.InitialEnergy.Value)
                throw new ArgumentException("Work owner or reservoir differs from declaration.");
            for (var body=0; body<bodies.Count; body++)
            {
                var id=bodies[body].Body.Id;
                if (!declared.Targets.Contains(id)) continue;
                if (occurrence >= OccurrenceCount || _occurrences![occurrence].Work.Value != i ||
                    _occurrences![occurrence].Target != id)
                    throw new ArgumentException("Contact target population differs from declaration.");
                var value=_occurrences![occurrence++];
                if (value.Sequence != 0 && (value.Collider.Value >= scene.Colliders.Length ||
                    scene.Colliders[value.Collider.Value].Body != declared.Owner))
                    throw new ArgumentException("Contact occurrence collider does not belong to its owner.");
            }
        }
        if (occurrence != OccurrenceCount) throw new ArgumentException("Contact read contains a foreign target.");
    }
}
