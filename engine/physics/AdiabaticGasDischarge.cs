using System;

namespace CuriousContraptions.Physics;

/// <summary>Pure finite discharge from a homogeneous, stationary, rigid, adiabatic
/// ideal-gas reservoir. Outlet total enthalpy (J) includes downstream kinetic energy.
/// This proposal does not authorize a world mutation or apply nozzle thrust.</summary>
public sealed record AdiabaticGasDischarge
{
    public SealedGasState Before { get; }
    public SealedGasState After { get; }
    public double DischargedMass => Before.Mass - After.Mass;
    public double OutletEnthalpy => Before.InternalEnergy - After.InternalEnergy;

    private AdiabaticGasDischarge(SealedGasState before, SealedGasState after)
    {
        Before = before;
        After = after;
    }

    /// <summary>Propose a final inventory in kg, avoiding an implicit rounded mass debit.
    /// Remaining mass must be positive and no larger than the source inventory.
    /// No heat input, inlet flow, moving wall or reservoir bulk kinetic energy is included.</summary>
    public static AdiabaticGasDischarge Propose(SealedGasState source, double remainingMass)
    {
        ArgumentNullException.ThrowIfNull(source);
        IdealGasMaterial.RequirePositive(remainingMass, nameof(remainingMass));
        if (remainingMass > source.Mass)
            throw new ArgumentOutOfRangeException(nameof(remainingMass),
                "Discharge cannot increase the reservoir inventory.");
        if (remainingMass == source.Mass) return new(source, source);

        // dU/dm = Cp T = gamma U/m. Use the exact supplied final mass;
        // near unity, log1p avoids losing its fractional change in a rounded ratio.
        var fraction = (remainingMass - source.Mass) / source.Mass;
        var logRatio = fraction > -.5
            ? double.LogP1(fraction)
            : Math.Log(remainingMass / source.Mass);
        var energy = source.InternalEnergy * Math.Exp(source.Material.HeatCapacityRatio * logRatio);
        if (!double.IsFinite(energy) || energy <= 0 || energy >= source.InternalEnergy)
            throw new ArgumentException("Discharge energy is outside representable numerical resolution.",
                nameof(remainingMass));
        var after = new SealedGasState(source.Material, remainingMass, source.Volume, energy);
        return new(source, after);
    }
}
