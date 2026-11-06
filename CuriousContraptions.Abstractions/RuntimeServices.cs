using System;
using CuriousContraptions.Presentation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public enum WorkshopSimulationPhase { Uninitialized, Initializing, Building, Admitting, Starting, Running, Paused, Completed, Resetting, Faulted, Disposed }
public enum WorkshopCommandOutcome { Applied, Rejected, Superseded, Faulted, Cancelled }
// Wire values are explicit. 6 was DeviceLost: the worker owns no device since the pure C# engine (Story 9.2b) and
// Story 9.4b deleted that path; 6 is undefined on the wire and rejects.
public enum WorkshopRejection { None = 0, Busy = 1, InvalidConstruction = 2, WrongRevision = 3, WrongPhase = 4,
    GpuAdmission = 5, InvalidRead = 7, IdentityExhausted = 8, StaleGeneration = 9, Cancelled = 10, AlreadyCommitted = 11,
    Capacity = 12, ReliableStalled = 13, Transport = 14, UnknownRopeSpan = 15, RopeAlreadyCut = 16,
    UnknownRunControl = 17, ForeignRunControl = 18, RunControlKindMismatch = 19, RunControlOutOfRange = 20 }
public readonly record struct WorkshopCommandResult(WorkshopCommandOutcome Outcome, WorkshopRejection Reason);
public readonly record struct WorkshopGpuCandidate(CommandSequence Sequence, WorkshopRead Read);

/// <summary>Implemented only by the simulation worker's WebGPU adapter. No numerical host alternative.</summary>
public interface IWorkshopGpuDevice : IAsyncDisposable
{
    ValueTask<WorkshopGpuCandidate> Admit(ICompiledScene construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence);
    /// <summary>One physics step from the committed world. The controls carry every player input the step reads (per-source enables
    /// and per-drive target speeds); the device adds committed switch state for its compiled switched supplies. Physics never reads
    /// authoring state.</summary>
    ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead committed, CommandSequence sequence, WorkshopTickControls controls);
    // Swap candidate buffers only after host validation. Discard is idempotent, nonthrowing, and
    // sequence-specific; a superseded request cannot free a newer candidate or committed buffers.
    void Commit(CommandSequence sequence);
    void Discard(CommandSequence sequence);
}
/// <summary>Current scene demand cannot be represented by the available storage type.</summary>
public sealed class WorkshopStorageException(string message) : Exception(message);

public sealed class GpuAdmissionException : Exception
{
    public GpuAdmissionException(string message) : base(message) { }
}

/// <summary>
/// Exclusive discrete owner in the simulation worker. An awaited GPU candidate cannot mutate a
/// newer operation. At most one active GPU request plus one superseding Reset can be retained.
/// Callers publish the value-only Committed read after an Applied result; no observer callback is
/// part of the authority transaction.
/// </summary>
/// <summary>The presentation installation did not complete: superseded (no cause) or failed (the cause is inner).</summary>
public sealed class WorkshopInstallationException : Exception
{
    public WorkshopRejection Reason { get; } = WorkshopRejection.Transport;
    public WorkshopInstallationException() { }
    public WorkshopInstallationException(Exception cause) : base(null, cause) { }
    public WorkshopInstallationException(WorkshopRejection reason)
    {
        if (reason is not (WorkshopRejection.Transport or WorkshopRejection.Capacity))
            throw new ArgumentOutOfRangeException(nameof(reason));
        Reason = reason;
    }
}

public interface IWorkshopInstallation
{
    void Abort(CommandSequence owner);
    void Retire();
    ValueTask Prepare(CommandSequence owner, WorkshopRead read, ICompiledScene construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase);
}

/// <summary>Whether disposing a simulation also disposes its GPU device. The worker's simulation owns the device;
/// a borrowed simulation must leave the shared device to that owner.</summary>
public enum GpuDeviceOwnership { Owned = 1, Borrowed = 2 }

public interface IWorkshopSimulation : IAsyncDisposable
{
    WorkshopCommandResult CutRope(RopeSpanId span);
    RunControlValue? HeldRunControl(RunControlId id);
    WorkshopCommandResult ConfigureRunControl(WorkshopRunControl control);
    WorkshopGpuProfile Profile { get; }
    bool HasPendingGpuOperation { get; }
    AuthorityRevision Revision { get; }
    SimulationEpoch Epoch { get; }
    ICompiledScene Scene { get; }
    WorkshopRead Committed { get; }
    WorkshopSimulationPhase Phase { get; }
    WorkshopRejection Fault { get; }
    IWorkshopDiagnostics Diagnostics { get; }
    ValueTask<WorkshopCommandResult> Initialize();
    ValueTask<WorkshopCommandResult> Construct(ICompiledScene candidate);
    WorkshopCommandResult Save();
    ValueTask<WorkshopCommandResult> Run();
    ValueTask<WorkshopCommandResult> Pause();
    ValueTask<WorkshopCommandResult> ResumeRun();
    ValueTask<WorkshopCommandResult> Configure(ICompiledScene construction, CadenceRevision revision);
    ValueTask<WorkshopCommandResult> Advance();
    ValueTask<WorkshopCommandResult> StepOnce();
    ValueTask<WorkshopCommandResult> Reset();
    WorkshopCommandResult CancelPending();
    void StopSimulation(WorkshopRejection reason);
    ValueTask CompleteRun();
}

public interface ICoreServices
{
    IWorkshopClientCursor CreateClientCursor(RuntimeSessionId session);
    IWorkshopPoseHistory CreatePoseHistory();
    IMemoryServices Memory { get; }
    IWorkshopTurnRegistry CreateTurnRegistry();
    IReliableReadLedger CreateReadLedger(RuntimeSessionId session);
    IWorkshopCommandRouter CreateCommandRouter(Func<WorkshopCommand, Task<byte[]>> execute);
    IWorkshopCommandExecutor CreateCommandExecutor(IWorkshopGpuDevice device, IWorkshopCommandHost host, IWorkshopDiagnostics? diagnostics = null);
    IWorkshopWorkerDevice CreateWorkerDevice(CuriousContraptions.Abstractions.IEventRingPublication publication, PhysicsDocumentId document);
    IWorkshopTrace CreateTrace(IWorkshopDiagnostics? diagnostics = null);
    Task RunTicks(ITickLoopHost host);
    ICoreGeometry Geometry { get; }
    ISupplyQueries Supply { get; }
    IWorkshopUiCurves UiCurves { get; }
    IRunControlServices RunControls { get; }
    CaptureLatch ConsumeCapture(CaptureLatch latch, ResidenceRead candidate);
    uint TimerDuration(float seconds, SimulationCadence cadence);
    IClockServices Clocks { get; }
    IRuntimeCodecs Codecs { get; }
    ISchedulingServices Scheduling { get; }
    IPresentationServices Presentation { get; }
    IWorkshopAnimationTracks CreateAnimationTracks(IAnimationServices animation);
    IAnimationControlDeferral CreateAnimationDeferral(IWorkshopDiagnostics? diagnostics = null);
    IWorkshopDiagnostics CreateDiagnostics(Action<WorkshopDiagnostic>? publish = null);
    IWorkshopSimulation CreateSimulation(IWorkshopGpuDevice device, IWorkshopCaptureClock clock, ICompiledScene scene, CadenceRevision revision,
        IWorkshopInstallation installation, GpuDeviceOwnership deviceOwnership, IWorkshopDiagnostics? diagnostics = null);
    IPhysicsSceneDeclaration CreatePhysicsScene(PhysicsDocumentId document, ulong nextIdentity,
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
        ReadOnlySpan<DrivenSurfaceDeclaration> drivenSurfaces = default);
    IActivationProgram CreateActivation(ReadOnlySpan<ActivationNodeDeclaration> nodes, ReadOnlySpan<ActivationEdge> edges,
        uint substeps = 4, ReadOnlySpan<PassageSensorDeclaration> passageSensors = default,
        ReadOnlySpan<CompiledSignalContact> contacts = default);
    ICompiledScene CreateScene(CompiledSceneId identity, ConstructionRevision revision, WorkshopCadenceSettings settings,
        IPhysicsSceneDeclaration physics, IActivationProgram activation, IEnumerable<CompiledPresentationTrack> presentation,
        IEnumerable<GpuBodyId> owners, IEnumerable<RunControlRow> runControls);
}
