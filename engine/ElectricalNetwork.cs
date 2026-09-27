using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Snapshot reachability of supply through closed contacts. Cables include return;
/// this is binary supply availability, not a voltage/current or battery-charge solver.</summary>
public static class ElectricalNetwork
{
    public static void Solve(MachineWorld world)
    {
        var edges = new Dictionary<(MachinePart Part, string Port), List<(MachinePart Part, string Port)>>();
        var ports = world.Parts.ToDictionary(p => p, p => p.ConnectionPorts.ToArray());
        var conjunctions = new Dictionary<MachinePart, ElectricalConjunction[]>();
        var pending = new Queue<(MachinePart Part, string Port)>();
        void Edge((MachinePart Part, string Port) from, (MachinePart Part, string Port) to)
        {
            if (!edges.TryGetValue(from, out var next)) edges[from] = next = new();
            next.Add(to);
        }
        foreach (var part in world.Parts)
        {
            part.ClearElectricalPower();
            conjunctions[part] = part.ElectricalConjunctions.ToArray();
            foreach (var rule in conjunctions[part])
            {
                bool HasPort(string id, PortDirection direction) => ports[part].Any(p =>
                    p.Id == id && p.Domain == ConnectionDomain.Electrical && p.Direction == direction);
                if (rule.First == rule.Second || !HasPort(rule.First, PortDirection.Input)
                    || !HasPort(rule.Second, PortDirection.Input) || !HasPort(rule.Output, PortDirection.Output))
                    throw new System.InvalidOperationException("Invalid electrical conjunction sockets.");
            }
            foreach (var port in ports[part])
                if (port.Domain == ConnectionDomain.Electrical && port.Direction == PortDirection.Output
                    && part.SuppliesElectricity(port.Id))
                    pending.Enqueue((part, port.Id));
            // Route openness is sampled before any power is assigned: no entity-order feedback.
            foreach (var route in part.ElectricalRoutes)
            {
                if (!ports[part].Any(p => p.Id == route.Input && p.Domain == ConnectionDomain.Electrical
                    && p.Direction == PortDirection.Input) ||
                    !ports[part].Any(p => p.Id == route.Output && p.Domain == ConnectionDomain.Electrical
                    && p.Direction == PortDirection.Output)) continue;
                Edge((part, route.Input), (part, route.Output));
            }
        }
        foreach (var link in world.Connections)
        {
            if (link.Type != ConnectionDomain.Electrical || !world.IsValidConnection(link)) continue;
            Edge((world.FindPart(link.From)!, link.FromPort!), (world.FindPart(link.To)!, link.ToPort!));
        }
        var reached = new HashSet<(MachinePart Part, string Port)>();
        while (pending.TryDequeue(out var socket))
        {
            if (!reached.Add(socket)) continue;
            // Monotone two-input reachability: no stale previous-tick power or artificial source.
            foreach (var rule in conjunctions[socket.Part])
                if (reached.Contains((socket.Part, rule.First)) && reached.Contains((socket.Part, rule.Second)))
                    pending.Enqueue((socket.Part, rule.Output));
            if (edges.TryGetValue(socket, out var next))
                foreach (var destination in next) pending.Enqueue(destination);
        }
        // Commit together; cycles have no power unless reachable from an actual source.
        foreach (var socket in reached)
            if (ports[socket.Part].Any(p => p.Id == socket.Port && p.Direction == PortDirection.Input))
                socket.Part.SupplyElectricalPower(socket.Port);
    }
}
