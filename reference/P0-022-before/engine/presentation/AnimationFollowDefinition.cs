using System;

namespace CuriousContraptions.Presentation;

/// <summary>Bounded scalar exponential response. Response is inverse clock-seconds.
/// Initial is the construction value within the inclusive From/To interval.</summary>
public sealed record AnimationFollowDefinition
{
    public double From { get; }
    public double To { get; }
    public double Initial { get; }
    public double Response { get; }
    public AnimationClock Clock { get; }
    public AnimationFollowDefinition(double from,double to,double initial,double response,AnimationClock clock)
    {
        if(!double.IsFinite(from)||!double.IsFinite(to)||!double.IsFinite(to-from))
            throw new ArgumentOutOfRangeException(nameof(to));
        if(!double.IsFinite(response)||response<=0)throw new ArgumentOutOfRangeException(nameof(response));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        if(!double.IsFinite(initial)||initial<Math.Min(from,to)||initial>Math.Max(from,to))
            throw new ArgumentOutOfRangeException(nameof(initial));
        From=from;To=to;Initial=initial;Response=response;Clock=clock;
    }
}
