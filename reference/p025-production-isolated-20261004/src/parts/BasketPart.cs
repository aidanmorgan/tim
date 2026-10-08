using Godot;
using CuriousContraptions.Gpu;
using System;

namespace CuriousContraptions;

/// <summary>Receiver mesh/animation target binding only; no contact, timer or capture authority.</summary>
public partial class BasketPart : MachinePart
{
    private MeshInstance3D? _halo;
    protected override void Build()
    {
        PickRadius = .85f;
        foreach (var box in ReceiverGeometry.Walls)
            PartArt.Box(Visual, new((float)box.HalfExtents.X * 2, (float)box.HalfExtents.Y * 2, (float)box.HalfExtents.Z * 2),
                Definition.Color, new((float)box.Centre.X, (float)box.Centre.Y, (float)box.Centre.Z));
        _halo = PartArt.Ring(Visual, .94f, .025f, new("#bdf4bd"), new(0, .55f, 0));
        _halo.Scale = Vector3.One * (1 + (float)ReceiverCaptureSettings.Free.Margin.Value);
        ApplyHalo((Half)0);
    }
    public void ApplyHalo(Half opacity)
    {
        if (!Half.IsFinite(opacity) || opacity < (Half)0 || opacity > (Half)1)
            throw new ArgumentException("Invalid committed halo animation sample.");
        if (_halo?.MaterialOverride is not StandardMaterial3D material) return;
        material.AlbedoColor = new Color("#bdf4bd").Lerp(Colors.White, (float)opacity);
    }
}
