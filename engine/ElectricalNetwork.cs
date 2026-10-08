using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

public sealed class ElectricalFeedbackException(string message) : InvalidOperationException(message);

/// <summary>
/// Scene boundary for an immutable per-Run binary circuit. Ports, wires, gate rules and all
/// potential contacts compile once. Each tick samples only source availability and contact state.
/// Power inputs belong to the part runtime checkpoint; circuit scratch contains no persistent state.
/// </summary>
internal sealed class ElectricalNetwork
{
    private readonly record struct Socket(MachinePart Part,SocketId Port);
    private readonly MachinePart[] _parts;
    private ElectricalSourceBinding[] _sources;
    private readonly (Socket Socket,CircuitNodeId Node)[] _inputs;
    private ElectricalContactBinding[] _contacts;
    private bool _runtimeBound;
    private readonly BinaryCircuit _circuit;
    private readonly bool[] _sourceValues,_contactValues,_result;
    private ElectricalInputRead[]? _publication;
    public void BindRuntime(ElectricalRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if(_runtimeBound)throw new InvalidOperationException("Electrical runtime is already bound.");
        var bindings=new ElectricalContactBinding[_contacts.Length];
        for(var i=0;i<bindings.Length;i++)bindings[i]=_contacts[i].Bind(runtime);
        var sources=new ElectricalSourceBinding[_sources.Length];
        for(var i=0;i<sources.Length;i++)sources[i]=_sources[i].Bind(runtime);
        _contacts=bindings;_sources=sources;_runtimeBound=true;
    }
    public void BindPublication(ScenePhysicsAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if(_publication is not null)throw new InvalidOperationException("Electrical publication is already bound.");
        var reads=new ElectricalInputRead[_inputs.Length];
        for(var i=0;i<reads.Length;i++)
        {
            var socket=_inputs[i].Socket;
            if(!Enum.IsDefined(socket.Port))throw new ArgumentException("Unsupported electrical input socket.");
            reads[i]=new(new(assembly.QueryOwnerId(new(socket.Part,MachinePart.RootBody)),socket.Port),
                ElectricalAvailability.Unavailable);
        }
        _publication=reads;
    }
    /// <summary>Borrowed producer view, copied by the committed buffer before any further solve.</summary>
    public ReadOnlySpan<ElectricalInputRead> CapturePublicationReads()
    {
        var reads=_publication??throw new InvalidOperationException("Electrical publication requires a binding.");
        for(var i=0;i<reads.Length;i++)
            reads[i]=reads[i] with {Availability=_result[_inputs[i].Node.Value]
                ?ElectricalAvailability.Available:ElectricalAvailability.Unavailable};
        return reads;
    }

    public ElectricalNetwork(MachineWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _parts=world.Parts.ToArray();
        var ports=_parts.ToDictionary(part=>part,part=>part.ConnectionPorts.ToArray());
        var nodes=new Dictionary<Socket,CircuitNodeId>();
        var sockets=new List<Socket>();
        var sources=new List<CircuitNodeId>();
        var sourceBindings=new List<ElectricalSourceBinding>();
        var inputs=new List<(Socket,CircuitNodeId)>();
        foreach(var part in _parts)
            foreach(var port in ports[part])
            {
                if(port.Domain!=ConnectionDomain.Electrical)continue;
                var socket=new Socket(part,port.Id);var id=new CircuitNodeId(nodes.Count);
                nodes.Add(socket,id);sockets.Add(socket);
                if(port.Direction==PortDirection.Input)inputs.Add((socket,id));
            }
        var contacts=new List<CircuitContact>();var gates=new List<CircuitGate>();
        var bindings=new List<ElectricalContactBinding>();var wires=new List<CircuitWire>();
        foreach(var part in _parts)
        {
            CircuitNodeId Require(SocketId id,PortDirection direction)
            {
                if(!ports[part].Any(port=>port.Id==id&&port.Domain==ConnectionDomain.Electrical&&port.Direction==direction))
                    throw new InvalidOperationException("Invalid electrical declaration socket.");
                return nodes[new(part,id)];
            }
            foreach(var source in part.ElectricalSources)
            {
                sources.Add(Require(source.Output,PortDirection.Output));
                sourceBindings.Add(new(part,source.Signal));
            }
            foreach(var route in part.ElectricalRoutes)
            {
                contacts.Add(new(Require(route.Input,PortDirection.Input),Require(route.Output,PortDirection.Output)));
                bindings.Add(new(part,route.Signal));
            }
            foreach(var gate in part.ElectricalGates)
                gates.Add(new(gate.Operation,Require(gate.First,PortDirection.Input),
                    Require(gate.Second,PortDirection.Input),Require(gate.Supply,PortDirection.Input),
                    Require(gate.Output,PortDirection.Output)));
        }
        foreach(var link in world.Connections)
        {
            if(link.Type!=ConnectionDomain.Electrical)continue;
            if(!world.IsValidConnection(link))throw new InvalidOperationException("Invalid electrical wire.");
            wires.Add(new(nodes[new(world.FindPart(link.From)!,link.FromPort!.Value)],
                nodes[new(world.FindPart(link.To)!,link.ToPort!.Value)]));
        }
        _sources=sourceBindings.ToArray();_inputs=inputs.ToArray();_contacts=bindings.ToArray();
        try
        {
            _circuit=new(nodes.Count,sources.ToArray(),
                wires.ToArray(),contacts.ToArray(),gates.ToArray());
        }
        catch(CircuitFeedbackException error)
        {
            var owners=new SortedSet<string>(StringComparer.Ordinal);
            foreach(var node in error.Nodes)owners.Add(sockets[node.Value].Part.Uid);
            throw new ElectricalFeedbackException("Break the wire loop through "+string.Join(", ",owners)+
                ". XOR, NOR and NAND outputs cannot feed their own inputs.");
        }
        _sourceValues=new bool[_circuit.SourceCount];_contactValues=new bool[_circuit.ContactCount];
        _result=new bool[_circuit.NodeCount];
    }

    public void Solve()
    {
        for(var i=0;i<_sources.Length;i++)
            _sourceValues[i]=_sources[i].Read();
        for(var i=0;i<_contacts.Length;i++)_contactValues[i]=_contacts[i].Read();
        _circuit.Solve(_sourceValues,_contactValues,_result);
        // Nothing observable changes until all inputs have been accepted and settling succeeds.
        foreach(var part in _parts)part.ClearElectricalPower();
        foreach(var input in _inputs)
            if(_result[input.Node.Value])input.Socket.Part.SupplyElectricalPower(input.Socket.Port);
    }
}
