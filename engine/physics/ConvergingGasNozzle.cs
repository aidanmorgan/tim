using System;

namespace CuriousContraptions.Physics;

public enum GasNozzleRegime { NoFlow, Subsonic, Choked }

/// <summary>Immutable instantaneous SI fluxes, not a committed mass/energy transfer.</summary>
public sealed record GasNozzleFlow
{
    public GasNozzleRegime Regime { get; }
    public double MassRate { get; }
    public double ExitPressure { get; }
    public double ExitTemperature { get; }
    public double ExitSpeed { get; }
    public double EnthalpyPower { get; }
    public double MomentumRate { get; }
    public double PressureForce { get; }
    public double Thrust => MomentumRate + PressureForce;

    internal GasNozzleFlow(GasNozzleRegime regime, double massRate, double pressure, double temperature,
        double speed, double power, double momentum, double pressureForce)
    {
        if (!Enum.IsDefined(regime)) throw new ArgumentOutOfRangeException(nameof(regime));
        ReadOnlySpan<double> values = stackalloc double[] { massRate, pressure, temperature, speed, power, momentum, pressureForce, momentum + pressureForce };
        foreach (var value in values)
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentException("Nozzle flux exceeds its numerical range.");
        Regime = regime;
        MassRate = massRate;
        ExitPressure = pressure;
        ExitTemperature = temperature;
        ExitSpeed = speed;
        EnthalpyPower = power;
        MomentumRate = momentum;
        PressureForce = pressureForce;
    }
}

/// <summary>Ideal adiabatic, lossless converging nozzle. Upstream state is a stagnant
/// reservoir; the exit is the throat. Reverse flow and downstream plume expansion
/// require their own declarations and are not inferred.</summary>
public sealed class ConvergingGasNozzle
{
    public double Area { get; }
    public ConvergingGasNozzle(double area)
    {
        if (!double.IsFinite(area) || area < 0) throw new ArgumentOutOfRangeException(nameof(area));
        Area = area;
    }

    public GasNozzleFlow Evaluate(SealedGasState source, double backPressure)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!double.IsFinite(backPressure) || backPressure < 0 || backPressure > source.Pressure)
            throw new ArgumentOutOfRangeException(nameof(backPressure), "This declaration supports forward flow only.");
        if (Area == 0 || backPressure == source.Pressure)
            return new(GasNozzleRegime.NoFlow, 0, source.Pressure, source.Temperature, 0, 0, 0, 0);
        var material = source.Material;
        var gamma = material.HeatCapacityRatio;
        var exponent = (gamma - 1) / gamma;
        var criticalRatio = Math.Pow(2 / (gamma + 1), 1 / exponent);
        var criticalPressure = source.Pressure * criticalRatio;
        IdealGasMaterial.RequirePositive(criticalPressure, nameof(source));
        var choked = backPressure <= criticalPressure;
        var pressure = choked ? criticalPressure : backPressure;
        var ratio = pressure / source.Pressure;
        var temperatureDrop = source.Temperature * -double.ExpM1(exponent * Math.Log(ratio));
        var temperature = source.Temperature - temperatureDrop;
        var cp = material.SpecificHeatAtConstantVolume + material.SpecificGasConstant;
        var speed = Math.Sqrt(2 * cp * temperatureDrop);
        var density = pressure / (material.SpecificGasConstant * temperature);
        if (!double.IsFinite(temperature) || temperature < material.MinimumTemperature ||
            temperature > material.MaximumTemperature || !double.IsFinite(density) ||
            density <= 0 || density > material.MaximumDensity)
            throw new ArgumentException("Nozzle exit is outside the declared gas material envelope.");
        var massRate = Area * density * speed;
        IdealGasMaterial.RequirePositive(speed, nameof(source));
        IdealGasMaterial.RequirePositive(massRate, nameof(Area));
        var power = massRate * (cp * source.Temperature);
        var momentum = massRate * speed;
        var pressureForce = Area * (pressure - backPressure);
        IdealGasMaterial.RequirePositive(power, nameof(source));
        IdealGasMaterial.RequirePositive(momentum, nameof(source));
        if (pressure > backPressure) IdealGasMaterial.RequirePositive(pressureForce, nameof(Area));
        return new(choked ? GasNozzleRegime.Choked : GasNozzleRegime.Subsonic,
            massRate, pressure, temperature, speed, power, momentum, pressureForce);
    }
}
