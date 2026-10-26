using System;

namespace CuriousContraptions.Abstractions;

/// <summary>Quantity-specific game acceptance in metres, not solver thresholds or a universal epsilon.</summary>
public static class PhysicalTolerance
{
    /// <summary>0.01% of nominal local horizontal size, bounded by 5 µm and 50 µm.</summary>
    public static float RestingDisplacementMetres(float nominalSizeMetres)
    {
        if (!float.IsFinite(nominalSizeMetres) || nominalSizeMetres <= 0f)
            throw new ArgumentOutOfRangeException(nameof(nominalSizeMetres), "Size must be finite and positive.");
        return Math.Clamp(1e-4f * nominalSizeMetres, 5e-6f, 50e-6f);
    }

    public static bool WithinRestingDisplacement(float displacementMetres, float nominalSizeMetres) =>
        WithinAbsolute(displacementMetres, 0f, RestingDisplacementMetres(nominalSizeMetres));

    /// <summary>The authored bore compatibility contract: inclusive 1 µm, with no relative allowance.</summary>
    public static bool CompatibleBoreRadii(float firstMetres, float secondMetres) =>
        WithinAbsolute(firstMetres, secondMetres, 1e-6f);

    private static bool WithinAbsolute(float first, float second, float allowance) =>
        float.IsFinite(first) && float.IsFinite(second) && MathF.Abs(first - second) <= allowance;
}
