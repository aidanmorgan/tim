using System;

namespace CuriousContraptions.Gpu;

/// <summary>Authored clear_pipe lesson ("Through the looking tube"); ball entry, bore traversal, and receiver capture run on generic physics.</summary>
public static class ClearPipe
{
    public static AssistanceProfile PipeAssistance => FirstPrinciples.BallAssistance with
    {
        Forgiving = FirstPrinciples.BallAssistance.Forgiving with
        {
            MaximumPositionCorrection = new((Half).25),
            MaximumRotationCorrectionDegrees = (Half)5
        },
        Balanced = FirstPrinciples.BallAssistance.Balanced with
        {
            MaximumPositionCorrection = new((Half).1),
            MaximumRotationCorrectionDegrees = (Half)2
        }
    };

    public static WorkshopBall Ball(GpuBodyId id) =>
        WorkshopInput.Basketball(id, -1.4, 6.0, 0, 0, 0, 0, 1) with { Locked = true };

    public static WorkshopReceiver Receiver(GpuBodyId id, PuzzlePrecision precision)
    {
        var assistance = FirstPrinciples.ReceiverAssistance.Evaluate(precision);
        return WorkshopInput.Receiver(id, 2.0, 0.6, 0, 0, 0, 0, 1) with
        {
            Locked = true,
            Capture = new(assistance.CaptureMargin, assistance.CaptureSpeed, assistance.CaptureDwell, SensorParticipation.Enabled),
            ForceRegion = ReceiverForceRegion.Create(assistance.CaptureMargin, assistance.GuideAcceleration)
        };
    }

    public static WorkshopConstruction Create(ConstructionRevision revision, WorkshopCadenceSettings settings,
        GpuBodyId ball, GpuBodyId receiver, PuzzlePrecision precision)
    {
        var target = Receiver(receiver, precision);
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.ClearPipe, WorkshopPlacementMode.Manual,
            precision, PartId.Pipe, 1,
            new(WorkshopGoalKind.Captured, ball, receiver, WorkshopPhysicsCompiler.CaptureSensor(target)),
            FirstPrinciples.BallAssistance, FirstPrinciples.ReceiverAssistance, PipeAssistance);
        var construction = new WorkshopConstruction(revision, settings, new(Ball(ball), target), puzzle);
        construction.Validate(); return construction;
    }

    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.ClearPipe) throw new ArgumentException("Not the clear pipe lesson.");
        var next = construction.WithInstance(Receiver(construction.Puzzle.Goal.Target, precision)) with
            { Puzzle = construction.Puzzle with { Precision = precision } };
        next.Validate(); return next;
    }

    public static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.Placement != WorkshopPlacementMode.Manual || puzzle.InventoryPart != PartId.Pipe ||
            puzzle.InventoryCount != 1 || puzzle.Goal.Kind != WorkshopGoalKind.Captured ||
            puzzle.BallAssistance != FirstPrinciples.BallAssistance ||
            puzzle.ReceiverAssistance != FirstPrinciples.ReceiverAssistance || puzzle.RampAssistance != PipeAssistance ||
            construction.Connections.Count != 0)
            throw new ArgumentException("Unsupported clear pipe authored settings.");
        var receiver = Receiver(puzzle.Goal.Target, puzzle.Precision);
        if (construction.Ball != Ball(puzzle.Goal.Body) || construction.Receiver != receiver ||
            puzzle.Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(receiver))
            throw new ArgumentException("Fixed clear pipe declarations changed.");
        var receivers = 0; var pipes = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopBall or WorkshopReceiver or WorkshopGenericInstance { PartId.Value: "pipe" }) ||
                instance is WorkshopGenericInstance { PartId.Value: "pipe", Locked: true })
                throw new ArgumentException("Unsupported clear pipe inventory.");
            if (instance is WorkshopReceiver) receivers++;
            if (instance is WorkshopGenericInstance { PartId.Value: "pipe" }) pipes++;
        }
        if (receivers != 1 || pipes > 1) throw new ArgumentException("Clear pipe inventory exceeded.");
    }
}
