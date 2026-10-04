using System;

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
public readonly record struct CanonicalRotation(Half X, Half Y, Half Z, Half W)
{
    public static CanonicalRotation Identity => new((Half)0, (Half)0, (Half)0, (Half)1);

    public void Validate()
    {
        if (!Half.IsFinite(X) || !Half.IsFinite(Y) || !Half.IsFinite(Z) || !Half.IsFinite(W) ||
            X < (Half)(-1) || X > (Half)1 || Y < (Half)(-1) || Y > (Half)1 ||
            Z < (Half)(-1) || Z > (Half)1 || W < (Half)(-1) || W > (Half)1 ||
            (X == (Half)0 && Y == (Half)0 && Z == (Half)0 && W == (Half)0))
            throw new ArgumentException("Unsupported canonical rotation.");
        // Validation at the canonical input/codec boundary; this value is never used to correct rotation.
        var norm = (double)X * (double)X + (double)Y * (double)Y + (double)Z * (double)Z + (double)W * (double)W;
        if (norm < 0.999 || norm > 1.001)
            throw new ArgumentException("Canonical rotation must represent a unit quaternion.");
    }
}

/// <summary>The immutable admitted default Basketball declaration. Render adapters widen these values.</summary>
public readonly record struct BasketballMaterial(
    Metres Radius, Kilograms Mass, Restitution Bounce, InverseSeconds Drag, Acceleration Buoyancy)
{
    public static BasketballMaterial Default => new(new((Half)0.34), new((Half)1),
        new((Half)0.55), new((Half)0.04), new((Half)0));
    public void Validate()
    {
        var expected = Default;
        if (!HalfBits.Equal(Radius.Value, expected.Radius.Value) || !HalfBits.Equal(Mass.Value, expected.Mass.Value) ||
            !HalfBits.Equal(Bounce.Value, expected.Bounce.Value) || !HalfBits.Equal(Drag.Value, expected.Drag.Value) ||
            !HalfBits.Equal(Buoyancy.Value, expected.Buoyancy.Value))
            throw new ArgumentException("Only the default Basketball material is admitted.");
    }
}

/// <summary>Durable construction uses canonical bits and identities, never a live Godot transform.</summary>
public readonly record struct WorkshopBall(
    GpuBodyId Id, CellOrigin Cell, LocalPosition Local, CanonicalRotation Rotation, BasketballMaterial Material, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Basketball;
    public void Validate()
    {
        Material.Validate();
        Rotation.Validate();
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
    }
}

public readonly record struct ReceiverCaptureSettings(
    Metres Margin, LinearSpeed SpeedLimit, DurationSeconds Dwell, SensorParticipation Participation)
{
    public static ReceiverCaptureSettings Free => new(new((Half).02), new((Half)1.5), new((Half).35), SensorParticipation.Enabled);
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(Margin.Value, (Half)0, (Half)1);
        PhysicsDeclarationBounds.Range(SpeedLimit.Value, (Half)(1.0 / 1024), (Half)64);
        PhysicsDeclarationBounds.Range(Dwell.Value, (Half)(1.0 / 1024), (Half)30);
        if (!Enum.IsDefined(Participation)) throw new ArgumentException("Undefined sensor participation.");
    }
}
public readonly record struct WorkshopReceiver(
    GpuBodyId Id, CellOrigin Cell, LocalPosition Local, CanonicalRotation Rotation, ReceiverCaptureSettings Capture, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Receiver;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Capture.Validate();
    }
}

/// <summary>Immutable authored Workshop composition; parts compile declarations for the shared engine.</summary>
public readonly record struct WorkshopConstruction(ConstructionRevision Revision, WorkshopCadenceSettings Settings, WorkshopInstances Instances, WorkshopPuzzle Puzzle = default)
{
    public WorkshopBall? Ball => Instances.Find<WorkshopBall>();
    public WorkshopReceiver? Receiver => Instances.Find<WorkshopReceiver>();
    public WorkshopConstruction WithInstance(IWorkshopInstance instance) => this with { Instances = Instances.With(instance) };
    public WorkshopConstruction WithoutInstance(GpuBodyId id) => this with { Instances = Instances.Without(id) };
    public static Acceleration Gravity => new((Half)9.81);
    public static Metres WorkbenchSurface => new((Half)(-0.46));
    public void Validate()
    {
        if (Revision.Value == 0) throw new ArgumentException("Construction revision must be nonzero.");
        Settings.Validate();
        ArgumentNullException.ThrowIfNull(Instances);
        Instances.Validate(); Puzzle.Validate(this);
    }
}

/// <summary>Only UI/resource boundary code may convert wider external numbers into game values.</summary>
public static class WorkshopInput
{
    public static WorkshopBall Basketball(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var rotation = new CanonicalRotation(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw));
        var result = new WorkshopBall(id, new(px.Cell, py.Cell, pz.Cell),
            new(px.Local, py.Local, pz.Local), rotation, BasketballMaterial.Default);
        result.Validate();
        return result;
    }

    public static WorkshopReceiver Receiver(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopReceiver(id, new(px.Cell, py.Cell, pz.Cell),
            new(px.Local, py.Local, pz.Local), new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)),
            ReceiverCaptureSettings.Free);
        result.Validate(); return result;
    }

    public static WorkshopRamp Ramp(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, RampDimensions dimensions)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopRamp(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), dimensions);
        result.Validate(); return result;
    }

    internal static (int Cell, Half Local) Position(double metres)
    {
        if (!double.IsFinite(metres) || metres < -64 || metres > 64)
            throw new ArgumentOutOfRangeException(nameof(metres));
        var cells = metres * 16;
        var cell = checked((int)Math.Floor(cells + 0.5));
        var local = (Half)(cells - cell);
        if (local == (Half)0.5) { cell++; local = (Half)(-0.5); }
        return (cell, local);
    }

    private static Half Rotation(double value)
    {
        if (!double.IsFinite(value) || value < -1 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value));
        return (Half)value;
    }
}


/// <summary>Canonical bit identity is stronger than Half numeric equality, including signed zero.</summary>
public static class HalfBits
{
    public static bool Equal(Half a, Half b) => BitConverter.HalfToUInt16Bits(a) == BitConverter.HalfToUInt16Bits(b);
    public static bool Equal(LocalPosition a, LocalPosition b) => Equal(a.X, b.X) && Equal(a.Y, b.Y) && Equal(a.Z, b.Z);
    public static bool IsPositiveZero(CellVelocity value) => BitConverter.HalfToUInt16Bits(value.X) == 0 &&
        BitConverter.HalfToUInt16Bits(value.Y) == 0 && BitConverter.HalfToUInt16Bits(value.Z) == 0;
}
