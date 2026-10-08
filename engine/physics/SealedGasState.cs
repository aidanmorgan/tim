using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable homogeneous gas inventory. Mass (kg), volume (m³), internal energy (J);
/// temperature (K) and absolute pressure (Pa) are derived, never independently writable.
/// This material primitive does not commit work to a mechanical world.</summary>
public sealed record SealedGasState
{
    public IdealGasMaterial Material { get; }
    public double Mass { get; }
    public double Volume { get; }
    public double InternalEnergy { get; }
    public double Temperature { get; }
    public double Density { get; }
    public double Pressure { get; }

    public SealedGasState(IdealGasMaterial material, double mass, double volume, double internalEnergy)
    {
        ArgumentNullException.ThrowIfNull(material);
        IdealGasMaterial.RequirePositive(mass, nameof(mass));
        IdealGasMaterial.RequirePositive(volume, nameof(volume));
        IdealGasMaterial.RequirePositive(internalEnergy, nameof(internalEnergy));
        var heatCapacity = mass * material.SpecificHeatAtConstantVolume;
        IdealGasMaterial.RequirePositive(heatCapacity, nameof(mass));
        var temperature = internalEnergy / heatCapacity;
        var density = mass / volume;
        var pressure = density * material.SpecificGasConstant * temperature;
        if (!double.IsFinite(temperature) || temperature < material.MinimumTemperature ||
            temperature > material.MaximumTemperature ||
            !double.IsFinite(density) || density <= 0 || density > material.MaximumDensity ||
            !double.IsFinite(pressure) || pressure <= 0 || pressure > material.MaximumPressure)
            throw new ArgumentException("Gas state exceeds the declared material or numerical envelope.");
        Material = material;
        Mass = mass;
        Volume = volume;
        InternalEnergy = internalEnergy;
        Temperature = temperature;
        Density = density;
        Pressure = pressure;
    }

    /// <summary>Pure reversible adiabatic proposal. Positive work is supplied TO the gas.
    /// A future coupled world commit must debit the other port by the same amount.</summary>
    public GasVolumeChange ProposeAdiabaticVolume(double volume)
    {
        IdealGasMaterial.RequirePositive(volume, nameof(volume));
        if (volume == Volume) return new(this, this, 0);
        var ratio = Volume / volume;
        IdealGasMaterial.RequirePositive(ratio, nameof(volume));
        var energy = InternalEnergy * Math.Pow(ratio,
            Material.SpecificGasConstant / Material.SpecificHeatAtConstantVolume);
        var after = new SealedGasState(Material, Mass, volume, energy);
        var work = after.InternalEnergy - InternalEnergy;
        if (!double.IsFinite(work) || work == 0 ||
            (volume < Volume ? work < 0 : work > 0))
            throw new ArgumentException("Volume work is outside representable numerical resolution.", nameof(volume));
        return new(this, after, work);
    }
}

/// <summary>Owned before/after states and the signed finite mechanical energy exchange.
/// Construction is internal: callers cannot substitute an unbalanced work amount.</summary>
public sealed record GasVolumeChange
{
    public SealedGasState Before { get; }
    public SealedGasState After { get; }
    public double WorkOnGas { get; }

    internal GasVolumeChange(SealedGasState before, SealedGasState after, double workOnGas)
    {
        Before = before;
        After = after;
        WorkOnGas = workOnGas;
    }
}
