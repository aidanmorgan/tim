using System;

namespace CuriousContraptions.Gpu;

/// <summary>Calorically perfect single-species gas. R and Cv are J/(kg K); all limits are inclusive.</summary>
public readonly record struct GasMaterial(float R, float Cv, float MinimumTemperature, float MaximumTemperature,
    float MaximumDensity, float MaximumPressure)
{
    public float Gamma => 1f + R / Cv;
    public float Cp => Cv + R;

    public void Validate()
    {
        Positive(R); Positive(Cv); Positive(MinimumTemperature); Positive(MaximumTemperature);
        Positive(MaximumDensity); Positive(MaximumPressure); Positive(Cp); Positive(Gamma);
        if (MinimumTemperature > MaximumTemperature || Gamma <= 1f)
            throw new ArgumentException("Invalid gas material interval.");
    }

    public void ValidateState(float mass, float volume, float energy)
    {
        Validate();
        Positive(mass); Positive(volume); Positive(energy);
        var temperature = energy / mass / Cv;
        var density = mass / volume;
        var pressure = density * R * temperature;
        if (!float.IsFinite(temperature) || temperature < MinimumTemperature || temperature > MaximumTemperature ||
            !float.IsFinite(density) || density > MaximumDensity ||
            !float.IsFinite(pressure) || pressure > MaximumPressure || pressure <= 0f)
            throw new ArgumentException("Gas state exceeds its material domain.");
    }

    internal static void Positive(float value)
    {
        if (!float.IsFinite(value) || value <= 0f) throw new ArgumentException("Expected a finite positive gas quantity.");
    }

    internal static void Nonnegative(float value)
    {
        if (!float.IsFinite(value) || value < 0f) throw new ArgumentException("Expected a finite nonnegative gas quantity.");
    }
}
