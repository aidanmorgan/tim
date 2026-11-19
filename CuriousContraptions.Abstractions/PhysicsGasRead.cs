using System;
using System.Collections.Immutable;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct GasStoreRead(GasStoreId Id, GpuBodyId Body, GasEnvelopeState State, GasOutletPhase Outlet,
    float EnthalpyOut, float BoundaryWork, float ElasticReleased, Vector3 MomentumOut, float KineticOut,
    float PreparationWork, float PreparationElastic)
{
    public void Validate()
    {
        if (Id.Value == 0 || Body.Value == 0 || !Enum.IsDefined(Outlet) ||
            !float.IsFinite(State.Mass) || !float.IsFinite(State.Volume) || !float.IsFinite(State.Energy) ||
            State.Mass < 0 || State.Volume < 0 || State.Energy < 0 ||
            (State.Mass == 0) != (State.Volume == 0) || (State.Mass == 0) != (State.Energy == 0))
            throw new ArgumentException("Invalid committed gas inventory.");
        foreach (var value in new[] { EnthalpyOut, BoundaryWork, ElasticReleased, KineticOut })
            GasMaterial.Nonnegative(value);
        foreach (var value in new[] { MomentumOut.X, MomentumOut.Y, MomentumOut.Z, PreparationWork, PreparationElastic })
            if (!float.IsFinite(value)) throw new ArgumentException("Invalid committed gas work.");
    }
}

public readonly struct PhysicsGasRead
{
    private readonly ImmutableArray<GasStoreRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public GasStoreRead this[int index] => index >= 0 && index < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
    public PhysicsGasRead(ReadOnlySpan<GasStoreRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value)
                throw new ArgumentException("Gas observations require ordered unique identities.");
        }
    }
    public bool HasSameValues(PhysicsGasRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++) if (this[i] != other[i]) return false;
        return true;
    }
    public bool TryGet(GasStoreId id, out GasStoreRead value)
    {
        for (var i = 0; i < Count; i++)
            if (this[i].Id == id) { value = this[i]; return true; }
        value = default; return false;
    }
}

public readonly struct PhysicsIntegrityRead
{
    private readonly ImmutableArray<IntegrityRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;
    public IntegrityRead this[int index] => index >= 0 && index < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
    public PhysicsIntegrityRead(ReadOnlySpan<IntegrityRead> values)
    {
        _values = ImmutableArray.Create(values);
        for (var i = 0; i < _values.Length; i++)
        {
            _values[i].Validate();
            if (i > 0 && _values[i - 1].Id.Value >= _values[i].Id.Value)
                throw new ArgumentException("Integrity observations require ordered unique identities.");
        }
    }
    public bool HasSameValues(PhysicsIntegrityRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++) if (this[i] != other[i]) return false;
        return true;
    }
    public bool TryGet(IntegrityObservationId id, out IntegrityRead value)
    {
        for (var i = 0; i < Count; i++)
            if (this[i].Id == id) { value = this[i]; return true; }
        value = default; return false;
    }
}
