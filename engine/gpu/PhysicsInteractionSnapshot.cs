using System;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

public readonly record struct CapturePairKey(GpuSensorId Policy, GpuBodyId Target);
public readonly record struct WorkPairKey(GpuContactWorkId Policy, GpuBodyId Target);
public readonly record struct CapturePairState(ResidenceRead Residence, CaptureLatch Latch);

/// <summary>Immutable actual-interaction state committed with its physical world. Empty potential pairs own no rows.
/// Persistent maps share unchanged state between a staged candidate and its committed predecessor.</summary>
public sealed class PhysicsInteractionSnapshot
{
    public static readonly PhysicsInteractionSnapshot Empty = new(
        ImmutableDictionary<CapturePairKey,CapturePairState>.Empty,
        ImmutableDictionary<WorkPairKey,ContactWorkOccurrence>.Empty);
    public ImmutableDictionary<CapturePairKey,CapturePairState> Captures { get; }
    public ImmutableDictionary<WorkPairKey,ContactWorkOccurrence> Works { get; }

    public PhysicsInteractionSnapshot(ImmutableDictionary<CapturePairKey,CapturePairState> captures,
        ImmutableDictionary<WorkPairKey,ContactWorkOccurrence> works)
    {
        ArgumentNullException.ThrowIfNull(captures);
        ArgumentNullException.ThrowIfNull(works);
        foreach(var pair in captures)
        {
            var state=pair.Value;
            state.Residence.Validate(uint.MaxValue);
            state.Latch.Validate();
            if(pair.Key.Policy!=state.Residence.Id || pair.Key.Target!=state.Residence.Target ||
                state.Latch.Sensor!=pair.Key.Policy || state.Latch.Target!=pair.Key.Target ||
                (state.Residence.Phase==ResidencePhase.Outside && state.Latch.Phase!=CaptureLatchPhase.Latched))
                throw new ArgumentException("Capture state must own an actual dwelling or earned pair.");
        }
        foreach(var pair in works)
        {
            pair.Value.Validate();
            if(pair.Key.Policy.Value==0 || pair.Key.Target!=pair.Value.Target || pair.Value.Sequence==0)
                throw new ArgumentException("Work state must own an actual earned occurrence.");
        }
        Captures=captures; Works=works;
    }

    /// <summary>An absent interaction is outside with no earned event, without materializing state.</summary>
    public ResidenceRead Residence(CapturePairKey pair) => Captures.TryGetValue(pair,out var state)
        ? state.Residence : new(pair.Policy,pair.Target,ResidencePhase.Outside,0,0,0,0,0);
}
