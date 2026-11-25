using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class LinearPusherArtTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task NativeHeadSpanAndIndicatorFollowCommittedMotionAndRestoreOnReset()
    {
        var construction = LinearPusherTests.Machine(true, false, false);
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var root = new Node3D();
        var part = registry.Create(new("linear_pusher"));
        part.AuthoredId = new(1);
        part.Position = new(0, 3, 0);
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var identities = construction.Instances.Identities;
        var headId = identities[new(1)].Sliders[0].Body;
        var physical = new WorkshopPhysicalPresentation();
        try
        {
            godot.Tree.Root.AddChild(root);
            root.AddChild(part);
            part.EnsureConstructed();
            part.InstallPhysicalIdentity(new(1), headId, identities);
            var head = part.GetNode<Node3D>("Visual/Head");
            var rod = part.GetNode<MeshInstance3D>("Visual/Rod");
            var indicator = part.GetNode<MeshInstance3D>("Visual/Indicator");
            void Observe(Color expected)
            {
                foreach (var sample in harness.Presentation!.Advance(AnimationTestTime.Seconds(harness.Committed.Tick.Value / 120.0)))
                    if (sample.Owner == new GpuBodyId(1)) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
                Assert.True(harness.Committed.Bodies.TryGet(headId, out var body));
                physical.Apply([part], root.GlobalTransform, new PresentedBodySet([
                    new(headId, body.Body.Cell, body.Body.Local, body.Rotation)]));
                var lower = rod.Transform * new Vector3(0, -.125f, 0);
                var upper = rod.Transform * new Vector3(0, .125f, 0);
                Assert.InRange(lower.DistanceTo(new(.6f, 0, 0)), 0f, .001f);
                Assert.InRange(upper.DistanceTo(head.Position), 0f, .001f); // At most 1 mm gap between authored rod and committed head.
                var actual = Assert.IsType<StandardMaterial3D>(indicator.MaterialOverride).AlbedoColor;
                Assert.InRange(MathF.Abs(actual.R - expected.R), 0f, .01f);
                Assert.InRange(MathF.Abs(actual.G - expected.G), 0f, .01f);
                Assert.InRange(MathF.Abs(actual.B - expected.B), 0f, .01f); // Visible palette channels, not shader byte equality.
            }
            var slate = new Color(.333f, .396f, .451f);
            var cyan = new Color(.4f, .72f, .79f);
            var gold = new Color(.969f, .796f, .322f);
            Observe(slate);
            await harness.Run(); await harness.Advance(1); Observe(slate);
            async Task Enable(ulong owner, ElectricalEnable value) =>
                await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                    runControl: harness.RunControl(new(owner), RunControlLane.SourceEnable, RunControlValue.Of(value)));
            await Enable(3, ElectricalEnable.Enabled);
            await harness.Advance(45); Observe(cyan);
            Assert.True(head.Position.X > 1f);
            await Enable(4, ElectricalEnable.Enabled);
            await harness.Advance(1); Observe(gold);
            await Enable(3, ElectricalEnable.Disabled);
            await harness.Advance(1); Observe(cyan);
            await Enable(2, ElectricalEnable.Disabled);
            await harness.Advance(1); Observe(slate);
            await harness.Reset();
            part.ResetPhysicalVisuals();
            Observe(slate);
            Assert.Equal(new Vector3(.85f, 0, 0), head.Position);
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part); part.Free();
            root.GetParent()?.RemoveChild(root); root.Free();
        }
    }

    /// <summary>The five travel marks are printed flat on the barrel's front face, not raised (owner decision, 21 November 2026; the
    /// old boxes stood 2 to 14 mm proud): each a 0.022 × 0.1 m quad facing out of the face, between 0 and 0.5 mm in front of it (the
    /// face plane read from the barrel's own mesh), inside the face, and in the gold (0.969, 0.796, 0.322) the scene authored for the
    /// raised marks.</summary>
    [Fact]
    public void TheTravelMarksArePrintedFlatOnTheBarrelFace()
    {
        var root = WorkshopResources.LoadScene(TestBundle.Entry(new("linear_pusher")).Metadata.ScenePath).Instantiate<Node3D>();
        try
        {
            var barrel = VisibleSolidColliderTests.VerticesInRoot(root, root.GetNode<MeshInstance3D>("Visual/Barrel"));
            for (var i = 0; i < 5; i++)
            {
                var mark = VisibleSolidColliderTests.AssertPrintedFlat(root, root.GetNode<MeshInstance3D>($"Visual/TravelMark{i}"),
                    barrel.Max(v => v.Z), new(.022f, .1f), new Color(.969f, .796f, .322f));
                VisibleSolidColliderTests.AssertInsideRectangle(mark, barrel, $"TravelMark{i}");
            }
        }
        finally { root.Free(); }
    }
}
