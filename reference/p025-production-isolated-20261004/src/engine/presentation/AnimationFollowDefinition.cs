using System;

namespace CuriousContraptions.Presentation;

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
