using CuriousContraptions.Abstractions.Signal;
using System;

namespace CuriousContraptions.Gpu;

public static class ActivationTimerTransitions
{
    public static uint Duration(float seconds, SimulationCadence cadence)
    {
        if (!float.IsFinite(seconds) || seconds <= 0f) throw new ArgumentException("Invalid timer duration.");
        var rate = cadence switch
        {
            SimulationCadence.Hz60 => 60f,
            SimulationCadence.Hz120 => 120f,
            SimulationCadence.Hz240 => 240f,
            _ => throw new ArgumentException("Unsupported timer cadence.")
        };
        return checked((uint)MathF.Ceiling(seconds * rate));
    }
    public static ActivationTimerState Trigger(this ActivationTimerState state, ActivationTimerDeclaration declaration, ActivationOccurrence occurrence, uint substeps)
    {
        state.Validate(); declaration.Validate(); occurrence.Validate();
        if (state.Node != declaration.Node) throw new ArgumentException("Timer declaration does not own this state.");
        if (state.Phase != ActivationTimerPhase.Ready) return state;
        var start = occurrence.Time.CeilingTick(substeps);
        var due = checked(start + declaration.DurationTicks);
        // Delayed events use the same finite physical timestamp domain as the committed stream.
        _ = ActivationTime.Boundary(new(due), substeps);
        return new(state.Node, ActivationTimerPhase.Counting, start, due, occurrence);
    }
    public static ActivationTimerState Complete(this ActivationTimerState state, ActivationTimerDeclaration declaration, SimulationTick boundary, uint substeps, out ActivationOccurrence? occurrence)
    {
        state.Validate(); declaration.Validate();
        if (state.Node != declaration.Node) throw new ArgumentException("Timer declaration does not own this state.");
        _ = ActivationTime.Boundary(boundary, substeps); occurrence = null;
        if (state.Phase != ActivationTimerPhase.Counting || state.DueTick > boundary.Value) return state;
        if (declaration.ElapsedSignal == TimerElapsedSignal.Activation)
        {
            occurrence = new(ActivationOccurrenceKind.TimerElapsed, state.Node,
                ActivationTime.Boundary(new(state.DueTick), substeps), state.Input.Cause);
            occurrence.Value.Validate();
        }
        return declaration.Completion == TimerCompletionPolicy.Rearm ? ActivationTimerState.Ready(state.Node) : state with { Phase = ActivationTimerPhase.Finished };
    }
}
