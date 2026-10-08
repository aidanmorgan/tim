using Godot;
using System;
using CuriousContraptions.Gpu;
namespace CuriousContraptions;

/// <summary>Signal artwork with shared activation-driven colour and emission bindings.</summary>
public partial class LampPart : MachinePart
{
    protected override void Build()
    {
        PickRadius = .65f;
        PartArt.Box(Visual, new(.85f, .2f, .85f), new("#334856"), new(0, -.35f, 0));
        PartArt.Cylinder(Visual, .18f, .3f, new("#c7c7bb"), new(0, -.17f, 0));
        var neutral = new Color("#556573"); var lit = new Color("#fff0a5"); var emission = new Color("#e9b24c");
        var bulb = PartArt.Sphere(Visual, .43f, neutral, new(0, .22f, 0));
        var material = (StandardMaterial3D)bulb.MaterialOverride;
        material.EmissionEnabled = true; material.Emission = Colors.Black;
        var curve = CosmeticCurves.SignalLamp;
        BindVisual(curve, bulb, WorkshopVisualProperty.AlbedoRed, (Half)neutral.R, (Half)lit.R);
        BindVisual(curve, bulb, WorkshopVisualProperty.AlbedoGreen, (Half)neutral.G, (Half)lit.G);
        BindVisual(curve, bulb, WorkshopVisualProperty.AlbedoBlue, (Half)neutral.B, (Half)lit.B);
        BindVisual(curve, bulb, WorkshopVisualProperty.EmissionRed, (Half)0, (Half)emission.R);
        BindVisual(curve, bulb, WorkshopVisualProperty.EmissionGreen, (Half)0, (Half)emission.G);
        BindVisual(curve, bulb, WorkshopVisualProperty.EmissionBlue, (Half)0, (Half)emission.B);
    }
}
