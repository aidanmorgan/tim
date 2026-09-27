using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Single warm rope with visible knots. Slack is shared across spans.
/// The curve is a length-matched display approximation, not rope self-collision.</summary>
public partial class RopeVisual : Node3D
{
    private const int Steps = 12;
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
        foreach (var socket in Path.Sockets)
            _knots.Add(PartArt.Sphere(this, .07f, new("#f7cb52")));
        _Process(0);
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
        var slack = Mathf.Max(0, Path.Length - Path.CurrentLength) / _spans.Count;
        for (var span = 0; span < _spans.Count; span++)
        {
            var a = Path.Sockets[span].Position;
            var b = Path.Sockets[span + 1].Position;
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
                var piece = _spans[span][i];
                piece.Visible = Path.Complete || i % 2 == 0; // Dashed threading preview until both rope ends are tied.
                var direction = end - start;
                piece.Position = (start + end) * .5f;
                ((CylinderMesh)piece.Mesh).Height = Mathf.Max(.001f, direction.Length());
                if (direction.LengthSquared() > .000001f)
                    piece.Quaternion = new Quaternion(Vector3.Up, direction.Normalized());
            }
        }
        for (var i = 0; i < _knots.Count; i++) _knots[i].Position = Path.Sockets[i].Position;
    }
}
