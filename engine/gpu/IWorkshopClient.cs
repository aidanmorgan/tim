using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
    void SetDeclaredVisible(GpuBodyId owner, bool visible);
    bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out IReadOnlyList<DeclaredPresentationSample> samples);
    SimulationEpoch Epoch { get; }
    AuthorityRevision Revision { get; }
    WorkshopCommandIdentity? Pending { get; }
    WorkshopTransportState TransportState { get; }
    Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null, RopeSpanId? cut = null);
    Task<WorkshopDelivery> CancelPending();
    bool TryRead(out WorkshopResponse response);
    bool TryPresent(ulong frame, out WorkshopPresentationSample sample);
    void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene);
}

/// <summary>The browser clock and worker-message boundary; production delegates to the JavaScript module.</summary>
internal interface IWorkshopBrowserTransport
{
    bool AnimationQualified(int client);
    void AnimationKinds(int client, int[] kinds);
    int[] DisplayRate(int client);
    void ScheduleResult(int client, byte[] bytes);
    void SendAnimationControl(int client, byte[] bytes);
    MonotonicNanoseconds NativeNow();
    void SendProbe(int client, byte[] probe);
    Task<int> CreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
        Action clockReply,
        Action physicalRead,
        Action scheduleControl,
        Action animationOutput,
        Action service, Action observation, Action<string> retired);
    byte[] MasterBootstrap(int client);
    void ActivateClient(int client);
    byte[] EventBytes(int client);
    MonotonicNanoseconds EventTime(int client);
    void Qualified(int client);
    int Status(int client);
    Task Send(int client, byte[] command, byte[] sequence);
    byte[] Acknowledgement(int client, byte[] sequence);
    void CompleteAcknowledgement(int client, byte[] sequence);
    void DisposeClient(int client);
    byte[] ReadEvents(int client, string epoch);
    int OpenObservationEvents(int client);
    byte[] ReadObservationEvents(int client, int cursor, string epoch);
    void CloseObservationEvents(int client, int cursor);
}
