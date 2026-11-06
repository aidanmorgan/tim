using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

/// <summary>The committed contact-work owners of a read: each owner's identity, occurrence count and reservoir (stores only). Story 9A.23
/// D9: the per-target occurrence rows are device-private (retained by the device); a consumer learns of each hit from its
/// event ring Impact record, never from the read.</summary>
public readonly struct PhysicsContactWorkRead
{
    // Story 9R.5b: exactly the compiled work population; no fixed table capacity.
    private readonly ImmutableArray<ContactWorkRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public PhysicsContactWorkRead(ReadOnlySpan<ContactWorkRead> values) : this(ImmutableArray.Create(values)) { }

    /// <summary>Retains immutable, identity-ordered values.</summary>
    public static PhysicsContactWorkRead FromImmutable(ImmutableArray<ContactWorkRead> values) => new(values);

    private PhysicsContactWorkRead(ImmutableArray<ContactWorkRead> values)
    {
        if (values.IsDefault) values = [];
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

}
