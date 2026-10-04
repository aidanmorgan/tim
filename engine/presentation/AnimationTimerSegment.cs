using System;
namespace CuriousContraptions.Presentation;

public enum AnimationTimerPhase : uint { None, Ready, Counting, Finished }
public readonly record struct AnimationTimerObservation(ulong Started, ulong Due, ulong Observed,
    ulong InputEmitter, AnimationTimerPhase Phase)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Phase)) throw new ArgumentException("Unknown timer animation phase.");
        if (Phase == AnimationTimerPhase.None)
        {
            if (this != default) throw new ArgumentException("Absent timer observation has payload.");
            return;
        }
        if (Phase == AnimationTimerPhase.Ready)
        {
            if (Started != 0 || Due != 0 || InputEmitter != 0)
                throw new ArgumentException("Ready timer observation has an occurrence.");
            return;
        }
        if (InputEmitter == 0 || Due <= Started || Observed < Started ||
            (Phase == AnimationTimerPhase.Counting && Observed >= Due) ||
            (Phase == AnimationTimerPhase.Finished && Observed < Due))
            throw new ArgumentException("Invalid committed timer interval.");
    }
}
public readonly record struct AnimationTimerFrame(AnimationTimerPhase Phase, AnimationColourBlend Progress);
/// <summary>A worker-produced committed interval. Sampling cannot extrapolate beyond its observation.</summary>
public readonly record struct AnimationTimerSegment(AnimationTimerObservation Observation, AnimationColourBlend Endpoint)
{
    public static AnimationTimerSegment Create(AnimationTimerObservation observation)
    {
        observation.Validate();
        if (observation.Phase == AnimationTimerPhase.None) throw new ArgumentException("Missing timer observation.");
        var progress = observation.Phase == AnimationTimerPhase.Ready ? 0d :
            (double)(Math.Min(observation.Observed, observation.Due) - observation.Started) /
            (observation.Due - observation.Started);
        return new(observation, new((Half)progress));
    }
    public bool TrySample(double displayTick, out AnimationTimerFrame frame)
    {
        frame = default;
        if (!double.IsFinite(displayTick) || displayTick < 0 || displayTick > Observation.Observed) return false;
        if (Observation.Phase == AnimationTimerPhase.Ready || displayTick < Observation.Started)
        { frame = new(AnimationTimerPhase.Ready, new((Half)0)); return true; }
        if (displayTick >= Observation.Due)
        {
            if (Observation.Phase != AnimationTimerPhase.Finished) return false;
            frame = new(AnimationTimerPhase.Finished, new((Half)1)); return true;
        }
        var end = Math.Min(Observation.Observed, Observation.Due);
        var blend = end == Observation.Started ? (Half)0 :
            (Half)((displayTick - Observation.Started) / (end - Observation.Started));
        var value = AnimationValue.Blend(new(new AnimationColourBlend((Half)0)), new(Endpoint),
            new AnimationColourBlend(blend));
        frame = new(AnimationTimerPhase.Counting, new(BitConverter.UInt16BitsToHalf(value.CanonicalBits)));
        return true;
    }
}
