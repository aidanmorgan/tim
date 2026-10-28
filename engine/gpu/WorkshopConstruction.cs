using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

public readonly record struct ConstructionRevision(ulong Value);
public readonly record struct AuthorityRevision(ulong Value);
public readonly record struct SimulationEpoch(ulong Value);
public readonly record struct SimulationTick(ulong Value);
public readonly record struct CommandSequence(ulong Value);
public readonly record struct Metres(float Value);
public readonly record struct Kilograms(float Value);
public readonly record struct Acceleration(float Value);
public readonly record struct InverseSeconds(float Value);
public readonly record struct Restitution(float Value);
public readonly record struct CanonicalRotation(float X, float Y, float Z, float W)
{
    public static CanonicalRotation Identity => new(0f, 0f, 0f, 1f);

    public static CanonicalRotation Normalize(float x, float y, float z, float w)
    {
        var q = new System.Numerics.Quaternion(x, y, z, w);
        var length = q.LengthSquared();
        if (!float.IsFinite(length) || length < .999f || length > 1.001f)
            throw new ArgumentException("Input rotation must represent a unit quaternion.");
        q = System.Numerics.Quaternion.Normalize(q);
        return new(q.X, q.Y, q.Z, q.W);
    }

    public void Validate()
    {
        ValidateCommitted();
    }

    public void ValidateCommitted()
    {
        if (!float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Z) || !float.IsFinite(W) ||
            X < -1f || X > 1f || Y < -1f || Y > 1f ||
            Z < -1f || Z > 1f || W < -1f || W > 1f ||
            (X == 0f && Y == 0f && Z == 0f && W == 0f))
            throw new ArgumentException("Unsupported committed rotation.");
        // One canonical f32 rotation: allow rounding residual, reject scaled quaternions.
        var norm = X * X + Y * Y + Z * Z + W * W;
        if (norm < .99999f || norm > 1.00001f)
            throw new ArgumentException("Canonical rotation must represent a unit quaternion.");
    }
}

/// <summary>Immutable authored Workshop composition; parts compile declarations for the shared engine.</summary>
public readonly record struct WorkshopConstruction(ConstructionRevision Revision, WorkshopCadenceSettings Settings, WorkshopInstances Instances, WorkshopPuzzle Puzzle = default, WorkshopConnections Connections = null!)
{
    public WorkshopConstruction WithInstance(IWorkshopInstance instance) =>
        PuzzleCatalogue.WithInstance(this, instance);
    public WorkshopConnections Connections { get; init; } = Connections ?? WorkshopConnections.Empty;
    public WorkshopConstruction WithoutInstance(GpuBodyId id) => this with { Instances = Instances.Without(id), Connections = Connections.Without(id) };
    public static Acceleration Gravity => new(9.81f);
    public static Metres WorkbenchSurface => new(-0.46f);
    public void Validate()
    {
        if (Revision.Value == 0) throw new ArgumentException("Construction revision must be nonzero.");
        Settings.Validate();
        ArgumentNullException.ThrowIfNull(Instances);
        Instances.Validate(); ArgumentNullException.ThrowIfNull(Connections); Connections.Validate(Instances);
        Puzzle.Validate(this);
    }
}

/// <summary>Canonical f32 authoring helpers; external adapters validate and narrow before calling.</summary>
public static class WorkshopInput
{
    public static WorkshopGenericInstance Generic(PartId partId, GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw, InstanceParameterCollection? parameters = null)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopGenericInstance(partId, id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)))
            { Parameters = parameters ?? InstanceParameterCollection.Defaults(partId) };
        result.Validate(); return result;
    }
    /// <summary>A straight pipe of the CAT-048 default length (3.6 m), the length a newly placed pipe starts from.</summary>
    public static WorkshopPipe Pipe(GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw) => Pipe(id, x, y, z, qx, qy, qz, qw, PipeLength.Default);
    public static WorkshopPipe Pipe(GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw, PipeLength length)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopPipe(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), length);
        result.Validate(); return result;
    }
    public static WorkshopGenericInstance PipeBend45(GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw) =>
        Generic(PartId.PipeBend45, id, x, y, z, qx, qy, qz, qw);
    public static WorkshopGenericInstance PipeBend90(GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw) =>
        Generic(PartId.PipeBend90, id, x, y, z, qx, qy, qz, qw);
    public static WorkshopGenericInstance Funnel(GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw) =>
        Generic(PartId.Funnel, id, x, y, z, qx, qy, qz, qw);
    internal static (int Cell, float Local) Position(float metres)
    {
        if (!float.IsFinite(metres) || metres < -64 || metres > 64)
            throw new ArgumentOutOfRangeException(nameof(metres));
        var cells = metres * 16;
        var cell = checked((int)MathF.Floor(cells + 0.5f));
        var local = (float)(cells - cell);
        if (local == .5f) { cell++; local = -.5f; }
        return (cell, local);
    }

    private static float Rotation(float value)
    {
        if (!float.IsFinite(value) || value < -1 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value));
        return (float)value;
    }
}


/// <summary>Canonical f32 bit identity, including signed zero; positive zero is the all-zero bit pattern.</summary>
public static class F32Bits
{
    public static bool Equal(LocalPosition a, LocalPosition b) => Equal(a.X,b.X) && Equal(a.Y,b.Y) && Equal(a.Z,b.Z);
    public static bool Equal(MetreVector a, MetreVector b) => Equal(a.X,b.X) && Equal(a.Y,b.Y) && Equal(a.Z,b.Z);
    public static bool Equal(float a, float b) => BitConverter.SingleToUInt32Bits(a) == BitConverter.SingleToUInt32Bits(b);
    public static bool IsPositiveZero(float x, float y, float z) =>
        (BitConverter.SingleToUInt32Bits(x) | BitConverter.SingleToUInt32Bits(y) | BitConverter.SingleToUInt32Bits(z)) == 0;
}
