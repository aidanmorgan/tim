using System;

namespace CuriousContraptions.Gpu;

/// <summary>Exact authored domino_effect fixtures and inventory; the cascade and the orientation sensor run on shared declarations.
/// The locked Basketball falls 0.4 m left of the first tile position, four tiles at 1.0 m spacing reach the locked end Domino
/// 4.1 m away, and the player wires the end Domino's ActivationOut to the locked Lamp (goal ActivatedAfter end → lamp, 0 s).</summary>
public static class DominoEffect
{
    public const uint Inventory = 4;
    public static WorkshopBall Ball(GpuBodyId id) => WorkshopInput.Basketball(id, -4.4, 3, 0, 0, 0, 0, 1) with { Locked = true };
    public static WorkshopDomino EndDomino(GpuBodyId id) => WorkshopInput.Domino(id, .1, .09, 0, 0, 0, 0, 1) with { Locked = true };
    public static WorkshopLamp Lamp(GpuBodyId id) => WorkshopInput.Lamp(id, 3, 1, 0, 0, 0, 0, 1) with { Locked = true };

    public static WorkshopConstruction Create(ConstructionRevision revision, WorkshopCadenceSettings settings,
        GpuBodyId ball, GpuBodyId end, GpuBodyId lamp, PuzzlePrecision precision)
    {
        var goal = new WorkshopGoal(WorkshopGoalKind.ActivatedAfter, default, default, default,
            new(end.Value), new(lamp.Value), new((Half)0));
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.DominoEffect, WorkshopPlacementMode.Manual,
            precision, WorkshopPartKind.Domino, Inventory, goal, default, default, default);
        var result = new WorkshopConstruction(revision, settings, new(Ball(ball), EndDomino(end), Lamp(lamp)), puzzle);
        result.Validate(); return result;
    }
    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.DominoEffect) throw new ArgumentException("Not the domino effect lesson.");
        var result = construction with { Puzzle = construction.Puzzle with { Precision = precision } };
        result.Validate(); return result;
    }
    internal static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        var end = new GpuBodyId(puzzle.Goal.SourceNode.Value);
        if (puzzle.InventoryKind != WorkshopPartKind.Domino || puzzle.InventoryCount != Inventory ||
            puzzle.Goal.Kind != WorkshopGoalKind.ActivatedAfter || !PhysicsDeclarationBounds.Zero(puzzle.Goal.MinimumDelay.Value) ||
            puzzle.BallAssistance != default || puzzle.ReceiverAssistance != default || puzzle.RampAssistance != default ||
            construction.Ball is not { } ball || ball != Ball(ball.Id) ||
            construction.Instances.Find<WorkshopLamp>() != Lamp(new(puzzle.Goal.TargetNode.Value)))
            throw new ArgumentException("Changed domino effect fixture or goal.");
        var dominoes = 0; var endFound = false;
        foreach (var instance in construction.Instances)
        {
            if (instance is WorkshopDomino tile && tile.Id == end)
            {
                if (tile != EndDomino(end)) throw new ArgumentException("Changed domino effect end Domino.");
                endFound = true; continue;
            }
            if (instance is WorkshopDomino { Locked: false }) { dominoes++; continue; }
            if (instance is WorkshopBall || instance is WorkshopLamp lamp && lamp.Id.Value == puzzle.Goal.TargetNode.Value) continue;
            throw new ArgumentException("Unsupported domino effect inventory.");
        }
        if (!endFound) throw new ArgumentException("The domino effect requires its fixed end Domino.");
        if (dominoes > puzzle.InventoryCount) throw new ArgumentException("Domino inventory exhausted.");
    }
}
