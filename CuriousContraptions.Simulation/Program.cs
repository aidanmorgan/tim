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
    private static MemoryObservationId _lastMemoryObservation;
    private static readonly WorkshopCommandRouter Commands = new(Execute);
    private static readonly byte[] ReadOutput = new byte[WorkshopWire.ResponseBytes];
    private static ulong _loopOwner;
    private static WorkshopCommandIdentity? _pendingCommand;
    private static bool _reliableStalled;
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
        var session = WorkshopWire.ReadSession(bytes);
        var origin = WorkshopNativeClock.FromMilliseconds(NativeMilliseconds());
        var peer = new WorkshopClockPeer(session, new(1), origin, new(1), new(100_000), WorkshopRuntimeRole.Browser);
        _device = new(new BrowserGpuTransport(), new(session.Low, session.High));
        _peer = peer;
        return WorkshopClockWire.EncodePeer(peer);
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
            var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
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

    [JSImport("publish", "workshopGpu")]
    private static partial void Publish(byte[] read, bool observationPending, bool retainOccurrence);
    [JSImport("reserveOccurrenceRead", "workshopGpu")] private static partial bool ReserveOccurrenceRead();
    [JSImport("releaseOccurrenceRead", "workshopGpu")] private static partial void ReleaseOccurrenceRead();

    [JSImport("acknowledge", "workshopGpu")]
    private static partial void Acknowledge(byte[] response, bool observationPending);
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

    [JSExport]
    public static byte[] ObserveMemory(byte[] bytes, bool transportIdle)
    {
        var request = WorkshopMemoryWire.DecodeRequest(bytes);
        if (request.Context != WorkshopMemoryContext.Simulation)
            throw new ArgumentException("Wrong memory observation context.");
        var status = request.Session != Peer.Session || request.Epoch != Simulation.Epoch ||
            request.Revision != Simulation.Revision || request.Publication.Value != _publication || request.Id.Value <= _lastMemoryObservation.Value
            ? WorkshopMemoryStatus.Stale
            : !transportIdle || _pendingCommand is not null || Simulation.HasPendingGpuOperation || !WorkshopMemoryWire.IsStopped(Simulation.Phase)
                ? WorkshopMemoryStatus.Busy : WorkshopMemoryStatus.Accepted;
        if (status != WorkshopMemoryStatus.Stale) _lastMemoryObservation = request.Id;
        return WorkshopMemoryWire.Encode(status == WorkshopMemoryStatus.Accepted
            ? WorkshopMemoryWire.Capture(request) : new(request, status));
    }

    [JSExport]
    public static int[] CommandAbi() => [16, 65536];
    [JSExport]
    public static int[] ResponseAbi() => WorkshopWire.ResponseAbi();

    [JSExport]
    public static async Task Dispatch(byte[] bytes)
    {
        var response = await Commands.Dispatch(bytes);
        Acknowledge(response, Trace.CanFlush(Simulation.Phase));
    }


    private static async Task<byte[]> Execute(WorkshopCommand command, byte[] output)
    {
        if (!WorkshopWire.Matches(command, _simulation?.Epoch ?? new(1), _simulation?.Revision ?? default, Peer.Session,
            _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default))
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), Simulation.Phase, Simulation.Committed));
        var identity = new WorkshopCommandIdentity(command.Sequence, command.Epoch, command.Revision, command.Cadence, command.Projection);
        if (command.Kind == WorkshopCommandKind.Cancel)
        {
            var result = command.Target == _pendingCommand
                ? Simulation.CancelPending()
                : new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.AlreadyCommitted);
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                result, Simulation.Phase, Simulation.Committed));
        }
        if ((_pendingCommand is not null || _preparedEndpoint is not null) && command.Kind != WorkshopCommandKind.Dispose)
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), Simulation.Phase, Simulation.Committed));
        _pendingCommand = identity;
        try { return await Apply(command, output); }
        finally { if (_pendingCommand == identity) _pendingCommand = null; }
    }

    private static async Task<byte[]> Apply(WorkshopCommand command, byte[] output)
    {
        WorkshopCommandResult result;
        _transition = command.Kind switch
        {
            WorkshopCommandKind.Run => ScheduleTransition.Run, WorkshopCommandKind.Pause => ScheduleTransition.Pause,
            WorkshopCommandKind.Resume => ScheduleTransition.Resume, WorkshopCommandKind.Step => ScheduleTransition.Step,
            WorkshopCommandKind.Reset => ScheduleTransition.Reset, _ => ScheduleTransition.Configure
        };
        if (!WorkshopWire.Matches(command, _simulation?.Epoch ?? new(1), _simulation?.Revision ?? default, Peer.Session,
            _schedule?.Revision ?? default, _schedule?.World.Epoch ?? default))
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
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
                if (result.Outcome == WorkshopCommandOutcome.Applied)
                {
                    _loopOwner++;
                    _reliableStalled = false;
                }
                break;
            case WorkshopCommandKind.Dispose:
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Disposed);
                _loopOwner++;
                await Simulation.DisposeAsync();
                result = new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);
                break;
            default: throw new ArgumentException("Unsupported Workshop command.");
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
        return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
            result, Simulation.Phase, Simulation.Committed));
    }

    private static byte[] EncodeResponse(byte[] output, WorkshopResponse response)
    {
        var populated = response with { Session = Peer.Session, Cadence = _schedule?.Revision ?? default,
            MasterGeneration = _schedule is null ? default : Peer.Generation,
            Projection = _schedule?.World.Epoch ?? default, SourcePulse = new(response.Read.Tick.Value) };
        if (output.Length == 0)
        {
            return WorkshopWireFlatBuffers.EncodeResponse(populated);
        }
        WorkshopWire.Write(populated, output);
        return output;
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
        public bool DebtExceeded => _reliableStalled || Schedule.ExceedsPhysicalDebt(Simulation.Committed.Tick, MasterNow());
        public bool AtRunLimit => Simulation.Committed.Tick.Value == Simulation.Profile.RunTickLimit;
        public bool CaughtUp => Simulation.Committed.Tick.Value >= Schedule.World.DueTick(Schedule.Settings, MasterNow()).Value;

        public ValueTask StopForDebt()
        {
            Console.WriteLine($"CCGPU_DEBT_FAULT: committedTick={Simulation.Committed.Tick.Value} reliableStalled={_reliableStalled}");
            Simulation.Stop(_reliableStalled ? WorkshopRejection.ReliableStalled : WorkshopRejection.Capacity);
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
        var encoded = EncodeResponse(ReadOutput, readResponse);
        _publication++;
        // Existing read-only observation channel, in the production build every Chrome suite runs against (owner decision
        // h; Story 9A.24 replaces it with the typed observation port): retained energy/event commits prove finite
        // transfers in UI runs. Its measured cost (Story 9.4b D1, evidence-9-4b/diagnosis/retained-cost) is flagged to
        // the owner. The PlaytestDiagnostics variant only adds diagnostic recording and is never a gate.
        if (retainOccurrence)
        {
            Console.WriteLine("CCGPU_RETAINED " + Convert.ToBase64String(WorkshopWireFlatBuffers.EncodeResponse(readResponse)));
        }
        Publish(encoded, Trace.CanFlush(Simulation.Phase), retainOccurrence);
    }

    [JSExport]
    public static void SetReliableStall(bool stalled) => _reliableStalled = stalled;
}
