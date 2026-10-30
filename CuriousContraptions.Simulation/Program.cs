using System.Runtime.Intrinsics;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;

[assembly: SupportedOSPlatform("browser")]

namespace CuriousContraptions.Simulation;

public static partial class Program
{
    private static readonly WorkshopDiagnostics Diagnostics = new(PublishDiagnostic);
    private static WorkshopGpuDevice? _device;
    private static WorkshopGpuDevice Device => _device ?? throw new InvalidOperationException("Worker document has not bootstrapped.");
    private static WorkshopClockPeer? _peer;
    private static WorkshopSimulation Simulation => Executor.Simulation;
    private static WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Worker clock has not bootstrapped.");
    private static ulong _publication;
    // The worker's own reliable-read accounting (Story 9A.3): every publication, and every browser receipt matched against it.
    private static WorkshopReliableReadLedger? _reads;
    private static WorkshopReliableReadLedger Reads => _reads ?? throw new InvalidOperationException("Worker session has not bootstrapped.");
    private static WorkshopCommandExecutor? _executor;
    private static WorkshopCommandExecutor Executor => _executor ?? throw new InvalidOperationException("Worker has not bootstrapped.");
    private static readonly WorkshopCommandRouter Commands = new(command => Executor.Execute(command));
    // One builder for every physical read this worker publishes: the per-tick encode reuses its storage.
    private static readonly Google.FlatBuffers.FlatBufferBuilder ReadBuilder = new(4096);
    private static ulong _loopOwner;
    private static readonly CuriousContraptions.Simulation.WorkshopTrace Trace = new(Diagnostics);
    [JSImport("observation", "workshopGpu")]
    private static partial void PublishObservation(byte[] bytes);
    private static void PublishDiagnostic(WorkshopDiagnostic record) =>
        Diagnostics.Publish(record, Peer.Session, WorkshopRuntimeRole.Simulation, PublishObservation);
    [JSExport]
    public static void ReportObservationFailure(string detail) =>
        Diagnostics.Fault(FaultSite.TraceFlush, new InvalidOperationException(detail));

    public static void Main() { }

    [JSImport("now", "workshopGpu")]
    internal static partial double NativeMilliseconds();

    private sealed class CaptureClock : IWorkshopCaptureClock
    {
        public ClockGeneration Generation => Peer.Generation;
        public WorkshopClockStamp Capture() => new(WorkshopClockDomain.SimulationMonotonic, Generation,
            WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()), Peer.Uncertainty);
    }

    [JSExport]
    public static byte[] Bootstrap(byte[] bytes)
    {
        if (_peer is not null) throw new InvalidOperationException("Worker session is already installed.");
        if (bytes.Length != 16) throw new ArgumentException("Unsupported session bootstrap.");
        var session = RuntimeSessionId.Read(bytes);
        var origin = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        var peer = new WorkshopClockPeer(session, new(1), origin, new(1), new(100_000), WorkshopRuntimeRole.Browser);
        _device = new(new BrowserGpuTransport(new WorkerEventPublication()), new(session.Low, session.High));
        _reads = new(session);
        _peer = peer;
        _executor = new(Device, new CommandHost(), Diagnostics);
        return WorkshopClockWire.EncodePeer(peer);
    }

    [JSExport]
    public static async Task PrepareGpu()
    {
        await Device.Prepare();
    }

    /// <summary>Publishes one committed read with its 24-byte receipt identity and its composed pose-ring slot: the worker copies
    /// the slot into the ring verbatim and the browser echoes the identity, so no script parses the read.</summary>
    [JSImport("publish", "workshopGpu")]
    private static partial void Publish(byte[] read, byte[] readIdentity, byte[] poseSlot, bool observationPending);
    [JSImport("prepareEvents", "workshopGpu")] private static partial int PrepareEvents(byte[] batch);
    [JSImport("commitEvents", "workshopGpu")] private static partial void CommitEvents();
    [JSImport("discardEvents", "workshopGpu")] private static partial void DiscardEvents();
    [JSImport("publishEvents", "workshopGpu")] private static partial void PublishEvents();
    [JSImport("disposeEvents", "workshopGpu")] private static partial void DisposeEvents();

    private sealed class WorkerEventPublication : CuriousContraptions.Abstractions.IEventRingPublication
    {
        public CuriousContraptions.Abstractions.EventPublicationPreparation Prepare(byte[] batch)
        {
            var result = (CuriousContraptions.Abstractions.EventPublicationPreparation)PrepareEvents(batch);
            if (!Enum.IsDefined(result)) throw new ArgumentException("Undefined event preparation result.");
            return result;
        }
        public void Commit() => CommitEvents();
        public void Discard() => DiscardEvents();
        public void Publish() => PublishEvents();
        public void Dispose() => DisposeEvents();
    }

    /// <summary>Acknowledges one command with its response and its 8-byte little-endian sequence (the browser's receipt key).</summary>
    [JSImport("acknowledge", "workshopGpu")]
    private static partial void Acknowledge(byte[] response, byte[] sequence, bool observationPending);
    [JSImport("retire", "workshopGpu")]
    private static partial void Retire(string detail);

    [JSExport]
    public static int CaptureMode() => (int)WorkshopBuild.CaptureMode;

    // Called only after the existing read/command transport admission barrier.
    [JSExport]
    public static void FlushObservation() => Trace.Flush(Simulation.Phase);
    private static double ObservationMilliseconds()
    {
        try { return NativeMilliseconds(); }
        catch (JSException) { return double.NaN; } // Incomplete observation never replaces the physical result.
    }

    private static void RecordObservedTickDuration(SimulationTick tick, double started) =>
        Trace.RecordTickDuration(tick, ObservationMilliseconds() - started);

    /// <summary>Matches the browser's receipt of a posted read (its 24-byte identity, echoed unread) and retires every read up to it.
    /// A mismatch throws its typed <see cref="ReadReceiptRejectedException"/>; the worker reports it as a transport failure.</summary>
    [JSExport]
    public static void AcknowledgeRead(byte[] receipt) => Reads.Receipt(receipt);

    /// <summary>Refuses an envelope no handler owns, naming its keys, and records it in this worker's fault log; returns the
    /// rejection the worker reports to the browser.</summary>
    [JSExport]
    public static string RejectUnknownEnvelope(string[] keys) =>
        RejectEnvelope(new(WorkerEnvelopeRejection.UnknownEnvelope, keys));

    [JSExport]
    public static string RejectSaturatedCommand() => RejectEnvelope(new(WorkerEnvelopeRejection.CommandTransportSaturated, []));

    private static string RejectEnvelope(WorkerEnvelopeRejectedException rejection)
    {
        Diagnostics.Fault(FaultSite.WorkerEnvelope, rejection);
        return rejection.Message;
    }


    [JSExport]
    public static async Task Dispatch(byte[] bytes)
    {
        var routed = Commands.Dispatch(bytes);
        var response = await routed.Response;
        var sequence = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(sequence, routed.Sequence.Value);
        _ = Device.DrainEvents();
        Acknowledge(response, sequence, Trace.CanFlush(Simulation.Phase));
    }


    private sealed class CommandHost : IWorkshopCommandHost
    {
        public RuntimeSessionId Session => Peer.Session;
        public WorkshopSchedule? Schedule => _schedule;
        public ClockGeneration Generation => Peer.Generation;
        public bool InstallationPending => _preparedEndpoint is not null;
        public bool ReadsIdle => Reads.Idle;
        public IWorkshopCaptureClock Clock { get; } = new CaptureClock();
        public IWorkshopInstallation Installation { get; } = new Installation();
        public WorkshopMemoryMeasurement CaptureMemory() => WorkshopMemoryMeasurement.Capture();

        public async ValueTask Before(WorkshopCommandKind kind, WorkshopSimulation? simulation)
        {
            _transition = kind switch
            {
                WorkshopCommandKind.Run => ScheduleTransition.Run, WorkshopCommandKind.Pause => ScheduleTransition.Pause,
                WorkshopCommandKind.Resume => ScheduleTransition.Resume, WorkshopCommandKind.Step => ScheduleTransition.Step,
                WorkshopCommandKind.Reset => ScheduleTransition.Reset, _ => ScheduleTransition.Configure
            };
            if (kind == WorkshopCommandKind.Pause) _loopOwner++;
            if (kind is WorkshopCommandKind.Pause or WorkshopCommandKind.ConfigureElectrical or WorkshopCommandKind.CutRope)
                while (Simulation.HasPendingGpuOperation) await Task.Delay(1);
            if (kind == WorkshopCommandKind.Reset) Trace.End(TraceEnd.Reset);
            if (kind == WorkshopCommandKind.Dispose)
            {
                Trace.End(TraceEnd.Disposed);
                _loopOwner++;
            }
            if (kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Run or WorkshopCommandKind.Reset)
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        }

        public async ValueTask After(WorkshopCommandKind kind, WorkshopCommandResult result, WorkshopSimulation simulation)
        {
            if (kind == WorkshopCommandKind.Pause && result.Outcome != WorkshopCommandOutcome.Applied &&
                simulation.Phase == WorkshopSimulationPhase.Running) _ = RunLoop(++_loopOwner);
            if (kind == WorkshopCommandKind.Reset && result.Outcome == WorkshopCommandOutcome.Applied) _loopOwner++;
            if (result.Outcome != WorkshopCommandOutcome.Applied || _preparedEndpoint is null) return;
            await CommitInstallation();
            if (kind == WorkshopCommandKind.Run)
            {
                Trace.Begin(Peer.Session, simulation.Scene, simulation.Epoch, _schedule!.Value.World.Epoch, simulation.Profile);
#if PLAYTEST
                Trace.Append(Device.DiagnosticCommitted);
#endif
            }
#if PLAYTEST
            if (kind == WorkshopCommandKind.Step) Trace.Append(Device.DiagnosticCommitted);
#endif
            if (kind is WorkshopCommandKind.Run or WorkshopCommandKind.Resume) _ = RunLoop(++_loopOwner);
        }
    }

    private static async Task RunLoop(ulong owner)
    {
        try { await RunTicks(owner); }
        catch (Exception error)
        {
            // A superseded loop no longer owns the simulation, so it records its failure and changes nothing.
            if (owner != _loopOwner) { Diagnostics.Fault(FaultSite.StaleRunLoop, error); return; }
            _loopOwner++;
            Simulation.Stop(WorkshopRejection.Transport);
            Diagnostics.Fault(FaultSite.RunLoop, error);
            Trace.End(TraceEnd.Fault);
            Retire("Physical publication failed after commit: " + error.Message);
        }
    }

    private static Task RunTicks(ulong owner) => WorkshopTickLoop.Run(new RunLoopHost(owner));

    // One-hop scheduler turns (Story 9.4b). A Task completed from a JS promise resumes its .NET continuation through a
    // second runtime macrotask (the background-job timer), so every yield cost two turns of any competing work (measured
    // under the 20 ms injection: evidence-9-4b/tick-loop). The worker instead calls ResumeTurn from the MessageChannel or
    // timer task itself, and the awaiting loop continues inline in that same task.
    private static readonly WorkshopTurnRegistry Turns = new();

    [JSImport("scheduleTurn", "workshopGpu")] private static partial void ScheduleTurn(int turn, int delayMilliseconds);

    /// <summary>Ends this scheduler turn: delay 0 resumes on the next macrotask with no timer clamp; a positive delay
    /// resumes on the ordinary timer.</summary>
    private static Task NextTurn(int delayMilliseconds)
    {
        var (turn, resumed) = Turns.Begin();
        ScheduleTurn(turn, delayMilliseconds);
        return resumed;
    }

    [JSExport]
    public static void ResumeTurn(int turn) => Turns.Resume(turn);

    /// <summary>The worker's side of the run loop (WorkshopTickLoop owns the scheduling and its per-turn bound).</summary>
    private sealed class RunLoopHost(ulong owner) : ITickLoopHost
    {
        private static WorkshopSchedule Schedule => _schedule ?? throw new InvalidOperationException("Missing physical schedule.");
        public bool Owns => owner == _loopOwner && Simulation.Phase == WorkshopSimulationPhase.Running;
        public bool Blocked => Simulation.HasPendingGpuOperation || _preparedEndpoint is not null || Executor.Pending is not null;
        public bool DebtExceeded
        {
            get
            {
                var now = MasterNow();
                return Reads.Stalled(now) || Schedule.ExceedsPhysicalDebt(Simulation.Committed.Tick, now);
            }
        }
        public bool AtRunLimit => Simulation.Committed.Tick.Value == Simulation.Profile.RunTickLimit;
        public bool CaughtUp => Simulation.Committed.Tick.Value >= Schedule.World.DueTick(Schedule.Settings, MasterNow()).Value;

        public ValueTask StopForDebt()
        {
            var now = MasterNow();
            var reason = Reads.DebtReason(now);
            var oldest = Reads.Oldest;
            var age = oldest is { } read ? (now.Value - read.Published.Value) / 1_000_000 : 0;
            Diagnostics.Capture(() => new WorkshopDebtDiagnostic(Simulation.Committed.Tick, reason,
                oldest?.Publication ?? default, age));
            Simulation.Stop(reason);
            Trace.End(TraceEnd.Fault);
            EmitRead(new(WorkshopCommandOutcome.Faulted, Simulation.Fault));
            return ValueTask.CompletedTask;
        }

        public async ValueTask CompleteRun()
        {
            _transition = ScheduleTransition.Completed;
            await Simulation.CompleteRun();
            await CommitInstallation();
            Trace.End(TraceEnd.Complete);
            EmitRead(new(WorkshopCommandOutcome.Applied, WorkshopRejection.None));
        }

        public async ValueTask<TickAttempt> Advance()
        {
            var tickStarted = ObservationMilliseconds();
            var result = await Simulation.Advance();
            if (owner != _loopOwner) return TickAttempt.Stopped;
            if (result.Outcome is WorkshopCommandOutcome.Superseded or WorkshopCommandOutcome.Cancelled) return TickAttempt.Superseded;
#if PLAYTEST
            if (result.Outcome == WorkshopCommandOutcome.Applied) Trace.Append(Device.DiagnosticCommitted);
#endif
            if (result.Outcome != WorkshopCommandOutcome.Applied)
            {
                Diagnostics.Capture(() => new WorkshopTickFailureDiagnostic(result, Simulation.Fault));
                Trace.End(TraceEnd.Fault);
            }
            EmitRead(result);
            if (result.Outcome != WorkshopCommandOutcome.Applied) return TickAttempt.Stopped;
            RecordObservedTickDuration(Simulation.Committed.Tick, tickStarted);
            return TickAttempt.Committed;
        }

        public async ValueTask YieldTurn() => await NextTurn(0);
        public async ValueTask AwaitWork() => await NextTurn(1);
    }

    /// <summary>Publishes the committed read: the event ring publication first (Story 9A.22 D7 order), then the read, which the worker
    /// coalesces to the latest (Story 9A.23 D9: no read is reliable; every one-shot occurrence is a ring record).</summary>
    private static void EmitRead(WorkshopCommandResult result)
    {
        if (_publication == ulong.MaxValue) throw new InvalidOperationException("Physical publication identity exhausted.");
        var sequence = new PublicationSequence(_publication + 1);
        var readResponse = Executor.Populate(new WorkshopResponse(default, WorkshopResponseKind.Read, result,
            Simulation.Phase, Simulation.Committed, Publication: sequence));
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(readResponse, ReadBuilder);
        Reads.Publish(sequence, MasterNow());
        _publication++;
        // Preserve transfers before the browser coalesces ordinary reads.
        Diagnostics.RetainTransfer(readResponse);
        _ = Device.DrainEvents();
        Publish(encoded, WorkshopWireFlatBuffers.ReadIdentity(readResponse), WorkshopPoseRing.ComposeSlot(readResponse.Read.Capture?.Time.Value ?? 0, readResponse.Read.Bodies),
            Trace.CanFlush(Simulation.Phase));
    }
}
