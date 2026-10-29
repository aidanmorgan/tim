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
    private readonly WorkshopObservation _observation;
    public IWorkshopObservation Observation => _observation;
    public WorkshopDiagnostics Diagnostics { get; }
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
    private BrowserWorkshopClient(WorkshopObservation observation, IWorkshopBrowserTransport transport, WorkshopDiagnostics diagnostics)
    {
        var session = observation.Session;
        session.Validate(); _cursor = new(session); _transport = transport;
        Diagnostics = diagnostics;
        _observation = observation;
    }

    public static async Task<BrowserWorkshopClient> Create(WorkshopDiagnostics? diagnostics = null)
    {
        var journal = diagnostics ?? new();
        var observation = WorkshopObservation.Begin(journal);
        try
        {
            if (!OperatingSystem.IsBrowser())
                throw new PlatformNotSupportedException("This Workshop requires a browser environment.");
            await BrowserWorkshopObservation.Start(observation);
            return await Create(observation, journal);
        }
        catch (Exception error)
        {
            journal.Fault(FaultSite.Startup, error);
            observation.Retire(error.ToString());
            throw;
        }
    }

    internal static async Task<BrowserWorkshopClient> Create(WorkshopObservation observation, WorkshopDiagnostics diagnostics)
    {
        try
        {
            if (!OperatingSystem.IsBrowser())
                throw new PlatformNotSupportedException("This Workshop requires a browser environment.");
            using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document")
                ?? throw new InvalidOperationException("A browser document is required.");
            var baseUri = document.GetPropertyAsString("baseURI")
                ?? throw new InvalidOperationException("The document resource origin is unavailable.");
            await JSHost.ImportAsync("workshopClient", new Uri(new Uri(baseUri), "workshop-client.js").AbsoluteUri);
            return await Create(observation, new BrowserWorkshopTransport(), diagnostics);
        }
        catch (Exception error)
        {
            diagnostics.Fault(FaultSite.Startup, error);
            observation.Retire(error.ToString());
            throw;
        }
    }

    internal static async Task<BrowserWorkshopClient> Create(WorkshopObservation observation, IWorkshopBrowserTransport transport, WorkshopDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(transport);
        var session = observation.Session;
        var identity = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(identity, session.Low);
        BinaryPrimitives.WriteUInt64LittleEndian(identity.AsSpan(8), session.High);
        var client = new BrowserWorkshopClient(observation, transport, diagnostics);
        try
        {
            client._construction = new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty);
            client._id = new(await client._transport.CreateClient([(int)WorkshopTransportState.Ready, (int)WorkshopTransportState.Backpressure,
                (int)WorkshopTransportState.TimedOut, (int)WorkshopTransportState.Indeterminate, (int)WorkshopTransportState.RecoveryBlocked],
                identity, [(int)NativeClockProfile.IsolatedWitnessedPerformanceNow,
                    WorkshopClockWire.PeerBytes, WorkshopClockWire.ProbeBytes, WorkshopClockWire.ReplyBytes,
                    (int)WorkshopRuntimeRole.Browser, (int)WorkshopRuntimeRole.Simulation],
                (int)WorkshopBuild.CaptureMode, client._construction.Settings.Presentation == PresentationCadence.AdmittedDisplay, client.ReceiveClockReply, client.ReceiveRead, client.ReceiveSchedule, client.ReceiveAnimation,
                client.Service, client.ReceiveObservation, client.ReceiveRetirement));
            client._observation.BindEvents(() => client._transport.OpenObservationEvents(client._id.Value),
                (cursor, epoch) => client._transport.ReadObservationEvents(client._id.Value, cursor, epoch.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                cursor => client._transport.CloseObservationEvents(client._id.Value, cursor));
            var peer = WorkshopClockWire.DecodePeer(client._transport.MasterBootstrap(client._id.Value));
            if (peer.Session != session || peer.Generation.Value != 1 || peer.RequesterGeneration.Value != 1 ||
                peer.RequesterRole != WorkshopRuntimeRole.Browser)
                throw new ArgumentException("Unexpected master bootstrap identity.");
            client._peer = peer;
            client._clock = new(peer, client.Diagnostics);
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
            client.Diagnostics.Fault(FaultSite.Startup, error);
            observation.Retire(error.ToString());
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
        WorkshopConstruction? authored = kind switch
        {
            WorkshopCommandKind.Initialize => _construction with { Settings = settings!.Value },
            WorkshopCommandKind.Construct => construction!.Value,
            WorkshopCommandKind.ConfigureCadence => _construction with { Settings = settings!.Value },
            _ => null
        };
        var scene = authored is { } source
            ? WorkshopSceneCompiler.Compile(source, new(Peer.Session.Low, Peer.Session.High), new(sequence.Value)) : null;
        var command = new WorkshopCommand(sequence, kind, Epoch,
            revisionKind == ExpectedRevisionKind.Any ? default : Revision, scene, target, revisionKind, Peer.Session,
            _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default, electrical);
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        if (authored is { } original) _observation.Associate(command, original);
        if (kind == WorkshopCommandKind.Run) _observation.BeginRun(command);
        _sequence = sequence.Value;
        _pending++;
        var dispatchedReadOrder = _cursor.ReadOrder;
        var identity = new WorkshopCommandIdentity(sequence, command.Epoch, command.Revision, command.Cadence, command.Projection);
        if (kind != WorkshopCommandKind.Cancel) Pending = identity;
        if (authored is not null) { _proposedConstruction = authored; _proposedScene = scene; _constructionOwner = identity; }
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
                _observation.Receipt(command, response, prepared.Applicable);
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
                    _observation.Commit(response);
                    if (history is { } ready) _history.Commit(ready);
                    if (response.Result.Outcome == WorkshopCommandOutcome.Applied && authored is { } admitted)
                    {
                        _construction = admitted;
                        EnsureInstalledScene(_construction, scene!);
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
        finally { _observation.CompleteCandidate(command); _pending--; if (_constructionOwner == identity) { _proposedConstruction = null; _proposedScene = null; _constructionOwner = null; } if (Pending == identity) Pending = null; }
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
        var pending = Clock.Pending;
        if (Clock.BeginProbe(now) is { } probe)
        {
            if (pending is { } expired) Diagnostics.Capture(() => new WorkshopClockSampleDiagnostic(expired, null, ClockProbeOutcome.Expired, default, now));
            _transport.SendProbe(_id.Value, WorkshopClockWire.EncodeProbe(probe, Peer));
        }
    }

    private void ReceiveObservation()
    {
        try
        {
            var batch = WorkshopWireFlatBuffers.DecodeObservations(_transport.EventBytes(_id.Value));
            if (batch.Session != Observation.Session || batch.Role is not (WorkshopRuntimeRole.Simulation or WorkshopRuntimeRole.Animation))
                throw new ArgumentException("Foreign observation session or runtime.");
            foreach (var record in batch.Records) Diagnostics.Record(record);
            if (batch.Incomplete is { } detail)
            {
                Diagnostics.MarkIncomplete(detail);
                _observation.Retire(detail);
            }
        }
        catch (Exception error) when (error is WireFaultException or ArgumentException or InvalidOperationException)
        {
            Diagnostics.Fault(FaultSite.WorkerEnvelope, error);
            _observation.Retire(error.ToString());
        }
    }
    private void ReceiveRetirement(string detail)
    {
        Diagnostics.Fault(FaultSite.Transport, new InvalidOperationException(detail));
        _observation.Retire(detail);
    }

    private void ReceiveClockReply()
    {
        if (_disposed) return;
        var bytes = _transport.EventBytes(_id.Value);
        var received = _transport.EventTime(_id.Value);
        // The requesters' one reply path: a malformed reply is one counted, typed rejection, never applied or dropped.
        if (Clock.ReceiveEncoded(bytes, received) is { } observation)
            Diagnostics.Capture(() => new WorkshopClockSampleDiagnostic(observation.Reply.Probe, observation.Reply, observation.Outcome, observation.Offset, received));
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
        EnsureInstalledScene(_construction, _compiledScene ?? throw new InvalidOperationException("Read has no installed compiled scene."));
        _cursor.Commit(prepared);
        _observation.Commit(response);
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
        if (_disposed) return;
        Diagnostics.Capture(() => new WorkshopPresentationDiagnostic(sample, selected, scene,
            _transport.NativeNow(), _schedule!.Value.Revision, _lastPresentation));
    }

    private void ThrowIfTransportFailed()
    {
        if (TransportState is WorkshopTransportState.Indeterminate or WorkshopTransportState.RecoveryBlocked)
            throw new InvalidOperationException("Worker session is unavailable; its unacknowledged operations remain indeterminate.");
    }

    private WorkshopConstruction? _readConstruction;
    private PhysicsSceneDeclaration? _readScene;
    private WorkshopCompiledScene? _compiledScene, _proposedScene;
    private void EnsureInstalledScene(WorkshopConstruction construction, WorkshopCompiledScene scene)
    {
        if (_compiledScene?.Identity != scene.Identity)
        {
            _readScene = scene.Physics;
            _compiledScene = scene;
            _readConstruction = construction;
            InstallDeclaredTargets(scene);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (!_started)
        {
            // Startup failure: retire locally without a worker command.
            // A client whose browser registration failed has no id (browser ids start at 1).
            _observation.Retire("Worker session disposed."); _disposed = true; _clock?.Retire(); _history.Retire();
            if (_id.Value != 0) _transport.DisposeClient(_id.Value);
            return;
        }
        try
        {
            var disposal = Execute(WorkshopCommandKind.Dispose);
            if (await Task.WhenAny(disposal, Task.Delay(1000)) == disposal) await disposal;
            // A late disposal failure is observed and logged; the client is already retired below.
            else _ = disposal.ContinueWith(task => Diagnostics.Fault(FaultSite.LateDispose, task.Exception!),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
        finally { _observation.Retire("Worker session disposed."); _disposed = true; Clock.Retire(); _history.Retire(); _transport.DisposeClient(_id.Value); }
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
    [JSImport("create", "workshopClient")]
    private static partial Task<int> JsCreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
        [JSMarshalAs<JSType.Function>] Action clockReply,
        [JSMarshalAs<JSType.Function>] Action physicalRead,
        [JSMarshalAs<JSType.Function>] Action scheduleControl,
        [JSMarshalAs<JSType.Function>] Action animationOutput,
        [JSMarshalAs<JSType.Function>] Action service,
        [JSMarshalAs<JSType.Function>] Action observation,
        [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> retired);
    public Task<int> CreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
        Action clockReply,
        Action physicalRead,
        Action scheduleControl,
        Action animationOutput,
        Action service, Action observation, Action<string> retired) => JsCreateClient(states, bootstrap, clockAbi, captureMode, admitDisplay, clockReply, physicalRead, scheduleControl, animationOutput, service, observation, retired);
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
    [JSImport("openObservationEvents", "workshopClient")]
    private static partial int JsOpenObservationEvents(int client);
    public int OpenObservationEvents(int client) => JsOpenObservationEvents(client);
    [JSImport("readObservationEvents", "workshopClient")]
    private static partial byte[] JsReadObservationEvents(int client, int cursor, string epoch);
    public byte[] ReadObservationEvents(int client, int cursor, string epoch) => JsReadObservationEvents(client, cursor, epoch);
    [JSImport("closeObservationEvents", "workshopClient")]
    private static partial void JsCloseObservationEvents(int client, int cursor);
    public void CloseObservationEvents(int client, int cursor) => JsCloseObservationEvents(client, cursor);
    [JSImport("readEvents", "workshopClient")]
    private static partial byte[] JsReadEvents(int client, string epoch);
    public byte[] ReadEvents(int client, string epoch) => JsReadEvents(client, epoch);
}
