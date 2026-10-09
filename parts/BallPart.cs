using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Artwork only: the visible sphere is the declared collision radius of whichever ball kind the definition names.</summary>
public partial class BallPart : MachinePart
{
    protected override void Build()
    {
        var material = (Definition.Ball ?? throw new ArgumentException("Canonical ball material resource is required.")).Capture(Definition.WorkshopKind);
        var radius = (float)material.Radius.Value; // Explicit Godot mesh adapter.
        PickRadius = radius + .12f;
        PartArt.Sphere(Visual, radius, Definition.Color);
        var stripe = PartArt.Ring(Visual, radius * .99f, .014f, Definition.Color.Darkened(.4f));
        stripe.RotationDegrees = new(0, 0, 35);
    }
}
