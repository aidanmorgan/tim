using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct PrismaticConstraintId(ulong Value);
public enum ConnectedCollision : uint { Disabled, Enabled }

/// <summary>F32 body-local joint frame; local Y is the allowed translation axis.</summary>
public readonly record struct ConstraintFrame(Vector3 Position, Quaternion Rotation)
{
    public static ConstraintFrame Identity => new(Vector3.Zero, Quaternion.Identity);
    public void Validate()
    {
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y) || !float.IsFinite(Position.Z) ||
            Position.LengthSquared() > 256 || !float.IsFinite(Rotation.LengthSquared()) ||
            MathF.Abs(Rotation.LengthSquared() - 1) > 1e-5f)
            throw new ArgumentException("Invalid canonical constraint frame.");
    }
}

/// <summary>One generic 3D prismatic joint: five locked freedoms, an axial spring and two stops.</summary>
public readonly record struct PrismaticConstraintDeclaration(PrismaticConstraintId Id,
    GpuBodyId BodyA, GpuBodyId BodyB, ConstraintFrame FrameA, ConstraintFrame FrameB,
    float Lower, float Upper, float Stiffness, float Damping, ConnectedCollision Collision)
{
    public void Validate()
    {
        if (Id.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB ||
            !Enum.IsDefined(Collision) || !float.IsFinite(Lower) || !float.IsFinite(Upper) ||
            Lower < -16 || Upper > 16 || Lower >= Upper || Lower > 0 || Upper < 0)
            throw new ArgumentException("Invalid prismatic declaration.");
        FrameA.Validate(); FrameB.Validate();
        _ = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, 1f / WorkshopCadenceSettings.PhysicalFrequency);
    }
}

/// <summary>How a prismatic's drive is enabled: it has none, or it drives while its owner's compiled PowerIn route is closed.</summary>
public enum PrismaticDriveKind : uint { None, Supplied }

/// <summary>The supplied drive of one prismatic joint, moving its BodyB along the joint axis toward <see cref="Target"/> (m) at the
/// stopping-profile speed sign(d)·min(<see cref="MaxSpeed"/>, √(2·<see cref="StoppingAcceleration"/>·|d|), |d|/h) for a substep h,
/// with its force bounded
/// by <see cref="MaxEffort"/> (N) and its power by <see cref="MaxPower"/> (W). A separate table, so the joint rows stay the
/// declared slider's rows.</summary>
public readonly record struct PrismaticDriveDeclaration(PrismaticConstraintId Joint, float Target, float MaxSpeed,
    float StoppingAcceleration, float MaxEffort, float MaxPower)
{
    public void Validate(PrismaticConstraintDeclaration joint)
    {
        if (Joint.Value == 0 || Joint != joint.Id || !float.IsFinite(Target) || Target < joint.Lower || Target > joint.Upper ||
            !Positive(MaxSpeed) || MaxSpeed > LinearVelocity.MaximumSpeed || !Positive(StoppingAcceleration) ||
            !Positive(MaxEffort) || !Positive(MaxPower))
            throw new ArgumentException("Invalid prismatic drive declaration.");
        static bool Positive(float value) => float.IsFinite(value) && value > 0f;
    }
}
