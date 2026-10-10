using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public enum PrismaticRowKind : int { TransverseX, TransverseZ, AngularX, AngularY, AngularZ, Spring, LowerStop, UpperStop }

public readonly record struct ConstraintBodyFrame(Vector3 Position, Quaternion Rotation, Vector3 CentreOfMass);
public readonly record struct PrismaticRow(Vector3 LinearA, Vector3 LinearB, Vector3 AngularA, Vector3 AngularB, float Error);

/// <summary>Canonical f32 Jacobians for a full 3D prismatic joint; independent of catalogue identity.</summary>
public static class PrismaticRows
{
    public const int Count = 8;
    public static bool IsStop(PrismaticRowKind kind) => kind is PrismaticRowKind.LowerStop or PrismaticRowKind.UpperStop;
    public static void Prepare(ConstraintBodyFrame a, ConstraintBodyFrame b, ConstraintFrame localA,
        ConstraintFrame localB, float lower, float upper, Span<PrismaticRow> output)
    {
        if (output.Length < Count) throw new ArgumentException("Eight constraint rows are required.");
        var anchorA = a.Position + Vector3.Transform(localA.Position, a.Rotation);
        var anchorB = b.Position + Vector3.Transform(localB.Position, b.Rotation);
        var rotationA = Quaternion.Multiply(a.Rotation, localA.Rotation);
        var rotationB = Quaternion.Multiply(b.Rotation, localB.Rotation);
        var difference = anchorB - anchorA;
        var armA = anchorA - a.CentreOfMass + difference;
        var armB = anchorB - b.CentreOfMass;
        var axis = Vector3.Transform(Vector3.UnitY, rotationA);
        var transverseX = Vector3.Transform(Vector3.UnitX, rotationA);
        var transverseZ = Vector3.Transform(Vector3.UnitZ, rotationA);
        output[(int)PrismaticRowKind.TransverseX] = Linear(transverseX, Vector3.Dot(difference, transverseX), armA, armB);
        output[(int)PrismaticRowKind.TransverseZ] = Linear(transverseZ, Vector3.Dot(difference, transverseZ), armA, armB);
        var relative = Quaternion.Multiply(rotationB, Quaternion.Conjugate(rotationA));
        var imaginary = new Vector3(relative.X, relative.Y, relative.Z);
        var sine = imaginary.Length();
        var scale = sine > 1e-8f ? 2f * MathF.Atan2(sine, MathF.Abs(relative.W)) / sine : 2f;
        var rotationError = imaginary * (relative.W < 0 ? -scale : scale);
        output[(int)PrismaticRowKind.AngularX] = new(Vector3.Zero, Vector3.Zero, -Vector3.UnitX, Vector3.UnitX, rotationError.X);
        output[(int)PrismaticRowKind.AngularY] = new(Vector3.Zero, Vector3.Zero, -Vector3.UnitY, Vector3.UnitY, rotationError.Y);
        output[(int)PrismaticRowKind.AngularZ] = new(Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Vector3.UnitZ, rotationError.Z);
        var translation = Vector3.Dot(difference, axis);
        output[(int)PrismaticRowKind.Spring] = Linear(axis, translation, armA, armB);
        output[(int)PrismaticRowKind.LowerStop] = Linear(axis, translation - lower, armA, armB);
        output[(int)PrismaticRowKind.UpperStop] = Linear(-axis, upper - translation, armA, armB);
    }

    private static PrismaticRow Linear(Vector3 axis, float error, Vector3 armA, Vector3 armB) =>
        new(-axis, axis, -Vector3.Cross(armA, axis), Vector3.Cross(armB, axis), error);
}
