using System;

namespace CuriousContraptions.Gpu;

/// <summary>Bounded value read of committed discrete controllers; no scene node or mutable array escapes. Only actual capture pairs, ordered by policy and target.</summary>
public readonly struct PhysicsCaptureRead
{
    private readonly CaptureLatch[]? _values;
    public int Count => _values?.Length ?? 0;
    public PhysicsCaptureRead(ReadOnlySpan<CaptureLatch> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && (values[i - 1].Sensor.Value > values[i].Sensor.Value ||
                (values[i-1].Sensor==values[i].Sensor && values[i-1].Target.Value>=values[i].Target.Value)))
                throw new ArgumentException("Capture identities must be unique and ordered.");
        }
        _values = values.ToArray();
    }
    public bool TryGet(CapturePairKey key,out CaptureLatch latch)
    {
        for(var i=0;i<Count;i++) if(_values![i].Sensor==key.Policy && _values[i].Target==key.Target)
        { latch=_values[i]; return true; }
        latch=default; return false;
    }
    public void ValidateAdvance(PhysicsCaptureRead previous)
    {
        for(var i=0;i<previous.Count;i++)
        {
            var prior=previous[i];
            if(prior.Phase==CaptureLatchPhase.Latched && (!TryGet(new(prior.Sensor,prior.Target),out var current) || current!=prior))
                throw new ArgumentException("Earned capture state was lost or changed.");
        }
    }
    public CaptureLatch this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));
}
