using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

/// <summary>Ordered observations from one committed physical revision.</summary>
public readonly struct PhysicsAirflowRead
{
    private readonly ImmutableArray<AirflowSourceRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public AirflowSourceRead this[int index] => index >= 0 && index < Count
        ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));

    public PhysicsAirflowRead(ReadOnlySpan<AirflowSourceRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value)
                throw new ArgumentException("Airflow observations require ordered unique identities.");
        }
    }

    public bool HasSameValues(PhysicsAirflowRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++) if (this[i] != other[i]) return false;
        return true;
    }

    public bool TryGet(AirflowSourceId id, out AirflowSourceRead value)
    {
        for (var i = 0; i < Count; i++)
            if (this[i].Id == id) { value = this[i]; return true; }
        value = default;
        return false;
    }
}
