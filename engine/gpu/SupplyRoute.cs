using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

public enum SupplyNodeKind { Source, Contact, Consumer }
public readonly record struct SupplyNodeDeclaration(GpuBodyId Owner, SupplyNodeKind Kind,
    ElectricalSourceId Source = default, PrismaticConstraintId Consumer = default);
public readonly record struct SupplyEdge(GpuBodyId Source, GpuBodyId Target);

/// <summary>Declared topology shared by physical supply projection and source provenance queries.</summary>
public sealed record SupplyTopology(SupplyNodeDeclaration[] Nodes, SupplyEdge[] Edges)
{
    internal static SupplyTopology Create(WorkshopConstruction construction,
        Func<PartId, CuriousContraptions.Bundle.BundleEntry> declaration)
    {
        var nodes=new System.Collections.Generic.List<SupplyNodeDeclaration>();
        foreach(var instance in construction.Instances)
        {
            var entry=declaration(instance.PartId);
            var kind=entry.Electrical?.Supply?.Kind;
            if(kind==CuriousContraptions.Abstractions.Electrical.SupplyCapabilityKind.Source)
                nodes.Add(new(instance.Id,SupplyNodeKind.Source,WorkshopPhysicsCompiler.ElectricalSource(instance.Id)));
            else if(kind==CuriousContraptions.Abstractions.Electrical.SupplyCapabilityKind.SwitchedContact)
                nodes.Add(new(instance.Id,SupplyNodeKind.Contact));
            else if(entry.Ports.Any(port=>port.Socket==CuriousContraptions.Abstractions.PortSocket.PowerIn))
                nodes.Add(new(instance.Id,SupplyNodeKind.Consumer));
        }
        var byOwner=nodes.ToDictionary(node=>node.Owner);
        var edges=construction.Connections.Where(link=>link.Domain==WorkshopConnectionDomain.Electrical &&
            byOwner.TryGetValue(link.Source,out var from) && from.Kind!=SupplyNodeKind.Consumer &&
            byOwner.TryGetValue(link.Target,out var to) && to.Kind!=SupplyNodeKind.Source)
            .Select(link=>new SupplyEdge(link.Source,link.Target)).ToArray();
        return new(nodes.ToArray(),edges);
    }
    public GpuBodyId[] UpstreamSources(GpuBodyId target)
    {
        var seen=new System.Collections.Generic.HashSet<GpuBodyId>();
        var pending=new System.Collections.Generic.Queue<GpuBodyId>(); pending.Enqueue(target);
        while(pending.TryDequeue(out var current))
            if(seen.Add(current)) foreach(var edge in Edges) if(edge.Target==current) pending.Enqueue(edge.Source);
        return Nodes.Where(node=>node.Kind==SupplyNodeKind.Source && seen.Contains(node.Owner))
            .Select(node=>node.Owner).OrderBy(owner=>owner.Value).ToArray();
    }
}

/// <summary>One immutable, declared electrical graph. Reachability is recomputed from enabled sources each tick.</summary>
public sealed class CompiledSupplyGraph
{
    private readonly SupplyNodeDeclaration[] _nodes;
    private readonly int[] _sources;
    private readonly int[] _consumers;
    private readonly int[][] _outputs;
    public ReadOnlySpan<SupplyNodeDeclaration> Nodes => _nodes;
    public CompiledSupplyGraph(ReadOnlySpan<SupplyNodeDeclaration> nodes, ReadOnlySpan<SupplyEdge> edges,
        ReadOnlySpan<ElectricalSourceDeclaration> sources, ReadOnlySpan<PrismaticConstraintDeclaration> consumers,
        ReadOnlySpan<RigidBodyDeclaration> bodies)
    {
        _nodes = nodes.ToArray();
        _sources = new int[nodes.Length]; _consumers = new int[nodes.Length];
        Array.Fill(_sources, -1); Array.Fill(_consumers, -1);
        var slots = new System.Collections.Generic.Dictionary<GpuBodyId,int>();
        var outputs = new System.Collections.Generic.List<int>[nodes.Length];
        var seenConsumers = new System.Collections.Generic.HashSet<PrismaticConstraintId>();
        for (var i=0;i<nodes.Length;i++)
        {
            var node=nodes[i]; outputs[i]=[];
            if (!Enum.IsDefined(node.Kind) || !slots.TryAdd(node.Owner,i) ||
                !bodies.ToArray().Any(body=>body.Id==node.Owner))
                throw new ArgumentException("Electrical graph node requires a unique admitted owner and defined kind.");
            if (node.Kind==SupplyNodeKind.Source)
            {
                for(var s=0;s<sources.Length;s++) if(sources[s].Id==node.Source && sources[s].Owner==node.Owner) _sources[i]=s;
                if(_sources[i]<0 || node.Consumer!=default) throw new ArgumentException("Electrical source node is not admitted.");
            }
            else if(node.Kind==SupplyNodeKind.Contact)
            {
                if(node.Source!=default || node.Consumer!=default || !bodies.ToArray().Any(body=>body.Id==node.Owner && body.Motion==RigidMotionKind.Static))
                    throw new ArgumentException("Electrical contact requires one static owner.");
            }
            else
            {
                for(var c=0;c<consumers.Length;c++) if(consumers[c].Id==node.Consumer && consumers[c].BodyA==node.Owner) _consumers[i]=c;
                if((node.Consumer!=default && (_consumers[i]<0 || !seenConsumers.Add(node.Consumer))) || node.Source!=default)
                    throw new ArgumentException("Electrical consumer requires one admitted actuator.");
            }
        }
        var seenEdges=new System.Collections.Generic.HashSet<SupplyEdge>();
        foreach(var edge in edges)
        {
            if(!slots.TryGetValue(edge.Source,out var from) || !slots.TryGetValue(edge.Target,out var to) || from==to ||
                !seenEdges.Add(edge) || nodes[from].Kind==SupplyNodeKind.Consumer || nodes[to].Kind==SupplyNodeKind.Source)
                throw new ArgumentException("Electrical graph edge is unsupported or duplicated.");
            outputs[from].Add(to);
        }
        _outputs=outputs.Select(list=>list.ToArray()).ToArray();
    }
    public bool[] Resolve(int consumers, WorkshopTickControls controls, Func<GpuBodyId,bool> contactClosed)
    {
        ArgumentNullException.ThrowIfNull(contactClosed);
        var reached=new bool[_nodes.Length]; var pending=new int[_nodes.Length]; var head=0; var tail=0;
        var closed=new bool[consumers];
        for(var i=0;i<_nodes.Length;i++)
            if(_sources[i]>=0 && controls.SourceEnable[_sources[i]]==ElectricalEnable.Enabled)
            { reached[i]=true; pending[tail++]=i; }
        while(head<tail)
        {
            var slot=pending[head++];
            if(_nodes[slot].Kind==SupplyNodeKind.Contact && !contactClosed(_nodes[slot].Owner)) continue;
            if(_consumers[slot]>=0) closed[_consumers[slot]]=true;
            foreach(var next in _outputs[slot]) if(!reached[next]) {reached[next]=true;pending[tail++]=next;}
        }
        return closed;
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
public sealed record PhysicsTickInputs(ElectricalEnable[] SourceEnable, bool[] SupplyClosed)
{
    public static PhysicsTickInputs Admission { get; } = new([], []);
    public static PhysicsTickInputs Compose(CompiledSupplyGraph graph, int prismatics, WorkshopTickControls controls,
        Func<GpuBodyId, bool> contactClosed) => new(controls.SourceEnable, graph.Resolve(prismatics, controls, contactClosed));
    public void Validate(int sources, int prismatics)
    {
        if (SourceEnable is null || SupplyClosed is null || SourceEnable.Length != sources ||
            SupplyClosed.Length != prismatics || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Physics tick inputs differ from the admitted source and prismatic tables.");
    }
}
