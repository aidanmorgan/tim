using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct RopeSocket(MachinePart Part, ConnectionPort Port)
{
    public Vector3 Position => Part.Transform * Port.LocalPosition;
}

/// <summary>One unbranched rope, including any intervening fixed point guides.
/// Segment lengths are authored spool contributions; only their sum constrains a complete rope.</summary>
public sealed class RopePath(List<RopeSocket> sockets, float length)
{
    public IReadOnlyList<RopeSocket> Sockets { get; } = sockets;
    public float Length { get; } = length;
    public bool Complete => Sockets[0].Part.RopeAttachment != RopeAttachmentKind.Guide
        && Sockets[^1].Part.RopeAttachment != RopeAttachmentKind.Guide;
    public float CurrentLength
    {
        get
        {
            var total = 0f;
            for (var i = 1; i < Sockets.Count; i++) total += Sockets[i - 1].Position.DistanceTo(Sockets[i].Position);
            return total;
        }
    }
    public RopeState State => !Complete ? RopeState.Open
        : CurrentLength < Length - .001f ? RopeState.Slack : RopeState.Taut;

    private (Vector3 A, Vector3 B, float Wa, float Wb, float Effective) Gradient()
    {
        var a = (Sockets[0].Position - Sockets[1].Position).Normalized();
        var b = (Sockets[^1].Position - Sockets[^2].Position).Normalized();
        var wa = Sockets[0].Part.RopeAttachment == RopeAttachmentKind.Load ? 1 / Sockets[0].Part.Mass : 0;
        var wb = Sockets[^1].Part.RopeAttachment == RopeAttachmentKind.Load ? 1 / Sockets[^1].Part.Mass : 0;
        return (a, b, wa, wb, wa * a.LengthSquared() + wb * b.LengthSquared());
    }
    public void SolveVelocity(float delta)
    {
        if (!Complete) return; // Untied ends cannot carry tension.
        var (a, b, wa, wb, effective) = Gradient();
        if (effective < .000001f) return; // Fixed endpoints or coincident gradients have no movable degree of freedom.
        var first = Sockets[0].Part;
        var last = Sockets[^1].Part;
        var rate = a.Dot(first.Velocity) + b.Dot(last.Velocity);
        var allowedRate = Mathf.Max(0, Length - CurrentLength) / delta;
        if (rate <= allowedRate) return; // A rope cannot push or resist shortening.
        var impulse = (rate - allowedRate) / effective;
        first.Velocity -= a * (impulse * wa);
        last.Velocity -= b * (impulse * wb);
    }
    public void SolvePosition()
    {
        if (!Complete) return;
        var excess = CurrentLength - Length;
        if (excess <= 0) return;
        var (a, b, wa, wb, effective) = Gradient();
        if (effective < .000001f) return;
        var correction = excess / effective;
        Sockets[0].Part.Position -= a * (correction * wa);
        Sockets[^1].Part.Position -= b * (correction * wb);
    }
    public float[] GuideDistances()
    {
        var distance = 0f;
        var result = new float[Sockets.Count];
        for (var i = 1; i < Sockets.Count; i++)
        {
            distance += Sockets[i - 1].Position.DistanceTo(Sockets[i].Position);
            result[i] = distance;
        }
        return result;
    }
    public void AnimateGuides(float[] before)
    {
        if (State != RopeState.Taut) return;
        var after = GuideDistances();
        for (var i = 1; i < Sockets.Count - 1; i++)
            Sockets[i].Part.AdvanceRope(after[i] - before[i]);
    }
}

public static class RopeNetwork
{
    private readonly record struct Span(RopeSocket Other, float Length);

    public static List<RopePath> Build(IEnumerable<MachinePart> parts, IEnumerable<ConnectionSpec> links)
    {
        var byId = parts.ToDictionary(p => p.Uid, StringComparer.Ordinal);
        var edges = new Dictionary<RopeSocket, List<Span>>();
        void Attach(RopeSocket at, RopeSocket other, float length)
        {
            if (at.Part.RopeAttachment == RopeAttachmentKind.None)
                throw new ArgumentException("Part has no rope attachment behaviour: " + at.Part.Uid);
            if (!edges.TryGetValue(at, out var next)) edges[at] = next = new();
            if (next.Any(e => e.Other == other)) throw new ArgumentException("Duplicate rope span.");
            next.Add(new(other, length));
            var capacity = at.Part.RopeAttachment == RopeAttachmentKind.Guide ? 2 : 1;
            if (next.Count > capacity) throw new ArgumentException("Ropes cannot branch; loads and anchors accept one end.");
        }
        foreach (var link in links.Where(c => c.Type == ConnectionDomain.Rope))
        {
            if (!byId.TryGetValue(link.From, out var from) || !byId.TryGetValue(link.To, out var to)
                || !ConnectionRules.TryResolve(link, from.ConnectionPorts, to.ConnectionPorts, out var a, out var b))
                throw new ArgumentException("Rope links require explicit matching sockets and a finite positive length.");
            var source = new RopeSocket(from, a);
            var target = new RopeSocket(to, b);
            Attach(source, target, link.RopeLength!.Value);
            Attach(target, source, link.RopeLength.Value);
        }
        var paths = new List<RopePath>();
        var seen = new HashSet<RopeSocket>();
        // Canonical endpoint order keeps simulation and pulley rotation independent of link direction/order.
        foreach (var endpoint in edges.Keys.Where(p => edges[p].Count == 1)
            .OrderBy(p => p.Part.Uid, StringComparer.Ordinal).ThenBy(p => p.Port.Id))
        {
            if (seen.Contains(endpoint)) continue;
            var path = new List<RopeSocket>();
            var length = 0f;
            var current = endpoint;
            RopeSocket? previous = null;
            while (true)
            {
                if (!seen.Add(current)) throw new ArgumentException("Rope routing contains a loop.");
                path.Add(current);
                var next = edges[current].Where(e => previous == null || e.Other != previous.Value).ToArray();
                if (next.Length == 0) break;
                length += next[0].Length;
                previous = current;
                current = next[0].Other;
            }
            paths.Add(new(path, length));
        }
        if (seen.Count != edges.Count) throw new ArgumentException("Closed rope loops are not supported.");
        return paths;
    }

    public static bool CanConnect(IEnumerable<MachinePart> parts, IEnumerable<ConnectionSpec> links)
    {
        try { Build(parts, links); return true; }
        catch (ArgumentException) { return false; }
    }
}
