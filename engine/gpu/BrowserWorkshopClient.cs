using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopClientId(int Value);
public sealed class WorkshopTransportException(WorkshopCommandIdentity identity, WorkshopTransportState state, Exception cause)
    : Exception("Workshop operation is indeterminate; recovery may require page reload.", cause)
{
    public WorkshopCommandIdentity Identity { get; } = identity;
    public WorkshopTransportState State { get; } = state;
}

/// <summary>Main-thread transport only; canonical state and numerical decisions remain in the worker.</summary>
public sealed partial class BrowserWorkshopClient : IWorkshopClient
{
    private readonly IWorkshopBrowserTransport _transport;
    private WorkshopClientId _id;
    private WorkshopClockPeer? _peer;
    private WorkshopClockMapping? _clock;
    private bool _started;
    private WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Master bootstrap is not installed.");
    private WorkshopClockMapping Clock => _clock ?? throw new InvalidOperationException("Master bootstrap is not installed.");
    private readonly WorkshopPoseHistory _history = new();
    private WorkshopConstruction _construction;
    private ulong _sequence;
    private int _pending;
    private bool _disposed;
    private readonly WorkshopMemoryQueryOwner _memoryQuery = new();
    private readonly WorkshopClientCursor _cursor;
    public WorkshopCommandIdentity? Pending { get; private set; }
    public WorkshopTransportState TransportState
    {
        get
        {
            if (_disposed) return WorkshopTransportState.RecoveryBlocked;
            var state = (WorkshopTransportState)_transport.Status(_id.Value);
            if (!Enum.IsDefined(state)) throw new InvalidOperationException("Unknown transport state.");
            return state;
        }
    }
    public SimulationEpoch Epoch => _cursor.Epoch;
    public AuthorityRevision Revision => _cursor.Revision;
    private BrowserWorkshopClient(RuntimeSessionId session, IWorkshopBrowserTransport transport)
    { session.Validate(); _cursor = new(session); _transport = transport; }

    public static async Task<BrowserWorkshopClient> Create()
    {
        if (!OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException("This Workshop requires a browser environment.");
        using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document")
            ?? throw new InvalidOperationException("A browser document is required.");
        var baseUri = document.GetPropertyAsString("baseURI")
            ?? throw new InvalidOperationException("The document resource origin is unavailable.");
        await JSHost.ImportAsync("workshopClient", new Uri(new Uri(baseUri), "workshop-client.js").AbsoluteUri);
        var identity = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(identity);
        return await Create(RuntimeSessionId.Read(identity), new BrowserWorkshopTransport());
    }

    internal static async Task<BrowserWorkshopClient> Create(RuntimeSessionId session, IWorkshopBrowserTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        var identity = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(identity, session.Low);
        BinaryPrimitives.WriteUInt64LittleEndian(identity.AsSpan(8), session.High);
        var client = new BrowserWorkshopClient(session, transport);
        try
        {
            client._construction = new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty);
            client._id = new(await client._transport.CreateClient([(int)WorkshopTransportState.Ready, (int)WorkshopTransportState.Backpressure,
                (int)WorkshopTransportState.TimedOut, (int)WorkshopTransportState.Indeterminate, (int)WorkshopTransportState.RecoveryBlocked],
                identity, [(int)NativeClockProfile.IsolatedWitnessedPerformanceNow,
                    WorkshopClockWire.PeerBytes, WorkshopClockWire.ProbeBytes, WorkshopClockWire.ReplyBytes,
                    WorkshopClockWire.DiagnosticBytes, (int)WorkshopRuntimeRole.Browser, (int)WorkshopRuntimeRole.Simulation],
                (int)WorkshopBuild.CaptureMode, client._construction.Settings.Presentation == PresentationCadence.AdmittedDisplay, client.ReceiveClockReply, client.ReceiveRead, client.ReceiveSchedule, client.ReceiveAnimation,
                client.Service));
            var peer = WorkshopClockWire.DecodePeer(client._transport.MasterBootstrap(client._id.Value));
            if (peer.Session != session || peer.Generation.Value != 1 || peer.RequesterGeneration.Value != 1 ||
                peer.RequesterRole != WorkshopRuntimeRole.Browser)
                throw new ArgumentException("Unexpected master bootstrap identity.");
            client._peer = peer;
            client._clock = new(peer);
            client._transport.AnimationKinds(client._id.Value, [(int)AnimationOutputKind.Acknowledgement, (int)AnimationOutputKind.Sample, (int)AnimationOutputKind.Rejected]);
            client._transport.ActivateClient(client._id.Value);
            if (client._construction.Settings.Presentation == PresentationCadence.AdmittedDisplay)
            {
                var display = client._transport.DisplayRate(client._id.Value);
                client._admittedDisplay = new(checked((uint)display[0]), checked((uint)display[1]));
                client._construction = client._construction with
                { Settings = client._construction.Settings with { PresentationRate = client._admittedDisplay } };
            }
            while (!client.Clock.IsQualified || !client._transport.AnimationQualified(client._id.Value))
            {
                client.ThrowIfTransportFailed();
                await Task.Delay(10);
            }
            var initialized = await client.Execute(WorkshopCommandKind.Initialize, settings: client._construction.Settings);
            if (initialized.Response.Result.Outcome != WorkshopCommandOutcome.Applied)
                throw new InvalidOperationException("The WebGPU simulation worker could not initialize.");
            client._transport.Qualified(client._id.Value);
            client._started = true;
            return client;
        }
        catch (Exception error)
        {
            WorkshopDiagnostics.Fault(FaultSite.Startup, error);
            // An unstarted client retires locally (DisposeAsync's startup path); the worker never saw a command.
            await client.DisposeAsync();
            throw;
        }
    }

    public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null, WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
    {
        _memoryQuery.AdmitCommand(kind);
        return ExecuteCore(kind, construction, target, settings, electrical);
    }

    private async Task<WorkshopDelivery> ExecuteCore(WorkshopCommandKind kind, WorkshopConstruction? construction = null, WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (kind == WorkshopCommandKind.Construct && _constructionOwner is not null)
            throw new InvalidOperationException("A construction candidate already owns the client.");
        if (_pending >= 2) throw new InvalidOperationException("Workshop command queue is full.");
        if (_sequence == ulong.MaxValue) throw new InvalidOperationException("Workshop command sequence is exhausted.");
        var sequence = new CommandSequence(_sequence + 1);
        // Pause targets the current drained endpoint; physical commits may advance after B sends it.
        var revisionKind = kind is WorkshopCommandKind.Pause or WorkshopCommandKind.Reset or WorkshopCommandKind.Cancel or WorkshopCommandKind.Dispose or WorkshopCommandKind.ConfigureElectrical
            or WorkshopCommandKind.ObserveMemory
            ? ExpectedRevisionKind.Any : ExpectedRevisionKind.Exact;
        var command = new WorkshopCommand(sequence, kind, Epoch,
            revisionKind == ExpectedRevisionKind.Any ? default : Revision, construction, target, revisionKind, Peer.Session, _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default, settings, electrical);
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        _sequence = sequence.Value;
        _pending++;
        var dispatchedReadOrder = _cursor.ReadOrder;
        var identity = new WorkshopCommandIdentity(sequence, command.Epoch, command.Revision, command.Cadence, command.Projection);
        if (kind != WorkshopCommandKind.Cancel) Pending = identity;
        if (construction is not null) { _proposedConstruction = construction; _constructionOwner = identity; }
        try
        {
            var encodedIdentity = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(encodedIdentity, sequence.Value);
            try { await _transport.Send(_id.Value, bytes, encodedIdentity); }
            catch (Exception error) { throw new WorkshopTransportException(identity, TransportState, error); }
            var response = WorkshopWireFlatBuffers.DecodeResponse(_transport.Acknowledgement(_id.Value, encodedIdentity));
            try
            {
                WorkshopMemoryQuery.ValidateCarriage(kind, response);
                var prepared = _cursor.PrepareAcknowledgement(command, response, dispatchedReadOrder, _disposed);
                if (prepared.Applicable)
                {
                    var boundary = response.Result.Outcome == WorkshopCommandOutcome.Applied ? kind switch
                    {
                        WorkshopCommandKind.Initialize or WorkshopCommandKind.Construct or WorkshopCommandKind.Reset => PresentationBoundary.Seed,
                        WorkshopCommandKind.Run => PresentationBoundary.Run,
                        _ => PresentationBoundary.Continuous
                    } : PresentationBoundary.Continuous;
                    WorkshopPoseHistory.Prepared? history = response.Read.Capture is null || _history.Latest is { } installed &&
                        installed.Read.HasSameContent(response.Read) ? null :
                        PrepareHistory(response, boundary, _transport.NativeNow());
                    _cursor.Commit(prepared);
                    if (history is { } ready) _history.Commit(ready);
                    if (response.Result.Outcome == WorkshopCommandOutcome.Applied && construction is { } admitted)
                    {
                        _construction = admitted;
                        EnsureInstalledScene(_construction);
                    }
                }
                return new(response, prepared.Applicable);
            }
            finally
            {
                // A valid terminal receipt releases its lease even when a newer read owns presentation.
                _transport.CompleteAcknowledgement(_id.Value, encodedIdentity);
            }
        }
        finally { _pending--; if (_constructionOwner == identity) { _proposedConstruction = null; _constructionOwner = null; } if (Pending == identity) Pending = null; }
    }

    /// <summary>The stopped memory observation query (Story 9A.3): one typed command whose acknowledgement carries the Simulation
    /// worker's measurement, paired with this context's own measurement taken after it.</summary>
    public Task<WorkshopMemoryReply> QueryMemory()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _memoryQuery.Execute(_pending, () => ExecuteCore(WorkshopCommandKind.ObserveMemory),
            () => !_disposed && _pending == 0, WorkshopMemoryMeasurement.Capture);
    }

    public Task<WorkshopDelivery> CancelPending()
    {
        var target = Pending ?? throw new InvalidOperationException("No pending operation to cancel.");
        return Execute(WorkshopCommandKind.Cancel, target: target);
    }

    public bool TryRead(out WorkshopResponse response)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfTransportFailed();
        return _history.TryTakeLatest(out response);
    }

    private void Service()
    {
        if (_disposed) return;
        var now = _transport.NativeNow();
        Clock.Observe(now);
#if PLAYTEST
        var pending = Clock.Pending;
#endif
        if (Clock.BeginProbe(now) is { } probe)
        {
#if PLAYTEST
            if (pending is { } expired) _transport.RecordClockObservation(_id.Value, WorkshopClockWire.EncodeExpiredProbe(expired, Peer, now));
#endif
            _transport.SendProbe(_id.Value, WorkshopClockWire.EncodeProbe(probe, Peer));
        }
    }

    private void ReceiveClockReply()
    {
        if (_disposed) return;
        var bytes = _transport.EventBytes(_id.Value);
        var received = _transport.EventTime(_id.Value);
        // The requesters' one reply path: a malformed reply is one counted, typed rejection, never applied or dropped.
        if (Clock.ReceiveEncoded(bytes, received) is not { } observation)
        {
#if PLAYTEST
            _transport.RecordClockObservation(_id.Value, WorkshopClockWire.EncodeRejectedReply(bytes, received));
#endif
            return;
        }
#if PLAYTEST
        _transport.RecordClockObservation(_id.Value, WorkshopClockWire.EncodeObservation(observation, Peer));
#endif
    }

    private void ReceiveRead()
    {
        if (_disposed) return;
        var bytes = _transport.EventBytes(_id.Value);
        var received = _transport.EventTime(_id.Value);
        // One decode: a refused read is its WireFaultException, a transport fault, never retried through another codec.
        var response = WorkshopWireFlatBuffers.DecodeResponse(bytes);
        if (_schedule is not { } active || response.Cadence.Value < active.Revision.Value ||
            response.Projection.Value < active.World.Epoch.Value) return;
        if (response.Cadence != active.Revision || response.Projection != active.World.Epoch)
            throw new ArgumentException("Future read cannot install a schedule.");
        var prepared = _cursor.PrepareRead(response);
        if (!prepared.Applicable) return;
        var boundary = response.Phase == WorkshopSimulationPhase.Completed ? PresentationBoundary.Completed : PresentationBoundary.Continuous;
        var history = PrepareHistory(response, boundary, received);
        EnsureInstalledScene(_construction);
        _cursor.Commit(prepared);
        _history.Commit(history);
    }

    private WorkshopPoseHistory.Prepared PrepareHistory(WorkshopResponse response, PresentationBoundary boundary, MonotonicNanoseconds now)
    {
        MappedCapture? mapped = response.Read.Capture is { } stamp && Clock.TryMap(stamp, now, out var captured) ? captured : null;
        return _history.Prepare(response, mapped, boundary, Clock.DisplayEpoch);
    }

    public bool TryPresent(ulong frame, out WorkshopPresentationSample sample)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfTransportFailed();
        if (!AdmitPresentationFrame(frame) || _framePoseTaken) { sample = default; return false; }
        _framePoseTaken = true;
        return _history.TryPresent(_frameObservedAt, _frameMaster, Clock, out sample);
    }

    public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene)
    {
        if (selected) _lastPresentationApplied = _frameMaster;
        if (selected && _awaitingPresented is { } awaiting)
        {
            SendScheduleResult(awaiting, ScheduleControlKind.Presented);
            _awaitingPresented = null;
        }
        #if PLAYTEST
        if (_disposed) return;
        _transport.RecordPresentationObservation(_id.Value, WorkshopPresentationWire.Encode(sample, selected, scene,
            _transport.NativeNow(), _schedule!.Value.Revision, _lastPresentation));
        #endif
    }

    private void ThrowIfTransportFailed()
    {
        if (TransportState is WorkshopTransportState.Indeterminate or WorkshopTransportState.RecoveryBlocked)
            throw new InvalidOperationException("Worker session is unavailable; its unacknowledged operations remain indeterminate.");
    }

    private WorkshopConstruction? _readConstruction;
    private PhysicsSceneDeclaration? _readScene;
    private void EnsureInstalledScene(WorkshopConstruction construction)
    {
        if (_readConstruction != construction)
        {
            _readScene = WorkshopPhysicsCompiler.Compile(construction, new(Peer.Session.Low, Peer.Session.High));
            _readConstruction = construction;
            InstallDeclaredTargets(construction);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (!_started)
        {
            // Startup failure: retire locally without a worker command.
            // A client whose browser registration failed has no id (browser ids start at 1).
            _disposed = true; _clock?.Retire(); _history.Retire();
            if (_id.Value != 0) _transport.DisposeClient(_id.Value);
            return;
        }
        try
        {
            var disposal = Execute(WorkshopCommandKind.Dispose);
            if (await Task.WhenAny(disposal, Task.Delay(1000)) == disposal) await disposal;
            // A late disposal failure is observed and logged; the client is already retired below.
            else _ = disposal.ContinueWith(task => WorkshopDiagnostics.Fault(FaultSite.LateDispose, task.Exception!),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
        finally { _disposed = true; Clock.Retire(); _history.Retire(); _transport.DisposeClient(_id.Value); }
    }
}

/// <summary>JavaScript implementation of the browser transport boundary.</summary>
internal sealed partial class BrowserWorkshopTransport : IWorkshopBrowserTransport
{
    [JSImport("animationQualified", "workshopClient")]
    private static partial bool JsAnimationQualified(int client);
    public bool AnimationQualified(int client) => JsAnimationQualified(client);
    [JSImport("animationKinds", "workshopClient")]
    private static partial void JsAnimationKinds(int client, int[] kinds);
    public void AnimationKinds(int client, int[] kinds) => JsAnimationKinds(client, kinds);
    [JSImport("displayRate", "workshopClient")]
    private static partial int[] JsDisplayRate(int client);
    public int[] DisplayRate(int client) => JsDisplayRate(client);
    [JSImport("scheduleResult", "workshopClient")]
    private static partial void JsScheduleResult(int client, byte[] bytes);
    public void ScheduleResult(int client, byte[] bytes) => JsScheduleResult(client, bytes);
    [JSImport("animationControl", "workshopClient")]
    private static partial void JsSendAnimationControl(int client, byte[] bytes);
    public void SendAnimationControl(int client, byte[] bytes) => JsSendAnimationControl(client, bytes);
    [JSImport("now", "workshopClient")]
    private static partial double JsNativeMilliseconds();
    public MonotonicNanoseconds NativeNow() => WorkshopNativeClock.FromMilliseconds(JsNativeMilliseconds());
    [JSImport("probe", "workshopClient")]
    private static partial void JsSendProbe(int client, byte[] probe);
    public void SendProbe(int client, byte[] probe) => JsSendProbe(client, probe);
    [JSImport("clockObservation", "workshopClient")]
    private static partial void JsRecordClockObservation(int client, byte[] observation);
    public void RecordClockObservation(int client, byte[] observation) => JsRecordClockObservation(client, observation);
    [JSImport("presentationObservation", "workshopClient")]
    private static partial void JsRecordPresentationObservation(int client, byte[] observation);
    public void RecordPresentationObservation(int client, byte[] observation) => JsRecordPresentationObservation(client, observation);
    [JSImport("create", "workshopClient")]
    private static partial Task<int> JsCreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
        [JSMarshalAs<JSType.Function>] Action clockReply,
        [JSMarshalAs<JSType.Function>] Action physicalRead,
        [JSMarshalAs<JSType.Function>] Action scheduleControl,
        [JSMarshalAs<JSType.Function>] Action animationOutput,
        [JSMarshalAs<JSType.Function>] Action service);
    public Task<int> CreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
        Action clockReply,
        Action physicalRead,
        Action scheduleControl,
        Action animationOutput,
        Action service) => JsCreateClient(states, bootstrap, clockAbi, captureMode, admitDisplay, clockReply, physicalRead, scheduleControl, animationOutput, service);
    [JSImport("bootstrap", "workshopClient")]
    private static partial byte[] JsMasterBootstrap(int client);
    public byte[] MasterBootstrap(int client) => JsMasterBootstrap(client);
    [JSImport("activate", "workshopClient")]
    private static partial void JsActivateClient(int client);
    public void ActivateClient(int client) => JsActivateClient(client);
    [JSImport("eventBytes", "workshopClient")]
    private static partial byte[] JsEventBytes(int client);
    public byte[] EventBytes(int client) => JsEventBytes(client);
    [JSImport("eventMilliseconds", "workshopClient")]
    private static partial double JsEventMilliseconds(int client);
    public MonotonicNanoseconds EventTime(int client) => WorkshopNativeClock.FromMilliseconds(JsEventMilliseconds(client));
    [JSImport("qualified", "workshopClient")]
    private static partial void JsQualified(int client);
    public void Qualified(int client) => JsQualified(client);
    [JSImport("status", "workshopClient")]
    private static partial int JsStatus(int client);
    public int Status(int client) => JsStatus(client);
    [JSImport("send", "workshopClient")]
    private static partial Task JsSend(int client, byte[] command, byte[] sequence);
    public Task Send(int client, byte[] command, byte[] sequence) => JsSend(client, command, sequence);
    [JSImport("acknowledgement", "workshopClient")]
    private static partial byte[] JsAcknowledgement(int client, byte[] sequence);
    public byte[] Acknowledgement(int client, byte[] sequence) => JsAcknowledgement(client, sequence);
    [JSImport("completeAcknowledgement", "workshopClient")]
    private static partial void JsCompleteAcknowledgement(int client, byte[] sequence);
    public void CompleteAcknowledgement(int client, byte[] sequence) => JsCompleteAcknowledgement(client, sequence);
    [JSImport("dispose", "workshopClient")]
    private static partial void JsDisposeClient(int client);
    public void DisposeClient(int client) => JsDisposeClient(client);
    [JSImport("readEvents", "workshopClient")]
    private static partial byte[] JsReadEvents(int client, string epoch);
    public byte[] ReadEvents(int client, string epoch) => JsReadEvents(client, epoch);
}
