using System;

namespace CuriousContraptions.Gpu;

/// <summary>Authored quarter_bend lesson ("Around the corner"); ball entry, bore traversal through 90 deg bend, and receiver capture run on generic physics.</summary>
public static class QuarterBend
{
    public static AssistanceProfile BendAssistance => FirstPrinciples.BallAssistance with
    {
        Forgiving = FirstPrinciples.BallAssistance.Forgiving with
        {
            PositionWindow = new((Half).6),
            RotationWindowDegrees = (Half)5,
            MaximumPositionCorrection = new((Half).6),
            MaximumRotationCorrectionDegrees = (Half)5
        },
        Balanced = FirstPrinciples.BallAssistance.Balanced with
        {
            PositionWindow = new((Half).5),
            RotationWindowDegrees = (Half)2,
            MaximumPositionCorrection = new((Half).5),
            MaximumRotationCorrectionDegrees = (Half)2
        }
    };

    public static WorkshopBall Ball(GpuBodyId id) =>
        WorkshopInput.Basketball(id, 0.7029, 7.3870564, 0, 0, 0, 0, 1) with { Locked = true };

    public static WorkshopReceiver Receiver(GpuBodyId id, PuzzlePrecision precision)
    {
        var assistance = FirstPrinciples.ReceiverAssistance.Evaluate(precision);
        return WorkshopInput.Receiver(id, -5.5, 0.6, 0, 0, 0, 0, 1) with
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
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.QuarterBend, WorkshopPlacementMode.Manual,
            precision, PartId.PipeBend90, 1,
            new(WorkshopGoalKind.Captured, ball, receiver, WorkshopPhysicsCompiler.CaptureSensor(target)),
            FirstPrinciples.BallAssistance, FirstPrinciples.ReceiverAssistance, BendAssistance);
        var construction = new WorkshopConstruction(revision, settings, new(Ball(ball), target), puzzle);
        construction.Validate(); return construction;
    }

    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.QuarterBend) throw new ArgumentException("Not the quarter bend lesson.");
        var next = construction.WithInstance(Receiver(construction.Puzzle.Goal.Target, precision)) with
            { Puzzle = construction.Puzzle with { Precision = precision } };
        next.Validate(); return next;
    }

    public static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.Placement != WorkshopPlacementMode.Manual || puzzle.InventoryPart != PartId.PipeBend90 ||
            puzzle.InventoryCount != 1 || puzzle.Goal.Kind != WorkshopGoalKind.Captured ||
            puzzle.BallAssistance != FirstPrinciples.BallAssistance ||
            puzzle.ReceiverAssistance != FirstPrinciples.ReceiverAssistance || puzzle.RampAssistance != BendAssistance ||
            construction.Connections.Count != 0)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.PuzzleSettings, "Unsupported quarter bend authored settings.");
        var receiver = Receiver(puzzle.Goal.Target, puzzle.Precision);
        if (construction.Ball != Ball(puzzle.Goal.Body) || construction.Receiver != receiver ||
            puzzle.Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(receiver))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.FixedPart, "Fixed quarter bend declarations changed.");
        var receivers = 0; var bends = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopBall or WorkshopReceiver or WorkshopGenericInstance { PartId.Value: "pipe_bend_90" }) ||
                instance is WorkshopGenericInstance { PartId.Value: "pipe_bend_90", Locked: true })
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InventoryUnsupported, "Unsupported quarter bend inventory.");
            if (instance is WorkshopReceiver) receivers++;
            if (instance is WorkshopGenericInstance { PartId.Value: "pipe_bend_90" }) bends++;
        }
        if (receivers != 1 || bends > 1)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InventoryExceeded, "Quarter bend inventory exceeded.");
    }
}
