using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

public enum WorkshopPlacementMode : uint { Manual }
public enum WorkshopGoalKind : uint { None, Captured, ActivatedAfter }
public readonly record struct PuzzlePrecision(float Value);
public readonly record struct WorkshopGoal(WorkshopGoalKind Kind, GpuBodyId Body, GpuBodyId Target,
    ActivationNodeId SourceNode = default, ActivationNodeId TargetNode = default, DurationSeconds MinimumDuration = default)
{
    public void Validate()
    {
        switch (Kind)
        {
            case WorkshopGoalKind.None when this == default: return;
            case WorkshopGoalKind.Captured when Body.Value != 0 && Target.Value != 0 && Body != Target &&
                SourceNode == default && TargetNode == default && MinimumDuration == default: return;
            case WorkshopGoalKind.ActivatedAfter when Body == default && Target == default &&
                SourceNode.Value != 0 && TargetNode.Value != 0 && SourceNode != TargetNode:
                PhysicsDeclarationBounds.Range(MinimumDuration.Value, 0f, 120f); return;
            default: throw new ArgumentException("Unsupported named goal.");
        }
    }
}

/// <summary>Canonical authored assistance, including physical nudging data retained for a later supported mode.</summary>
public readonly record struct AssistanceKnot(float Precision, Metres PositionWindow, float RotationWindowDegrees,
    Metres MaximumPositionCorrection, float MaximumRotationCorrectionDegrees, DurationSeconds Blend,
    Metres CaptureMargin, LinearSpeed CaptureSpeed, DurationSeconds CaptureDwell, Acceleration GuideAcceleration,
    float TriggerThreshold);
public readonly record struct AssistanceProfile(AssistanceKnot Forgiving, AssistanceKnot Balanced, AssistanceKnot Precise)
{
    /// <summary>The authored knot rule (content and saves alike): no profile, or every lane finite and non-negative with its knots at
    /// precisions exactly 0, 0.45 and 1.</summary>
    internal bool HasAuthoredShape()
    {
        if (this == default) return true;
        static bool Admitted(AssistanceKnot k) => new[]
        {
            k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value, k.MaximumRotationCorrectionDegrees,
            k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value, k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold
        }.All(lane => float.IsFinite(lane) && lane >= 0f);
        return Admitted(Forgiving) && Admitted(Balanced) && Admitted(Precise) &&
            Forgiving.Precision == 0f && Balanced.Precision == .45f && Precise.Precision == 1f;
    }

    public AssistanceKnot Evaluate(PuzzlePrecision precision)
    {
        PhysicsDeclarationBounds.Range(precision.Value, 0f, 1f);
        if (precision.Value == Forgiving.Precision) return Forgiving;
        if (precision.Value == Balanced.Precision) return Balanced;
        if (precision.Value == Precise.Precision) return Precise;
        var a = precision.Value < Balanced.Precision ? Forgiving : Balanced;
        var b = precision.Value < Balanced.Precision ? Balanced : Precise;
        // f32 linear blend between the bracketing knots: each operation rounds once to the nearest f32.
        var t = (precision.Value - a.Precision) / (b.Precision - a.Precision);
        float Mix(float x, float y) => x + (y - x) * t;
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
