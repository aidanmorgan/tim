using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private readonly ActivationLatch?[] _activationRequested = new ActivationLatch?[ActivationNetwork.Capacity];
    private readonly WorkshopAnimationSample?[] _activationSamples = new WorkshopAnimationSample?[ActivationNetwork.Capacity];
    private readonly ulong[] _activationOrdinals = new ulong[ActivationNetwork.Capacity];
    private readonly bool[] _activationFrameTaken = new bool[ActivationNetwork.Capacity];
    // Bistable Memory feedback (State Latch): one Transition per committed On/Off change, on the node's own activation target.
    private readonly WorkshopAnimationControl?[] _memoryRequested = new WorkshopAnimationControl?[ActivationNetwork.Capacity];
    private readonly WorkshopAnimationSample?[] _memorySamples = new WorkshopAnimationSample?[ActivationNetwork.Capacity];
    private readonly bool[] _memoryRetry = new bool[ActivationNetwork.Capacity];
    private readonly ulong[] _memoryOrdinals = new ulong[ActivationNetwork.Capacity];

    private static AnimationTargetId ActivationTarget(ActivationNodeId node) => new(checked(node.Value + 2));
    private void RetireActivationFeedback()
    {
        RetireTimerFeedback(); RetireElectricalFeedback();
        Array.Clear(_activationRequested); Array.Clear(_activationSamples); Array.Clear(_activationOrdinals);
        Array.Clear(_memoryRequested); Array.Clear(_memorySamples); Array.Clear(_memoryRetry); Array.Clear(_memoryOrdinals);
    }
    /// <summary>Drives a Memory cosmetic toward the committed value: Latched → 1 (On), Clear → 0 (Off). Off at admission needs no control.</summary>
    private void PumpMemory()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration) return;
        for (var i = 0; i < latest.Read.Activations.Count; i++)
        {
            var value = latest.Read.Activations[i];
            if (!TryCosmeticDeclaration(value.Owner, AnimationFeedbackSource.Memory, out var declaration)) continue;
            var to = value.Phase == ActivationPhase.Latched ? (Half)1 : (Half)0;
            if (_memoryRequested[i] is { } previous ? previous.To == to && !_memoryRetry[i] : to == (Half)0) continue;
            var from = _memorySamples[i]?.Value ?? (Half)1 - to;
            var control = new WorkshopAnimationControl(ActivationTarget(value.Node), latest.Read.Epoch, checked(_animationSequence + 1),
                (_memoryRequested[i]?.Generation ?? 0) + 1, AnimationControlKind.Transition, true, from, to, declaration.Duration,
                declaration.Curve, latest.Read.Motion?.LastOrdinal ?? 0, default, AnimationProperty.ColourBlend);
            SendAnimation(control, schedule);
            _memoryRequested[i] = control; _memoryRetry[i] = false;
            return; // The single reliable Animation lease owns this control until its ACK.
        }
    }
    private int MemorySlot(AnimationTargetId target)
    {
        for (var i = 0; i < _memoryRequested.Length; i++)
            if (_memoryRequested[i] is { } control && control.Target == target) return i;
        return -1;
    }
    private bool IsMemoryTarget(AnimationTargetId target) => MemorySlot(target) >= 0;
    private void RetryMemoryFeedback(WorkshopAnimationControl rejected)
    {
        var slot = MemorySlot(rejected.Target);
        if (slot >= 0 && _memoryRequested[slot] == rejected) _memoryRetry[slot] = true;
    }
    private void ReceiveMemorySample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        var slot = MemorySlot(sample.Target);
        if (slot < 0 || _memoryRequested[slot] is not { } control) throw new ArgumentException("Unowned memory animation.");
        if (sample.Generation < control.Generation) return;
        if (sample.World != active.World.WorldGeneration || sample.Generation != control.Generation ||
            sample.Property != AnimationProperty.ColourBlend || sample.EventOrdinal != control.EventOrdinal ||
            !HalfBits.Equal(sample.EventPhase, control.EventPhase))
            throw new ArgumentException("Memory animation observation changed.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _memoryOrdinals[slot]) throw new ArgumentException("Memory animation reversed.");
            _memoryOrdinals[slot] = sample.Pulse.Value;
        }
        _memorySamples[slot] = sample;
    }
    private bool TryMemorySample(WorkshopPresentationSample physical, ActivationNodeId node, out WorkshopCosmeticSample result)
    {
        if (physical.Evidence.WorldEpoch == Epoch)
            for (var i = 0; i < physical.Activations.Count; i++)
            {
                if (physical.Activations[i].Node != node || _memorySamples[i] is not { } sample || sample.World != Epoch ||
                    sample.AppliedAt.Value > _frameMaster.Value) continue;
                _lastPresentationApplied = _frameMaster;
                result = new(sample.Value, AnimationTimerPhase.None); return true;
            }
        result = default; return false;
    }
    private void PumpActivations()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration) return;
        for (var i = 0; i < latest.Read.Activations.Count; i++)
        {
            var activation = latest.Read.Activations[i];
            if (activation.Phase != ActivationPhase.Latched || _activationRequested[i] is not null ||
                !TryCosmeticDeclaration(activation.Owner, AnimationFeedbackSource.Activation, out var declaration)) continue;
            var control = new WorkshopAnimationControl(ActivationTarget(activation.Node), latest.Read.Epoch,
                checked(_animationSequence + 1), 1, AnimationControlKind.Endpoint, true, (Half)1, (Half)1, declaration.Duration,
                declaration.Curve, activation.EventOrdinal, activation.EventPhase, AnimationProperty.ColourBlend);
            SendAnimation(control, schedule);
            _activationRequested[i] = activation;
            return; // The existing single reliable Animation lease owns this control until its ACK.
        }
    }
    private void ReceiveActivationSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        var slot = -1;
        for (var i = 0; i < _activationRequested.Length; i++)
            if (_activationRequested[i] is { } requested && ActivationTarget(requested.Node) == sample.Target) slot = i;
        if (slot < 0 || _activationRequested[slot] is not { } activation ||
            sample.World != active.World.WorldGeneration || sample.Property != AnimationProperty.ColourBlend ||
            sample.Generation != 1 || sample.EventOrdinal != activation.EventOrdinal ||
            !HalfBits.Equal(sample.EventPhase, activation.EventPhase))
            throw new ArgumentException("Activation animation does not own its committed occurrence.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _activationOrdinals[slot]) throw new ArgumentException("Reversed activation animation publication.");
            _activationOrdinals[slot] = sample.Pulse.Value;
        }
        _activationSamples[slot] = sample;
    }
    private bool TryActivationSample(WorkshopPresentationSample physical, ActivationNodeId node, out WorkshopCosmeticSample result)
    {
        if (physical.Evidence.WorldEpoch == Epoch && physical.FeedbackCapture is not null)
            for (var i = 0; i < physical.Activations.Count; i++)
            {
                var activation = physical.Activations[i];
                if (activation.Node != node || _activationFrameTaken[i] || _activationRequested[i] != activation ||
                    _activationSamples[i] is not { } sample || sample.World != Epoch || sample.AppliedAt.Value > _frameMaster.Value)
                    continue;
                _activationFrameTaken[i] = true;
                _lastPresentationApplied = _frameMaster;
                result = new(sample.Value, AnimationTimerPhase.None); return true;
            }
        result = default; return false;
    }
}
