using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Ideal directed shaft-speed transmission, not a torque/load solver.
/// One belt per input; fan-out is allowed. Loops and competing drives are unsupported
/// and rejected explicitly, never resolved by entity order or an arbitrary winner.</summary>
public static class MechanicalNetwork
{
    private readonly record struct Socket(MachinePart Part, string Port);
    private readonly record struct Edge(Socket Target, float Ratio);
    private sealed class Graph
    {
        public Dictionary<Socket, List<Edge>> Edges { get; } = new();
        public Dictionary<Socket, int> Incoming { get; } = new();
        public List<Socket> Order { get; } = new();
    }

    private static Graph Build(IEnumerable<MachinePart> parts, IEnumerable<ConnectionSpec> links)
    {
        var graph = new Graph();
        var byId = parts.ToDictionary(p => p.Uid, StringComparer.Ordinal);
        var ports = byId.Values.ToDictionary(p => p, p => p.ConnectionPorts.ToArray());
        foreach (var part in byId.Values)
        foreach (var port in ports[part])
            if (port.Domain == ConnectionDomain.Mechanical)
            {
                var socket = new Socket(part, port.Id);
                graph.Edges.Add(socket, new());
                graph.Incoming.Add(socket, 0);
            }

        void Add(Socket from, Socket to, float ratio)
        {
            if (!float.IsFinite(ratio) || ratio == 0)
                throw new ArgumentException("Mechanical ratios must be finite and non-zero.");
            if (++graph.Incoming[to] > 1)
                throw new ArgumentException("A mechanical socket accepts exactly one upstream drive.");
            graph.Edges[from].Add(new(to, ratio));
        }
        foreach (var part in byId.Values)
        {
            foreach (var route in part.MechanicalRoutes)
            {
                if (!ports[part].Any(p => p.Id == route.Input && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Input)
                    || !ports[part].Any(p => p.Id == route.Output && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Output))
                    throw new ArgumentException("Invalid internal mechanical route on " + part.Uid);
                Add(new(part, route.Input), new(part, route.Output), route.Ratio);
            }
            var sources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in part.MechanicalSources)
                if (!sources.Add(source.Output) || !float.IsFinite(source.RadiansPerSecond)
                    || !ports[part].Any(p => p.Id == source.Output && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Output)
                    || graph.Incoming[new(part, source.Output)] != 0)
                    throw new ArgumentException("Invalid mechanical source on " + part.Uid);
        }
        foreach (var link in links.Where(c => c.Type == ConnectionDomain.Mechanical))
        {
            if (!byId.TryGetValue(link.From, out var from) || !byId.TryGetValue(link.To, out var to)
                || !ConnectionRules.TryResolve(link, ports[from], ports[to], out _, out _))
                throw new ArgumentException("Invalid mechanical belt endpoints.");
            Add(new(from, link.FromPort!), new(to, link.ToPort!), 1);
        }
        var incoming = new Dictionary<Socket, int>(graph.Incoming);
        var pending = new Queue<Socket>(incoming.Where(p => p.Value == 0).Select(p => p.Key));
        while (pending.TryDequeue(out var socket))
        {
            graph.Order.Add(socket);
            foreach (var edge in graph.Edges[socket])
                if (--incoming[edge.Target] == 0) pending.Enqueue(edge.Target);
        }
        if (graph.Order.Count != incoming.Count)
            throw new ArgumentException("Mechanical drive loops are not supported.");
        return graph;
    }

    public static void Validate(IEnumerable<MachinePart> parts, IEnumerable<ConnectionSpec> links) => Build(parts, links);

    public static bool CanConnect(IEnumerable<MachinePart> parts, IEnumerable<ConnectionSpec> links)
    {
        try { Validate(parts, links); return true; }
        catch (ArgumentException) { return false; }
    }

    public static void Solve(MachineWorld world)
    {
        var graph = Build(world.Parts, world.Connections);
        foreach (var part in world.Parts)
        {
            part.ClearMechanicalDrive();
            foreach (var source in part.MechanicalSources)
                part.SetMechanicalSpeed(source.Output, source.RadiansPerSecond);
        }
        foreach (var socket in graph.Order)
        foreach (var edge in graph.Edges[socket])
        {
            var speed = socket.Part.MechanicalSpeed(socket.Port) * edge.Ratio;
            if (!float.IsFinite(speed)) throw new InvalidOperationException("Mechanical drive speed overflow.");
            edge.Target.Part.SetMechanicalSpeed(edge.Target.Port, speed);
        }
    }
}
