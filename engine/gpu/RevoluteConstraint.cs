using System;

namespace CuriousContraptions.Gpu;

public readonly record struct RevoluteConstraintId(ulong Value);

/// <summary>One generic 3D revolute joint: locks three linear translation and two transverse angular degrees of freedom, leaving rotation about local Z.
/// <see cref="FrictionTorque"/> (N·m, finite, at least 0) bounds the hinge's Coulomb friction row: each substep's friction impulse about
/// the axis is clamped to ±<see cref="RevoluteRows.FrictionImpulseLimit"/>; 0 is frictionless through the same clamp.</summary>
public readonly record struct RevoluteConstraintDeclaration(RevoluteConstraintId Id,
    GpuBodyId BodyA, GpuBodyId BodyB, ConstraintFrame FrameA, ConstraintFrame FrameB,
    float Lower, float Upper, float Stiffness, float Damping, float FrictionTorque, ConnectedCollision Collision)
{
    public void Validate()
    {
        if (Id.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB ||
            !Enum.IsDefined(Collision) || !float.IsFinite(Lower) || !float.IsFinite(Upper) ||
            Lower < -MathF.PI || Upper > MathF.PI || Lower >= Upper ||
            !float.IsFinite(Stiffness) || Stiffness < 0f ||
            !float.IsFinite(Damping) || Damping < 0f ||
            !float.IsFinite(FrictionTorque) || FrictionTorque < 0f)
            throw new ArgumentException("Invalid revolute declaration.");
        FrameA.Validate(); FrameB.Validate();
        if (Stiffness > 0f)
            _ = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, RevoluteRows.SubstepSeconds);
        else if (Damping > 0f)
            _ = SoftConstraintCoefficients.FromDamper(Damping, RevoluteRows.SubstepSeconds);
    }
}
