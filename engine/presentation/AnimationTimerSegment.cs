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
/// <summary>A worker-admitted committed interval; its endpoint seeds the shared clip that reaches 1 at Due.</summary>
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
}
