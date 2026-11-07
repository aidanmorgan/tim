using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public interface IPhysicsSceneDeclaration
{
    ReadOnlySpan<TensileSpanDeclaration> TensileSpans { get; }
    ReadOnlySpan<ContactLoadSensorDeclaration> ContactLoadSensors { get; }
    ReadOnlySpan<PrismaticConstraintDeclaration> Prismatics { get; }
    ReadOnlySpan<RevoluteConstraintDeclaration> Revolutes { get; }
    ReadOnlySpan<CompliantSurfaceDeclaration> CompliantSurfaces { get; }
    ReadOnlySpan<PrismaticDriveDeclaration> PrismaticDrives { get; }
    ReadOnlySpan<RevoluteDriveDeclaration> RevoluteDrives { get; }
    ReadOnlySpan<RopeGuideDeclaration> RopeGuides { get; }
    ReadOnlySpan<MechanicalTransmissionDeclaration> Transmissions { get; }
    ReadOnlySpan<DrivenSurfaceDeclaration> DrivenSurfaces { get; }
    ICompiledSupplyGraph SupplyGraph { get; }
    IElectricalSupplyPlan Electrical { get; }
    ICompiledMemoryLayout Layout { get; }
    IPhysicsWorldLayout World { get; }
    PhysicsDocumentId Document { get; }
    ulong NextIdentity { get; }
    ReadOnlySpan<RigidBodyDeclaration> Bodies { get; }
    ReadOnlySpan<ColliderDeclaration> Colliders { get; }
    ReadOnlySpan<ContactMaterialDeclaration> Materials { get; }
    ReadOnlySpan<ResidenceSensorDeclaration> Sensors { get; }
    ReadOnlySpan<PlanarGuideDeclaration> Guides { get; }
    ReadOnlySpan<ContactTriggerDeclaration> Triggers { get; }
    ReadOnlySpan<ContactWorkDeclaration> ContactWorks { get; }
    ReadOnlySpan<OrientationSensorDeclaration> OrientationSensors { get; }
}

public interface ICompiledMemoryLayout
{
    IReadOnlyDictionary<GpuBodyId, int> BodySlots { get; }
    IReadOnlyDictionary<GpuBodyId, int> BodyOffsets { get; }
    IReadOnlyDictionary<GpuColliderId, int> ColliderSlots { get; }
    IReadOnlyDictionary<GpuColliderId, int> ColliderOffsets { get; }
    IReadOnlyDictionary<GpuMaterialId, int> MaterialSlots { get; }
    IReadOnlyDictionary<GpuMaterialId, int> MaterialOffsets { get; }
    IReadOnlyDictionary<GpuSensorId, int> SensorSlots { get; }
    IReadOnlyDictionary<GpuSensorId, int> SensorOffsets { get; }
    IReadOnlyDictionary<GpuForceId, int> GuideSlots { get; }
    IReadOnlyDictionary<GpuForceId, int> GuideOffsets { get; }
    IReadOnlyDictionary<GpuContactTriggerId, int> TriggerSlots { get; }
    IReadOnlyDictionary<GpuContactTriggerId, int> TriggerOffsets { get; }
    IReadOnlyDictionary<GpuContactWorkId, int> ContactWorkSlots { get; }
    IReadOnlyDictionary<GpuContactWorkId, int> ContactWorkOffsets { get; }
    IReadOnlyDictionary<GpuOrientationSensorId, int> OrientationSensorSlots { get; }
    IReadOnlyDictionary<GpuOrientationSensorId, int> OrientationSensorOffsets { get; }
    IReadOnlyDictionary<ElectricalSourceId, int> ElectricalSourceSlots { get; }
    IReadOnlyDictionary<ElectricalSourceId, int> ElectricalSourceOffsets { get; }
    IReadOnlyDictionary<ulong, int> PrismaticSlots { get; }
    IReadOnlyDictionary<ulong, int> PrismaticOffsets { get; }
    IReadOnlyDictionary<ulong, int> RevoluteSlots { get; }
    IReadOnlyDictionary<ulong, int> RevoluteOffsets { get; }
    IReadOnlyDictionary<ulong, int> CompliantSurfaceSlots { get; }
    IReadOnlyDictionary<ulong, int> CompliantSurfaceOffsets { get; }
    int GetBodySlot(GpuBodyId id);
    int GetBodyOffset(GpuBodyId id);
    int GetColliderSlot(GpuColliderId id);
    int GetColliderOffset(GpuColliderId id);
    int GetMaterialSlot(GpuMaterialId id);
    int GetMaterialOffset(GpuMaterialId id);
    int GetSensorSlot(GpuSensorId id);
    int GetSensorOffset(GpuSensorId id);
    int GetGuideSlot(GpuForceId id);
    int GetGuideOffset(GpuForceId id);
    int GetTriggerSlot(GpuContactTriggerId id);
    int GetTriggerOffset(GpuContactTriggerId id);
    int GetContactWorkSlot(GpuContactWorkId id);
    int GetContactWorkOffset(GpuContactWorkId id);
    int GetOrientationSensorSlot(GpuOrientationSensorId id);
    int GetOrientationSensorOffset(GpuOrientationSensorId id);
    int GetElectricalSourceSlot(ElectricalSourceId id);
    int GetElectricalSourceOffset(ElectricalSourceId id);
    int GetPrismaticSlot(ulong id);
    int GetPrismaticOffset(ulong id);
    int GetRevoluteSlot(ulong id);
    int GetRevoluteOffset(ulong id);
    int GetCompliantSurfaceSlot(ulong id);
    int GetCompliantSurfaceOffset(ulong id);
}

public interface IPhysicsWorldLayout
{
    int Bodies { get; }
    int Colliders { get; }
    int Materials { get; }
    int Sensors { get; }
    int Dynamic { get; }
    int Guides { get; }
    int Triggers { get; }
    int ContactWorks { get; }
    int OrientationSensors { get; }
    int ElectricalSources { get; }
    int ElectricalBindings { get; }
    int Prismatics { get; }
    int ContactLoads { get; }
    int TensileSpans { get; }
    int Revolutes { get; }
    int CompliantSurfaces { get; }
    int MaterialsOffset { get; }
    int SensorsOffset { get; }
    int GuidesOffset { get; }
    int TriggersOffset { get; }
    int ContactWorksOffset { get; }
    int OrientationSensorsOffset { get; }
    int MotionOffset { get; }
    int MotionBytes { get; }
    int ElectricalSourcesOffset { get; }
    int ElectricalBindingsOffset { get; }
    int PrismaticsOffset { get; }
    int ContactLoadsOffset { get; }
    int TensileSpansOffset { get; }
    int RevoluteJointsOffset { get; }
    int CompliantSurfacesOffset { get; }
    int CompliantEngagementsOffset { get; }
    int CollidersOffset { get; }
    int ByteLength { get; }
    int BodyOffset(int slot);
    int MaterialOffset(int slot);
    int SensorOffset(int slot);
    int GuideOffset(int slot);
    int TriggerOffset(int slot);
    int ContactWorkOffset(int slot);
    int OrientationSensorOffset(int slot);
    int ElectricalSourceOffset(int slot);
    int ElectricalBindingOffset(int slot);
    int PrismaticOffset(int slot);
    int ContactLoadOffset(int slot);
    int ColliderOffset(int slot);
    int TensileSpanOffset(int slot);
    int RevoluteOffset(int slot);
    int CompliantSurfaceOffset(int slot);
    int CompliantEngagementOffset(int surface, int dynamicIndex);
    void WriteCounts(Span<byte> header);
}

public interface IElectricalSupplyPlan
{
    ReadOnlySpan<ElectricalSourceDeclaration> Sources { get; }
    ReadOnlySpan<ElectricalStorageBinding> Bindings { get; }
}

public interface ICompiledSupplyGraph
{
    ReadOnlySpan<SupplyNodeDeclaration> Nodes { get; }
    ReadOnlySpan<SupplyEdge> Edges { get; }
    int DriveSlots { get; }
    ElectricalLogicRead[] NeutralReads();
    SupplyResolution Resolve(WorkshopTickControls controls, Func<SupplyNodeId, bool> contactClosed, PhysicsElectricalRead electrical);
}

public interface IActivationProgram
{
    uint Substeps { get; }
    IEnumerable<CompiledSignalContact> Contacts { get; }
    ReadOnlySpan<ActivationNodeDeclaration> Nodes { get; }
    ReadOnlySpan<ActivationEdge> Edges { get; }
    ReadOnlySpan<PassageSensorDeclaration> PassageSensors { get; }
    void ValidateCommittedMemory(PhysicsMemoryRead memory, SimulationTick tick, IPhysicsSceneDeclaration scene);
    PhysicsActivationRead Clear();
    PhysicsTimerRead ClearTimers();
    PhysicsMemoryRead ClearMemory();
    ActivationMemory Memory(PhysicsMemoryRead values, ActivationNodeId node);
    uint GetCounterTarget(ActivationNodeId node);
    SimulationLatchPhase StateLatchPhase(PhysicsActivationRead values, ActivationNodeId node);
    bool ContactClosed(PhysicsActivationRead values, PhysicsTimerRead timers, SupplyNodeId id);
    ActivationCheckpoint Consume(PhysicsActivationRead committed, PhysicsTimerRead committedTimers,
        ReadOnlySpan<ContactTriggerRead> events, SimulationTick boundary, ReadOnlySpan<OrientationSensorRead> orientations,
        ReadOnlySpan<PassageSensorRead> passages = default, SupplyOutputRead supply = default,
        PhysicsMemoryRead memory = default, ReadOnlySpan<ContactLoadSensorRead> contactLoads = default);
    IActivationSchedule AdvanceScheduled(ActivationCheckpoint committed, SimulationTick boundary);
    ActivationCheckpoint ConsumeScheduled(IActivationSchedule scheduled, ReadOnlySpan<ContactTriggerRead> events,
        SimulationTick boundary, ReadOnlySpan<OrientationSensorRead> orientations, ReadOnlySpan<PassageSensorRead> passages,
        SupplyOutputRead supply, ReadOnlySpan<ContactLoadSensorRead> contactLoads);
    void ValidateMemory(PhysicsMemoryRead memory);
    void ValidateOrigins(PhysicsActivationRead values, PhysicsTimerRead timers, SimulationTick tick, IPhysicsSceneDeclaration scene);
    void ValidateRead(PhysicsActivationRead values, PhysicsTimerRead timers, SimulationTick tick,
        uint substeps, IPhysicsSceneDeclaration scene);
}

public interface IActivationSchedule
{
    ActivationCheckpoint State { get; }
    ReadOnlySpan<ActivationOccurrence> Deferred { get; }
}

public interface ICompiledScene
{
    CompiledSceneId Identity { get; }
    ConstructionRevision Revision { get; }
    WorkshopCadenceSettings Settings { get; }
    IPhysicsSceneDeclaration Physics { get; }
    IActivationProgram Activation { get; }
    ImmutableArray<CompiledPresentationTrack> Presentation { get; }
    ImmutableArray<GpuBodyId> Owners { get; }
    ImmutableArray<RunControlRow> RunControls { get; }
    bool TryRunControl(RunControlId id, out RunControlRow row);
    bool OwnsCapture(CapturePairKey pair);
    void Validate();
}
