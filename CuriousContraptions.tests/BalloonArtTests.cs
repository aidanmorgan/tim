using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class BalloonArtTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task NativeEnvelopeAndNeckFollowCommittedRadiusWithAnIndependentDryCarrierAndCenterTie()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(new("balloon"));
        try
        {
            godot.Tree.Root.AddChild(part); part.EnsureConstructed();
            var envelope = part.GetNode<MeshInstance3D>("Envelope");
            var sphere = Assert.IsType<SphereMesh>(envelope.Mesh);
            Assert.Equal(1f, sphere.Radius);
            Assert.Equal(new Color(237f / 255f, 99f / 255f, 120f / 255f),
                Assert.IsType<StandardMaterial3D>(sphere.SurfaceGetMaterial(0)).AlbedoColor);
            Assert.Equal(Vector3.Zero, part.GetNode<Node3D>("Loc_Port_Tie").Position);
            Assert.NotNull(part.GetNode<MeshInstance3D>("String").Mesh);
            await using var h = await WorkshopIntegrationHarness.Running(
                TennisBallTests.Scene(BalloonTests.Balloon(y: 5, buoyancy: 0, mass: 2)));
            await h.Advance(360);
            Assert.Equal(GasInventoryPhase.Empty, h.Committed.GasStores[0].State.Phase);
            foreach (var sample in h.Presentation!.Advance(AnimationTestTime.Seconds(3)))
                if (sample.Owner == new GpuBodyId(1)) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
            Assert.Equal(.0625f, envelope.Scale.X);
            Assert.Equal(-.0625f, part.GetNode<Node3D>("Neck").Position.Y);
            Assert.Equal(Vector3.One, part.GetNode<Node3D>("Carrier").Scale);
            Assert.Equal(Vector3.Zero, part.GetNode<Node3D>("Loc_Port_Tie").Position);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }
}
