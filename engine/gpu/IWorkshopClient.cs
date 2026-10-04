using System;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public enum WorkshopTransportState { Ready, Backpressure, TimedOut, Indeterminate, RecoveryBlocked }

/// <summary>Transport boundary; the browser client is the sole production implementation.</summary>
internal interface IWorkshopClient : IAsyncDisposable
{
    WorkshopCadenceSettings Settings { get; }
    void ControlHint(AnimationControlKind kind, bool visible);
    bool TryHint(ulong frame, out WorkshopHintSample sample);
    void RecordHintPresentation(ulong frame);
    bool TryCaptureOpacity(ulong frame, WorkshopPresentationSample physical, out Half opacity);
    void RecordCapturePresentation(ulong frame);
    bool TryActivationBlend(ulong frame, WorkshopPresentationSample physical, ActivationNodeId node, out Half blend);
    SimulationEpoch Epoch { get; }
    AuthorityRevision Revision { get; }
    WorkshopCommandIdentity? Pending { get; }
    WorkshopTransportState TransportState { get; }
    Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null);
    Task<WorkshopDelivery> CancelPending();
    bool TryRead(out WorkshopResponse response);
    bool TryPresent(ulong frame, out WorkshopPresentationSample sample);
    void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene);
}
