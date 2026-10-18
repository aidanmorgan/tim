using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>
/// Pre-calculated fixed-offset simulation buffer layout and slot addresses generated at compile time.
/// Eliminates runtime linear searches (BodySlot, MaterialSlot) and dynamic manual byte math in favour
/// of instant O(1) address resolution.
/// </summary>
public sealed class CompiledMemoryLayout
{
    private readonly Dictionary<GpuBodyId, int> _bodySlots = [];
    private readonly Dictionary<GpuBodyId, int> _bodyOffsets = [];
    private readonly Dictionary<GpuColliderId, int> _colliderSlots = [];
    private readonly Dictionary<GpuColliderId, int> _colliderOffsets = [];
    private readonly Dictionary<GpuMaterialId, int> _materialSlots = [];
    private readonly Dictionary<GpuMaterialId, int> _materialOffsets = [];
    private readonly Dictionary<GpuSensorId, int> _sensorSlots = [];
    private readonly Dictionary<GpuSensorId, int> _sensorOffsets = [];
    private readonly Dictionary<GpuForceId, int> _guideSlots = [];
    private readonly Dictionary<GpuForceId, int> _guideOffsets = [];
    private readonly Dictionary<GpuContactTriggerId, int> _triggerSlots = [];
    private readonly Dictionary<GpuContactTriggerId, int> _triggerOffsets = [];
    private readonly Dictionary<GpuContactWorkId, int> _contactWorkSlots = [];
    private readonly Dictionary<GpuContactWorkId, int> _contactWorkOffsets = [];
    private readonly Dictionary<GpuOrientationSensorId, int> _orientationSensorSlots = [];
    private readonly Dictionary<GpuOrientationSensorId, int> _orientationSensorOffsets = [];
    private readonly Dictionary<ElectricalSourceId, int> _electricalSourceSlots = [];
    private readonly Dictionary<ElectricalSourceId, int> _electricalSourceOffsets = [];
    private readonly Dictionary<ulong, int> _prismaticSlots = [];
    private readonly Dictionary<ulong, int> _prismaticOffsets = [];

    public IReadOnlyDictionary<GpuBodyId, int> BodySlots => _bodySlots;
    public IReadOnlyDictionary<GpuBodyId, int> BodyOffsets => _bodyOffsets;
    public IReadOnlyDictionary<GpuColliderId, int> ColliderSlots => _colliderSlots;
    public IReadOnlyDictionary<GpuColliderId, int> ColliderOffsets => _colliderOffsets;
    public IReadOnlyDictionary<GpuMaterialId, int> MaterialSlots => _materialSlots;
    public IReadOnlyDictionary<GpuMaterialId, int> MaterialOffsets => _materialOffsets;
    public IReadOnlyDictionary<GpuSensorId, int> SensorSlots => _sensorSlots;
    public IReadOnlyDictionary<GpuSensorId, int> SensorOffsets => _sensorOffsets;
    public IReadOnlyDictionary<GpuForceId, int> GuideSlots => _guideSlots;
    public IReadOnlyDictionary<GpuForceId, int> GuideOffsets => _guideOffsets;
    public IReadOnlyDictionary<GpuContactTriggerId, int> TriggerSlots => _triggerSlots;
    public IReadOnlyDictionary<GpuContactTriggerId, int> TriggerOffsets => _triggerOffsets;
    public IReadOnlyDictionary<GpuContactWorkId, int> ContactWorkSlots => _contactWorkSlots;
    public IReadOnlyDictionary<GpuContactWorkId, int> ContactWorkOffsets => _contactWorkOffsets;
    public IReadOnlyDictionary<GpuOrientationSensorId, int> OrientationSensorSlots => _orientationSensorSlots;
    public IReadOnlyDictionary<GpuOrientationSensorId, int> OrientationSensorOffsets => _orientationSensorOffsets;
    public IReadOnlyDictionary<ElectricalSourceId, int> ElectricalSourceSlots => _electricalSourceSlots;
    public IReadOnlyDictionary<ElectricalSourceId, int> ElectricalSourceOffsets => _electricalSourceOffsets;
    public IReadOnlyDictionary<ulong, int> PrismaticSlots => _prismaticSlots;
    public IReadOnlyDictionary<ulong, int> PrismaticOffsets => _prismaticOffsets;

    public int GetBodySlot(GpuBodyId id) => _bodySlots[id];
    public int GetBodyOffset(GpuBodyId id) => _bodyOffsets[id];
    public int GetColliderSlot(GpuColliderId id) => _colliderSlots[id];
    public int GetColliderOffset(GpuColliderId id) => _colliderOffsets[id];
    public int GetMaterialSlot(GpuMaterialId id) => _materialSlots[id];
    public int GetMaterialOffset(GpuMaterialId id) => _materialOffsets[id];
    public int GetSensorSlot(GpuSensorId id) => _sensorSlots[id];
    public int GetSensorOffset(GpuSensorId id) => _sensorOffsets[id];
    public int GetGuideSlot(GpuForceId id) => _guideSlots[id];
    public int GetGuideOffset(GpuForceId id) => _guideOffsets[id];
    public int GetTriggerSlot(GpuContactTriggerId id) => _triggerSlots[id];
    public int GetTriggerOffset(GpuContactTriggerId id) => _triggerOffsets[id];
    public int GetContactWorkSlot(GpuContactWorkId id) => _contactWorkSlots[id];
    public int GetContactWorkOffset(GpuContactWorkId id) => _contactWorkOffsets[id];
    public int GetOrientationSensorSlot(GpuOrientationSensorId id) => _orientationSensorSlots[id];
    public int GetOrientationSensorOffset(GpuOrientationSensorId id) => _orientationSensorOffsets[id];
    public int GetElectricalSourceSlot(ElectricalSourceId id) => _electricalSourceSlots[id];
    public int GetElectricalSourceOffset(ElectricalSourceId id) => _electricalSourceOffsets[id];
    public int GetPrismaticSlot(ulong id) => _prismaticSlots[id];
    public int GetPrismaticOffset(ulong id) => _prismaticOffsets[id];

    public static CompiledMemoryLayout Build(
        ReadOnlySpan<RigidBodyDeclaration> bodies,
        ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<ContactMaterialDeclaration> materials,
        ReadOnlySpan<ResidenceSensorDeclaration> sensors,
        ReadOnlySpan<PlanarGuideDeclaration> guides,
        ReadOnlySpan<ContactTriggerDeclaration> triggers,
        ReadOnlySpan<ContactWorkDeclaration> contactWorks,
        ReadOnlySpan<OrientationSensorDeclaration> orientationSensors,
        ReadOnlySpan<ElectricalSourceDeclaration> electricalSources,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics)
    {
        var layout = new CompiledMemoryLayout();
        for (var i = 0; i < bodies.Length; i++)
        {
            var id = bodies[i].Id;
            layout._bodySlots[id] = i;
            layout._bodyOffsets[id] = PhysicsGpuAbi.BodiesOffset + i * PhysicsGpuAbi.BodyBytes;
        }
        for (var i = 0; i < colliders.Length; i++)
        {
            var id = colliders[i].Id;
            layout._colliderSlots[id] = i;
            layout._colliderOffsets[id] = PhysicsGpuAbi.CollidersOffset + i * PhysicsGpuAbi.ColliderBytes;
        }
        for (var i = 0; i < materials.Length; i++)
        {
            var id = materials[i].Id;
            layout._materialSlots[id] = i;
            layout._materialOffsets[id] = PhysicsGpuAbi.MaterialsOffset + i * PhysicsGpuAbi.MaterialBytes;
        }
        for (var i = 0; i < sensors.Length; i++)
        {
            var id = sensors[i].Id;
            layout._sensorSlots[id] = i;
            layout._sensorOffsets[id] = PhysicsGpuAbi.SensorsOffset + i * PhysicsGpuAbi.SensorBytes;
        }
        for (var i = 0; i < guides.Length; i++)
        {
            var id = guides[i].Id;
            layout._guideSlots[id] = i;
            layout._guideOffsets[id] = PhysicsGpuAbi.GuidesOffset + i * PhysicsGpuAbi.GuideBytes;
        }
        for (var i = 0; i < triggers.Length; i++)
        {
            var id = triggers[i].Id;
            layout._triggerSlots[id] = i;
            layout._triggerOffsets[id] = PhysicsGpuAbi.TriggersOffset + i * PhysicsGpuAbi.TriggerBytes;
        }
        for (var i = 0; i < contactWorks.Length; i++)
        {
            var id = contactWorks[i].Id;
            layout._contactWorkSlots[id] = i;
            layout._contactWorkOffsets[id] = PhysicsGpuAbi.ContactWorksOffset + i * PhysicsGpuAbi.ContactWorkBytes;
        }
        for (var i = 0; i < orientationSensors.Length; i++)
        {
            var id = orientationSensors[i].Id;
            layout._orientationSensorSlots[id] = i;
            layout._orientationSensorOffsets[id] = PhysicsGpuAbi.OrientationSensorsOffset + i * PhysicsGpuAbi.OrientationSensorBytes;
        }
        for (var i = 0; i < electricalSources.Length; i++)
        {
            var id = electricalSources[i].Id;
            layout._electricalSourceSlots[id] = i;
            layout._electricalSourceOffsets[id] = PhysicsGpuAbi.ElectricalSourcesOffset + i * PhysicsGpuAbi.ElectricalSourceBytes;
        }
        for (var i = 0; i < prismatics.Length; i++)
        {
            var id = prismatics[i].Id.Value;
            layout._prismaticSlots[id] = i;
            layout._prismaticOffsets[id] = PhysicsGpuAbi.PrismaticRecordsOffset + i * PhysicsGpuAbi.PrismaticBytes;
        }
        return layout;
    }
}
