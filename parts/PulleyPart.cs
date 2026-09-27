using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Fixed, frictionless point-guide pulley; no moving-block ratio or wheel inertia.</summary>
public partial class PulleyPart : MachinePart
{
    public const float WheelRadius = RopeGeometry.PulleyRadius;
    private Node3D _wheel = null!;
    public float WheelAngle { get; private set; }
    public override RopeAttachmentKind RopeAttachment => RopeAttachmentKind.Guide;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.Tie, ConnectionDomain.Rope, PortDirection.Bidirectional, new(0, WheelRadius, RopeGeometry.PulleySocketDepth))
    ];
    protected override void Build()
    {
        PickRadius = .65f;
        AddBox(new(0, .55f, -.1f), new(.5f, .15f, .4f), new("#293954"));
        PartArt.Box(Visual, new(.12f, .55f, .12f), new("#fff8e9"), new(0, .3f, -.13f));
        Spheres.Add(new(Vector3.Zero, WheelRadius));
        _wheel = new Node3D();
        Visual.AddChild(_wheel);
        var wheel = PartArt.Cylinder(_wheel, WheelRadius, .17f, Definition.Color);
        wheel.RotationDegrees = new(90, 0, 0);
        var rim = PartArt.Ring(_wheel, WheelRadius, .035f, new("#fff8e9"), new(0, 0, .1f));
        rim.RotationDegrees = new(90, 0, 0);
        PartArt.Box(_wheel, new(.57f, .065f, .04f), new("#293954"), new(0, 0, .12f));
        PartArt.Sphere(_wheel, .06f, new("#f7cb52"), new(.27f, 0, .15f));
    }
    public override void AdvanceRope(float distance)
    {
        WheelAngle = Mathf.PosMod(WheelAngle + distance / WheelRadius, Mathf.Tau);
        _wheel.Rotation = new(0, 0, WheelAngle);
    }
}
