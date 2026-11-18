using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class CannonArtTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task NativeAnnuliAndPrivateCuesUseTheManagedFactoryAndRestoreAuthoredArt()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(new("cannon"));
        var control = registry.Create(new("cannon"));
        await using var harness = await WorkshopIntegrationHarness.Building(CannonIntegrationTests.Vertical());
        try
        {
            godot.Tree.Root.AddChild(part); godot.Tree.Root.AddChild(control);
            part.EnsureConstructed(); control.EnsureConstructed();
            var jacket = part.GetNode<MeshInstance3D>("Visual/Jacket");
            var mesh = Assert.IsType<ArrayMesh>(jacket.Mesh);
            var vertices = mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            // The visible bore must remain open and have a real inner wall; 1 mm is the authored art alignment allowance.
            Assert.Contains(vertices, value => MathF.Abs(new Vector2(value.X, value.Z).Length() - .48f) <= .001f);
            Assert.Contains(vertices, value => MathF.Abs(new Vector2(value.X, value.Z).Length() - .58f) <= .001f);
            Assert.Contains(vertices, value => MathF.Abs(value.Y - .55f) <= .001f);
            Assert.Contains(vertices, value => MathF.Abs(value.Y + .55f) <= .001f);
            Assert.IsType<ArrayMesh>(part.GetNode<MeshInstance3D>("Visual/Sleeve").Mesh);
            var indicator = part.GetNode<MeshInstance3D>("Visual/Indicator");
            var controlMaterial = Assert.IsType<StandardMaterial3D>(control.GetNode<MeshInstance3D>("Visual/Indicator").MaterialOverride);
            var material = Assert.IsType<StandardMaterial3D>(indicator.MaterialOverride);
            Assert.NotSame(material, controlMaterial);
            Assert.Equal(new Color(85f / 255f, 101f / 255f, 115f / 255f), controlMaterial.AlbedoColor);
            var authoredFlag = part.GetNode<Node3D>("Visual/ReadyFlag").Rotation;
            Assert.True(authoredFlag.Z < -1f); // visibly down before a ready payload raises it
            await harness.Run(); await harness.Advance(200);
            Assert.Equal(45f, harness.Committed.EnergyStores[0].Energy.Value);
            Assert.Equal(ChamberPhase.Seated, harness.Committed.ChamberRegions[0].Phase);
            foreach (var sample in harness.Presentation!.Advance(AnimationTestTime.Seconds(200d / 120d)))
                if (sample.Owner == new GpuBodyId(1)) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
            Assert.Equal(1f, part.GetNode<Node3D>("Visual/ChargeBar").Scale.X);
            Assert.Equal(0f, part.GetNode<Node3D>("Visual/ReadyFlag").Rotation.Z);
            Assert.Equal(new Color(247f / 255f, 203f / 255f, 82f / 255f), material.AlbedoColor);
            Assert.Equal(new Color(85f / 255f, 101f / 255f, 115f / 255f), controlMaterial.AlbedoColor);
            await harness.Reset(); part.ResetPhysicalVisuals();
            Assert.Equal(.001f, part.GetNode<Node3D>("Visual/ChargeBar").Scale.X);
            Assert.Equal(authoredFlag, part.GetNode<Node3D>("Visual/ReadyFlag").Rotation);
            Assert.Equal(-.22f, part.GetNode<Node3D>("Visual/Sleeve").Position.X);
            Assert.Equal(new Color(85f / 255f, 101f / 255f, 115f / 255f), material.AlbedoColor);
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part); part.Free();
            control.GetParent()?.RemoveChild(control); control.Free();
        }
    }
}
