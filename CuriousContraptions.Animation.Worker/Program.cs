using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

[assembly: SupportedOSPlatform("browser")]

/// <summary>Independent owner of the existing hint animation; all evaluation uses S master time.</summary>
public static partial class Program
{
    private static WorkshopClockPeer? _peer;
    private static WorkshopClockMapping? _clock;
    private static WorkshopSchedule? _schedule;
    private static Preparation? _prepared;
    private static byte[]? _queuedControl;
    private static readonly AnimationBatch Tracks = new(2);
    private static readonly Track[] Instances = new Track[2];
    private struct Track
    {
        public AnimationHandle? Handle;
        public WorkshopOpacityControl Control;
    }
    private static ulong _commandSequence, _lastOrdinal, _evaluationSequence;
    private static MasterTimeNanoseconds _lastApplied;
    private static readonly byte[] Output = new byte[WorkshopHintWire.OutputBytes];
    private static readonly byte[] Result = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.ResultBytes];
    private readonly record struct Preparation(ScheduleControlHeader Header, WorkshopCadenceSettings Settings,
        SimulationEpoch World, SimulationTick Tick, WorkshopSimulationPhase Phase);
    private static WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Animation master is not installed.");
    private static WorkshopClockMapping Clock => _clock ?? throw new InvalidOperationException("Animation mapping is not installed.");

    public static void Main() { }
    [JSExport] public static int[] OutputKinds() => [(int)HintOutputKind.Acknowledgement, (int)HintOutputKind.Sample, (int)HintOutputKind.Rejected];
    [JSImport("now", "workshopAnimation")] private static partial double NativeMilliseconds();
    [JSImport("probe", "workshopAnimation")] private static partial void SendProbe(byte[] bytes);
    [JSImport("control", "workshopAnimation")] private static partial void SendControl(byte[] bytes);
    [JSImport("output", "workshopAnimation")] private static partial void SendOutput(byte[] bytes);
    [JSImport("qualified", "workshopAnimation")] private static partial void Qualified();
    [JSImport("acceptedReply", "workshopAnimation")] private static partial void AcceptedReply();

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
        var observation = Clock.Receive(WorkshopClockWire.DecodeReply(bytes, Peer, receipt));
        if (observation.Outcome == ClockProbeOutcome.Accepted) AcceptedReply();
        if (Clock.IsQualified) Qualified();
    }

    [JSExport]
    public static void Service()
    {
        var now = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        if (Clock.BeginProbe(now) is { } probe) SendProbe(WorkshopClockWire.EncodeProbe(probe, Peer));
        if (_prepared is not null || _schedule is not { } schedule || !Clock.TryMasterNow(now, out var interval)) return;
        var due = WorkshopPulse.Due(schedule.Settings.AnimationRate, new(checked((long)interval.Lower)));
        if (due.Value < schedule.FirstAnimation.Value || due.Value <= _lastOrdinal) return;
        // Coalesce cosmetic evaluation opportunities, never create a Task for an individual pulse.
        Tracks.Advance(checked(++_evaluationSequence), (double)due.Value * schedule.Settings.AnimationRate.Denominator /
            schedule.Settings.AnimationRate.Numerator, 0);
        _lastApplied = WorkshopPulse.Deadline(schedule.Settings.AnimationRate, due);
        for (var i = 0; i < Instances.Length; i++)
            if (Instances[i].Handle is { } handle)
                Emit(HintOutputKind.Sample, due.Value, Instances[i].Control, Tracks.Read(handle).Value.Opacity.Value);
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
                var endpoint = WorkshopWire.DecodeResponse(data[80..]);
                if (endpoint.Session != Peer.Session || endpoint.MasterGeneration != Peer.Generation ||
                    endpoint.Cadence != header.Revision || endpoint.Projection != header.Projection ||
                    endpoint.Read.Epoch.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[56..]) ||
                    endpoint.Read.Tick.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[64..]) ||
                    endpoint.Publication.Value != BinaryPrimitives.ReadUInt64LittleEndian(data[72..]))
                    throw new ArgumentException("Prepared Animation endpoint differs from its envelope.");
                _prepared = new(header, settings, endpoint.Read.Epoch, endpoint.Read.Tick, endpoint.Phase);
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
        if (_queuedControl is not { } bytes) return;
        _queuedControl = null; HintControl(bytes);
    }

    [JSExport]
    public static void HintControl(byte[] bytes)
    {
        if (_schedule is not { } schedule || bytes.Length != WorkshopHintWire.ControlBytes)
            throw new InvalidOperationException("Animation installation is not available.");
        var cadence = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)));
        cadence.Validate();
        var control = WorkshopHintWire.ReadControl(bytes, Peer.Session, Peer.Generation, cadence);
        if (control.Sequence <= _commandSequence || cadence.Value > schedule.Revision.Value ||
            control.World.Value > schedule.World.WorldGeneration.Value)
            throw new ArgumentException("Foreign opacity control.");
        if (_prepared is not null)
        {
            if (_queuedControl is not null) throw new InvalidOperationException("Deferred animation control capacity exceeded.");
            _queuedControl = bytes; return;
        }
        if (cadence.Value < schedule.Revision.Value ||
            (control.World.Value != 0 && control.World != schedule.World.WorldGeneration))
        {
            _commandSequence = control.Sequence;
            Emit(HintOutputKind.Rejected, control.Sequence, control, control.From, cadence); return;
        }
        // This admission supports autonomous timed clips and world-bound constant endpoint drives.
        // Timed world feedback awaits its committed-history capability; it cannot borrow UI time.
        if (control.World.Value != 0 && control.Kind == HintControlKind.Reveal)
            throw new ArgumentException("Timed world opacity requires a committed-world history.");
        var slot = -1; var vacant = -1;
        for (var i = 0; i < Instances.Length; i++)
        {
            if (Instances[i].Handle is null) vacant = i;
            else if (Instances[i].Control.Target == control.Target) slot = i;
        }
        if (slot < 0) slot = vacant;
        if (slot < 0) throw new InvalidOperationException("Animation target capacity exceeded.");
        var previous = Instances[slot];
        if (previous.Handle is not null && (control.World != previous.Control.World ||
            control.Generation < previous.Control.Generation ||
            (control.Kind == HintControlKind.Visibility && control.Generation != previous.Control.Generation)))
            throw new ArgumentException("Stale animation target generation.");
        if (control.Kind == HintControlKind.Visibility && previous.Handle is null)
            throw new ArgumentException("Visibility requires its registered target.");
        if (!Clock.TryMasterNow(WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), out var now))
            throw new InvalidOperationException("Animation control requires qualified master time.");
        if (control.Kind != HintControlKind.Visibility)
        {
            var definition = new AnimationDefinition(new AnimationValue(new AnimationOpacity(control.From)),
                new AnimationValue(new AnimationOpacity(control.To)), new AnimationDurationSeconds(control.Duration),
                control.Curve, AnimationRepeat.Once, AnimationClock.Presentation);
            if (previous.Handle is { } old) Tracks.Remove(old);
            var handle = Tracks.Register(new(control.Target, AnimationProperty.Opacity), definition);
            if (control.Kind == HintControlKind.Reveal)
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
        Emit(HintOutputKind.Acknowledgement, control.Sequence, control,
            Tracks.Read(Instances[slot].Handle!.Value).Value.Opacity.Value);
    }

    private static void Emit(HintOutputKind kind, ulong ordinal, WorkshopOpacityControl control, Half opacity, CadenceRevision? cadence = null)
    {
        if (!Half.IsFinite(opacity) || opacity < (Half)0 || opacity > (Half)1)
            throw new ArgumentException("Invalid evaluated opacity.");
        Output.AsSpan().Clear();
        WorkshopWire.WriteSession(Output, Peer.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(16), Peer.Generation.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(24), (cadence ?? _schedule!.Value.Revision).Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(32), control.Generation);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(40), ordinal);
        BinaryPrimitives.WriteInt64LittleEndian(Output.AsSpan(48), _lastApplied.Value);
        BinaryPrimitives.WriteUInt16LittleEndian(Output.AsSpan(56), BitConverter.HalfToUInt16Bits(opacity));
        BinaryPrimitives.WriteUInt32LittleEndian(Output.AsSpan(60), (uint)kind);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(64), control.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(Output.AsSpan(72), control.World.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(Output.AsSpan(80), control.EventOrdinal);
        BinaryPrimitives.WriteUInt16LittleEndian(Output.AsSpan(84), BitConverter.HalfToUInt16Bits(control.EventPhase));
        SendOutput(Output);
    }
}
