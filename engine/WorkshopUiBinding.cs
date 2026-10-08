using System;
using Godot;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Render boundary for one UI control's opacity, sampled by the shared animation worker. The only UI Modulate writer.</summary>
public sealed class WorkshopUiBinding
{
    private readonly CanvasItem _target;
    private readonly UiCurveDeclaration _declaration;
    public WorkshopUiTarget Target => _declaration.Target;
    public WorkshopUiBinding(WorkshopUiTarget target, CanvasItem item)
    {
        _declaration = UiCurveDeclaration.For(target); _declaration.Validate();
        _target = item ?? throw new ArgumentNullException(nameof(item));
    }
    public void Apply(AnimationOpacity opacity)
    {
        var colour = _target.Modulate;
        colour.A = (float)opacity.Value; // Named Godot presentation adapter.
        _target.Modulate = colour;
    }
    /// <summary>The declared resting opacity: Reset, Hide and construction changes return here.</summary>
    public void ApplyNeutral() => Apply(new(_declaration.Neutral));
}
