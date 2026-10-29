using System;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>Environment ownership around the single command implementation. Hosts never apply commands.</summary>
public interface IWorkshopCommandHost
{
    RuntimeSessionId Session { get; }
    WorkshopSchedule? Schedule { get; }
    ClockGeneration Generation { get; }
    bool InstallationPending { get; }
    bool ReadsIdle { get; }
    IWorkshopCaptureClock Clock { get; }
    IWorkshopInstallation Installation { get; }
    ValueTask Before(WorkshopCommandKind kind, WorkshopSimulation? simulation);
    ValueTask After(WorkshopCommandKind kind, WorkshopCommandResult result, WorkshopSimulation simulation);
    WorkshopMemoryMeasurement CaptureMemory();
}

/// <summary>Shared worker/native command admission, execution and acknowledgement ownership.</summary>
public sealed class WorkshopCommandExecutor(IWorkshopGpuDevice device, IWorkshopCommandHost host, WorkshopDiagnostics? diagnostics = null) : IAsyncDisposable
{
    private WorkshopSimulation? _simulation;
    private ulong _nextCadence = 1;
    public WorkshopSimulation Simulation => _simulation ?? throw new InvalidOperationException("Workshop has not initialized.");
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
                case WorkshopCommandKind.Resume: result = await Simulation.Resume(); break;
                case WorkshopCommandKind.Step: result = await Simulation.Step(); break;
                case WorkshopCommandKind.ConfigureElectrical: result = Simulation.ConfigureElectrical(command.Electrical!.Value); break;
                case WorkshopCommandKind.Save: result = Simulation.Save(); break;
                case WorkshopCommandKind.Construct: result = await Simulation.Construct(command.Scene!); break;
                case WorkshopCommandKind.Run: result = await Simulation.Run(); break;
                case WorkshopCommandKind.Reset: result = await Simulation.Reset(); break;
                case WorkshopCommandKind.Dispose:
                    await Simulation.DisposeAsync();
                    result = new(WorkshopCommandOutcome.Applied, WorkshopRejection.None); break;
                default: throw new WireFaultException(WireFaultReason.UnknownCommand, $"Command kind {(uint)command.Kind} has no executor.");
            }
            await host.After(command.Kind, result, Simulation);
            return Reply(command, result);
        }
        finally { if (Pending == identity) Pending = null; }
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

