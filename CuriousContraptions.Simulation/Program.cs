using System.Runtime.Intrinsics;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;

[assembly: SupportedOSPlatform("browser")]

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

    [JSExport]
    public static Task PrepareGpu() => Device.Prepare();

    [JSExport]
    public static byte[] ClockReply(byte[] bytes, double receivedMilliseconds) =>
        WorkshopClockWire.EncodeReply(bytes, Peer, WorkshopNativeClock.FromMilliseconds(receivedMilliseconds),
            WorkshopNativeClock.FromMilliseconds(NativeMilliseconds()));

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
        catch { return double.NaN; } // Incomplete observation never replaces the physical result.
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
    public static int ElectricalState(int ownerLow, int ownerHigh, int previous)
    {
        if (previous is < 0 or > 1) throw new ArgumentException("Invalid electrical enable value.");
        return (int)Simulation.ElectricalState(new(((ulong)(uint)ownerHigh << 32) | (uint)ownerLow), (ElectricalEnable)previous);
    }

    [JSExport]
    public static double[] AllocateElectrical(int enabled, double balance, double power, double debit,
        double[] balances, double[] capacities, double[] credits)
    {
        if (enabled is < 0 or > 1 || balances.Length != capacities.Length || balances.Length != credits.Length || balances.Length > ElectricalSupply.LoadCapacity)
            throw new ArgumentException("Invalid electrical boundary shape.");
        Span<float> before = stackalloc float[ElectricalSupply.LoadCapacity];
        Span<float> limits = stackalloc float[ElectricalSupply.LoadCapacity];
        Span<float> after = stackalloc float[ElectricalSupply.LoadCapacity];
        for (var i = 0; i < balances.Length; i++) { before[i] = (float)balances[i]; limits[i] = (float)capacities[i]; }
        var transfer = ElectricalSupply.Allocate((ElectricalEnable)enabled, (float)balance, (float)power,
            before[..balances.Length], limits[..balances.Length], after[..balances.Length]);
        var result = new double[2 + 2 * balances.Length];
        result[0] = transfer.Remaining; result[1] = (float)debit + transfer.Debit;
        for (var i = 0; i < balances.Length; i++)
        {
            result[2 + 2 * i] = after[i];
            result[3 + 2 * i] = (float)credits[i] + (after[i] - before[i]);
        }
        return result;
    }

    // Explicit JS boundary: the reusable C# law executes f32/SIMD inside this WASM worker.
    [JSExport]
    public static double[] PaidContactImpulse(double speed, double target, double inverseMass,
        double available, double nx, double ny, double nz)
    {
        var response = ContactWorkImpulse.FromStore((float)speed, (float)target, (float)inverseMass,
            (float)available, System.Runtime.Intrinsics.Vector128.Create((float)nx, (float)ny, (float)nz, 0f));
        return [response.Remaining, response.Debit,
            response.Impulse.GetElement(0), response.Impulse.GetElement(1), response.Impulse.GetElement(2)];
    }

    [JSExport]
    public static int[] OperationAbi() => [(int)WorkshopGpuOperation.Admit, (int)WorkshopGpuOperation.Advance];
    [JSExport]
    public static int StateBytes() => PhysicsGpuAbi.ByteLength;
    [JSExport]
    public static int[] CommandAbi() => [WorkshopWire.CommandHeaderBytes, WorkshopWire.CommandHeaderBytes + WorkshopWire.ConstructionBytes];
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
                _simulation = new(Device, new CaptureClock(), command.Settings!.Value, new(1), new Installation());
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
            case WorkshopCommandKind.Construct: result = await Simulation.Construct(command.Construction!.Value); break;
            case WorkshopCommandKind.Run:
                result = await Simulation.Run();
                break;
            case WorkshopCommandKind.Reset:
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Reset);
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
        WorkshopWire.Write(response with { Session = Peer.Session, Cadence = _schedule?.Revision ?? default,
            MasterGeneration = _schedule is null ? default : Peer.Generation,
            Projection = _schedule?.World.Epoch ?? default, SourcePulse = new(response.Read.Tick.Value) }, output);
        return output;
    }

    private static async Task RunLoop(ulong owner)
    {
        try { await RunTicks(owner); }
        catch (Exception error)
        {
            if (owner != _loopOwner) return;
            _loopOwner++;
            Simulation.Stop(WorkshopRejection.Transport);
            Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
            Retire("Physical publication failed after commit: " + error.Message);
        }
    }

    private static async Task RunTicks(ulong owner)
    {
        var batch = 0;
        while (owner == _loopOwner && Simulation.Phase == WorkshopSimulationPhase.Running)
        {
            if (Simulation.HasPendingGpuOperation || _preparedEndpoint is not null || _pendingCommand is not null)
            { await Task.Delay(1); continue; }
            var schedule = _schedule ?? throw new InvalidOperationException("Missing physical schedule.");
            var now = MasterNow();
            if (_reliableStalled || schedule.ExceedsPhysicalDebt(Simulation.Committed.Tick, now))
            {
                Simulation.Stop(_reliableStalled ? WorkshopRejection.ReliableStalled : WorkshopRejection.Capacity);
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
                EmitRead(new(WorkshopCommandOutcome.Faulted, Simulation.Fault));
                return;
            }
            if (Simulation.Committed.Tick.Value == Simulation.Profile.RunTickLimit)
            {
                _transition = ScheduleTransition.Completed;
                await Simulation.CompleteRun();
                await CommitInstallation();
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Complete);
                EmitRead(new(WorkshopCommandOutcome.Applied, WorkshopRejection.None));
                return;
            }
            var due = schedule.World.DueTick(schedule.Settings, now);
            if (Simulation.Committed.Tick.Value >= due.Value)
            { batch = 0; await Task.Delay(1); continue; }
            if (!ReserveOccurrenceRead()) { await Task.Delay(1); continue; }
            var previousRead = Simulation.Committed;
            var tickStarted = ObservationMilliseconds();
            WorkshopCommandResult result;
            try { result = await Simulation.Advance(); }
            catch { ReleaseOccurrenceRead(); throw; }
            if (owner != _loopOwner) { ReleaseOccurrenceRead(); return; }
            if (result.Outcome is WorkshopCommandOutcome.Superseded or WorkshopCommandOutcome.Cancelled)
            { ReleaseOccurrenceRead(); continue; }
#if PLAYTEST
            if (result.Outcome == WorkshopCommandOutcome.Applied) Trace.Append(Device.DiagnosticCommitted);
#endif
            if (result.Outcome != WorkshopCommandOutcome.Applied) Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
            EmitRead(result, Simulation.Committed.RequiresReliableRead(previousRead));
            if (result.Outcome == WorkshopCommandOutcome.Applied)
                Trace.RecordTickDuration(Simulation.Committed.Tick, ObservationMilliseconds() - tickStarted);
            if (result.Outcome != WorkshopCommandOutcome.Applied) return;
            if (++batch == 4) { batch = 0; await Task.Delay(1); }
        }
    }
    private static void EmitRead(WorkshopCommandResult result, bool retainOccurrence = false)
    {
        if (_publication == ulong.MaxValue) throw new InvalidOperationException("Physical publication identity exhausted.");
        var sequence = new PublicationSequence(_publication + 1);
        var encoded = EncodeResponse(ReadOutput, new WorkshopResponse(default, WorkshopResponseKind.Read, result,
            Simulation.Phase, Simulation.Committed, Peer.Session, sequence));
        _publication++;
#if PLAYTEST
        // Existing binary read diagnostics; retained energy/event commits prove finite transfers in UI runs.
        if (retainOccurrence) Console.WriteLine("CCGPU_RETAINED " + Convert.ToBase64String(encoded));
#endif
        Publish(encoded, Trace.CanFlush(Simulation.Phase), retainOccurrence);
    }

    [JSExport]
    public static void SetReliableStall(bool stalled) => _reliableStalled = stalled;

    [JSExport]
    public static void DeviceLost()
    {
        _loopOwner++;
        Trace.End(CuriousContraptions.Simulation.TraceEnd.DeviceLost);
        Device.MarkLost();
        if (_simulation is null) throw new InvalidOperationException("GPU device was lost during startup preparation.");
        Simulation.DeviceLost();
        EmitRead(new(WorkshopCommandOutcome.Faulted, WorkshopRejection.DeviceLost));
    }
}
