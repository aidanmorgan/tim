using System;

namespace CuriousContraptions.Gpu;

/// <summary>Typed authored sidekick lesson; contact and capture remain shared engines.</summary>
public static class BumperSidekick
{
    public static AssistanceProfile BumperAssistance => FirstPrinciples.BallAssistance with
    {
        Forgiving = FirstPrinciples.BallAssistance.Forgiving with { MaximumPositionCorrection = new((Half).25) },
        Balanced = FirstPrinciples.BallAssistance.Balanced with { MaximumPositionCorrection = new((Half).1) }
    };
    public static WorkshopBall Ball(GpuBodyId id) =>
        WorkshopInput.Basketball(id, -3, 5, 0, 0, 0, 0, 1) with { Locked = true };
    public static WorkshopReceiver Receiver(GpuBodyId id, PuzzlePrecision precision)
    {
        var assistance = FirstPrinciples.ReceiverAssistance.Evaluate(precision);
        return WorkshopInput.Receiver(id, 2, .9, 0, 0, 0, 0, 1) with
        {
            Locked = true,
            Capture = new(assistance.CaptureMargin, assistance.CaptureSpeed, assistance.CaptureDwell, SensorParticipation.Enabled),
            ForceRegion = ReceiverForceRegion.Create(assistance.CaptureMargin, assistance.GuideAcceleration)
        };
    }
    public static WorkshopConstruction Create(ConstructionRevision revision, WorkshopCadenceSettings settings,
        GpuBodyId ball, GpuBodyId receiver, PuzzlePrecision precision)
    {
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.BumperSidekick, WorkshopPlacementMode.Manual,
            precision, WorkshopPartKind.PinballBumper, 1,
            new(WorkshopGoalKind.Captured, ball, receiver, WorkshopPhysicsCompiler.CaptureSensor(Receiver(receiver, precision))),
            FirstPrinciples.BallAssistance, FirstPrinciples.ReceiverAssistance, BumperAssistance);
        var construction = new WorkshopConstruction(revision, settings, new(Ball(ball), Receiver(receiver, precision)), puzzle);
        construction.Validate(); return construction;
    }
    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.BumperSidekick) throw new ArgumentException("Not the sidekick lesson.");
        var next = construction.WithInstance(Receiver(construction.Puzzle.Goal.Target, precision)) with
            { Puzzle = construction.Puzzle with { Precision = precision } };
        next.Validate(); return next;
    }
    public static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.Placement != WorkshopPlacementMode.Manual || puzzle.InventoryKind != WorkshopPartKind.PinballBumper ||
            puzzle.InventoryCount != 1 || puzzle.Goal.Kind != WorkshopGoalKind.Captured ||
            puzzle.BallAssistance != FirstPrinciples.BallAssistance ||
            puzzle.ReceiverAssistance != FirstPrinciples.ReceiverAssistance || puzzle.RampAssistance != BumperAssistance ||
            construction.Connections.Count != 0)
            throw new ArgumentException("Unsupported sidekick authored settings.");
        var receiver = Receiver(puzzle.Goal.Target, puzzle.Precision);
        if (construction.Ball != Ball(puzzle.Goal.Body) || construction.Receiver != receiver ||
            puzzle.Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(receiver))
            throw new ArgumentException("Fixed sidekick declarations changed.");
        var receivers = 0; var bumpers = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopBall or WorkshopReceiver or WorkshopBumper) ||
                instance is WorkshopBumper { Locked: true })
                throw new ArgumentException("Unsupported sidekick inventory.");
            if (instance is WorkshopReceiver) receivers++;
            if (instance is WorkshopBumper) bumpers++;
        }
        if (receivers != 1 || bumpers > 1) throw new ArgumentException("Sidekick inventory exceeded.");
    }
}
