using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class RopeAnchorPart : MachinePart
{
    public override RopeAttachmentKind RopeAttachment => RopeAttachmentKind.Anchor;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketIds.Tie, ConnectionDomain.Rope, PortDirection.Bidirectional, new(0, 0, .18f))
    ];
    protected override void Build()
    {
        PickRadius = .55f;
        AddBox(new(0, 0, -.12f), new(.55f, .55f, .16f), new("#fff8e9"));
        var ring = PartArt.Ring(Visual, .2f, .055f, Definition.Color, new(0, 0, .16f));
        ring.RotationDegrees = new(90, 0, 0);
    }
}
