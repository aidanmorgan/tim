using System;

namespace CuriousContraptions.Gpu;

/// <summary>Preserved depth and Wall/Bumper lessons; physics and goals use the shared engines.</summary>
public static class BumperAdvanced
{
    public static AssistanceProfile WallAssistance => BumperSidekick.BumperAssistance with
    {
        Forgiving = BumperSidekick.BumperAssistance.Forgiving with { MaximumRotationCorrectionDegrees = (Half)5 },
        Balanced = BumperSidekick.BumperAssistance.Balanced with { MaximumRotationCorrectionDegrees = (Half)2 }
    };
    private static bool IsDepth(WorkshopPuzzleId id) => id switch
    {
        WorkshopPuzzleId.BumperDepth => true,
        WorkshopPuzzleId.WallAndBumper => false,
        _ => throw new ArgumentException("Not an advanced Bumper lesson.")
    };
    public static WorkshopBall Ball(WorkshopPuzzleId lesson, GpuBodyId id) => IsDepth(lesson)
        ? WorkshopInput.Basketball(id, 0, 5, 3, 0, Math.Sqrt(.5), 0, Math.Sqrt(.5)) with { Locked = true }
        : WorkshopInput.Basketball(id, -3, 5, 0, 0, 0, 0, 1) with { Locked = true };
    public static WorkshopReceiver Receiver(WorkshopPuzzleId lesson, GpuBodyId id, PuzzlePrecision precision)
    {
        var assistance = FirstPrinciples.ReceiverAssistance.Evaluate(precision);
        var receiver = IsDepth(lesson)
            ? WorkshopInput.Receiver(id, 0, .9, -2, 0, Math.Sqrt(.5), 0, Math.Sqrt(.5))
            : WorkshopInput.Receiver(id, -5, .9, 0, 0, 0, 0, 1);
        return receiver with
        {
            Locked = true,
            Capture = new(assistance.CaptureMargin, assistance.CaptureSpeed, assistance.CaptureDwell, SensorParticipation.Enabled),
            ForceRegion = ReceiverForceRegion.Create(assistance.CaptureMargin, assistance.GuideAcceleration)
        };
    }
    public static WorkshopConstruction Create(WorkshopPuzzleId lesson, ConstructionRevision revision,
        WorkshopCadenceSettings settings, GpuBodyId ball, GpuBodyId receiver, PuzzlePrecision precision)
    {
        var target = Receiver(lesson, receiver, precision);
        var puzzle = new WorkshopPuzzle(lesson, WorkshopPlacementMode.Manual, precision,
            WorkshopPartKind.PinballBumper, 1,
            new(WorkshopGoalKind.Captured, ball, receiver, WorkshopPhysicsCompiler.CaptureSensor(target)),
            FirstPrinciples.BallAssistance, FirstPrinciples.ReceiverAssistance, BumperSidekick.BumperAssistance);
        var construction = new WorkshopConstruction(revision, settings, new(Ball(lesson, ball), target), puzzle);
        construction.Validate(); return construction;
    }
    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        var next = construction.WithInstance(Receiver(construction.Puzzle.Id, construction.Puzzle.Goal.Target, precision))
            with { Puzzle = construction.Puzzle with { Precision = precision } };
        next.Validate(); return next;
    }
    public static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        var depth = IsDepth(puzzle.Id);
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.Placement != WorkshopPlacementMode.Manual || puzzle.InventoryKind != WorkshopPartKind.PinballBumper ||
            puzzle.InventoryCount != 1 || puzzle.Goal.Kind != WorkshopGoalKind.Captured ||
            puzzle.BallAssistance != FirstPrinciples.BallAssistance ||
            puzzle.ReceiverAssistance != FirstPrinciples.ReceiverAssistance ||
            puzzle.RampAssistance != BumperSidekick.BumperAssistance || construction.Connections.Count != 0)
            throw new ArgumentException("Unsupported advanced Bumper authored settings.");
        var receiver = Receiver(puzzle.Id, puzzle.Goal.Target, puzzle.Precision);
        if (construction.Ball != Ball(puzzle.Id, puzzle.Goal.Body) || construction.Receiver != receiver ||
            puzzle.Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(receiver))
            throw new ArgumentException("Fixed advanced Bumper declarations changed.");
        var receivers = 0; var bumpers = 0; var walls = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopBall or WorkshopReceiver or WorkshopBumper or WorkshopWall) ||
                instance is WorkshopBumper { Locked: true } or WorkshopWall { Locked: true })
                throw new ArgumentException("Unsupported advanced Bumper inventory.");
            if (instance is WorkshopReceiver) receivers++;
            if (instance is WorkshopBumper) bumpers++;
            if (instance is WorkshopWall) walls++;
        }
        if (receivers != 1 || bumpers > 1 || walls > (depth ? 0 : 1))
            throw new ArgumentException("Advanced Bumper inventory exceeded.");
    }
}
