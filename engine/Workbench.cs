using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Visible adapter for canonical deck support and finite workbench extents (f32 metres).</summary>
public static class Workbench
{
    public readonly record struct RenderBox(Vector3 At, Vector3 HalfExtent);
    public static readonly RenderBox Base = new(new(0, -.7f, 0), new(8.5f, .175f, 5f));
    public static readonly RenderBox Deck = new(
        new(0, WorkshopConstruction.WorkbenchSurface.Value - .03f, 0),
        new(8.4f, .03f, 4.9f));
}
