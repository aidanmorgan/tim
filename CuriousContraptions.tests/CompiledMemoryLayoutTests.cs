using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The compiled layout addresses every declaration table record by its declaration order: slot i sits at the
/// table's ABI offset plus i records, and the GPU encoder writes each record through exactly that address.</summary>
public sealed class CompiledMemoryLayoutTests
{
    private static PhysicsSceneDeclaration Scene()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 2, 0, 0, 0, 0, 1),
            PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Delay, new(4), 2, 2, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(5), 0, 1, 0, 0, 0, 0, 1));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instances);
        return WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
    }

    [Fact]
    public void EveryBodyAddressIsItsDeclarationIndexInTheBodyTable()
    {
        var scene = Scene();
        Assert.True(scene.Bodies.Length > 1);
        for (var i = 0; i < scene.Bodies.Length; i++)
        {
            Assert.Equal(i, scene.Layout.GetBodySlot(scene.Bodies[i].Id));
            Assert.Equal(PhysicsWorldLayout.BodiesOffset + i * PhysicsGpuAbi.BodyBytes, scene.Layout.GetBodyOffset(scene.Bodies[i].Id));
        }
        Assert.Equal(scene.Bodies.Length, scene.Layout.BodySlots.Count);
    }

    [Fact]
    public void EveryColliderAndMaterialAddressIsItsDeclarationIndex()
    {
        var scene = Scene();
        Assert.True(scene.Colliders.Length > 1);
        for (var i = 0; i < scene.Colliders.Length; i++)
        {
            Assert.Equal(i, scene.Layout.GetColliderSlot(scene.Colliders[i].Id));
            Assert.Equal(scene.World.CollidersOffset + i * PhysicsGpuAbi.ColliderBytes, scene.Layout.GetColliderOffset(scene.Colliders[i].Id));
        }
        for (var i = 0; i < scene.Materials.Length; i++)
        {
            Assert.Equal(i, scene.Layout.GetMaterialSlot(scene.Materials[i].Id));
            Assert.Equal(scene.World.MaterialsOffset + i * PhysicsGpuAbi.MaterialBytes, scene.Layout.GetMaterialOffset(scene.Materials[i].Id));
        }
        Assert.Equal(scene.Colliders.Length, scene.Layout.ColliderSlots.Count);
        Assert.Equal(scene.Materials.Length, scene.Layout.MaterialSlots.Count);
    }

    [Fact]
    public void EveryContactTriggerAddressIsItsDeclarationIndex()
    {
        var scene = Scene();
        Assert.Equal(2, scene.Triggers.Length);
        for (var i = 0; i < scene.Triggers.Length; i++)
        {
            Assert.Equal(i, scene.Layout.GetTriggerSlot(scene.Triggers[i].Id));
            Assert.Equal(scene.World.TriggersOffset + i * PhysicsGpuAbi.TriggerBytes, scene.Layout.GetTriggerOffset(scene.Triggers[i].Id));
        }
    }

    [Fact]
    public void AnUndeclaredIdentityHasNoAddress()
    {
        var scene = Scene();
        Assert.Throws<KeyNotFoundException>(() => scene.Layout.GetBodySlot(new GpuBodyId(999)));
    }
}
