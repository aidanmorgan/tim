using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
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
public readonly record struct ElectricalLogicRead(SupplyNodeId Node, GpuBodyId Owner, ElectricalInputState Inputs, bool Supplied, bool Active)
{
    public void Validate()
    {
        if (Node.Value==0 || Owner.Value==0 || !Enum.IsDefined(Inputs) || (Active && !Supplied))
            throw new ArgumentException("Invalid committed electrical logic observation.");
    }
    public bool At(CuriousContraptions.Abstractions.Electrical.SupplyTerminal role) => role switch
    {
        CuriousContraptions.Abstractions.Electrical.SupplyTerminal.FirstIn => (Inputs & ElectricalInputState.FirstOnly)!=0,
        CuriousContraptions.Abstractions.Electrical.SupplyTerminal.SecondIn => (Inputs & ElectricalInputState.SecondOnly)!=0,
        CuriousContraptions.Abstractions.Electrical.SupplyTerminal.PowerIn => Supplied,
        CuriousContraptions.Abstractions.Electrical.SupplyTerminal.Output => Active,
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
    float Upper, PrismaticMotorRead? Motor = null)
{
    /// <summary>The slot order: prismatic slots before revolute slots, each by joint identity.</summary>
    public (SupplyConsumerKind Kind, ulong Joint) Slot => (Drive.Kind, Drive.Kind switch
    {
        SupplyConsumerKind.PrismaticDrive => Drive.PrismaticJoint.Value,
        SupplyConsumerKind.RevoluteDrive => Drive.RevoluteJoint.Value,
        _ => throw new InvalidOperationException("Undefined actuator drive kind.")
    });

    public void Validate()
    {
        if (Drive.Kind is not (SupplyConsumerKind.PrismaticDrive or SupplyConsumerKind.RevoluteDrive) ||
            Slot.Joint == 0 || Owner.Value == 0 ||
            (Drive.Kind == SupplyConsumerKind.RevoluteDrive && (MovingContact || Motor.HasValue)) || !float.IsFinite(Coordinate) || !float.IsFinite(Speed) ||
            !float.IsFinite(Lower) || !float.IsFinite(Upper) || Lower > Upper)
            throw new ArgumentException("Invalid committed actuator observation.");
        Motor?.Validate();
    }
}

/// <summary>Committed state of one mechanical transmission slot: its normalized reaction impulse conjugate to the driven coordinate
/// (N*s or N*m*s; zero while disengaged), and its accepted signed interface work and positive transfer (J).</summary>
public readonly record struct MechanicalTransmissionStateRead(float Impulse, float Work, float PositiveWork);

public readonly record struct MechanicalTransmissionRead(
    MechanicalTransmissionId Transmission,
    GpuBodyId Owner,
    float Closure,
    TransmissionEngagementPhase Phase)
{
    public void Validate()
    {
        if (Transmission.Value == 0 || Owner.Value == 0 || !float.IsFinite(Closure) || Closure < 0f || Closure > 1f || !Enum.IsDefined(Phase))
            throw new ArgumentException("Invalid committed mechanical transmission observation.");
        var consistent = Phase switch
        {
            TransmissionEngagementPhase.Open => PhysicsDeclarationBounds.Zero(Closure),
            TransmissionEngagementPhase.Engaged => Closure == 1f,
            TransmissionEngagementPhase.Closing => Closure > 0f && Closure < 1f,
            TransmissionEngagementPhase.Opening => Closure > 0f,
            _ => false
        };
        if (!consistent) throw new ArgumentException("Committed transmission phase disagrees with its closure.");
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
    private readonly MechanicalTransmissionRead[]? _transmissions;
    private readonly PrismaticRetentionRead[]? _retentions;
    public ReadOnlySpan<PrismaticRetentionRead> Retentions => _retentions;
    public ReadOnlySpan<ElectricalLogicRead> Logic => _logic;
    public ReadOnlySpan<ActuatorDriveRead> Actuators => _actuators;
    public ReadOnlySpan<ContactLoadSensorRead> Loads => _loads;
    public ReadOnlySpan<MechanicalTransmissionRead> Transmissions => _transmissions;
    public PhysicsPresentationRead(ReadOnlySpan<ActuatorDriveRead> actuators, ReadOnlySpan<ContactLoadSensorRead> loads, ReadOnlySpan<ElectricalLogicRead> logic = default,
        ReadOnlySpan<RopeSpanRead> ropes = default, ReadOnlySpan<RopeGuideRead> guides = default,
        ReadOnlySpan<CompliantSurfaceRead> surfaces = default, ReadOnlySpan<MechanicalTransmissionRead> transmissions = default, ReadOnlySpan<PrismaticRetentionRead> retentions = default)
    {
        for (var i = 0; i < surfaces.Length; i++)
        {
            surfaces[i].Validate();
            if (i > 0 && surfaces[i - 1].Frame.Value >= surfaces[i].Frame.Value)
                throw new ArgumentException("Compliant surface observations require ordered unique frames.");
        }
        for (var i = 0; i < retentions.Length; i++)
        {
            retentions[i].Validate();
            if (i > 0 && retentions[i - 1].Id.Value >= retentions[i].Id.Value)
                throw new ArgumentException("Retention observations require ordered unique identities.");
        }
        _retentions = retentions.ToArray();
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
        ValidateLoads(loads);
        for(var i=0;i<logic.Length;i++)
        {
            logic[i].Validate();
            if(i>0 && logic[i-1].Node.Value>=logic[i].Node.Value)
                throw new ArgumentException("Electrical logic observations require ordered unique owners.");
        }
        for (var i = 0; i < transmissions.Length; i++)
        {
            transmissions[i].Validate();
            if (i > 0 && transmissions[i - 1].Transmission.Value >= transmissions[i].Transmission.Value)
                throw new ArgumentException("Mechanical transmission observations require ordered unique identities.");
        }
        _actuators = actuators.ToArray(); _loads = loads.ToArray(); _logic=logic.ToArray(); _transmissions = transmissions.ToArray();
    }
    private static void ValidateLoads(ReadOnlySpan<ContactLoadSensorRead> loads)
    {
        for (var i = 0; i < loads.Length; i++)
        {
            var load = loads[i];
            if (load.Sensor.Value == 0 || load.Frame.Value == 0 || load.Region.Value == 0 ||
                !float.IsFinite(load.MeasuredMass) || load.MeasuredMass < 0 || !Enum.IsDefined(load.Phase) ||
                (i > 0 && loads[i - 1].Sensor.Value >= load.Sensor.Value))
                throw new ArgumentException("Invalid committed contact-load observation.");
        }
    }

    public bool HasSameBits(PhysicsPresentationRead other) =>
        Actuators.SequenceEqual(other.Actuators) && Loads.SequenceEqual(other.Loads) && Logic.SequenceEqual(other.Logic) && Ropes.SequenceEqual(other.Ropes) &&
        Guides.SequenceEqual(other.Guides) && Surfaces.SequenceEqual(other.Surfaces) && Transmissions.SequenceEqual(other.Transmissions) &&
        Retentions.SequenceEqual(other.Retentions);
}
