using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

internal static class AnimationEvaluation
{
    internal static AnimationValue Evaluate(this AnimationDefinition definition, float position, bool completed)
    {
        var phase = completed ? 1 : definition.Repeat switch
        {
            AnimationRepeat.Once => position,
            AnimationRepeat.Loop => position % 1,
            AnimationRepeat.PingPong => 1 - Math.Abs(position % 2 - 1),
            _ => throw new InvalidOperationException("Unsupported animation repetition.")
        };
        if (phase == 0) return definition.From;
        if (phase == 1) return definition.Curve == AnimationCurve.SinePulse ? definition.From : definition.To;
        var p = Math.Min(phase, 1 - phase);
        var smooth = p * p * p * (p * (p * 6 - 15) + 10);
        var amount = definition.Curve switch
        {
            AnimationCurve.Linear => phase,
            AnimationCurve.CubicSmoothStep => phase * phase * (3 - 2 * phase),
            AnimationCurve.SmoothStep => phase <= .5f ? smooth : 1 - smooth,
            AnimationCurve.SinePulse => MathF.Sin(MathF.PI * phase),
            _ => throw new InvalidOperationException("Unsupported animation curve.")
        };
        return AnimationValue.FromScalar(definition.From.Property, AnimationNumbers.Interpolate(definition.From.Number, definition.To.Number, amount));
    }
    internal static float Envelope(this AnimationImpulseDefinition definition, float elapsed)
    {
        if(elapsed<=0)return definition.PeakPhase.Value==(float)0?1:0;
        if(elapsed>=definition.Duration.Value)return 0;
        var phase=elapsed/definition.Duration.Value;
        return definition.Curve switch
        {
            AnimationImpulseCurve.LinearDecay=>1-phase,
            AnimationImpulseCurve.SmoothDecay=>Smooth(1-phase),
            AnimationImpulseCurve.SineSquaredPulse=>SquaredSine(phase),
            AnimationImpulseCurve.SmoothRiseFall=>phase<=definition.PeakPhase.Value?Smooth(phase/definition.PeakPhase.Value):Smooth((1-phase)/(1-definition.PeakPhase.Value)),
            _=>throw new InvalidOperationException("Unsupported impulse curve.")
        };
    }
    internal static float SquaredSine(float phase)
    {
        var sine=MathF.Sin(MathF.PI*phase);return sine*sine;
    }
    private static float Smooth(float phase)
    {
        var p=Math.Min(phase,1-phase);
        var smooth=p*p*p*(p*(p*6-15)+10);
        return phase<=.5f?smooth:1-smooth;
    }
}
