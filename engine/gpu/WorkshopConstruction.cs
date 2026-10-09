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
        ValidateCommitted();
        // Authored input admission is separate from rounded, normalised GPU output.
        var norm = (double)X * (double)X + (double)Y * (double)Y + (double)Z * (double)Z + (double)W * (double)W;
        if (norm < 0.999 || norm > 1.001)
            throw new ArgumentException("Canonical rotation must represent a unit quaternion.");
    }

    public void ValidateCommitted()
    {
        if (!Half.IsFinite(X) || !Half.IsFinite(Y) || !Half.IsFinite(Z) || !Half.IsFinite(W) ||
            X < (Half)(-1) || X > (Half)1 || Y < (Half)(-1) || Y > (Half)1 ||
            Z < (Half)(-1) || Z > (Half)1 || W < (Half)(-1) || W > (Half)1 ||
            (X == (Half)0 && Y == (Half)0 && Z == (Half)0 && W == (Half)0))
            throw new ArgumentException("Unsupported committed rotation.");
    }
}

/// <summary>The immutable declared material of one ball kind: every dynamic sphere is this record plus a kind. Friction, the bounce
/// threshold and the rolling-resistance coefficient are declared here, never compiler or solver constants. Render adapters widen these
/// values.</summary>
public readonly record struct BallMaterial(
    Metres Radius, Kilograms Mass, Restitution Bounce, InverseSeconds Drag, Acceleration Buoyancy,
    FrictionCoefficient Friction, LinearSpeed BounceThreshold, RollingResistance Rolling)
{
    /// <summary>Game-scale declarations (parts/catalog/*.tres carry the same bits): Basketball 0.34 m / 1 kg / bounce 0.55 / rolling
    /// resistance 0.035 (an inflated shell); Bowling ball 0.28 m / 4 kg / bounce 0.14 / rolling resistance 0.03 (a smaller, heavier hard shell). Both share
    /// drag 0.04 1/s, no buoyancy, friction 0.3 and a 0.1 m/s bounce threshold. A ball struck to 1 m/s on the bench rests within 4.5 s.</summary>
    public static BallMaterial For(WorkshopPartKind kind) => kind switch
    {
        WorkshopPartKind.Basketball => new(new((Half)0.34), new((Half)1), new((Half)0.55), new((Half)0.04), new((Half)0), new((Half).3), new((Half).1), new((Half).035)),
        WorkshopPartKind.BowlingBall => new(new((Half)0.28), new((Half)4), new((Half)0.14), new((Half)0.04), new((Half)0), new((Half).3), new((Half).1), new((Half).03)),
        _ => throw new ArgumentException("Unsupported ball kind.")
    };
    public void Validate(WorkshopPartKind kind)
    {
        var expected = For(kind);
        if (!HalfBits.Equal(Radius.Value, expected.Radius.Value) || !HalfBits.Equal(Mass.Value, expected.Mass.Value) ||
            !HalfBits.Equal(Bounce.Value, expected.Bounce.Value) || !HalfBits.Equal(Drag.Value, expected.Drag.Value) ||
            !HalfBits.Equal(Buoyancy.Value, expected.Buoyancy.Value) || !HalfBits.Equal(Friction.Value, expected.Friction.Value) ||
            !HalfBits.Equal(BounceThreshold.Value, expected.BounceThreshold.Value) || !HalfBits.Equal(Rolling.Value, expected.Rolling.Value))
            throw new ArgumentException("Only the declared material of this ball kind is admitted.");
    }
}

/// <summary>One record for every dynamic sphere; the kind names its declared material. Durable construction uses canonical bits and
/// identities, never a live Godot transform.</summary>
public readonly record struct WorkshopBall(
    GpuBodyId Id, WorkshopPartKind Kind, CellOrigin Cell, LocalPosition Local, CanonicalRotation Rotation, BallMaterial Material, bool Locked = false) : IWorkshopInstance
{
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;
    public void Validate()
    {
        Material.Validate(Kind);
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
/// <summary>Authored force region; evaluated only by the shared physical law.</summary>
public readonly record struct ReceiverForceRegion(MetreVector Minimum, MetreVector Maximum,
    Metres SupportHeight, Metres SupportMargin, Acceleration MaximumAcceleration)
{
    public static ReceiverForceRegion Free => Create(new((Half)0), new((Half)0));
    public static ReceiverForceRegion Create(Metres margin, Acceleration acceleration) => new(
        new((Half)(-1.1), (Half).5, (Half)(-1.1)), new((Half)1.1, (Half)1.5, (Half)1.1),
        new((Half).5), margin, acceleration);
    public void Validate() => new PlanarGuideDeclaration(new(1), new(2), new(3),
        RigidLocalPose.Identity, Minimum, Maximum, SupportHeight, SupportMargin, MaximumAcceleration).Validate();
}
public readonly record struct WorkshopReceiver(
    GpuBodyId Id, CellOrigin Cell, LocalPosition Local, CanonicalRotation Rotation, ReceiverCaptureSettings Capture, bool Locked = false) : IWorkshopInstance
{
    public ReceiverForceRegion ForceRegion { get; init; } = ReceiverForceRegion.Free;
    public WorkshopPartKind Kind => WorkshopPartKind.Receiver;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurves.Receiver;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Capture.Validate(); ForceRegion.Validate();
    }
}

/// <summary>Immutable authored Workshop composition; parts compile declarations for the shared engine.</summary>
public readonly record struct WorkshopConstruction(ConstructionRevision Revision, WorkshopCadenceSettings Settings, WorkshopInstances Instances, WorkshopPuzzle Puzzle = default, WorkshopConnections Connections = null!)
{
    public WorkshopBall? Ball => Instances.Find<WorkshopBall>();
    public WorkshopReceiver? Receiver => Instances.Find<WorkshopReceiver>();
    public WorkshopConstruction WithInstance(IWorkshopInstance instance) => this with { Instances = Instances.With(instance) };
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
        WorkbenchCapacity.ValidateConnected(Instances, Connections); Puzzle.Validate(this);
    }
}

/// <summary>Only UI/resource boundary code may convert wider external numbers into game values.</summary>
public static class WorkshopInput
{
    public static WorkshopBall Ball(WorkshopPartKind kind, GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var rotation = new CanonicalRotation(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw));
        var result = new WorkshopBall(id, kind, new(px.Cell, py.Cell, pz.Cell),
            new(px.Local, py.Local, pz.Local), rotation, BallMaterial.For(kind));
        result.Validate();
        return result;
    }
    /// <summary>Authored puzzles name this kind explicitly.</summary>
    public static WorkshopBall Basketball(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw) => Ball(WorkshopPartKind.Basketball, id, x, y, z, qx, qy, qz, qw);

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

    public static WorkshopWall Wall(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, WallDimensions dimensions)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopWall(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), dimensions);
        result.Validate(); return result;
    }

    public static WorkshopSwitch Switch(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, ContactTriggerSettings trigger)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopSwitch(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), trigger);
        result.Validate(); return result;
    }
    public static WorkshopDelay Delay(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, DelayDuration duration)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopDelay(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), duration);
        result.Validate(); return result;
    }
    public static WorkshopBumper Bumper(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, BumperWork work)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopBumper(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), work);
        result.Validate(); return result;
    }
    public static WorkshopDomino Domino(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopDomino(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)), DominoMaterial.Default);
        result.Validate(); return result;
    }
    public static WorkshopLamp Lamp(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopLamp(id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            new(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)));
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
    public static bool Equal(MetreVector a, MetreVector b) => Equal(a.X, b.X) && Equal(a.Y, b.Y) && Equal(a.Z, b.Z);
}

/// <summary>Canonical f32 bit identity, including signed zero; positive zero is the all-zero bit pattern.</summary>
public static class F32Bits
{
    public static bool Equal(float a, float b) => BitConverter.SingleToUInt32Bits(a) == BitConverter.SingleToUInt32Bits(b);
    public static bool IsPositiveZero(float x, float y, float z) =>
        (BitConverter.SingleToUInt32Bits(x) | BitConverter.SingleToUInt32Bits(y) | BitConverter.SingleToUInt32Bits(z)) == 0;
}
