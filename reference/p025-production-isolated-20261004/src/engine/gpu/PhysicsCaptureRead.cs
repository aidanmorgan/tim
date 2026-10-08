using System;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

[InlineArray(PhysicsSceneDeclaration.SensorCapacity)]
internal struct CaptureLatchStorage { private CaptureLatch _first; }

/// <summary>Bounded value read of committed discrete controllers; no scene node or mutable array escapes.</summary>
public readonly struct PhysicsCaptureRead
{
    private readonly CaptureLatchStorage _values;
    public byte Count { get; }
    public PhysicsCaptureRead(ReadOnlySpan<CaptureLatch> values)
    {
        if (values.Length > PhysicsSceneDeclaration.SensorCapacity) throw new ArgumentException("Capture capacity exceeded.");
        Count = checked((byte)values.Length);
        _values = default;
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Sensor.Value >= values[i].Sensor.Value)
                throw new ArgumentException("Capture identities must be unique and ordered.");
            _values[i] = values[i];
        }
    }
    public CaptureLatch this[int index] => index >= 0 && index < Count ? _values[index] :
        throw new ArgumentOutOfRangeException(nameof(index));
}
