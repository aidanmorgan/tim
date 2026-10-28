using System;
using System.Numerics;

namespace CuriousContraptions.Abstractions;

/// <summary>Quantity-specific game acceptance and contact comparison policies, never a universal epsilon.</summary>
public static class PhysicalTolerance
{
    /// <summary>The shared contact geometry allowance in metres.</summary>
    public const float ContactSeparationMetres = .0002f;

    /// <summary>Curved incident-normal reconstruction allowance, capped by contact displacement.</summary>
    public static float IncidentNormalAllowance(float witnessWindowMetres, float radiusMetres)
    {
        const float baseline = 128f / 16777216f;
        if (!float.IsFinite(witnessWindowMetres) || witnessWindowMetres < 0f ||
            !float.IsFinite(radiusMetres) || radiusMetres <= 0f) return 0f;
        var delta = MathF.Min(witnessWindowMetres, ContactSeparationMetres * .5f);
        if (!(radiusMetres > delta)) return 0f;
        return MathF.Min(baseline + 2f * delta / (radiusMetres - delta), ContactSeparationMetres / radiusMetres);
    }

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

    /// <summary>Practical f32 normal-frame compatibility, capped by displacement over the actual support span.</summary>
    public static bool CompatibleContactNormals(Vector3 first, Vector3 second, float spanMetres, float displacementMetres)
    {
        // 64u per reconstruction chain, 128u for a pair; engineering allowance,
        // not an operation-count certificate for arbitrary analytic queries.
        const float pairBudget = 128f / 16777216f;
        if (!float.IsFinite(spanMetres) || spanMetres < 0f || !float.IsFinite(displacementMetres) || displacementMetres <= 0f ||
            !Unit(first) || !Unit(second)) return false;
        var difference = Vector3.Abs(first - second);
        return difference.X <= pairBudget && difference.Y <= pairBudget && difference.Z <= pairBudget &&
            spanMetres * difference.Length() <= displacementMetres;

        static bool Unit(Vector3 value)
        {
            var squared = value.LengthSquared();
            return float.IsFinite(squared) && MathF.Abs(squared - 1f) <= pairBudget;
        }
    }

    /// <summary>World witness reconstruction resolution for support rank, capped by its existing geometric allowance.</summary>
    public static float ContactSupportResolutionMetres(float coordinateScaleMetres, float displacementMetres)
    {
        if (!float.IsFinite(coordinateScaleMetres) || coordinateScaleMetres < 0f ||
            !float.IsFinite(displacementMetres) || displacementMetres <= 0f)
            throw new ArgumentOutOfRangeException(nameof(coordinateScaleMetres), "Support scales must be finite and nonnegative, with a positive cap.");
        return MathF.Min((64f / 16777216f) * coordinateScaleMetres, displacementMetres);
    }

    private static bool WithinAbsolute(float first, float second, float allowance) =>
        float.IsFinite(first) && float.IsFinite(second) && MathF.Abs(first - second) <= allowance;
}
