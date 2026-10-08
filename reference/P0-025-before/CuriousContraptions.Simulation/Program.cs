using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;

[assembly: SupportedOSPlatform("browser")]

public static partial class Program
{
    private static readonly WorkshopGpuDevice Device = new(new BrowserGpuTransport());
    private static readonly WorkshopSimulation Simulation = new(Device);
    private static readonly WorkshopCommandRouter Commands = new(Execute);
    private static readonly byte[] ReadOutput = new byte[WorkshopWire.ResponseBytes];
    private static ulong _loopOwner;
    private static WorkshopCommandIdentity? _pendingCommand;
    private static bool _reliableStalled;
#if PLAYTEST
    private static readonly CuriousContraptions.Simulation.WorkshopTrace Trace = new();
#endif
    public static void Main() { }

    [JSImport("publish", "workshopGpu")]
    private static partial void Publish(byte[] read);

    [JSImport("acknowledge", "workshopGpu")]
    private static partial void Acknowledge(byte[] response);
    [JSImport("retire", "workshopGpu")]
    private static partial void Retire(string detail);

    [JSExport]
    public static int[] OperationAbi() => [(int)WorkshopGpuOperation.Admit, (int)WorkshopGpuOperation.Advance];
    [JSExport]
    public static int StateBytes() => WorkshopGpuAbi.ByteLength;

    [JSExport]
    public static async Task Dispatch(byte[] bytes) => Acknowledge(await Commands.Dispatch(bytes));


    private static async Task<byte[]> Execute(WorkshopCommand command, byte[] output)
    {
        if (!WorkshopWire.Matches(command, Simulation.Epoch, Simulation.Revision))
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), Simulation.Phase, Simulation.Committed));
        var identity = new WorkshopCommandIdentity(command.Sequence, command.Epoch, command.Revision);
        if (command.Kind == WorkshopCommandKind.Cancel)
        {
            var result = command.Target == _pendingCommand
                ? Simulation.CancelPending()
                : new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.AlreadyCommitted);
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                result, Simulation.Phase, Simulation.Committed));
        }
        if (_pendingCommand is not null && command.Kind != WorkshopCommandKind.Dispose)
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), Simulation.Phase, Simulation.Committed));
        _pendingCommand = identity;
        try { return await Apply(command, output); }
        finally { if (_pendingCommand == identity) _pendingCommand = null; }
    }

    private static async Task<byte[]> Apply(WorkshopCommand command, byte[] output)
    {
        WorkshopCommandResult result;
        if (!WorkshopWire.Matches(command, Simulation.Epoch, Simulation.Revision))
            return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), Simulation.Phase, Simulation.Committed));
        switch (command.Kind)
        {
            case WorkshopCommandKind.Initialize: result = await Simulation.Initialize(); break;
            case WorkshopCommandKind.Construct: result = await Simulation.Construct(command.Construction!.Value); break;
            case WorkshopCommandKind.Run:
                result = await Simulation.Run();
                if (result.Outcome == WorkshopCommandOutcome.Applied)
                {
#if PLAYTEST
                    Trace.Begin(Simulation.Construction, Simulation.Epoch, Device.DiagnosticCommitted);
#endif
                    var owner = ++_loopOwner;
                    _ = RunLoop(owner);
                }
                break;
            case WorkshopCommandKind.Reset:
#if PLAYTEST
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Reset);
#endif
                var resetting = Simulation.Reset();
                result = await resetting;
                if (result.Outcome == WorkshopCommandOutcome.Applied)
                {
                    _loopOwner++;
                    _reliableStalled = false;
                }
                break;
            case WorkshopCommandKind.Dispose:
#if PLAYTEST
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Disposed);
#endif
                _loopOwner++;
                await Simulation.DisposeAsync();
                result = new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);
                break;
            default: throw new ArgumentException("Unsupported Workshop command.");
        }
        return EncodeResponse(output, new WorkshopResponse(command.Sequence, WorkshopResponseKind.Acknowledgement,
            result, Simulation.Phase, Simulation.Committed));
    }

    private static byte[] EncodeResponse(byte[] output, WorkshopResponse response)
    {
        WorkshopWire.Write(response, output);
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
#if PLAYTEST
            Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
#endif
            Retire("Physical publication failed after commit: " + error.Message);
        }
    }

    private static async Task RunTicks(ulong owner)
    {
        var clock = Stopwatch.StartNew();
        var batch = 0;
        while (owner == _loopOwner && Simulation.Phase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Resetting)
        {
            if (Simulation.Phase == WorkshopSimulationPhase.Resetting || Simulation.HasPendingGpuOperation)
            { await Task.Delay(1); continue; }
            var debt = clock.Elapsed.TotalMilliseconds - Simulation.Committed.Tick.Value * (1000.0 / 120);
            if (_reliableStalled || debt > 100)
            {
                Simulation.Stop(_reliableStalled ? WorkshopRejection.ReliableStalled : WorkshopRejection.Capacity);
#if PLAYTEST
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
#endif
                EmitRead(new(WorkshopCommandOutcome.Faulted, Simulation.Fault));
                return;
            }
            if (Simulation.Committed.Tick.Value == 3600)
            {
                Simulation.CompleteRun();
#if PLAYTEST
                Trace.End(CuriousContraptions.Simulation.TraceEnd.Complete);
#endif
                EmitRead(new(WorkshopCommandOutcome.Applied, WorkshopRejection.None));
                return;
            }
            var result = await Simulation.Advance();
            if (owner != _loopOwner) return;
            if (result.Outcome is WorkshopCommandOutcome.Superseded or WorkshopCommandOutcome.Cancelled) continue;
#if PLAYTEST
            if (result.Outcome == WorkshopCommandOutcome.Applied) Trace.Append(Device.DiagnosticCommitted);
            else Trace.End(CuriousContraptions.Simulation.TraceEnd.Fault);
#endif
            EmitRead(result);
            if (result.Outcome != WorkshopCommandOutcome.Applied) return;
            var remaining = Simulation.Committed.Tick.Value * (1000.0 / 120) - clock.Elapsed.TotalMilliseconds;
            if (remaining >= 1) { batch = 0; await Task.Delay((int)remaining); }
            else if (++batch == 4) { batch = 0; await Task.Delay(1); }
        }
    }
    private static void EmitRead(WorkshopCommandResult result) =>
        Publish(EncodeResponse(ReadOutput, new WorkshopResponse(default, WorkshopResponseKind.Read, result,
            Simulation.Phase, Simulation.Committed)));

    [JSExport]
    public static void ReliableStalled() => _reliableStalled = true;

    [JSExport]
    public static void DeviceLost()
    {
        _loopOwner++;
#if PLAYTEST
        Trace.End(CuriousContraptions.Simulation.TraceEnd.DeviceLost);
#endif
        Device.MarkLost();
        Simulation.DeviceLost();
        EmitRead(new(WorkshopCommandOutcome.Faulted, WorkshopRejection.DeviceLost));
    }
}
