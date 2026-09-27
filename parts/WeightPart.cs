using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>A translating load with a spherical collision envelope and a rope eye.
/// Locked means fixed during editing, not immovable during simulation.</summary>
public partial class WeightPart : MachinePart
{
    public override RopeAttachmentKind RopeAttachment => RopeAttachmentKind.Load;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.Tie, ConnectionDomain.Rope, PortDirection.Bidirectional,
            new(0, RopeGeometry.WeightTieHeight(Properties[WeightParameters.Mass]), 0))
    ];
    public override void ValidateParameters()
    {
        var mass = Properties[WeightParameters.Mass];
        if (!float.IsFinite(mass) || mass < .25f || mass > 8)
            throw new ArgumentException("Weight mass must be between 0.25 and 8.");
    }
    protected override void Build()
    {
        Mass = Properties[WeightParameters.Mass];
        Dynamic = true;
        Radius = RopeGeometry.WeightRadius(Mass);
        Bounce = .08f;
        Drag = 0;
        PickRadius = Radius + .22f;
        PartArt.Cylinder(Visual, Radius * .85f, Radius * 1.35f, Definition.Color);
        PartArt.Cylinder(Visual, Radius * .87f, Radius * .12f, new("#fff8e9"), new(0, -Radius * .55f, 0));
        var eye = PartArt.Ring(Visual, .14f, .035f, new("#f7cb52"), new(0, Radius * .72f, 0));
        eye.RotationDegrees = new(90, 0, 0);
        // Larger physical size and countable bands communicate heavier authored loads.
        var bands = Mathf.CeilToInt(Mass);
        for (var i = 0; i < bands; i++)
        {
            PartArt.Ring(Visual, Radius * .86f, .015f, new("#fff8e9"),
                new(0, -Radius * .35f + i * Radius * .7f / bands, 0));
        }
    }
}
