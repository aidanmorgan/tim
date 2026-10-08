using System;

namespace CuriousContraptions.Gpu;

/// <summary>Exact authored delayed_signal fixtures and inventory; all execution uses shared declarations.</summary>
public static class DelayedSignal
{
    // These three locked fixtures have the same source assistance knots.
    public static AssistanceProfile FixedAssistance => FirstPrinciples.BallAssistance;
    public static AssistanceProfile DelayAssistance => FixedAssistance with
    {
        Forgiving = FixedAssistance.Forgiving with { MaximumPositionCorrection = new((Half).25), MaximumRotationCorrectionDegrees = (Half)5 },
        Balanced = FixedAssistance.Balanced with { MaximumPositionCorrection = new((Half).1), MaximumRotationCorrectionDegrees = (Half)2 }
    };
    public static WorkshopBall Ball(GpuBodyId id) => WorkshopInput.Basketball(id, -3, 4, 0, 0, 0, 0, 1) with { Locked = true };
    public static WorkshopSwitch Switch(GpuBodyId id, PuzzlePrecision precision) =>
        WorkshopInput.Switch(id, -3, 1, 0, 0, 0, 0, 1, new(new(FixedAssistance.Evaluate(precision).TriggerThreshold))) with
        { Locked = true, Trigger = new(new(FixedAssistance.Evaluate(precision).TriggerThreshold)) };
    public static WorkshopLamp Lamp(GpuBodyId id) => WorkshopInput.Lamp(id, 3, 1, 0, 0, 0, 0, 1) with { Locked = true };

    public static WorkshopConstruction Create(ConstructionRevision revision, WorkshopCadenceSettings settings,
        GpuBodyId ball, GpuBodyId source, GpuBodyId target, PuzzlePrecision precision)
    {
        var goal = new WorkshopGoal(WorkshopGoalKind.ActivatedAfter, default, default, default,
            new(source.Value), new(target.Value), new((Half)1));
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.DelayedSignal, WorkshopPlacementMode.Manual,
            precision, WorkshopPartKind.Delay, 1, goal, default, default, default);
        var result = new WorkshopConstruction(revision, settings,
            new(Ball(ball), Switch(source, precision), Lamp(target)), puzzle);
        result.Validate(); return result;
    }
    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.DelayedSignal) throw new ArgumentException("Not the delayed signal lesson.");
        var result = construction.WithInstance(Switch(new(construction.Puzzle.Goal.SourceNode.Value), precision)) with
            { Puzzle = construction.Puzzle with { Precision = precision } };
        result.Validate(); return result;
    }
    internal static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.InventoryKind != WorkshopPartKind.Delay || puzzle.InventoryCount != 1 ||
            puzzle.Goal.Kind != WorkshopGoalKind.ActivatedAfter || puzzle.Goal.MinimumDelay != new DurationSeconds((Half)1) ||
            puzzle.BallAssistance != default || puzzle.ReceiverAssistance != default || puzzle.RampAssistance != default ||
            construction.Ball is not { } ball || ball != Ball(ball.Id) ||
            construction.Instances.Find<WorkshopSwitch>() != Switch(new(puzzle.Goal.SourceNode.Value), puzzle.Precision) ||
            construction.Instances.Find<WorkshopLamp>() != Lamp(new(puzzle.Goal.TargetNode.Value)))
            throw new ArgumentException("Changed delayed signal fixture or goal.");
        var delays = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is WorkshopDelay { Locked: false }) { delays++; continue; }
            if (instance is WorkshopBall || instance is WorkshopLamp lamp && lamp.Id.Value == puzzle.Goal.TargetNode.Value ||
                instance is WorkshopSwitch trigger && trigger.Id.Value == puzzle.Goal.SourceNode.Value) continue;
            throw new ArgumentException("Unsupported delayed signal inventory.");
        }
        if (delays > puzzle.InventoryCount) throw new ArgumentException("Delay inventory exhausted.");
    }
}
