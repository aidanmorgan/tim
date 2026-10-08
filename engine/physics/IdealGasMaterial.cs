using System;

namespace CuriousContraptions.Physics;

/// <summary>Calorically perfect single-species gas in an explicitly supported SI envelope.
/// No ambient source, phase transition or variable heat capacity is implied.</summary>
public sealed record IdealGasMaterial
{
    public double SpecificGasConstant { get; }
    public double SpecificHeatAtConstantVolume { get; }
    public double MinimumTemperature { get; }
    public double MaximumTemperature { get; }
    public double MaximumDensity { get; }
    public double MaximumPressure { get; }
    public double HeatCapacityRatio { get; }

    public IdealGasMaterial(double specificGasConstant, double specificHeatAtConstantVolume,
        double minimumTemperature, double maximumTemperature, double maximumDensity, double maximumPressure)
    {
        RequirePositive(specificGasConstant, nameof(specificGasConstant));
        RequirePositive(specificHeatAtConstantVolume, nameof(specificHeatAtConstantVolume));
        RequirePositive(minimumTemperature, nameof(minimumTemperature));
        RequirePositive(maximumTemperature, nameof(maximumTemperature));
        RequirePositive(maximumDensity, nameof(maximumDensity));
        RequirePositive(maximumPressure, nameof(maximumPressure));
        if (maximumTemperature < minimumTemperature)
            throw new ArgumentOutOfRangeException(nameof(maximumTemperature));
        var ratio = 1 + specificGasConstant / specificHeatAtConstantVolume;
        if (!double.IsFinite(ratio) || ratio <= 1)
            throw new ArgumentException("The heat-capacity ratio must be finite and distinguishable from one.");
        SpecificGasConstant = specificGasConstant;
        SpecificHeatAtConstantVolume = specificHeatAtConstantVolume;
        MinimumTemperature = minimumTemperature;
        MaximumTemperature = maximumTemperature;
        MaximumDensity = maximumDensity;
        MaximumPressure = maximumPressure;
        HeatCapacityRatio = ratio;
    }

    internal static void RequirePositive(double value, string parameter)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(parameter);
    }
}
