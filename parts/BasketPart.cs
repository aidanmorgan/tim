using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Receiver mesh with its declared capture halo binding only; no contact, timer or capture authority.</summary>
public partial class BasketPart : MachinePart
{
    protected override void Build()
    {
        PickRadius = .85f;
        foreach (var box in ReceiverGeometry.Walls)
            PartArt.Box(Visual, new((float)box.HalfExtents.X * 2, (float)box.HalfExtents.Y * 2, (float)box.HalfExtents.Z * 2),
                Definition.Color, new((float)box.Centre.X, (float)box.Centre.Y, (float)box.Centre.Z));
        var neutral = new Color("#bdf4bd"); var captured = Colors.White;
        var halo = PartArt.Ring(Visual, .94f, .025f, neutral, new(0, .55f, 0));
        halo.Scale = Vector3.One * (1 + (float)ReceiverCaptureSettings.Free.Margin.Value);
        var curve = CosmeticCurves.Receiver;
        BindVisual(curve, halo, WorkshopVisualProperty.AlbedoRed, (Half)neutral.R, (Half)captured.R);
        BindVisual(curve, halo, WorkshopVisualProperty.AlbedoGreen, (Half)neutral.G, (Half)captured.G);
        BindVisual(curve, halo, WorkshopVisualProperty.AlbedoBlue, (Half)neutral.B, (Half)captured.B);
    }
}
