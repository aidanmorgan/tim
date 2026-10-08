using System;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

[InlineArray(PhysicsBodyReadSet.Capacity)]
internal struct PresentedBodyStorage { private PresentedBody _first; }

/// <summary>All body poses selected at the presentation sample's single display time.</summary>
public readonly struct PresentedBodySet
{
    private readonly PresentedBodyStorage _values;
    public byte Count { get; }
    public PresentedBodySet(ReadOnlySpan<PresentedBody> values)
    {
        if (values.Length > PhysicsBodyReadSet.Capacity) throw new ArgumentException("Presentation body capacity exceeded.");
        _values = default; Count = checked((byte)values.Length);
        for (var i = 0; i < Count; i++)
        {
            if (values[i].Id.Value == 0 || (i > 0 && values[i - 1].Id.Value >= values[i].Id.Value))
                throw new ArgumentException("Presentation identities must be unique and ordered.");
            _values[i] = values[i];
        }
    }
    public PresentedBody this[int index] => index >= 0 && index < Count ? _values[index] :
        throw new ArgumentOutOfRangeException(nameof(index));
}
