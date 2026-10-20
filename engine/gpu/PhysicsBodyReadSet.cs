using System;

namespace CuriousContraptions.Gpu;

public readonly record struct PhysicsBodyRead(CanonicalBody Body, CanonicalRotation Rotation,
    AngularVelocity AngularVelocity, MetreVector LocalCentreOfMass)
{
    public void Validate()
    {
        Body.Validate(); Rotation.ValidateCommitted(); AngularVelocity.Validate();
        PhysicsDeclarationBounds.Vector(LocalCentreOfMass.X, LocalCentreOfMass.Y, LocalCentreOfMass.Z, (Half)16);
    }

    public bool HasSameBits(PhysicsBodyRead other) =>
        Body.Id == other.Body.Id && Body.Epoch == other.Body.Epoch && Body.Tick == other.Body.Tick &&
        Body.Cell == other.Body.Cell && F32Bits.Equal(Body.Local, other.Body.Local) &&
        Body.Velocity.HasSameBits(other.Body.Velocity) &&
        F32Bits.Equal(Rotation.X, other.Rotation.X) && F32Bits.Equal(Rotation.Y, other.Rotation.Y) &&
        F32Bits.Equal(Rotation.Z, other.Rotation.Z) && F32Bits.Equal(Rotation.W, other.Rotation.W) &&
        AngularVelocity.HasSameBits(other.AngularVelocity) &&
        HalfBits.Equal(LocalCentreOfMass.X, other.LocalCentreOfMass.X) &&
        HalfBits.Equal(LocalCentreOfMass.Y, other.LocalCentreOfMass.Y) &&
        HalfBits.Equal(LocalCentreOfMass.Z, other.LocalCentreOfMass.Z);
}


/// <summary>One immutable, identity-ordered complete body sample at one committed time. Exactly the world's dynamic population:
/// there is no body capacity (owner decision, 20 Oct 2026: "the physics body table needs to be infinite").</summary>
public readonly struct PhysicsBodyReadSet
{
    private readonly PhysicsBodyRead[]? _values;
    public int Count => _values?.Length ?? 0;

    public PhysicsBodyReadSet(ReadOnlySpan<PhysicsBodyRead> values) : this(values.ToArray()) { }

    /// <summary>Takes ownership of a freshly decoded array (no copy); the caller never touches it again.</summary>
    internal static PhysicsBodyReadSet Adopt(PhysicsBodyRead[] values) => new(values);

    private PhysicsBodyReadSet(PhysicsBodyRead[] values)
    {
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

    public void ValidateScene(PhysicsSceneDeclaration scene, SimulationEpoch epoch, SimulationTick tick)
    {
        ValidateTime(epoch, tick);
        var index = 0;
        foreach (var declared in scene.Bodies)
        {
            if (declared.Motion != RigidMotionKind.Dynamic) continue;
            if (index >= Count || _values![index].Body.Id != declared.Id)
                throw new ArgumentException("Complete body set differs from the installed scene.");
            var value = _values![index++]; value.Validate();
            ColliderDeclaration? geometry = null;
            foreach (var collider in scene.Colliders)
                if (collider.Body == declared.Id)
                {
                    if (geometry.HasValue) throw new ArgumentException("Dynamic compound is not admitted.");
                    geometry = collider;
                }
            if (geometry is not { } shape || !HalfBits.Equal(value.LocalCentreOfMass, shape.Pose.Translation))
                throw new ArgumentException("Committed centre of mass differs from declaration.");
            if (tick.Value == 0 && (value.Body.Cell != declared.Cell ||
                !F32Bits.Equal(value.Body.Local, declared.Local) ||
                !value.Body.Velocity.HasSameBits(declared.Velocity) ||
                !F32Bits.Equal(value.Rotation.X, declared.Rotation.X) ||
                !F32Bits.Equal(value.Rotation.Y, declared.Rotation.Y) ||
                !F32Bits.Equal(value.Rotation.Z, declared.Rotation.Z) ||
                !F32Bits.Equal(value.Rotation.W, declared.Rotation.W) ||
                !value.AngularVelocity.HasSameBits(declared.AngularVelocity)))
                throw new ArgumentException("Admission changed a canonical body.");
        }
        if (index != Count) throw new ArgumentException($"Read contains a foreign dynamic body: expected {index}, received {Count}, scene bodies {scene.Bodies.Length}, epoch {epoch.Value}, tick {tick.Value}.");
    }

    public bool HasSameBits(PhysicsBodyReadSet other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (!_values![i].HasSameBits(other._values![i])) return false;
        return true;
    }
}
