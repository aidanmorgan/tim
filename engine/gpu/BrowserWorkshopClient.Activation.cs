using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private readonly ActivationLatch?[] _activationRequested = new ActivationLatch?[ActivationNetwork.Capacity];
    private readonly WorkshopAnimationSample?[] _activationSamples = new WorkshopAnimationSample?[ActivationNetwork.Capacity];
    private readonly ulong[] _activationOrdinals = new ulong[ActivationNetwork.Capacity];
    private readonly bool[] _activationFrameTaken = new bool[ActivationNetwork.Capacity];

    private static AnimationTargetId ActivationTarget(ActivationNodeId node) => new(checked(node.Value + 2));
    private void RetireActivationFeedback()
    {
        RetireTimerFeedback();
        Array.Clear(_activationRequested); Array.Clear(_activationSamples); Array.Clear(_activationOrdinals);
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
                checked(_hintSequence + 1), 1, AnimationControlKind.Endpoint, true, (Half)1, (Half)1, declaration.Duration,
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
