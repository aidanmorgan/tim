using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class TennisArtTests(NativeSceneFixture godot)
{
    [Fact]
    public void NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(new("tennis"));
        var control = registry.Create(new("tennis"));
        try
        {
            godot.Tree.Root.AddChild(part); godot.Tree.Root.AddChild(control);
            part.EnsureConstructed(); control.EnsureConstructed();
            var felt = part.GetNode<MeshInstance3D>("Visual/Felt");
            var sphere = Assert.IsType<SphereMesh>(felt.Mesh);
            Assert.Equal(.25f, sphere.Radius);
            var material = Assert.IsType<StandardMaterial3D>(sphere.SurfaceGetMaterial(0));
            Assert.Equal(new Color(184f / 255f, 219f / 255f, 89f / 255f), material.AlbedoColor);
            Assert.NotNull(part.GetNode<MeshInstance3D>("Visual/Seam").Mesh);
            foreach (var radius in new[] { .125f, .5f, 16f })
            {
                var instance = TennisBallTests.Ball(y: 20, radius: radius);
                part.ApplyPresentationParameters(instance.Parameters.ToDictionary(p => p.Key.Value, p => p.Value));
                var visual = part.GetNode<Node3D>("Visual");
                Assert.Equal(new Vector3(radius / .25f, radius / .25f, radius / .25f), visual.Scale);
                var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
                    TennisBallTests.Scene(instance), new(1, 2));
                Assert.Equal(radius, Assert.Single(scene.Colliders.ToArray(), c => c.Body == instance.Id).Radius.Value);
                Assert.Equal(radius, sphere.Radius * visual.Scale.X);
                Assert.Equal(Vector3.One, control.GetNode<Node3D>("Visual").Scale);
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part); part.Free();
            control.GetParent()?.RemoveChild(control); control.Free();
        }
    }
}
