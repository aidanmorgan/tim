using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Visible adapter for canonical deck support and finite workbench extents.</summary>
public static class Workbench
{
    public readonly record struct RenderBox(Vector3 At, Vector3 Half);
    private static float Render(Half value) => (float)value;
    public static readonly RenderBox Base = new(new(0, Render((Half)(-.7)), 0),
        new(Render((Half)8.5), Render((Half).175), Render((Half)5)));
    public static readonly RenderBox Deck = new(
        new(0, Render(WorkshopConstruction.WorkbenchSurface.Value) - Render((Half).03), 0),
        new(Render((Half)8.4), Render((Half).03), Render((Half)4.9)));
}
