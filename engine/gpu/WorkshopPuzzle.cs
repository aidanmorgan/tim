using System;

namespace CuriousContraptions.Gpu;

public enum WorkshopPuzzleId : uint { Free, FirstPrinciples, DelayedSignal, DominoEffect, BumperSidekick, BumperDepth, WallAndBumper }
public enum WorkshopPlacementMode : uint { Manual }
public enum WorkshopGoalKind : uint { None, Captured, ActivatedAfter }
public readonly record struct PuzzlePrecision(Half Value);
public readonly record struct WorkshopGoal(WorkshopGoalKind Kind, GpuBodyId Body, GpuBodyId Target, GpuSensorId EventSource,
    ActivationNodeId SourceNode = default, ActivationNodeId TargetNode = default, DurationSeconds MinimumDelay = default)
{
    public void Validate()
    {
        switch (Kind)
        {
            case WorkshopGoalKind.None when this == default: return;
            case WorkshopGoalKind.Captured when Body.Value != 0 && Target.Value != 0 && Body != Target &&
                EventSource.Value != 0 && SourceNode == default && TargetNode == default && MinimumDelay == default: return;
            case WorkshopGoalKind.ActivatedAfter when Body == default && Target == default && EventSource == default &&
                SourceNode.Value != 0 && TargetNode.Value != 0 && SourceNode != TargetNode:
                PhysicsDeclarationBounds.Range(MinimumDelay.Value, (Half)0, (Half)120); return;
            default: throw new ArgumentException("Unsupported named goal.");
        }
    }
}

/// <summary>Canonical authored assistance, including physical nudging data retained for a later supported mode.</summary>
public readonly record struct AssistanceKnot(Half Precision, Metres PositionWindow, Half RotationWindowDegrees,
    Metres MaximumPositionCorrection, Half MaximumRotationCorrectionDegrees, DurationSeconds Blend,
    Metres CaptureMargin, LinearSpeed CaptureSpeed, DurationSeconds CaptureDwell, Acceleration GuideAcceleration,
    Half TriggerThreshold);
public readonly record struct AssistanceProfile(AssistanceKnot Forgiving, AssistanceKnot Balanced, AssistanceKnot Precise)
{
    public AssistanceKnot Evaluate(PuzzlePrecision precision)
    {
        PhysicsDeclarationBounds.Range(precision.Value, (Half)0, (Half)1);
        if (precision.Value == Forgiving.Precision) return Forgiving;
        if (precision.Value == Balanced.Precision) return Balanced;
        if (precision.Value == Precise.Precision) return Precise;
        var a = precision.Value < Balanced.Precision ? Forgiving : Balanced;
        var b = precision.Value < Balanced.Precision ? Balanced : Precise;
        var t = (Half)((Half)(precision.Value - a.Precision) / (Half)(b.Precision - a.Precision));
        Half Mix(Half x, Half y) => (Half)(x + (Half)((Half)(y - x) * t));
        return new(precision.Value, new(Mix(a.PositionWindow.Value, b.PositionWindow.Value)),
            Mix(a.RotationWindowDegrees, b.RotationWindowDegrees), new(Mix(a.MaximumPositionCorrection.Value, b.MaximumPositionCorrection.Value)),
            Mix(a.MaximumRotationCorrectionDegrees, b.MaximumRotationCorrectionDegrees), new(Mix(a.Blend.Value, b.Blend.Value)),
            new(Mix(a.CaptureMargin.Value, b.CaptureMargin.Value)), new(Mix(a.CaptureSpeed.Value, b.CaptureSpeed.Value)),
            new(Mix(a.CaptureDwell.Value, b.CaptureDwell.Value)), new(Mix(a.GuideAcceleration.Value, b.GuideAcceleration.Value)),
            Mix(a.TriggerThreshold, b.TriggerThreshold));
    }
}

public readonly record struct WorkshopPuzzle(WorkshopPuzzleId Id, WorkshopPlacementMode Placement,
    PuzzlePrecision Precision, WorkshopPartKind InventoryKind, uint InventoryCount, WorkshopGoal Goal, AssistanceProfile BallAssistance,
    AssistanceProfile ReceiverAssistance, AssistanceProfile RampAssistance)
{
    public AssistanceProfile WallAssistance => Id == WorkshopPuzzleId.WallAndBumper ? BumperAdvanced.WallAssistance : default;

    public void Validate(WorkshopConstruction construction)
    {
        if (!Enum.IsDefined(Id) || Placement != WorkshopPlacementMode.Manual) throw new ArgumentException("Unsupported puzzle or placement mode.");
        if (Id == WorkshopPuzzleId.Free)
        {
            if (this != default) throw new ArgumentException("Free Workshop has no authored puzzle settings.");
            return;
        }
        // Authored puzzles name the Basketball kind explicitly; no other ball kind is offered or admitted.
        var ballCount = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not WorkshopBall ball) continue;
            if (ball.Kind != WorkshopPartKind.Basketball) throw new ArgumentException("Authored puzzles admit only their named Basketball.");
            ballCount++;
        }
        if (ballCount != 1) throw new ArgumentException("Authored puzzles require their one named Basketball.");
        Goal.Validate();
        if (Id == WorkshopPuzzleId.DelayedSignal) { DelayedSignal.Validate(this, construction); return; }
        if (Id == WorkshopPuzzleId.BumperSidekick) { BumperSidekick.Validate(this, construction); return; }
        if (Id is WorkshopPuzzleId.BumperDepth or WorkshopPuzzleId.WallAndBumper) { BumperAdvanced.Validate(this, construction); return; }
        if (Id == WorkshopPuzzleId.DominoEffect) { DominoEffect.Validate(this, construction); return; }
        PhysicsDeclarationBounds.Range(Precision.Value, (Half)0, (Half)1);
        if (construction.Connections.Count != 0 || InventoryKind != WorkshopPartKind.Ramp || InventoryCount != 2 || Goal.Kind != WorkshopGoalKind.Captured || Goal.Body.Value == 0 || Goal.Target.Value == 0 || Goal.Body == Goal.Target ||
            BallAssistance != FirstPrinciples.BallAssistance || ReceiverAssistance != FirstPrinciples.ReceiverAssistance || RampAssistance != FirstPrinciples.RampAssistance)
            throw new ArgumentException("Unsupported First principles authored settings.");
        var expectedBall = FirstPrinciples.Ball(Goal.Body);
        var expectedReceiver = FirstPrinciples.Receiver(Goal.Target, Precision);
        if (construction.Ball != expectedBall || construction.Receiver != expectedReceiver ||
            Goal.EventSource != WorkshopPhysicsCompiler.CaptureSensor(expectedReceiver))
            throw new ArgumentException("Fixed puzzle declarations changed.");
        var receivers = 0; var ramps = 0;
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopBall or WorkshopReceiver or WorkshopRamp) || instance is WorkshopRamp { Locked: true })
                throw new ArgumentException("Unsupported puzzle inventory instance.");
            if (instance is WorkshopReceiver) receivers++;
            if (instance is WorkshopRamp) ramps++;
        }
        if (receivers != 1) throw new ArgumentException("Authored puzzles require their one named Receiver.");
        if (ramps > InventoryCount) throw new ArgumentException("Ramp inventory exhausted.");
    }
}

/// <summary>Typed authoring of content/puzzles.json:first_principles. No solution placement or numerical law lives here.</summary>
public static class FirstPrinciples
{
    public static AssistanceProfile BallAssistance => new(
        Knot((Half)0, (Half)3, (Half)100, (Half)0, (Half)0, (Half).3, (Half)3, (Half).15, (Half)0, (Half).2),
        Knot((Half).45, (Half)1.5, (Half)45, (Half)0, (Half)0, (Half).174, (Half)2.325, (Half).24, (Half)0, (Half).47),
        Knot((Half)1, (Half)0, (Half)0, (Half)0, (Half)0, (Half).02, (Half)1.5, (Half).35, (Half)0, (Half).8));
    public static AssistanceProfile ReceiverAssistance => BallAssistance with
    {
        Forgiving = BallAssistance.Forgiving with { GuideAcceleration = new((Half)12) },
        Balanced = BallAssistance.Balanced with { GuideAcceleration = new((Half)6.6) }
    };
    public static AssistanceProfile RampAssistance => BallAssistance with
    {
        Forgiving = BallAssistance.Forgiving with { MaximumPositionCorrection = new((Half).3), MaximumRotationCorrectionDegrees = (Half)5 },
        Balanced = BallAssistance.Balanced with { MaximumPositionCorrection = new((Half).120000005), MaximumRotationCorrectionDegrees = (Half)2 }
    };
    private static AssistanceKnot Knot(Half precision, Half position, Half rotation, Half maximumPosition, Half maximumRotation,
        Half margin, Half speed, Half dwell, Half guide, Half trigger) =>
        new(precision, new(position), rotation, new(maximumPosition), maximumRotation, new((Half).4), new(margin), new(speed), new(dwell), new(guide), trigger);
    public static WorkshopBall Ball(GpuBodyId id) => WorkshopInput.Basketball(id, -4, 6.5, 0, 0, 0, 0, 1) with { Locked = true };
    public static WorkshopReceiver Receiver(GpuBodyId id, PuzzlePrecision precision)
    {
        var assistance = ReceiverAssistance.Evaluate(precision);
        return WorkshopInput.Receiver(id, 2.5, .9, 0, 0, 0, 0, 1) with
        {
            Locked = true,
            Capture = new(assistance.CaptureMargin, assistance.CaptureSpeed, assistance.CaptureDwell, SensorParticipation.Enabled),
            ForceRegion = ReceiverForceRegion.Create(assistance.CaptureMargin, assistance.GuideAcceleration)
        };
    }
    public static WorkshopConstruction Create(ConstructionRevision revision, WorkshopCadenceSettings settings, GpuBodyId ball, GpuBodyId receiver, PuzzlePrecision precision)
    {
        var puzzle = new WorkshopPuzzle(WorkshopPuzzleId.FirstPrinciples, WorkshopPlacementMode.Manual, precision, WorkshopPartKind.Ramp, 2,
            new(WorkshopGoalKind.Captured, ball, receiver, WorkshopPhysicsCompiler.CaptureSensor(Receiver(receiver, precision))), BallAssistance, ReceiverAssistance, RampAssistance);
        var construction = new WorkshopConstruction(revision, settings, new(Ball(ball), Receiver(receiver, precision)), puzzle);
        construction.Validate(); return construction;
    }
    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        if (construction.Puzzle.Id != WorkshopPuzzleId.FirstPrinciples) throw new ArgumentException("Construction has no authored assistance.");
        var next = construction.WithInstance(Receiver(construction.Puzzle.Goal.Target, precision)) with
            { Puzzle = construction.Puzzle with { Precision = precision } };
        next.Validate(); return next;
    }
}
