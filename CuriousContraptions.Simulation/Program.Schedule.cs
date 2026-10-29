using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

public static partial class Program
{
    private static WorkshopSchedule? _schedule;
    private static ulong _nextProjection;
    private static bool _earlyPresented;
    private static WorkshopResponse? _preparedEndpoint;
    private static CommandSequence _installationOwner;
    private static byte[] _preparedPoseIdentity = [];
    [JSImport("preparePose", "workshopGpu")] private static partial int PreparePose(byte[] identity, int bodies, int capacity);
    [JSImport("commitPose", "workshopGpu")] private static partial void CommitPose(byte[] identity);
    [JSImport("discardPose", "workshopGpu")] private static partial void DiscardPose(byte[] identity);
    private static WorkshopCadenceSettings _preparedSettings;
    private static ScheduleTransition _transition, _preparedTransition;
    private static ScheduleControlKind _awaiting;
    private static TaskCompletionSource? _receipts;
    private static bool _browserReceived, _animationReceived;
    private static MasterTimeNanoseconds _browserApplied, _animationApplied;
    [JSImport("scheduleControl", "workshopGpu")] private static partial void SendScheduleControl(byte[] bytes, int recipient);

    [JSExport] public static int[] ScheduleRoles() => [(int)WorkshopRuntimeRole.Browser, (int)WorkshopRuntimeRole.Animation];

    private static MasterTimeNanoseconds MasterNow() => WorkshopPulse.FromNative(
        WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), Peer.MasterOrigin);

    [JSExport]
    public static byte[] AnimationPeer() => WorkshopClockWire.EncodePeer(Peer with { RequesterRole = WorkshopRuntimeRole.Animation });

    // The Simulation's continuous check of the clock master context's derived domain (worker.js startClockMaster).
    private static readonly WorkshopClockDerivation ClockDerivation = new(Diagnostics);

    [JSExport]
    public static int[] ClockDerivationProfile() =>
        [WorkshopClockDerivation.ExchangeTimeoutMilliseconds, WorkshopClockDerivation.CheckIntervalMilliseconds,
            WorkshopClockDerivation.CheckExchangeLimit];

    [JSExport] public static bool ClockDerivationQualified() => ClockDerivation.IsQualified;

    [JSExport]
    public static void QualifyClockDerivation(byte[] exchange) =>
        ClockDerivation.Qualify(ClockDerivationWire.DecodeExchange(exchange));

    [JSExport] public static void ClockDerivationQualificationMissed() => ClockDerivation.QualificationMissed();
    /// <summary>The derived startup correction the ring installs under the initial epoch before the session is admitted.</summary>
    [JSExport] public static byte[] ClockDerivationStartupCorrection() => ClockDerivationWire.EncodeCorrection(ClockDerivation.StartupCorrection, ClockDerivationOutcome.Consistent);

    /// <summary>An integral correction envelope, or no installation when the current mapping stands.</summary>
    [JSExport]
    public static byte[] CheckClockDerivation(byte[] exchange)
    {
        var outcome = ClockDerivation.Check(ClockDerivationWire.DecodeExchange(exchange), out var correction);
        return outcome switch
        {
            ClockDerivationOutcome.Rederived or ClockDerivationOutcome.Slewed => ClockDerivationWire.EncodeCorrection(correction, outcome),
            ClockDerivationOutcome.Consistent or ClockDerivationOutcome.Inconclusive => [],
            _ => throw new InvalidOperationException("Unexpected clock derivation check outcome."),
        };
    }

    [JSExport] public static void AdoptClockDerivation(int epoch) => ClockDerivation.Adopt(DerivationEpoch(epoch));
    [JSExport] public static void ClockDerivationMissed() => ClockDerivation.Missed();
    /// <summary>Whether a running check's exchange is narrow enough to be evidence; a wide one is retried within the check.</summary>
    [JSExport]
    public static bool ClockDerivationExchangeNarrow(byte[] exchange) =>
        WorkshopClockDerivation.IsNarrow(ClockDerivationWire.DecodeExchange(exchange));
    [JSExport] public static void ClockDerivationRetried() => ClockDerivation.Retried();

    private static ClockDerivationEpoch DerivationEpoch(int epoch) => epoch >= 0 ? new((uint)epoch)
        : throw new ArgumentOutOfRangeException(nameof(epoch), epoch, "A clock derivation epoch is nonnegative.");

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
                profile.Revision, Peer.Generation, projection, new(read.Tick.Value), Construction: construction);
            // Validate the complete endpoint before retaining a candidate or touching either recipient.
            var encoded = WorkshopWireFlatBuffers.EncodeResponse(endpoint);
            _installationOwner = owner; _preparedEndpoint = endpoint; _preparedSettings = construction.Settings; _preparedTransition = _transition;
            _preparedPoseIdentity = new byte[16];
            BinaryPrimitives.WriteUInt64LittleEndian(_preparedPoseIdentity, owner.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(_preparedPoseIdentity.AsSpan(8), read.Epoch.Value);
            BeginReceipts(ScheduleControlKind.Prepared);
            try
            {
                var pose = (PosePublicationPreparation)PreparePose(_preparedPoseIdentity, read.Bodies.Count, WorkshopPoseRing.Capacity(read.Bodies.Count));
                if (!Enum.IsDefined(pose)) throw new ArgumentException("Undefined pose preparation result.");
                if (pose == PosePublicationPreparation.StorageUnavailable)
                    throw new WorkshopInstallationException(WorkshopRejection.Capacity);
                SendPrepare(WorkshopRuntimeRole.Browser, encoded);
                SendPrepare(WorkshopRuntimeRole.Animation, encoded);
                await RequireReceipts();
            }
            catch (Exception error) when (error is WorkshopInstallationException or WorkshopStorageException or OutOfMemoryException)
            {
                // Superseded or already wrapped: keep it as it is.
                if (owner == _installationOwner) AbortInstallation();
                throw;
            }
            catch (Exception error)
            {
                if (owner == _installationOwner) AbortInstallation();
                throw new WorkshopInstallationException(error);
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
        // One control per Prepare (per installation, not per tick), exactly as wide as its endpoint read (Story 9R.5b).
        var payload = WorkshopScheduleWire.PrepareBytes(endpoint.Length);
        var control = new byte[WorkshopScheduleWire.HeaderBytes + payload];
        WorkshopScheduleWire.WriteHeader(Header(ScheduleControlKind.Prepare, recipient), control.AsSpan(0, WorkshopScheduleWire.HeaderBytes), payload);
        var data = control.AsSpan(WorkshopScheduleWire.HeaderBytes);
        WorkshopCadenceWire.Write(_preparedSettings, data[..32]);
        BinaryPrimitives.WriteUInt64LittleEndian(data[32..], _schedule?.Revision.Value ?? 0);
        BinaryPrimitives.WriteUInt64LittleEndian(data[40..], _schedule?.World.Epoch.Value ?? 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data[48..], (uint)_preparedTransition);
        var read = _preparedEndpoint!.Value;
        BinaryPrimitives.WriteUInt64LittleEndian(data[56..], read.Read.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[64..], read.Read.Tick.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[72..], read.Publication.Value);
        endpoint.CopyTo(data[WorkshopScheduleWire.PrepareFieldBytes..]);
        SendScheduleControl(control, (int)recipient);
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
        CommitPose(_preparedPoseIdentity);
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
        _preparedEndpoint = null; _installationOwner = default; _preparedPoseIdentity = [];
    }

    private static void AbortInstallation()
    {
        if (_preparedEndpoint is null) return;
        DiscardPose(_preparedPoseIdentity);
        foreach (var recipient in new[] { WorkshopRuntimeRole.Browser, WorkshopRuntimeRole.Animation })
        {
            var bytes = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.AbortBytes];
            WorkshopScheduleWire.WriteHeader(Header(ScheduleControlKind.Abort, recipient), bytes.AsSpan(0, 64));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(64), (uint)WorkshopRejection.Transport);
            SendScheduleControl(bytes, (int)recipient);
        }
        _receipts?.TrySetException(new WorkshopInstallationException());
        _receipts = null; _preparedEndpoint = null; _installationOwner = default; _preparedPoseIdentity = [];
    }
}
