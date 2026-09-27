using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Regulated shaft-speed transmission with finite per-substep work allowances.
/// Torque is shared equally between live branches and local consumers, transformed
/// inversely to each speed ratio. Unused work expires; it is not duplicated or stored.
/// This is not shaft inertia, elastic-belt dynamics or load-dependent motor speed.
/// One belt per input; loops and competing drives are rejected explicitly.</summary>
public static class MechanicalNetwork
{
    private readonly record struct Socket(MachinePart Part, SocketId Port);
    private readonly record struct Edge(Socket Target, float Ratio, bool Enabled);
    private sealed class Graph
    {
        public Dictionary<Socket, List<Edge>> Edges { get; } = new();
        public Dictionary<Socket, int> Incoming { get; } = new();
        public List<Socket> Order { get; } = new();
        public HashSet<Socket> Loads { get; } = new();
        public Dictionary<Socket, MechanicalSource> Sources { get; } = new();
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

        void Add(Socket from, Socket to, float ratio, bool enabled)
        {
            if (!float.IsFinite(ratio) || ratio == 0)
                throw new ArgumentException("Mechanical ratios must be finite and non-zero.");
            if (++graph.Incoming[to] > 1)
                throw new ArgumentException("A mechanical socket accepts exactly one upstream drive.");
            graph.Edges[from].Add(new(to, ratio, enabled));
        }
        foreach (var part in byId.Values)
        {
            foreach (var route in part.MechanicalRoutes)
            {
                if (!ports[part].Any(p => p.Id == route.Input && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Input)
                    || !ports[part].Any(p => p.Id == route.Output && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Output))
                    throw new ArgumentException("Invalid internal mechanical route on " + part.Uid);
                Add(new(part, route.Input), new(part, route.Output), route.Ratio, route.Enabled);
            }
            foreach (var input in part.MechanicalLoads)
                if (!ports[part].Any(p => p.Id == input && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Input)
                    || !graph.Loads.Add(new(part, input)))
                    throw new ArgumentException("Invalid mechanical consumer on " + part.Uid);
            var sources = new HashSet<SocketId>();
            foreach (var source in part.MechanicalSources)
            {
                if (!sources.Add(source.Output) || !float.IsFinite(source.RadiansPerSecond)
                    || !float.IsFinite(source.TorqueLimit) || source.TorqueLimit < 0
                    || !ports[part].Any(p => p.Id == source.Output && p.Domain == ConnectionDomain.Mechanical && p.Direction == PortDirection.Output)
                    || graph.Incoming[new(part, source.Output)] != 0)
                    throw new ArgumentException("Invalid mechanical source on " + part.Uid);
                graph.Sources.Add(new(part, source.Output), source);
            }
        }
        foreach (var link in links.Where(c => c.Type == ConnectionDomain.Mechanical))
        {
            if (!byId.TryGetValue(link.From, out var from) || !byId.TryGetValue(link.To, out var to)
                || !ConnectionRules.TryResolve(link, ports[from], ports[to], out _, out _))
                throw new ArgumentException("Invalid mechanical belt endpoints.");
            Add(new(from, link.FromPort!.Value), new(to, link.ToPort!.Value), 1, true);
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

    public static void Solve(MachineWorld world, float delta)
    {
        if (!float.IsFinite(delta) || delta <= 0) throw new ArgumentOutOfRangeException(nameof(delta));
        var graph = Build(world.Parts, world.Connections);
        var hasLoad = new Dictionary<Socket, bool>();
        foreach (var socket in graph.Order.AsEnumerable().Reverse())
            hasLoad[socket] = graph.Loads.Contains(socket) ||
                graph.Edges[socket].Any(e => e.Enabled && hasLoad[e.Target]);

        var drive = graph.Order.ToDictionary(s => s, _ => (Speed: 0f, Torque: 0d));
        var allowances = new Dictionary<Socket, (float Speed, double Torque, double Work)>();
        foreach (var (socket, source) in graph.Sources)
            drive[socket] = (source.RadiansPerSecond, source.TorqueLimit);
        foreach (var socket in graph.Order)
        {
            var input = drive[socket];
            var local = graph.Loads.Contains(socket);
            var branches = graph.Edges[socket].Count(e => e.Enabled && hasLoad[e.Target]);
            var recipients = branches + (local ? 1 : 0);
            var share = recipients == 0 ? 0 : input.Torque / recipients;
            // Round down the division if equal branches would exceed the input allowance.
            if (share * recipients > input.Torque) share = Math.BitDecrement(share);
            var work = local ? Math.Abs((double)input.Speed) * share * delta : 0;
            if (!double.IsFinite(work)) throw new InvalidOperationException("Mechanical work overflow.");
            allowances.Add(socket, (input.Speed, local ? share : input.Torque, work));
            foreach (var edge in graph.Edges[socket])
            {
                var speed = edge.Enabled ? input.Speed * edge.Ratio : 0;
                var torque = edge.Enabled && hasLoad[edge.Target] ? share / Math.Abs((double)edge.Ratio) : 0;
                if (!float.IsFinite(speed) || !double.IsFinite(torque))
                    throw new InvalidOperationException("Mechanical drive overflow.");
                // Float speed multiplication can round upwards. Never increase transmitted power.
                var power = Math.Abs((double)input.Speed) * share;
                if (speed != 0 && Math.Abs((double)speed) * torque > power)
                    torque = Math.BitDecrement(power / Math.Abs((double)speed));
                drive[edge.Target] = (speed, Math.Max(0, torque));
            }
        }
        // Commit only after all validation/calculation succeeds; no partial clearing on failure.
        foreach (var part in world.Parts) part.ClearMechanicalDrive();
        foreach (var (socket, value) in allowances)
            socket.Part.SetMechanicalDrive(socket.Port, value.Speed, value.Torque, value.Work);
    }
}
