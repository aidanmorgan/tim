using System;

namespace CuriousContraptions.Gpu;

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

public readonly record struct WorkshopPuzzle(PuzzleId Id, WorkshopPlacementMode Placement,
    PuzzlePrecision Precision, PartId InventoryPart, uint InventoryCount, WorkshopGoal Goal,
    AssistanceProfile PrimaryAssistance, AssistanceProfile TargetAssistance, AssistanceProfile PlacementAssistance)
{
    public AssistanceProfile SecondaryAssistance => Id.IsAuthored ? PuzzleCatalogue.Definition(Id).Secondary : default;

    public void Validate(WorkshopConstruction construction)
    {
        if (!Id.IsAuthored)
        {
            if (this != default)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.PuzzleSettings,
                    "Free Workshop has no authored settings.");
            return;
        }
        PuzzleCatalogue.Validate(this, construction);
    }
}
