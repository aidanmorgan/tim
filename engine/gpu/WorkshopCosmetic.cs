using System;
using CuriousContraptions.Presentation;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Committed physics feedback that drives one part's cosmetic blend. Closed set; None means undeclared.
/// Activation and capture are one-shot occurrences; ongoing state is evaluated from package bindings.</summary>
public enum AnimationFeedbackSource : uint { None = 0, Activation = 1, Timer = 2, ContactWork = 3, Capture = 4 }

/// <summary>Sampled 0..1 cosmetic blend plus the committed timer phase that keys phase bindings.</summary>
public readonly record struct WorkshopCosmeticSample(float Blend, AnimationTimerPhase Phase)
{
    public static WorkshopCosmeticSample Neutral => new((float)0, AnimationTimerPhase.Ready);
}

/// <summary>Declared curve data for one part kind. Not persisted; the client forwards it unchanged to the animation worker.</summary>
public readonly record struct CosmeticCurveDeclaration(AnimationFeedbackSource Source, AnimationCurve Curve, float Duration,
    AnimationImpulseCurve ImpulseCurve, AnimationImpulseOverlap Overlap)
{
    public static CosmeticCurveDeclaration From(PresentationBinding binding) => binding.Driver switch
    {
        PresentationDriver.ActivationPulse => new(AnimationFeedbackSource.Activation,
            binding.Curve == PresentationCurve.SmoothStep ? AnimationCurve.SmoothStep : AnimationCurve.Linear, binding.Duration, default, default),
        PresentationDriver.CapturePulse => new(AnimationFeedbackSource.Capture,
            binding.Curve == PresentationCurve.SmoothStep ? AnimationCurve.SmoothStep : AnimationCurve.Linear, binding.Duration, default, default),
        PresentationDriver.ContactImpulse => new(AnimationFeedbackSource.ContactWork, AnimationCurve.Linear, binding.Duration,
            AnimationImpulseCurve.SineSquaredPulse, AnimationImpulseOverlap.SaturatingSum),
        _ => default
    };
    public static CosmeticCurveDeclaration For(PartId part)
    {
        var result = default(CosmeticCurveDeclaration);
        foreach (var binding in DeclaredPhysics.Of(part).Presentation.Bindings)
        {
            var value = From(binding);
            if (!value.IsDeclared) continue;
            if (result.IsDeclared && result != value) throw new ArgumentException("One-shot relay requires one declared envelope per owner.");
            result = value;
        }
        return result;
    }
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
            case AnimationFeedbackSource.Capture:
                if (!float.IsFinite(Duration) || Duration <= (float)0 || Duration > (float)30 || ImpulseCurve != default || Overlap != default)
                    throw new ArgumentException("Activation and capture cosmetics require a positive duration and no impulse envelope.");
                return;
            case AnimationFeedbackSource.Timer:
                if (Duration != (float)0 || ImpulseCurve != default || Overlap != default)
                    throw new ArgumentException("Timer cosmetic takes its duration from the committed interval.");
                return;
            case AnimationFeedbackSource.ContactWork:
                if (!float.IsFinite(Duration) || Duration <= (float)0 || Duration > (float)30 || Curve != AnimationCurve.Linear)
                    throw new ArgumentException("Contact cosmetic requires a positive duration and its impulse envelope.");
                return;
            default: throw new ArgumentException("Unknown cosmetic feedback source.");
        }
    }
    /// <summary>Peak phase each impulse envelope admits; declared curve data, not an evaluator.</summary>
    public AnimationPeakPhase ImpulsePeak => AnimationImpulseDefinition.CanonicalPeak(ImpulseCurve);
}
