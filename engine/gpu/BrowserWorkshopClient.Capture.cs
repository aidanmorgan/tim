using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>Receiver capture feedback on the shared cosmetic path: latched sensor → declared Capture curve → worker → blend.</summary>
public sealed partial class BrowserWorkshopClient
{
    private readonly CaptureLatch?[] _captureRequested = new CaptureLatch?[PhysicsSceneDeclaration.SensorCapacity];
    private readonly WorkshopAnimationSample?[] _captureSamples = new WorkshopAnimationSample?[PhysicsSceneDeclaration.SensorCapacity];
    private readonly ulong[] _captureOrdinals = new ulong[PhysicsSceneDeclaration.SensorCapacity];
    private readonly bool[] _captureFrameTaken = new bool[PhysicsSceneDeclaration.SensorCapacity];
    private const ulong CaptureTargetRange = 3;
    private static AnimationTargetId CaptureTarget(GpuBodyId owner) => new(checked((CaptureTargetRange << 32) + owner.Value));
    private static bool IsCaptureTarget(AnimationTargetId target) => target.Value >> 32 == CaptureTargetRange;
    private void RetireCaptureFeedback()
    { Array.Clear(_captureRequested); Array.Clear(_captureSamples); Array.Clear(_captureOrdinals); }
    /// <summary>Sensor → owning receiver body from the compiled scene's residence sensors (the frame body), never an assumed identity.</summary>
    private bool TryCaptureOwner(GpuSensorId sensor, out GpuBodyId owner)
    {
        owner = default;
        if (_readScene is not { } scene) return false;
        foreach (var declaration in scene.Sensors)
            if (declaration.Id == sensor) { owner = declaration.Frame; return true; }
        return false;
    }
    private bool CaptureRequestedFor(GpuBodyId owner)
    {
        foreach (var requested in _captureRequested)
            if (requested is { } latch && TryCaptureOwner(latch.Sensor, out var frame) && frame == owner) return true;
        return false;
    }
    private void PumpCapture()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration) return;
        for (var i = 0; i < latest.Read.Captures.Count; i++)
        {
            var capture = latest.Read.Captures[i];
            // A re-capture within the world is a new committed occurrence: release the slot so it requests a fresh clip.
            if (_captureRequested[i] is { } prior && prior != capture) { _captureRequested[i] = null; _captureSamples[i] = null; }
            // One halo per receiver: the first latched sensor of a receiver owns its capture target for the world.
            if (capture.Phase != CaptureLatchPhase.Latched || _captureRequested[i] is not null ||
                !TryCaptureOwner(capture.Sensor, out var owner) || CaptureRequestedFor(owner) ||
                !TryCosmeticDeclaration(owner, AnimationFeedbackSource.Capture, out var declaration)) continue;
            var control = new WorkshopAnimationControl(CaptureTarget(owner), latest.Read.Epoch,
                checked(_animationSequence + 1), 1, AnimationControlKind.Endpoint, true, (Half)1, (Half)1, declaration.Duration,
                declaration.Curve, capture.EventOrdinal, capture.EventPhase, AnimationProperty.ColourBlend);
            // The slot is owned with the lease; a faulted send faults the transport rather than re-requesting.
            _captureRequested[i] = capture;
            SendAnimation(control, schedule);
            return; // The single reliable Animation lease owns this control until its ACK.
        }
    }
    /// <summary>A rejected capture control releases its slot so the next pump requests the halo again within the world.</summary>
    private void RetryRejectedCapture(WorkshopAnimationControl command)
    {
        for (var i = 0; i < _captureRequested.Length; i++)
            if (_captureRequested[i] is { } requested && TryCaptureOwner(requested.Sensor, out var owner) && CaptureTarget(owner) == command.Target &&
                command.World == Epoch && command.EventOrdinal == requested.EventOrdinal && HalfBits.Equal(command.EventPhase, requested.EventPhase))
            { _captureRequested[i] = null; _captureSamples[i] = null; }
    }
    private void ReceiveCaptureSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        var slot = -1;
        for (var i = 0; i < _captureRequested.Length; i++)
            if (_captureRequested[i] is { } requested && TryCaptureOwner(requested.Sensor, out var owner) && CaptureTarget(owner) == sample.Target) slot = i;
        if (slot < 0 || _captureRequested[slot] is not { } capture ||
            sample.World != active.World.WorldGeneration || sample.Property != AnimationProperty.ColourBlend ||
            sample.Generation != 1 || sample.EventOrdinal != capture.EventOrdinal ||
            !HalfBits.Equal(sample.EventPhase, capture.EventPhase))
            throw new ArgumentException("Capture animation does not own its committed occurrence.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _captureOrdinals[slot]) throw new ArgumentException("Reversed capture animation publication.");
            _captureOrdinals[slot] = sample.Pulse.Value;
        }
        _captureSamples[slot] = sample;
    }
    private bool TryCaptureSample(WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample result)
    {
        if (physical.Evidence.WorldEpoch == Epoch && physical.FeedbackCapture is not null)
            for (var i = 0; i < physical.Captures.Count; i++)
            {
                var capture = physical.Captures[i];
                if (!TryCaptureOwner(capture.Sensor, out var frame) || frame != owner || _captureFrameTaken[i] || _captureRequested[i] != capture ||
                    _captureSamples[i] is not { } sample || sample.World != Epoch || sample.AppliedAt.Value > _frameMaster.Value)
                    continue;
                _captureFrameTaken[i] = true;
                _lastPresentationApplied = _frameMaster;
                result = new(sample.Value, AnimationTimerPhase.None); return true;
            }
        result = default; return false;
    }
}
