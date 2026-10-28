using System;
using System.Linq;
using System.Collections.Immutable;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

public enum SupplyNodeKind { Source, Contact, Consumer, LogicGate }
public readonly record struct SupplyNodeDeclaration(GpuBodyId Owner, SupplyNodeKind Kind,
    ElectricalSourceId Source = default, ImmutableArray<PrismaticConstraintId> Consumers = default, LogicGateOperation? Operation = null);
public readonly record struct SupplyEdge(GpuBodyId Source, GpuBodyId Target, WorkshopSocket Input);
public readonly record struct SupplyResolution(bool[] Consumers, ElectricalLogicRead[] Logic);

/// <summary>Declared topology shared by physical supply projection and source provenance queries.</summary>
public sealed record SupplyTopology(SupplyNodeDeclaration[] Nodes, SupplyEdge[] Edges)
{
    internal static SupplyTopology Create(WorkshopConstruction construction,
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
    public GpuBodyId[] UpstreamSources(GpuBodyId target)
    {
        var seen=new System.Collections.Generic.HashSet<GpuBodyId>(16);
        var pending=new System.Collections.Generic.Queue<GpuBodyId>(16); pending.Enqueue(target);
        while(pending.TryDequeue(out var current))
            if(seen.Add(current)) foreach(var edge in Edges) if(edge.Target==current && edge.Input==WorkshopSocket.PowerIn) pending.Enqueue(edge.Source);
        return Nodes.Where(node=>node.Kind==SupplyNodeKind.Source && seen.Contains(node.Owner))
            .Select(node=>node.Owner).OrderBy(owner=>owner.Value).ToArray();
    }
}

/// <summary>One immutable, declared electrical graph. Reachability is recomputed from enabled sources each tick.</summary>
public sealed class CompiledSupplyGraph
{
    private readonly SupplyNodeDeclaration[] _nodes;
    private readonly int[] _sources;
    private readonly int[][] _consumers;
    private readonly (int Source, WorkshopSocket Input)[][] _inputs;
    private readonly int[][] _components;
    private readonly bool[] _cyclic;
    public ReadOnlySpan<SupplyNodeDeclaration> Nodes => _nodes;
    public CompiledSupplyGraph(ReadOnlySpan<SupplyNodeDeclaration> nodes, ReadOnlySpan<SupplyEdge> edges,
        ReadOnlySpan<ElectricalSourceDeclaration> sources, ReadOnlySpan<PrismaticConstraintDeclaration> consumers,
        ReadOnlySpan<PrismaticDriveDeclaration> drives, ReadOnlySpan<RigidBodyDeclaration> bodies)
    {
        _nodes = nodes.ToArray();
        _sources = new int[nodes.Length]; _consumers = new int[nodes.Length][];
        Array.Fill(_sources, -1);
        var slots = new System.Collections.Generic.Dictionary<GpuBodyId,int>(16);
        var inputs = new System.Collections.Generic.List<(int Source, WorkshopSocket Input)>[nodes.Length];
        var seenConsumers = new System.Collections.Generic.HashSet<PrismaticConstraintId>(4);
        for (var i=0;i<nodes.Length;i++)
        {
            var node=nodes[i]; inputs[i]=new(4); _consumers[i]=[];
            if (!Enum.IsDefined(node.Kind) || !slots.TryAdd(node.Owner,i) ||
                !bodies.ToArray().Any(body=>body.Id==node.Owner))
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
                if(node.Source!=default || !node.Consumers.IsDefaultOrEmpty || !bodies.ToArray().Any(body=>body.Id==node.Owner && body.Motion==RigidMotionKind.Static))
                    throw new ArgumentException("Electrical contact requires one static owner.");
            }
            else
            {
                if (node.Source != default) throw new ArgumentException("Electrical consumer cannot declare a source.");
                var admitted = new System.Collections.Generic.List<int>(4);
                if (!node.Consumers.IsDefaultOrEmpty) foreach (var joint in node.Consumers)
                {
                    var slot = -1;
                    for (var c=0;c<consumers.Length;c++)
                        if (consumers[c].Id==joint && consumers[c].BodyA==node.Owner) slot=c;
                    var supplied=false;
                    foreach(var drive in drives) if(drive.Joint==joint) supplied=true;
                    if (slot<0 || !supplied || !seenConsumers.Add(joint))
                        throw new ArgumentException("Electrical consumer requires unique admitted driven actuators.");
                    admitted.Add(slot);
                }
                _consumers[i]=admitted.ToArray();
            }
        }
        var seenEdges=new System.Collections.Generic.HashSet<SupplyEdge>(16);
        foreach(var edge in edges)
        {
            if(!slots.TryGetValue(edge.Source,out var from) || !slots.TryGetValue(edge.Target,out var to) || from==to ||
                !seenEdges.Add(edge) || nodes[from].Kind==SupplyNodeKind.Consumer || nodes[to].Kind==SupplyNodeKind.Source ||
                (edge.Input != WorkshopSocket.PowerIn &&
                    (nodes[to].Kind != SupplyNodeKind.LogicGate || edge.Input is not (WorkshopSocket.FirstIn or WorkshopSocket.SecondIn))))
                throw new ArgumentException("Electrical graph edge is unsupported or duplicated.");
            inputs[to].Add((from,edge.Input));
        }
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
    public ElectricalLogicRead[] NeutralReads() => _nodes.Where(node=>node.Kind==SupplyNodeKind.LogicGate)
        .OrderBy(node=>node.Owner.Value).Select(node=>new ElectricalLogicRead(node.Owner,ElectricalInputState.Neither,false,false)).ToArray();
    public SupplyResolution Resolve(int consumers, WorkshopTickControls controls, Func<GpuBodyId,bool> contactClosed)
    {
        ArgumentNullException.ThrowIfNull(contactClosed);
        var powered = new bool[_nodes.Length];
        var contacts = new bool[_nodes.Length];
        for (var i=0;i<_nodes.Length;i++)
        {
            if (_sources[i]>=0) powered[i]=controls.SourceEnable[_sources[i]]==ElectricalEnable.Enabled;
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
        var closed=new bool[consumers];
        var logic=new System.Collections.Generic.List<ElectricalLogicRead>(4);
        for(var i=0;i<_nodes.Length;i++)
        {
            foreach (var consumer in _consumers[i]) closed[consumer]=powered[i];
            if(_nodes[i].Kind==SupplyNodeKind.LogicGate)
                logic.Add(new(_nodes[i].Owner,
                    (Input(i,WorkshopSocket.FirstIn) ? ElectricalInputState.FirstOnly : ElectricalInputState.Neither) |
                    (Input(i,WorkshopSocket.SecondIn) ? ElectricalInputState.SecondOnly : ElectricalInputState.Neither),
                    Input(i,WorkshopSocket.PowerIn),powered[i]));
        }
        return new(closed,logic.OrderBy(row=>row.Owner.Value).ToArray());
        bool Input(int node, WorkshopSocket socket) => _inputs[node].Any(edge=>edge.Input==socket && powered[edge.Source]);
        bool Active(int i) => _nodes[i].Kind switch
        {
            SupplyNodeKind.Contact => Input(i,WorkshopSocket.PowerIn) && contacts[i],
            SupplyNodeKind.Consumer => Input(i,WorkshopSocket.PowerIn),
            SupplyNodeKind.LogicGate => Input(i,WorkshopSocket.PowerIn) && Evaluate(_nodes[i].Operation!.Value,
                Input(i,WorkshopSocket.FirstIn),Input(i,WorkshopSocket.SecondIn)),
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

/// <summary>Player controls the discrete owner hands to one physics step: the enable state of every admitted source, in source order.</summary>
public sealed record WorkshopTickControls(ElectricalEnable[] SourceEnable)
{
    public static WorkshopTickControls For(PhysicsElectricalRead committed, Func<GpuBodyId, ElectricalEnable, ElectricalEnable> enable)
    {
        ArgumentNullException.ThrowIfNull(enable);
        var values = new ElectricalEnable[committed.Count];
        for (var i = 0; i < values.Length; i++) values[i] = enable(committed[i].Owner, committed[i].Enabled);
        return new(values);
    }
    public void Validate(int sources)
    {
        if (SourceEnable is null || SourceEnable.Length != sources || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Tick controls must name one defined enable state per admitted source.");
    }
}

/// <summary>Everything one physics step reads besides the committed world: per-source enables and, per prismatic record,
/// whether its compiled supply route is closed this tick. Data flows one way: committed discrete state → next physics step.</summary>
public sealed record PhysicsTickInputs(ElectricalEnable[] SourceEnable, bool[] SupplyClosed, ElectricalLogicRead[]? Logic = null)
{
    public static PhysicsTickInputs Admission { get; } = new([], []);
    public static PhysicsTickInputs Compose(CompiledSupplyGraph graph, int prismatics, WorkshopTickControls controls,
        Func<GpuBodyId, bool> contactClosed)
    {
        var resolved=graph.Resolve(prismatics,controls,contactClosed);
        return new(controls.SourceEnable,resolved.Consumers,resolved.Logic);
    }
    public void Validate(int sources, int prismatics)
    {
        if (SourceEnable is null || SupplyClosed is null || SourceEnable.Length != sources ||
            SupplyClosed.Length != prismatics || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Physics tick inputs differ from the admitted source and prismatic tables.");
    }
}
