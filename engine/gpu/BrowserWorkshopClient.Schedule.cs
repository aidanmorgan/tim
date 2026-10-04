using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private WorkshopSchedule? _schedule;
    private Preparation? _preparation;
    private WorkshopConstruction? _proposedConstruction;
    private WorkshopCommandIdentity? _constructionOwner;
    private PulseRate _admittedDisplay;
    private PulseOrdinal _lastPresentation;
    private ulong _presentationFrame;
    private bool _hasPresentationFrame, _frameAdmitted, _framePoseTaken, _frameHintTaken;
    private MasterTimeNanoseconds _frameMaster;
    private MonotonicNanoseconds _frameObservedAt;
    private MasterTimeNanoseconds _lastPresentationApplied;
    private ScheduleControlHeader? _awaitingPresented;
    private ulong _hintGeneration, _hintSequence, _hintAcknowledged;
    private WorkshopHintSample? _hintSample;
    private static readonly AnimationTargetId HintTarget = new(1), CaptureTarget = new(2);
    private WorkshopAnimationSample? _captureSample;
    private CaptureLatch? _captureRequested;
    private SimulationEpoch _captureWorld;
    private ulong _lastCaptureOrdinal;
    private bool _frameCaptureTaken;
    private WorkshopAnimationControl? _animationPending;
    private CadenceRevision _animationPendingCadence;
    private ulong _lastHintOrdinal;
    private readonly record struct Preparation(ScheduleControlHeader Header, WorkshopCadenceSettings Settings,
        WorkshopResponse Endpoint, ScheduleTransition Transition);
    public WorkshopCadenceSettings Settings => _schedule?.Settings ?? _construction.Settings;

    [JSImport("animationQualified", "workshopClient")] private static partial bool AnimationQualified(int client);
    [JSImport("animationKinds", "workshopClient")] private static partial void AnimationKinds(int client, int[] kinds);
    [JSImport("displayRate", "workshopClient")] private static partial int[] DisplayRate(int client);
    [JSImport("scheduleResult", "workshopClient")] private static partial void ScheduleResult(int client, byte[] bytes);
    [JSImport("animationControl", "workshopClient")] private static partial void SendAnimationControl(int client, byte[] bytes);

    private void ReceiveSchedule()
    {
        var bytes = EventBytes(_id.Value);
        var header = WorkshopScheduleWire.ReadHeader(bytes);
        if (header.Session != Peer.Session || header.MasterGeneration != Peer.Generation ||
            header.Sender != WorkshopRuntimeRole.Simulation || header.Recipient != WorkshopRuntimeRole.Browser)
            throw new ArgumentException("Foreign browser schedule control.");
        var data = bytes.AsSpan(64);
        switch (header.Kind)
        {
            case ScheduleControlKind.Prepare:
                if (_preparation is not null) throw new InvalidOperationException("Browser schedule already prepared.");
                var settings = WorkshopCadenceWire.Read(data[..32]);
                if (settings.Presentation == PresentationCadence.AdmittedDisplay && settings.PresentationRate != _admittedDisplay)
                    throw new ArgumentException("Unadmitted display rate.");
                if (BinaryPrimitives.ReadUInt64LittleEndian(data[32..]) != (_schedule?.Revision.Value ?? 0) ||
                    BinaryPrimitives.ReadUInt64LittleEndian(data[40..]) != (_schedule?.World.Epoch.Value ?? 0) ||
                    header.Revision.Value < (_schedule?.Revision.Value ?? 0) ||
                    header.Projection.Value <= (_schedule?.World.Epoch.Value ?? 0) ||
                    BinaryPrimitives.ReadUInt32LittleEndian(data[52..]) != 0)
                    throw new ArgumentException("Stale browser schedule Prepare.");
                var transition = (ScheduleTransition)BinaryPrimitives.ReadUInt32LittleEndian(data[48..]);
                if (!Enum.IsDefined(transition)) throw new ArgumentException("Unknown projection transition.");
                var endpoint = WorkshopWire.DecodeResponse(data[80..]);
                if (endpoint.Session != Peer.Session || endpoint.Cadence != header.Revision ||
                    endpoint.Projection != header.Projection || endpoint.MasterGeneration != Peer.Generation ||
                    endpoint.Read.Epoch.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[56..]) ||
                    endpoint.Read.Tick.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[64..]) ||
                    endpoint.Publication.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[72..]))
                    throw new ArgumentException("Prepared endpoint identity mismatch.");
                ValidateRead(endpoint.Read, _proposedConstruction ?? _construction);
                _preparation = new(header, settings, endpoint, transition);
                SendScheduleResult(header, ScheduleControlKind.Prepared);
                break;
            case ScheduleControlKind.Commit:
                var schedule = WorkshopScheduleWire.ReadCommit(bytes, out _);
                if (schedule.Settings.Presentation == PresentationCadence.AdmittedDisplay)
                    schedule.ValidateDisplay(_admittedDisplay);
                if (_preparation is not { } prepared || !SameInstallation(prepared.Header, header) ||
                    prepared.Settings != schedule.Settings || schedule.MasterNativeOrigin != Peer.MasterOrigin)
                    throw new ArgumentException("Browser Commit lacks its matching preparation.");
                var stamp = prepared.Endpoint.Read.Capture!.Value;
                var now = WorkshopNativeClock.FromMilliseconds(EventMilliseconds(_id.Value));
                MappedCapture? mapped = Clock.TryMap(stamp, now, out var value) ? value : null;
                var boundary = schedule.World.Playback == WorldPlayback.Running ? PresentationBoundary.Run :
                    schedule.World.Playback == WorldPlayback.Completed ? PresentationBoundary.Completed : PresentationBoundary.Seed;
                var history = _history.PrepareInstall(schedule, prepared.Endpoint, mapped, boundary, Clock.DisplayEpoch);
                var cursor = _cursor.PrepareInstallation(prepared.Endpoint);
                _history.Commit(history); _cursor.Commit(cursor);
                ReconcileAnimationSchedule(schedule);
                _schedule = schedule; _preparation = null;
                _construction = (_proposedConstruction ?? _construction) with { Settings = schedule.Settings };
                _lastPresentation = new(schedule.FirstPresentation.Value - 1);
                _frameAdmitted = false;
                if (prepared.Transition is ScheduleTransition.Step or ScheduleTransition.Completed or ScheduleTransition.Reset)
                    _awaitingPresented = header;
                SendScheduleResult(header, ScheduleControlKind.Applied);
                break;
            case ScheduleControlKind.Abort:
                if (_preparation is not { } pending || !SameInstallation(pending.Header, header) ||
                    !Enum.IsDefined((WorkshopRejection)BinaryPrimitives.ReadUInt32LittleEndian(data)) ||
                    BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != 0)
                    throw new ArgumentException("Foreign browser Abort.");
                _preparation = null;
                if (_schedule is { } old && Clock.TryMasterNow(WorkshopNativeClock.FromMilliseconds(EventMilliseconds(_id.Value)), out var current))
                    _lastPresentation = new(Math.Max(_lastPresentation.Value, WorkshopPulse.Due(old.Settings.PresentationRate, new(checked((long)current.Lower))).Value));
                break;
            default: throw new ArgumentException("Unsupported browser schedule control.");
        }
    }
    private static bool SameInstallation(ScheduleControlHeader a, ScheduleControlHeader b) =>
        a.Session == b.Session && a.MasterGeneration == b.MasterGeneration && a.Revision == b.Revision && a.Projection == b.Projection;
    private void SendScheduleResult(ScheduleControlHeader request, ScheduleControlKind kind)
    {
        var result = new byte[80];
        WorkshopScheduleWire.WriteHeader(request with { Kind = kind, Sender = WorkshopRuntimeRole.Browser,
            Recipient = WorkshopRuntimeRole.Simulation }, result.AsSpan(0,64));
        BinaryPrimitives.WriteInt64LittleEndian(result.AsSpan(64), _lastPresentationApplied.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(72), (uint)WorkshopCommandOutcome.Applied);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(76), (uint)WorkshopRejection.None);
        ScheduleResult(_id.Value, result);
    }

    public void ControlHint(AnimationControlKind kind, bool visible)
    {
        if (_schedule is not { } schedule || _preparation is not null) return;
        if (kind == AnimationControlKind.Endpoint) throw new ArgumentException("Hint requires its autonomous control.");
        var generation = kind is AnimationControlKind.Reveal or AnimationControlKind.Hide ? checked(_hintGeneration + 1) : _hintGeneration;
        if (generation == 0) return;
        var control = new WorkshopAnimationControl(HintTarget, default, checked(_hintSequence + 1), generation,
            kind, visible, kind == AnimationControlKind.Hide ? (Half)1 : (Half)0, (Half)1, (Half).16, AnimationCurve.SmoothStep);
        SendAnimation(control, schedule);
        _hintGeneration = generation;
        if (kind != AnimationControlKind.Visibility) _hintSample = null;
    }
    private void SendAnimation(WorkshopAnimationControl control, WorkshopSchedule schedule)
    {
        if (_animationPending is not null) throw new InvalidOperationException("An animation control is pending.");
        var bytes = WorkshopAnimationWire.Control(Peer.Session, Peer.Generation, schedule.Revision, control);
        SendAnimationControl(_id.Value, bytes);
        _hintSequence = control.Sequence; _animationPending = control; _animationPendingCadence = schedule.Revision;
    }
    private void PumpCapture()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _captureRequested is not null || _history.Latest is not { } latest ||
            latest.Read.Epoch != schedule.World.WorldGeneration || latest.Read.Captures.Count == 0) return;
        var capture = latest.Read.Captures[0];
        if (capture.Phase != CaptureLatchPhase.Latched) return;
        var control = new WorkshopAnimationControl(CaptureTarget, latest.Read.Epoch, checked(_hintSequence + 1), 1,
            AnimationControlKind.Endpoint, true, (Half)1, (Half)1, (Half)1, AnimationCurve.Linear,
            capture.EventOrdinal, capture.EventPhase);
        SendAnimation(control, schedule);
        _captureRequested = capture; _captureWorld = latest.Read.Epoch;
    }
    private void ReconcileAnimationSchedule(WorkshopSchedule schedule)
    {
        if (_schedule?.World.WorldGeneration != schedule.World.WorldGeneration)
        { _captureSample = null; _captureRequested = null; _captureWorld = schedule.World.WorldGeneration; _lastCaptureOrdinal = 0; RetireActivationFeedback(); RetireGoalFeedback(); }
        if (_schedule?.Revision != schedule.Revision)
        { _lastHintOrdinal = _lastCaptureOrdinal = _goalOrdinal = 0; Array.Clear(_activationOrdinals); Array.Clear(_timerOrdinals); }
    }
    private void ReceiveAnimation() => ReceiveAnimation(EventBytes(_id.Value));
    internal void ReceiveAnimation(byte[] bytes)
    {
        if (bytes.Length != WorkshopAnimationWire.OutputBytes) throw new ArgumentException("Invalid Animation frame.");
        var cadence = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)));
        cadence.Validate();
        var sample = WorkshopAnimationWire.Read(bytes, Peer.Session, Peer.Generation, cadence, out var kind);
        var active = _schedule ?? throw new InvalidOperationException("Animation output before schedule.");
        if (cadence.Value > active.Revision.Value) throw new ArgumentException("Future Animation cadence.");
        // Reliable ACK belongs to its exact sent command even if a world transition has since retired its target.
        if (kind is AnimationOutputKind.Acknowledgement or AnimationOutputKind.Rejected)
        {
            if (_animationPending is not { } command || cadence != _animationPendingCadence || sample.Pulse.Value != command.Sequence ||
                sample.Property != command.Property || sample.Target != command.Target || sample.World != command.World || sample.Generation != command.Generation ||
                sample.Timer != command.Timer || sample.EventOrdinal != command.EventOrdinal || !HalfBits.Equal(sample.EventPhase, command.EventPhase))
                throw new ArgumentException("Unowned animation acknowledgement.");
            _hintAcknowledged = sample.Pulse.Value; _animationPending = null;
            if (kind == AnimationOutputKind.Rejected) { RetryRejectedTimer(command); RetryRejectedGoal(command); return; }
        }
        if (cadence.Value < active.Revision.Value) return;
        if (sample.Target == HintTarget)
        {
            if (sample.Property != AnimationProperty.Opacity || sample.World.Value != 0 || sample.EventOrdinal != 0 || sample.EventPhase != (Half)0)
                throw new ArgumentException("Autonomous output carried a world event.");
            if (sample.Generation < _hintGeneration) return;
            if (sample.Generation != _hintGeneration) throw new ArgumentException("Future UI generation.");
            if (kind == AnimationOutputKind.Sample)
            {
                if (sample.Pulse.Value <= _lastHintOrdinal) throw new ArgumentException("Reversed UI publication.");
                _lastHintOrdinal = sample.Pulse.Value;
            }
            _hintSample = new(sample.Generation, sample.Pulse, sample.AppliedAt, sample.Value);
        }
        else if (sample.Target == CaptureTarget)
        {
            if (sample.World.Value < active.World.WorldGeneration.Value) return;
            if (sample.Property != AnimationProperty.Opacity || sample.World != _captureWorld || _captureRequested is not { } capture || sample.Generation != 1 ||
                sample.EventOrdinal != capture.EventOrdinal || !HalfBits.Equal(sample.EventPhase, capture.EventPhase))
                throw new ArgumentException("World feedback does not own the committed occurrence.");
            if (kind == AnimationOutputKind.Sample)
            {
                if (sample.Pulse.Value <= _lastCaptureOrdinal) throw new ArgumentException("Reversed world publication.");
                _lastCaptureOrdinal = sample.Pulse.Value;
            }
            _captureSample = sample;
        }
        else if (sample.Target == GoalTarget) ReceiveGoalSample(sample, kind, active);
        else if (sample.Timer.Phase != AnimationTimerPhase.None) ReceiveTimerSample(sample, kind, active);
        else ReceiveActivationSample(sample, kind, active);
        // The JS ACK lease releases after this callback returns; next presentation pumps the next owner.
    }
    public bool TryCaptureOpacity(ulong frame, WorkshopPresentationSample physical, out Half opacity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed(); PumpCapture();
        if (!AdmitPresentationFrame(frame) || _frameCaptureTaken || _captureSample is not { } sample ||
            sample.World != Epoch || _captureRequested is not { } capture ||
            physical.Evidence.WorldEpoch != sample.World || physical.Captures.Count != 1 ||
            physical.Captures[0] != capture || physical.FeedbackCapture is null)
        { opacity = default; return false; }
        _frameCaptureTaken = true; _captureSample = null; opacity = sample.Value; return true;
    }
    public void RecordCapturePresentation(ulong frame)
    {
        if (!_hasPresentationFrame || frame != _presentationFrame || !_frameAdmitted || !_frameCaptureTaken)
            throw new InvalidOperationException("World opacity lacks its admitted display pulse.");
        _lastPresentationApplied = _frameMaster;
    }
    private bool AdmitPresentationFrame(ulong frame)
    {
        if (_hasPresentationFrame && frame < _presentationFrame) throw new ArgumentException("Display frame identity reversed.");
        if (_hasPresentationFrame && frame == _presentationFrame) return _frameAdmitted;
        _hasPresentationFrame = true; _presentationFrame = frame;
        _frameAdmitted = _framePoseTaken = _frameHintTaken = _frameCaptureTaken = _frameGoalTaken = false;
        Array.Clear(_activationFrameTaken);
        _frameObservedAt = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        if (_preparation is not null || _schedule is not { } schedule ||
            !Clock.TryMasterNow(_frameObservedAt, out var master)) return false;
        var due = WorkshopPulse.Due(schedule.Settings.PresentationRate, new(checked((long)master.Lower)));
        if (due.Value < schedule.FirstPresentation.Value || due.Value <= _lastPresentation.Value) return false;
        _lastPresentation = due;
        _frameMaster = WorkshopPulse.Deadline(schedule.Settings.PresentationRate, due);
        _frameAdmitted = true;
        return true;
    }
    public bool TryHint(ulong frame, out WorkshopHintSample sample)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfTransportFailed();
        if (!AdmitPresentationFrame(frame) || _frameHintTaken || _hintSample is not { } value || value.AppliedAt.Value > _frameMaster.Value)
        { sample = default; return false; }
        _frameHintTaken = true; _hintSample = null; sample = value; return true;
    }
    public void RecordHintPresentation(ulong frame)
    {
        if (!_hasPresentationFrame || frame != _presentationFrame || !_frameAdmitted || !_frameHintTaken)
            throw new InvalidOperationException("Hint application lacks its admitted display pulse.");
        _lastPresentationApplied = _frameMaster;
    }
}
