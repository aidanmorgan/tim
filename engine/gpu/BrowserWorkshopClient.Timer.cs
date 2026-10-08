using System;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private readonly WorkshopAnimationControl?[] _timerRequested = new WorkshopAnimationControl?[ActivationNetwork.Capacity];
    private readonly WorkshopAnimationSample?[] _timerSamples = new WorkshopAnimationSample?[ActivationNetwork.Capacity];
    private readonly bool[] _timerRetry = new bool[ActivationNetwork.Capacity];
    private readonly ulong[] _timerOrdinals = new ulong[ActivationNetwork.Capacity];
    private static AnimationTargetId TimerTarget(ActivationNodeId node) => new(checked((1UL << 32) + node.Value));
    private void RetireTimerFeedback()
    { Array.Clear(_timerRequested); Array.Clear(_timerSamples); Array.Clear(_timerOrdinals); Array.Clear(_timerRetry); }
    private void PumpTimers()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration) return;
        for (var i = 0; i < latest.Read.Timers.Count; i++)
        {
            var timer = latest.Read.Timers[i];
            var phase = timer.Phase switch
            {
                ActivationTimerPhase.Ready => AnimationTimerPhase.Ready,
                ActivationTimerPhase.Counting => AnimationTimerPhase.Counting,
                ActivationTimerPhase.Finished => AnimationTimerPhase.Finished,
                _ => throw new ArgumentException("Unknown committed timer phase.")
            };
            // A Ready timer is its declared neutral; only committed intervals need a worker control.
            if (phase == AnimationTimerPhase.Ready ||
                !TryCosmeticDeclaration(new(timer.Node.Value), AnimationFeedbackSource.Timer, out var declaration)) continue;
            var observation = new AnimationTimerObservation(timer.StartedTick, timer.DueTick, latest.Read.Tick.Value,
                timer.Input.Emitter.Value, phase);
            // One control per committed interval and phase; a newer observation tick alone is not a new interval.
            if (_timerRequested[i] is { } prior && prior.Timer == observation with { Observed = prior.Timer.Observed } && !_timerRetry[i]) continue;
            var rate = schedule.Settings.SimulationRate;
            var duration = (Half)Math.Min(30, (timer.DueTick - timer.StartedTick) * (double)rate.Denominator / rate.Numerator);
            var control = new WorkshopAnimationControl(TimerTarget(timer.Node), latest.Read.Epoch,
                checked(_hintSequence + 1), checked(latest.Read.Tick.Value + 1), AnimationControlKind.TimerObservation,
                true, (Half)0, (Half)1, duration, declaration.Curve,
                timer.Input.Time.Ordinal, timer.Input.Time.Phase, AnimationProperty.ColourBlend, observation);
            SendAnimation(control, schedule); _timerRequested[i] = control; _timerRetry[i] = false; return;
        }
    }
    private void RetryRejectedTimer(WorkshopAnimationControl rejected)
    {
        for (var i = 0; i < _timerRequested.Length; i++)
            if (_timerRequested[i] == rejected) _timerRetry[i] = true;
    }
    private void ReceiveTimerSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        var slot = -1;
        for (var i = 0; i < _timerRequested.Length; i++)
            if (_timerRequested[i] is { } requested && requested.Target == sample.Target) slot = i;
        if (slot < 0 || _timerRequested[slot] is not { } control) throw new ArgumentException("Unowned timer sample.");
        if (sample.Generation < control.Generation) return;
        if (sample.World != active.World.WorldGeneration || sample.Property != AnimationProperty.ColourBlend ||
            sample.Generation != control.Generation || sample.Timer != control.Timer ||
            sample.EventOrdinal != control.EventOrdinal || !HalfBits.Equal(sample.EventPhase, control.EventPhase))
            throw new ArgumentException("Timer animation does not own its committed interval.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _timerOrdinals[slot]) throw new ArgumentException("Reversed timer publication.");
            _timerOrdinals[slot] = sample.Pulse.Value;
        }
        _timerSamples[slot] = sample;
    }
    private bool TryTimerSample(WorkshopPresentationSample physical, ActivationNodeId node, out WorkshopCosmeticSample result)
    {
        if (physical.Evidence.WorldEpoch == Epoch)
            for (var i = 0; i < physical.Timers.Count; i++)
            {
                var timer = physical.Timers[i];
                if (timer.Node != node || _timerSamples[i] is not { } sample || sample.World != Epoch ||
                    sample.AppliedAt.Value > _frameMaster.Value || sample.Timer.Started != timer.StartedTick ||
                    sample.Timer.Due != timer.DueTick || sample.Timer.InputEmitter != timer.Input.Emitter.Value ||
                    sample.EventOrdinal != timer.Input.Time.Ordinal || !HalfBits.Equal(sample.EventPhase, timer.Input.Time.Phase))
                    continue;
                result = new(sample.Value, sample.Timer.Phase);
                _lastPresentationApplied = _frameMaster; return true;
            }
        result = default; return false;
    }
}
