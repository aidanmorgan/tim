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

public readonly record struct PulleyWheelRead(GpuBodyId Pulley, float Travel, float Angle)
{
    public void Validate()
    {
        if (Pulley.Value == 0 || !float.IsFinite(Travel) || !float.IsFinite(Angle))
            throw new ArgumentException("Invalid committed pulley observation.");
    }
}

/// <summary>Immutable committed presentation queries. Population comes from the compiled world.</summary>
public readonly struct PhysicsPresentationRead
{
    private readonly RopeSpanRead[]? _ropes;
    public ReadOnlySpan<RopeSpanRead> Ropes => _ropes;
    private readonly PulleyWheelRead[]? _pulleys;
    public ReadOnlySpan<PulleyWheelRead> Pulleys => _pulleys;
    private readonly ActuatorDriveRead[]? _actuators;
    private readonly ContactLoadSensorRead[]? _loads;
    private readonly ElectricalLogicRead[]? _logic;
    public ReadOnlySpan<ElectricalLogicRead> Logic => _logic;
    public ReadOnlySpan<ActuatorDriveRead> Actuators => _actuators;
    public ReadOnlySpan<ContactLoadSensorRead> Loads => _loads;
    public PhysicsPresentationRead(ReadOnlySpan<ActuatorDriveRead> actuators, ReadOnlySpan<ContactLoadSensorRead> loads, ReadOnlySpan<ElectricalLogicRead> logic = default,
        ReadOnlySpan<RopeSpanRead> ropes = default, ReadOnlySpan<PulleyWheelRead> pulleys = default)
    {
        for (var i = 0; i < ropes.Length; i++)
        {
            ropes[i].Validate();
            if (i > 0 && ropes[i - 1].Id.Value >= ropes[i].Id.Value)
                throw new ArgumentException("Rope observations require ordered unique identities.");
        }
        _ropes = ropes.ToArray();
        for (var i = 0; i < pulleys.Length; i++)
        {
            pulleys[i].Validate();
            if (i > 0 && pulleys[i - 1].Pulley.Value >= pulleys[i].Pulley.Value)
                throw new ArgumentException("Pulley observations require ordered unique identities.");
        }
        _pulleys = pulleys.ToArray();
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
        Actuators.SequenceEqual(other.Actuators) && Loads.SequenceEqual(other.Loads) && Logic.SequenceEqual(other.Logic) && Ropes.SequenceEqual(other.Ropes) && Pulleys.SequenceEqual(other.Pulleys);
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
        var pulleyMap = new Dictionary<GpuBodyId, (float Travel, float Angle)>();
        if (inputs.Pulleys is { Length: > 0 })
        {
            foreach (var p in inputs.Pulleys)
                pulleyMap.TryAdd(p, (0f, 0f));
        }

        var rawSpans = new (TensileSpanDeclaration Span, Vector3 From, Vector3 To, float Length, RopeIntegrity Integrity, float Impulse, float Travel, RopeSpanId CutSpan, Vector3 FrameBPos, Quaternion FrameBRot)[layout.TensileSpans];
        for (var i = 0; i < layout.TensileSpans; i++)
        {
            var span = ReadTensileSpan(bytes, i);
            var record = bytes.Slice(layout.TensileSpanOffset(i), TensileSpanBytes);
            var a = PresentationFrame(bytes.Slice(layout.BodyOffset((int)R32(record, TensileBodyAOffset)), BodyBytes));
            var b = PresentationFrame(bytes.Slice(layout.BodyOffset((int)R32(record, TensileBodyBOffset)), BodyBytes));
            var from = a.Position + Vector3.Transform(span.LocalA, a.Rotation);
            var to = b.Position + Vector3.Transform(span.LocalB, b.Rotation);
            var length = Vector3.Distance(from, to);
            var integrity = (RopeIntegrity)R32(record, TensileIntegrityOffset);
            var impulse = RF(record, TensileImpulseOffset);
            var travel = RF(record, TensileTravelOffset);
            var cutSpan = new RopeSpanId(R64(record, TensileCutSpanOffset));
            rawSpans[i] = (span, from, to, length, integrity, impulse, travel, cutSpan, b.Position, b.Rotation);
        }

        var routeGroups = rawSpans.Select((raw, index) => (raw, index))
            .GroupBy(x => x.raw.Span.Route);

        foreach (var group in routeGroups)
        {
            var ordered = group.OrderBy(x => x.raw.Span.Order).ToArray();
            var travel = ordered.Length > 0 ? ordered[0].raw.Travel : 0f;
            var impulse = ordered.Length > 0 ? ordered[0].raw.Impulse : 0f;

            var guides = new (Vector3 Position, Quaternion Rotation)[ordered.Length > 1 ? ordered.Length - 1 : 0];
            for (var i = 0; i < ordered.Length - 1; i++)
            {
                if (ordered[i].raw.Span.BodyB == ordered[i + 1].raw.Span.BodyA)
                {
                    var guideBody = ordered[i].raw.Span.BodyB;
                    var angle = ((travel / RopeGeometry.PulleyRadius) % MathF.Tau + MathF.Tau) % MathF.Tau;
                    pulleyMap[guideBody] = (travel, angle);
                }
                guides[i] = (ordered[i].raw.FrameBPos, ordered[i].raw.FrameBRot);
            }

            var terminalA = ordered[0].raw.From;
            var terminalB = ordered[^1].raw.To;
            var evaluated = RopeGeometry.EvaluateRoute(terminalA, terminalB, guides);
            var totalLength = evaluated.TotalLength;
            var totalMax = ordered.Sum(x => x.raw.Span.MaximumLength);
            var isCutOrIncomplete = ordered.Any(x => x.raw.Integrity == RopeIntegrity.Cut || !x.raw.Span.Complete);
            var routeState = isCutOrIncomplete
                ? RopeState.Open
                : (totalLength >= totalMax - 1e-3f || impulse > 0f ? RopeState.Taut : RopeState.Slack);

            foreach (var item in ordered)
            {
                var r = item.raw;
                ropes[item.index] = new(r.Span.Id, r.Span.Route, r.Span.Order, r.Span.BodyA, r.Span.BodyB,
                    r.From, r.To, r.Span.MaximumLength, r.Length, r.Integrity, routeState, r.CutSpan);
            }
        }

        var pulleys = pulleyMap.OrderBy(kv => kv.Key.Value)
            .Select(kv => new PulleyWheelRead(kv.Key, kv.Value.Travel, kv.Value.Angle)).ToArray();
        return new(actuators, loads, inputs.Logic, ropes, pulleys);
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

