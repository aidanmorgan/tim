using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public readonly record struct CompiledSceneId(ulong Value);
public readonly record struct CompiledPresentationBinding(PresentationProperty Property, PresentationDriver Driver,
    float From, float To, PresentationCurve Curve, float Duration, PresentationOverlap Overlap, float Threshold,
    PresentationTimerPhase Phase, float Response, float? Intermediate, PortSocket? Socket,
    SimulationEventKind Event, PresentationEventMagnitude EventMagnitude);
public readonly record struct CompiledPresentationTrack(AnimationTargetId Target, GpuBodyId Owner, int Binding,
    CompiledPresentationBinding Declaration, ActivationNodeDeclaration Activation, float SourceCapacity);

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
    public ImmutableArray<GpuBodyId> EditableSources { get; }

    public WorkshopCompiledScene(CompiledSceneId identity, ConstructionRevision revision, WorkshopCadenceSettings settings,
        PhysicsSceneDeclaration physics, ActivationNetwork activation, IEnumerable<CompiledPresentationTrack> presentation,
        IEnumerable<GpuBodyId> owners, IEnumerable<GpuBodyId> editableSources)
    {
        Identity = identity; Revision = revision; Settings = settings;
        Physics = physics ?? throw new ArgumentNullException(nameof(physics));
        Activation = activation ?? throw new ArgumentNullException(nameof(activation));
        Presentation = presentation.ToImmutableArray(); Owners = owners.ToImmutableArray();
        EditableSources = editableSources.ToImmutableArray();
        Validate();
    }

    public bool OwnsCapture(CapturePairKey pair) =>
        Physics.Sensors.ToArray().Any(sensor => sensor.Id == pair.Policy) &&
        Physics.Bodies.ToArray().Any(body => body.Id == pair.Target && body.Payload == PayloadPolicy.CaptureTarget);

    public void Validate()
    {
        if (Identity.Value == 0 || Revision.Value == 0) throw new ArgumentException("Compiled scene identity is absent.");
        Settings.Validate();
        var bodies = Physics.Bodies.ToArray().Select(body => body.Id).ToHashSet();
        var owners = Owners.ToHashSet();
        if (owners.Count != Owners.Length || Owners.Any(owner => owner.Value == 0 || !bodies.Contains(owner)) ||
            EditableSources.Distinct().Count() != EditableSources.Length ||
            EditableSources.Any(owner => !owners.Contains(owner) || !Physics.Electrical.Sources.ToArray().Any(source => source.Owner == owner)))
            throw new ArgumentException("Compiled scene ownership is invalid.");
        if (Activation.Substeps != Settings.PhysicalStepsPerCommit)
            throw new ArgumentException("Compiled activation cadence differs from physics.");
        var nodes = Activation.Nodes.ToArray();
        var triggers = Physics.Triggers.ToArray();
        var orientations = Physics.OrientationSensors.ToArray();
        var loads = Physics.ContactLoadSensors.ToArray();
        var passages = Activation.PassageSensors.ToArray();
        var colliders = Physics.Colliders.ToArray();
        var sources = Physics.Electrical.Sources.ToArray();
        if (passages.Any(sensor => !owners.Contains(sensor.Frame)) || nodes.Any(node =>
            node.Trigger.Value != 0 && !triggers.Any(row => row.Id == node.Trigger && row.Owner == node.Owner) ||
            node.Sensor.Value != 0 && !orientations.Any(row => row.Id == node.Sensor && row.Body == node.Owner) ||
            node.ContactLoad.Value != 0 && !loads.Any(row => row.Id == node.ContactLoad && row.Frame == node.Owner) ||
            node.Passage.Value != 0 && !passages.Any(row => row.Id == node.Passage && row.Frame == node.Owner) ||
            node.Collider.Value != 0 && !colliders.Any(row => row.Id == node.Collider && row.Body == node.Owner) ||
            node.PowerSource.Value != 0 && !sources.Any(row => row.Owner == node.PowerSource)))
            throw new ArgumentException("Compiled activation references a foreign capability.");
        if (nodes.Any(node => !owners.Contains(node.Owner)))
            throw new ArgumentException("Activation owner is outside the installed scene.");
        var targets = new HashSet<AnimationTargetId>(16);
        var bindings = new HashSet<(GpuBodyId, int)>(16);
        foreach (var track in Presentation)
        {
            var d = track.Declaration;
            if (!owners.Contains(track.Owner) || track.Target.Value == 0 || !targets.Add(track.Target) ||
                track.Binding < 0 || !bindings.Add((track.Owner, track.Binding)) ||
                (track.Activation != default && (track.Activation.Owner != track.Owner || !nodes.Contains(track.Activation))) ||
                !float.IsFinite(track.SourceCapacity) || track.SourceCapacity < 0 ||
                !Enum.IsDefined(d.Property) || !Enum.IsDefined(d.Driver) || !Enum.IsDefined(d.Curve) ||
                !Enum.IsDefined(d.Overlap) || !Enum.IsDefined(d.Phase) || !Enum.IsDefined(d.Event) ||
                !Enum.IsDefined(d.EventMagnitude) || (d.Socket is { } socket && !Enum.IsDefined(socket)) ||

                (d.Driver == PresentationDriver.CommittedElectricalSocket) != d.Socket.HasValue ||
                (d.Socket is { } electricalSocket && electricalSocket is not (PortSocket.FirstIn or PortSocket.SecondIn or PortSocket.PowerIn or PortSocket.Supply)) ||
                !PresentationDriverRules.Supports(d.Driver, d.Curve, d.Duration, d.Overlap) ||
                (d.Curve == PresentationCurve.ExponentialFollow ? d.Response <= 0 : d.Response != 0) ||
                (d.Driver == PresentationDriver.CommittedTimerPhase && d.Phase == PresentationTimerPhase.None) ||
                (d.Driver == PresentationDriver.Event) != (d.Event != SimulationEventKind.None) ||
                (d.Driver != PresentationDriver.Event && d.EventMagnitude != PresentationEventMagnitude.Any) ||
                !float.IsFinite(d.From) || !float.IsFinite(d.To) || !float.IsFinite(d.Duration) || d.Duration < 0 ||
                !float.IsFinite(d.Threshold) || !float.IsFinite(d.Response) || d.Response < 0 ||
                (d.Intermediate is { } intermediate && !float.IsFinite(intermediate)))
                throw new ArgumentException("Compiled presentation track is invalid.");
        }
    }
}
