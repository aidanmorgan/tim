using System;

namespace CuriousContraptions.Gpu;

/// <summary>Bounded value read of committed discrete controllers; no scene node or mutable array escapes. Exactly the scene's
/// residence-sensor population (Story 9R.5b: no capture capacity).</summary>
public readonly struct PhysicsCaptureRead
{
    private readonly CaptureLatch[]? _values;
    public int Count => _values?.Length ?? 0;
    public PhysicsCaptureRead(ReadOnlySpan<CaptureLatch> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Sensor.Value >= values[i].Sensor.Value)
                throw new ArgumentException("Capture identities must be unique and ordered.");
        }
        _values = values.ToArray();
    }
    public CaptureLatch this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));
}
