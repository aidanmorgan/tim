using System;

namespace CuriousContraptions.Physics;

/// <summary>Quadratic potential on a signed axial coordinate (length or angle).
/// The interval effort is its negative discrete gradient: effort times travel
/// equals released potential, including intervals crossing the rest coordinate.
/// It declares a constitutive law only; the shared world must solve motion.</summary>
public sealed class AxialElasticPotential
{
    public double Stiffness { get; }
    public double RestCoordinate { get; }
    public AxialElasticPotential(double stiffness,double restCoordinate)
    {
        if(!double.IsFinite(stiffness)||stiffness<=0)throw new ArgumentOutOfRangeException(nameof(stiffness));
        if(!double.IsFinite(restCoordinate))throw new ArgumentOutOfRangeException(nameof(restCoordinate));
        Stiffness=stiffness;RestCoordinate=restCoordinate;
    }
    private double Displacement(double coordinate)
    {
        if(!double.IsFinite(coordinate))throw new ArgumentOutOfRangeException(nameof(coordinate));
        var displacement=coordinate-RestCoordinate;
        if(!double.IsFinite(displacement))throw new InvalidOperationException("Elastic displacement exceeds numeric range.");
        return displacement;
    }
    public double Energy(double coordinate)
    {
        var displacement=Displacement(coordinate);
        var energy=.5*Stiffness*displacement*displacement;
        if(!double.IsFinite(energy))throw new InvalidOperationException("Elastic energy exceeds numeric range.");
        return energy;
    }
    public double IntervalEffort(double startCoordinate,double endCoordinate)
    {
        // Factoring the difference of squares avoids subtracting near-equal
        // energies or dividing by zero travel. Halving first avoids sum overflow.
        var mean=.5*Displacement(startCoordinate)+.5*Displacement(endCoordinate);
        var effort=-Stiffness*mean;
        if(!double.IsFinite(effort))throw new InvalidOperationException("Elastic effort exceeds numeric range.");
        return effort;
    }
}
