using CuriousContraptions.Abstractions;
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
    public bool At(PortRole role) => role switch
    {
        PortRole.FirstIn => (Inputs & ElectricalInputState.FirstOnly)!=0,
        PortRole.SecondIn => (Inputs & ElectricalInputState.SecondOnly)!=0,
        PortRole.PowerIn => Supplied,
        PortRole.Supply => Active,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}

/// <summary>One drive slot's exact supply input consumed by a committed physics tick, with its resulting motion, keyed by the typed
/// <see cref="SupplyConsumer"/> that names the slot's joint. Every prismatic slot comes first, then every revolute slot, each in
/// identity order. A prismatic slot reads its axial offset (m) and speed (m/s) between its limits, and whether its moving body was in
/// solver contact with a body other than its base during that tick. A revolute slot's coordinate is its committed signed travel (rad),
/// accumulated without wrapping, so a Continuous hinge's travel is not bounded by the ±π it reports as its limits; its speed is the
/// relative spin about the hinge axis (rad/s), and it reports no moving contact.</summary>
public readonly record struct ActuatorDriveRead(
    SupplyConsumer Drive, GpuBodyId Owner, bool SupplyClosed, bool MovingContact, float Coordinate, float Speed, float Lower,
    float Upper)
{
    /// <summary>The slot order: prismatic slots before revolute slots, each by joint identity.</summary>
    public (SupplyConsumerKind Kind, ulong Joint) Slot => (Drive.Kind, Drive.Kind switch
    {
        SupplyConsumerKind.PrismaticDrive => Drive.PrismaticJoint.Value,
        SupplyConsumerKind.RevoluteDrive => Drive.RevoluteJoint.Value,
        _ => throw new InvalidOperationException("Undefined actuator drive kind.")
    });

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
        if (!Enum.IsDefined(Drive.Kind) || Slot.Joint == 0 || Owner.Value == 0 ||
            (Drive.Kind == SupplyConsumerKind.RevoluteDrive && MovingContact) || !float.IsFinite(Coordinate) || !float.IsFinite(Speed) ||
            !float.IsFinite(Lower) || !float.IsFinite(Upper) || Lower > Upper)
            throw new ArgumentException("Invalid committed actuator observation.");
    }
}

public readonly record struct RopeGuideRead(RopeGuideId Guide, float Travel, float Angle)
{
    public void Validate()
    {
        if (Guide.Value == 0 || !float.IsFinite(Travel) || !float.IsFinite(Angle))
            throw new ArgumentException("Invalid committed rope guide observation.");
    }
}

/// <summary>A compliant surface's committed bed deflection (Story 10.5): the depth below its rest plane of the deepest engaged
/// support sample, within its stroke; zero when nothing is engaged.</summary>
public readonly record struct CompliantSurfaceRead(GpuBodyId Frame, float Deflection)
{
    public void Validate()
    {
        if (Frame.Value == 0 || !float.IsFinite(Deflection) || Deflection < 0f)
            throw new ArgumentException("Invalid committed compliant surface observation.");
    }
}

/// <summary>Immutable committed presentation queries. Population comes from the compiled world.</summary>
public readonly struct PhysicsPresentationRead
{
    private readonly RopeSpanRead[]? _ropes;
    public ReadOnlySpan<RopeSpanRead> Ropes => _ropes;
    private readonly RopeGuideRead[]? _guides;
    public ReadOnlySpan<RopeGuideRead> Guides => _guides;
    private readonly CompliantSurfaceRead[]? _surfaces;
    public ReadOnlySpan<CompliantSurfaceRead> Surfaces => _surfaces;
    private readonly ActuatorDriveRead[]? _actuators;
    private readonly ContactLoadSensorRead[]? _loads;
    private readonly ElectricalLogicRead[]? _logic;
    public ReadOnlySpan<ElectricalLogicRead> Logic => _logic;
    public ReadOnlySpan<ActuatorDriveRead> Actuators => _actuators;
    public ReadOnlySpan<ContactLoadSensorRead> Loads => _loads;
    public PhysicsPresentationRead(ReadOnlySpan<ActuatorDriveRead> actuators, ReadOnlySpan<ContactLoadSensorRead> loads, ReadOnlySpan<ElectricalLogicRead> logic = default,
        ReadOnlySpan<RopeSpanRead> ropes = default, ReadOnlySpan<RopeGuideRead> guides = default,
        ReadOnlySpan<CompliantSurfaceRead> surfaces = default)
    {
        for (var i = 0; i < surfaces.Length; i++)
        {
            surfaces[i].Validate();
            if (i > 0 && surfaces[i - 1].Frame.Value >= surfaces[i].Frame.Value)
                throw new ArgumentException("Compliant surface observations require ordered unique frames.");
        }
        _surfaces = surfaces.ToArray();
        for (var i = 0; i < ropes.Length; i++)
        {
            ropes[i].Validate();
            if (i > 0 && ropes[i - 1].Id.Value >= ropes[i].Id.Value)
                throw new ArgumentException("Rope observations require ordered unique identities.");
        }
        _ropes = ropes.ToArray();
        for (var i = 0; i < guides.Length; i++)
        {
            guides[i].Validate();
            if (i > 0 && guides[i - 1].Guide.Value >= guides[i].Guide.Value)
                throw new ArgumentException("Rope guide observations require ordered unique identities.");
        }
        _guides = guides.ToArray();
        for (var i = 0; i < actuators.Length; i++)
        {
            actuators[i].Validate();
            if (i > 0 && actuators[i - 1].Slot.CompareTo(actuators[i].Slot) >= 0)
                throw new ArgumentException("Actuator observations require ordered unique drive slots.");
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
        Actuators.SequenceEqual(other.Actuators) && Loads.SequenceEqual(other.Loads) && Logic.SequenceEqual(other.Logic) && Ropes.SequenceEqual(other.Ropes) &&
        Guides.SequenceEqual(other.Guides) && Surfaces.SequenceEqual(other.Surfaces);
}

public static partial class PhysicsGpuAbi
{
    /// <summary>Projection of validated output plus the very inputs supplied to this tick; no supply reconstruction.</summary>
    private static ActuatorDriveRead[] ReadActuatorPresentation(ReadOnlySpan<byte> bytes, PhysicsTickInputs inputs, bool admission, PhysicsWorldLayout layout)
    {
        // One row per drive slot: every prismatic slot, then every revolute slot. A slot's supply is the very input of this tick.
        var actuators = new ActuatorDriveRead[layout.Prismatics + layout.Revolutes];
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        for (var i = 0; i < layout.Prismatics; i++)
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
            actuators[i] = new(SupplyConsumer.Prismatic(joint.Id), joint.BodyA, !admission && inputs.SupplyClosed[i],
                ReadPrismaticMovingContact(bytes, i), axial.Error, speed, joint.Lower, joint.Upper);
        }
        Span<RevoluteRow> hinge = stackalloc RevoluteRow[RevoluteRows.Count];
        for (var i = 0; i < layout.Revolutes; i++)
        {
            var slot = layout.Prismatics + i;
            var joint = ReadRevolute(bytes, i);
            var record = bytes.Slice(layout.RevoluteOffset(i), RevoluteBytes);
            var a = bytes.Slice(layout.BodyOffset((int)R32(record, RevoluteBodyAOffset)), BodyBytes);
            var b = bytes.Slice(layout.BodyOffset((int)R32(record, RevoluteBodyBOffset)), BodyBytes);
            RevoluteRows.Prepare(PresentationFrame(a), PresentationFrame(b), joint.FrameA, joint.FrameB, joint.Lower, joint.Upper, hinge);
            var axial = hinge[(int)RevoluteRowKind.Axial];
            var spin = Vector3.Dot(axial.AngularA, PresentationVector(a, BodyAngularVelocityOffset)) +
                Vector3.Dot(axial.AngularB, PresentationVector(b, BodyAngularVelocityOffset));
            actuators[slot] = new(SupplyConsumer.Revolute(joint.Id), joint.BodyA, !admission && inputs.SupplyClosed[slot], false,
                ReadRevoluteState(bytes, i).Travel, spin, joint.Lower, joint.Upper);
        }
        return actuators;
    }

    public static PhysicsPresentationRead ReadPresentation(ReadOnlySpan<byte> bytes, PhysicsTickInputs inputs, bool admission)
    {
        var layout = Layout(bytes);
        var actuators = ReadActuatorPresentation(bytes, inputs, admission, layout);
        var loads = new ContactLoadSensorRead[ReadContactLoadCount(bytes)];
        for (var i = 0; i < loads.Length; i++) loads[i] = ReadContactLoadSensor(bytes, i);
        var ropes = new RopeSpanRead[layout.TensileSpans];
        var guideMap = new Dictionary<RopeGuideId, (float Travel, float Angle)>();
        if (inputs.Guides is { Length: > 0 })
        {
            foreach (var g in inputs.Guides)
                guideMap.TryAdd(g.Id, (0f, 0f));
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

            var routeGuides = new RouteGuideFrame[ordered.Length > 1 ? ordered.Length - 1 : 0];
            for (var i = 0; i < ordered.Length - 1; i++)
            {
                var guideBody = ordered[i].raw.Span.BodyB;
                var radius = ordered[i].raw.Span.GuideRadius;
                var offset = ordered[i].raw.Span.GuideOffset;
                if (ordered[i].raw.Span.BodyB == ordered[i + 1].raw.Span.BodyA)
                {
                    var angle = radius > 0f
                        ? ((travel / radius) % MathF.Tau + MathF.Tau) % MathF.Tau
                        : 0f;
                    if (inputs.Guides is { Length: > 0 })
                    {
                        foreach (var g in inputs.Guides)
                        {
                            if (g.Body == guideBody &&
                                MathF.Abs(g.Radius - radius) < 1e-4f &&
                                Vector3.DistanceSquared(g.Offset, offset) < 1e-4f)
                            {
                                guideMap[g.Id] = (travel, angle);
                                break;
                            }
                        }
                    }
                }
                routeGuides[i] = new(ordered[i].raw.FrameBPos, ordered[i].raw.FrameBRot, radius, offset);
            }

            var terminalA = ordered[0].raw.From;
            var terminalB = ordered[^1].raw.To;
            var evaluated = RopeGeometry.EvaluateRoute(terminalA, terminalB, routeGuides);
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

        var guides = guideMap.OrderBy(kv => kv.Key.Value)
            .Select(kv => new RopeGuideRead(kv.Key, kv.Value.Travel, kv.Value.Angle)).ToArray();
        var surfaces = new CompliantSurfaceRead[layout.CompliantSurfaces];
        for (var i = 0; i < surfaces.Length; i++) surfaces[i] = ReadCompliantSurfaceDeflection(bytes, i);
        Array.Sort(surfaces, (a, b) => a.Frame.Value.CompareTo(b.Frame.Value));
        return new(actuators, loads, inputs.Logic, ropes, guides, surfaces);
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

