using CuriousContraptions.Abstractions;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

[assembly: SupportedOSPlatform("browser")]

namespace CuriousContraptions.Animation.Worker;

/// <summary>Independent owner of the declared animation channels; all evaluation uses S master time.</summary>
public static partial class Program
{
    private static IWorkshopDiagnostics? _diagnostics;
    private static IWorkshopDiagnostics Diagnostics => _diagnostics ?? throw new InvalidOperationException("Animation diagnostics are not installed.");
    private static ICoreServices? _core;
    private static ICoreServices Core => _core ?? throw new InvalidOperationException("Core provider is not installed.");
    private static WorkshopClockPeer? _peer;
    private static IClockMapping? _clock;
    private static WorkshopSchedule? _schedule;
    private static Preparation? _prepared;
    private static IWorkshopDeclaredPresentation? _declared;
    private static IAnimationControlDeferral? _deferred;
    private static IAnimationControlDeferral Deferred => _deferred ?? throw new InvalidOperationException("Animation deferral is not installed.");
    private static IAnimationServices? _animation;
    private static IWorkshopAnimationTracks? _controls;
    private static IAnimationServices Animation => _animation ?? throw new InvalidOperationException("Animation provider is not installed.");
    private static IWorkshopAnimationTracks Controls => _controls ?? throw new InvalidOperationException("Animation controls are not installed.");
    private static ulong _commandSequence, _lastOrdinal, _evaluationSequence;
    private static MasterTimeNanoseconds _lastApplied;
    private static byte[]? _output;
    private static byte[] Output => _output ?? throw new InvalidOperationException("Animation output is not installed.");
    private static byte[]? _result;
    private static byte[] Result => _result ?? throw new InvalidOperationException("Animation result is not installed.");
    private readonly record struct Preparation(ScheduleControlHeader Header, WorkshopCadenceSettings Settings,
        SimulationEpoch World, SimulationTick Tick, WorkshopSimulationPhase Phase, IWorkshopDeclaredPresentation Presentation, WorkshopResponse Endpoint, bool Retained);
    private static WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Animation master is not installed.");
    private static IClockMapping Clock => _clock ?? throw new InvalidOperationException("Animation mapping is not installed.");

    [JSImport("observation", "workshopAnimation")]
    private static partial void PublishObservation(byte[] bytes);
    private static void PublishDiagnostic(WorkshopDiagnostic record) =>
        Diagnostics.Publish(record, Peer.Session, WorkshopRuntimeRole.Animation, PublishObservation);
    [JSExport]
    public static void ReportObservationFailure(string detail) =>
        Diagnostics.Fault(FaultSite.TraceFlush, new InvalidOperationException(detail));

    public static void Main() { }
    [JSExport] public static int[] OutputKinds() => [(int)AnimationOutputKind.Acknowledgement, (int)AnimationOutputKind.Sample, (int)AnimationOutputKind.Rejected];
    [JSImport("now", "workshopAnimation")] private static partial double NativeMilliseconds();
    [JSImport("probe", "workshopAnimation")] private static partial void SendProbe(byte[] bytes);
    [JSImport("control", "workshopAnimation")] private static partial void SendControl(byte[] bytes);
    [JSImport("output", "workshopAnimation")] private static partial void SendOutput(byte[] bytes);
    [JSImport("qualified", "workshopAnimation")] private static partial void Qualified();

    [JSExport]
    public static void Bootstrap(byte[] bytes)
    {
        if (_peer is not null) throw new InvalidOperationException("Animation master cannot be replaced.");
        var core = RuntimeProviders.Core();
        var animation = RuntimeProviders.Animation();
        var peer = core.Codecs.Clocks.DecodePeer(bytes);
        if (peer.RequesterRole != WorkshopRuntimeRole.Animation || peer.RequesterGeneration.Value != 1)
            throw new ArgumentException("Wrong Animation bootstrap role.");
        var diagnostics = core.CreateDiagnostics(PublishDiagnostic);
        var controls = core.CreateAnimationTracks(animation);
        var deferred = core.CreateAnimationDeferral(diagnostics);
        var output = new byte[core.Codecs.Animation.OutputBytes];
        var result = new byte[core.Codecs.Schedules.HeaderBytes + core.Codecs.Schedules.ResultBytes];
        var clock = core.Clocks.CreateMapping(peer, diagnostics);
        _core = core; _animation = animation; _diagnostics = diagnostics; _controls = controls;
        _deferred = deferred; _output = output; _result = result; _peer = peer; _clock = clock;
    }

    [JSExport]
    public static void Reply(byte[] bytes, double receiptMilliseconds)
    {
        var receipt = Core.Clocks.FromMilliseconds(receiptMilliseconds);
        // The requesters' one reply path: a malformed reply is one counted, typed rejection, never applied or dropped.
        if (Core.Codecs.Clocks.ReceiveEncoded(Clock, bytes, receipt) is null) return;
        if (Clock.IsQualified) Qualified();
    }

    [JSExport]
    public static void Service()
    {
        var now = Core.Clocks.FromMilliseconds(NativeMilliseconds());
        if (Clock.BeginProbe(now) is { } probe) SendProbe(Core.Codecs.Clocks.EncodeProbe(probe, Peer));
        // A deferred control is applied once master time is available again, and that turn ends there (AnimationControlDeferral).
        var interval = default(ClockInterval);
        var masterTime = _schedule is not null && _prepared is null && Clock.TryMasterNow(now, out interval);
        switch (Deferred.Turn(_schedule is not null, _prepared is not null, masterTime))
        {
            case AnimationServiceTurn.Wait: return;
            case AnimationServiceTurn.ApplyDeferred: DrainControl(); return;
        }
        var schedule = _schedule!.Value;
        var due = Core.Scheduling.Due(schedule.Settings.AnimationRate, new(checked((long)interval.Lower)));
        if (due.Value < schedule.FirstAnimation.Value || due.Value <= _lastOrdinal) return;
        // Coalesce cosmetic evaluation opportunities, never create a Task for an individual pulse.
        var presentationTime = PhysicalTime.FromRatio((UInt128)due.Value * schedule.Settings.AnimationRate.Denominator,
            schedule.Settings.AnimationRate.Numerator);
        Controls.Advance(checked(++_evaluationSequence), presentationTime);
        _lastApplied = Core.Scheduling.Deadline(schedule.Settings.AnimationRate, due);
        for (var i = 0; i < Controls.Capacity; i++)
            if (Controls.TryRead(i, out var control, out var value))
                Emit(AnimationOutputKind.Sample, due.Value, control, value);
        if (_declared is { } declared)
            foreach (var sample in declared.Advance(schedule, presentationTime))
                Emit(AnimationOutputKind.Sample, due.Value,
                    new(sample.Target, schedule.World.WorldGeneration, 1, schedule.World.Epoch.Value,
                        AnimationControlKind.Endpoint, true, 0, 0, .01f, AnimationCurve.Linear, Property: sample.Value.Property),
                    BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits));
        _lastOrdinal = due.Value;
    }

    [JSExport]
    public static void ScheduleControl(byte[] bytes)
    {
        var header = Core.Codecs.Schedules.ReadHeader(bytes);
        if (header.Session != Peer.Session || header.MasterGeneration != Peer.Generation ||
            header.Sender != WorkshopRuntimeRole.Simulation || header.Recipient != WorkshopRuntimeRole.Animation)
            throw new ArgumentException("Wrong Animation schedule identity or route.");
        var data = bytes.AsSpan(Core.Codecs.Schedules.HeaderBytes);
        switch (header.Kind)
        {
            case ScheduleControlKind.Prepare:
                PrepareSchedule(data, header); return;
            case ScheduleControlKind.Commit:
                CommitSchedule(bytes, header); return;
            case ScheduleControlKind.Abort:
                AbortSchedule(data, header); return;
            default: throw new ArgumentException("Unsupported Animation schedule control.");
        }
    }

    private static void PrepareSchedule(ReadOnlySpan<byte> data, ScheduleControlHeader header)
    {
        if (_prepared is not null) throw new InvalidOperationException("Animation installation already prepared.");
        var settings = Core.Codecs.Schedules.ReadCadence(data[..32]);
        var expectedRevision = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(data[32..]));
        var expectedProjection = new ProjectionEpoch(BinaryPrimitives.ReadUInt64LittleEndian(data[40..]));
        var transition = (ScheduleTransition)BinaryPrimitives.ReadUInt32LittleEndian(data[48..]);
        if (!Enum.IsDefined(transition) || BinaryPrimitives.ReadUInt32LittleEndian(data[52..]) != 0 ||
            expectedRevision != (_schedule?.Revision ?? default) || expectedProjection != (_schedule?.World.Epoch ?? default) ||
            header.Revision.Value < expectedRevision.Value || header.Projection.Value <= expectedProjection.Value)
            throw new ArgumentException("Stale Animation installation.");
        var endpoint = Core.Codecs.Commands.DecodeResponse(data[Core.Codecs.Schedules.PrepareFieldBytes..]);
        if (endpoint.Session != Peer.Session || endpoint.MasterGeneration != Peer.Generation ||
            endpoint.Cadence != header.Revision || endpoint.Projection != header.Projection ||
            endpoint.Read.Epoch.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[56..]) ||
            endpoint.Read.Tick.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[64..]) ||
            endpoint.Publication.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[72..]))
            throw new ArgumentException("Prepared Animation endpoint differs from its envelope.");
        if (endpoint.Scene is not { } construction) throw new ArgumentException("Animation installation lacks its compiled scene.");
        var retained = transition != ScheduleTransition.Reset && _declared is not null && _declared.CanRetain(construction, endpoint);
        if (retained) _declared!.ValidateRebind(construction, endpoint);
        _prepared = new(header, settings, endpoint.Read.Epoch, endpoint.Read.Tick, endpoint.Phase,
            retained ? _declared! : Core.Presentation.Create(Animation, construction, endpoint), endpoint, retained);
        Respond(header, ScheduleControlKind.Prepared);
    }

    private static void CommitSchedule(byte[] bytes, ScheduleControlHeader header)
    {
        var schedule = Core.Codecs.Schedules.ReadCommit(bytes, out _);
        if (_prepared is not { } candidate || !Matches(candidate.Header, header) ||
            candidate.Settings != schedule.Settings || candidate.World != schedule.World.WorldGeneration ||
            candidate.Tick != schedule.World.AnchorTick || !Core.Presentation.MatchesPlayback(candidate.Phase, schedule.World.Playback) ||
            schedule.MasterNativeOrigin != Peer.MasterOrigin)
            throw new ArgumentException("Animation Commit lacks its matching prepared endpoint.");
        Controls.RetireWorld(schedule.World.WorldGeneration);
        if (candidate.Retained) candidate.Presentation.Rebind(candidate.Endpoint.Scene!, candidate.Endpoint);
        _declared = candidate.Presentation;
        _schedule = schedule; _prepared = null; _lastOrdinal = schedule.FirstAnimation.Value - 1;
        Respond(header, ScheduleControlKind.Applied);
        DrainControl();
    }

    private static void AbortSchedule(ReadOnlySpan<byte> data, ScheduleControlHeader header)
    {
        if (_prepared is not { } aborting || !Matches(aborting.Header, header) ||
            !Enum.IsDefined((WorkshopRejection)BinaryPrimitives.ReadUInt32LittleEndian(data)) ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != 0)
            throw new ArgumentException("Animation Abort does not own preparation.");
        _prepared = null;
        if (_schedule is { } old && Clock.TryMasterNow(Core.Clocks.FromMilliseconds(NativeMilliseconds()), out var current))
            _lastOrdinal = Math.Max(_lastOrdinal, Core.Scheduling.Due(old.Settings.AnimationRate,
                new(checked((long)current.Lower))).Value);
        DrainControl();
    }

    /// <summary>Story 9A.23 D5: the epoch whose event ring this worker reads, the installed world's (0 while none is installed or an
    /// installation is being prepared). The shared reader holds a newer ring unread and ignores an older one.</summary>
    [JSExport]
    public static string EventEpoch() =>
        (_schedule is { } schedule && _prepared is null && _declared is not null ? schedule.World.WorldGeneration.Value : 0)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Story 9A.23 D1: the records the shared reader copied at the start of this Service turn, for the declared one-shot cues.</summary>
    [JSExport]
    public static void EventRecords(byte[] delivery)
    {
        if (_schedule is not { } schedule || _prepared is not null || _declared is null)
            throw new InvalidOperationException("Event records arrived without an installed world.");
        _declared.AcceptEvents(Abstractions.EventRingDelivery.Read(delivery), schedule.World);
    }

    [JSExport]
    public static void CommittedRead(byte[] bytes)
    {
        var response = Core.Codecs.Commands.DecodeResponse(bytes);
        if (_schedule is not { } schedule || _prepared is not null || _declared is null) return;
        // A queued read from a retired installation is harmless; a future/foreign identity is rejected by the owner.
        if (response.Read.Epoch.Value < schedule.World.WorldGeneration.Value ||
            response.Projection.Value < schedule.World.Epoch.Value || response.Cadence.Value < schedule.Revision.Value) return;
        _declared.Accept(response);
    }

    private static bool Matches(ScheduleControlHeader first, ScheduleControlHeader second) =>
        first.Session == second.Session && first.MasterGeneration == second.MasterGeneration &&
        first.Revision == second.Revision && first.Projection == second.Projection;

    private static void Respond(ScheduleControlHeader request, ScheduleControlKind kind)
    {
        Result.AsSpan().Clear();
        Core.Codecs.Schedules.WriteHeader(request with { Kind = kind, Sender = WorkshopRuntimeRole.Animation,
            Recipient = WorkshopRuntimeRole.Simulation }, Result.AsSpan(0, Core.Codecs.Schedules.HeaderBytes));
        BinaryPrimitives.WriteInt64LittleEndian(Result.AsSpan(64), _lastApplied.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(Result.AsSpan(72), (uint)WorkshopCommandOutcome.Applied);
        BinaryPrimitives.WriteUInt32LittleEndian(Result.AsSpan(76), (uint)WorkshopRejection.None);
        SendControl(Result);
    }

    private static void DrainControl()
    {
        if (Deferred.Release() is not { } bytes) return;
        AnimationControl(bytes);
    }

    [JSExport]
    public static void AnimationControl(byte[] bytes)
    {
        if (_schedule is not { } schedule || bytes.Length != Core.Codecs.Animation.ControlBytes)
            throw new InvalidOperationException("Animation installation is not available.");
        var cadence = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(Core.Codecs.Animation.CadenceOffset)));
        cadence.Validate();
        var control = Core.Codecs.Animation.ReadControl(bytes, Peer.Session, Peer.Generation, cadence);
        if (control.Sequence <= _commandSequence || cadence.Value > schedule.Revision.Value ||
            control.World.Value > schedule.World.WorldGeneration.Value)
            throw new ArgumentException("Foreign channel control.");
        if (_prepared is not null)
        {
            Deferred.Hold(bytes, AnimationDeferralCause.Preparation);
            return;
        }
        if (cadence.Value < schedule.Revision.Value ||
            (control.World.Value != 0 && control.World != schedule.World.WorldGeneration))
        {
            _commandSequence = control.Sequence;
            Emit(AnimationOutputKind.Rejected, control.Sequence, control, control.From, cadence); return;
        }
        if (control.Target.Value >= Core.Presentation.TargetBase)
        {
            if (control.Kind != AnimationControlKind.Visibility || control.World != schedule.World.WorldGeneration ||
                control.Generation != schedule.World.Epoch.Value || _declared is null)
                throw new ArgumentException("Foreign declared visibility control.");
            _declared.SetVisible(control.Target, control.Property, control.Visible);
            _commandSequence = control.Sequence;
            Emit(AnimationOutputKind.Acknowledgement, control.Sequence, control, control.From);
            return;
        }
        Controls.Validate(control);
        if (!Clock.TryMasterNow(Core.Clocks.FromMilliseconds(NativeMilliseconds()), out var now))
        {
            // A recalibrating mapping (counted ClockRecalibrated diagnostics) has no master time yet: the control
            // waits unapplied and unacknowledged, so the browser sends no other, until the mapping requalifies (Story 9.4b).
            Deferred.Hold(bytes, AnimationDeferralCause.Recalibrating);
            return;
        }
        var applied = Controls.Apply(control,
            PhysicalTime.FromNanoseconds(checked((long)now.Lower)));
        if (applied.Started)
            _lastOrdinal = Math.Max(_lastOrdinal, Core.Scheduling.Due(schedule.Settings.AnimationRate,
                new(checked((long)now.Lower))).Value);
        _commandSequence = control.Sequence;
        Emit(AnimationOutputKind.Acknowledgement, control.Sequence, control, applied.Value);
    }

    private static void Emit(AnimationOutputKind kind, ulong ordinal, WorkshopAnimationControl control, float channel, CadenceRevision? cadence = null)
    {
        _ = Core.Codecs.Animation.Value(control.Property, channel);
        Output.AsSpan().Clear();
        Core.Codecs.Sessions.WriteTo(Peer.Session, Output.AsSpan(Core.Codecs.Animation.SessionOffset, RuntimeSessionId.ByteLength));
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.MasterOffset), Peer.Generation.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.CadenceOffset), (cadence ?? _schedule!.Value.Revision).Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputGenerationOffset), control.Generation);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputPulseOffset), ordinal);
        BinaryPrimitives.WriteInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputAppliedAtOffset), _lastApplied.Value);
        BinaryPrimitives.WriteSingleLittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputValueOffset), channel);
        BinaryPrimitives.WriteUInt16LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputPropertyOffset), (ushort)control.Property);
        BinaryPrimitives.WriteUInt32LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputKindOffset), (uint)kind);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputTargetOffset), control.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputWorldOffset), control.World.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputEventOrdinalOffset), control.EventOrdinal);
        BinaryPrimitives.WriteSingleLittleEndian(Output.AsSpan(Core.Codecs.Animation.OutputEventPhaseOffset), control.EventPhase);
        SendOutput(Output);
    }
}
