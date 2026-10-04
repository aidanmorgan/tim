using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
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
    private WorkshopClientId _id;
    private WorkshopClockPeer? _peer;
    private WorkshopClockMapping? _clock;
    private WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Master bootstrap is not installed.");
    private WorkshopClockMapping Clock => _clock ?? throw new InvalidOperationException("Master bootstrap is not installed.");
    private readonly WorkshopPoseHistory _history = new();
    private WorkshopConstruction _construction;
    private ulong _sequence;
    private int _pending;
    private ulong _memorySequence;
    private WorkshopMemoryRequest? _memoryRequest;
    private PublicationSequence _observedPublication;
    private bool _disposed;
    private readonly WorkshopClientCursor _cursor;
    public WorkshopCommandIdentity? Pending { get; private set; }
    public WorkshopTransportState TransportState
    {
        get
        {
            if (_disposed) return WorkshopTransportState.RecoveryBlocked;
            var state = (WorkshopTransportState)Status(_id.Value);
            if (!Enum.IsDefined(state)) throw new ArgumentException("Unknown transport state.");
            return state;
        }
    }
    public SimulationEpoch Epoch => _cursor.Epoch;
    public AuthorityRevision Revision => _cursor.Revision;
    private BrowserWorkshopClient(RuntimeSessionId session) { session.Validate(); _cursor = new(session); }

    [JSImport("now", "workshopClient")]
    private static partial double NativeMilliseconds();
    [JSImport("probe", "workshopClient")]
    private static partial void SendProbe(int client, byte[] probe);
    [JSImport("acceptedClockReply", "workshopClient")]
    private static partial void AcceptedClockReply(int client);
    [JSImport("clockObservation", "workshopClient")]
    private static partial void RecordClockObservation(int client, byte[] observation);
    [JSImport("presentationObservation", "workshopClient")]
    private static partial void RecordPresentationObservation(int client, byte[] observation);

    [JSImport("create", "workshopClient")]
    private static partial Task<int> CreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
        [JSMarshalAs<JSType.Function>] Action clockReply,
        [JSMarshalAs<JSType.Function>] Action physicalRead,
        [JSMarshalAs<JSType.Function>] Action scheduleControl,
        [JSMarshalAs<JSType.Function>] Action animationOutput,
        [JSMarshalAs<JSType.Function>] Action service,
        [JSMarshalAs<JSType.Function>] Action beginMemory,
        [JSMarshalAs<JSType.Function>] Action receiveMemory,
        [JSMarshalAs<JSType.Function>] Action releaseMemory);
    [JSImport("bootstrap", "workshopClient")]
    private static partial byte[] MasterBootstrap(int client);
    [JSImport("activate", "workshopClient")]
    private static partial void ActivateClient(int client);
    [JSImport("sendMemoryRequest", "workshopClient")]
    private static partial void SendMemoryRequest(int client, byte[] request);
    [JSImport("completeMemoryObservation", "workshopClient")]
    private static partial void CompleteMemoryObservation(int client, byte[] result);
    [JSImport("eventBytes", "workshopClient")]
    private static partial byte[] EventBytes(int client);
    [JSImport("eventMilliseconds", "workshopClient")]
    private static partial double EventMilliseconds(int client);
    [JSImport("qualified", "workshopClient")]
    private static partial void Qualified(int client);
    [JSImport("status", "workshopClient")]
    private static partial int Status(int client);
    [JSImport("send", "workshopClient")]
    private static partial Task Send(int client, byte[] command);
    [JSImport("acknowledgement", "workshopClient")]
    private static partial byte[] Acknowledgement(int client, byte[] sequence);
    [JSImport("completeAcknowledgement", "workshopClient")]
    private static partial void CompleteAcknowledgement(int client, byte[] sequence);
    [JSImport("dispose", "workshopClient")]
    private static partial void DisposeClient(int client);

    public static async Task<BrowserWorkshopClient> Create()
    {
        if (!OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException("This Workshop requires Chrome WebGPU with shader-f16.");
        using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document")
            ?? throw new InvalidOperationException("A browser document is required.");
        var baseUri = document.GetPropertyAsString("baseURI")
            ?? throw new InvalidOperationException("The document resource origin is unavailable.");
        await JSHost.ImportAsync("workshopClient", new Uri(new Uri(baseUri), "workshop-client.js").AbsoluteUri);
        var identity = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(identity);
        var session = WorkshopWire.ReadSession(identity);
        var client = new BrowserWorkshopClient(session);
        client._construction = new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty);
        client._id = new(await CreateClient([(int)WorkshopTransportState.Ready, (int)WorkshopTransportState.Backpressure,
            (int)WorkshopTransportState.TimedOut, (int)WorkshopTransportState.Indeterminate, (int)WorkshopTransportState.RecoveryBlocked],
            identity, [(int)WorkshopClockWire.Version, (int)NativeClockProfile.Chromium154MacIsolated,
                WorkshopClockWire.PeerBytes, WorkshopClockWire.ProbeBytes, WorkshopClockWire.ReplyBytes,
                WorkshopClockWire.DiagnosticBytes, (int)WorkshopRuntimeRole.Browser, (int)WorkshopRuntimeRole.Simulation],
            (int)WorkshopBuild.CaptureMode, client._construction.Settings.Presentation == PresentationCadence.AdmittedDisplay, client.ReceiveClockReply, client.ReceiveRead, client.ReceiveSchedule, client.ReceiveAnimation,
            client.Service, client.BeginMemory, client.ReceiveMemory, client.ReleaseMemory));
        try
        {
            var peer = WorkshopClockWire.DecodePeer(MasterBootstrap(client._id.Value));
            if (peer.Session != session || peer.Generation.Value != 1 || peer.RequesterGeneration.Value != 1 ||
                peer.RequesterRole != WorkshopRuntimeRole.Browser)
                throw new ArgumentException("Unexpected master bootstrap identity.");
            client._peer = peer;
            client._clock = new(peer);
            AnimationKinds(client._id.Value, [(int)HintOutputKind.Acknowledgement, (int)HintOutputKind.Sample, (int)HintOutputKind.Rejected]);
            ActivateClient(client._id.Value);
            if (client._construction.Settings.Presentation == PresentationCadence.AdmittedDisplay)
            {
                var display = DisplayRate(client._id.Value);
                client._admittedDisplay = new(checked((uint)display[0]), checked((uint)display[1]));
                client._construction = client._construction with
                { Settings = client._construction.Settings with { PresentationRate = client._admittedDisplay } };
            }
            while (!client.Clock.IsQualified || !AnimationQualified(client._id.Value))
            {
                client.ThrowIfTransportFailed();
                await Task.Delay(10);
            }
            var initialized = await client.Execute(WorkshopCommandKind.Initialize, settings: client._construction.Settings);
            if (initialized.Response.Result.Outcome != WorkshopCommandOutcome.Applied)
                throw new InvalidOperationException("The WebGPU simulation worker could not initialize.");
            Qualified(client._id.Value);
            return client;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("CCGPU_STARTUP_EXCEPTION " + error);
            client._disposed = true; client._clock?.Retire(); DisposeClient(client._id.Value); throw;
        }
    }

    public async Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null, WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_memoryRequest is not null) throw new InvalidOperationException("A stopped memory observation owns this client.");
        if (kind == WorkshopCommandKind.Construct && _constructionOwner is not null)
            throw new InvalidOperationException("A construction candidate already owns the client.");
        if (_pending >= 2) throw new InvalidOperationException("Workshop command queue is full.");
        if (_sequence == ulong.MaxValue) throw new InvalidOperationException("Workshop command sequence is exhausted.");
        var sequence = new CommandSequence(_sequence + 1);
        // Pause targets the current drained endpoint; physical commits may advance after B sends it.
        var revisionKind = kind is WorkshopCommandKind.Pause or WorkshopCommandKind.Reset or WorkshopCommandKind.Cancel or WorkshopCommandKind.Dispose
            ? ExpectedRevisionKind.Any : ExpectedRevisionKind.Exact;
        var command = new WorkshopCommand(sequence, kind, Epoch,
            revisionKind == ExpectedRevisionKind.Any ? default : Revision, construction, target, revisionKind, Peer.Session, _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default, settings);
        var bytes = WorkshopWire.Encode(command);
        _sequence = sequence.Value;
        _pending++;
        var dispatchedReadOrder = _cursor.ReadOrder;
        var identity = new WorkshopCommandIdentity(sequence, command.Epoch, command.Revision, command.Cadence, command.Projection);
        if (kind != WorkshopCommandKind.Cancel) Pending = identity;
        if (construction is not null) { _proposedConstruction = construction; _constructionOwner = identity; }
        try
        {
            try { await Send(_id.Value, bytes); }
            catch (Exception error) { throw new WorkshopTransportException(identity, TransportState, error); }
            var encodedIdentity = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(encodedIdentity, sequence.Value);
            var response = WorkshopWire.DecodeResponse(Acknowledgement(_id.Value, encodedIdentity));
            var prepared = _cursor.PrepareAcknowledgement(command, response, dispatchedReadOrder, _disposed);
            if (prepared.Applicable)
            {
                ValidateCommandRead(response, construction, _construction);
                var boundary = response.Result.Outcome == WorkshopCommandOutcome.Applied ? kind switch
                {
                    WorkshopCommandKind.Initialize or WorkshopCommandKind.Construct or WorkshopCommandKind.Reset => PresentationBoundary.Seed,
                    WorkshopCommandKind.Run => PresentationBoundary.Run,
                    _ => PresentationBoundary.Continuous
                } : PresentationBoundary.Continuous;
                WorkshopPoseHistory.Prepared? history = response.Read.Capture is null || _history.Latest is { } installed &&
                    installed.Read.HasSameContent(response.Read) ? null :
                    PrepareHistory(response, boundary, WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()));
                _cursor.Commit(prepared);
                if (history is { } ready) _history.Commit(ready);
                if (response.Result.Outcome == WorkshopCommandOutcome.Applied && construction is { } admitted)
                    _construction = admitted;
            }
            // A valid terminal receipt releases its lease even when a newer read owns presentation.
            // Decode/identity/history failures above keep the reliable work unresolved.
            CompleteAcknowledgement(_id.Value, encodedIdentity);
            return new(response, prepared.Applicable);
        }
        finally { _pending--; if (_constructionOwner == identity) { _proposedConstruction = null; _constructionOwner = null; } if (Pending == identity) Pending = null; }
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
        var now = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        Clock.Observe(now);
#if PLAYTEST
        var pending = Clock.Pending;
#endif
        if (Clock.BeginProbe(now) is { } probe)
        {
#if PLAYTEST
            if (pending is { } expired) RecordClockObservation(_id.Value, WorkshopClockWire.EncodeExpiredProbe(expired, Peer, now));
#endif
            SendProbe(_id.Value, WorkshopClockWire.EncodeProbe(probe, Peer));
        }
    }

    private void ReceiveClockReply()
    {
        if (_disposed) return;
        var bytes = EventBytes(_id.Value);
        var milliseconds = EventMilliseconds(_id.Value);
        var received = WorkshopNativeClock.FromMilliseconds(milliseconds);
        ClockReply reply;
        try { reply = WorkshopClockWire.DecodeReply(bytes, Peer, received); }
        catch (ArgumentException)
        {
#if PLAYTEST
            RecordClockObservation(_id.Value, WorkshopClockWire.EncodeRejectedReply(bytes, received));
#endif
            return;
        }
        var observation = Clock.Receive(reply);
        if (observation.Outcome == ClockProbeOutcome.Accepted) AcceptedClockReply(_id.Value);
#if PLAYTEST
        RecordClockObservation(_id.Value, WorkshopClockWire.EncodeObservation(observation, Peer));
#endif
    }

    private void ReceiveRead()
    {
        if (_disposed) return;
        var bytes = EventBytes(_id.Value);
        var milliseconds = EventMilliseconds(_id.Value);
        var response = WorkshopWire.DecodeResponse(bytes);
        if (_schedule is not { } active || response.Cadence.Value < active.Revision.Value ||
            response.Projection.Value < active.World.Epoch.Value) return;
        if (response.Cadence != active.Revision || response.Projection != active.World.Epoch)
            throw new ArgumentException("Future read cannot install a schedule.");
        var prepared = _cursor.PrepareRead(response);
        if (response.Publication.Value > _observedPublication.Value) _observedPublication = response.Publication;
        if (!prepared.Applicable) return;
        ValidateRead(response.Read, _construction);
        var boundary = response.Phase == WorkshopSimulationPhase.Completed ? PresentationBoundary.Completed : PresentationBoundary.Continuous;
        var history = PrepareHistory(response, boundary, WorkshopNativeClock.FromMilliseconds(milliseconds));
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
        RecordPresentationObservation(_id.Value, WorkshopPresentationWire.Encode(sample, selected, scene,
            WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), _schedule!.Value.Revision, _lastPresentation));
        #endif
    }

    private void BeginMemory()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_memoryRequest is not null || _pending != 0 || TransportState != WorkshopTransportState.Ready ||
            !WorkshopMemoryWire.IsStopped(_cursor.Phase))
            throw new InvalidOperationException("Memory observation requires a stopped, idle client.");
        if (_memorySequence == ulong.MaxValue) throw new InvalidOperationException("Memory observation identity exhausted.");
        var request = new WorkshopMemoryRequest(WorkshopMemoryKind.PostGc, WorkshopMemoryContext.Simulation,
            Peer.Session, new(++_memorySequence), Epoch, Revision, _observedPublication);
        _memoryRequest = request;
        SendMemoryRequest(_id.Value, WorkshopMemoryWire.Encode(request));
    }

    private void ReceiveMemory()
    {
        var request = _memoryRequest ?? throw new InvalidOperationException("No stopped observation is owned.");
        var result = WorkshopMemoryWire.DecodeResult(EventBytes(_id.Value));
        if (_disposed || result.Request != request) throw new ArgumentException("Memory result belongs to another observation.");
        if (result.Status != WorkshopMemoryStatus.Accepted) return;
        if (_pending != 0 || TransportState != WorkshopTransportState.Ready || !WorkshopMemoryWire.IsStopped(_cursor.Phase) ||
            request.Epoch != Epoch || request.Revision != Revision || request.Publication != _observedPublication)
            throw new InvalidOperationException("Stopped observation lost its committed identity.");
        var browser = WorkshopMemoryWire.Capture(request with { Context = WorkshopMemoryContext.Browser });
        CompleteMemoryObservation(_id.Value, WorkshopMemoryWire.Encode(browser));
    }

    private void ReleaseMemory() => _memoryRequest = null;

    private void ThrowIfTransportFailed()
    {
        if (TransportState is WorkshopTransportState.Indeterminate or WorkshopTransportState.RecoveryBlocked)
            throw new InvalidOperationException("Worker session is unavailable; its unacknowledged operations remain indeterminate.");
    }

    internal static void ValidateCommandRead(WorkshopResponse response, WorkshopConstruction? proposed, WorkshopConstruction current) =>
        ValidateRead(response.Read, response.Result.Outcome == WorkshopCommandOutcome.Applied ? proposed ?? current : current);

    private static void ValidateRead(WorkshopRead read, WorkshopConstruction construction)
    {
        if (read.Tick.Value > construction.Settings.RunTickLimit || (read.Ball is null) != (construction.Ball is null))
            throw new ArgumentException("Read does not own the admitted construction.");
        if (read.Tick.Value != 0 && (read.Motion is not { } motion ||
            motion.Substeps != construction.Settings.PhysicalStepsPerCommit))
            throw new ArgumentException("Motion cadence differs from the admitted construction.");
        var expectedSensors = construction.Ball.HasValue && construction.Receiver.HasValue ? 1 : 0;
        if (read.Captures.Count != expectedSensors || read.Rotation.HasValue != read.Ball.HasValue)
            throw new ArgumentException("Read physical/sensor population differs from its construction.");
        if (expectedSensors != 0)
        {
            var capture = read.Captures[0];
            var end = checked((uint)(read.Tick.Value * (ulong)construction.Settings.PhysicalStepsPerCommit));
            if (capture.Sensor != WorkshopPhysicsCompiler.CaptureSensor(construction.Receiver!.Value) ||
                capture.EventOrdinal > end || (capture.EventOrdinal == end && capture.EventPhase > (Half)0) ||
                (read.Tick.Value == 0 && capture.Phase != CaptureLatchPhase.Clear))
                throw new ArgumentException("Capture identity/time differs from the admitted construction.");
        }
        if (read.Tick.Value == 0 && (read.Rotation != construction.Ball?.Rotation ||
            !PhysicsDeclarationBounds.Zero(read.Angular.X, read.Angular.Y, read.Angular.Z)))
            throw new ArgumentException("Initial physical orientation differs from construction.");
        if (read.Ball is not { } body || construction.Ball is not { } ball) return;
        body.Validate();
        if (body.Id != ball.Id || body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value ||
            (read.Tick.Value == 0 && (body.Cell != ball.Cell || !HalfBits.Equal(body.Local, ball.Local) || !HalfBits.IsPositiveZero(body.Velocity))))
            throw new ArgumentException("Read differs from the admitted canonical construction.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_memoryRequest is not null)
        {
            _disposed = true; Clock.Retire(); _history.Retire(); DisposeClient(_id.Value);
            return;
        }
        try
        {
            var disposal = Execute(WorkshopCommandKind.Dispose);
            if (await Task.WhenAny(disposal, Task.Delay(1000)) == disposal) await disposal;
            else _ = disposal.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
        finally { _disposed = true; Clock.Retire(); _history.Retire(); DisposeClient(_id.Value); }
    }
}
