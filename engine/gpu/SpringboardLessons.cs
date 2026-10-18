using System;
using System.Linq;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Preserved passive and precharged lessons; all motion remains generic prismatic/contact physics.</summary>
public static class SpringboardLessons
{
    public static AssistanceProfile SpringAssistance => FirstPrinciples.BallAssistance with
    {
        Forgiving = FirstPrinciples.BallAssistance.Forgiving with
            { MaximumPositionCorrection = new((Half).2), MaximumRotationCorrectionDegrees = (Half)3 },
        Balanced = FirstPrinciples.BallAssistance.Balanced with
            { MaximumPositionCorrection = new((Half).080000006), MaximumRotationCorrectionDegrees = (Half)1.2 }
    };
    public static AssistanceProfile ReadyAssistance => FirstPrinciples.BallAssistance with
    {
        Forgiving = FirstPrinciples.BallAssistance.Forgiving with
            { PositionWindow = new((Half).2), RotationWindowDegrees = (Half)5, TriggerThreshold = (Half).8 },
        Balanced = FirstPrinciples.BallAssistance.Balanced with
            { PositionWindow = new((Half).1), RotationWindowDegrees = (Half)2, TriggerThreshold = (Half).8 }
    };
    public static AssistanceProfile ReadyReceiverAssistance => ReadyAssistance with
    {
        Forgiving = ReadyAssistance.Forgiving with
            { MaximumPositionCorrection = new((Half).2), MaximumRotationCorrectionDegrees = (Half)5 },
        Balanced = ReadyAssistance.Balanced with
            { MaximumPositionCorrection = new((Half).1), MaximumRotationCorrectionDegrees = (Half)2 }
    };
    private static bool IsReady(WorkshopPuzzleId lesson) => lesson switch
    {
        WorkshopPuzzleId.SpringForward => false,
        WorkshopPuzzleId.ReadyToRebound => true,
        _ => throw new ArgumentException("Not a Springboard lesson.")
    };
    public static WorkshopBall Ball(WorkshopPuzzleId lesson, GpuBodyId id) => (IsReady(lesson)
        ? WorkshopInput.Basketball(id, .1785, 2.809171, 0, 0, 0, 0, 1)
        : WorkshopInput.Basketball(id, -3, 3.6, 0, 0, 0, 0, 1)) with { Locked = true };
    public static WorkshopSpringboard ChargedBoard(GpuBodyId id)
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 6);
        return WorkshopInput.Springboard(id, 0, 2.5, 0, q.X, q.Y, q.Z, q.W,
            new(400, .2f, .2f)) with { Locked = true };
    }
    public static WorkshopReceiver ReceiverSettings(WorkshopReceiver receiver, WorkshopPuzzleId lesson, PuzzlePrecision precision)
    {
        var assistance = (IsReady(lesson) ? ReadyReceiverAssistance : FirstPrinciples.ReceiverAssistance).Evaluate(precision);
        return receiver with
        {
            Capture = new(assistance.CaptureMargin, assistance.CaptureSpeed, assistance.CaptureDwell, SensorParticipation.Enabled),
            ForceRegion = ReceiverForceRegion.Create(assistance.CaptureMargin, assistance.GuideAcceleration)
        };
    }
    public static WorkshopReceiver FixedReceiver(GpuBodyId id, PuzzlePrecision precision) =>
        ReceiverSettings(WorkshopInput.Receiver(id, -.7, .55, 0, 0, 0, 0, 1),
            WorkshopPuzzleId.SpringForward, precision) with { Locked = true };

    public static WorkshopConstruction Create(WorkshopPuzzleId lesson, ConstructionRevision revision,
        WorkshopCadenceSettings settings, GpuBodyId ball, GpuBodyId fixedPart, GpuBodyId receiver, PuzzlePrecision precision)
    {
        var ready = IsReady(lesson);
        var target = ready ? WorkshopInput.Receiver(receiver, 0, 0, 0, 0, 0, 0, 1) : FixedReceiver(fixedPart, precision);
        var puzzle = new WorkshopPuzzle(lesson, WorkshopPlacementMode.Manual, precision,
            ready ? PartId.Receiver : PartId.Springboard, 1,
            new(WorkshopGoalKind.Captured, ball, target.Id, WorkshopPhysicsCompiler.CaptureSensor(target)),
            ready ? ReadyAssistance : FirstPrinciples.BallAssistance,
            ready ? ReadyReceiverAssistance : FirstPrinciples.ReceiverAssistance,
            ready ? ReadyAssistance : SpringAssistance);
        var construction = new WorkshopConstruction(revision, settings,
            ready ? new(Ball(lesson, ball), ChargedBoard(fixedPart)) : new(Ball(lesson, ball), target), puzzle);
        construction.Validate(); return construction;
    }
    public static WorkshopConstruction WithReceiver(WorkshopConstruction construction, WorkshopReceiver receiver)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.ReadyToRebound)
            throw new ArgumentException("The lesson does not author a Receiver.");
        receiver = ReceiverSettings(receiver, construction.Puzzle.Id, construction.Puzzle.Precision);
        return construction with
        {
            Instances = construction.Instances.With(receiver),
            Puzzle = construction.Puzzle with { Goal = construction.Puzzle.Goal with
                { Target = receiver.Id, EventSource = WorkshopPhysicsCompiler.CaptureSensor(receiver) } }
        };
    }
    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        var ready = IsReady(construction.Puzzle.Id);
        var next = construction with { Puzzle = construction.Puzzle with { Precision = precision } };
        if (!ready) next = next.WithInstance(FixedReceiver(next.Puzzle.Goal.Target, precision));
        else if (next.Receiver is { } receiver) next = WithReceiver(next, receiver);
        next.Validate(); return next;
    }
    public static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        var ready = IsReady(puzzle.Id);
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.InventoryPart != (ready ? PartId.Receiver : PartId.Springboard) ||
            puzzle.InventoryCount != 1 || puzzle.Goal.Kind != WorkshopGoalKind.Captured ||
            puzzle.BallAssistance != (ready ? ReadyAssistance : FirstPrinciples.BallAssistance) ||
            puzzle.ReceiverAssistance != (ready ? ReadyReceiverAssistance : FirstPrinciples.ReceiverAssistance) ||
            puzzle.RampAssistance != (ready ? ReadyAssistance : SpringAssistance) || construction.Connections.Count != 0 ||
            construction.Ball != Ball(puzzle.Id, puzzle.Goal.Body))
            throw new ArgumentException("Springboard lesson declarations changed.");
        var boards = construction.Instances.OfType<WorkshopSpringboard>().ToArray();
        var receivers = construction.Instances.OfType<WorkshopReceiver>().ToArray();
        if (construction.Instances.Any(x => x is not (WorkshopBall or WorkshopSpringboard or WorkshopReceiver)) ||
            boards.Length > 1 || receivers.Length > 1)
            throw new ArgumentException("Springboard lesson inventory exceeded.");
        if (ready)
        {
            if (boards.Length != 1 || boards[0] != ChargedBoard(boards[0].Id))
                throw new ArgumentException("The precharged fixture changed.");
            if (receivers.Length == 1 &&
                (receivers[0].Locked || receivers[0].Id != puzzle.Goal.Target ||
                 receivers[0] != ReceiverSettings(receivers[0], puzzle.Id, puzzle.Precision)))
                throw new ArgumentException("The placed Receiver has invalid lesson settings.");
        }
        else if (receivers.Length != 1 || receivers[0] != FixedReceiver(puzzle.Goal.Target, puzzle.Precision) ||
                 boards.Any(x => x.Locked))
            throw new ArgumentException("The passive lesson fixtures changed.");
        var target = WorkshopInput.Receiver(puzzle.Goal.Target, 0, 0, 0, 0, 0, 0, 1);
        if (puzzle.Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(target))
            throw new ArgumentException("The named capture target changed.");
    }
}
