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

    /// <summary>Part B2 (owner decision 24i): the seam is printed flush on the felt with no collider, as owner decision 11 rules for the
    /// planned Pool ball's stripe (EL-098), and the ball's only solid is its sphere. At the default radius every seam vertex lies between
    /// the felt's radius and 0.5 mm beyond it, the bound FaceMarkingTests uses for print: a raised seam a player would read as solid, or
    /// one buried in the felt, fails. The band is open, drawn in the scene's cream at its authored 8 mm width on the great circle turned
    /// 45° about X, and no facet between its vertex columns dips inside the felt's sphere. The compiled ball has exactly one collider,
    /// its sphere, at the default radius and at both declared bounds, so the seam adds no contact.</summary>
    [Fact]
    public void TheSeamIsPrintedFlushOnTheFeltAndTheBallHasOnlyItsSphereCollider()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(new("tennis"));
        try
        {
            godot.Tree.Root.AddChild(part);
            part.EnsureConstructed();
            var felt = part.GetNode<MeshInstance3D>("Visual/Felt");
            var seam = part.GetNode<MeshInstance3D>("Visual/Seam");
            var toRoot = part.GlobalTransform.AffineInverse();
            var feltPose = toRoot * felt.GlobalTransform;
            var r = Assert.IsType<SphereMesh>(felt.Mesh).Radius * feltPose.Basis.Scale.X;
            var seamVertices = VisibleSolidColliderTests.VerticesInRoot(part, seam);
            const float print = .0005f;
            Assert.All(seamVertices, v => Assert.InRange(v.DistanceTo(feltPose.Origin), r, r + print));
            var band = Assert.IsType<CylinderMesh>(seam.Mesh);
            Assert.False(band.CapTop || band.CapBottom, "the seam band is capped");
            Assert.Equal(new Color(232f / 255f, 220f / 255f, 203f / 255f),
                Assert.IsType<StandardMaterial3D>(seam.GetActiveMaterial(0)).AlbedoColor);
            var bandPose = toRoot * seam.GlobalTransform;
            Assert.True(MathF.Min(band.TopRadius, band.BottomRadius) * bandPose.Basis.Scale.X * MathF.Cos(MathF.PI / band.RadialSegments) >= r,
                "a seam facet dips inside the felt");
            // Within the print bound: a missing band is 8 mm short of its authored width, and a seam drawn off its authored great circle
            // (45° about X), a visibly different seam line, lifts its rim millimetres from that circle's plane (about 4 mm at 1°).
            var width = band.Height * bandPose.Basis.Scale.Y;
            Assert.InRange(width, .008f - print, .008f + print);
            var normal = new Vector3(0f, MathF.Cos(MathF.PI / 4), MathF.Sin(MathF.PI / 4));
            Assert.All(seamVertices, v => Assert.InRange((v - feltPose.Origin).Dot(normal), -width / 2 - print, width / 2 + print));
            foreach (var radius in new[] { .25f, 1f / 16f, 16f })
            {
                var instance = TennisBallTests.Ball(y: 20, radius: radius);
                var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
                    TennisBallTests.Scene(instance), new(1, 2));
                var collider = Assert.Single(scene.Colliders.ToArray(), c => c.Body == instance.Id);
                Assert.Equal(ColliderShapeKind.Sphere, collider.Shape);
                Assert.Equal(radius, collider.Radius.Value);
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part); part.Free();
        }
    }
}
