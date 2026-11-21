using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

/// <summary>Admits explicit supply capabilities and references before compilation can resolve handles.</summary>
internal static class SupplyBundleValidation
{
    public static IReadOnlyList<BundleError> Validate(BundleEntry entry)
    {
        var errors = new List<BundleError>();
        void Refuse(string field) => errors.Add(new(BundleErrorCode.OutOfRange, $"{entry.Metadata.Id}.SupplyGraph.{field}"));
        var graph = entry.Electrical?.SupplyGraph;
        if (graph is null)
        {
            if (entry.Ports.Any(port => port.Domain == SimulationDomain.Electrical) || Driven(entry).Any() || TimedTransmissions(entry).Any()) Refuse("Missing");
            return errors;
        }
        if (graph.Nodes is null || graph.Edges is null || graph.Ports is null) { Refuse("Collections"); return errors; }
        var nodes = new Dictionary<LocalSupplyNodeId, SupplyNodeDescriptor>();
        var targets = new HashSet<SupplyTarget>();
        foreach (var node in graph.Nodes)
        {
            if (node is null || node.Id == default || !nodes.TryAdd(node.Id, node)) { Refuse("Node.Identity"); continue; }
            ValidateNode(entry, node, targets, Refuse);
        }
        foreach (var drive in Driven(entry))
            if (!targets.Contains(new SupplyDriveTarget(drive))) Refuse("Drive.MissingConsumer");
        ValidateDriveCommands(entry, nodes, Refuse);
        foreach (var transmission in TimedTransmissions(entry))
            if (!targets.Contains(new SupplyTransmissionTarget(transmission))) Refuse("Transmission.MissingConsumer");
        ValidatePorts(entry, graph, nodes, Refuse);
        ValidateEdges(graph, nodes, Refuse);
        ValidatePresentation(entry, graph, nodes, Refuse);
        return errors;
    }

    private static void ValidateDriveCommands(BundleEntry entry, Dictionary<LocalSupplyNodeId, SupplyNodeDescriptor> nodes, Action<string> refuse)
    {
        foreach (var slider in entry.Mechanical?.Sliders ?? [])
        {
            if (slider.Drive is not { } drive) continue;
            if (!Enum.IsDefined(drive.Selection) || !Enum.IsDefined(drive.Inactive)) { refuse("Drive.Control"); continue; }
            if (drive.Selection == PrismaticTargetSelection.Fixed)
            {
                if (drive.Positive.HasValue || drive.Negative.HasValue) refuse("Drive.UnexpectedCommands");
            }
            else if (drive.Positive is not { } positive || drive.Negative is not { } negative ||
                positive == negative || !nodes.ContainsKey(positive) || !nodes.ContainsKey(negative))
                refuse("Drive.CommandReference");
        }
    }

    private static void ValidateNode(BundleEntry entry, SupplyNodeDescriptor node, HashSet<SupplyTarget> targets, Action<string> refuse)
    {
        switch (node)
        {
            case SupplySourceDescriptor source:
                ValidateStore(entry, source.Store, refuse);
                break;
            case SupplyLogicDescriptor logic:
                if (!Enum.IsDefined(logic.Operation)) refuse("Logic.Operation");
                break;
            case SupplyContactDescriptor contact:
                ValidateContact(entry, contact.Control, refuse);
                break;
            case SupplyConsumerDescriptor consumer:
                if (consumer.Targets is null) { refuse("Consumer.Targets"); break; }
                foreach (var target in consumer.Targets)
                    if (target is null || !targets.Add(target) || !ValidTarget(entry, target)) refuse("Consumer.Target");
                break;
            default: refuse("Node.Kind"); break;
        }
    }

    private static void ValidateContact(BundleEntry entry, SupplyContactControl control, Action<string> refuse)
    {
        if (control is SignalStateContactControl) return;
        if (control is not PrismaticContactControl endpoint || !Enum.IsDefined(endpoint.Endpoint) ||
            endpoint.Coordinate.Kind != CoordinateKind.Prismatic || endpoint.Coordinate.Index < 0 ||
            entry.Mechanical is not { } mechanical || endpoint.Coordinate.Index >= mechanical.Sliders.Count)
            refuse("Contact.Control");
    }

    private static bool ValidTarget(BundleEntry entry, SupplyTarget target) => target switch
    {
        SupplyEnergyStoreTarget store => store.Store != default && (entry.Mechanical?.EnergyStores ?? []).Any(value => value.Id == store.Store),
        SupplyDriveTarget drive => Driven(entry).Contains(drive.Coordinate),
        SupplyContinuousWorkTarget work => work.Source.Value != 0 &&
            (entry.Mechanical?.AirflowSources ?? []).Any(source => source.Id == work.Source),
        SupplyTransmissionTarget transmission => transmission.Transmission.Value != 0 &&
            TimedTransmissions(entry).Contains(transmission.Transmission),
        _ => false
    };

    private static IEnumerable<LocalMechanicalTransmissionId> TimedTransmissions(BundleEntry entry)
    {
        if (entry.Mechanical?.Transmissions is not { } transmissions) yield break;
        foreach (var transmission in transmissions)
            if (transmission.CloseSeconds is not null) yield return transmission.Id;
    }

    private static IEnumerable<CoordinateReference> Driven(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        for (var i = 0; i < mechanical.Sliders.Count; i++)
            if (mechanical.Sliders[i].Drive is not null) yield return new(CoordinateKind.Prismatic, i);
        for (var i = 0; i < mechanical.Hinges.Count; i++)
            if (mechanical.Hinges[i].Drive is not null) yield return new(CoordinateKind.Revolute, i);
    }

    private static void ValidateStore(BundleEntry entry, SupplySourceStoreDescriptor store, Action<string> refuse)
    {
        ParameterId[] keys = [store.Capacity, store.PowerLimit, store.InitialCharge, store.Enable];
        if (keys.Any(key => key == default) || keys.Distinct().Count() != keys.Length) refuse("Store.Parameters");
        ValidateReal(entry, store.Capacity, ParameterUnit.Joule, true, refuse);
        ValidateReal(entry, store.PowerLimit, ParameterUnit.Watt, true, refuse);
        ValidateReal(entry, store.InitialCharge, ParameterUnit.None, false, refuse);
        var enable = entry.Metadata.Parameters.Where(parameter => parameter.Key == store.Enable.Value).ToArray();
        if (enable.Length != 1 || enable[0].Kind != ParameterKind.Choice || enable[0].Unit != ParameterUnit.None ||
            !enable[0].Options.SequenceEqual(["Disabled", "Enabled"], StringComparer.Ordinal)) refuse("Store.Enable");
    }

    private static void ValidateReal(BundleEntry entry, ParameterId id, ParameterUnit unit, bool positive, Action<string> refuse)
    {
        var parameters = entry.Metadata.Parameters.Where(parameter => parameter.Key == id.Value).ToArray();
        if (parameters.Length != 1) { refuse("Store.Reference"); return; }
        var parameter = parameters[0];
        if (parameter.Kind != ParameterKind.Real || parameter.Unit != unit ||
            !float.IsFinite(parameter.Minimum) || !float.IsFinite(parameter.Maximum) ||
            (positive ? parameter.Minimum <= 0f : parameter.Minimum < 0f || parameter.Maximum > 1f))
            refuse("Store.Domain");
    }

    private static void ValidatePorts(BundleEntry entry, SupplyGraphDescriptor graph,
        Dictionary<LocalSupplyNodeId, SupplyNodeDescriptor> nodes, Action<string> refuse)
    {
        var bound = new HashSet<PortId>();
        foreach (var binding in graph.Ports)
        {
            var ports = entry.Ports.Where(port => port.Id == binding.Port).ToArray();
            if (!bound.Add(binding.Port) || ports.Length != 1 || !nodes.TryGetValue(binding.Endpoint.Node, out var node))
            { refuse("Port.Reference"); continue; }
            var port = ports[0];
            if (port.Domain != SimulationDomain.Electrical || !SupplyTerminals.Admits(node, binding.Endpoint.Terminal) ||
                port.Direction != (binding.Endpoint.Terminal == SupplyTerminal.Output ? PortDirection.Output : PortDirection.Input))
                refuse("Port.Terminal");
        }
        if (entry.Ports.Any(port => port.Domain == SimulationDomain.Electrical && !bound.Contains(port.Id))) refuse("Port.Missing");
    }

    private static void ValidateEdges(SupplyGraphDescriptor graph,
        Dictionary<LocalSupplyNodeId, SupplyNodeDescriptor> nodes, Action<string> refuse)
    {
        var seen = new HashSet<SupplyConnection>();
        var consumed = new HashSet<LocalSupplyNodeId>();
        var edges = new List<SupplyConnection>();
        foreach (var edge in graph.Edges)
        {
            if (!nodes.TryGetValue(edge.Source.Node, out var source) || !nodes.TryGetValue(edge.Target.Node, out var target) ||
                source.Id == target.Id || !SupplyTerminals.CanConnect(source, edge.Source, target, edge.Target) || !seen.Add(edge))
            { refuse("Edge.Terminal"); continue; }
            if (target is SupplyConsumerDescriptor && !consumed.Add(target.Id)) refuse("Consumer.FanIn");
            edges.Add(edge);
        }
        var slots = nodes.Keys.Select((id, slot) => (id, slot)).ToDictionary(pair => pair.id, pair => pair.slot);
        var predecessors = nodes.Keys.Select(id => edges.Where(edge => edge.Target.Node == id)
            .Select(edge => slots[edge.Source.Node]).ToArray()).ToArray();
        var ordered = nodes.Values.ToArray();
        foreach (var component in SupplyCycles.Components(predecessors))
            if (SupplyCycles.Cyclic(component, predecessors) && component.Any(slot =>
                ordered[slot] is SupplyLogicDescriptor logic && (!Enum.IsDefined(logic.Operation) || !SupplyCycles.IsMonotone(logic.Operation))))
                refuse("NonmonotoneCycle");
    }

    private static void ValidatePresentation(BundleEntry entry, SupplyGraphDescriptor? graph,
        Dictionary<LocalSupplyNodeId, SupplyNodeDescriptor> nodes, Action<string> refuse)
    {
        foreach (var binding in entry.Presentation.Bindings)
        {
            var sourceRead = binding.Driver is PresentationDriver.CommittedSupply or PresentationDriver.CommittedCharge;
            if (sourceRead != binding.SupplyNode.HasValue || (binding.SupplyNode is { } id &&
                (!nodes.TryGetValue(id, out var node) || node is not SupplySourceDescriptor))) refuse("Presentation.Source");
            if (binding.Driver == PresentationDriver.CommittedElectricalSocket)
            {
                if (!binding.Socket.HasValue || graph is null)
                {
                    refuse("Presentation.Socket");
                }
                else
                {
                    var bound = graph.Ports.Where(p => p.Port == binding.Socket.Value).ToArray();
                    if (bound.Length != 1 || !nodes.TryGetValue(bound[0].Endpoint.Node, out var socketNode) || socketNode is not SupplyLogicDescriptor)
                        refuse("Presentation.Socket");
                }
            }
        }
    }
}

