using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>Committed physics feedback that drives one part's cosmetic blend. Closed set; None means undeclared.</summary>
public enum AnimationFeedbackSource : uint { None = 0, Activation = 1, Timer = 2, ContactWork = 3 }

/// <summary>Sampled 0..1 cosmetic blend plus the committed timer phase that keys phase bindings.</summary>
public readonly record struct WorkshopCosmeticSample(Half Blend, AnimationTimerPhase Phase)
{
    public static WorkshopCosmeticSample Neutral => new((Half)0, AnimationTimerPhase.Ready);
}

/// <summary>Declared curve data for one part kind. Not persisted; the client forwards it unchanged to the animation worker.</summary>
public readonly record struct CosmeticCurveDeclaration(AnimationFeedbackSource Source, AnimationCurve Curve, Half Duration,
    AnimationImpulseCurve ImpulseCurve, AnimationImpulseOverlap Overlap)
{
    public static CosmeticCurveDeclaration None => default;
    public bool IsDeclared => Source != AnimationFeedbackSource.None;
    public void Validate()
    {
        if (!Enum.IsDefined(Source) || !Enum.IsDefined(Curve) || !Enum.IsDefined(ImpulseCurve) || !Enum.IsDefined(Overlap))
            throw new ArgumentException("Unknown cosmetic curve declaration member.");
        switch (Source)
        {
            case AnimationFeedbackSource.None:
                if (this != default) throw new ArgumentException("Undeclared cosmetic curve carries data.");
                return;
            case AnimationFeedbackSource.Activation:
                if (!Half.IsFinite(Duration) || Duration <= (Half)0 || Duration > (Half)30 || ImpulseCurve != default || Overlap != default)
                    throw new ArgumentException("Activation cosmetic requires a positive duration and no impulse envelope.");
                return;
            case AnimationFeedbackSource.Timer:
                if (Duration != (Half)0 || ImpulseCurve != default || Overlap != default)
                    throw new ArgumentException("Timer cosmetic takes its duration from the committed interval.");
                return;
            case AnimationFeedbackSource.ContactWork:
                if (!Half.IsFinite(Duration) || Duration <= (Half)0 || Duration > (Half)30 || Curve != AnimationCurve.Linear)
                    throw new ArgumentException("Contact cosmetic requires a positive duration and its impulse envelope.");
                return;
            default: throw new ArgumentException("Unknown cosmetic feedback source.");
        }
    }
    /// <summary>Peak phase each impulse envelope admits; declared curve data, not an evaluator.</summary>
    public AnimationPeakPhase ImpulsePeak => AnimationImpulseDefinition.CanonicalPeak(ImpulseCurve);
}

/// <summary>Per-part cosmetic constants referenced by both the instance record and its part artwork.</summary>
public static class CosmeticCurves
{
    public static CosmeticCurveDeclaration ImpactSwitch => new(AnimationFeedbackSource.Activation, AnimationCurve.SmoothStep, (Half).16, default, default);
    public static CosmeticCurveDeclaration SignalLamp => new(AnimationFeedbackSource.Activation, AnimationCurve.SmoothStep, (Half).16, default, default);
    public static CosmeticCurveDeclaration Delay => new(AnimationFeedbackSource.Timer, AnimationCurve.Linear, (Half)0, default, default);
    public static CosmeticCurveDeclaration PinballBumper => new(AnimationFeedbackSource.ContactWork, AnimationCurve.Linear, (Half).32,
        AnimationImpulseCurve.SineSquaredPulse, AnimationImpulseOverlap.SaturatingSum);
}
