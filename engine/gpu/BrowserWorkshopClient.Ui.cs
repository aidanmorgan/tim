using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>Declared UI bindings on the shared worker path: player controls queue behind the single lease; the goal follows its committed occurrence.</summary>
public sealed partial class BrowserWorkshopClient
{
    private readonly record struct UiRequest(WorkshopUiTarget Target, AnimationControlKind Kind, bool Visible);
    /// <summary>Bounded by construction: at most one Reveal/Hide and one Visibility per Control-fed target.</summary>
    private readonly List<UiRequest> _uiQueue = new();
    private ulong _hintGeneration, _hintOrdinal;
    private WorkshopAnimationSample? _hintSample;
    private bool _frameHintTaken;
    /// <summary>True while the last sent control hid the target; a hidden target applies no further samples.</summary>
    private bool _uiControlHidden;
    private ActivationTime? _goalRequested;
    private WorkshopAnimationSample? _goalSample;
    private ulong _goalOrdinal;
    private bool _goalRetry, _frameGoalTaken;

    public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var declaration = UiCurveDeclaration.For(target); declaration.Validate();
        if (declaration.Feedback != WorkshopUiFeedback.Control) throw new ArgumentException("UI target follows committed feedback, not a control.");
        if (kind is not (AnimationControlKind.Reveal or AnimationControlKind.Hide or AnimationControlKind.Visibility))
            throw new ArgumentException("UI control requires its autonomous kind.");
        // A Reveal/Hide opens a new generation and supersedes everything queued for the target; a Visibility change replaces the queued one.
        _uiQueue.RemoveAll(request => request.Target == target && (kind != AnimationControlKind.Visibility || request.Kind == AnimationControlKind.Visibility));
        _uiQueue.Add(new(target, kind, visible));
        PumpUiControls();
    }
    private void PumpUi() { PumpGoal(); PumpUiControls(); }
    private void PumpUiControls()
    {
        while (_uiQueue.Count != 0)
        {
            // Queued until a schedule is installed and the lease is free; the queue stays bounded by its supersede rule.
            if (_schedule is not { } schedule || _animationPending is not null || _preparation is not null) return;
            var request = _uiQueue[0];
            var declaration = UiCurveDeclaration.For(request.Target);
            var generation = request.Kind is AnimationControlKind.Reveal or AnimationControlKind.Hide ? checked(_hintGeneration + 1) : _hintGeneration;
            _uiQueue.RemoveAt(0);
            if (generation == 0) continue; // Visibility before the first Reveal has no registered target.
            var control = new WorkshopAnimationControl(declaration.AnimationTarget, default, checked(_animationSequence + 1), generation,
                request.Kind, request.Visible, request.Kind == AnimationControlKind.Hide ? (Half)1 : (Half)0, (Half)1, declaration.Duration, declaration.Curve);
            // The generation is owned with the lease; a faulted send faults the transport rather than reviving an older clip.
            _hintGeneration = generation;
            _uiControlHidden = request.Kind == AnimationControlKind.Hide || !request.Visible;
            if (request.Kind != AnimationControlKind.Visibility) _hintSample = null;
            SendAnimation(control, schedule);
        }
    }
    private void ReceiveHintSample(WorkshopAnimationSample sample, AnimationOutputKind kind)
    {
        if (sample.Property != AnimationProperty.Opacity || sample.World.Value != 0 || sample.EventOrdinal != 0 || sample.EventPhase != (Half)0)
            throw new ArgumentException("Autonomous output carried a world event.");
        if (sample.Generation < _hintGeneration) return;
        if (sample.Generation != _hintGeneration) throw new ArgumentException("Future UI generation.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _hintOrdinal) throw new ArgumentException("Reversed UI publication.");
            _hintOrdinal = sample.Pulse.Value;
        }
        _hintSample = sample;
    }
    private void RetireGoalFeedback()
    { _goalRequested = null; _goalSample = null; _goalOrdinal = 0; _goalRetry = false; }
    private void RetryRejectedGoal(WorkshopAnimationControl command)
    {
        if (command.Target == UiCurves.Goal.AnimationTarget && command.World == Epoch && _goalRequested is { } occurrence &&
            command.EventOrdinal == occurrence.Ordinal && HalfBits.Equal(command.EventPhase, occurrence.Phase))
            _goalRetry = true;
    }
    private void PumpGoal()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration ||
            (_goalRequested is not null && !_goalRetry)) return;
        if (WorkshopGoalEvaluator.Occurrence(_construction.Puzzle.Goal, latest.Read, Epoch) is not { } occurrence) return;
        var declaration = UiCurves.Goal;
        SendAnimation(new(declaration.AnimationTarget, Epoch, checked(_animationSequence + 1), 1,
            AnimationControlKind.Endpoint, true, (Half)1, (Half)1, declaration.Duration,
            declaration.Curve, occurrence.Ordinal, occurrence.Phase), schedule);
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
    public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out AnimationOpacity opacity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed(); PumpCosmetics();
        opacity = default;
        var declaration = UiCurveDeclaration.For(target); declaration.Validate();
        if (!AdmitPresentationFrame(frame)) return false;
        return declaration.Feedback switch
        {
            WorkshopUiFeedback.Control => TryUiControlSample(target, out opacity),
            WorkshopUiFeedback.Goal => TryUiGoalSample(physical, out opacity),
            _ => throw new ArgumentException("Unknown UI feedback.")
        };
    }
    /// <summary>Control-fed targets share the hint clip state; a target the last control hid applies no further samples.</summary>
    private bool TryUiControlSample(WorkshopUiTarget target, out AnimationOpacity opacity)
    {
        opacity = default;
        if (target != WorkshopUiTarget.Hint || _uiControlHidden || _frameHintTaken ||
            _hintSample is not { } sample || sample.AppliedAt.Value > _frameMaster.Value) return false;
        _frameHintTaken = true; _hintSample = null; _lastPresentationApplied = _frameMaster;
        opacity = new(sample.Value); return true;
    }
    private bool TryUiGoalSample(WorkshopPresentationSample physical, out AnimationOpacity opacity)
    {
        opacity = default;
        if (_frameGoalTaken || physical.Evidence.WorldEpoch != Epoch || physical.FeedbackCapture is null ||
            _goalSample is not { } sample || sample.World != Epoch || sample.AppliedAt.Value > _frameMaster.Value ||
            _goalRequested is not { } occurrence ||
            WorkshopGoalEvaluator.Occurrence(_construction.Puzzle.Goal, physical.Captures, physical.Activations) != occurrence)
            return false;
        _frameGoalTaken = true; _goalSample = null; _lastPresentationApplied = _frameMaster;
        opacity = new(sample.Value); return true;
    }
}
