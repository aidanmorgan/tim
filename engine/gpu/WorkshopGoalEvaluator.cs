using System;

namespace CuriousContraptions.Gpu;

public enum WorkshopGoalPhase { NotApplicable, Waiting, Solved }

/// <summary>Goal truth and its occurrence derive only from the committed named-event read.</summary>
public static class WorkshopGoalEvaluator
{
    public static WorkshopGoalPhase Evaluate(WorkshopGoal goal, WorkshopRead read, SimulationEpoch epoch)
    {
        goal.Validate();
        if (goal.Kind == WorkshopGoalKind.None) return WorkshopGoalPhase.NotApplicable;
        return Occurrence(goal, read, epoch) is not null ? WorkshopGoalPhase.Solved : WorkshopGoalPhase.Waiting;
    }
    public static ActivationTime? Occurrence(WorkshopGoal goal, WorkshopRead read, SimulationEpoch epoch)
    {
        goal.Validate();
        if (epoch.Value == 0 || read.Epoch != epoch) return null;
        if (goal.Kind == WorkshopGoalKind.Captured)
        {
            if (!read.Bodies.TryGet(goal.Body, out var body) || body.Body.Epoch != epoch.Value) return null;
            return Occurrence(goal, read.Captures, read.Activations);
        }
        return Occurrence(goal, read.Captures, read.Activations);
    }
    public static ActivationTime? Occurrence(WorkshopGoal goal, PhysicsCaptureRead captures, PhysicsActivationRead activations)
    {
        goal.Validate();
        if (goal.Kind == WorkshopGoalKind.Captured)
        {
            for (var i = 0; i < captures.Count; i++)
                if (captures[i].Sensor == goal.EventSource && captures[i].Phase == CaptureLatchPhase.Latched)
                    return new(captures[i].EventOrdinal, captures[i].EventPhase);
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
                ElapsedAtLeast(start.Occurrence.Time, finish.Occurrence.Time, goal.MinimumDelay))
                return finish.Occurrence.Time;
        }
        return null;
    }
    public static bool ElapsedAtLeast(ActivationTime source, ActivationTime target, DurationSeconds minimum)
    {
        source.Validate(); target.Validate();
        PhysicsDeclarationBounds.Range(minimum.Value, (Half)0, (Half)120);
        // Integer clock boundary adapter: 2^-36 of one canonical 480-Hz step.
        // A Half phase is exact in units 2^-24, then divided by 4096.
        // No elapsed time is rounded into Half or into simulation ticks.
        if (target.CompareTo(source) < 0) return false;
        var ordinals = (long)target.Ordinal - source.Ordinal;
        if (ordinals > 120 * 480) return true; // Signed phase difference has magnitude below one step.
        var elapsed = checked(ordinals * (1L << 36) +
            HalfClockUnits(target.Phase, 24) - HalfClockUnits(source.Phase, 24));
        var duration = checked(HalfClockUnits(minimum.Value, 36) * 480);
        return elapsed >= duration;
    }
    private static long HalfClockUnits(Half value, int scale)
    {
        var bits = BitConverter.HalfToUInt16Bits(value);
        var exponent = (bits >> 10) & 31;
        var significand = (long)(bits & 1023);
        var shift = scale - 24;
        if (exponent != 0) { significand |= 1024; shift = scale + exponent - 25; }
        var magnitude = checked(significand * (1L << shift));
        return (bits & 32768) == 0 ? magnitude : -magnitude;
    }
}
