using System;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public enum WorkshopTransportState { Ready, Backpressure, TimedOut, Indeterminate, RecoveryBlocked }

/// <summary>Transport boundary; the browser client is the sole production implementation.</summary>
internal interface IWorkshopClient : IAsyncDisposable
{
    SimulationEpoch Epoch { get; }
    AuthorityRevision Revision { get; }
    WorkshopCommandIdentity? Pending { get; }
    WorkshopTransportState TransportState { get; }
    Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null);
    Task<WorkshopDelivery> CancelPending();
    bool TryRead(out WorkshopResponse response);
}
