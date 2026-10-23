using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

public readonly record struct ConstructionRevision(ulong Value);
public readonly record struct AuthorityRevision(ulong Value);
public readonly record struct SimulationEpoch(ulong Value);
public readonly record struct SimulationTick(ulong Value);
public readonly record struct CommandSequence(ulong Value);
public readonly record struct Metres(Half Value);
public readonly record struct Kilograms(Half Value);
public readonly record struct Acceleration(Half Value);
public readonly record struct InverseSeconds(Half Value);
public readonly record struct Restitution(Half Value);
public readonly record struct CanonicalRotation(float X, float Y, float Z, float W)
{
    public static CanonicalRotation Identity => new((float)0, (float)0, (float)0, (float)1);

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
            X < (float)(-1) || X > (float)1 || Y < (float)(-1) || Y > (float)1 ||
            Z < (float)(-1) || Z > (float)1 || W < (float)(-1) || W > (float)1 ||
            (X == (float)0 && Y == (float)0 && Z == (float)0 && W == (float)0))
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
    public static Acceleration Gravity => new((Half)9.81);
    public static Metres WorkbenchSurface => new((Half)(-0.46));
    public void Validate()
    {
        if (Revision.Value == 0) throw new ArgumentException("Construction revision must be nonzero.");
        Settings.Validate();
        ArgumentNullException.ThrowIfNull(Instances);
        Instances.Validate(); ArgumentNullException.ThrowIfNull(Connections); Connections.Validate(Instances);
        Puzzle.Validate(this);
    }
}

/// <summary>Only UI/resource boundary code may convert wider external numbers into game values.</summary>
public static class WorkshopInput
{
    public static WorkshopDomino Domino(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopDomino(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), DominoMaterial.Default);
        result.Validate(); return result;
    }
    public static WorkshopLamp Lamp(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopLamp(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)));
        result.Validate(); return result;
    }
    public static WorkshopBallDetector BallDetector(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, bool locked = false)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopBallDetector(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)),
            locked);
        result.Validate(); return result;
    }
    public static WorkshopPressurePlate PressurePlate(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, float minimumMass, bool locked = false)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopPressurePlate(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), minimumMass, locked);
        result.Validate(); return result;
    }
    public static WorkshopCounter Counter(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, int targetCount, bool locked = false)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopCounter(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), targetCount, locked);
        result.Validate(); return result;
    }
    public static WorkshopClock Clock(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, float intervalSeconds, bool locked = false)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopClock(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), intervalSeconds, locked);
        result.Validate(); return result;
    }
    public static WorkshopLatch Latch(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, bool locked = false)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopLatch(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), locked);
        result.Validate(); return result;
    }
    public static WorkshopGenericInstance Generic(PartId partId, GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, InstanceParameterCollection? parameters = null)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopGenericInstance(partId, id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)))
            { Parameters = parameters ?? InstanceParameterCollection.Defaults(partId) };
        result.Validate(); return result;
    }
    /// <summary>A straight pipe of the CAT-048 default length (3.6 m), the length a newly placed pipe starts from.</summary>
    public static WorkshopPipe Pipe(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw) => Pipe(id, x, y, z, qx, qy, qz, qw, PipeLength.Default);
    public static WorkshopPipe Pipe(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, PipeLength length)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopPipe(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), length);
        result.Validate(); return result;
    }
    public static WorkshopGenericInstance PipeBend45(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw) =>
        Generic(PartId.PipeBend45, id, x, y, z, qx, qy, qz, qw);
    public static WorkshopGenericInstance PipeBend90(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw) =>
        Generic(PartId.PipeBend90, id, x, y, z, qx, qy, qz, qw);
    public static WorkshopGenericInstance Funnel(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw) =>
        Generic(PartId.Funnel, id, x, y, z, qx, qy, qz, qw);
    internal static (int Cell, float Local) Position(double metres)
    {
        if (!double.IsFinite(metres) || metres < -64 || metres > 64)
            throw new ArgumentOutOfRangeException(nameof(metres));
        var cells = metres * 16;
        var cell = checked((int)Math.Floor(cells + 0.5));
        var local = (float)(cells - cell);
        if (local == .5f) { cell++; local = -.5f; }
        return (cell, local);
    }

    private static float Rotation(double value)
    {
        if (!double.IsFinite(value) || value < -1 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value));
        return (float)value;
    }
}


/// <summary>Canonical bit identity is stronger than Half numeric equality, including signed zero.</summary>
public static class HalfBits
{
    public static bool Equal(Half a, Half b) => BitConverter.HalfToUInt16Bits(a) == BitConverter.HalfToUInt16Bits(b);
    public static bool Equal(MetreVector a, MetreVector b) => Equal(a.X, b.X) && Equal(a.Y, b.Y) && Equal(a.Z, b.Z);
}

/// <summary>Canonical f32 bit identity, including signed zero; positive zero is the all-zero bit pattern.</summary>
public static class F32Bits
{
    public static bool Equal(LocalPosition a, LocalPosition b) => Equal(a.X,b.X) && Equal(a.Y,b.Y) && Equal(a.Z,b.Z);
    public static bool Equal(float a, float b) => BitConverter.SingleToUInt32Bits(a) == BitConverter.SingleToUInt32Bits(b);
    public static bool IsPositiveZero(float x, float y, float z) =>
        (BitConverter.SingleToUInt32Bits(x) | BitConverter.SingleToUInt32Bits(y) | BitConverter.SingleToUInt32Bits(z)) == 0;
}
