using System;
namespace CuriousContraptions.Abstractions;

/// <summary>Generic availability of a chamber and its exact finite reservoir; at capacity does not promise positive response.</summary>
public enum FiniteWorkObservationState { Empty, Loading, SeatedUncharged, SeatedCharged, Departing, Unavailable }

public readonly record struct PresentationFiniteWorkValues(float Empty, float Loading, float SeatedUncharged,
    float SeatedCharged, float Departing, float Unavailable)
{
    public bool IsFinite() => float.IsFinite(Empty) && float.IsFinite(Loading) && float.IsFinite(SeatedUncharged) &&
        float.IsFinite(SeatedCharged) && float.IsFinite(Departing) && float.IsFinite(Unavailable);
    public float Map(FiniteWorkObservationState state) => state switch
    {
        FiniteWorkObservationState.Empty => Empty,
        FiniteWorkObservationState.Loading => Loading,
        FiniteWorkObservationState.SeatedUncharged => SeatedUncharged,
        FiniteWorkObservationState.SeatedCharged => SeatedCharged,
        FiniteWorkObservationState.Departing => Departing,
        FiniteWorkObservationState.Unavailable => Unavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };
}
