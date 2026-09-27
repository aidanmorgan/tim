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
    public RopePath Path { get; set; } = null!;
    private readonly List<MeshInstance3D[]> _spans = new();
    private readonly List<MeshInstance3D> _knots = new();
    public override void _Ready()
    {
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
            var winding = PulleyRopeRoute.Choose(Path.Sockets[i].Part.Transform,
                Path.Sockets[i - 1].Position, Path.Sockets[i + 1].Position);
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
    public override void _Process(double delta)
    {
        if (Path.Sockets.Any(s => !IsInstanceValid(s.Part))) return;
        var entries = Path.Sockets.Select(s => s.Position).ToArray();
        var exits = entries.ToArray();
        var routes = new Dictionary<int, PulleyRopeRoute>();
        // Iteration aligns adjacent wheel tangencies instead of aiming at their hubs.
        for (var pass = 0; pass < 8; pass++)
        foreach (var (index, wrap) in _wraps)
        {
            var route = PulleyRopeRoute.Create(Path.Sockets[index].Part.Transform,
                exits[index - 1], entries[index + 1], wrap.Winding);
            entries[index] = route.Point(0);
            exits[index] = route.Point(1);
            routes[index] = route;
        }
        foreach (var (index, route) in routes)
        for (var i = 0; i < ArcSteps; i++)
            SetSegment(_wraps[index].Pieces[i], route.Point(i / (float)ArcSteps),
                route.Point((i + 1) / (float)ArcSteps), Path.Complete || i % 2 == 0);
        var slack = Mathf.Max(0, Path.Length - Path.CurrentLength) / _spans.Count;
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
            _knots[i].Position = Path.Sockets[i].Position;
        }
    }
}
