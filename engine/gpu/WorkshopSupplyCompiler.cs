using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

public static class WorkshopSupplyCompiler
{
    public static ISupplyTopology Compile(ISupplyQueries supply, WorkshopConstruction construction,
        Func<PartId, BundleEntry> declaration) => Compile(supply, construction.Instances, construction.Connections, declaration);

    public static ISupplyTopology Compile(ISupplyQueries supply, WorkshopInstances instances, WorkshopConnections connections,
        Func<PartId, BundleEntry> declaration)
    {
        var nodes = new List<SupplyNodeDeclaration>();
        var edges = new List<SupplyEdge>();
        foreach (var instance in instances)
        {
            var graph = declaration(instance.PartId).Electrical?.SupplyGraph;
            if (graph is null) continue;
            var identities = instances.Identities[instance.Id];
            foreach (var node in graph.Nodes) nodes.Add(CompileNode(instance.Id, identities, node));
            foreach (var edge in graph.Edges)
                edges.Add(new(identities.Supply[edge.Source.Node].Node, identities.Supply[edge.Target.Node].Node, edge.Target.Terminal));
        }
        foreach (var link in connections.Where(link => link.Domain == WorkshopConnectionDomain.Electrical))
        {
            var source = instances.Single(instance => instance.Id == link.Source);
            var target = instances.Single(instance => instance.Id == link.Target);
            if (!TryEndpoint(declaration(source.PartId), link.Output, out var output) ||
                !TryEndpoint(declaration(target.PartId), link.Input, out var input))
                continue;
            edges.Add(new(instances.Identities[source.Id].Supply[output.Node].Node,
                instances.Identities[target.Id].Supply[input.Node].Node, input.Terminal));
        }
        try { return supply.CreateTopology(nodes.OrderBy(node => node.Id.Value).ToArray(), edges.ToArray()); }
        catch (SupplyTopologyException error)
        {
            var reason = error.Reason switch
            {
                SupplyTopologyRefusal.InputSupplied => WorkshopEditRejection.PowerInputSupplied,
                SupplyTopologyRefusal.NonmonotoneFeedback => WorkshopEditRejection.NonmonotoneFeedback,
                SupplyTopologyRefusal.InvalidConnection => WorkshopEditRejection.InvalidConnection,
                _ => throw new InvalidOperationException("Undefined supply refusal.")
            };
            throw new WorkshopEditRejectedException(reason, error.Message);
        }
    }

    public static bool TryEndpoint(BundleEntry entry, PortId port, out SupplyEndpoint endpoint)
    {
        var bindings = (entry.Electrical?.SupplyGraph?.Ports ?? []).Where(binding => binding.Port == port).ToArray();
        if (bindings.Length == 1)
        {
            endpoint = bindings[0].Endpoint;
            return true;
        }
        endpoint = default;
        return false;
    }

    public static SupplyEndpoint Endpoint(BundleEntry entry, PortId port) =>
        TryEndpoint(entry, port, out var endpoint) ? endpoint :
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Electrical socket has no unique declared endpoint.");

    private static SupplyNodeDeclaration CompileNode(GpuBodyId owner, CompiledPartIdentities identities, SupplyNodeDescriptor node)
    {
        var ids = identities.Supply[node.Id];
        return node switch
        {
            SupplySourceDescriptor => new(ids.Node, owner, SupplyNodeKind.Source, ids.Source),
            SupplyContactDescriptor => new(ids.Node, owner, SupplyNodeKind.Contact),
            SupplyLogicDescriptor logic => new(ids.Node, owner, SupplyNodeKind.LogicGate, Operation: logic.Operation),
            SupplyConsumerDescriptor consumer => Consumer(owner, ids.Node, identities, consumer),
            _ => throw new ArgumentException("Unknown supply capability.")
        };
    }

    private static SupplyNodeDeclaration Consumer(GpuBodyId owner, SupplyNodeId id, CompiledPartIdentities identities,
        SupplyConsumerDescriptor consumer)
    {
        var drives = ImmutableArray.CreateBuilder<SupplyConsumer>();
        var stores = ImmutableArray.CreateBuilder<GpuContactWorkId>();
        foreach (var target in consumer.Targets)
            switch (target)
            {
                case SupplyDriveTarget { Coordinate.Kind: CoordinateKind.Prismatic } drive:
                    drives.Add(SupplyConsumer.Prismatic(identities.Sliders[drive.Coordinate.Index].Joint));
                    break;
                case SupplyDriveTarget { Coordinate.Kind: CoordinateKind.Revolute } drive:
                    drives.Add(SupplyConsumer.Revolute(identities.Hinges[drive.Coordinate.Index].Joint));
                    break;
                case SupplyContactWorkTarget:
                    stores.Add(identities.Work);
                    break;
                default: throw new ArgumentException("Unknown supply consumer target.");
            }
        return new(id, owner, SupplyNodeKind.Consumer, Consumers: drives.ToImmutable(), Storages: stores.ToImmutable());
    }

    public static ElectricalStorageBinding[] StorageBindings(ISupplyTopology topology)
    {
        var bindings = new List<ElectricalStorageBinding>();
        foreach (var node in topology.Nodes)
        {
            if (node.Storages.IsDefaultOrEmpty) continue;
            var sources = topology.UpstreamSources(node.Id);
            if (sources.Length > 1)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied,
                    "A finite-work store requires at most one potential power source.");
            if (sources.Length == 0) continue;
            foreach (var store in node.Storages) bindings.Add(new(sources[0], store, node.Id));
        }
        return bindings.OrderBy(binding => binding.Source.Value).ThenBy(binding => binding.Storage.Value).ToArray();
    }
}
