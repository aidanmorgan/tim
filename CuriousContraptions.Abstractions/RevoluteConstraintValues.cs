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

/// <summary>One driven revolute's committed drive target (engine-revolute-drive-target): the base body of its hinge, which owns the
/// drive, and the target speed (rad/s) the next step drives toward. Admission commits the declared target; a RevoluteDriveTarget run
/// control changes it from the next committed step.</summary>
public readonly record struct RevoluteDriveTargetRead(GpuBodyId Owner, float TargetSpeed)
{
    public void Validate()
    {
        if (Owner.Value == 0 || !float.IsFinite(TargetSpeed) || MathF.Abs(TargetSpeed) > AngularVelocity.MaximumSpeed)
            throw new ArgumentException("Invalid committed drive target.");
    }
}

/// <summary>Every driven revolute's committed target, in compiled drive-table order (sorted by joint identity). Exactly the compiled
/// drive population; each row is validated at construction, which is also the response decoder's.</summary>
public readonly struct PhysicsDriveTargetRead
{
    private readonly RevoluteDriveTargetRead[]? _values;
    public int Count => _values?.Length ?? 0;
    public RevoluteDriveTargetRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public PhysicsDriveTargetRead(ReadOnlySpan<RevoluteDriveTargetRead> values)
    {
        var copy = values.ToArray();
        foreach (var value in copy) value.Validate();
        _values = copy;
    }

    /// <summary>Each row's committed target speed, in drive-table order: the tick input that keeps every target as committed.</summary>
    public float[] TargetSpeeds()
    {
        var speeds = new float[Count];
        for (var i = 0; i < speeds.Length; i++) speeds[i] = _values![i].TargetSpeed;
        return speeds;
    }

    public bool HasSameBits(PhysicsDriveTargetRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (this[i].Owner != other[i].Owner || !F32Bits.Equal(this[i].TargetSpeed, other[i].TargetSpeed)) return false;
        return true;
    }
}
