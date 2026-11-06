using System;

namespace CuriousContraptions.Gpu;

/// <summary>Immutable compiled SI principal moment in kg m², one IEEE-754 f32.</summary>
public readonly record struct PrincipalInertia(float Moment)
{
    /// <summary>The compiled domain: the smallest admitted box (1/1024 kg, 1/1024 m half-extents) is above 2^-32 kg m², and the
    /// largest admitted body (1024 kg, 16 m half-extents) is below 2^19 kg m².</summary>
    public const float Minimum = 1f / 4294967296f;
    public const float Maximum = 524288f;

    public void Validate()
    {
        if (!float.IsFinite(Moment) || Moment < Minimum || Moment >= Maximum)
            throw new ArgumentException("Principal inertia exceeds the compiled domain.");
    }
}

/// <summary>Evaluated package mass properties in the authored body frame.</summary>
public readonly record struct RigidMassProperties(MetreVector LocalCentreOfMass,
    CanonicalRotation PrincipalFrame, PrincipalInertia X, PrincipalInertia Y, PrincipalInertia Z)
{
    public void Validate()
    {
        PhysicsDeclarationBounds.Vector(LocalCentreOfMass.X, LocalCentreOfMass.Y, LocalCentreOfMass.Z, 16f);
        PrincipalFrame.Validate(); X.Validate(); Y.Validate(); Z.Validate();
        if (X.Moment > Y.Moment + Z.Moment || Y.Moment > X.Moment + Z.Moment || Z.Moment > X.Moment + Y.Moment)
            throw new ArgumentException("Principal moments must describe a physical mass distribution.");
    }

}
