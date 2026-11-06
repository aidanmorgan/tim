using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreMemoryServices : IMemoryServices
{
    public WorkshopMemoryMeasurement Capture() => WorkshopMemorySampler.Capture();
    public bool IsStopped(WorkshopSimulationPhase phase) => WorkshopMemoryQuery.IsStopped(phase);
    public WorkshopCommandResult Admit(WorkshopSimulationPhase phase, bool gpuOperationPending, bool readsReceipted) => WorkshopMemoryQuery.Admit(phase, gpuOperationPending, readsReceipted);
    public void ValidateCarriage(WorkshopCommandKind kind, WorkshopResponse acknowledgement) => WorkshopMemoryQuery.ValidateCarriage(kind, acknowledgement);
    public WorkshopMemoryReply Complete(WorkshopCommandKind kind, WorkshopResponse acknowledgement, bool applicable, Func<WorkshopMemoryMeasurement> captureBrowser) => WorkshopMemoryQuery.Complete(kind, acknowledgement, applicable, captureBrowser);
    public IMemoryQueryOwner CreateOwner() => new WorkshopMemoryQueryOwner();
}
