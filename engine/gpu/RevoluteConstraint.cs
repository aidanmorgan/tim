using System;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct RevoluteConstraintId(ulong Value);

/// <summary>One generic 3D revolute joint: locks three linear translation and two transverse angular degrees of freedom, leaving rotation about local Z.
/// <see cref="Travel"/> is <see cref="RevoluteTravel.Limited"/> (stops at <see cref="Lower"/> and <see cref="Upper"/>) or
/// <see cref="RevoluteTravel.Continuous"/> (limits exactly [−π, π], no axial spring, stop rows inert through
/// <see cref="RevoluteRows.StopImpulseLimit"/>). <see cref="FrictionTorque"/> (N·m, finite, at least 0) bounds the hinge's Coulomb
/// friction row: each substep's friction impulse about the axis is clamped to ±<see cref="RevoluteRows.FrictionImpulseLimit"/>; 0 is
/// frictionless through the same clamp.</summary>
public readonly record struct RevoluteConstraintDeclaration(RevoluteConstraintId Id,
    GpuBodyId BodyA, GpuBodyId BodyB, ConstraintFrame FrameA, ConstraintFrame FrameB,
    float Lower, float Upper, RevoluteTravel Travel, float Stiffness, float Damping, float FrictionTorque, ConnectedCollision Collision)
{
    public void Validate()
    {
        if (Id.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB ||
            !Enum.IsDefined(Collision) || !Enum.IsDefined(Travel) || !float.IsFinite(Lower) || !float.IsFinite(Upper) ||
            Lower < -MathF.PI || Upper > MathF.PI || Lower >= Upper ||
            !float.IsFinite(Stiffness) || Stiffness < 0f ||
            !float.IsFinite(Damping) || Damping < 0f ||
            !float.IsFinite(FrictionTorque) || FrictionTorque < 0f ||
            (Travel == RevoluteTravel.Continuous && (Lower != -MathF.PI || Upper != MathF.PI || Stiffness != 0f)))
            throw new ArgumentException("Invalid revolute declaration.");
        FrameA.Validate(); FrameB.Validate();
        if (Stiffness > 0f)
            _ = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, RevoluteRows.SubstepSeconds);
        else if (Damping > 0f)
            _ = SoftConstraintCoefficients.FromDamper(Damping, RevoluteRows.SubstepSeconds);
    }
}

/// <summary>How a revolute's drive is enabled: it has none, or it drives while its owner's compiled PowerIn route is closed.</summary>
public enum RevoluteDriveKind : uint { None, Supplied }

/// <summary>The supplied velocity drive of one revolute joint: one velocity-level row on the hinge axis driving BodyB's spin relative
/// to BodyA toward <see cref="TargetSpeed"/> (rad/s, signed, |ω| within <see cref="AngularVelocity.MaximumSpeed"/>) with its accumulated
/// impulse bounded by <see cref="RevoluteRows.DriveImpulseLimit"/>: ±<see cref="MaxTorque"/>·h while supplied, zero otherwise. A
/// separate table, so the joint rows stay the declared hinge's rows.</summary>
public readonly record struct RevoluteDriveDeclaration(RevoluteConstraintId Joint, float TargetSpeed, float MaxTorque)
{
    public void Validate(RevoluteConstraintDeclaration joint)
    {
        if (Joint.Value == 0 || Joint != joint.Id || !float.IsFinite(TargetSpeed) || MathF.Abs(TargetSpeed) > AngularVelocity.MaximumSpeed ||
            !float.IsFinite(MaxTorque) || MaxTorque <= 0f)
            throw new ArgumentException("Invalid revolute drive declaration.");
    }
}

/// <summary>One revolute's committed drive state: whether its supply route was closed for the committed tick, its signed travel (rad)
/// and its positive drive work (J), each zero at admission (Run and Reset).</summary>
public readonly record struct RevoluteStateRead(bool Supplied, float Travel, float Work);
