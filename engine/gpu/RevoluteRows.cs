using System;
using System.Numerics;

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
    UpperStop
}

public readonly record struct RevoluteRow(Vector3 LinearA, Vector3 LinearB, Vector3 AngularA, Vector3 AngularB, float Error);

/// <summary>Canonical f32 Jacobians for a full 3D revolute hinge joint; independent of catalogue identity.</summary>
public static class RevoluteRows
{
    public const int Count = 8;
    public static bool IsStop(RevoluteRowKind kind) => kind is RevoluteRowKind.LowerStop or RevoluteRowKind.UpperStop;

    public static void Prepare(ConstraintBodyFrame a, ConstraintBodyFrame b, ConstraintFrame localA,
        ConstraintFrame localB, float lower, float upper, Span<RevoluteRow> output)
    {
        if (output.Length < Count) throw new ArgumentException("Eight constraint rows are required.");
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
    }

    private static RevoluteRow Linear(Vector3 axis, float error, Vector3 armA, Vector3 armB) =>
        new(-axis, axis, -Vector3.Cross(armA, axis), Vector3.Cross(armB, axis), error);
}
