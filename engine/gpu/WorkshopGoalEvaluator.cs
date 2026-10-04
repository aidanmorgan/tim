using System;

namespace CuriousContraptions.Gpu;

public enum WorkshopGoalPhase { NotApplicable, Waiting, Solved }

/// <summary>Generic named-event goal projection of an already committed monotonic controller read.</summary>
public static class WorkshopGoalEvaluator
{
    public static WorkshopGoalPhase Evaluate(WorkshopGoal goal, WorkshopRead read, SimulationEpoch epoch)
    {
        if (goal.Kind == WorkshopGoalKind.None)
        {
            if (goal != default) throw new ArgumentException("Absent goal contains an identity.");
            return WorkshopGoalPhase.NotApplicable;
        }
        if (goal.Kind != WorkshopGoalKind.Captured || goal.Body.Value == 0 || goal.Target.Value == 0 || goal.EventSource.Value == 0)
            throw new ArgumentException("Unsupported named goal.");
        if (epoch.Value == 0 || read.Epoch != epoch || read.Ball is not { } body || body.Id != goal.Body || body.Epoch != epoch.Value) return WorkshopGoalPhase.Waiting;
        for (var i = 0; i < read.Captures.Count; i++)
        {
            var capture = read.Captures[i];
            if (capture.Sensor == goal.EventSource && capture.Phase == CaptureLatchPhase.Latched)
                return WorkshopGoalPhase.Solved;
        }
        return WorkshopGoalPhase.Waiting;
    }
}
