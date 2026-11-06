using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

public readonly record struct PhysicsBodyRead(CanonicalBody Body, CanonicalRotation Rotation,
    AngularVelocity AngularVelocity, MetreVector LocalCentreOfMass)
{
    public void Validate()
    {
        Body.Validate(); Rotation.ValidateCommitted(); AngularVelocity.Validate();
        PhysicsDeclarationBounds.Vector(LocalCentreOfMass.X, LocalCentreOfMass.Y, LocalCentreOfMass.Z, 16f);
    }

    public bool HasSameBits(PhysicsBodyRead other) =>
        Body.Id == other.Body.Id && Body.Epoch == other.Body.Epoch && Body.Tick == other.Body.Tick &&
        Body.Cell == other.Body.Cell && F32Bits.Equal(Body.Local, other.Body.Local) &&
        Body.Velocity.HasSameBits(other.Body.Velocity) &&
        F32Bits.Equal(Rotation.X, other.Rotation.X) && F32Bits.Equal(Rotation.Y, other.Rotation.Y) &&
        F32Bits.Equal(Rotation.Z, other.Rotation.Z) && F32Bits.Equal(Rotation.W, other.Rotation.W) &&
        AngularVelocity.HasSameBits(other.AngularVelocity) &&
        F32Bits.Equal(LocalCentreOfMass, other.LocalCentreOfMass);
}


/// <summary>One immutable, identity-ordered complete body sample at one committed time. Exactly the world's dynamic population:
/// there is no body capacity (owner decision, 20 Oct 2026: "the physics body table needs to be infinite").</summary>
public readonly struct PhysicsBodyReadSet
{
    private readonly ImmutableArray<PhysicsBodyRead> _values;
    public int Count => _values.IsDefault ? 0 : _values.Length;

    public PhysicsBodyReadSet(ReadOnlySpan<PhysicsBodyRead> values) : this(ImmutableArray.Create(values)) { }

    /// <summary>Retains an immutable ordered body population.</summary>
    public static PhysicsBodyReadSet FromImmutable(ImmutableArray<PhysicsBodyRead> values) => new(values);

    private PhysicsBodyReadSet(ImmutableArray<PhysicsBodyRead> values)
    {
        if (values.IsDefault) values = [];
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i > 0 && (values[i - 1].Body.Id.Value >= values[i].Body.Id.Value ||
                values[0].Body.Epoch != values[i].Body.Epoch || values[0].Body.Tick != values[i].Body.Tick))
                throw new ArgumentException("Body reads require ordered unique identities and one committed time.");
        }
        _values = values;
    }

    public PhysicsBodyRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool TryGet(GpuBodyId id, out PhysicsBodyRead value)
    {
        // Identities are strictly ordered, so the search is logarithmic in the (unbounded) population.
        int low = 0, high = Count - 1;
        while (low <= high)
        {
            var middle = low + (high - low) / 2; var key = _values![middle].Body.Id.Value;
            if (key == id.Value) { value = _values![middle]; return true; }
            if (key < id.Value) low = middle + 1; else high = middle - 1;
        }
        value = default; return false;
    }


    public void ValidateTime(SimulationEpoch epoch, SimulationTick tick)
    {
        for (var i = 0; i < Count; i++)
            if (epoch.Value == 0 || _values![i].Body.Epoch != epoch.Value || _values![i].Body.Tick != tick.Value)
                throw new ArgumentException("Body set does not belong to its committed envelope.");
    }

    public bool HasSameBits(PhysicsBodyReadSet other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (!_values![i].HasSameBits(other._values![i])) return false;
        return true;
    }
}
