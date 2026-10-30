using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>
/// Pre-calculated simulation buffer slot addresses generated at compile time, in the scene's own exactly sized world
/// (<see cref="PhysicsWorldLayout"/>).
/// Eliminates runtime linear searches (BodySlot, MaterialSlot) and dynamic manual byte math in favour
/// of instant O(1) address resolution.
/// </summary>
public sealed class CompiledMemoryLayout
{
    private readonly Dictionary<GpuBodyId, int> _bodySlots = new(16);
    private readonly Dictionary<GpuBodyId, int> _bodyOffsets = new(16);
    private readonly Dictionary<GpuColliderId, int> _colliderSlots = new(32);
    private readonly Dictionary<GpuColliderId, int> _colliderOffsets = new(32);
    private readonly Dictionary<GpuMaterialId, int> _materialSlots = new(32);
    private readonly Dictionary<GpuMaterialId, int> _materialOffsets = new(32);
    private readonly Dictionary<GpuSensorId, int> _sensorSlots = new(4);
    private readonly Dictionary<GpuSensorId, int> _sensorOffsets = new(4);
    private readonly Dictionary<GpuForceId, int> _guideSlots = new(4);
    private readonly Dictionary<GpuForceId, int> _guideOffsets = new(4);
    private readonly Dictionary<GpuContactTriggerId, int> _triggerSlots = new(4);
    private readonly Dictionary<GpuContactTriggerId, int> _triggerOffsets = new(4);
    private readonly Dictionary<GpuContactWorkId, int> _contactWorkSlots = new(4);
    private readonly Dictionary<GpuContactWorkId, int> _contactWorkOffsets = new(4);
    private readonly Dictionary<GpuOrientationSensorId, int> _orientationSensorSlots = new(4);
    private readonly Dictionary<GpuOrientationSensorId, int> _orientationSensorOffsets = new(4);
    private readonly Dictionary<ElectricalSourceId, int> _electricalSourceSlots = new(4);
    private readonly Dictionary<ElectricalSourceId, int> _electricalSourceOffsets = new(4);
    private readonly Dictionary<ulong, int> _prismaticSlots = new(4);
    private readonly Dictionary<ulong, int> _prismaticOffsets = new(4);
    private readonly Dictionary<ulong, int> _revoluteSlots = new(4);
    private readonly Dictionary<ulong, int> _revoluteOffsets = new(4);

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
    public IReadOnlyDictionary<ulong, int> RevoluteSlots => _revoluteSlots;
    public IReadOnlyDictionary<ulong, int> RevoluteOffsets => _revoluteOffsets;

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
    public int GetRevoluteSlot(ulong id) => _revoluteSlots[id];
    public int GetRevoluteOffset(ulong id) => _revoluteOffsets[id];

    public static CompiledMemoryLayout Build(
        PhysicsWorldLayout world,
        ReadOnlySpan<RigidBodyDeclaration> bodies,
        ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<ContactMaterialDeclaration> materials,
        ReadOnlySpan<ResidenceSensorDeclaration> sensors,
        ReadOnlySpan<PlanarGuideDeclaration> guides,
        ReadOnlySpan<ContactTriggerDeclaration> triggers,
        ReadOnlySpan<ContactWorkDeclaration> contactWorks,
        ReadOnlySpan<OrientationSensorDeclaration> orientationSensors,
        ReadOnlySpan<ElectricalSourceDeclaration> electricalSources,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics,
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes = default)
    {
        var layout = new CompiledMemoryLayout();
        for (var i = 0; i < bodies.Length; i++)
        {
            var id = bodies[i].Id;
            layout._bodySlots[id] = i;
            layout._bodyOffsets[id] = world.BodyOffset(i);
        }
        for (var i = 0; i < colliders.Length; i++)
        {
            var id = colliders[i].Id;
            layout._colliderSlots[id] = i;
            layout._colliderOffsets[id] = world.ColliderOffset(i);
        }
        for (var i = 0; i < materials.Length; i++)
        {
            var id = materials[i].Id;
            layout._materialSlots[id] = i;
            layout._materialOffsets[id] = world.MaterialOffset(i);
        }
        for (var i = 0; i < sensors.Length; i++)
        {
            var id = sensors[i].Id;
            layout._sensorSlots[id] = i;
            layout._sensorOffsets[id] = world.SensorOffset(i);
        }
        for (var i = 0; i < guides.Length; i++)
        {
            var id = guides[i].Id;
            layout._guideSlots[id] = i;
            layout._guideOffsets[id] = world.GuideOffset(i);
        }
        for (var i = 0; i < triggers.Length; i++)
        {
            var id = triggers[i].Id;
            layout._triggerSlots[id] = i;
            layout._triggerOffsets[id] = world.TriggerOffset(i);
        }
        for (var i = 0; i < contactWorks.Length; i++)
        {
            var id = contactWorks[i].Id;
            layout._contactWorkSlots[id] = i;
            layout._contactWorkOffsets[id] = world.ContactWorkOffset(i);
        }
        for (var i = 0; i < orientationSensors.Length; i++)
        {
            var id = orientationSensors[i].Id;
            layout._orientationSensorSlots[id] = i;
            layout._orientationSensorOffsets[id] = world.OrientationSensorOffset(i);
        }
        for (var i = 0; i < electricalSources.Length; i++)
        {
            var id = electricalSources[i].Id;
            layout._electricalSourceSlots[id] = i;
            layout._electricalSourceOffsets[id] = world.ElectricalSourceOffset(i);
        }
        for (var i = 0; i < prismatics.Length; i++)
        {
            var id = prismatics[i].Id.Value;
            layout._prismaticSlots[id] = i;
            layout._prismaticOffsets[id] = world.PrismaticOffset(i);
        }
        for (var i = 0; i < revolutes.Length; i++)
        {
            var id = revolutes[i].Id.Value;
            layout._revoluteSlots[id] = i;
            layout._revoluteOffsets[id] = world.RevoluteOffset(i);
        }
        return layout;
    }
}
