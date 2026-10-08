using Godot;
using System;

namespace CuriousContraptions;

public partial class BallPart : MachinePart
{
    protected override void Build()
    {
        var material = (Definition.Basketball ?? throw new ArgumentException("Canonical Basketball resource is required.")).Capture();
        var radius = (float)material.Radius.Value; // Explicit Godot mesh adapter.
        PickRadius = radius + .12f;
        PartArt.Sphere(Visual, radius, Definition.Color);
        var stripe = PartArt.Ring(Visual, radius * .99f, .014f, Definition.Color.Darkened(.4f));
        stripe.RotationDegrees = new(0, 0, 35);
    }
}
