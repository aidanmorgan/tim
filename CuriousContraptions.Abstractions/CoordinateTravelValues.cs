using System;
using System.Collections.Immutable;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct LocalCoordinateTravelId(uint Value);
public readonly record struct CoordinateTravelId(ulong Value);
public enum CoordinateTravelPhase : byte { Accumulating, Reached }

/// <summary>One first-occurrence observer of absolute accepted revolute travel, in radians.</summary>
public readonly record struct CoordinateTravelDescriptor(LocalCoordinateTravelId Id,
    CoordinateReference Coordinate, float Threshold);

public readonly record struct CoordinateTravelDeclaration(CoordinateTravelId Id, GpuBodyId Owner,
    RevoluteConstraintId Coordinate, float Threshold)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Coordinate.Value == 0 || !float.IsFinite(Threshold) || Threshold <= 0)
            throw new ArgumentException("Invalid coordinate travel observation.");
    }
}

public readonly record struct CoordinateTravelRead(CoordinateTravelId Id, GpuBodyId Owner, float Threshold,
    float Travel, CoordinateTravelPhase Phase, uint Ordinal)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || !float.IsFinite(Threshold) || Threshold <= 0 ||
            !float.IsFinite(Travel) || Travel < 0 || Travel > Threshold || !Enum.IsDefined(Phase) ||
            (Phase == CoordinateTravelPhase.Reached ? Travel != Threshold || Ordinal == 0 : Travel >= Threshold || Ordinal != 0))
            throw new ArgumentException("Invalid committed coordinate travel.");
    }
}

public readonly struct PhysicsCoordinateTravelRead
{
    private readonly ImmutableArray<CoordinateTravelRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public CoordinateTravelRead this[int index] => index >= 0 && index < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
    public PhysicsCoordinateTravelRead(ReadOnlySpan<CoordinateTravelRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value)
                throw new ArgumentException("Coordinate travel observations require ordered unique identities.");
        }
    }
    public bool HasSameValues(PhysicsCoordinateTravelRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++) if (this[i] != other[i]) return false;
        return true;
    }
}
