using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

// A canonical control anchor, never a second clock or accumulated numerical remainder.
// The repeat kind fixes its period: Once [0,1], Loop [0,1), PingPong [0,2).
internal readonly record struct AnimationPhase
{
    public float Value { get; }
    private AnimationPhase(float value) => Value = value;
    public static AnimationPhase Capture(float phase, AnimationRepeat repeat)
    {
        if (!float.IsFinite(phase)) throw new ArgumentOutOfRangeException(nameof(phase));
        var period = repeat == AnimationRepeat.PingPong ? 2.0f : 1.0f;
        if (repeat == AnimationRepeat.Once) return new((float)Math.Clamp(phase, 0, 1));
        if (!Enum.IsDefined(repeat)) throw new ArgumentOutOfRangeException(nameof(repeat));
        var reduced = phase % period;
        if (reduced < 0) reduced += period;
        var canonical = (float)reduced;
        return new(canonical == period ? (float)0 : canonical);
    }
}
