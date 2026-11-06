using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

public enum AnimationProperty { LocalRotationAngle = 1, LocalTranslation = 2, Opacity = 3, UniformScale = 4, ColourBlend = 5, ColourRed = 6, ColourGreen = 7, ColourBlue = 8 }
public enum AnimationCurve { Linear, SmoothStep, SinePulse, CubicSmoothStep }
public enum AnimationRepeat { Once, Loop, PingPong }
public enum AnimationClock { Presentation, Simulation }
public enum AnimationPlayback { Stopped, Playing, Completed }
public enum AnimationStop { Hold, RestoreInitial }
public enum AnimationEndpoint { From, To }

public readonly record struct AnimationTargetId
{
    public ulong Value { get; }
    public AnimationTargetId(ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value);
        Value = value;
    }
}
public readonly record struct AnimationGeneration
{
    public ulong Value { get; }
    public AnimationGeneration(ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value);
        Value = value;
    }
}
public readonly record struct AnimationBinding(AnimationTargetId Target, AnimationProperty Property);
public interface IAnimationHandle
{
    AnimationGeneration Generation { get; }
    int Slot { get; }
    ulong Version { get; }
}
public readonly record struct AnimationSample(AnimationBinding Binding, AnimationValue Value);
public readonly record struct AnimationRead(AnimationPlayback Playback, AnimationValue Value, bool Visible);
public readonly record struct AnimationFrameWork(int Evaluated, int DirtyWrites, int Scheduled, int Registered);

/// <summary>Canonical property-specific clip with f32 local evaluation.</summary>
public sealed record AnimationDefinition
{
    public AnimationValue From { get; }
    public AnimationValue To { get; }
    public AnimationDurationSeconds Duration { get; }
    public AnimationCurve Curve { get; }
    public AnimationRepeat Repeat { get; }
    public AnimationClock Clock { get; }

    public AnimationDefinition(AnimationValue from, AnimationValue to, AnimationDurationSeconds duration,
        AnimationCurve curve, AnimationRepeat repeat, AnimationClock clock)
    {
        AnimationValue.SameProperty(from, to);
        AnimationNumbers.Positive(duration.Value);
        if (!Enum.IsDefined(curve)) throw new ArgumentOutOfRangeException(nameof(curve));
        if (!Enum.IsDefined(repeat)) throw new ArgumentOutOfRangeException(nameof(repeat));
        if (!Enum.IsDefined(clock)) throw new ArgumentOutOfRangeException(nameof(clock));
        From = from; To = to; Duration = duration; Curve = curve; Repeat = repeat; Clock = clock;
    }

}

/// <summary>Canonical exponential response. Anchors and targets have the bound property's units.</summary>
public sealed record AnimationFollowDefinition
{
    public AnimationValue From { get; }
    public AnimationValue To { get; }
    public AnimationValue Initial { get; }
    public AnimationResponsePerSecond Response { get; }
    public AnimationClock Clock { get; }
    public AnimationFollowDefinition(AnimationValue from, AnimationValue to, AnimationValue initial,
        AnimationResponsePerSecond response, AnimationClock clock)
    {
        AnimationValue.SameProperty(from,to); AnimationValue.SameProperty(from,initial);
        AnimationNumbers.Positive(response.Value);
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        if(initial.Number<Math.Min(from.Number,to.Number)||initial.Number>Math.Max(from.Number,to.Number))
            throw new ArgumentOutOfRangeException(nameof(initial));
        From=from;To=to;Initial=initial;Response=response;Clock=clock;
    }
}
public enum AnimationImpulseCurve { LinearDecay, SmoothDecay, SineSquaredPulse, SmoothRiseFall }
public enum AnimationImpulseOverlap { Maximum, SaturatingSum, HyperbolicTangentSum }
public enum AnimationImpulseVisibility { DeferUntilVisible, AdvanceWhileHidden }
public enum AnimationImpulseTiming { EventTime, FirstPresentation, EventTimeRetainFirstFrame }
public sealed class AnimationStorageException : Exception
{
    public AnimationStorageException() : base("Animation storage cannot represent or allocate current demand.") { }
}
public readonly record struct AnimationOccurrenceId
{
    public ulong Value { get; }
    public AnimationOccurrenceId(ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value);
        Value=value;
    }
}
public readonly record struct AnimationImpulseRead(int Pending,int Playing,ulong Accepted,
    ulong Completed,ulong Cancelled,int HighWaterMark,int EvaluatedLastFrame,int Capacity);

/// <summary>Normalized independent impulse envelopes. Each accepted occurrence owns a slot until
/// completion or explicit cancellation. Overlap combines values, never occurrence identities.</summary>
public sealed record AnimationImpulseDefinition
{
    public AnimationDurationSeconds Duration { get; }
    public AnimationPeakPhase PeakPhase { get; }
    public AnimationImpulseCurve Curve { get; }
    public AnimationImpulseOverlap Overlap { get; }
    public AnimationImpulseVisibility Visibility { get; }
    public AnimationImpulseTiming Timing { get; }
    public AnimationClock Clock { get; }
    public AnimationImpulseDefinition(AnimationDurationSeconds duration,AnimationImpulseCurve curve,AnimationImpulseOverlap overlap,
        AnimationImpulseVisibility visibility,AnimationClock clock,AnimationImpulseTiming timing,AnimationPeakPhase peakPhase)
    {
        AnimationNumbers.Positive(duration.Value);
        if(!Enum.IsDefined(curve))throw new ArgumentOutOfRangeException(nameof(curve));
        if(!Enum.IsDefined(overlap))throw new ArgumentOutOfRangeException(nameof(overlap));
        if(!Enum.IsDefined(visibility))throw new ArgumentOutOfRangeException(nameof(visibility));
        if(!Enum.IsDefined(timing))throw new ArgumentOutOfRangeException(nameof(timing));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        AnimationNumbers.Unit(peakPhase.Value);
        if(curve switch
        {
            AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay=>peakPhase.Value!=(float)0,
            AnimationImpulseCurve.SineSquaredPulse=>peakPhase.Value!=(float).5f,
            AnimationImpulseCurve.SmoothRiseFall=>peakPhase.Value<1e-6f||peakPhase.Value>1-1e-6f,
            _=>true
        })throw new ArgumentOutOfRangeException(nameof(peakPhase));
        PeakPhase=peakPhase;
        Duration=duration;Curve=curve;Overlap=overlap;Visibility=visibility;Clock=clock;Timing=timing;
    }
    /// <summary>The one peak phase each curve admits; declarations and the worker share this table.</summary>
    public static AnimationPeakPhase CanonicalPeak(AnimationImpulseCurve curve)=>curve switch
    {
        AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay=>new((float)0),
        AnimationImpulseCurve.SineSquaredPulse or AnimationImpulseCurve.SmoothRiseFall=>new((float).5f),
        _=>throw new ArgumentOutOfRangeException(nameof(curve))
    };
}

/// <summary>Analytic cosmetic underdamped motion, with canonical float anchors and no physical feedback.</summary>
public sealed record AnimationOscillationDefinition
{
    public AnimationDecayPerSecond Decay { get; }
    public AnimationFrequencyRadiansPerSecond Frequency { get; }
    public AnimationVelocity VelocityPerStrength { get; }
    public AnimationClock Clock { get; }
    public AnimationOscillationDefinition(AnimationDecayPerSecond decay,
        AnimationFrequencyRadiansPerSecond frequency,AnimationVelocity velocityPerStrength,AnimationClock clock)
    {
        AnimationNumbers.OscillationCoefficient(decay.Value);
        AnimationNumbers.OscillationCoefficient(frequency.Value);
        velocityPerStrength.Validate();
        if(velocityPerStrength.Number<=0)throw new ArgumentOutOfRangeException(nameof(velocityPerStrength));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        Decay=decay;Frequency=frequency;VelocityPerStrength=velocityPerStrength;Clock=clock;
    }
}
public readonly record struct AnimationOscillationRead(AnimationValue Position, AnimationVelocity Velocity,
    ulong Accepted, AnimationOccurrenceId LastOccurrence);
