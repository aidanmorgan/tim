using System;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

/// <summary>Declared topology shared by physical supply projection and source provenance queries.</summary>
public sealed class SupplyTopology : ISupplyTopology
{
    public System.Collections.Generic.IReadOnlyList<SupplyNodeDeclaration> Nodes { get; }
    public System.Collections.Generic.IReadOnlyList<SupplyEdge> Edges { get; }
    public SupplyTopology(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges)
    {
        Validate(nodes, edges);
        Nodes = Array.AsReadOnly((SupplyNodeDeclaration[])nodes.Clone());
        Edges = Array.AsReadOnly((SupplyEdge[])edges.Clone());
    }
    private static void Validate(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges)
    {
        var slots = new Dictionary<SupplyNodeId, int>();
        var sources = new HashSet<ElectricalSourceId>();
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node.Id.Value == 0 || node.Owner.Value == 0 || !Enum.IsDefined(node.Kind) || !slots.TryAdd(node.Id, i))
                throw new ArgumentException("A supply node requires a unique identity and defined kind.");
            if ((node.Kind == SupplyNodeKind.Source) != (node.Source.Value != 0) ||
                node.Kind == SupplyNodeKind.Source && !sources.Add(node.Source))
                throw new ArgumentException("A source capability requires a unique source.");
            if ((node.Kind == SupplyNodeKind.LogicGate) != node.Operation.HasValue ||
                node.Operation is { } operation && !Enum.IsDefined(operation))
                throw new ArgumentException("A logic capability requires a defined operation.");
        }
        ValidateEdges(nodes, edges, slots);
    }

    private static void ValidateEdges(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges, Dictionary<SupplyNodeId, int> slots)
    {
        var seen = new HashSet<SupplyEdge>();
        var consumers = new HashSet<SupplyNodeId>();
        foreach (var edge in edges)
        {
            if (!slots.TryGetValue(edge.Source, out var from) || !slots.TryGetValue(edge.Target, out var to) || from == to ||
                !seen.Add(edge) || nodes[from].Kind == SupplyNodeKind.Consumer || nodes[to].Kind == SupplyNodeKind.Source ||
                (edge.Input != SupplyTerminal.PowerIn && (nodes[to].Kind != SupplyNodeKind.LogicGate ||
                    edge.Input is not (SupplyTerminal.FirstIn or SupplyTerminal.SecondIn))))
                throw new SupplyTopologyException(SupplyTopologyRefusal.InvalidConnection, "Unsupported or duplicate resolved supply edge.");
            if (nodes[to].Kind == SupplyNodeKind.Consumer && !consumers.Add(edge.Target))
                throw new SupplyTopologyException(SupplyTopologyRefusal.InputSupplied, "Consumer already has a supplier.");
        }
        var predecessors = nodes.Select(node => edges.Where(edge => edge.Target == node.Id)
            .Select(edge => slots[edge.Source]).ToArray()).ToArray();
        foreach (var component in SupplyCycles.Components(predecessors))
            if (SupplyCycles.Cyclic(component, predecessors) && component.Any(slot => !SupplyCycles.Monotone(nodes[slot].Operation)))
            {
                var loop = component.Select(slot => nodes[slot].Owner).Where(owner => owner.Value != 0).Distinct().OrderBy(owner => owner.Value).ToArray();
                if (loop.Length > 0)
                {
                    const int LoopPartsNamed = 4;
                    var named = string.Join(", ", loop.Take(LoopPartsNamed).Select(owner => owner.Value));
                    var rest = loop.Length > LoopPartsNamed ? $" and {loop.Length - LoopPartsNamed} more." : ".";
                    throw new SupplyTopologyException(SupplyTopologyRefusal.NonmonotoneFeedback,
                        $"Break the wire loop through parts {named}{rest} This gate's output cannot feed its own inputs.");
                }
                throw new SupplyTopologyException(SupplyTopologyRefusal.NonmonotoneFeedback,
                    "Break the wire loop through the nonmonotone supply capability.");
            }
    }

    public ElectricalSourceId[] UpstreamSources(SupplyNodeId target)
    {
        var seen=new System.Collections.Generic.HashSet<SupplyNodeId>(16);
        var pending=new System.Collections.Generic.Queue<SupplyNodeId>(16); pending.Enqueue(target);
        while(pending.TryDequeue(out var current))
            if(seen.Add(current)) foreach(var edge in Edges) if(edge.Target==current && edge.Input==SupplyTerminal.PowerIn) pending.Enqueue(edge.Source);
        return Nodes.Where(node=>node.Kind==SupplyNodeKind.Source && seen.Contains(node.Id))
            .Select(node=>node.Source).OrderBy(source=>source.Value).ToArray();
    }
}

/// <summary>One immutable, declared electrical graph. Reachability is recomputed from enabled sources each tick.</summary>
public sealed class CompiledSupplyGraph : ICompiledSupplyGraph
{
    private readonly SupplyNodeDeclaration[] _nodes;
    private readonly SupplyEdge[] _edges;
    public ReadOnlySpan<SupplyEdge> Edges => _edges;
    private readonly int[] _sources;
    private readonly int[] _storageRoutes;
    private readonly int[][] _consumers;
    private readonly (int Source, SupplyTerminal Input)[][] _inputs;
    private readonly int[][] _components;
    private readonly bool[] _cyclic;
    public ReadOnlySpan<SupplyNodeDeclaration> Nodes => _nodes;
    /// <summary>The number of drive slots a resolution closes or opens: every admitted prismatic slot first, then every admitted
    /// revolute slot, each in its table's identity order. A joint without a drive keeps an always-open slot.</summary>
    public int DriveSlots { get; }
    public CompiledSupplyGraph(ReadOnlySpan<SupplyNodeDeclaration> nodes, ReadOnlySpan<SupplyEdge> edges,
        ReadOnlySpan<ElectricalSourceDeclaration> sources, ReadOnlySpan<PrismaticConstraintDeclaration> prismatics,
        ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives, ReadOnlySpan<RevoluteConstraintDeclaration> revolutes,
        ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives, ReadOnlySpan<ElectricalStorageBinding> bindings,
        ReadOnlySpan<ContactWorkDeclaration> storages)
    {
        try
        {
            _ = new SupplyTopology(nodes.ToArray(), edges.ToArray());
        }
        catch (SupplyTopologyException ex) when (ex.Reason == SupplyTopologyRefusal.NonmonotoneFeedback)
        {
            throw new ArgumentException("A nonmonotone electrical gate lies on a directed cycle.", ex);
        }
        catch (SupplyTopologyException ex)
        {
            throw new ArgumentException(ex.Message, ex);
        }
        _nodes = nodes.ToArray(); _edges = edges.ToArray();
        _sources = new int[nodes.Length]; _consumers = new int[nodes.Length][];
        DriveSlots = prismatics.Length + revolutes.Length;
        Array.Fill(_sources, -1);
        var slots = new System.Collections.Generic.Dictionary<SupplyNodeId,int>(16);
        var inputs = new System.Collections.Generic.List<(int Source, SupplyTerminal Input)>[nodes.Length];
        var seenConsumers = new System.Collections.Generic.HashSet<SupplyConsumer>(4);
        for (var i=0;i<nodes.Length;i++)
        {
            inputs[i] = new(4);
            (_sources[i], _consumers[i]) = AdmitNode(nodes[i], i, slots, sources, seenConsumers,
                prismatics, prismaticDrives, revolutes, revoluteDrives);
        }
        // Every declared drive of either kind is consumed by exactly one consumer node (uniqueness is checked above): a drive no route
        // names could never be supplied.
        foreach (var drive in prismaticDrives)
            if (!seenConsumers.Contains(SupplyConsumer.Prismatic(drive.Joint)))
                throw new ArgumentException("A declared prismatic drive has no consumer node.");
        foreach (var drive in revoluteDrives)
            if (!seenConsumers.Contains(SupplyConsumer.Revolute(drive.Joint)))
                throw new ArgumentException("A declared revolute drive has no consumer node.");
        _storageRoutes = AdmitStorageTargets(nodes, bindings, storages, slots);
        AdmitEdges(nodes, edges, slots, inputs);
        _inputs=inputs.Select(list=>list.ToArray()).ToArray();
        var predecessors=_inputs.Select(list=>list.Select(edge=>edge.Source).ToArray()).ToArray();
        _components=SupplyCycles.Components(predecessors);
        _cyclic=new bool[_components.Length];
        for(var c=0;c<_components.Length;c++)
        {
            _cyclic[c]=SupplyCycles.Cyclic(_components[c],predecessors);
            // Connection validation refuses these loops first; reaching here is a defect, never a refusal.
            if(_cyclic[c] && _components[c].Any(i=>!SupplyCycles.Monotone(_nodes[i].Operation)))
                throw new ArgumentException("A nonmonotone electrical gate lies on a directed cycle.");
        }
    }
    private static (int SourceSlot, int[] Consumers) AdmitNode(
        SupplyNodeDeclaration node,
        int slot,
        Dictionary<SupplyNodeId, int> slots,
        ReadOnlySpan<ElectricalSourceDeclaration> sources,
        HashSet<SupplyConsumer> seenConsumers,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics,
        ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives,
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes,
        ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives)
    {
        if (!Enum.IsDefined(node.Kind) || !slots.TryAdd(node.Id, slot) ||
            node.Id.Value == 0 || node.Owner.Value == 0)
            throw new ArgumentException("Electrical graph node requires a unique admitted owner and defined kind.");
        if ((node.Kind == SupplyNodeKind.LogicGate) != node.Operation.HasValue ||
            (node.Operation is { } operation && !Enum.IsDefined(operation)))
            throw new ArgumentException("Electrical logic operation is not admitted.");
        if (node.Kind == SupplyNodeKind.Source)
        {
            var sourceIndex = -1;
            for (var s = 0; s < sources.Length; s++)
                if (sources[s].Id == node.Source && sources[s].Owner == node.Owner) sourceIndex = s;
            if (sourceIndex < 0 || (!node.Consumers.IsDefaultOrEmpty || !node.Storages.IsDefaultOrEmpty))
                throw new ArgumentException("Electrical source node is not admitted.");
            return (sourceIndex, []);
        }
        if (node.Kind is SupplyNodeKind.Contact or SupplyNodeKind.LogicGate)
        {
            if (node.Source != default || !node.Consumers.IsDefaultOrEmpty || !node.Storages.IsDefaultOrEmpty)
                throw new ArgumentException("Electrical contact cannot carry a source or drive consumers.");
            return (-1, []);
        }
        return (-1, AdmitConsumers(node, seenConsumers, prismatics, prismaticDrives, revolutes, revoluteDrives));
    }
    private static int[] AdmitStorageTargets(ReadOnlySpan<SupplyNodeDeclaration> nodes,
        ReadOnlySpan<ElectricalStorageBinding> bindings, ReadOnlySpan<ContactWorkDeclaration> storages,
        Dictionary<SupplyNodeId, int> slots)
    {
        var targets = new Dictionary<GpuContactWorkId, SupplyNodeId>();
        foreach (var node in nodes)
            foreach (var storage in node.Storages.IsDefault ? [] : node.Storages)
            {
                var found = false;
                foreach (var declaration in storages)
                    if (declaration.Id == storage && declaration.Owner == node.Owner) found = true;
                if (node.Kind != SupplyNodeKind.Consumer || !found || !targets.TryAdd(storage, node.Id))
                    throw new ArgumentException("A storage consumer requires a unique owned store.");
            }
        var routes = new int[bindings.Length];
        for (var i = 0; i < bindings.Length; i++)
            if (!targets.TryGetValue(bindings[i].Storage, out var route) || route != bindings[i].Route ||
                !slots.TryGetValue(route, out routes[i]))
                throw new ArgumentException("A storage binding requires its exact consumer node.");
        return routes;
    }

    private static int ResolveConsumerSlot(GpuBodyId owner, SupplyConsumer consumer,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics, ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives,
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes, ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives)
    {
        // A consumer names one owned joint that declares a drive of its kind; its slot is the joint's drive slot.
        var slot = -1;
        var supplied = false;
        switch (consumer.Kind)
        {
            case SupplyConsumerKind.PrismaticDrive:
                for (var c=0;c<prismatics.Length;c++)
                    if (prismatics[c].Id==consumer.PrismaticJoint && prismatics[c].BodyA==owner) slot=c;
                foreach(var drive in prismaticDrives) if(drive.Joint==consumer.PrismaticJoint) supplied=true;
                break;
            case SupplyConsumerKind.RevoluteDrive:
                for (var r=0;r<revolutes.Length;r++)
                    if (revolutes[r].Id==consumer.RevoluteJoint && revolutes[r].BodyA==owner) slot=prismatics.Length+r;
                foreach(var drive in revoluteDrives) if(drive.Joint==consumer.RevoluteJoint) supplied=true;
                break;
            default:
                throw new ArgumentException("Electrical consumer names an undefined drive kind.");
        }
        if (slot<0 || !supplied)
            throw new ArgumentException("Electrical consumer names no driven joint its node owns.");
        return slot;
    }

    private static void AdmitEdges(ReadOnlySpan<SupplyNodeDeclaration> nodes, ReadOnlySpan<SupplyEdge> edges,
        System.Collections.Generic.Dictionary<SupplyNodeId, int> slots,
        System.Collections.Generic.List<(int Source, SupplyTerminal Input)>[] inputs)
    {
        var seenEdges=new System.Collections.Generic.HashSet<SupplyEdge>(16);
        foreach(var edge in edges)
        {
            if(!slots.TryGetValue(edge.Source,out var from) || !slots.TryGetValue(edge.Target,out var to) || from==to ||
                !seenEdges.Add(edge) || nodes[from].Kind==SupplyNodeKind.Consumer || nodes[to].Kind==SupplyNodeKind.Source ||
                (edge.Input != SupplyTerminal.PowerIn &&
                    (nodes[to].Kind != SupplyNodeKind.LogicGate || edge.Input is not (SupplyTerminal.FirstIn or SupplyTerminal.SecondIn))))
                throw new ArgumentException("Electrical graph edge is unsupported or duplicated.");
            inputs[to].Add((from,edge.Input));
        }
    }

    private static int[] AdmitConsumers(SupplyNodeDeclaration node, System.Collections.Generic.HashSet<SupplyConsumer> seenConsumers,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics, ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives,
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes, ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives)
    {
        if (node.Source != default) throw new ArgumentException("Electrical consumer cannot declare a source.");
        var admitted = new System.Collections.Generic.List<int>(4);
        if (!node.Consumers.IsDefaultOrEmpty) foreach (var consumer in node.Consumers)
        {
            var slot = ResolveConsumerSlot(node.Owner, consumer, prismatics, prismaticDrives, revolutes, revoluteDrives);
            if (!seenConsumers.Add(consumer))
                throw new ArgumentException("Electrical consumer names a driven joint more than once.");
            admitted.Add(slot);
        }
        return admitted.ToArray();
    }

    public ElectricalLogicRead[] NeutralReads() => _nodes.Where(node=>node.Kind==SupplyNodeKind.LogicGate)
        .OrderBy(node=>node.Id.Value).Select(node=>new ElectricalLogicRead(node.Id,node.Owner,ElectricalInputState.Neither,false,false)).ToArray();
    public SupplyResolution Resolve(WorkshopTickControls controls, Func<SupplyNodeId,bool> contactClosed, PhysicsElectricalRead electrical)
    {
        ArgumentNullException.ThrowIfNull(contactClosed);
        if (electrical.Count != controls.SourceEnable.Length)
            throw new ArgumentException("Supply source population differs from controls.");
        var powered = new bool[_nodes.Length];
        var contacts = new bool[_nodes.Length];
        for (var i=0;i<_nodes.Length;i++)
        {
            if (_sources[i] >= 0)
            {
                var slot = _sources[i];
                var source = electrical[slot];
                if (source.Id != _nodes[i].Source || source.Owner != _nodes[i].Owner)
                    throw new ArgumentException("Supply source identity changed.");
                powered[i] = controls.SourceEnable[slot] == ElectricalEnable.Enabled && source.Remaining.Value > 0;
            }
            if (_nodes[i].Kind==SupplyNodeKind.Contact) contacts[i]=contactClosed(_nodes[i].Id);
        }
        // Components in evaluation order: every input of a component is settled before it is visited. A singleton is evaluated
        // once. A cycle holds only monotone rules, so it settles at its least fixed point from unpowered. No previous tick feeds
        // this candidate's solution.
        for (var c=0;c<_components.Length;c++)
        {
            var changed = true;
            while (changed)
            {
                changed=false;
                foreach (var i in _components[c])
                    if (!powered[i] && _nodes[i].Kind!=SupplyNodeKind.Source && Active(i)) { powered[i]=true; changed=_cyclic[c]; }
            }
        }
        var closed=new bool[DriveSlots];
        var logic=new System.Collections.Generic.List<ElectricalLogicRead>(4);
        for(var i=0;i<_nodes.Length;i++)
        {
            foreach (var consumer in _consumers[i]) closed[consumer]=powered[i];
            if(_nodes[i].Kind==SupplyNodeKind.LogicGate)
                logic.Add(new(_nodes[i].Id,_nodes[i].Owner,
                    (Input(i,SupplyTerminal.FirstIn) ? ElectricalInputState.FirstOnly : ElectricalInputState.Neither) |
                    (Input(i,SupplyTerminal.SecondIn) ? ElectricalInputState.SecondOnly : ElectricalInputState.Neither),
                    Input(i,SupplyTerminal.PowerIn),powered[i]));
        }
        return new(closed,_storageRoutes.Select(slot => powered[slot]).ToArray(),logic.OrderBy(row=>row.Node.Value).ToArray(),
            new SupplyOutputRead(_nodes.Select((node, i) => new SupplyOutput(node.Id, powered[i], Input(i, SupplyTerminal.PowerIn))).ToArray()));
        bool Input(int node, SupplyTerminal socket) => _inputs[node].Any(edge=>edge.Input==socket && powered[edge.Source]);
        bool Active(int i) => _nodes[i].Kind switch
        {
            SupplyNodeKind.Contact => Input(i,SupplyTerminal.PowerIn) && contacts[i],
            SupplyNodeKind.Consumer => Input(i,SupplyTerminal.PowerIn),
            SupplyNodeKind.LogicGate => Input(i,SupplyTerminal.PowerIn) && Evaluate(_nodes[i].Operation!.Value,
                Input(i,SupplyTerminal.FirstIn),Input(i,SupplyTerminal.SecondIn)),
            _ => throw new ArgumentException("Unsupported electrical node.")
        };
    }
    public static bool Evaluate(LogicGateOperation operation, bool first, bool second) => operation switch
    {
        LogicGateOperation.And => first && second,
        LogicGateOperation.Nand => !(first && second),
        LogicGateOperation.Nor => !(first || second),
        LogicGateOperation.Or => first || second,
        LogicGateOperation.Xor => first != second,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };


}

public static class TickControlComposition
{
    public static WorkshopTickControls For(WorkshopRead committed,
        System.Collections.Generic.IEnumerable<(RunControlRow Row, RunControlValue Value)> held)
    {
        ArgumentNullException.ThrowIfNull(held);
        var values = new ElectricalEnable[committed.Electrical.Count];
        for (var i = 0; i < values.Length; i++) values[i] = committed.Electrical[i].Enabled;
        var targets = committed.DriveTargets.TargetSpeeds();
        foreach (var (row, value) in held)
            switch (row.Lane)
            {
                case CuriousContraptions.Abstractions.RunControlLane.SourceEnable: values[row.LaneIndex] = value.Enable; break;
                case CuriousContraptions.Abstractions.RunControlLane.RevoluteDriveTarget: targets[row.LaneIndex] = value.Scalar; break;
                default: throw new ArgumentOutOfRangeException(nameof(held), row.Lane, "Undefined run-control lane.");
            }
        return new(values) { DriveTarget = targets };
    }
    public static PhysicsTickInputs Compose(ICompiledSupplyGraph graph, WorkshopTickControls controls, Func<SupplyNodeId, bool> contactClosed, PhysicsElectricalRead electrical)
    {
        var resolved=graph.Resolve(controls,contactClosed,electrical);
        return new(controls.SourceEnable,resolved.Consumers,resolved.Logic) { StorageRoutes = resolved.StorageRoutes, Cuts = controls.Cuts, Guides = controls.Guides, DriveTarget = controls.DriveTarget };
    }
}
