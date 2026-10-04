using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private static readonly AnimationTargetId GoalTarget = new(ulong.MaxValue);
    private ActivationTime? _goalRequested;
    private WorkshopAnimationSample? _goalSample;
    private ulong _goalOrdinal;
    private bool _goalRetry, _frameGoalTaken;
    private void RetireGoalFeedback()
    { _goalRequested = null; _goalSample = null; _goalOrdinal = 0; _goalRetry = false; }
    private void RetryRejectedGoal(WorkshopAnimationControl command)
    {
        if (command.Target == GoalTarget && command.World == Epoch && _goalRequested is { } occurrence &&
            command.EventOrdinal == occurrence.Ordinal && HalfBits.Equal(command.EventPhase, occurrence.Phase))
            _goalRetry = true;
    }
    private void PumpGoal()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration ||
            (_goalRequested is not null && !_goalRetry)) return;
        if (WorkshopGoalEvaluator.Occurrence(_construction.Puzzle.Goal, latest.Read, Epoch) is not { } occurrence) return;
        SendAnimation(new(GoalTarget, Epoch, checked(_hintSequence + 1), 1,
            AnimationControlKind.Endpoint, true, (Half)1, (Half)1, (Half)1,
            AnimationCurve.Linear, occurrence.Ordinal, occurrence.Phase), schedule);
        _goalRequested = occurrence; _goalRetry = false;
    }
    private void ReceiveGoalSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        if (_goalRequested is not { } occurrence || sample.World != active.World.WorldGeneration ||
            sample.Property != AnimationProperty.Opacity || sample.Generation != 1 ||
            sample.EventOrdinal != occurrence.Ordinal || !HalfBits.Equal(sample.EventPhase, occurrence.Phase))
            throw new ArgumentException("Goal feedback does not own the committed occurrence.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _goalOrdinal) throw new ArgumentException("Reversed goal publication.");
            _goalOrdinal = sample.Pulse.Value;
        }
        _goalSample = sample;
    }
    public bool TryGoalOpacity(ulong frame, WorkshopPresentationSample physical, out Half opacity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed(); PumpGoal();
        if (!AdmitPresentationFrame(frame) || _frameGoalTaken || physical.Evidence.WorldEpoch != Epoch ||
            physical.FeedbackCapture is null || _goalSample is not { } sample || sample.World != Epoch ||
            sample.AppliedAt.Value > _frameMaster.Value || _goalRequested is not { } occurrence ||
            WorkshopGoalEvaluator.Occurrence(_construction.Puzzle.Goal, physical.Captures, physical.Activations) != occurrence)
        { opacity = default; return false; }
        _frameGoalTaken = true; _goalSample = null; _lastPresentationApplied = _frameMaster;
        opacity = sample.Value; return true;
    }
}
