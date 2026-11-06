using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Bundle;

/// <summary>Validates complete package-local signal declarations before consumers resolve any handles.</summary>
internal static class SignalBundleValidation
{
    public static IReadOnlyList<BundleError> Validate(BundleEntry entry)
    {
        var errors = new List<BundleError>();
        void Refuse(string field) => errors.Add(new(BundleErrorCode.OutOfRange, $"{entry.Metadata.Id}.Signal.{field}"));
        var signal = entry.Signal;
        if (signal is null)
        {
            if (entry.Ports.Any(port => port.Domain == SimulationDomain.Signal) || entry.Electrical?.Supply?.Control is not null ||
                entry.Electrical?.Supply?.Kind == SupplyCapabilityKind.SwitchedContact) Refuse("Missing");
            if (entry.Events.Any(occurrence => occurrence.SignalNode is not null || SimulationEvents.SignalSource(occurrence.Kind)))
                Refuse("Event.Source");
            return errors;
        }
        if (signal.Nodes is null || signal.Edges is null || signal.Ports is null)
        {
            Refuse("Collections");
            return errors;
        }
        var nodes = new Dictionary<SignalNodeId, SignalNodeDescriptor>();
        foreach (var node in signal.Nodes)
        {
            if (node is null || node.Id == default || !nodes.TryAdd(node.Id, node)) Refuse("Node.Identity");
            else ValidateNode(entry, node, Refuse);
        }
        ValidateEvents(entry, nodes, Refuse);
        ValidatePresentation(entry, nodes, Refuse);
        ValidateContact(entry, nodes, Refuse);
        ValidatePorts(entry, signal, nodes, Refuse);
        ValidateTopology(signal, nodes, Refuse);
        return errors;
    }

    private static void ValidatePorts(BundleEntry entry, SignalPayload signal, Dictionary<SignalNodeId, SignalNodeDescriptor> nodes, Action<string> Refuse)
    {
        var ports = new HashSet<PortId>();
        foreach (var binding in signal.Ports)
        {
            var declared = entry.Ports.Where(port => port.Id == binding.Port).ToArray();
            if (!ports.Add(binding.Port) || declared.Length != 1 || !nodes.TryGetValue(binding.Endpoint.Node, out var node))
            { Refuse("Port.Reference"); continue; }
            var port = declared[0];
            if (port.Domain != SimulationDomain.Signal || port.Signal != SignalTerminals.Kind(node, binding.Endpoint.Terminal) ||
                port.Direction != (binding.Endpoint.Terminal == SignalTerminal.Output ? PortDirection.Output : PortDirection.Input))
                Refuse("Port.Terminal");
        }
        if (entry.Ports.Any(port => port.Domain == SimulationDomain.Signal && !ports.Contains(port.Id))) Refuse("Port.Missing");
    }

    private static void ValidateTopology(SignalPayload signal, Dictionary<SignalNodeId, SignalNodeDescriptor> nodes, Action<string> Refuse)
    {
        var graph = new List<SignalTopologyEdge>();
        var identities = nodes.Keys.Select((id, index) => (id, value: index)).ToDictionary(pair => pair.id, pair => pair.value);
        foreach (var edge in signal.Edges)
        {
            if (!nodes.TryGetValue(edge.Source.Node, out var source) || !nodes.TryGetValue(edge.Target.Node, out var target) ||
                !SignalTerminals.CanConnect(source, edge.Source, target, edge.Target))
            { Refuse("Edge.Terminal"); continue; }
            graph.Add(new(identities[source.Id], identities[target.Id], edge.Target.Terminal,
                SignalTerminals.Kind(source, edge.Source.Terminal) == PortSignalKind.Level));
        }
        try
        {
            SignalTopology.Validate(nodes.Values.Select(node => new SignalTopologyNode(identities[node.Id],
                node is SignalTimerDescriptor)).ToArray(), graph);
        }
        catch (ArgumentException) { Refuse("Topology"); }
    }

    private static void ValidateEvents(BundleEntry entry, Dictionary<SignalNodeId, SignalNodeDescriptor> nodes, Action<string> Refuse)
    {
        foreach (var occurrence in entry.Events)
            if (SimulationEvents.SignalSource(occurrence.Kind))
            {
                if (occurrence.SignalNode is not { } selected || !nodes.TryGetValue(selected, out var source) ||
                    !SignalObservationRules.Emits(source.Kind, source is SignalTimerDescriptor timer ? timer.ElapsedSignal : TimerElapsedSignal.None, occurrence.Kind))
                    Refuse("Event.Source");
            }
            else if (occurrence.SignalNode is not null) Refuse("Event.UnexpectedSource");
    }

    private static void ValidatePresentation(BundleEntry entry, Dictionary<SignalNodeId, SignalNodeDescriptor> nodes, Action<string> Refuse)
    {
        foreach (var binding in entry.Presentation.Bindings)
            if (PresentationDriverRules.SignalSource(binding.Driver, binding.Event) &&
                (binding.SignalNode is not { } selected || !nodes.TryGetValue(selected, out var source) ||
                 !SignalObservationRules.Supports(source.Kind, source is SignalTimerDescriptor timer ? timer.ElapsedSignal : TimerElapsedSignal.None, binding.Driver, binding.Event)))
                Refuse("Presentation.Source");
    }

    private static void ValidateContact(BundleEntry entry, Dictionary<SignalNodeId, SignalNodeDescriptor> nodes, Action<string> Refuse)
    {
        var supply = entry.Electrical?.Supply;
        if (supply is { Kind: SupplyCapabilityKind.SwitchedContact })
        {
            if (supply.Value.Control is not { } control || !nodes.TryGetValue(control.Node, out var controlled) ||
                !Enum.IsDefined(control.State) || (control.State == SignalContactState.TimerCounting
                    ? controlled is not SignalTimerDescriptor
                    : controlled is SignalTimerDescriptor or SignalIndicatorDescriptor or SignalOscillatorDescriptor))
                Refuse("Contact.Control");
        }
        else if (supply?.Control is not null) Refuse("Contact.UnexpectedControl");
    }

    private static void ValidateNode(BundleEntry entry, SignalNodeDescriptor node, Action<string> refuse)
    {
        switch (node)
        {
            case SignalTimerDescriptor timer:
                ValidateTimer(entry, timer, refuse);
                break;
            case SignalPulseCountDescriptor count:
                if (!Parameter(entry, count.TargetParameter, ParameterKind.WholeNumber, ParameterUnit.None, 1, 4294967296f)) refuse("Count.Target");
                break;
            case SignalOscillatorDescriptor oscillator:
                ValidateOscillator(entry, oscillator, refuse);
                break;
            case SignalContactDescriptor contact:
                ValidateContactNode(entry, contact, refuse);
                break;
            case SignalOrientationDescriptor orientation:
                if (!Body(entry, orientation.Body, KinematicMode.Dynamic) || !float.IsFinite(orientation.ThresholdDegrees) ||
                    orientation.ThresholdDegrees is < 4 or > 179) refuse("Orientation");
                break;
            case SignalPassageDescriptor passage:
                ValidatePassage(entry, passage, refuse);
                break;
            case SignalContactLoadDescriptor load:
                ValidateLoad(entry, load, refuse);
                if (!Parameter(entry, load.MinimumMassParameter, ParameterKind.Real, ParameterUnit.Kilogram, float.Epsilon)) refuse("Load.Parameter");
                break;
            case SignalMemoryDescriptor or SignalIndicatorDescriptor:
                break;
            default: refuse("Operation"); break;
        }
    }

    private static void ValidateTimer(BundleEntry entry, SignalTimerDescriptor timer, Action<string> refuse)
    {
        if (!PositiveSeconds(entry, timer.DurationParameter) || !Enum.IsDefined(timer.Completion) ||
            !Enum.IsDefined(timer.Boundary) || !Enum.IsDefined(timer.ElapsedSignal) ||
            timer.Completion == TimerCompletionPolicy.Rearm && (timer.ElapsedSignal != TimerElapsedSignal.None || timer.Boundary != TimerBoundary.BeforeNetworks) ||
            timer.Completion == TimerCompletionPolicy.Latch && (timer.ElapsedSignal != TimerElapsedSignal.Activation || timer.Boundary != TimerBoundary.BeforePhysics))
            refuse("Timer");
    }

    private static void ValidateOscillator(BundleEntry entry, SignalOscillatorDescriptor oscillator, Action<string> refuse)
    {
        if (!PositiveSeconds(entry, oscillator.IntervalParameter) ||
            !entry.Ports.Any(port => port.Id == oscillator.SupplyInput && port.Domain == SimulationDomain.Electrical &&
                port.Direction == PortDirection.Input && port.Role == PortRole.PowerIn) ||
            entry.Electrical?.Nodes.Any(row => row.Role == ElectricalRole.Consumer) != true) refuse("Oscillator");
    }

    private static void ValidateContactNode(BundleEntry entry, SignalContactDescriptor contact, Action<string> refuse)
    {
        if (!Body(entry, contact.Body, KinematicMode.Static) || !Parameter(entry, contact.ThresholdParameter, ParameterKind.Real, ParameterUnit.MetrePerSecond, 0, MathF.BitIncrement(64)) ||
            !Enum.IsDefined(contact.Target.Kind) ||
            (contact.Target.Kind == TargetSelectorKind.Named ? string.IsNullOrWhiteSpace(contact.Target.NamedTarget) : contact.Target.NamedTarget is not null))
            refuse("Contact");
    }

    private static void ValidatePassage(BundleEntry entry, SignalPassageDescriptor passage, Action<string> refuse)
    {
        if (!Body(entry, passage.Frame, KinematicMode.Static) ||
            !Finite(passage.LocalPosition) || !float.IsFinite(passage.LocalRotation.LengthSquared()) ||
            MathF.Abs(passage.LocalRotation.LengthSquared() - 1) > 1e-6f ||
            !float.IsFinite(passage.ApertureRadius) || passage.ApertureRadius is < .01f or > 10 ||
            !float.IsFinite(passage.RearmClearance) || passage.RearmClearance is < .001f or > 10) refuse("Passage");
    }

    private static void ValidateLoad(BundleEntry entry, SignalContactLoadDescriptor load, Action<string> refuse)
    {
        var colliders = Colliders(entry, load.Frame);
        if (!Body(entry, load.Frame, KinematicMode.Static) || load.RegionCollider < 0 || load.RegionCollider >= colliders.Count ||
            colliders[load.RegionCollider].ShapeKind != ColliderShapeKind.Box || colliders[load.RegionCollider].Rotation != Quaternion.Identity ||
            load.SupportingColliders is null || load.SupportingColliders.Count == 0 ||
            load.SupportingColliders.Distinct().Count() != load.SupportingColliders.Count ||
            load.SupportingColliders.Any(index => index < 0 || index >= colliders.Count) ||
            !Finite(load.LocalNormal) || MathF.Abs(load.LocalNormal.LengthSquared() - 1) > 1e-6f ||
            !float.IsFinite(load.MinimumAlignment) || load.MinimumAlignment is <= 0 or > 1 ||
            !float.IsFinite(load.ContactDistance) || load.ContactDistance is < 0 or > .01f ||
            !float.IsFinite(load.DepthMargin) || load.DepthMargin is < 0 or > .01f ||
            !float.IsFinite(load.ReleaseHoldSeconds) || load.ReleaseHoldSeconds is <= 0 or > 1)
            refuse("Load");
    }

    private static bool Parameter(BundleEntry entry, ParameterId id, ParameterKind kind, ParameterUnit unit, float minimum, float exclusiveMaximum = float.PositiveInfinity) =>
        entry.Metadata.Parameters.Any(parameter => parameter.Key == id.Value && parameter.Kind == kind &&
            parameter.Unit == unit && float.IsFinite(parameter.Minimum) && float.IsFinite(parameter.Maximum) &&
            parameter.Minimum >= minimum && parameter.Maximum < exclusiveMaximum);
    private static bool PositiveSeconds(BundleEntry entry, ParameterId id) => entry.Metadata.Parameters.Any(parameter => parameter.Key == id.Value &&
        parameter.Kind == ParameterKind.Real && parameter.Unit == ParameterUnit.Second &&
        float.IsFinite(parameter.Minimum) && parameter.Minimum > 0);

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static bool Body(BundleEntry entry, PackageBodyIndex body, KinematicMode? mode)
    {
        var children = (entry.Mechanical?.Sliders.Count ?? 0) + (entry.Mechanical?.Hinges.Count ?? 0);
        if (body.Value < 0 || body.Value > children || entry.Footprint.Bodies == 0) return false;
        var motion = body.Value > 0 ? KinematicMode.Dynamic : entry.Mechanical?.KinematicMode ?? KinematicMode.Static;
        return mode is null || mode == motion;
    }
    private static IReadOnlyList<ColliderDescriptor> Colliders(BundleEntry entry, PackageBodyIndex body)
    {
        if (body.Value == 0) return entry.Colliders;
        var index = body.Value - 1;
        if (entry.Mechanical is not { } mechanical || index < 0) return [];
        if (index < mechanical.Sliders.Count) return mechanical.Sliders[index].MovingBody.Colliders;
        index -= mechanical.Sliders.Count;
        return index < mechanical.Hinges.Count ? mechanical.Hinges[index].MovingBody.Colliders : [];
    }
}
