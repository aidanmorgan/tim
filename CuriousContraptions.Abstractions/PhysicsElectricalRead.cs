using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

public readonly record struct ElectricalSourceRead(ElectricalSourceId Id, GpuBodyId Owner, Joules Remaining,
    Joules Debit, ElectricalEnable Enabled)
{
    public bool Available => Enabled == ElectricalEnable.Enabled && Remaining.Value > 0;
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || !Enum.IsDefined(Enabled))
            throw new ArgumentException("Invalid electrical observation identity.");
        Remaining.Validate(CuriousContraptions.Abstractions.Electrical.ElectricalSourceEnvelope.MaximumCapacity);
        Debit.Validate(8f);
    }
}

public readonly struct PhysicsElectricalRead
{
    // Story 9R.5b: exactly the compiled source population; no fixed source capacity.
    private readonly ImmutableArray<ElectricalSourceRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public ElectricalSourceRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));
    public PhysicsElectricalRead(ReadOnlySpan<ElectricalSourceRead> values) : this(ImmutableArray.Create(values)) { }

    /// <summary>Retains immutable, identity-ordered values.</summary>
    public static PhysicsElectricalRead FromImmutable(ImmutableArray<ElectricalSourceRead> values) => new(values);

    private PhysicsElectricalRead(ImmutableArray<ElectricalSourceRead> values)
    {
        if (values.IsDefault) values = [];
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i > 0 && values[i - 1].Id.Value >= values[i].Id.Value)
                throw new ArgumentException("Electrical observations require ordered unique identities.");
        }
        _values = values;
    }
    public bool HasSameBits(PhysicsElectricalRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (this[i].Id != other[i].Id || this[i].Owner != other[i].Owner || this[i].Enabled != other[i].Enabled ||
                !this[i].Remaining.HasSameBits(other[i].Remaining) || !this[i].Debit.HasSameBits(other[i].Debit)) return false;
        return true;
    }
}
