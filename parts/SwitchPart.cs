using Godot;
using System;
using CuriousContraptions.Gpu;
namespace CuriousContraptions;

/// <summary>Artwork and immutable visual bindings; shared GPU declarations own both colliders.</summary>
public partial class SwitchPart : MachinePart
{
    protected override void Build()
    {
        PickRadius = .65f;
        PartArt.Sphere(Visual, .08f, new("#293954"), new(-.58f, 0, 0));
        PartArt.Sphere(Visual, .08f, new("#293954"), new(.58f, 0, 0));
        PartArt.Box(Visual, new(1.1f, .25f, 1), new("#2c3a4c"), new(0, -.15f, 0));
        var button = PartArt.Cylinder(Visual, .38f, .18f, Definition.Color, new(0, .06f, 0));
        PartArt.Sphere(Visual, .09f, new("#ffd899"), new(.45f, 0, .4f));
        var pressed = new Color("#bff5b0");
        var curve = CosmeticCurves.ImpactSwitch;
        BindVisual(curve, button, WorkshopVisualProperty.LocalY, (Half).06, (Half)(-.02));
        BindVisual(curve, button, WorkshopVisualProperty.AlbedoRed, (Half)Definition.Color.R, (Half)pressed.R);
        BindVisual(curve, button, WorkshopVisualProperty.AlbedoGreen, (Half)Definition.Color.G, (Half)pressed.G);
        BindVisual(curve, button, WorkshopVisualProperty.AlbedoBlue, (Half)Definition.Color.B, (Half)pressed.B);
    }
}
