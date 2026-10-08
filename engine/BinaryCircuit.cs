using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Dense identity within one immutable compiled circuit; never a scene or catalogue ID.</summary>
public readonly record struct CircuitNodeId
{
    public int Value { get; }
    public CircuitNodeId(int value)
    {
        if(value<0)throw new ArgumentOutOfRangeException(nameof(value));
        Value=value;
    }
}
public readonly record struct CircuitWire(CircuitNodeId Input,CircuitNodeId Output);
public readonly record struct CircuitContact(CircuitNodeId Input,CircuitNodeId Output);
public readonly record struct CircuitGate(LogicGateKind Operation,CircuitNodeId First,
    CircuitNodeId Second,CircuitNodeId Supply,CircuitNodeId Output);

public sealed class CircuitFeedbackException : InvalidOperationException
{
    private readonly CircuitNodeId[] _nodes;
    public ReadOnlySpan<CircuitNodeId> Nodes=>_nodes;
    internal CircuitFeedbackException(CircuitNodeId[] nodes)
        :base("Zero-delay XOR, NOR and NAND feedback is unsupported.")=>_nodes=nodes;
}

/// <summary>
/// Immutable binary-availability topology with reusable, unobservable solve scratch.
/// This is a logical control law, not an electrical voltage/current/energy model.
/// Single-thread ownership: sources and contacts are sampled together; only the completed
/// least fixed point is copied to the caller. Every solve starts false, including feedback.
/// </summary>
public sealed class BinaryCircuit
{
    private readonly record struct ContactInput(int Index,CircuitNodeId Input);
    private readonly record struct Component(CircuitNodeId[] Nodes,bool Cyclic);
    private readonly CircuitNodeId[] _sources;
    private readonly CircuitNodeId[][] _wires;
    private readonly ContactInput[][] _contacts;
    private readonly CircuitGate[][] _gates;
    private readonly Component[] _components;
    private readonly bool[] _powered;
    public int NodeCount=>_powered.Length;
    public int SourceCount=>_sources.Length;
    public int ContactCount { get; }

    public BinaryCircuit(int nodeCount,ReadOnlySpan<CircuitNodeId> sources,
        ReadOnlySpan<CircuitWire> wires,ReadOnlySpan<CircuitContact> contacts,
        ReadOnlySpan<CircuitGate> gates)
    {
        if(nodeCount<0)throw new ArgumentOutOfRangeException(nameof(nodeCount));
        _powered=new bool[nodeCount];_sources=sources.ToArray();ContactCount=contacts.Length;
        var wireLists=new List<CircuitNodeId>[nodeCount];
        var contactLists=new List<ContactInput>[nodeCount];
        var gateLists=new List<CircuitGate>[nodeCount];
        var dependencies=new List<CircuitNodeId>[nodeCount];
        for(var i=0;i<nodeCount;i++)
        {
            wireLists[i]=[];contactLists[i]=[];gateLists[i]=[];dependencies[i]=[];
        }
        void RequireNode(CircuitNodeId node)
        {
            if(node.Value>=nodeCount)throw new ArgumentOutOfRangeException(nameof(node));
        }
        var uniqueSources=new HashSet<CircuitNodeId>();
        foreach(var source in sources)
        {
            RequireNode(source);
            if(!uniqueSources.Add(source))throw new ArgumentException("Duplicate source node.");
        }
        foreach(var wire in wires)
        {
            RequireNode(wire.Input);RequireNode(wire.Output);
            wireLists[wire.Output.Value].Add(wire.Input);
            dependencies[wire.Output.Value].Add(wire.Input);
        }
        for(var i=0;i<contacts.Length;i++)
        {
            var contact=contacts[i];RequireNode(contact.Input);RequireNode(contact.Output);
            contactLists[contact.Output.Value].Add(new(i,contact.Input));
            // Potential topology includes open contacts; closing one cannot introduce a new SCC.
            dependencies[contact.Output.Value].Add(contact.Input);
        }
        foreach(var gate in gates)
        {
            RequireNode(gate.First);RequireNode(gate.Second);RequireNode(gate.Supply);RequireNode(gate.Output);
            if(!Enum.IsDefined(gate.Operation))throw new ArgumentOutOfRangeException(nameof(gates));
            if(gate.First==gate.Second||gate.First==gate.Supply||gate.Second==gate.Supply)
                throw new ArgumentException("Gate conditions and supply require distinct nodes.");
            gateLists[gate.Output.Value].Add(gate);
            dependencies[gate.Output.Value].Add(gate.First);
            dependencies[gate.Output.Value].Add(gate.Second);
            dependencies[gate.Output.Value].Add(gate.Supply);
        }
        _wires=new CircuitNodeId[nodeCount][];_contacts=new ContactInput[nodeCount][];
        _gates=new CircuitGate[nodeCount][];
        for(var i=0;i<nodeCount;i++)
        {
            _wires[i]=wireLists[i].ToArray();_contacts[i]=contactLists[i].ToArray();
            _gates[i]=gateLists[i].ToArray();
        }
        _components=CompileComponents(dependencies);
        foreach(var component in _components)
        {
            if(!component.Cyclic)continue;
            foreach(var node in component.Nodes)
                foreach(var gate in _gates[node.Value])
                    if(gate.Operation is LogicGateKind.Xor or LogicGateKind.Nor or LogicGateKind.Nand)
                        throw new CircuitFeedbackException(component.Nodes);
        }
    }

    public void Solve(ReadOnlySpan<bool> sources,ReadOnlySpan<bool> closedContacts,Span<bool> result)
    {
        if(sources.Length!=SourceCount||closedContacts.Length!=ContactCount||result.Length!=NodeCount)
            throw new ArgumentException("Circuit input and result lengths must exactly match the compiled topology.");
        // Scratch is never a previous-state seed. Output may alias either input because copying is last.
        Array.Clear(_powered);
        for(var i=0;i<_sources.Length;i++)_powered[_sources[i].Value]=sources[i];
        foreach(var component in _components)
        {
            bool changed;
            do
            {
                changed=false;
                foreach(var node in component.Nodes)
                {
                    var index=node.Value;
                    if(_powered[index])continue;
                    var on=false;
                    foreach(var input in _wires[index])if(_powered[input.Value]){on=true;break;}
                    if(!on)foreach(var contact in _contacts[index])
                        if(closedContacts[contact.Index]&&_powered[contact.Input.Value]){on=true;break;}
                    if(!on)foreach(var gate in _gates[index])
                        if(_powered[gate.Supply.Value]&&LogicGate.Evaluate(gate.Operation,
                            _powered[gate.First.Value],_powered[gate.Second.Value])){on=true;break;}
                    if(on){_powered[index]=true;changed=true;}
                }
            }while(component.Cyclic&&changed);
        }
        _powered.AsSpan().CopyTo(result);
    }

    // Iterative dependency-directed Tarjan: no recursion limit or per-solve graph discovery.
    private static Component[] CompileComponents(List<CircuitNodeId>[] dependencies)
    {
        var count=dependencies.Length;
        var indices=new int[count];Array.Fill(indices,-1);
        var low=new int[count];var stacked=new bool[count];
        var nodes=new Stack<CircuitNodeId>();var frames=new Stack<(CircuitNodeId Node,int Next)>();
        var result=new List<Component>();var next=0;
        void Enter(CircuitNodeId node)
        {
            indices[node.Value]=low[node.Value]=next++;
            nodes.Push(node);stacked[node.Value]=true;frames.Push((node,0));
        }
        for(var root=0;root<count;root++)
        {
            if(indices[root]>=0)continue;
            Enter(new(root));
            while(frames.Count>0)
            {
                var frame=frames.Pop();var index=frame.Node.Value;
                if(frame.Next<dependencies[index].Count)
                {
                    var dependency=dependencies[index][frame.Next];
                    frames.Push((frame.Node,frame.Next+1));
                    if(indices[dependency.Value]<0)Enter(dependency);
                    else if(stacked[dependency.Value])low[index]=Math.Min(low[index],indices[dependency.Value]);
                    continue;
                }
                if(low[index]==indices[index])
                {
                    var component=new List<CircuitNodeId>();CircuitNodeId member;
                    do
                    {
                        member=nodes.Pop();stacked[member.Value]=false;component.Add(member);
                    }while(member!=frame.Node);
                    component.Sort((a,b)=>a.Value.CompareTo(b.Value));
                    result.Add(new(component.ToArray(),component.Count>1||dependencies[index].Contains(frame.Node)));
                }
                if(frames.TryPeek(out var parent))
                    low[parent.Node.Value]=Math.Min(low[parent.Node.Value],low[index]);
            }
        }
        return result.ToArray();
    }
}

