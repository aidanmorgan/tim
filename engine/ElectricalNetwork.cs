using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Settled binary availability, not voltage/current. Each solve starts unpowered.</summary>
public static class ElectricalNetwork
{
    private readonly record struct Socket(MachinePart Part,string Port);
    private sealed class Equation
    {
        public bool Source;
        public List<Socket> Wires = [];
        public List<(ElectricalGate Rule,Socket First,Socket Second,Socket Supply)> Gates = [];
        public IEnumerable<Socket> Dependencies => Wires.Concat(Gates.SelectMany(g=>new[]{g.First,g.Second,g.Supply}));
    }

    public static void Solve(MachineWorld world)
    {
        foreach(var part in world.Parts) part.ClearElectricalPower();
        var equations=Snapshot(world);
        var components=Components(equations);
        var powered=new HashSet<Socket>();
        foreach(var component in components)
        {
            // Tarjan visits dependencies first. Nonmonotone cycles are rejected before any commit.
            var cyclic=component.Count>1 || equations[component[0]].Dependencies.Contains(component[0]);
            if(cyclic && component.Any(s=>equations[s].Gates.Any(g=>
                g.Rule.Operation is LogicGateKind.Xor or LogicGateKind.Nor or LogicGateKind.Nand)))
                throw new InvalidOperationException("Electrical XOR/NOR/NAND feedback needs an explicit memory or delay boundary.");
            bool changed;
            do
            {
                changed=false;
                foreach(var socket in component)
                {
                    var e=equations[socket];
                    var on=e.Source || e.Wires.Any(powered.Contains) || e.Gates.Any(g=>
                        powered.Contains(g.Supply) && LogicGate.Evaluate(g.Rule.Operation,
                            powered.Contains(g.First),powered.Contains(g.Second)));
                    if(on && powered.Add(socket)) changed=true;
                }
            } while(cyclic && changed); // finite monotone least fixed point, not an iteration cap
        }
        foreach(var socket in powered)
            if(socket.Part.ConnectionPorts.Any(p=>p.Id==socket.Port && p.Direction==PortDirection.Input))
                socket.Part.SupplyElectricalPower(socket.Port);
    }

    private static Dictionary<Socket,Equation> Snapshot(MachineWorld world)
    {
        var equations=new Dictionary<Socket,Equation>();
        var ports=world.Parts.ToDictionary(p=>p,p=>p.ConnectionPorts.ToArray());
        foreach(var part in world.Parts)
            foreach(var port in ports[part].Where(p=>p.Domain==ConnectionDomain.Electrical))
                equations.Add(new(part,port.Id),new(){Source=port.Direction==PortDirection.Output && part.SuppliesElectricity(port.Id)});
        foreach(var part in world.Parts)
        {
            bool Has(string id,PortDirection direction)=>ports[part].Any(p=>
                p.Id==id && p.Domain==ConnectionDomain.Electrical && p.Direction==direction);
            foreach(var route in part.ElectricalRoutes)
            {
                if(!Has(route.Input,PortDirection.Input)||!Has(route.Output,PortDirection.Output))
                    throw new InvalidOperationException("Invalid electrical route sockets.");
                equations[new(part,route.Output)].Wires.Add(new(part,route.Input));
            }
            foreach(var gate in part.ElectricalGates)
            {
                if(!Enum.IsDefined(gate.Operation)||new[]{gate.First,gate.Second,gate.Supply}.Distinct().Count()!=3
                    ||!Has(gate.First,PortDirection.Input)||!Has(gate.Second,PortDirection.Input)
                    ||!Has(gate.Supply,PortDirection.Input)||!Has(gate.Output,PortDirection.Output))
                    throw new InvalidOperationException("Invalid electrical gate operation or sockets.");
                equations[new(part,gate.Output)].Gates.Add((gate,new(part,gate.First),new(part,gate.Second),new(part,gate.Supply)));
            }
        }
        foreach(var link in world.Connections.Where(l=>l.Type==ConnectionDomain.Electrical))
        {
            if(!world.IsValidConnection(link))throw new InvalidOperationException("Invalid electrical wire.");
            equations[new(world.FindPart(link.To)!,link.ToPort!)].Wires.Add(new(world.FindPart(link.From)!,link.FromPort!));
        }
        return equations;
    }

    // Dependency-directed Tarjan: completed SCCs are already in evaluation order.
    private static List<List<Socket>> Components(Dictionary<Socket,Equation> equations)
    {
        var indices=new Dictionary<Socket,int>();
        var low=new Dictionary<Socket,int>();
        var stack=new Stack<Socket>();
        var stacked=new HashSet<Socket>();
        var result=new List<List<Socket>>();
        var next=0;
        void Visit(Socket socket)
        {
            indices[socket]=low[socket]=next++;
            stack.Push(socket);stacked.Add(socket);
            foreach(var dependency in equations[socket].Dependencies)
            {
                if(!indices.ContainsKey(dependency))
                {
                    Visit(dependency);
                    low[socket]=Math.Min(low[socket],low[dependency]);
                }
                else if(stacked.Contains(dependency))low[socket]=Math.Min(low[socket],indices[dependency]);
            }
            if(low[socket]!=indices[socket])return;
            var component=new List<Socket>();
            Socket member;
            do{member=stack.Pop();stacked.Remove(member);component.Add(member);}while(member!=socket);
            result.Add(component);
        }
        foreach(var socket in equations.Keys)if(!indices.ContainsKey(socket))Visit(socket);
        return result;
    }
}
