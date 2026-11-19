using System;

namespace CuriousContraptions.Gpu;

/// <summary>Authored pressure boundary, immutable during a Run. It is not a finite gas inventory.</summary>
public enum AtmospherePreset : uint { Standard, Vacuum }

public readonly record struct AtmosphereBoundary(float Pressure, float RelativePressure)
{
    public static AtmosphereBoundary For(AtmospherePreset preset) => preset switch
    {
        AtmospherePreset.Standard => new(101325f, 1f),
        AtmospherePreset.Vacuum => new(0f, 0f),
        _ => throw new ArgumentOutOfRangeException(nameof(preset), "Unsupported atmosphere boundary.")
    };
}
