using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Authored joined_pipe lesson ("Meet in the middle", content/puzzles.json): the granted pipe, shortened to fit and snapped
/// to the fixed bend's upper mouth, carries the falling ball into the receiver. Every motion runs on generic physics.</summary>
public static class JoinedPipe
{
    /// <summary>The locked ball and the locked bend repeat the same source knots.</summary>
    public static AssistanceProfile FixtureAssistance => FirstPrinciples.BallAssistance;

    /// <summary>The granted pipe's knots (content solution pipe_1).</summary>
    public static AssistanceProfile PipeAssistance => FirstPrinciples.BallAssistance with
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
        WorkshopInput.Basketball(id, -1.3081, 8.402188, 0, 0, 0, 0, 1) with { Locked = true };

    /// <summary>The fixed 90° bend at (1, 3.5, 0), turned −45° about Z: its upper mouth faces up and left toward the drop line.</summary>
    public static WorkshopGenericInstance Bend(GpuBodyId id) =>
        WorkshopInput.PipeBend90(id, 1, 3.5, 0, 0, 0, -0.38268343236508984, 0.9238795325112867) with { Locked = true };

    public static WorkshopReceiver Receiver(GpuBodyId id, PuzzlePrecision precision)
    {
        var assistance = FirstPrinciples.ReceiverAssistance.Evaluate(precision);
        return WorkshopInput.Receiver(id, -0.2, 0.6, 0, 0, 0, 0, 1) with
        {
            Locked = true,
            Capture = new(assistance.CaptureMargin, assistance.CaptureSpeed, assistance.CaptureDwell, SensorParticipation.Enabled),
            ForceRegion = ReceiverForceRegion.Create(assistance.CaptureMargin, assistance.GuideAcceleration)
        };
    }

    public static WorkshopConstruction Create(ConstructionRevision revision, WorkshopCadenceSettings settings,
        GpuBodyId ball, GpuBodyId bend, GpuBodyId receiver, PuzzlePrecision precision)
    {
        var target = Receiver(receiver, precision);
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.JoinedPipe, WorkshopPlacementMode.Manual,
            precision, PartId.Pipe, 1,
            new(WorkshopGoalKind.Captured, ball, receiver, WorkshopPhysicsCompiler.CaptureSensor(target)),
            FirstPrinciples.BallAssistance, FirstPrinciples.ReceiverAssistance, PipeAssistance);
        var construction = new WorkshopConstruction(revision, settings, new(Ball(ball), Bend(bend), target), puzzle);
        construction.Validate(); return construction;
    }

    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.JoinedPipe) throw new ArgumentException("Not the joined pipe lesson.");
        var next = construction.WithInstance(Receiver(construction.Puzzle.Goal.Target, precision)) with
            { Puzzle = construction.Puzzle with { Precision = precision } };
        next.Validate(); return next;
    }

    /// <summary>The fixed bend is exactly the authored declaration: its pose, its lock and its compound colliders.</summary>
    private static bool IsFixture(WorkshopGenericInstance bend)
    {
        var expected = Bend(bend.Id);
        return bend.Locked && bend.PartId == expected.PartId && bend.Cell == expected.Cell && bend.Local == expected.Local &&
            bend.Rotation == expected.Rotation && bend.DeclaredColliders.SequenceEqual(expected.DeclaredColliders);
    }

    public static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, (Half)0, (Half)1);
        if (puzzle.Placement != WorkshopPlacementMode.Manual || puzzle.InventoryPart != PartId.Pipe ||
            puzzle.InventoryCount != 1 || puzzle.Goal.Kind != WorkshopGoalKind.Captured ||
            puzzle.BallAssistance != FirstPrinciples.BallAssistance ||
            puzzle.ReceiverAssistance != FirstPrinciples.ReceiverAssistance || puzzle.RampAssistance != PipeAssistance ||
            construction.Connections.Count != 0)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.PuzzleSettings, "Unsupported joined pipe authored settings.");
        var receiver = Receiver(puzzle.Goal.Target, puzzle.Precision);
        var fixtures = construction.Instances.Count(instance => instance is WorkshopGenericInstance { PartId.Value: "pipe_bend_90", Locked: true });
        if (construction.Ball != Ball(puzzle.Goal.Body) || construction.Receiver != receiver ||
            puzzle.Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(receiver) || fixtures != 1 ||
            !construction.Instances.OfType<WorkshopGenericInstance>().Any(instance => instance.PartId == PartId.PipeBend90 && IsFixture(instance)))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.FixedPart, "Fixed joined pipe declarations changed.");
        var receivers = 0; var pipes = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopBall or WorkshopReceiver or WorkshopPipe { Locked: false } or
                WorkshopGenericInstance { PartId.Value: "pipe_bend_90", Locked: true }))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InventoryUnsupported, "Unsupported joined pipe inventory.");
            if (instance is WorkshopReceiver) receivers++;
            if (instance is WorkshopPipe) pipes++;
        }
        if (receivers != 1 || pipes > puzzle.InventoryCount)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InventoryExceeded, "Joined pipe inventory exceeded.");
    }
}
