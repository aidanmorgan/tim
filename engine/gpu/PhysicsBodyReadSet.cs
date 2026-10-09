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
        Body.Cell == other.Body.Cell && HalfBits.Equal(Body.Local, other.Body.Local) &&
        Body.Velocity.HasSameBits(other.Body.Velocity) &&
        HalfBits.Equal(Rotation.X, other.Rotation.X) && HalfBits.Equal(Rotation.Y, other.Rotation.Y) &&
        HalfBits.Equal(Rotation.Z, other.Rotation.Z) && HalfBits.Equal(Rotation.W, other.Rotation.W) &&
        AngularVelocity.HasSameBits(other.AngularVelocity) &&
        HalfBits.Equal(LocalCentreOfMass.X, other.LocalCentreOfMass.X) &&
        HalfBits.Equal(LocalCentreOfMass.Y, other.LocalCentreOfMass.Y) &&
        HalfBits.Equal(LocalCentreOfMass.Z, other.LocalCentreOfMass.Z);
}


/// <summary>One immutable, identity-ordered complete body sample at one committed time.</summary>
public readonly struct PhysicsBodyReadSet
{
    public const int Capacity = 16;
    private readonly PhysicsBodyRead[]? _values;
    public byte Count { get; }

    public PhysicsBodyReadSet(ReadOnlySpan<PhysicsBodyRead> values)
    {
        if (values.Length > Capacity) throw new ArgumentException("Dynamic body read capacity exceeded.");
        _values = new PhysicsBodyRead[values.Length]; Count = checked((byte)values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i > 0 && (values[i - 1].Body.Id.Value >= values[i].Body.Id.Value ||
                values[0].Body.Epoch != values[i].Body.Epoch || values[0].Body.Tick != values[i].Body.Tick))
                throw new ArgumentException("Body reads require ordered unique identities and one committed time.");
            _values![i] = values[i];
        }
    }

    public PhysicsBodyRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool TryGet(GpuBodyId id, out PhysicsBodyRead value)
    {
        for (var i = 0; i < Count; i++)
            if (_values![i].Body.Id == id) { value = _values![i]; return true; }
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
                !HalfBits.Equal(value.Body.Local, declared.Local) ||
                !value.Body.Velocity.HasSameBits(declared.Velocity) ||
                !HalfBits.Equal(value.Rotation.X, declared.Rotation.X) ||
                !HalfBits.Equal(value.Rotation.Y, declared.Rotation.Y) ||
                !HalfBits.Equal(value.Rotation.Z, declared.Rotation.Z) ||
                !HalfBits.Equal(value.Rotation.W, declared.Rotation.W) ||
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
