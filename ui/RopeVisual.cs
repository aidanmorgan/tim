using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Single warm rope with visible knots. Slack is shared across spans.
/// Slack is length-matched on the free spans; rim arcs are presentation only.
/// The solver still constrains point-guide lengths, not these finite-radius paths.</summary>
public partial class RopeVisual : Node3D
{
    private const int Steps = 12;
    private const int ArcSteps = 32;
    private readonly Dictionary<int, (RopeWinding Winding, MeshInstance3D[] Pieces)> _wraps = new();
    public required RopePath Path { get; init; }
    public required MachineWorld World { get; init; }
    private readonly List<MeshInstance3D[]> _spans = new();
    private readonly List<MeshInstance3D> _knots = new();
    public override void _Ready()
    {
        System.ArgumentNullException.ThrowIfNull(Path);
        System.ArgumentNullException.ThrowIfNull(World);
        var poses=CaptureSocketPoses();
        var points=SocketPoints(poses);
        for (var span = 1; span < Path.Sockets.Count; span++)
        {
            var pieces = new MeshInstance3D[Steps];
            for (var i = 0; i < Steps; i++)
                pieces[i] = PartArt.Line(this, Vector3.Zero, Vector3.Up, new("#b77c42"), .035f);
            _spans.Add(pieces);
        }
        for (var i = 1; i < Path.Sockets.Count - 1; i++)
        {
            if (Path.Sockets[i].Part.RopeAttachment != RopeAttachmentKind.Guide) continue;
            var pieces = new MeshInstance3D[ArcSteps];
            for (var j = 0; j < ArcSteps; j++)
                pieces[j] = PartArt.Line(this, Vector3.Zero, Vector3.Up, new("#b77c42"), .035f);
            var winding = PulleyRopeRoute.Choose(poses[i], points[i - 1], points[i + 1]);
            _wraps.Add(i, (winding, pieces));
        }
        foreach (var socket in Path.Sockets)
            _knots.Add(PartArt.Sphere(this, .07f, new("#f7cb52")));
        _Process(0);
    }
    private static void SetSegment(MeshInstance3D piece, Vector3 start, Vector3 end, bool visible)
    {
        piece.Visible = visible;
        var direction = end - start;
        piece.Position = (start + end) * .5f;
        ((CylinderMesh)piece.Mesh).Height = Mathf.Max(.001f, direction.Length());
        if (direction.LengthSquared() > .000001f)
            piece.Quaternion = new Quaternion(Vector3.Up, direction.Normalized());
    }
    private static Vector3 Point(Vector3 a, Vector3 b, float t, float sag) =>
        a.Lerp(b, t) + Vector3.Down * (4 * t * (1 - t) * sag);
    private static float CurveLength(Vector3 a, Vector3 b, float sag)
    {
        var length = 0f;
        var previous = a;
        for (var i = 1; i <= Steps; i++)
        {
            var point = Point(a, b, i / (float)Steps, sag);
            length += previous.DistanceTo(point);
            previous = point;
        }
        return length;
    }
    private Transform3D[] CaptureSocketPoses()
    {
        if(!World.HasPhysicsState)
            return Path.Sockets.Select(socket=>
                WorldGeometry.CaptureSpatialState(World,new(socket.Part,MachinePart.RootBody)).Pose.ToScene()).ToArray();
        using var committed=World.ReadCommittedPoses();
        var poses=new Transform3D[Path.Sockets.Count];
        for(var i=0;i<poses.Length;i++)
        {
            var body=World.PhysicsAssembly.Body(new(Path.Sockets[i].Part,MachinePart.RootBody)).Id;
            poses[i]=committed.SampleAcceptedPose(body,World.DisplaySimulationTime).ToScene();
        }
        return poses;
    }
    private Vector3[] SocketPoints(Transform3D[] poses)=>Path.Sockets
        .Select((socket,index)=>poses[index]*socket.Port.LocalPosition).ToArray();
    public override void _Process(double delta)
    {
        if (Path.Sockets.Any(s => !IsInstanceValid(s.Part))) return;
        var poses=CaptureSocketPoses();
        var points=SocketPoints(poses);
        var entries = points.ToArray();
        var exits = entries.ToArray();
        var routes = new Dictionary<int, PulleyRopeRoute>();
        // Iteration aligns adjacent wheel tangencies instead of aiming at their hubs.
        for (var pass = 0; pass < 8; pass++)
        foreach (var (index, wrap) in _wraps)
        {
            var route = PulleyRopeRoute.Create(poses[index],
                exits[index - 1], entries[index + 1], wrap.Winding);
            entries[index] = route.Point(0);
            exits[index] = route.Point(1);
            routes[index] = route;
        }
        foreach (var (index, route) in routes)
        for (var i = 0; i < ArcSteps; i++)
            SetSegment(_wraps[index].Pieces[i], route.Point(i / (float)ArcSteps),
                route.Point((i + 1) / (float)ArcSteps), Path.Complete || i % 2 == 0);
        var guideLength=0f;
        for(var i=1;i<points.Length;i++)guideLength+=points[i-1].DistanceTo(points[i]);
        var slack = Mathf.Max(0, Path.Length - guideLength) / _spans.Count;
        for (var span = 0; span < _spans.Count; span++)
        {
            var a = exits[span];
            var b = entries[span + 1];
            var target = a.DistanceTo(b) + slack;
            var low = 0f;
            var high = target;
            for (var pass = 0; pass < 16; pass++)
            {
                var mid = (low + high) * .5f;
                if (CurveLength(a, b, mid) < target) low = mid;
                else high = mid;
            }
            var sag = slack > .0001f ? (low + high) * .5f : 0;
            for (var i = 0; i < Steps; i++)
            {
                var start = Point(a, b, i / (float)Steps, sag);
                var end = Point(a, b, (i + 1) / (float)Steps, sag);
                SetSegment(_spans[span][i], start, end, Path.Complete || i % 2 == 0);
            }
        }
        for (var i = 0; i < _knots.Count; i++)
        {
            _knots[i].Visible = Path.Sockets[i].Part.RopeAttachment != RopeAttachmentKind.Guide;
            _knots[i].Position = points[i];
        }
    }
}
