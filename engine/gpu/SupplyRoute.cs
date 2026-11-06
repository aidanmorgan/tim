using System;
using System.Linq;
using System.Collections.Immutable;
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
        Nodes = Array.AsReadOnly((SupplyNodeDeclaration[])nodes.Clone());
        Edges = Array.AsReadOnly((SupplyEdge[])edges.Clone());
    }
    public GpuBodyId[] UpstreamSources(GpuBodyId target)
    {
        var seen=new System.Collections.Generic.HashSet<GpuBodyId>(16);
        var pending=new System.Collections.Generic.Queue<GpuBodyId>(16); pending.Enqueue(target);
        while(pending.TryDequeue(out var current))
            if(seen.Add(current)) foreach(var edge in Edges) if(edge.Target==current && edge.Input==PortRole.PowerIn) pending.Enqueue(edge.Source);
        return Nodes.Where(node=>node.Kind==SupplyNodeKind.Source && seen.Contains(node.Owner))
            .Select(node=>node.Owner).OrderBy(owner=>owner.Value).ToArray();
    }
}

/// <summary>One immutable, declared electrical graph. Reachability is recomputed from enabled sources each tick.</summary>
public sealed class CompiledSupplyGraph : ICompiledSupplyGraph
{
    private readonly SupplyNodeDeclaration[] _nodes;
    private readonly SupplyEdge[] _edges;
    public ReadOnlySpan<SupplyEdge> Edges => _edges;
    private readonly int[] _sources;
    private readonly int[][] _consumers;
    private readonly (int Source, PortRole Input)[][] _inputs;
    private readonly int[][] _components;
    private readonly bool[] _cyclic;
    public ReadOnlySpan<SupplyNodeDeclaration> Nodes => _nodes;
    /// <summary>The number of drive slots a resolution closes or opens: every admitted prismatic slot first, then every admitted
    /// revolute slot, each in its table's identity order. A joint without a drive keeps an always-open slot.</summary>
    public int DriveSlots { get; }
    public CompiledSupplyGraph(ReadOnlySpan<SupplyNodeDeclaration> nodes, ReadOnlySpan<SupplyEdge> edges,
        ReadOnlySpan<ElectricalSourceDeclaration> sources, ReadOnlySpan<PrismaticConstraintDeclaration> prismatics,
        ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives, ReadOnlySpan<RevoluteConstraintDeclaration> revolutes,
        ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives)
    {
        _nodes = nodes.ToArray(); _edges = edges.ToArray();
        _sources = new int[nodes.Length]; _consumers = new int[nodes.Length][];
        DriveSlots = prismatics.Length + revolutes.Length;
        Array.Fill(_sources, -1);
        var slots = new System.Collections.Generic.Dictionary<GpuBodyId,int>(16);
        var inputs = new System.Collections.Generic.List<(int Source, PortRole Input)>[nodes.Length];
        var seenConsumers = new System.Collections.Generic.HashSet<SupplyConsumer>(4);
        for (var i=0;i<nodes.Length;i++)
        {
            var node=nodes[i]; inputs[i]=new(4); _consumers[i]=[];
            if (!Enum.IsDefined(node.Kind) || !slots.TryAdd(node.Owner,i) ||
                node.Owner.Value == 0)
                throw new ArgumentException("Electrical graph node requires a unique admitted owner and defined kind.");
            if ((node.Kind==SupplyNodeKind.LogicGate) != node.Operation.HasValue ||
                (node.Operation is { } operation && !Enum.IsDefined(operation)))
                throw new ArgumentException("Electrical logic operation is not admitted.");
            if (node.Kind==SupplyNodeKind.Source)
            {
                for(var s=0;s<sources.Length;s++) if(sources[s].Id==node.Source && sources[s].Owner==node.Owner) _sources[i]=s;
                if(_sources[i]<0 || !node.Consumers.IsDefaultOrEmpty) throw new ArgumentException("Electrical source node is not admitted.");
            }
            else if(node.Kind is SupplyNodeKind.Contact or SupplyNodeKind.LogicGate)
            {
                if(node.Source!=default || !node.Consumers.IsDefaultOrEmpty)
                    throw new ArgumentException("Electrical contact cannot carry a source or drive consumers.");
            }
            else
            {
                _consumers[i] = AdmitConsumers(node, seenConsumers, prismatics, prismaticDrives, revolutes, revoluteDrives);
            }
        }
        // Every declared drive of either kind is consumed by exactly one consumer node (uniqueness is checked above): a drive no route
        // names could never be supplied.
        foreach (var drive in prismaticDrives)
            if (!seenConsumers.Contains(SupplyConsumer.Prismatic(drive.Joint)))
                throw new ArgumentException("A declared prismatic drive has no consumer node.");
        foreach (var drive in revoluteDrives)
            if (!seenConsumers.Contains(SupplyConsumer.Revolute(drive.Joint)))
                throw new ArgumentException("A declared revolute drive has no consumer node.");
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
        System.Collections.Generic.Dictionary<GpuBodyId, int> slots,
        System.Collections.Generic.List<(int Source, PortRole Input)>[] inputs)
    {
        var seenEdges=new System.Collections.Generic.HashSet<SupplyEdge>(16);
        foreach(var edge in edges)
        {
            if(!slots.TryGetValue(edge.Source,out var from) || !slots.TryGetValue(edge.Target,out var to) || from==to ||
                !seenEdges.Add(edge) || nodes[from].Kind==SupplyNodeKind.Consumer || nodes[to].Kind==SupplyNodeKind.Source ||
                (edge.Input != PortRole.PowerIn &&
                    (nodes[to].Kind != SupplyNodeKind.LogicGate || edge.Input is not (PortRole.FirstIn or PortRole.SecondIn))))
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
        .OrderBy(node=>node.Owner.Value).Select(node=>new ElectricalLogicRead(node.Owner,ElectricalInputState.Neither,false,false)).ToArray();
    public SupplyResolution Resolve(WorkshopTickControls controls, Func<GpuBodyId,bool> contactClosed, PhysicsElectricalRead electrical)
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
            if (_nodes[i].Kind==SupplyNodeKind.Contact) contacts[i]=contactClosed(_nodes[i].Owner);
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
                logic.Add(new(_nodes[i].Owner,
                    (Input(i,PortRole.FirstIn) ? ElectricalInputState.FirstOnly : ElectricalInputState.Neither) |
                    (Input(i,PortRole.SecondIn) ? ElectricalInputState.SecondOnly : ElectricalInputState.Neither),
                    Input(i,PortRole.PowerIn),powered[i]));
        }
        return new(closed,logic.OrderBy(row=>row.Owner.Value).ToArray(),
            new SupplyOutputRead(_nodes.Select((node, i) => new SupplyOutput(node.Owner, powered[i])).ToArray()));
        bool Input(int node, PortRole socket) => _inputs[node].Any(edge=>edge.Input==socket && powered[edge.Source]);
        bool Active(int i) => _nodes[i].Kind switch
        {
            SupplyNodeKind.Contact => Input(i,PortRole.PowerIn) && contacts[i],
            SupplyNodeKind.Consumer => Input(i,PortRole.PowerIn),
            SupplyNodeKind.LogicGate => Input(i,PortRole.PowerIn) && Evaluate(_nodes[i].Operation!.Value,
                Input(i,PortRole.FirstIn),Input(i,PortRole.SecondIn)),
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
    /// <summary>Whether more powered inputs can never unpower the output. Only monotone rules may share a directed cycle.</summary>
    public static bool IsMonotone(LogicGateOperation operation) => operation switch
    {
        LogicGateOperation.And => true,
        LogicGateOperation.Nand => false,
        LogicGateOperation.Nor => false,
        LogicGateOperation.Or => true,
        LogicGateOperation.Xor => false,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

}

/// <summary>Directed cycles of the declared electrical topology. Only monotone rules may share one: a gate that more input power can
/// switch off (a nonmonotone gate) has no settled zero-delay state on a loop, so connection validation refuses such a loop before any
/// solve, even when only an open contact closes it.</summary>
public static class SupplyCycles
{
    /// <summary>A node without a gate law (source, contact, consumer) only ever passes more power on with more power in.</summary>
    public static bool Monotone(LogicGateOperation? operation) => operation is not { } gate || CompiledSupplyGraph.IsMonotone(gate);

    /// <summary>The strongly connected components of a graph given as predecessor lists, in evaluation order: every edge runs from an
    /// earlier component into a later one or stays inside one. Tarjan over predecessors emits each component after everything
    /// upstream of it; iterative, so a long chain cannot exhaust the worker's stack.</summary>
    public static int[][] Components(int[][] predecessors)
    {
        ArgumentNullException.ThrowIfNull(predecessors);
        var count=predecessors.Length;
        var index=new int[count]; var low=new int[count]; var onStack=new bool[count];
        Array.Fill(index,-1);
        var stack=new System.Collections.Generic.Stack<int>(4);
        var work=new System.Collections.Generic.Stack<(int Node,int Edge)>(16);
        var components=new System.Collections.Generic.List<int[]>(4);
        var next=0;
        for(var root=0;root<count;root++)
        {
            if(index[root]>=0) continue;
            Open(root);
            while(work.TryPop(out var frame))
            {
                var (node,edge)=frame;
                if(edge<predecessors[node].Length)
                {
                    work.Push((node,edge+1));
                    var other=predecessors[node][edge];
                    if(index[other]<0) Open(other);
                    else if(onStack[other]) low[node]=Math.Min(low[node],index[other]);
                    continue;
                }
                if(work.TryPeek(out var parent)) low[parent.Node]=Math.Min(low[parent.Node],low[node]);
                if(low[node]!=index[node]) continue;
                var members=new System.Collections.Generic.List<int>(4);
                int member;
                do { member=stack.Pop(); onStack[member]=false; members.Add(member); } while(member!=node);
                members.Sort();
                components.Add(members.ToArray());
            }
        }
        return components.ToArray();
        void Open(int node) { index[node]=low[node]=next++; stack.Push(node); onStack[node]=true; work.Push((node,0)); }
    }

    public static bool Cyclic(int[] component, int[][] predecessors) =>
        component.Length>1 || Array.IndexOf(predecessors[component[0]],component[0])>=0;

    /// <summary>The ascending owners of the first directed cycle through a nonmonotone gate, or empty. <paramref name="operation"/>
    /// names each owner's declared gate law (null for a part without one); the edges are the potential topology, every electrical
    /// wire whatever its socket and whether or not a contact on it is closed.</summary>
    public static GpuBodyId[] NonmonotoneCycle(System.Collections.Generic.IEnumerable<(GpuBodyId Source, GpuBodyId Target)> edges,
        Func<GpuBodyId, LogicGateOperation?> operation)
    {
        ArgumentNullException.ThrowIfNull(edges); ArgumentNullException.ThrowIfNull(operation);
        var wires=edges.ToArray();
        var owners=wires.SelectMany(wire=>new[]{wire.Source,wire.Target}).Distinct().OrderBy(owner=>owner.Value).ToArray();
        var slot=owners.Select((owner,i)=>(owner,i)).ToDictionary(pair=>pair.owner,pair=>pair.i);
        var predecessors=owners.Select(owner=>wires.Where(wire=>wire.Target==owner).Select(wire=>slot[wire.Source]).ToArray()).ToArray();
        foreach(var component in Components(predecessors))
            if(Cyclic(component,predecessors) && component.Any(i=>!Monotone(operation(owners[i]))))
                return component.Select(i=>owners[i]).ToArray();
        return [];
    }
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
    public static PhysicsTickInputs Compose(ICompiledSupplyGraph graph, WorkshopTickControls controls, Func<GpuBodyId, bool> contactClosed, PhysicsElectricalRead electrical)
    {
        var resolved=graph.Resolve(controls,contactClosed,electrical);
        return new(controls.SourceEnable,resolved.Consumers,resolved.Logic) { Cuts = controls.Cuts, Guides = controls.Guides, DriveTarget = controls.DriveTarget };
    }
}
