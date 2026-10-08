using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>A declared attachment, not a scene-derived runtime position.
/// Resolve its pose through WorldGeometry with the owning MachineWorld.</summary>
public readonly record struct RopeSocket(MachinePart Part, ConnectionPort Port);

/// <summary>One unbranched rope, including any intervening fixed point guides.
/// Segment lengths are authored spool contributions; only their sum constrains a complete rope.</summary>
public sealed class RopePath
{
    // Stable for this captured route's lifetime, not a serialized or global identity.
    public JointSlot ConstraintSlot { get; }=new();
    public IReadOnlyList<RopeSocket> Sockets { get; }
    public float Length { get; }
    public RopePath(IEnumerable<RopeSocket> sockets,float length)
    {
        ArgumentNullException.ThrowIfNull(sockets);
        var captured=sockets.ToArray();
        if(captured.Length<2||!float.IsFinite(length)||length<=0)
            throw new ArgumentException("Rope path requires at least two sockets and finite positive length.");
        foreach(var socket in captured)
        {
            ArgumentNullException.ThrowIfNull(socket.Part);
            if(socket.Port.Domain!=ConnectionDomain.Rope||!socket.Port.LocalPosition.IsFinite())
                throw new ArgumentException("Rope path requires finite rope sockets.");
        }
        Sockets=Array.AsReadOnly(captured); Length=length;
    }
    public bool Complete => Sockets[0].Part.RopeAttachment != RopeAttachmentKind.Guide
        && Sockets[^1].Part.RopeAttachment != RopeAttachmentKind.Guide;
    public double CurrentLength(MachineWorld world)=>GuideDistances(world)[^1];
    public RopeState State(MachineWorld world)=>!Complete?RopeState.Open:
        CurrentLength(world)<Length-.001?RopeState.Slack:RopeState.Taut;

    public double[] GuideDistances(MachineWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var positions=Sockets.Select(socket=>
            WorldGeometry.CaptureSpatialState(world,new(socket.Part,MachinePart.RootBody)).Pose
                .TransformPoint(SceneGeometryAdapter.CaptureVector(socket.Port.LocalPosition))).ToArray();
        var result=new double[positions.Length];
        for(var i=1;i<positions.Length;i++) result[i]=result[i-1]+(positions[i]-positions[i-1]).Length;
        return result;
    }
    public void AnimateGuides(MachineWorld world,double[] before)
    {
        ArgumentNullException.ThrowIfNull(before);
        if(before.Length!=Sockets.Count||before.Any(value=>!double.IsFinite(value)||value<0))
            throw new ArgumentException("Guide samples must match the route and contain finite nonnegative distances.",nameof(before));
        var after=GuideDistances(world);
        if(!Complete||after[^1]<Length-.001) return;
        for(var i=1;i<Sockets.Count-1;i++) Sockets[i].Part.AdvanceRope(after[i]-before[i]);
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

    public static IReadOnlyList<SceneRopeJoint> DeclarePhysics(IEnumerable<RopePath> paths,
        IReadOnlyList<SceneWorldBodyDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(paths); ArgumentNullException.ThrowIfNull(declarations);
        var bodies=declarations.ToDictionary(d=>new SceneBodyKey(d.Geometry.Owner,d.Geometry.Slot));
        var result=new List<SceneRopeJoint>();
        foreach(var path in paths)
        {
            ArgumentNullException.ThrowIfNull(path);
            // An untied guide end cannot transmit tension. This is authored topology,
            // not a failed constraint silently replaced by a different solver.
            if(!path.Complete) continue;
            var anchors=path.Sockets.Select(s=>new SceneRopeAnchor(new(s.Part,MachinePart.RootBody),
                SceneGeometryAdapter.CaptureVector(s.Port.LocalPosition))).ToArray();
            var routeBodies=anchors.Select(a=>bodies.TryGetValue(a.Body,out var body)?body:
                throw new ArgumentException("Rope attachment has no captured body.")).ToArray();
            if(!routeBodies.Any(b=>b.Dynamics.Motion==PhysicsMotionType.Dynamic))
            {
                if(routeBodies.Any(b=>b.Dynamics.Motion!=PhysicsMotionType.Static))
                    throw new ArgumentException("A prescribed moving rope requires a dynamic participant.");
                double length=0;
                for(var i=1;i<anchors.Length;i++)
                    length+=(routeBodies[i].Geometry.Pose.TransformPoint(anchors[i].LocalPosition)-
                        routeBodies[i-1].Geometry.Pose.TransformPoint(anchors[i-1].LocalPosition)).Length;
                if(length>path.Length+ConvexDistance.DefaultTolerance)
                    throw new ArgumentException("A fixed rope route exceeds its authored length.");
                continue; // Satisfied fixed geometry has no dynamic equation.
            }
            result.Add(new(new(path.Sockets[0].Part,path.ConstraintSlot),anchors,path.Length,ConnectedBodyCollision.Enabled));
        }
        return result;
    }

    public static bool CanConnect(IEnumerable<MachinePart> parts, IEnumerable<ConnectionSpec> links)
    {
        try { Build(parts, links); return true; }
        catch (ArgumentException) { return false; }
    }
}
