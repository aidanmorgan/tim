using System;

namespace CuriousContraptions.Gpu;

/// <summary>All body poses selected at the presentation sample's single display time: exactly the presented population
/// (Story 9R.5b: no presentation body capacity).</summary>
public readonly struct PresentedBodySet
{
    private readonly PresentedBody[]? _values;
    public int Count => _values?.Length ?? 0;
    public PresentedBodySet(ReadOnlySpan<PresentedBody> values)
    {
        for (var i = 0; i < values.Length; i++)
            if (values[i].Id.Value == 0 || (i > 0 && values[i - 1].Id.Value >= values[i].Id.Value))
                throw new ArgumentException("Presentation identities must be unique and ordered.");
        _values = values.ToArray();
    }
    public PresentedBody this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));
}
