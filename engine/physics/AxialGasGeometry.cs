using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable chamber geometry in SI units; contains no gas inventory.
/// SignedArea is dV/dq, shared by both chamber orientations.</summary>
public sealed record AxialGasGeometry
{
    public double ReferenceVolume { get; }
    public double ReferenceCoordinate { get; }
    public double SignedArea { get; }

    public AxialGasGeometry(double referenceVolume, double referenceCoordinate, double signedArea)
    {
        IdealGasMaterial.RequirePositive(referenceVolume, nameof(referenceVolume));
        if (!double.IsFinite(referenceCoordinate))
            throw new ArgumentOutOfRangeException(nameof(referenceCoordinate));
        if (!double.IsFinite(signedArea) || signedArea == 0)
            throw new ArgumentOutOfRangeException(nameof(signedArea));
        ReferenceVolume = referenceVolume;
        ReferenceCoordinate = referenceCoordinate;
        SignedArea = signedArea;
    }

    public double Volume(double coordinate)
    {
        if (!double.IsFinite(coordinate)) throw new ArgumentOutOfRangeException(nameof(coordinate));
        var displacement = coordinate - ReferenceCoordinate;
        var volume = Math.FusedMultiplyAdd(SignedArea, displacement, ReferenceVolume);
        if (!double.IsFinite(displacement) || !double.IsFinite(volume) || volume <= 0)
            throw new ArgumentOutOfRangeException(nameof(coordinate), "Chamber volume is outside its numerical domain.");
        return volume;
    }
}
