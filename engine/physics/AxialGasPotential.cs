using System;

namespace CuriousContraptions.Physics;

/// <summary>Sealed adiabatic gas potential on a signed slider coordinate (m).
/// Signed area (m²) defines dV/dq; both chamber orientations use the same law.
/// No external ambient pressure, mass flow or world mutation is implied.</summary>
public sealed class AxialGasPotential
{
    public SealedGasState ReferenceState { get; }
    public double ReferenceCoordinate { get; }
    public AxialGasGeometry Geometry { get; }
    public double SignedArea => Geometry.SignedArea;

    public AxialGasPotential(SealedGasState referenceState, double referenceCoordinate, double signedArea)
    {
        ArgumentNullException.ThrowIfNull(referenceState);
        Geometry = new(referenceState.Volume, referenceCoordinate, signedArea);
        ReferenceState = referenceState;
        ReferenceCoordinate = referenceCoordinate;
    }

    /// <summary>Bind the current inventory to permanent geometry. No old thermodynamic
    /// reference is retained when the world starts a new accepted step.</summary>
    public AxialGasPotential(SealedGasState referenceState, double referenceCoordinate, AxialGasGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(referenceState);
        ArgumentNullException.ThrowIfNull(geometry);
        if (referenceState.Volume != geometry.Volume(referenceCoordinate))
            throw new ArgumentException("Committed gas volume must match chamber geometry.");
        Geometry = geometry;
        ReferenceState = referenceState;
        ReferenceCoordinate = referenceCoordinate;
    }

    public SealedGasState State(double coordinate)
    {
        var volume = Geometry.Volume(coordinate);
        if (coordinate == ReferenceCoordinate) return ReferenceState;
        var exponent = ReferenceState.Material.SpecificGasConstant / ReferenceState.Material.SpecificHeatAtConstantVolume;
        var energy = ReferenceState.InternalEnergy * Math.Pow(ReferenceState.Volume / volume, exponent);
        return new(ReferenceState.Material, ReferenceState.Mass, volume, energy);
    }

    public double Energy(double coordinate) => State(coordinate).InternalEnergy;

    /// <summary>Negative discrete energy gradient. The small-stroke evaluation avoids
    /// subtracting nearly equal stored energies. Sign follows increasing slider coordinate.</summary>
    public double IntervalEffort(double startCoordinate, double endCoordinate)
    {
        // Ordering makes swapped endpoints take exactly the same arithmetic path.
        var low = Math.Min(startCoordinate, endCoordinate);
        var high = Math.Max(startCoordinate, endCoordinate);
        var start = State(low);
        _ = State(high); // Validate the whole monotone thermodynamic interval.
        var travel = high - low;
        if (!double.IsFinite(travel)) throw new ArgumentOutOfRangeException(nameof(endCoordinate));
        var exponent = start.Material.SpecificGasConstant / start.Material.SpecificHeatAtConstantVolume;
        var fraction = SignedArea * travel / start.Volume;
        double effort;
        if (travel == 0)
            effort = SignedArea * start.Pressure;
        else
        {
            if (!double.IsFinite(fraction) || fraction <= -1 || fraction == 0)
                throw new ArgumentOutOfRangeException(nameof(endCoordinate), "Chamber volume ratio is outside its numerical domain.");
            var energyFraction = double.ExpM1(-exponent * double.LogP1(fraction));
            effort = -start.InternalEnergy * (energyFraction / travel);
        }
        if (!double.IsFinite(effort) || effort == 0 || Math.Sign(effort) != Math.Sign(SignedArea))
            throw new InvalidOperationException("Gas effort is outside representable numerical resolution.");
        return effort;
    }
}
