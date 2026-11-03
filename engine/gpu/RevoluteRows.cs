using System;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public enum RevoluteRowKind : int
{
    LinearX,
    LinearY,
    LinearZ,
    AngularX,
    AngularY,
    Axial,
    LowerStop,
    UpperStop,
    /// <summary>Coulomb friction about the hinge axis: the axial Jacobian with zero error, its impulse bounded each substep by
    /// ±<see cref="RevoluteRows.FrictionImpulseLimit"/>. Appended after the stops, so it resolves last among the joint's rows.</summary>
    Friction,
    /// <summary>The supplied velocity drive about the hinge axis: the axial Jacobian with zero error and the declared target speed as its
    /// own velocity target, its accumulated impulse bounded each substep by <see cref="RevoluteRows.DriveImpulseLimit"/>. Appended last,
    /// so it resolves after friction among the joint's rows.</summary>
    Drive
}

public readonly record struct RevoluteRow(Vector3 LinearA, Vector3 LinearB, Vector3 AngularA, Vector3 AngularB, float Error);

/// <summary>Canonical f32 Jacobians for a full 3D revolute hinge joint; independent of catalogue identity.</summary>
public static class RevoluteRows
{
    public const int Count = 10;
    public static bool IsStop(RevoluteRowKind kind) => kind is RevoluteRowKind.LowerStop or RevoluteRowKind.UpperStop;

    /// <summary>The physical substep h (s) every revolute row is solved over.</summary>
    public const float SubstepSeconds = 1f / WorkshopCadenceSettings.PhysicalFrequency;

    /// <summary>The friction row's per-substep impulse bound τ·h (N·m·s) for a declared friction torque τ: the one expression the solver
    /// clamps the accumulated friction impulse to and admission refuses a committed friction impulse beyond.</summary>
    public static float FrictionImpulseLimit(float frictionTorque) => frictionTorque * SubstepSeconds;

    /// <summary>The drive row's per-substep impulse bound (N·m·s) for a declared maximum torque τ_d: τ_d·h while the drive's supply
    /// route is closed, 0 while it is open (an undeclared drive has τ_d = 0). The one expression the solver clamps the accumulated drive
    /// impulse to ±, and admission refuses a committed drive impulse beyond, both reading the same committed supplied flag.</summary>
    public static float DriveImpulseLimit(float maxTorque, bool supplied) => supplied ? maxTorque * SubstepSeconds : 0f;

    /// <summary>A stop row's upper impulse bound (its lower bound is 0): unbounded for <see cref="RevoluteTravel.Limited"/> travel, 0 for
    /// <see cref="RevoluteTravel.Continuous"/> travel, whose stops are then inert. The solver's clamp and admission share it.</summary>
    public static float StopImpulseLimit(RevoluteTravel travel) => travel switch
    {
        RevoluteTravel.Limited => float.MaxValue,
        RevoluteTravel.Continuous => 0f,
        _ => throw new ArgumentOutOfRangeException(nameof(travel), travel, "Undefined revolute travel.")
    };

    public static void Prepare(ConstraintBodyFrame a, ConstraintBodyFrame b, ConstraintFrame localA,
        ConstraintFrame localB, float lower, float upper, Span<RevoluteRow> output)
    {
        if (output.Length < Count) throw new ArgumentException("Ten constraint rows are required.");
        var localAnchorA = Vector3.Transform(localA.Position, a.Rotation);
        var localAnchorB = Vector3.Transform(localB.Position, b.Rotation);
        var anchorA = a.Position + localAnchorA;
        var anchorB = b.Position + localAnchorB;
        var rotationA = Quaternion.Multiply(a.Rotation, localA.Rotation);
        var rotationB = Quaternion.Multiply(b.Rotation, localB.Rotation);

        var difference = (b.Position - a.Position) + (localAnchorB - localAnchorA);
        var armA = anchorA - a.CentreOfMass + difference;
        var armB = anchorB - b.CentreOfMass;

        var axisX = Vector3.Transform(Vector3.UnitX, rotationA);
        var axisY = Vector3.Transform(Vector3.UnitY, rotationA);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotationA);

        output[(int)RevoluteRowKind.LinearX] = Linear(axisX, Vector3.Dot(difference, axisX), armA, armB);
        output[(int)RevoluteRowKind.LinearY] = Linear(axisY, Vector3.Dot(difference, axisY), armA, armB);
        output[(int)RevoluteRowKind.LinearZ] = Linear(axisZ, Vector3.Dot(difference, axisZ), armA, armB);

        var relative = Quaternion.Multiply(rotationB, Quaternion.Conjugate(rotationA));
        var imaginary = new Vector3(relative.X, relative.Y, relative.Z);
        var sine = imaginary.Length();
        var scale = sine > 1e-8f ? 2f * MathF.Atan2(sine, MathF.Abs(relative.W)) / sine : 2f;
        var rotationError = imaginary * (relative.W < 0 ? -scale : scale);

        output[(int)RevoluteRowKind.AngularX] = new(Vector3.Zero, Vector3.Zero, -axisX, axisX, Vector3.Dot(rotationError, axisX));
        output[(int)RevoluteRowKind.AngularY] = new(Vector3.Zero, Vector3.Zero, -axisY, axisY, Vector3.Dot(rotationError, axisY));

        var qRel = Quaternion.Multiply(Quaternion.Conjugate(rotationA), rotationB);
        var angleZ = 2f * MathF.Atan2(qRel.Z, qRel.W);
        if (angleZ > MathF.PI) angleZ -= 2f * MathF.PI;
        else if (angleZ < -MathF.PI) angleZ += 2f * MathF.PI;

        output[(int)RevoluteRowKind.Axial] = new(Vector3.Zero, Vector3.Zero, -axisZ, axisZ, angleZ);
        output[(int)RevoluteRowKind.LowerStop] = new(Vector3.Zero, Vector3.Zero, -axisZ, axisZ, angleZ - lower);
        output[(int)RevoluteRowKind.UpperStop] = new(Vector3.Zero, Vector3.Zero, axisZ, -axisZ, upper - angleZ);
        output[(int)RevoluteRowKind.Friction] = new(Vector3.Zero, Vector3.Zero, -axisZ, axisZ, 0f);
        output[(int)RevoluteRowKind.Drive] = new(Vector3.Zero, Vector3.Zero, -axisZ, axisZ, 0f);
    }

    private static RevoluteRow Linear(Vector3 axis, float error, Vector3 armA, Vector3 armB) =>
        new(-axis, axis, -Vector3.Cross(armA, axis), Vector3.Cross(armB, axis), error);
}
