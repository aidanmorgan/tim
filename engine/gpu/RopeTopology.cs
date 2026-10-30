using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Main-owned graph admission using declared roles, independent of package identities.</summary>
public static class RopeTopology
{
    public readonly record struct Endpoint(GpuBodyId Body, WorkshopSocket Socket) : IComparable<Endpoint>
    {
        public static bool operator <(Endpoint left, Endpoint right) => left.CompareTo(right) < 0;
        public static bool operator >(Endpoint left, Endpoint right) => left.CompareTo(right) > 0;
        public static bool operator <=(Endpoint left, Endpoint right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Endpoint left, Endpoint right) => left.CompareTo(right) >= 0;
        public int CompareTo(Endpoint other)
        {
            var body = Body.Value.CompareTo(other.Body.Value);
            return body == 0 ? Socket.CompareTo(other.Socket) : body;
        }
    }
    public sealed record Route(Endpoint[] Attachments, WorkshopConnection[] Spans, bool Complete)
    {
        public float MaximumLength => Spans.Sum(span => span.RopeLength!.Value);
    }

    public static Route[] Compile(IEnumerable<WorkshopConnection> connections, Func<Endpoint, RopeAttachmentRole> role)
    {
        var edges = connections.Where(e => e.Domain == WorkshopConnectionDomain.Rope).ToArray();
        var adjacent = new SortedDictionary<Endpoint, List<WorkshopConnection>>();
        var seen = new HashSet<(Endpoint, Endpoint)>();
        foreach (var edge in edges)
        {
            var a = new Endpoint(edge.Source, edge.Output);
            var b = new Endpoint(edge.Target, edge.Input);
            if (a.Body.Value == 0 || b.Body.Value == 0 || a.Body == b.Body ||
                edge.Output is not (WorkshopSocket.Tie or WorkshopSocket.TieA or WorkshopSocket.TieB) ||
                edge.Input is not (WorkshopSocket.Tie or WorkshopSocket.TieA or WorkshopSocket.TieB) ||
                edge.RopeLength is not { } length || !float.IsFinite(length) || length < .05f || length > 200f ||
                !seen.Add(a.CompareTo(b) < 0 ? (a, b) : (b, a)))
                Refuse("Invalid or duplicate rope span.");
            Add(a, edge); Add(b, edge);
        }
        foreach (var (endpoint, links) in adjacent)
        {
            var declared = role(endpoint);
            if (!Enum.IsDefined(declared) || declared == RopeAttachmentRole.None ||
                links.Count > (declared == RopeAttachmentRole.Guide ? 2 : 1))
                Refuse("Ropes cannot branch or attach to an undeclared endpoint.");
        }
        var visited = new HashSet<WorkshopConnection>();
        var routes = new List<Route>();
        foreach (var (endpoint, links) in adjacent)
        {
            if (links.Count != 1 || visited.Contains(links[0])) continue;
            var points = new List<Endpoint> { endpoint };
            var spans = new List<WorkshopConnection>();
            var current = endpoint;
            while (true)
            {
                var next = adjacent[current].Where(e => !visited.Contains(e)).ToArray();
                if (next.Length == 0) break;
                var edge = next[0]; visited.Add(edge); spans.Add(edge);
                current = edge.Source == current.Body && edge.Output == current.Socket
                    ? new(edge.Target, edge.Input) : new(edge.Source, edge.Output);
                points.Add(current);
            }
            var complete = role(points[0]) != RopeAttachmentRole.Guide && role(points[^1]) != RopeAttachmentRole.Guide;
            routes.Add(new(points.ToArray(), spans.ToArray(), complete));
        }
        if (visited.Count != edges.Length) Refuse("Rope cycles are unsupported.");
        return routes.ToArray();

        void Add(Endpoint endpoint, WorkshopConnection edge)
        {
            if (!adjacent.TryGetValue(endpoint, out var links)) adjacent.Add(endpoint, links = []);
            links.Add(edge);
        }
    }
    private static void Refuse(string reason) =>
        throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, reason);
}
