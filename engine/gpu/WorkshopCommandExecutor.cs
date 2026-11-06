using System;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>Shared worker/native command admission, execution and acknowledgement ownership.</summary>
public sealed class WorkshopCommandExecutor(IWorkshopGpuDevice device, IWorkshopCommandHost host, IWorkshopDiagnostics? diagnostics = null) : IWorkshopCommandExecutor
{
    private WorkshopSimulation? _simulation;
    private ulong _nextCadence = 1;
    public IWorkshopSimulation Simulation => _simulation ?? throw new InvalidOperationException("Workshop has not initialized.");
    public WorkshopCommandIdentity? Pending { get; private set; }
    public ValueTask DisposeAsync() => _simulation?.DisposeAsync() ?? ValueTask.CompletedTask;

    public async Task<byte[]> Execute(WorkshopCommand command)
    {
        if (!WorkshopMessages.Matches(command, _simulation?.Epoch ?? new(1), _simulation?.Revision ?? default,
            host.Session, host.Schedule?.Revision ?? default, host.Schedule?.World.Epoch ?? default))
            return Reply(command, new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration));
        var identity = new WorkshopCommandIdentity(command.Sequence, command.Epoch, command.Revision, command.Cadence, command.Projection);
        if (command.Kind == WorkshopCommandKind.Cancel)
            return Reply(command, command.Target == Pending && Pending is not null
                ? Simulation.CancelPending()
                : new(WorkshopCommandOutcome.Rejected, WorkshopRejection.AlreadyCommitted));
        if ((Pending is not null || host.InstallationPending) && command.Kind != WorkshopCommandKind.Dispose)
            return Reply(command, new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy));
        if (command.Kind == WorkshopCommandKind.ObserveMemory)
        {
            var admitted = WorkshopMemoryQuery.Admit(Simulation.Phase, Simulation.HasPendingGpuOperation, host.ReadsIdle);
            return Encode(new(command.Sequence, WorkshopResponseKind.Acknowledgement, admitted, Simulation.Phase,
                Simulation.Committed, Memory: admitted.Outcome == WorkshopCommandOutcome.Applied ? host.CaptureMemory() : null));
        }
        Pending = identity;
        try
        {
            await host.Before(command.Kind, _simulation);
            var result = await ApplyCommand(command);
            await host.After(command.Kind, result, Simulation);
            return Reply(command, result);
        }
        finally { if (Pending == identity) Pending = null; }
    }

    private async Task<WorkshopCommandResult> ApplyCommand(WorkshopCommand command)
    {
        WorkshopCommandResult result;
        switch (command.Kind)
        {
            case WorkshopCommandKind.Initialize:
                if (_simulation is not null) throw new InvalidOperationException("Already initialized.");
                _simulation = new(device, host.Clock, command.Scene!, new(1), host.Installation, GpuDeviceOwnership.Owned, diagnostics);
                result = await Simulation.Initialize(); break;
            case WorkshopCommandKind.ConfigureCadence:
                result = await Simulation.Configure(command.Scene!, new(checked(++_nextCadence))); break;
            case WorkshopCommandKind.Pause: result = await Simulation.Pause(); break;
            case WorkshopCommandKind.Resume: result = await Simulation.ResumeRun(); break;
            case WorkshopCommandKind.Step: result = await Simulation.StepOnce(); break;
            case WorkshopCommandKind.ConfigureRunControl: result = Simulation.ConfigureRunControl(command.RunControl!.Value); break;
            case WorkshopCommandKind.CutRope: result = Simulation.CutRope(command.Cut!.Value); break;
            case WorkshopCommandKind.Save: result = Simulation.Save(); break;
            case WorkshopCommandKind.Construct: result = await Simulation.Construct(command.Scene!); break;
            case WorkshopCommandKind.Run: result = await Simulation.Run(); break;
            case WorkshopCommandKind.Reset: result = await Simulation.Reset(); break;
            case WorkshopCommandKind.Dispose:
                await Simulation.DisposeAsync();
                result = new(WorkshopCommandOutcome.Applied, WorkshopRejection.None); break;
            default: throw new WireFaultException(WireFaultReason.UnknownCommand, $"Command kind {(uint)command.Kind} has no executor.");
        }
        return result;
    }

    private byte[] Reply(WorkshopCommand command, WorkshopCommandResult result) =>
        Encode(new(command.Sequence, WorkshopResponseKind.Acknowledgement, result,
            _simulation?.Phase ?? WorkshopSimulationPhase.Uninitialized, _simulation?.Committed ?? default));

    public byte[] Encode(WorkshopResponse response) => WorkshopWireFlatBuffers.EncodeResponse(Populate(response));

    public WorkshopResponse Populate(WorkshopResponse response) => response with
    {
        Session = host.Session, Cadence = host.Schedule?.Revision ?? default,
        MasterGeneration = host.Schedule is null ? default : host.Generation,
        Projection = host.Schedule?.World.Epoch ?? default, SourcePulse = new(response.Read.Tick.Value)
    };
}

