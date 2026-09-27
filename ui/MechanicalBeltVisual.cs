using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Closed belt artwork tracks both moving sockets, including placement assistance.
/// Gold witness marks follow the signed shaft speed; no new controls or floating labels.</summary>
public partial class MechanicalBeltVisual : Node3D
{
    public MachineWorld World { get; set; } = null!;
    public MachinePart Source { get; set; } = null!;
    public MachinePart Target { get; set; } = null!;
    public ConnectionPort Output { get; set; }
    public ConnectionPort Input { get; set; }
    private readonly List<MeshInstance3D> _strands = new();
    private MeshInstance3D _forward = null!;
    private MeshInstance3D _return = null!;
    private float _phase;

    public override void _Ready()
    {
        for (var i = 0; i < 4; i++)
            _strands.Add(PartArt.Line(this, Vector3.Zero, Vector3.Up, new("#293954"), .035f));
        _forward = PartArt.Sphere(this, .055f, new("#f7cb52"));
        _return = PartArt.Sphere(this, .055f, new("#f7cb52"));
        _Process(0);
    }
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Source) || !IsInstanceValid(Target)) return; // Removed with its endpoints.
        var a = Source.Transform * Output.LocalPosition;
        var b = Target.Transform * Input.LocalPosition;
        var direction = b - a;
        var across = direction.Cross(Vector3.Forward);
        if (across.LengthSquared() < .001f) across = direction.Cross(Vector3.Up);
        if (direction.LengthSquared() < .001f) across = Source.Basis.Y;
        across = across.Normalized() * .12f;
        Segment(_strands[0], a + across, b + across);
        Segment(_strands[1], b - across, a - across);
        Segment(_strands[2], a - across, a + across);
        Segment(_strands[3], b + across, b - across);
        if (World.Running)
            _phase = Mathf.PosMod(_phase + Source.MechanicalSpeed(Output.Id) * (float)delta * .16f, 1);
        _forward.Position = LoopPoint(_phase, a, b, across);
        _return.Position = LoopPoint(Mathf.PosMod(_phase + .5f, 1), a, b, across);
    }
    private static Vector3 LoopPoint(float phase, Vector3 a, Vector3 b, Vector3 across)
    {
        // Traverse the closed loop continuously, including the short end turns.
        var length = a.DistanceTo(b);
        var endLength = across.Length() * 2;
        var distance = phase * 2 * (length + endLength);
        if (distance < length) return (a + across).Lerp(b + across, distance / length);
        distance -= length;
        if (distance < endLength) return (b + across).Lerp(b - across, distance / endLength);
        distance -= endLength;
        if (distance < length) return (b - across).Lerp(a - across, distance / length);
        return (a - across).Lerp(a + across, (distance - length) / endLength);
    }
    private static void Segment(MeshInstance3D strand, Vector3 a, Vector3 b)
    {
        var span = b - a;
        strand.Position = (a + b) * .5f;
        ((CylinderMesh)strand.Mesh).Height = Mathf.Max(.001f, span.Length());
        if (span.LengthSquared() > .00001f)
            strand.Quaternion = new Quaternion(Vector3.Up, span.Normalized());
    }
}
