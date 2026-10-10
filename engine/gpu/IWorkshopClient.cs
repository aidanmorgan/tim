using System;
using System.Threading.Tasks;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public enum WorkshopTransportState { Ready, Backpressure, TimedOut, Indeterminate, RecoveryBlocked }

/// <summary>Transport boundary; the browser client is the sole production implementation.</summary>
internal interface IWorkshopClient : IAsyncDisposable
{
    WorkshopCadenceSettings Settings { get; }
    /// <summary>Queue one player control for a Control-fed UI target; it is sent when the single animation lease is free.</summary>
    void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible);
    /// <summary>One sampled opacity for a declared UI target, at most once per admitted display frame.</summary>
    bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out AnimationOpacity opacity);
    /// <summary>One sampled cosmetic blend for the part whose instance declares a cosmetic curve.</summary>
    bool TryCosmeticFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample sample);
    bool TryElectricalFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out ElectricalIndicatorSample sample);
    SimulationEpoch Epoch { get; }
    AuthorityRevision Revision { get; }
    WorkshopCommandIdentity? Pending { get; }
    WorkshopTransportState TransportState { get; }
    Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null);
    Task<WorkshopDelivery> CancelPending();
    bool TryRead(out WorkshopResponse response);
    bool TryPresent(ulong frame, out WorkshopPresentationSample sample);
    void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene);
}
