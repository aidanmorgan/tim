using System;
using System.Linq;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

public static class WorkshopSupplyCompiler
{
    public static SupplyTopology Compile(WorkshopConstruction construction,
        Func<PartId, CuriousContraptions.Bundle.BundleEntry> declaration)
    {
        var nodes=new System.Collections.Generic.List<SupplyNodeDeclaration>(16);
        foreach(var instance in construction.Instances)
        {
            var entry=declaration(instance.PartId);
            var kind=entry.Electrical?.Supply?.Kind;
            if(kind==CuriousContraptions.Abstractions.Electrical.SupplyCapabilityKind.Source)
                nodes.Add(new(instance.Id,SupplyNodeKind.Source,construction.Instances.Identities[instance.Id].Source));
            else if(kind==CuriousContraptions.Abstractions.Electrical.SupplyCapabilityKind.SwitchedContact)
                nodes.Add(new(instance.Id,SupplyNodeKind.Contact));
            else if(kind==SupplyCapabilityKind.LogicGate)
                nodes.Add(new(instance.Id,SupplyNodeKind.LogicGate, Operation: entry.Electrical!.Supply!.Value.Operation));
            else if(entry.Ports.Any(port=>port.Socket==CuriousContraptions.Abstractions.PortSocket.PowerIn))
                nodes.Add(new(instance.Id,SupplyNodeKind.Consumer));
        }
        var byOwner=nodes.ToDictionary(node=>node.Owner);
        var edges=construction.Connections.Where(link=>link.Domain==WorkshopConnectionDomain.Electrical &&
            byOwner.TryGetValue(link.Source,out var from) && from.Kind!=SupplyNodeKind.Consumer &&
            byOwner.TryGetValue(link.Target,out var to) && to.Kind!=SupplyNodeKind.Source)
            .Select(link=>new SupplyEdge(link.Source,link.Target,link.Input)).ToArray();
        return new(nodes.ToArray(),edges);
    }
}
