using CuriousContraptions.Abstractions.Signal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public readonly record struct CompiledSceneId(ulong Value);
public readonly record struct CompiledPresentationBinding(PresentationProperty Property, PresentationDriver Driver, PresentationValueMapping Mapping,
    float From, float To, PresentationCurve Curve, float Duration, PresentationOverlap Overlap, float Threshold,
    PresentationTimerPhase Phase, float Response, float? Intermediate, PortRole? Socket,
    SimulationEventKind Event, PresentationEventMagnitude EventMagnitude, PresentationActuatorStateValues? StateValues = null);
public readonly record struct CompiledPresentationTrack(AnimationTargetId Target, GpuBodyId Owner, int Binding,
    CompiledPresentationBinding Declaration, ActivationNodeDeclaration Activation, float SourceCapacity, SupplyConsumer? Actuator = null, RopeGuideId Guide = default);

/// <summary>The complete runtime installation. No authored instance, package identity or bundle is retained.</summary>
public sealed class WorkshopCompiledScene
{
    public CompiledSceneId Identity { get; }
    public ConstructionRevision Revision { get; }
    public WorkshopCadenceSettings Settings { get; }
    public PhysicsSceneDeclaration Physics { get; }
    public ActivationNetwork Activation { get; }
    public ImmutableArray<CompiledPresentationTrack> Presentation { get; }
    public ImmutableArray<GpuBodyId> Owners { get; }
    /// <summary>The run-control table: one row per declared RunControl parameter of an unlocked instance, each naming the lane slot
    /// it drives while Running or Paused.</summary>
    public ImmutableArray<RunControlRow> RunControls { get; }

    public WorkshopCompiledScene(CompiledSceneId identity, ConstructionRevision revision, WorkshopCadenceSettings settings,
        PhysicsSceneDeclaration physics, ActivationNetwork activation, IEnumerable<CompiledPresentationTrack> presentation,
        IEnumerable<GpuBodyId> owners, IEnumerable<RunControlRow> runControls)
    {
        Identity = identity; Revision = revision; Settings = settings;
        Physics = physics ?? throw new ArgumentNullException(nameof(physics));
        Activation = activation ?? throw new ArgumentNullException(nameof(activation));
        Presentation = presentation.ToImmutableArray(); Owners = owners.ToImmutableArray();
        RunControls = runControls.ToImmutableArray();
        Validate();
    }

    /// <summary>The compiled row named <paramref name="id"/>, if the table has one.</summary>
    public bool TryRunControl(RunControlId id, out RunControlRow row)
    {
        foreach (var candidate in RunControls)
            if (candidate.Id == id) { row = candidate; return true; }
        row = default;
        return false;
    }

    public bool OwnsCapture(CapturePairKey pair) =>
        Physics.Sensors.ToArray().Any(sensor => sensor.Id == pair.Policy) &&
        Physics.Bodies.ToArray().Any(body => body.Id == pair.Target && body.Payload == PayloadPolicy.CaptureTarget);

    public void Validate()
    {
        if (Identity.Value == 0 || Revision.Value == 0) throw new ArgumentException("Compiled scene identity is absent.");
        Settings.Validate();
        var owners = Owners.ToHashSet();
        if (owners.Count != Owners.Length || Owners.Any(owner => owner.Value == 0))
            throw new ArgumentException("Compiled scene ownership is invalid.");
        ValidateRunControls(owners);
        if (Activation.Substeps != Settings.PhysicalStepsPerCommit)
            throw new ArgumentException("Compiled activation cadence differs from physics.");
        if (Physics.SupplyGraph.Nodes.ToArray().Any(node => !owners.Contains(node.Owner)))
            throw new ArgumentException("Electrical owner is outside the installed scene.");
        var nodes = Activation.Nodes.ToArray();
        ValidateActivationReferences(owners, nodes);
        ValidatePresentation(owners, nodes);
    }
    private void ValidateRunControls(HashSet<GpuBodyId> owners)
    {
        // Each row resolves: a unique nonzero id, an installed owner, a defined lane, at most one row per lane slot, a slot that exists
        // and belongs to the row's owner, and bounds its lane admits (zero on an enable lane).
        var ids = new HashSet<RunControlId>(RunControls.Length);
        var slots = new HashSet<(RunControlLane, int)>(RunControls.Length);
        foreach (var row in RunControls)
            if (row.Id.Value == 0 || !ids.Add(row.Id) || !owners.Contains(row.Owner) || !Enum.IsDefined(row.Lane) ||
                !slots.Add((row.Lane, row.LaneIndex)) || !RunControlLanes.Resolves(row, Physics) || !RunControlLanes.BoundsAdmitted(row))
                throw new ArgumentException("Compiled run-control table is invalid.");
    }

    private void ValidateActivationReferences(HashSet<GpuBodyId> owners, ActivationNodeDeclaration[] nodes)
    {
        var triggers = Physics.Triggers.ToArray();
        var orientations = Physics.OrientationSensors.ToArray();
        var loads = Physics.ContactLoadSensors.ToArray();
        var passages = Activation.PassageSensors.ToArray();
        var providers = Physics.SupplyGraph.Nodes.ToArray();
        if (passages.Any(sensor => !owners.Contains(sensor.Frame)) ||
            nodes.Any(node => InvalidPhysicalReference(node, triggers, orientations, loads, passages) ||
                node.PowerProvider.Value != 0 && !providers.Any(row => row.Owner == node.PowerProvider &&
                    row.Kind is SupplyNodeKind.Source or SupplyNodeKind.Contact or SupplyNodeKind.LogicGate)))
            throw new ArgumentException("Compiled activation references a foreign capability.");
        if (nodes.Any(node => !owners.Contains(node.Owner)))
            throw new ArgumentException("Activation owner is outside the installed scene.");
    }

    private bool OwnsBody(GpuBodyId owner, GpuBodyId body) => owner == body ||
        Physics.Prismatics.ToArray().Any(joint => joint.BodyA == owner && joint.BodyB == body) ||
        Physics.Revolutes.ToArray().Any(joint => joint.BodyA == owner && joint.BodyB == body);

    private bool InvalidPhysicalReference(ActivationNodeDeclaration node, ContactTriggerDeclaration[] triggers,
        OrientationSensorDeclaration[] orientations, ContactLoadSensorDeclaration[] loads, PassageSensorDeclaration[] passages) =>
        node.Trigger.Value != 0 && !triggers.Any(row => row.Id == node.Trigger && OwnsBody(node.Owner, row.Owner)) ||
        node.Sensor.Value != 0 && !orientations.Any(row => row.Id == node.Sensor && OwnsBody(node.Owner, row.Body)) ||
        node.ContactLoad.Value != 0 && !loads.Any(row => row.Id == node.ContactLoad && OwnsBody(node.Owner, row.Frame)) ||
        node.Passage.Value != 0 && !passages.Any(row => row.Id == node.Passage && OwnsBody(node.Owner, row.Frame));

    private void ValidatePresentation(HashSet<GpuBodyId> owners, ActivationNodeDeclaration[] nodes)
    {
        var targets = new HashSet<AnimationTargetId>(16);
        var bindings = new HashSet<(GpuBodyId, int)>(16);
        foreach (var track in Presentation)
        {
            var d = track.Declaration;
            if (!owners.Contains(track.Owner) || track.Target.Value == 0 || !targets.Add(track.Target) ||
                track.Binding < 0 || !bindings.Add((track.Owner, track.Binding)) ||
                InvalidSignalBinding(track, nodes) || InvalidPhysicalBinding(track) || InvalidTrackFields(track) ||
                InvalidBindingEvaluation(d) || InvalidBindingValues(d))
                throw new ArgumentException("Compiled presentation track is invalid.");
        }
    }

    private bool InvalidPhysicalBinding(CompiledPresentationTrack track)
    {
        var actuatorDriver = track.Declaration.Driver is PresentationDriver.CommittedActuatorDrive or PresentationDriver.CommittedActuatorState;
        if (actuatorDriver != track.Actuator.HasValue) return true;
        if (track.Actuator is { } actuator && !Physics.SupplyGraph.Nodes.ToArray().Any(node =>
            node.Owner == track.Owner && !node.Consumers.IsDefault && node.Consumers.Contains(actuator))) return true;
        var guideDriver = track.Declaration.Driver == PresentationDriver.CommittedWheelAngle;
        return guideDriver != (track.Guide.Value != 0) || guideDriver &&
            !Physics.RopeGuides.ToArray().Any(guide => guide.Id == track.Guide && OwnsBody(track.Owner, guide.Body));
    }

    private static bool InvalidSignalBinding(CompiledPresentationTrack track, ActivationNodeDeclaration[] nodes)
    {
        var d = track.Declaration;
        var signalSource = PresentationDriverRules.SignalSource(d.Driver, d.Event);
        return (signalSource != (track.Activation != default)) ||
                (track.Activation != default && (track.Activation.Owner != track.Owner || !nodes.Contains(track.Activation) ||
                    !SignalObservationRules.Supports(ActivationNetwork.Operation(track.Activation.Kind), track.Activation.ElapsedSignal, d.Driver, d.Event)));
    }

    private static bool InvalidTrackFields(CompiledPresentationTrack track)
    {
        var d = track.Declaration;
        return !float.IsFinite(track.SourceCapacity) || track.SourceCapacity < 0 ||
                !Enum.IsDefined(d.Property) || !Enum.IsDefined(d.Driver) || !Enum.IsDefined(d.Curve) ||
                !Enum.IsDefined(d.Overlap) || !Enum.IsDefined(d.Phase) || !Enum.IsDefined(d.Event) ||
                !Enum.IsDefined(d.EventMagnitude) || (d.Socket is { } socket && !Enum.IsDefined(socket));
    }

    private static bool InvalidBindingEvaluation(CompiledPresentationBinding d)
    {
        return (d.Driver == PresentationDriver.CommittedElectricalSocket) != d.Socket.HasValue ||
                (d.Socket is { } electricalSocket && electricalSocket is not (PortRole.FirstIn or PortRole.SecondIn or PortRole.PowerIn or PortRole.Supply)) ||
                !PresentationDriverRules.Supports(d.Driver, d.Curve, d.Duration, d.Overlap) || !PresentationDriverRules.SupportsMapping(d.Driver, d.Mapping) ||
                (d.Curve == PresentationCurve.ExponentialFollow ? d.Response <= 0 : d.Response != 0) ||
                (d.Driver == PresentationDriver.CommittedTimerPhase && d.Phase == PresentationTimerPhase.None) ||
                (d.Driver == PresentationDriver.Event) != (d.Event != SimulationEventKind.None) ||
                (d.Driver != PresentationDriver.Event && d.EventMagnitude != PresentationEventMagnitude.Any);
    }

    private static bool InvalidBindingValues(CompiledPresentationBinding d)
    {
        return (d.Driver == PresentationDriver.CommittedActuatorState) != d.StateValues.HasValue ||
                (d.StateValues is { } states && (!states.IsFinite() || d.Intermediate.HasValue)) ||
                !float.IsFinite(d.From) || !float.IsFinite(d.To) || !float.IsFinite(d.Duration) || d.Duration < 0 ||
                !float.IsFinite(d.Threshold) || !float.IsFinite(d.Response) || d.Response < 0 ||
                (d.Intermediate is { } intermediate && !float.IsFinite(intermediate));
    }

}
