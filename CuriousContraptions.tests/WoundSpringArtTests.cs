using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class WoundSpringArtTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task CoilAndChargeIndexFollowActualHeadAndResetToAuthoredGeometry()
    {
        var spring = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(1), 0, 3, 0, 0, 0, 0, 1);
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(2), -3, 3, 2, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(3), -5, 3, 2);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(spring, motor, battery),
            Connections: new(new WorkshopConnection(new(3), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new WorkshopConnection(new(2), PortId.Drive, new(1), PortId.DriveIn, WorkshopConnectionDomain.Mechanical)));
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var root = new Node3D();
        var part = registry.Create(new("wound_spring"));
        part.AuthoredId = new(1);
        part.Position = new(0, 3, 0);
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var ids = construction.Instances.Identities;
        var headId = ids[new(1)].Sliders[0].Body;
        var shaftId = ids[new(1)].Hinges[0].Body;
        var physical = new WorkshopPhysicalPresentation();
        try
        {
            godot.Tree.Root.AddChild(root); root.AddChild(part);
            part.EnsureConstructed(); part.InstallPhysicalIdentity(new(1), headId, ids);
            part.InstallPhysicalIdentity(new(2), shaftId, ids);
            var head = part.GetNode<Node3D>("Visual/Head");
            var shaft = part.GetNode<Node3D>("Visual/Shaft");
            var coil = part.GetNode<Node3D>("Visual/Coil");
            var index = part.GetNode<Node3D>("Visual/Head/ChargeIndex");
            var mark4 = part.GetNode<Node3D>("Visual/Mark4");
            var sleeve = Assert.IsType<CylinderMesh>(part.GetNode<MeshInstance3D>("Visual/Sleeve").Mesh);
            Assert.False(sleeve.CapTop); Assert.False(sleeve.CapBottom);
            Assert.Equal(new Color(.4f, .72f, .79f, .18f),
                Assert.IsType<StandardMaterial3D>(sleeve.SurfaceGetMaterial(0)).AlbedoColor);
            Assert.Equal(0f, mark4.Position.Y);
            part.ApplyPresentationParameters(new Dictionary<string, float> { ["stiffness"] = 160f, ["stroke"] = .2f, ["winding_lead"] = .1f });
            Assert.InRange(mark4.Position.Y - .6f, -.001f, .001f); // The file's 1 mm visible alignment contract.
            part.ApplyPresentationParameters(new Dictionary<string, float> { ["stiffness"] = 160f, ["stroke"] = .8f, ["winding_lead"] = .1f });
            Assert.Equal(0f, mark4.Position.Y);
            void Observe()
            {
                var poses = Enumerable.Range(0, harness.Committed.Bodies.Count).Select(i => harness.Committed.Bodies[i])
                    .Where(body => body.Body.Id == headId || body.Body.Id == shaftId)
                    .Select(body => new PresentedBody(body.Body.Id, body.Body.Cell, body.Body.Local, body.Rotation)).ToArray();
                physical.Apply([part], root.GlobalTransform, new PresentedBodySet(poses));
                Assert.InRange((coil.Transform * new Vector3(0, -.69f, 0)).DistanceTo(new(0, -.8f, 0)), 0f, .001f);
                Assert.InRange((coil.Transform * new Vector3(0, .69f, 0)).DistanceTo(head.Position + new Vector3(0, -.32f, 0)), 0f, .001f);
                Assert.Equal(new Vector3(.6f, -.1f, .1004f), index.Position);
            }
            Observe();
            Assert.Equal(Quaternion.Identity, shaft.Quaternion);
            await harness.Run(); await harness.Advance(120); Observe();
            Assert.True(head.Position.Y < .4f);
            Assert.True(MathF.Abs(shaft.Quaternion.Z) > .1f, "The physical shaft must rotate visibly during winding.");
            var charged = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
            Observe(); Assert.Equal(charged, Assert.Single(harness.Committed.Presentation.Retentions.ToArray()));
            await harness.Reset(); part.ResetPhysicalVisuals();
            Assert.Equal(new Vector3(0, .9f, 0), head.Position); // Exact authored visual Reset precedes world/local pose conversion.
            Assert.Equal(Quaternion.Identity, shaft.Quaternion);
            Observe();
            Assert.InRange(head.Position.DistanceTo(new(0, .9f, 0)), 0f, .001f); // Same 1 mm visible alignment contract as the coil.
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part); part.Free();
            root.GetParent()?.RemoveChild(root); root.Free();
        }
    }

    /// <summary>The five stroke marks and the charge index are printed flat on the right rail's front face, and the shaft's spoke on
    /// the winding wheel's face, not raised (owner decision, 21 November 2026; the old boxes stood 7.5 to 57.5 mm proud and overhung
    /// the 0.13 m rail): each a quad of its authored size facing out of its face, between 0 and 0.5 mm in front of it (the face plane
    /// read from the face's own mesh), inside the face, and in the colour the scene authored for the raised box: navy (0.161, 0.224,
    /// 0.329) for the marks, gold (0.969, 0.796, 0.322) for the index and the spoke. The index, which slides over the marks, lies in
    /// front of them.</summary>
    [Fact]
    public void TheMarksIndexAndSpokeArePrintedFlatOnTheirFaces()
    {
        var root = WorkshopResources.LoadScene(TestBundle.Entry(new("wound_spring")).Metadata.ScenePath).Instantiate<Node3D>();
        try
        {
            var (navy, gold) = (new Color(.161f, .224f, .329f), new Color(.969f, .796f, .322f));
            var rail = VisibleSolidColliderTests.VerticesInRoot(root, root.GetNode<MeshInstance3D>("Visual/RightRail"));
            var marks = Enumerable.Range(0, 5).Select(i => VisibleSolidColliderTests.AssertPrintedFlat(root,
                root.GetNode<MeshInstance3D>($"Visual/Mark{i}"), rail.Max(v => v.Z), new(.1f, .018f), navy)).ToArray();
            var index = VisibleSolidColliderTests.AssertPrintedFlat(root, root.GetNode<MeshInstance3D>("Visual/Head/ChargeIndex"),
                rail.Max(v => v.Z), new(.12f, .045f), gold);
            foreach (var (print, i) in marks.Append(index).Select((print, i) => (print, i)))
                VisibleSolidColliderTests.AssertInsideRectangle(print, rail, i < marks.Length ? $"Mark{i}" : "ChargeIndex");
            Assert.All(marks, mark => Assert.True(index.Min(v => v.Z) > mark.Max(v => v.Z), "the charge index lies under a stroke mark"));
            var wheel = root.GetNode<MeshInstance3D>("Visual/Shaft/Wheel");
            var rim = VisibleSolidColliderTests.VerticesInRoot(root, wheel);
            var spoke = VisibleSolidColliderTests.AssertPrintedFlat(root, root.GetNode<MeshInstance3D>("Visual/Shaft/Spoke"),
                rim.Max(v => v.Z), new(.42f, .035f), gold);
            VisibleSolidColliderTests.AssertInsideDisc(spoke, rim, Assert.IsType<CylinderMesh>(wheel.Mesh).TopRadius, "Shaft/Spoke");
        }
        finally { root.Free(); }
    }
}
