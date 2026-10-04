using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using CuriousContraptions.Gpu;

public static partial class Program
{
    private static WorkshopSchedule? _schedule;
    private static ulong _nextProjection, _nextCadence = 1;
    private static bool _earlyPresented;
    private static WorkshopResponse? _preparedEndpoint;
    private static CommandSequence _installationOwner;
    private static WorkshopCadenceSettings _preparedSettings;
    private static ScheduleTransition _transition, _preparedTransition;
    private static ScheduleControlKind _awaiting;
    private static TaskCompletionSource? _receipts;
    private static bool _browserReceived, _animationReceived;
    private static MasterTimeNanoseconds _browserApplied, _animationApplied;
    private static readonly byte[] ControlOutput = new byte[WorkshopScheduleWire.MaximumControlBytes];
    [JSImport("scheduleControl", "workshopGpu")] private static partial void SendScheduleControl(byte[] bytes, int recipient);

    [JSExport] public static int[] ScheduleRoles() => [(int)WorkshopRuntimeRole.Browser, (int)WorkshopRuntimeRole.Animation];

    private static MasterTimeNanoseconds MasterNow() => WorkshopPulse.FromNative(
        WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), Peer.MasterOrigin);

    [JSExport]
    public static byte[] AnimationPeer() => WorkshopClockWire.EncodePeer(Peer with { RequesterRole = WorkshopRuntimeRole.Animation });

    [JSExport]
    public static byte[] AnimationClockReply(byte[] bytes, double receivedMilliseconds) =>
        WorkshopClockWire.EncodeReply(bytes, Peer with { RequesterRole = WorkshopRuntimeRole.Animation },
            WorkshopNativeClock.FromMilliseconds(receivedMilliseconds), WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()));

    private sealed class Installation : IWorkshopInstallation
    {
        public void Abort(CommandSequence owner) { if (owner == _installationOwner) AbortInstallation(); }
        public void Retire() => AbortInstallation();
        public async ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase)
        {
            if (owner.Value == 0 || _preparedEndpoint is not null || _receipts is not null) throw new InvalidOperationException("A schedule candidate is already owned.");
            var projection = new ProjectionEpoch(checked(++_nextProjection));
            var endpoint = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), phase, read, Peer.Session, default,
                profile.Revision, Peer.Generation, projection, new(read.Tick.Value));
            // Validate the complete endpoint before retaining a candidate or touching either recipient.
            var encoded = WorkshopWire.Encode(endpoint);
            _installationOwner = owner; _preparedEndpoint = endpoint; _preparedSettings = construction.Settings; _preparedTransition = _transition;
            BeginReceipts(ScheduleControlKind.Prepared);
            try
            {
                SendPrepare(WorkshopRuntimeRole.Browser, encoded);
                SendPrepare(WorkshopRuntimeRole.Animation, encoded);
                await RequireReceipts();
            }
            catch
            {
                if (owner == _installationOwner) AbortInstallation();
                throw new WorkshopInstallationException();
            }
        }
    }

    private static ScheduleControlHeader Header(ScheduleControlKind kind, WorkshopRuntimeRole recipient)
    {
        var endpoint = _preparedEndpoint ?? throw new InvalidOperationException("No owned schedule candidate.");
        return new(kind, Peer.Session, Peer.Generation, endpoint.Cadence, endpoint.Projection,
            WorkshopRuntimeRole.Simulation, recipient);
    }

    private static void SendPrepare(WorkshopRuntimeRole recipient, byte[] endpoint)
    {
        ControlOutput.AsSpan().Clear();
        WorkshopScheduleWire.WriteHeader(Header(ScheduleControlKind.Prepare, recipient), ControlOutput.AsSpan(0, 64));
        var data = ControlOutput.AsSpan(64);
        WorkshopCadenceWire.Write(_preparedSettings, data[..32]);
        BinaryPrimitives.WriteUInt64LittleEndian(data[32..], _schedule?.Revision.Value ?? 0);
        BinaryPrimitives.WriteUInt64LittleEndian(data[40..], _schedule?.World.Epoch.Value ?? 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data[48..], (uint)_preparedTransition);
        var read = _preparedEndpoint!.Value;
        BinaryPrimitives.WriteUInt64LittleEndian(data[56..], read.Read.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[64..], read.Read.Tick.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[72..], read.Publication.Value);
        endpoint.CopyTo(data[80..]);
        SendScheduleControl(ControlOutput, (int)recipient);
    }

    private static void BeginReceipts(ScheduleControlKind kind)
    {
        _awaiting = kind; _browserReceived = _animationReceived = false;
        if (kind == ScheduleControlKind.Prepared) _earlyPresented = false;
        _receipts = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (kind == ScheduleControlKind.Presented && _earlyPresented) _receipts.TrySetResult();
    }
    private static async Task RequireReceipts()
    {
        var receipts = _receipts ?? throw new InvalidOperationException("No receipt owner.");
        try { await receipts.Task.WaitAsync(TimeSpan.FromMilliseconds(250)); }
        finally { if (ReferenceEquals(_receipts, receipts)) _receipts = null; }
    }

    [JSExport]
    public static void ScheduleResult(byte[] bytes, int sourceRole)
    {
        var source = (WorkshopRuntimeRole)sourceRole;
        var header = WorkshopScheduleWire.ReadHeader(bytes);
        if (_preparedEndpoint is not { } endpoint || _receipts is null || !Enum.IsDefined(source) ||
            header.Sender != source || header.Recipient != WorkshopRuntimeRole.Simulation ||
            (header.Kind != _awaiting && !(header.Kind == ScheduleControlKind.Presented && _awaiting == ScheduleControlKind.Applied)) || header.Session != Peer.Session || header.MasterGeneration != Peer.Generation ||
            header.Revision != endpoint.Cadence || header.Projection != endpoint.Projection ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(72)) != (uint)WorkshopCommandOutcome.Applied ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(76)) != (uint)WorkshopRejection.None)
            throw new ArgumentException("Foreign or stale schedule receipt.");
        var applied = new MasterTimeNanoseconds(BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(64)));
        applied.Validate();
        if (applied.Value > MasterNow().Value) throw new ArgumentException("Future applied timestamp.");
        if (header.Kind == ScheduleControlKind.Presented && _awaiting == ScheduleControlKind.Applied)
        {
            if (source != WorkshopRuntimeRole.Browser || _earlyPresented || !_browserReceived ||
                _preparedTransition is not (ScheduleTransition.Step or ScheduleTransition.Completed or ScheduleTransition.Reset) ||
                applied.Value < _browserApplied.Value) throw new ArgumentException("Unowned early presentation receipt.");
            _earlyPresented = true; _browserApplied = applied; return;
        }
        switch (source)
        {
            case WorkshopRuntimeRole.Browser:
                if (_browserReceived || applied.Value < _browserApplied.Value) throw new ArgumentException("Duplicate or reversed browser receipt.");
                _browserApplied = applied; _browserReceived = true; break;
            case WorkshopRuntimeRole.Animation:
                if (_animationReceived || _awaiting == ScheduleControlKind.Presented || applied.Value < _animationApplied.Value)
                    throw new ArgumentException("Duplicate or reversed Animation receipt.");
                _animationApplied = applied; _animationReceived = true; break;
            default: throw new ArgumentException("Wrong schedule recipient.");
        }
        if (_browserReceived && (_animationReceived || _awaiting == ScheduleControlKind.Presented))
            _receipts.TrySetResult();
    }

    private static async Task CommitInstallation()
    {
        var endpoint = _preparedEndpoint ?? throw new InvalidOperationException("Physical commit lacks a prepared schedule.");
        var installed = MasterNow();
        var playback = endpoint.Phase switch
        {
            WorkshopSimulationPhase.Building => WorldPlayback.Building,
            WorkshopSimulationPhase.Running => WorldPlayback.Running,
            WorkshopSimulationPhase.Paused => WorldPlayback.Paused,
            WorkshopSimulationPhase.Completed => WorldPlayback.Completed,
            _ => throw new ArgumentException("Unsupported installed phase.")
        };
        var schedule = WorkshopSchedule.Create(_preparedSettings, endpoint.Cadence, Peer.Generation, Peer.MasterOrigin,
            installed, _animationApplied, _browserApplied,
            new(endpoint.Read.Epoch, endpoint.Projection, endpoint.Read.Tick, installed, playback));
        _schedule = schedule; // Physical commit already happened; any following failure retires the session.
        BeginReceipts(ScheduleControlKind.Applied);
        foreach (var recipient in new[] { WorkshopRuntimeRole.Browser, WorkshopRuntimeRole.Animation })
        {
            var bytes = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.ScheduleBytes];
            WorkshopScheduleWire.WriteCommit(Header(ScheduleControlKind.Commit, recipient), schedule, bytes);
            SendScheduleControl(bytes, (int)recipient);
        }
        await RequireReceipts();
        if (_preparedTransition is ScheduleTransition.Step or ScheduleTransition.Completed or ScheduleTransition.Reset)
        {
            BeginReceipts(ScheduleControlKind.Presented);
            await RequireReceipts();
        }
        _preparedEndpoint = null; _installationOwner = default;
    }

    private static void AbortInstallation()
    {
        if (_preparedEndpoint is null) return;
        foreach (var recipient in new[] { WorkshopRuntimeRole.Browser, WorkshopRuntimeRole.Animation })
        {
            var bytes = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.AbortBytes];
            WorkshopScheduleWire.WriteHeader(Header(ScheduleControlKind.Abort, recipient), bytes.AsSpan(0, 64));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(64), (uint)WorkshopRejection.Transport);
            SendScheduleControl(bytes, (int)recipient);
        }
        _receipts?.TrySetException(new WorkshopInstallationException());
        _receipts = null; _preparedEndpoint = null; _installationOwner = default;
    }
}
