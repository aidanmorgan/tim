using System;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;

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

}

/// <summary>How a prismatic's drive is enabled: it has none, or it drives while its owner's compiled PowerIn route is closed.</summary>
public enum PrismaticDriveKind : uint { None, Supplied }

/// <summary>Committed motor command, ideal-brake state, and positive delivered energy in joules.</summary>
public readonly record struct PrismaticMotorRead(PrismaticDrivePhase Phase, float CommandSpeed,
    bool Holding, float HoldCoordinate, float Impulse, float Work, float TickWork)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Phase) || !float.IsFinite(CommandSpeed) || !float.IsFinite(HoldCoordinate) ||
            !float.IsFinite(Impulse) || !float.IsFinite(Work) || Work < 0f || !float.IsFinite(TickWork) || TickWork < 0f)
            throw new ArgumentException("Invalid committed prismatic motor observation.");
    }
}

/// <summary>The supplied drive of one prismatic joint, moving its BodyB along the joint axis toward <see cref="Target"/> (m) at the
/// stopping-profile speed sign(d)·min(<see cref="MaxSpeed"/>, √(2·<see cref="StoppingAcceleration"/>·|d|), |d|/h) for a substep h,
/// with its force bounded
/// by <see cref="MaxEffort"/> (N) and its power by <see cref="MaxPower"/> (W). A separate table, so the joint rows stay the
/// declared slider's rows.</summary>
public readonly record struct PrismaticDriveDeclaration(PrismaticConstraintId Joint, float Target, float MaxSpeed,
    float StoppingAcceleration, float MaxEffort, float MaxPower,
    PrismaticTargetSelection Selection = PrismaticTargetSelection.Fixed,
    PrismaticInactiveResponse Inactive = PrismaticInactiveResponse.Free,
    SupplyNodeId Positive = default, SupplyNodeId Negative = default)
{
    public void Validate(PrismaticConstraintDeclaration joint)
    {
        if (Joint.Value == 0 || Joint != joint.Id || !float.IsFinite(Target) || Target < joint.Lower || Target > joint.Upper ||
            !PositiveValue(MaxSpeed) || MaxSpeed > LinearVelocity.MaximumSpeed || !PositiveValue(StoppingAcceleration) ||
            !PositiveValue(MaxEffort) || !PositiveValue(MaxPower) || !Enum.IsDefined(Selection) || !Enum.IsDefined(Inactive) ||
            (Selection == PrismaticTargetSelection.Fixed ? Positive != default || Negative != default :
                Positive == default || Negative == default || Positive == Negative))
            throw new ArgumentException("Invalid prismatic drive declaration.");
        static bool PositiveValue(float value) => float.IsFinite(value) && value > 0f;
    }
}
