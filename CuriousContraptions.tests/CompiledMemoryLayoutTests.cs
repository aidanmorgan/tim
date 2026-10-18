using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class CompiledMemoryLayoutTests
{
    [Fact]
    public void CompiledMemoryLayout_Build_MapsSlotsAndOffsetsAccurately()
    {
        var bodyId1 = new GpuBodyId(101);
        var bodyId2 = new GpuBodyId(102);
        var colliderId = new GpuColliderId(201);
        var materialId = new GpuMaterialId(301);
        var sensorId = new GpuSensorId(401);
        var guideId = new GpuForceId(501);
        var triggerId = new GpuContactTriggerId(601);
        var contactWorkId = new GpuContactWorkId(701);
        var orientationSensorId = new GpuOrientationSensorId(801);
        var electricalSourceId = new ElectricalSourceId(901);
        var prismaticId = new PrismaticConstraintId(1001);

        var bodies = new[]
        {
            new RigidBodyDeclaration(bodyId1, GpuBodyType.Dynamic, 1.0f, 0.5f, System.Numerics.Vector2.Zero, 0f),
            new RigidBodyDeclaration(bodyId2, GpuBodyType.Static, 0.0f, 0.0f, System.Numerics.Vector2.One, 0f),
        };
        var colliders = new[]
        {
            new ColliderDeclaration(colliderId, bodyId1, materialId, GpuShapeType.Circle, new(1f, 1f), System.Numerics.Vector2.Zero, 0f, 0u, 0u)
        };
        var materials = new[]
        {
            new ContactMaterialDeclaration(materialId, 0.2f, 0.8f)
        };
        var sensors = new[]
        {
            new ResidenceSensorDeclaration(sensorId, bodyId1, 1u, 0u)
        };
        var guides = new[]
        {
            new PlanarGuideDeclaration(guideId, bodyId1, System.Numerics.Vector2.UnitX, 0f, 10f)
        };
        var triggers = new[]
        {
            new ContactTriggerDeclaration(triggerId, colliderId, 1u, 0.5f)
        };
        var contactWorks = new[]
        {
            new ContactWorkDeclaration(contactWorkId, colliderId, 1u, 1.0f)
        };
        var orientationSensors = new[]
        {
            new OrientationSensorDeclaration(orientationSensorId, bodyId1, 0f, 0.1f)
        };
        var electricalSources = new[]
        {
            new ElectricalSourceDeclaration(electricalSourceId, bodyId1, 12f)
        };
        var prismatics = new[]
        {
            new PrismaticConstraintDeclaration(prismaticId, bodyId1, bodyId2, System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero, System.Numerics.Vector2.UnitY, 0f, 5f)
        };

        var layout = CompiledMemoryLayout.Build(
            bodies,
            colliders,
            materials,
            sensors,
            guides,
            triggers,
            contactWorks,
            orientationSensors,
            electricalSources,
            prismatics);

        Assert.Equal(0, layout.GetBodySlot(bodyId1));
        Assert.Equal(1, layout.GetBodySlot(bodyId2));
        Assert.Equal(PhysicsGpuAbi.BodiesOffset, layout.GetBodyOffset(bodyId1));
        Assert.Equal(PhysicsGpuAbi.BodiesOffset + PhysicsGpuAbi.BodyBytes, layout.GetBodyOffset(bodyId2));

        Assert.Equal(0, layout.GetColliderSlot(colliderId));
        Assert.Equal(PhysicsGpuAbi.CollidersOffset, layout.GetColliderOffset(colliderId));

        Assert.Equal(0, layout.GetMaterialSlot(materialId));
        Assert.Equal(PhysicsGpuAbi.MaterialsOffset, layout.GetMaterialOffset(materialId));

        Assert.Equal(0, layout.GetSensorSlot(sensorId));
        Assert.Equal(PhysicsGpuAbi.SensorsOffset, layout.GetSensorOffset(sensorId));

        Assert.Equal(0, layout.GetGuideSlot(guideId));
        Assert.Equal(PhysicsGpuAbi.GuidesOffset, layout.GetGuideOffset(guideId));

        Assert.Equal(0, layout.GetTriggerSlot(triggerId));
        Assert.Equal(PhysicsGpuAbi.TriggersOffset, layout.GetTriggerOffset(triggerId));

        Assert.Equal(0, layout.GetContactWorkSlot(contactWorkId));
        Assert.Equal(PhysicsGpuAbi.ContactWorksOffset, layout.GetContactWorkOffset(contactWorkId));

        Assert.Equal(0, layout.GetOrientationSensorSlot(orientationSensorId));
        Assert.Equal(PhysicsGpuAbi.OrientationSensorsOffset, layout.GetOrientationSensorOffset(orientationSensorId));

        Assert.Equal(0, layout.GetElectricalSourceSlot(electricalSourceId));
        Assert.Equal(PhysicsGpuAbi.ElectricalSourcesOffset, layout.GetElectricalSourceOffset(electricalSourceId));

        Assert.Equal(0, layout.GetPrismaticSlot(prismaticId.Value));
        Assert.Equal(PhysicsGpuAbi.PrismaticRecordsOffset, layout.GetPrismaticOffset(prismaticId.Value));
    }

    [Fact]
    public void PhysicsSceneDeclaration_ConstructsAndExposes_CompiledMemoryLayout()
    {
        var bodyId = new GpuBodyId(42);
        var bodies = new[]
        {
            new RigidBodyDeclaration(bodyId, GpuBodyType.Dynamic, 2.0f, 1.0f, System.Numerics.Vector2.Zero, 0f)
        };

        var scene = new PhysicsSceneDeclaration(
            Bodies: bodies,
            Colliders: Array.Empty<ColliderDeclaration>(),
            Materials: Array.Empty<ContactMaterialDeclaration>(),
            Sensors: Array.Empty<ResidenceSensorDeclaration>(),
            Guides: Array.Empty<PlanarGuideDeclaration>(),
            Triggers: Array.Empty<ContactTriggerDeclaration>(),
            ContactWorks: Array.Empty<ContactWorkDeclaration>(),
            OrientationSensors: Array.Empty<OrientationSensorDeclaration>(),
            ElectricalSources: Array.Empty<ElectricalSourceDeclaration>(),
            Prismatics: Array.Empty<PrismaticConstraintDeclaration>());

        Assert.NotNull(scene.Layout);
        Assert.Equal(0, scene.Layout.GetBodySlot(bodyId));
        Assert.Equal(PhysicsGpuAbi.BodiesOffset, scene.Layout.GetBodyOffset(bodyId));
    }
}
