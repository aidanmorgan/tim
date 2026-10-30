using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct RopeSpanRead(RopeSpanId Id, RopeRouteId Route, uint Order, GpuBodyId BodyA, GpuBodyId BodyB,
    Vector3 AttachmentA, Vector3 AttachmentB, float MaximumLength, float CurrentLength,
    RopeIntegrity Integrity, RopeState State, RopeSpanId CutSpan)
{
    public void Validate()
    {
        if (Id.Value == 0 || Route.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB ||
            !Enum.IsDefined(Integrity) || !Enum.IsDefined(State) || !Finite(AttachmentA) || !Finite(AttachmentB) ||
            !float.IsFinite(MaximumLength) || MaximumLength < .05f || MaximumLength > 200f ||
            !float.IsFinite(CurrentLength) || CurrentLength < 0f || CurrentLength != Vector3.Distance(AttachmentA, AttachmentB) ||
            (State == RopeState.Slack && CurrentLength >= MaximumLength) ||
            (State == RopeState.Taut && CurrentLength < MaximumLength) ||
            (Integrity == RopeIntegrity.Intact) != (CutSpan.Value == 0) ||
            (Integrity == RopeIntegrity.Cut && State != RopeState.Open))
            throw new ArgumentException("Invalid committed rope observation.");
    }
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

[Flags]
public enum ElectricalInputState : byte { Neither=0, FirstOnly=1, SecondOnly=2, Both=3 }
public readonly record struct ElectricalLogicRead(GpuBodyId Owner, ElectricalInputState Inputs, bool Supplied, bool Active)
{
    public void Validate()
    {
        if (Owner.Value==0 || !Enum.IsDefined(Inputs) || (Active && !Supplied))
            throw new ArgumentException("Invalid committed electrical logic observation.");
    }
    public bool At(WorkshopSocket socket) => socket switch
    {
        WorkshopSocket.FirstIn => (Inputs & ElectricalInputState.FirstOnly)!=0,
        WorkshopSocket.SecondIn => (Inputs & ElectricalInputState.SecondOnly)!=0,
        WorkshopSocket.PowerIn => Supplied,
        WorkshopSocket.Supply => Active,
        _ => throw new ArgumentOutOfRangeException(nameof(socket))
    };
}

/// <summary>The exact drive input consumed by one committed physics tick, with its resulting axial motion and whether its moving
/// body was in solver contact with a body other than its base during that tick.</summary>
public readonly record struct ActuatorDriveRead(
    PrismaticConstraintId Joint, GpuBodyId Owner, bool SupplyClosed, bool MovingContact, float Coordinate, float Speed, float Lower,
    float Upper)
{
    /// <summary>The game-grade envelope's resting contact slop, 0.5 mm: a coordinate within it of a stop is at that stop.</summary>
    public const float StopSlop = 5e-4f;
    /// <summary>The game-grade envelope's at-rest linear speed, 0.01 m/s (its sleep threshold): slower axial motion is at rest.</summary>
    public const float RestSpeed = .01f;

    /// <summary>Something is in the way (CAT-051 Q3, coordinator decision 2, 22 Oct 2026): the moving body is in solver contact with
    /// another body, held at rest short of both stops. A slider resting in its interior with nothing touching it (an inverted or
    /// tilted mount at its spring-gravity equilibrium) is not blocked. A single state for both directions (Q4).</summary>
    public bool Blocked => MovingContact && Coordinate > Lower + StopSlop && Coordinate < Upper - StopSlop && MathF.Abs(Speed) < RestSpeed;
    public void Validate()
    {
        if (Joint.Value == 0 || Owner.Value == 0 || !float.IsFinite(Coordinate) || !float.IsFinite(Speed) ||
            !float.IsFinite(Lower) || !float.IsFinite(Upper) || Lower > Upper)
            throw new ArgumentException("Invalid committed actuator observation.");
    }
}

/// <summary>Immutable committed presentation queries. Population comes from the compiled world.</summary>
public readonly struct PhysicsPresentationRead
{
    private readonly RopeSpanRead[]? _ropes;
    public ReadOnlySpan<RopeSpanRead> Ropes => _ropes;
    private readonly ActuatorDriveRead[]? _actuators;
    private readonly ContactLoadSensorRead[]? _loads;
    private readonly ElectricalLogicRead[]? _logic;
    public ReadOnlySpan<ElectricalLogicRead> Logic => _logic;
    public ReadOnlySpan<ActuatorDriveRead> Actuators => _actuators;
    public ReadOnlySpan<ContactLoadSensorRead> Loads => _loads;
    public PhysicsPresentationRead(ReadOnlySpan<ActuatorDriveRead> actuators, ReadOnlySpan<ContactLoadSensorRead> loads, ReadOnlySpan<ElectricalLogicRead> logic = default,
        ReadOnlySpan<RopeSpanRead> ropes = default)
    {
        for (var i = 0; i < ropes.Length; i++)
        {
            ropes[i].Validate();
            if (i > 0 && ropes[i - 1].Id.Value >= ropes[i].Id.Value)
                throw new ArgumentException("Rope observations require ordered unique identities.");
        }
        _ropes = ropes.ToArray();
        for (var i = 0; i < actuators.Length; i++)
        {
            actuators[i].Validate();
            if (i > 0 && actuators[i - 1].Joint.Value >= actuators[i].Joint.Value)
                throw new ArgumentException("Actuator observations require ordered unique identities.");
        }
        for (var i = 0; i < loads.Length; i++)
        {
            var load = loads[i];
            if (load.Sensor.Value == 0 || load.Frame.Value == 0 || load.Region.Value == 0 ||
                !float.IsFinite(load.MeasuredMass) || load.MeasuredMass < 0 || !Enum.IsDefined(load.Phase) ||
                (i > 0 && loads[i - 1].Sensor.Value >= load.Sensor.Value))
                throw new ArgumentException("Invalid committed contact-load observation.");
        }
        for(var i=0;i<logic.Length;i++)
        {
            logic[i].Validate();
            if(i>0 && logic[i-1].Owner.Value>=logic[i].Owner.Value)
                throw new ArgumentException("Electrical logic observations require ordered unique owners.");
        }
        _actuators = actuators.ToArray(); _loads = loads.ToArray(); _logic=logic.ToArray();
    }
    public bool HasSameBits(PhysicsPresentationRead other) =>
        Actuators.SequenceEqual(other.Actuators) && Loads.SequenceEqual(other.Loads) && Logic.SequenceEqual(other.Logic) && Ropes.SequenceEqual(other.Ropes);
}

public static partial class PhysicsGpuAbi
{
    /// <summary>Projection of validated output plus the very inputs supplied to this tick; no supply reconstruction.</summary>
    public static PhysicsPresentationRead ReadPresentation(ReadOnlySpan<byte> bytes, PhysicsTickInputs inputs, bool admission)
    {
        var layout = Layout(bytes);
        var actuators = new ActuatorDriveRead[layout.Prismatics];
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        for (var i = 0; i < actuators.Length; i++)
        {
            var joint = ReadPrismatic(bytes, i);
            var record = bytes.Slice(layout.PrismaticOffset(i), PrismaticBytes);
            var a = bytes.Slice(layout.BodyOffset((int)R32(record, PrismaticBodyAOffset)), BodyBytes);
            var b = bytes.Slice(layout.BodyOffset((int)R32(record, PrismaticBodyBOffset)), BodyBytes);
            PrismaticRows.Prepare(PresentationFrame(a), PresentationFrame(b), joint.FrameA, joint.FrameB,
                joint.Lower, joint.Upper, rows);
            var axial = rows[(int)PrismaticRowKind.Spring];
            var speed = Vector3.Dot(axial.LinearA, PresentationVector(a, BodyVelocityOffset)) +
                Vector3.Dot(axial.LinearB, PresentationVector(b, BodyVelocityOffset)) +
                Vector3.Dot(axial.AngularA, PresentationVector(a, BodyAngularVelocityOffset)) +
                Vector3.Dot(axial.AngularB, PresentationVector(b, BodyAngularVelocityOffset));
            actuators[i] = new(joint.Id, joint.BodyA, !admission && inputs.SupplyClosed[i], ReadPrismaticMovingContact(bytes, i),
                axial.Error, speed, joint.Lower, joint.Upper);
        }
        var loads = new ContactLoadSensorRead[ReadContactLoadCount(bytes)];
        for (var i = 0; i < loads.Length; i++) loads[i] = ReadContactLoadSensor(bytes, i);
        var ropes = new RopeSpanRead[layout.TensileSpans];
        for (var i = 0; i < ropes.Length; i++)
        {
            var span = ReadTensileSpan(bytes, i);
            var record = bytes.Slice(layout.TensileSpanOffset(i), TensileSpanBytes);
            var a = PresentationFrame(bytes.Slice(layout.BodyOffset((int)R32(record, TensileBodyAOffset)), BodyBytes));
            var b = PresentationFrame(bytes.Slice(layout.BodyOffset((int)R32(record, TensileBodyBOffset)), BodyBytes));
            var from = a.Position + Vector3.Transform(span.LocalA, a.Rotation);
            var to = b.Position + Vector3.Transform(span.LocalB, b.Rotation);
            var length = Vector3.Distance(from, to);
            var integrity = (RopeIntegrity)R32(record, TensileIntegrityOffset);
            ropes[i] = new(span.Id, span.Route, span.Order, span.BodyA, span.BodyB, from, to, span.MaximumLength, length,
                integrity, integrity == RopeIntegrity.Cut || !span.Complete ? RopeState.Open :
                    length < span.MaximumLength ? RopeState.Slack : RopeState.Taut, new(R64(record, TensileCutSpanOffset)));
        }
        return new(actuators, loads, inputs.Logic, ropes);
    }
    private static Vector3 PresentationVector(ReadOnlySpan<byte> body, int offset) =>
        new(RF(body, offset), RF(body, offset + 4), RF(body, offset + 8));
    private static ConstraintBodyFrame PresentationFrame(ReadOnlySpan<byte> body)
    {
        var value = ReadRotation(body, BodyRotationOffset);
        var rotation = new Quaternion(value.X, value.Y, value.Z, value.W);
        var position = PresentationPosition(body);
        var centre = ReadCentreOfMass(body);
        return new(position, rotation, position + Vector3.Transform(new((float)centre.X, (float)centre.Y, (float)centre.Z), rotation));
    }
    private static Vector3 PresentationPosition(ReadOnlySpan<byte> body)
    {
        var cell = ReadCell(body, BodyCellOffset); var local = ReadLocal(body, BodyLocalOffset);
        return new((cell.X + local.X) / 16f, (cell.Y + local.Y) / 16f, (cell.Z + local.Z) / 16f);
    }
}

