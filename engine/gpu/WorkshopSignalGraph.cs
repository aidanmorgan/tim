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
    private readonly record struct LocalEdge((GpuBodyId Owner, SignalNodeId Local) Source,
        (GpuBodyId Owner, SignalNodeId Local) Target, SignalTerminal Input);
    private sealed record LocalGraph(Dictionary<(GpuBodyId Owner, SignalNodeId Local), SignalNodeDescriptor> Nodes,
        HashSet<LocalEdge> Edges);

    public IReadOnlyList<ResolvedSignalNode> Nodes { get; }
    public IReadOnlyList<ResolvedSignalEdge> Edges { get; }

    public WorkshopSignalGraph(WorkshopInstances instances, WorkshopConnections connections)
    {
        var graph = Resolve(instances.ToDictionary(owner => owner.Id, owner => owner.PartId), connections);
        ActivationNodeId Id((GpuBodyId Owner, SignalNodeId Local) key) => instances.Identities[key.Owner].Signals[key.Local].Node;
        Nodes = graph.Nodes.Select(node => new ResolvedSignalNode(node.Key.Owner, node.Value, Id(node.Key))).ToArray();
        Edges = graph.Edges.Select(edge => new ResolvedSignalEdge(Id(edge.Source), Id(edge.Target), edge.Input))
            .OrderBy(edge => edge.Source.Value).ThenBy(edge => edge.Target.Value).ThenBy(edge => edge.Input).ToArray();
    }

    /// <summary>Applies the same graph admission to saved owner/part identities before domain instances are constructed.</summary>
    internal static void Validate(IReadOnlyDictionary<GpuBodyId, PartId> parts, WorkshopConnections connections) =>
        _ = Resolve(parts, connections);

    private static LocalGraph Resolve(IReadOnlyDictionary<GpuBodyId, PartId> parts, WorkshopConnections connections)
    {
        var nodes = new Dictionary<(GpuBodyId Owner, SignalNodeId Local), SignalNodeDescriptor>();
        foreach (var owner in parts)
            foreach (var node in DeclaredPhysics.Of(owner.Value).Signal?.Nodes ?? [])
                nodes.Add((owner.Key, node.Id), node);
        var edges = new HashSet<LocalEdge>();
        void Add(GpuBodyId from, SignalEndpoint output, GpuBodyId to, SignalEndpoint input)
        {
            if (!nodes.TryGetValue((from, output.Node), out var source) ||
                !nodes.TryGetValue((to, input.Node), out var target) ||
                !SignalTerminals.CanConnect(source, output, target, input))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported signal terminal.");
            edges.Add(new((from, output.Node), (to, input.Node), input.Terminal));
        }
        foreach (var owner in parts)
            foreach (var edge in DeclaredPhysics.Of(owner.Value).Signal?.Edges ?? [])
                Add(owner.Key, edge.Source, owner.Key, edge.Target);
        foreach (var link in connections.Where(link => link.Domain == WorkshopConnectionDomain.Activation))
            Add(link.Source, Endpoint(parts[link.Source], link.Output), link.Target, Endpoint(parts[link.Target], link.Input));
        var graph = new LocalGraph(nodes, edges);
        ValidateTopology(graph);
        return graph;
    }

    private static void ValidateTopology(LocalGraph graph)
    {
        var slots = graph.Nodes.Keys.Select((key, slot) => (key, slot)).ToDictionary(pair => pair.key, pair => pair.slot);
        try
        {
            SignalTopology.Validate(graph.Nodes.Select(node => new SignalTopologyNode(slots[node.Key],
                node.Value is SignalTimerDescriptor)).ToArray(),
                graph.Edges.Select(edge => new SignalTopologyEdge(slots[edge.Source], slots[edge.Target], edge.Input,
                    SignalTerminals.Kind(graph.Nodes[edge.Source], SignalTerminal.Output) == PortSignalKind.Level)).ToArray());
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
