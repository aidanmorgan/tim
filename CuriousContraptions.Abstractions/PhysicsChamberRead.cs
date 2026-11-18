using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

/// <summary>Identity-ordered committed region occupancy, including shared departure ownership.</summary>
public readonly struct PhysicsChamberRead
{
    private readonly ImmutableArray<ChamberRegionRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public PhysicsChamberRead(ReadOnlySpan<ChamberRegionRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value) throw new ArgumentException("Chamber identities must be unique and ordered.");
        }
    }
    public ChamberRegionRead this[int index] => index >= 0 && index < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
    public bool HasSameBits(PhysicsChamberRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++) if (this[i] != other[i]) return false;
        return true;
    }
}

/// <summary>Identity-ordered finite-work request outcomes, independently observed from contact discharges.</summary>
public readonly struct PhysicsFiniteWorkActuatorRead
{
    private readonly ImmutableArray<FiniteWorkActuatorRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public PhysicsFiniteWorkActuatorRead(ReadOnlySpan<FiniteWorkActuatorRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value) throw new ArgumentException("Actuator identities must be unique and ordered.");
        }
    }
    public FiniteWorkActuatorRead this[int index] => index >= 0 && index < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
    public bool HasSameBits(PhysicsFiniteWorkActuatorRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (this[i] != other[i] || !this[i].LastDebit.HasSameBits(other[i].LastDebit)) return false;
        return true;
    }
}
