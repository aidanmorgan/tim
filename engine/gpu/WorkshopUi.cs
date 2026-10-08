using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>UI presentation targets driven by the shared animation worker. Closed set; None means undeclared.</summary>
public enum WorkshopUiTarget : uint { None = 0, Hint = 1, Goal = 2 }

/// <summary>What drives a UI binding: a player control (Reveal/Hide/Visibility) or the committed goal occurrence.</summary>
public enum WorkshopUiFeedback : uint { None = 0, Control = 1, Goal = 2 }

/// <summary>Declared opacity curve for one UI target. Fixed animation target identities keep the worker element-free.</summary>
public readonly record struct UiCurveDeclaration(WorkshopUiTarget Target, WorkshopUiFeedback Feedback, AnimationCurve Curve, Half Duration, Half Neutral)
{
    public bool IsDeclared => Target != WorkshopUiTarget.None;
    public AnimationTargetId AnimationTarget => Target switch
    {
        WorkshopUiTarget.Hint => new(1),
        WorkshopUiTarget.Goal => new(ulong.MaxValue),
        _ => throw new ArgumentException("Undeclared UI target has no animation identity.")
    };
    public void Validate()
    {
        if (!Enum.IsDefined(Target) || !Enum.IsDefined(Feedback) || !Enum.IsDefined(Curve))
            throw new ArgumentException("Unknown UI curve declaration member.");
        if ((Target == WorkshopUiTarget.None) != (Feedback == WorkshopUiFeedback.None))
            throw new ArgumentException("A UI target and its feedback are declared together.");
        if (Target == WorkshopUiTarget.None)
        {
            if (this != default) throw new ArgumentException("Undeclared UI curve carries data.");
            return;
        }
        if (!Half.IsFinite(Duration) || Duration <= (Half)0 || Duration > (Half)30 ||
            !Half.IsFinite(Neutral) || Neutral < (Half)0 || Neutral > (Half)1)
            throw new ArgumentException("UI cosmetic requires a positive duration and a unit neutral opacity.");
    }
    public static UiCurveDeclaration For(WorkshopUiTarget target) => target switch
    {
        WorkshopUiTarget.Hint => UiCurves.Hint,
        WorkshopUiTarget.Goal => UiCurves.Goal,
        _ => throw new ArgumentException("Undeclared UI target.")
    };
}

/// <summary>The declared UI curves; the hint reveals from transparent, the goal label rests transparent until solved.</summary>
public static class UiCurves
{
    public static UiCurveDeclaration Hint => new(WorkshopUiTarget.Hint, WorkshopUiFeedback.Control, AnimationCurve.SmoothStep, (Half).16, (Half)1);
    public static UiCurveDeclaration Goal => new(WorkshopUiTarget.Goal, WorkshopUiFeedback.Goal, AnimationCurve.Linear, (Half)1, (Half)0);
}
