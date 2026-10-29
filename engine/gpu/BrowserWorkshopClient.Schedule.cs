using System;
using System.Buffers.Binary;
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
    private bool _hasPresentationFrame, _frameAdmitted, _framePoseTaken;
    private MasterTimeNanoseconds _frameMaster;
    private MonotonicNanoseconds _frameObservedAt;
    private MasterTimeNanoseconds _lastPresentationApplied;
    private ScheduleControlHeader? _awaitingPresented;
    private ulong _animationSequence;
    private WorkshopAnimationControl? _animationPending;
    private CadenceRevision _animationPendingCadence;
    private readonly record struct Preparation(ScheduleControlHeader Header, WorkshopCadenceSettings Settings,
        WorkshopResponse Endpoint, ScheduleTransition Transition);
    public WorkshopCadenceSettings Settings => _schedule?.Settings ?? _construction.Settings;

    private void ReceiveSchedule()
    {
        var bytes = _transport.EventBytes(_id.Value);
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
                var endpoint = WorkshopWireFlatBuffers.DecodeResponse(data[WorkshopScheduleWire.PrepareFieldBytes..]);
                if (endpoint.Session != Peer.Session || endpoint.Cadence != header.Revision ||
                    endpoint.Projection != header.Projection || endpoint.MasterGeneration != Peer.Generation ||
                    endpoint.Read.Epoch.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[56..]) ||
                    endpoint.Read.Tick.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[64..]) ||
                    endpoint.Publication.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[72..]))
                    throw new ArgumentException("Prepared endpoint identity mismatch.");
                if (endpoint.Scene is not { } scene || scene.Identity != (_proposedScene ?? _compiledScene)?.Identity || scene.Settings != settings)
                    throw new ArgumentException("Prepared endpoint compiled scene mismatch.");
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
                var now = _transport.EventTime(_id.Value);
                MappedCapture? mapped = Clock.TryMap(stamp, now, out var value) ? value : null;
                var boundary = schedule.World.Playback == WorldPlayback.Running ? PresentationBoundary.Run :
                    schedule.World.Playback == WorldPlayback.Completed ? PresentationBoundary.Completed : PresentationBoundary.Seed;
                var history = _history.PrepareInstall(schedule, prepared.Endpoint, mapped, boundary, Clock.DisplayEpoch);
                var cursor = _cursor.PrepareInstallation(prepared.Endpoint);
                _history.Commit(history); _cursor.Commit(cursor);
                _observation.Commit(prepared.Endpoint);
                ReconcileAnimationSchedule(schedule);
                EnsureInstalledScene(_proposedConstruction ?? _construction, prepared.Endpoint.Scene!);
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
                if (_schedule is { } old && Clock.TryMasterNow(_transport.EventTime(_id.Value), out var current))
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
        _transport.ScheduleResult(_id.Value, result);
    }

    /// <summary>The single animation lease: every pump checks <see cref="_animationPending"/> before building a control.</summary>
    private void SendAnimation(WorkshopAnimationControl control, WorkshopSchedule schedule)
    {
        if (_animationPending is not null) throw new InvalidOperationException("An animation control is pending.");
        var bytes = WorkshopAnimationWire.Control(Peer.Session, Peer.Generation, schedule.Revision, control);
        // The lease is owned once the validated control exists; a failed send faults the transport, never a second lease.
        _animationSequence = control.Sequence; _animationPending = control; _animationPendingCadence = schedule.Revision;
        _transport.SendAnimationControl(_id.Value, bytes);
    }
    private void ReconcileAnimationSchedule(WorkshopSchedule schedule)
    {
        _declaredVisibilitySent.Clear();
        if (_schedule?.World.WorldGeneration != schedule.World.WorldGeneration) RetireGoalFeedback();
        if (_schedule?.Revision != schedule.Revision) { _declaredSamples.Clear(); _hintOrdinal = _goalOrdinal = 0; }
    }
    private void ReceiveAnimation() => ReceiveAnimation(_transport.EventBytes(_id.Value));
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
                sample.EventOrdinal != command.EventOrdinal || !F32Bits.Equal(sample.EventPhase, command.EventPhase))
                throw new ArgumentException("Unowned animation acknowledgement.");
            _animationPending = null;
            if (kind == AnimationOutputKind.Rejected) { RetryRejectedGoal(command); RetryDeclaredVisibility(command); return; }
        }
        if (cadence.Value < active.Revision.Value) return;
        // Target ranges are declared identities: the fixed UI targets, then the declared (package binding) range. Every one-shot cue is
        // a declared binding the Animation worker drives from the event ring (Story 9A.25b); the main thread relays none.
        if (sample.Target == UiCurves.Hint.AnimationTarget) ReceiveHintSample(sample, kind);
        else if (sample.Target == UiCurves.Goal.AnimationTarget) ReceiveGoalSample(sample, kind, active);
        else if (sample.Target.Value >= WorkshopDeclaredPresentation.TargetBase) ReceiveDeclaredSample(sample, kind, active);
        else throw new ArgumentException("Unowned animation output target.");
        Diagnostics.Capture(() => new WorkshopAnimationDiagnostic(cadence, kind, sample));
        // The JS ACK lease releases after this callback returns; next presentation pumps the next owner.
    }
    private bool AdmitPresentationFrame(ulong frame)
    {
        if (_hasPresentationFrame && frame < _presentationFrame) throw new ArgumentException("Display frame identity reversed.");
        if (_hasPresentationFrame && frame == _presentationFrame) return _frameAdmitted;
        _hasPresentationFrame = true; _presentationFrame = frame;
        _frameAdmitted = _framePoseTaken = _frameHintTaken = _frameGoalTaken = false;
        _frameObservedAt = _transport.NativeNow();
        if (_preparation is not null || _schedule is not { } schedule ||
            !Clock.TryMasterNow(_frameObservedAt, out var master)) return false;
        var due = WorkshopPulse.Due(schedule.Settings.PresentationRate, new(checked((long)master.Lower)));
        if (due.Value < schedule.FirstPresentation.Value || due.Value <= _lastPresentation.Value) return false;
        _lastPresentation = due;
        _frameMaster = WorkshopPulse.Deadline(schedule.Settings.PresentationRate, due);
        _frameAdmitted = true;
        return true;
    }
}
