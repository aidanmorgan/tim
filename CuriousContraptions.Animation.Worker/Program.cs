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
    private static WorkshopClockPeer? _peer;
    private static WorkshopClockMapping? _clock;
    private static WorkshopSchedule? _schedule;
    private static Preparation? _prepared;
    private static WorkshopDeclaredPresentation? _declared;
    private static readonly AnimationControlDeferral Deferred = new();
    // Story 9R.5b: one track per animated target of the machine; both tables grow when a new target finds no vacancy, so the
    // machine's target population is the only bound (the initial size is only a starting allocation).
    private const int InitialTracks = 64;
    private static readonly AnimationBatch Tracks = new(InitialTracks);
    private static Track[] Instances = new Track[InitialTracks];
    private struct Track
    {
        public AnimationHandle? Handle;
        public WorkshopAnimationControl Control;
    }
    private static ulong _commandSequence, _lastOrdinal, _evaluationSequence;
    private static MasterTimeNanoseconds _lastApplied;
    private static readonly byte[] Output = new byte[WorkshopAnimationWire.OutputBytes];
    private static readonly byte[] Result = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.ResultBytes];
    private readonly record struct Preparation(ScheduleControlHeader Header, WorkshopCadenceSettings Settings,
        SimulationEpoch World, SimulationTick Tick, WorkshopSimulationPhase Phase, WorkshopDeclaredPresentation Presentation, WorkshopResponse Endpoint, bool Retained);
    private static WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Animation master is not installed.");
    private static WorkshopClockMapping Clock => _clock ?? throw new InvalidOperationException("Animation mapping is not installed.");

    public static void Main()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("elements.bundle.json")
            ?? throw new InvalidOperationException("Animation declaration bundle is missing.");
        using var reader = new System.IO.StreamReader(stream);
        var document = CuriousContraptions.Bundle.BundleSchema.Read(reader.ReadToEnd());
        DeclaredPhysics.Install(document.Elements);
        DeclaredPorts.Install(ElementPortCatalogue.FromBundle(document.Elements));
    }
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
        var peer = WorkshopClockWire.DecodePeer(bytes);
        if (peer.RequesterRole != WorkshopRuntimeRole.Animation || peer.RequesterGeneration.Value != 1)
            throw new ArgumentException("Wrong Animation bootstrap role.");
        _peer = peer; _clock = new(peer);
    }

    [JSExport]
    public static void Reply(byte[] bytes, double receiptMilliseconds)
    {
        var receipt = WorkshopNativeClock.FromMilliseconds(receiptMilliseconds);
        // The requesters' one reply path: a malformed reply is one counted, typed rejection, never applied or dropped.
        if (Clock.ReceiveEncoded(bytes, receipt) is null) return;
        if (Clock.IsQualified) Qualified();
    }

    [JSExport]
    public static void Service()
    {
        var now = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        if (Clock.BeginProbe(now) is { } probe) SendProbe(WorkshopClockWire.EncodeProbe(probe, Peer));
        // A deferred control is applied once master time is available again, and that turn ends there (AnimationControlDeferral).
        var interval = default(ClockInterval);
        var masterTime = _schedule is not null && _prepared is null && Clock.TryMasterNow(now, out interval);
        switch (Deferred.Turn(_schedule is not null, _prepared is not null, masterTime))
        {
            case AnimationServiceTurn.Wait: return;
            case AnimationServiceTurn.ApplyDeferred: DrainControl(); return;
        }
        var schedule = _schedule!.Value;
        var due = WorkshopPulse.Due(schedule.Settings.AnimationRate, new(checked((long)interval.Lower)));
        if (due.Value < schedule.FirstAnimation.Value || due.Value <= _lastOrdinal) return;
        // Coalesce cosmetic evaluation opportunities, never create a Task for an individual pulse.
        Tracks.Advance(checked(++_evaluationSequence), (double)due.Value * schedule.Settings.AnimationRate.Denominator /
            schedule.Settings.AnimationRate.Numerator, 0);
        _lastApplied = WorkshopPulse.Deadline(schedule.Settings.AnimationRate, due);
        for (var i = 0; i < Instances.Length; i++)
            if (Instances[i].Handle is { } handle)
                Emit(AnimationOutputKind.Sample, due.Value, Instances[i].Control, BitConverter.UInt32BitsToSingle(Tracks.Read(handle).Value.CanonicalBits));
        if (_declared is { } declared)
            foreach (var sample in declared.Advance((double)due.Value * schedule.Settings.AnimationRate.Denominator / schedule.Settings.AnimationRate.Numerator))
                Emit(AnimationOutputKind.Sample, due.Value,
                    new(sample.Target, schedule.World.WorldGeneration, 1, schedule.World.Epoch.Value,
                        AnimationControlKind.Endpoint, true, 0, 0, .01f, AnimationCurve.Linear, Property: sample.Value.Property),
                    BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits));
        _lastOrdinal = due.Value;
    }

    [JSExport]
    public static void ScheduleControl(byte[] bytes)
    {
        var header = WorkshopScheduleWire.ReadHeader(bytes);
        if (header.Session != Peer.Session || header.MasterGeneration != Peer.Generation ||
            header.Sender != WorkshopRuntimeRole.Simulation || header.Recipient != WorkshopRuntimeRole.Animation)
            throw new ArgumentException("Wrong Animation schedule identity or route.");
        var data = bytes.AsSpan(WorkshopScheduleWire.HeaderBytes);
        switch (header.Kind)
        {
            case ScheduleControlKind.Prepare:
                if (_prepared is not null) throw new InvalidOperationException("Animation installation already prepared.");
                var settings = WorkshopCadenceWire.Read(data[..32]);
                var expectedRevision = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(data[32..]));
                var expectedProjection = new ProjectionEpoch(BinaryPrimitives.ReadUInt64LittleEndian(data[40..]));
                var transition = (ScheduleTransition)BinaryPrimitives.ReadUInt32LittleEndian(data[48..]);
                if (!Enum.IsDefined(transition) || BinaryPrimitives.ReadUInt32LittleEndian(data[52..]) != 0 ||
                    expectedRevision != (_schedule?.Revision ?? default) || expectedProjection != (_schedule?.World.Epoch ?? default) ||
                    header.Revision.Value < expectedRevision.Value || header.Projection.Value <= expectedProjection.Value)
                    throw new ArgumentException("Stale Animation installation.");
                var endpoint = WorkshopWireFlatBuffers.DecodeResponse(data[WorkshopScheduleWire.PrepareFieldBytes..]);
                if (endpoint.Session != Peer.Session || endpoint.MasterGeneration != Peer.Generation ||
                    endpoint.Cadence != header.Revision || endpoint.Projection != header.Projection ||
                    endpoint.Read.Epoch.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[56..]) ||
                    endpoint.Read.Tick.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[64..]) ||
                    endpoint.Publication.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[72..]))
                    throw new ArgumentException("Prepared Animation endpoint differs from its envelope.");
                if (endpoint.Construction is not { } construction) throw new ArgumentException("Animation installation lacks its declared construction.");
                var retained = transition != ScheduleTransition.Reset && _declared is not null && _declared.CanRetain(construction, endpoint);
                if (retained) _declared!.ValidateRebind(construction, endpoint);
                _prepared = new(header, settings, endpoint.Read.Epoch, endpoint.Read.Tick, endpoint.Phase,
                    retained ? _declared! : new WorkshopDeclaredPresentation(construction, endpoint), endpoint, retained);
                Respond(header, ScheduleControlKind.Prepared);
                return;
            case ScheduleControlKind.Commit:
                var schedule = WorkshopScheduleWire.ReadCommit(bytes, out _);
                if (_prepared is not { } candidate || !Matches(candidate.Header, header) ||
                    candidate.Settings != schedule.Settings || candidate.World != schedule.World.WorldGeneration ||
                    candidate.Tick != schedule.World.AnchorTick || !MatchesPlayback(candidate.Phase, schedule.World.Playback) ||
                    schedule.MasterNativeOrigin != Peer.MasterOrigin)
                    throw new ArgumentException("Animation Commit lacks its matching prepared endpoint.");
                for (var i = 0; i < Instances.Length; i++)
                    if (Instances[i].Handle is { } retiring && Instances[i].Control.World.Value != 0 &&
                        Instances[i].Control.World != schedule.World.WorldGeneration)
                    { Tracks.Remove(retiring); Instances[i] = default; }
                if (candidate.Retained) candidate.Presentation.Rebind(candidate.Endpoint.Construction!.Value, candidate.Endpoint);
                _declared = candidate.Presentation;
                _schedule = schedule; _prepared = null; _lastOrdinal = schedule.FirstAnimation.Value - 1;
                Respond(header, ScheduleControlKind.Applied);
                DrainControl();
                return;
            case ScheduleControlKind.Abort:
                if (_prepared is not { } aborting || !Matches(aborting.Header, header) ||
                    !Enum.IsDefined((WorkshopRejection)BinaryPrimitives.ReadUInt32LittleEndian(data)) ||
                    BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != 0)
                    throw new ArgumentException("Animation Abort does not own preparation.");
                _prepared = null;
                if (_schedule is { } old && Clock.TryMasterNow(WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), out var current))
                    _lastOrdinal = Math.Max(_lastOrdinal, WorkshopPulse.Due(old.Settings.AnimationRate,
                        new(checked((long)current.Lower))).Value);
                DrainControl();
                return;
            default: throw new ArgumentException("Unsupported Animation schedule control.");
        }
    }

    /// <summary>Story 9A.23 D5: the epoch whose event ring this worker reads, the installed world's (0 while none is installed or an
    /// installation is being prepared). The shared reader holds a newer ring unread and ignores an older one.</summary>
    [JSExport]
    public static double EventEpoch() =>
        _schedule is { } schedule && _prepared is null && _declared is not null ? schedule.World.WorldGeneration.Value : 0;

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
        var response = WorkshopWireFlatBuffers.DecodeResponse(bytes);
        if (_schedule is not { } schedule || _prepared is not null || _declared is null) return;
        // A queued read from a retired installation is harmless; a future/foreign identity is rejected by the owner.
        if (response.Read.Epoch.Value < schedule.World.WorldGeneration.Value ||
            response.Projection.Value < schedule.World.Epoch.Value || response.Cadence.Value < schedule.Revision.Value) return;
        _declared.Accept(response);
    }

    private static bool Matches(ScheduleControlHeader first, ScheduleControlHeader second) =>
        first.Session == second.Session && first.MasterGeneration == second.MasterGeneration &&
        first.Revision == second.Revision && first.Projection == second.Projection;

    private static bool MatchesPlayback(WorkshopSimulationPhase phase, WorldPlayback playback) => playback switch
    {
        WorldPlayback.Building => phase == WorkshopSimulationPhase.Building,
        WorldPlayback.Running => phase == WorkshopSimulationPhase.Running,
        WorldPlayback.Paused => phase == WorkshopSimulationPhase.Paused,
        WorldPlayback.Completed => phase == WorkshopSimulationPhase.Completed,
        WorldPlayback.Faulted => phase == WorkshopSimulationPhase.Faulted,
        _ => false
    };

    private static void Respond(ScheduleControlHeader request, ScheduleControlKind kind)
    {
        Result.AsSpan().Clear();
        WorkshopScheduleWire.WriteHeader(request with { Kind = kind, Sender = WorkshopRuntimeRole.Animation,
            Recipient = WorkshopRuntimeRole.Simulation }, Result.AsSpan(0, WorkshopScheduleWire.HeaderBytes));
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
        if (_schedule is not { } schedule || bytes.Length != WorkshopAnimationWire.ControlBytes)
            throw new InvalidOperationException("Animation installation is not available.");
        var cadence = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)));
        cadence.Validate();
        var control = WorkshopAnimationWire.ReadControl(bytes, Peer.Session, Peer.Generation, cadence);
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
        if (control.Target.Value >= WorkshopDeclaredPresentation.TargetBase)
        {
            if (control.Kind != AnimationControlKind.Visibility || control.World != schedule.World.WorldGeneration ||
                control.Generation != schedule.World.Epoch.Value || _declared is null)
                throw new ArgumentException("Foreign declared visibility control.");
            _declared.SetVisible(control.Target, control.Property, control.Visible);
            _commandSequence = control.Sequence;
            Emit(AnimationOutputKind.Acknowledgement, control.Sequence, control, control.From);
            return;
        }
        // This admission supports autonomous timed clips and world-bound constant endpoint drives.
        // Timed world feedback awaits its committed-history capability; it cannot borrow UI time.
        if (control.World.Value != 0 && control.Kind == AnimationControlKind.Reveal)
            throw new ArgumentException("Timed world channel requires a committed-world history.");
        var slot = -1; var vacant = -1;
        for (var i = 0; i < Instances.Length; i++)
        {
            if (Instances[i].Handle is null) vacant = i;
            else if (Instances[i].Control.Target == control.Target) slot = i;
        }
        if (slot < 0) slot = vacant;
        if (slot < 0)
        {
            // A new target with every track in use: grow both tables (no target capacity).
            slot = Instances.Length;
            Array.Resize(ref Instances, Instances.Length * 2);
            Tracks.EnsureCapacity(Instances.Length);
        }
        var previous = Instances[slot];
        if (previous.Handle is not null && (control.Property != previous.Control.Property || control.World != previous.Control.World ||
            control.Generation < previous.Control.Generation ||
            (control.Kind == AnimationControlKind.Visibility && control.Generation != previous.Control.Generation)))
            throw new ArgumentException("Stale animation target generation.");
        if (control.Kind == AnimationControlKind.Visibility && previous.Handle is null)
            throw new ArgumentException("Visibility requires its registered target.");
        if (!Clock.TryMasterNow(WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), out var now))
        {
            // A recalibrating mapping (counted CCGPU_CLOCK_RECALIBRATED telemetry) has no master time yet: the control
            // waits unapplied and unacknowledged, so the browser sends no other, until the mapping requalifies (Story 9.4b).
            Deferred.Hold(bytes, AnimationDeferralCause.Recalibrating);
            return;
        }
        if (control.Kind != AnimationControlKind.Visibility)
        {
            float from, to;
            bool start;
            if (control.Kind == AnimationControlKind.TimerObservation)
            {
                if (control.Timer.Phase == AnimationTimerPhase.Counting)
                {
                    from = AnimationTimerSegment.Create(control.Timer).Endpoint.Value;
                    to = (float)1;
                    start = true;
                }
                else if (control.Timer.Phase == AnimationTimerPhase.Finished)
                {
                    from = to = (float)1;
                    start = false;
                }
                else
                {
                    from = to = (float)0;
                    start = false;
                }
            }
            else
            {
                from = control.From;
                to = control.To;
                start = control.Kind == AnimationControlKind.Reveal;
            }
            var duration = control.Kind == AnimationControlKind.TimerObservation && control.Timer.Phase == AnimationTimerPhase.Counting
                ? (float)Math.Max(0.01, (control.Timer.Due - control.Timer.Observed) * (double)schedule.Settings.SimulationRate.Denominator / schedule.Settings.SimulationRate.Numerator)
                : control.Duration;
            var definition = new AnimationDefinition(WorkshopAnimationWire.Value(control.Property, from),
                WorkshopAnimationWire.Value(control.Property, to), new AnimationDurationSeconds(duration),
                control.Curve, AnimationRepeat.Once, AnimationClock.Presentation);
            if (previous.Handle is { } old) Tracks.Remove(old);
            var handle = Tracks.Register(new(control.Target, control.Property), definition);
            if (start)
            {
                Tracks.StartAt(handle, (double)now.Lower / WorkshopPulse.NanosecondsPerSecond);
                _lastOrdinal = Math.Max(_lastOrdinal, WorkshopPulse.Due(schedule.Settings.AnimationRate,
                    new(checked((long)now.Lower))).Value);
            }
            Tracks.SetVisible(handle, control.Visible);
            Instances[slot] = new() { Handle = handle, Control = control };
        }
        else
        {
            Tracks.SetVisible(previous.Handle!.Value, control.Visible);
            Instances[slot].Control = control;
        }
        _commandSequence = control.Sequence;
        Emit(AnimationOutputKind.Acknowledgement, control.Sequence, control,
            BitConverter.UInt32BitsToSingle(Tracks.Read(Instances[slot].Handle!.Value).Value.CanonicalBits));
    }

    private static void Emit(AnimationOutputKind kind, ulong ordinal, WorkshopAnimationControl control, float channel, CadenceRevision? cadence = null)
    {
        _ = WorkshopAnimationWire.Value(control.Property, channel);
        Output.AsSpan().Clear();
        Peer.Session.WriteTo(Output.AsSpan(0, RuntimeSessionId.ByteLength));
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(16), Peer.Generation.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(24), (cadence ?? _schedule!.Value.Revision).Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(32), control.Generation);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(40), ordinal);
        BinaryPrimitives.WriteInt64LittleEndian(Output.AsSpan(48), _lastApplied.Value);
        BinaryPrimitives.WriteSingleLittleEndian(Output.AsSpan(56), channel);
        BinaryPrimitives.WriteUInt16LittleEndian(Output.AsSpan(136), (ushort)control.Property);
        BinaryPrimitives.WriteUInt32LittleEndian(Output.AsSpan(60), (uint)kind);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(64), control.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(72), control.World.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(Output.AsSpan(80), control.EventOrdinal);
        BinaryPrimitives.WriteSingleLittleEndian(Output.AsSpan(84), control.EventPhase);
        WorkshopAnimationWire.WriteTimer(Output, control.Timer);
        SendOutput(Output);
    }
}
