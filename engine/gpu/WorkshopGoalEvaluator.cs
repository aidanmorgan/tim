using System;

namespace CuriousContraptions.Gpu;

public enum WorkshopGoalPhase { NotApplicable, Waiting, Solved }

/// <summary>Goal truth and its occurrence derive only from the committed named-event read.</summary>
public static class WorkshopGoalEvaluator
{
    public static WorkshopGoalPhase Evaluate(WorkshopGoal goal, WorkshopRead read, SimulationEpoch epoch, CompiledIdentityMap identities)
    {
        goal.Validate();
        if (goal.Kind == WorkshopGoalKind.None) return WorkshopGoalPhase.NotApplicable;
        return Occurrence(goal, read, epoch, identities) is not null ? WorkshopGoalPhase.Solved : WorkshopGoalPhase.Waiting;
    }
    public static ActivationTime? Occurrence(WorkshopGoal goal, WorkshopRead read, SimulationEpoch epoch, CompiledIdentityMap identities)
    {
        goal.Validate();
        if (epoch.Value == 0 || read.Epoch != epoch) return null;
        if (goal.Kind == WorkshopGoalKind.Captured)
        {
            if (!read.Bodies.TryGet(goal.Body, out var body) || body.Body.Epoch != epoch.Value) return null;
            return Occurrence(goal, read.Captures, read.Activations, identities);
        }
        return Occurrence(goal, read.Captures, read.Activations, identities);
    }
    public static ActivationTime? Occurrence(WorkshopGoal goal, PhysicsCaptureRead captures, PhysicsActivationRead activations, CompiledIdentityMap identities)
    {
        goal.Validate();
        if (goal.Kind == WorkshopGoalKind.Captured)
        {
            if (!identities.TryCapture(goal.Target, goal.Body, out var rows)) return null;
            for (var i = 0; i < captures.Count; i++)
                if (captures[i].Sensor == rows.Sensor && captures[i].Target == goal.Body && captures[i].Phase == CaptureLatchPhase.Latched)
                    return new(captures[i].EventOrdinal, (float)captures[i].EventPhase);
        }
        else if (goal.Kind == WorkshopGoalKind.ActivatedAfter)
        {
            ActivationLatch? source = null, target = null;
            for (var i = 0; i < activations.Count; i++)
            {
                var value = activations[i];
                if (value.Phase != ActivationPhase.Latched) continue;
                if (value.Node == goal.SourceNode && value.Owner.Value == goal.SourceNode.Value) source = value;
                if (value.Node == goal.TargetNode && value.Owner.Value == goal.TargetNode.Value) target = value;
            }
            if (source is { } start && target is { } finish &&
                ElapsedAtLeast(start.Occurrence.Time, finish.Occurrence.Time, goal.MinimumDuration))
                return finish.Occurrence.Time;
        }
        return null;
    }
    public static bool ElapsedAtLeast(ActivationTime source, ActivationTime target, DurationSeconds minimum)
    {
        source.Validate(); target.Validate();
        PhysicsDeclarationBounds.Range(minimum.Value, 0f, 120f);
        if (target.CompareTo(source) < 0) return false;
        // Keep the integer origin separate: adding a small f32 phase to a large ordinal would erase it.
        var ordinals = (long)target.Ordinal - source.Ordinal;
        var required = minimum.Value * WorkshopCadenceSettings.PhysicalFrequency;
        var whole = (long)MathF.Floor(required);
        var fraction = required - whole;
        var elapsedPhase = target.Phase - source.Phase;
        if (ordinals < whole) return false;
        if (ordinals == whole) return elapsedPhase >= fraction * 4096f;
        if (ordinals == whole + 1) return elapsedPhase >= (fraction - 1f) * 4096f;
        return true;
    }
}
