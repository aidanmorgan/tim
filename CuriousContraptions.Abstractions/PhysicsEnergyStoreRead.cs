using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

/// <summary>Identity-ordered committed reservoirs, independent of their discharge mechanisms.</summary>
public readonly struct PhysicsEnergyStoreRead
{
    private readonly ImmutableArray<EnergyStoreRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;

    public PhysicsEnergyStoreRead(ReadOnlySpan<EnergyStoreRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value)
                throw new ArgumentException("Energy store identities must be unique and ordered.");
        }
    }

    public EnergyStoreRead this[int index] => index >= 0 && index < Count ? _values[index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool HasSameBits(PhysicsEnergyStoreRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
        {
            var a = this[i]; var b = other[i];
            if (a.Id != b.Id || a.Owner != b.Owner || !a.Energy.HasSameBits(b.Energy) ||
                !a.Supplied.HasSameBits(b.Supplied) || !a.Released.HasSameBits(b.Released) ||
                !a.Dissipated.HasSameBits(b.Dissipated)) return false;
        }
        return true;
    }
}
