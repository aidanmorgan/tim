using System;
using System.Threading.Tasks;
using CuriousContraptions.Presentation;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreServices : ICoreServices
{
    public IWorkshopClientCursor CreateClientCursor(RuntimeSessionId session) => new WorkshopClientCursor(session);
    public IWorkshopPoseHistory CreatePoseHistory() => new WorkshopPoseHistory();
    public IMemoryServices Memory { get; } = new CoreMemoryServices();
    public IWorkshopTurnRegistry CreateTurnRegistry() => new WorkshopTurnRegistry();
    public IReliableReadLedger CreateReadLedger(RuntimeSessionId session) => new WorkshopReliableReadLedger(session);
    public IWorkshopCommandRouter CreateCommandRouter(Func<WorkshopCommand, Task<byte[]>> execute) => new WorkshopCommandRouter(execute);
    public IWorkshopCommandExecutor CreateCommandExecutor(IWorkshopGpuDevice device, IWorkshopCommandHost host, IWorkshopDiagnostics? diagnostics = null) => new WorkshopCommandExecutor(device, host, diagnostics);
    public IWorkshopWorkerDevice CreateWorkerDevice(CuriousContraptions.Abstractions.IEventRingPublication publication, PhysicsDocumentId document) => new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(publication), document);
    public IWorkshopTrace CreateTrace(IWorkshopDiagnostics? diagnostics = null) => new CuriousContraptions.Simulation.WorkshopTrace(diagnostics);
    public Task RunTicks(ITickLoopHost host) => WorkshopTickLoop.Run(host);
    public ICoreGeometry Geometry { get; } = new CoreGeometry();
    public ISupplyQueries Supply { get; } = new CoreSupplyQueries();
    public IWorkshopUiCurves UiCurves { get; } = new WorkshopUiCurves();
    public IRunControlServices RunControls { get; } = new CoreRunControlServices();
    public CaptureLatch ConsumeCapture(CaptureLatch latch, ResidenceRead candidate) => CaptureLatchController.Consume(latch, candidate);
    public uint TimerDuration(float seconds, SimulationCadence cadence) => ActivationTimerTransitions.Duration(seconds, cadence);
    public IClockServices Clocks { get; } = new CoreClockServices();
    public IRuntimeCodecs Codecs { get; } = new CoreRuntimeCodecs();
    public ISchedulingServices Scheduling { get; } = new CoreSchedulingServices();
    public IPresentationServices Presentation { get; } = new CorePresentationServices();
    public IWorkshopAnimationTracks CreateAnimationTracks(IAnimationServices animation) => new WorkshopAnimationTracks(animation);
    public IAnimationControlDeferral CreateAnimationDeferral(IWorkshopDiagnostics? diagnostics = null) => new AnimationControlDeferral(diagnostics);
    public IWorkshopDiagnostics CreateDiagnostics(Action<WorkshopDiagnostic>? publish = null) => new WorkshopDiagnostics(publish);
    public IWorkshopSimulation CreateSimulation(IWorkshopGpuDevice device, IWorkshopCaptureClock clock, ICompiledScene scene, CadenceRevision revision,
        IWorkshopInstallation installation, GpuDeviceOwnership deviceOwnership, IWorkshopDiagnostics? diagnostics = null) =>
        new WorkshopSimulation(device, clock, scene, revision, installation, deviceOwnership, diagnostics);
    public IPhysicsSceneDeclaration CreatePhysicsScene(PhysicsDocumentId document, ulong nextIdentity,
        ReadOnlySpan<RigidBodyDeclaration> bodies, ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<ContactMaterialDeclaration> materials, ReadOnlySpan<ResidenceSensorDeclaration> sensors,
        ReadOnlySpan<PlanarGuideDeclaration> guides, ReadOnlySpan<ContactTriggerDeclaration> triggers = default,
        ReadOnlySpan<ContactWorkDeclaration> contactWorks = default, ReadOnlySpan<OrientationSensorDeclaration> orientationSensors = default,
        ReadOnlySpan<ElectricalSourceDeclaration> electricalSources = default, ReadOnlySpan<ElectricalStorageBinding> electricalBindings = default,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics = default,
        ReadOnlySpan<SupplyNodeDeclaration> supplyNodes = default,
        ReadOnlySpan<SupplyEdge> supplyEdges = default,
        ReadOnlySpan<ContactLoadSensorDeclaration> contactLoadSensors = default,
        ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives = default,
        ReadOnlySpan<TensileSpanDeclaration> tensileSpans = default,
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes = default,
        ReadOnlySpan<CompliantSurfaceDeclaration> compliantSurfaces = default,
        ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives = default,
        ReadOnlySpan<RopeGuideDeclaration> ropeGuides = default,
        ReadOnlySpan<MechanicalTransmissionDeclaration> transmissions = default,
        ReadOnlySpan<DrivenSurfaceDeclaration> drivenSurfaces = default) =>
        new PhysicsSceneDeclaration(document, nextIdentity, bodies, colliders, materials, sensors, guides, triggers, contactWorks, orientationSensors, electricalSources, electricalBindings, prismatics, supplyNodes, supplyEdges, contactLoadSensors, prismaticDrives, tensileSpans, revolutes, compliantSurfaces, revoluteDrives, ropeGuides, transmissions, drivenSurfaces);
    public IActivationProgram CreateActivation(ReadOnlySpan<ActivationNodeDeclaration> nodes, ReadOnlySpan<ActivationEdge> edges,
        uint substeps = 4, ReadOnlySpan<PassageSensorDeclaration> passageSensors = default,
        ReadOnlySpan<CompiledSignalContact> contacts = default) =>
        new ActivationNetwork(nodes, edges, substeps, passageSensors, contacts);
    public ICompiledScene CreateScene(CompiledSceneId identity, ConstructionRevision revision, WorkshopCadenceSettings settings,
        IPhysicsSceneDeclaration physics, IActivationProgram activation, IEnumerable<CompiledPresentationTrack> presentation,
        IEnumerable<GpuBodyId> owners, IEnumerable<RunControlRow> runControls) =>
        new WorkshopCompiledScene(identity, revision, settings, physics, activation, presentation, owners, runControls);
}
