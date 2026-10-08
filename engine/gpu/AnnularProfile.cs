using System;

namespace CuriousContraptions.Gpu;

/// <summary>Stable exposed meridian features: interior, negative/lower endpoint, positive/upper endpoint.</summary>
public enum AnnularFeature : uint
{
    Inner, InnerNegativeRim, InnerPositiveRim,
    MiddleOuter, MiddleNegativeShoulder, MiddlePositiveShoulder,
    NegativeOuter, NegativeOuterRim, NegativeOuterShoulder,
    PositiveOuter, PositiveOuterShoulder, PositiveOuterRim,
    NegativeEnd, NegativeEndInnerRim, NegativeEndOuterRim,
    PositiveEnd, PositiveEndInnerRim, PositiveEndOuterRim,
    NegativeShoulder, NegativeShoulderInnerRim, NegativeShoulderOuterRim,
    PositiveShoulder, PositiveShoulderInnerRim, PositiveShoulderOuterRim
}

/// <summary>
/// Static axial solid: a constant bore, a middle annulus and optional symmetric end bands.
/// HalfLength and EndHalfWidth remain separate canonical terms at the geometry boundary.
/// </summary>
public readonly record struct AnnularProfile(Metres HalfLength, Metres InnerRadius,
    Metres MiddleRadius, Metres EndRadius, Metres EndHalfWidth)
{
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(HalfLength.Value, (Half)(1.0 / 16), (Half)4);
        PhysicsDeclarationBounds.Range(InnerRadius.Value, (Half)(1.0 / 16), (Half)2);
        PhysicsDeclarationBounds.Range(MiddleRadius.Value, (Half)(1.0 / 16), (Half)4);
        PhysicsDeclarationBounds.Range(EndRadius.Value, (Half)(1.0 / 16), (Half)4);
        PhysicsDeclarationBounds.Range(EndHalfWidth.Value, (Half)0, (Half).25);
        if (MiddleRadius.Value <= InnerRadius.Value || EndRadius.Value < MiddleRadius.Value ||
            HalfLength.Value <= EndHalfWidth.Value ||
            ((EndHalfWidth.Value == (Half)0) != (EndRadius.Value == MiddleRadius.Value)))
            throw new ArgumentException("Invalid annular profile topology.");
    }
}
