using System;

namespace CuriousContraptions.Presentation;

/// <summary>A worker-admitted immutable occurrence envelope sampled only at selected physical time.</summary>
public readonly record struct AnimationPulseSegment(AnimationDurationSeconds Duration)
{
    public static AnimationPulseSegment Create(AnimationDurationSeconds duration)
    {
        AnimationNumbers.Positive(duration.Value);
        if (duration.Value > (Half)30) throw new ArgumentOutOfRangeException(nameof(duration));
        return new(duration);
    }

    public Half Sample(double selectedSeconds, uint eventOrdinal, Half eventPhase)
    {
        if (!double.IsFinite(selectedSeconds) || selectedSeconds < 0 ||
            !Half.IsFinite(eventPhase) || eventPhase < (Half)(-2048) || eventPhase >= (Half)2048)
            throw new ArgumentException("Invalid pulse sample time.");
        var occurred = (eventOrdinal + (double)eventPhase / 4096) / 480;
        var elapsed = selectedSeconds - occurred;
        if (elapsed <= 0 || elapsed >= (double)Duration.Value) return (Half)0;
        return (Half)AnimationImpulseDefinition.SquaredSine(elapsed / (double)Duration.Value);
    }
}
