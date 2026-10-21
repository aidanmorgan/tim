using System.Runtime.Intrinsics;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;

[assembly: SupportedOSPlatform("browser")]

namespace CuriousContraptions.Simulation;

public static partial class Program
{
    private static WorkshopGpuDevice? _device;
    private static WorkshopGpuDevice Device => _device ?? throw new InvalidOperationException("Worker document has not bootstrapped.");
    private static WorkshopSimulation? _simulation;
    private static WorkshopClockPeer? _peer;
    private static WorkshopSimulation Simulation => _simulation ?? throw new InvalidOperationException("Worker clock has not bootstrapped.");
    private static WorkshopClockPeer Peer => _peer ?? throw new InvalidOperationException("Worker clock has not bootstrapped.");
    private static ulong _publication;
    // The worker's own reliable-read accounting (Story 9A.3): every publication, and every browser receipt matched against it.
    private static WorkshopReliableReadLedger? _reads;
    private static WorkshopReliableReadLedger Reads => _reads ?? throw new InvalidOperationException("Worker session has not bootstrapped.");
    private static readonly WorkshopCommandRouter Commands = new(Execute);
    // One builder for every physical read this worker publishes: the per-tick encode reuses its storage.
    private static readonly Google.FlatBuffers.FlatBufferBuilder ReadBuilder = new(4096);
    private static ulong _loopOwner;
    private static WorkshopCommandIdentity? _pendingCommand;
    private static readonly CuriousContraptions.Simulation.WorkshopTrace Trace = new();
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
        DeclaredPorts.Install(BundlePorts());
        var session = RuntimeSessionId.Read(bytes);
        var origin = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        var peer = new WorkshopClockPeer(session, new(1), origin, new(1), new(100_000), WorkshopRuntimeRole.Browser);
        _device = new(new BrowserGpuTransport(), new(session.Low, session.High));
        _reads = new(session);
        _peer = peer;
        return WorkshopClockWire.EncodePeer(peer);
    }

    /// <summary>Story 9A.17: the ports of the element bundle this worker was built with, read and validated with the same schema
    /// the game loads; an invalid bundle refuses the bootstrap.</summary>
    private static ElementPortCatalogue BundlePorts()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("elements.bundle.json")
            ?? throw new InvalidDataException("The worker carries no element bundle.");
        using var reader = new StreamReader(stream);
        var document = CuriousContraptions.Bundle.BundleSchema.Read(reader.ReadToEnd());
        var errors = CuriousContraptions.Bundle.BundleValidator.Validate(
            document.Elements.Select(entry => new CuriousContraptions.Bundle.NamedEntry(entry.Metadata.Id, entry)).ToArray());
        if (errors.Count > 0) throw new InvalidDataException("The worker's element bundle is invalid: " + string.Join("; ", errors));
        DeclaredPhysics.Install(document.Elements);
        return ElementPortCatalogue.FromBundle(document.Elements);
    }

    private sealed class WarmupInstallation : IWorkshopInstallation
    {
        public void Abort(CommandSequence owner) { }
        public void Retire() { }
        public ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase) => ValueTask.CompletedTask;
    }

    [JSExport]
    public static async Task PrepareGpu()
    {
        await Device.Prepare();
        await WarmupSimulation();
    }

    private static async Task WarmupSimulation()
    {
        try
        {
            var battery = WorkshopInput.Battery(new(1), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
            var plate = WorkshopInput.PressurePlate(new(2), 0, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
            var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1);
            var ball = WorkshopInput.Basketball(new(4), 0, 2, 0, 0, 0, 0, 1);
            var ramp = WorkshopInput.Ramp(new(5), -1, 1, 0, 0, 0, 0, 1, RampDimensions.Default);

            var wire1 = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, plate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
            var wire2 = new WorkshopConnection(plate.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

            var settings = WorkshopCadenceSettings.Default();
            var construction = new WorkshopConstruction(
                new(2),
                settings,
                new([battery, plate, gate, ball, ramp]),
                Connections: new([wire1, wire2]));

            // The warm-up borrows the worker's device: disposing it retires the warm-up only, never the device.
            await using var sim = new WorkshopSimulation(Device, new CaptureClock(), settings, new(1), new WarmupInstallation(),
                GpuDeviceOwnership.Borrowed);
            var initResult = await sim.Initialize();
            Console.WriteLine($"WARMUP: init={initResult.Outcome} {initResult.Reason}");
            if (initResult.Outcome == WorkshopCommandOutcome.Applied)
            {
                var constructResult = await sim.Construct(construction);
                Console.WriteLine($"WARMUP: construct={constructResult.Outcome} {constructResult.Reason}");
                if (constructResult.Outcome == WorkshopCommandOutcome.Applied)
                {
                    var runResult = await sim.Run();
                    Console.WriteLine($"WARMUP: run={runResult.Outcome} {runResult.Reason}");
                    if (runResult.Outcome == WorkshopCommandOutcome.Applied)
                    {
                        for (var step = 0; step < 5; step++)
                        {
                            var t0 = NativeMilliseconds();
                            var adv = await sim.Advance();
                            var dt = NativeMilliseconds() - t0;
                            Console.WriteLine($"WARMUP: step {step} {adv.Outcome} {adv.Reason} in {dt:F2}ms");
                        }
                    }
                    await sim.Reset();
                }
            }
        }
        catch (Exception error)
        {
            // Non-fatal: the worker's own Initialize admits a fresh world on the same device afterwards.
            WorkshopDiagnostics.Fault(FaultSite.Warmup, error);
        }
    }

    /// <summary>Publishes one committed read with its 24-byte receipt identity and its composed pose-ring slot: the worker copies
    /// the slot into the ring verbatim and the browser echoes the identity, so no script parses the read.</summary>
    [JSImport("publish", "workshopGpu")]
    private static partial void Publish(byte[] read, byte[] readIdentity, byte[] poseSlot, bool observationPending, bool retainOccurrence);
    [JSImport("reserveOccurrenceRead", "workshopGpu")] private static partial bool ReserveOccurrenceRead();
    [JSImport("releaseOccurrenceRead", "workshopGpu")] private static partial void ReleaseOccurrenceRead();

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
        WorkshopDiagnostics.Fault(FaultSite.WorkerEnvelope, rejection);
        return rejection.Message;
    }


    [JSExport]
    public static async Task Dispatch(byte[] bytes)
    {
        var routed = Commands.Dispatch(bytes);
        var response = await routed.Response;
        var sequence = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(sequence, routed.Sequence.Value);
        Acknowledge(response, sequence, Trace.CanFlush(Simulation.Phase));
    }


    private static async Task<byte[]> Execute(WorkshopCommand command)
    {
        if (!WorkshopMessages.Matches(command, _simulation?.Epoch ?? new(1), _simulation?.Revision ?? default, Peer.Session,
            _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default))
            return EncodeResponse(new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), Simulation.Phase, Simulation.Committed));
        var identity = new WorkshopCommandIdentity(command.Sequence, command.Epoch, command.Revision, command.Cadence, command.Projection);
        if (command.Kind == WorkshopCommandKind.Cancel)
        {
            var result = command.Target == _pendingCommand
                ? Simulation.CancelPending()
                : new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.AlreadyCommitted);
            return EncodeResponse(new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                result, Simulation.Phase, Simulation.Committed));
        }
        if ((_pendingCommand is not null || _preparedEndpoint is not null) && command.Kind != WorkshopCommandKind.Dispose)
            return EncodeResponse(new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), Simulation.Phase, Simulation.Committed));
        if (command.Kind == WorkshopCommandKind.ObserveMemory)
        {
            // A query: answered by this acknowledgement, never queued behind a tick or a pending GPU operation.
            var admitted = WorkshopMemoryQuery.Admit(Simulation.Phase, Simulation.HasPendingGpuOperation, Reads.Idle);
            return EncodeResponse(new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement, admitted,
                Simulation.Phase, Simulation.Committed,
                Memory: admitted.Outcome == WorkshopCommandOutcome.Applied ? WorkshopMemoryMeasurement.Capture() : null));
        }
        _pendingCommand = identity;
        try { return await Apply(command); }
        finally { if (_pendingCommand == identity) _pendingCommand = null; }
    }

    private static async Task<byte[]> Apply(WorkshopCommand command)
    {
        WorkshopCommandResult result;
        _transition = command.Kind switch
        {
            WorkshopCommandKind.Run => ScheduleTransition.Run, WorkshopCommandKind.Pause => ScheduleTransition.Pause,
            WorkshopCommandKind.Resume => ScheduleTransition.Resume, WorkshopCommandKind.Step => ScheduleTransition.Step,
            WorkshopCommandKind.Reset => ScheduleTransition.Reset, _ => ScheduleTransition.Configure
        };
        if (!WorkshopMessages.Matches(command, _simulation?.Epoch ?? new(1), _simulation?.Revision ?? default, Peer.Session,
            _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default))
            return EncodeResponse(new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), Simulation.Phase, Simulation.Committed));
        switch (command.Kind)
        {
            case WorkshopCommandKind.Initialize:
                if (_simulation is not null) throw new InvalidOperationException("Already initialized.");
                _simulation = new(Device, new CaptureClock(), command.Settings!.Value, new(1), new Installation(), GpuDeviceOwnership.Owned);
                result = await Simulation.Initialize(); break;
            case WorkshopCommandKind.ConfigureCadence:
                result = await Simulation.Configure(command.Settings!.Value, new(checked(++_nextCadence))); break;
            case WorkshopCommandKind.Pause:
                _loopOwner++;
                while (Simulation.HasPendingGpuOperation) await Task.Delay(1);
                result = await Simulation.Pause();
                if (result.Outcome != WorkshopCommandOutcome.Applied && Simulation.Phase == WorkshopSimulationPhase.Running)
                    _ = RunLoop(++_loopOwner);
                break;
            case WorkshopCommandKind.Resume: result = await Simulation.Resume(); break;
            case WorkshopCommandKind.Step: result = await Simulation.Step(); break;
            case WorkshopCommandKind.ConfigureElectrical:
                while (Simulation.HasPendingGpuOperation) await Task.Delay(1);
                result = Simulation.ConfigureElectrical(command.Electrical!.Value); break;
            case WorkshopCommandKind.Save: result = Simulation.Save(); break;
            case WorkshopCommandKind.Construct:
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                result = await Simulation.Construct(command.Construction!.Value); break;
            case WorkshopCommandKind.Run:
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                result = await Simulation.Run();
                break;
            case WorkshopCommandKind.Reset:
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Reset);
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                var resetting = Simulation.Reset();
                result = await resetting;
                if (result.Outcome == WorkshopCommandOutcome.Applied) _loopOwner++;
                break;
            case WorkshopCommandKind.Dispose:
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Disposed);
                _loopOwner++;
                await Simulation.DisposeAsync();
                result = new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);
                break;
            // Cancel and the memory query are answered in Execute; the decoder refuses every undefined kind.
            default: throw new WireFaultException(WireFaultReason.UnknownCommand, $"Command kind {(uint)command.Kind} has no executor.");
        }
        if (result.Outcome == WorkshopCommandOutcome.Applied && _preparedEndpoint is not null)
        {
            await CommitInstallation();
            if (command.Kind == WorkshopCommandKind.Run)
            {
                Trace.Begin(Peer.Session, Simulation.Construction, Simulation.Epoch, _schedule!.Value.World.Epoch, Simulation.Profile);
#if PLAYTEST
                Trace.Append(Device.DiagnosticCommitted);
#endif
            }
#if PLAYTEST
            if (command.Kind == WorkshopCommandKind.Step) Trace.Append(Device.DiagnosticCommitted);
#endif
            if (command.Kind is WorkshopCommandKind.Run or WorkshopCommandKind.Resume)
                _ = RunLoop(++_loopOwner);
        }
        return EncodeResponse(new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
            result, Simulation.Phase, Simulation.Committed));
    }

    private static byte[] EncodeResponse(WorkshopResponse response)
    {
        var populated = response with { Session = Peer.Session, Cadence = _schedule?.Revision ?? default,
            MasterGeneration = _schedule is null ? default : Peer.Generation,
            Projection = _schedule?.World.Epoch ?? default, SourcePulse = new(response.Read.Tick.Value) };
        return WorkshopWireFlatBuffers.EncodeResponse(populated);
    }

    private static async Task RunLoop(ulong owner)
    {
        try { await RunTicks(owner); }
        catch (Exception error)
        {
            // A superseded loop no longer owns the simulation, so it records its failure and changes nothing.
            if (owner != _loopOwner) { WorkshopDiagnostics.Fault(FaultSite.StaleRunLoop, error); return; }
            _loopOwner++;
            Simulation.Stop(WorkshopRejection.Transport);
            WorkshopDiagnostics.Fault(FaultSite.RunLoop, error);
            Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
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
        public bool Blocked => Simulation.HasPendingGpuOperation || _preparedEndpoint is not null || _pendingCommand is not null;
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
            Console.WriteLine($"CCGPU_DEBT_FAULT: committedTick={Simulation.Committed.Tick.Value} reason={reason} " +
                $"oldestUnreceipted={oldest?.Publication.Value ?? 0} ageMs={age}");
            Simulation.Stop(reason);
            Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
            EmitRead(new(WorkshopCommandOutcome.Faulted, Simulation.Fault));
            return ValueTask.CompletedTask;
        }

        public async ValueTask CompleteRun()
        {
            _transition = ScheduleTransition.Completed;
            await Simulation.CompleteRun();
            await CommitInstallation();
            Trace.End(CuriousContraptions.Simulation.TraceEnd.Complete);
            EmitRead(new(WorkshopCommandOutcome.Applied, WorkshopRejection.None));
        }

        public async ValueTask<TickAttempt> Advance()
        {
            if (!ReserveOccurrenceRead()) return TickAttempt.NotReserved;
            var previousRead = Simulation.Committed;
            var tickStarted = ObservationMilliseconds();
            WorkshopCommandResult result;
            try { result = await Simulation.Advance(); }
            catch { ReleaseOccurrenceRead(); throw; }
            if (owner != _loopOwner) { ReleaseOccurrenceRead(); return TickAttempt.Stopped; }
            if (result.Outcome is WorkshopCommandOutcome.Superseded or WorkshopCommandOutcome.Cancelled)
            { ReleaseOccurrenceRead(); return TickAttempt.Superseded; }
#if PLAYTEST
            if (result.Outcome == WorkshopCommandOutcome.Applied) Trace.Append(Device.DiagnosticCommitted);
#endif
            if (result.Outcome != WorkshopCommandOutcome.Applied)
            {
                Console.WriteLine($"CCGPU_TICK_FAILED: {result.Outcome} {result.Reason} fault={Simulation.Fault}");
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
            }
            EmitRead(result, Simulation.Committed.RequiresReliableRead(previousRead));
            if (result.Outcome != WorkshopCommandOutcome.Applied) return TickAttempt.Stopped;
            Trace.RecordTickDuration(Simulation.Committed.Tick, ObservationMilliseconds() - tickStarted);
            return TickAttempt.Committed;
        }

        public async ValueTask YieldTurn() => await NextTurn(0);
        public async ValueTask AwaitWork() => await NextTurn(1);
    }

    private static void EmitRead(WorkshopCommandResult result, bool retainOccurrence = false)
    {
        if (_publication == ulong.MaxValue) throw new InvalidOperationException("Physical publication identity exhausted.");
        var sequence = new PublicationSequence(_publication + 1);
        var readResponse = new WorkshopResponse(default, WorkshopResponseKind.Read, result,
            Simulation.Phase, Simulation.Committed, Peer.Session, sequence,
            Cadence: _schedule?.Revision ?? default,
            MasterGeneration: _schedule is null ? default : Peer.Generation,
            Projection: _schedule?.World.Epoch ?? default,
            SourcePulse: new(Simulation.Committed.Tick.Value));
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(readResponse, ReadBuilder);
        Reads.Publish(sequence, MasterNow());
        _publication++;
        // Existing read-only observation channel, in the production build every Chrome suite runs against (owner decision
        // h; Story 9A.24 replaces it with the typed observation port): retained energy/event commits prove finite
        // transfers in UI runs. Its measured cost (Story 9.4b D1, evidence-9-4b/diagnosis/retained-cost) is flagged to
        // the owner. The PlaytestDiagnostics variant only adds diagnostic recording and is never a gate.
        if (retainOccurrence)
        {
            Console.WriteLine("CCGPU_RETAINED " + Convert.ToBase64String(encoded));
        }
        Publish(encoded, WorkshopWireFlatBuffers.ReadIdentity(readResponse), WorkshopPoseRing.ComposeSlot(readResponse.Read.Capture?.Time.Value ?? 0, readResponse.Read.Bodies),
            Trace.CanFlush(Simulation.Phase), retainOccurrence);
    }
}
