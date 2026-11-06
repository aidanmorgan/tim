using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>The declared UI curves; the hint reveals from transparent, the goal label rests transparent until solved.</summary>
public sealed class WorkshopUiCurves : IWorkshopUiCurves
{
    public UiCurveDeclaration CurveFor(WorkshopUiTarget target) => target switch
    {
        WorkshopUiTarget.Hint => Hint,
        WorkshopUiTarget.Goal => Goal,
        _ => throw new ArgumentException("Undeclared UI target.")
    };
    public UiCurveDeclaration Hint => new(WorkshopUiTarget.Hint, WorkshopUiFeedback.Control, AnimationCurve.SmoothStep, .16f, (float)1);
    public UiCurveDeclaration Goal => new(WorkshopUiTarget.Goal, WorkshopUiFeedback.Goal, AnimationCurve.Linear, (float)1, (float)0);
}
