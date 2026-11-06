using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

public readonly record struct ResolvedSignalNode(GpuBodyId Owner, SignalNodeDescriptor Declaration, ActivationNodeId Id);
public readonly record struct ResolvedSignalEdge(ActivationNodeId Source, ActivationNodeId Target, SignalTerminal Input);

/// <summary>Resolves package-local and public routes into one canonical delivery graph.</summary>
public sealed class WorkshopSignalGraph
{
    public IReadOnlyList<ResolvedSignalNode> Nodes { get; }
    public IReadOnlyList<ResolvedSignalEdge> Edges { get; }

    public WorkshopSignalGraph(WorkshopInstances instances, WorkshopConnections connections)
    {
        var nodes = new List<ResolvedSignalNode>();
        foreach (var owner in instances)
            foreach (var node in DeclaredPhysics.Of(owner.PartId).Signal?.Nodes ?? [])
                nodes.Add(new(owner.Id, node, instances.Identities[owner.Id].Signals[node.Id].Node));
        Nodes = nodes;
        var lookup = nodes.ToDictionary(node => (node.Owner, node.Declaration.Id));
        var edges = new HashSet<ResolvedSignalEdge>();
        void Add(GpuBodyId from, SignalEndpoint output, GpuBodyId to, SignalEndpoint input)
        {
            if (!lookup.TryGetValue((from, output.Node), out var source) ||
                !lookup.TryGetValue((to, input.Node), out var target) ||
                !SignalTerminals.CanConnect(source.Declaration, output, target.Declaration, input))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported signal terminal.");
            edges.Add(new(source.Id, target.Id, input.Terminal));
        }
        foreach (var owner in instances)
            foreach (var edge in DeclaredPhysics.Of(owner.PartId).Signal?.Edges ?? [])
                Add(owner.Id, edge.Source, owner.Id, edge.Target);
        foreach (var link in connections.Where(link => link.Domain == WorkshopConnectionDomain.Activation))
        {
            var source = instances.Single(instance => instance.Id == link.Source);
            var target = instances.Single(instance => instance.Id == link.Target);
            Add(source.Id, Endpoint(source.PartId, link.Output), target.Id, Endpoint(target.PartId, link.Input));
        }
        Edges = edges.OrderBy(edge => edge.Source.Value).ThenBy(edge => edge.Target.Value).ThenBy(edge => edge.Input).ToArray();
        var indexed = nodes.ToDictionary(node => node.Id);
        var slots = nodes.Select((node, slot) => (node.Id, slot)).ToDictionary(pair => pair.Id, pair => pair.slot);
        try
        {
            SignalTopology.Validate(nodes.Select(node => new SignalTopologyNode(slots[node.Id],
                node.Declaration is SignalTimerDescriptor)).ToArray(),
                Edges.Select(edge => new SignalTopologyEdge(slots[edge.Source], slots[edge.Target], edge.Input,
                    SignalTerminals.Kind(indexed[edge.Source].Declaration, SignalTerminal.Output) == PortSignalKind.Level)).ToArray());
        }
        catch (SignalTopologyException error)
        {
            throw new WorkshopEditRejectedException(error.Reason == SignalTopologyRejection.LevelFanIn
                ? WorkshopEditRejection.LevelInputFollowsOneLevel : WorkshopEditRejection.InvalidConnection, error.Message);
        }
    }
    private static SignalEndpoint Endpoint(PartId part, PortId port)
    {
        var bindings = DeclaredPhysics.Of(part).Signal?.Ports ?? [];
        foreach (var binding in bindings) if (binding.Port == port) return binding.Endpoint;
        throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unbound signal port.");
    }
}
