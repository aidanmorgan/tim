using System;
using System.IO;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class RopeGuideTests
{
    private static float BodyY(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var value));
        return (value.Body.Cell.Y + value.Body.Local.Y) / 16f;
    }

    [Fact]
    public void EvaluateRouteSupportsMultipleGuidesWithDistinctRadiiAndOffsets()
    {
        // Unfamiliar Guide A (r=0.2, offset 0.05), Pulley (r=0.4, offset 0.12), Unfamiliar Guide B (r=0.6, offset 0.20)
        var posA = new Vector3(-3f, 4f, 0f);
        var posB = new Vector3(3f, 4f, 0f);
        var guides = new RouteGuideFrame[]
        {
            new(new(-2f, 6f, 0f), Quaternion.Identity, 0.2f, new(0f, 0f, 0.05f)),
            new(new(0f, 6.5f, 0f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f)),
            new(new(2f, 6f, 0f), Quaternion.Identity, 0.6f, new(0f, 0f, 0.20f))
        };

        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, guides);

        Assert.Equal(3, evaluated.Arcs.Length);

        // Arc lengths equal r * |sweep|
        Assert.InRange(MathF.Abs(evaluated.Arcs[0].Length - 0.2f * MathF.Abs(evaluated.Arcs[0].Sweep)), 0f, 1e-3f);
        Assert.InRange(MathF.Abs(evaluated.Arcs[1].Length - 0.4f * MathF.Abs(evaluated.Arcs[1].Sweep)), 0f, 1e-3f);
        Assert.InRange(MathF.Abs(evaluated.Arcs[2].Length - 0.6f * MathF.Abs(evaluated.Arcs[2].Sweep)), 0f, 1e-3f);

        // All arcs have positive deflection angles and lengths
        Assert.True(MathF.Abs(evaluated.Arcs[0].Sweep) > 0f);
        Assert.True(MathF.Abs(evaluated.Arcs[1].Sweep) > 0f);
        Assert.True(MathF.Abs(evaluated.Arcs[2].Sweep) > 0f);

        // Sum of straight tangent segments and arcs equals TotalLength
        var leg0 = Vector3.Distance(posA, evaluated.Arcs[0].Entry);
        var leg1 = Vector3.Distance(evaluated.Arcs[0].Exit, evaluated.Arcs[1].Entry);
        var leg2 = Vector3.Distance(evaluated.Arcs[1].Exit, evaluated.Arcs[2].Entry);
        var leg3 = Vector3.Distance(evaluated.Arcs[2].Exit, posB);
        var arcSum = evaluated.Arcs[0].Length + evaluated.Arcs[1].Length + evaluated.Arcs[2].Length;
        var expectedTotal = leg0 + leg1 + leg2 + leg3 + arcSum;
        Assert.InRange(MathF.Abs(expectedTotal - evaluated.TotalLength), 0f, 1e-3f);
    }

    [Fact]
    public async Task DynamicSimulationRotatesDistinctGuidesInInverseProportionToRadius()
    {
        // Dynamic Atwood simulation with Guide A (r=0.2) and Guide B (r=0.6) under moving loads
        var scene = new LawScene(bench: false);
        var a = scene.Cylinder(new(-2.4f, 3f, 0f), .2f, .2f, 5f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);
        // Guide A at (-2, 6.4, -.05), local attachment (0, .2, .05) -> world attachment (-2, 6.6, 0)
        var g1 = scene.StaticCylinder(new(-2f, 6.4f, -.05f), .2f, .17f, Quaternion.Identity);
        // Guide B at (2, 6.0, -.15), local attachment (0, .6, .15) -> world attachment (2, 6.6, 0)
        var g2 = scene.StaticCylinder(new(2f, 6.0f, -.15f), .6f, .17f, Quaternion.Identity);
        var b = scene.Cylinder(new(2.4f, 3f, 0f), .2f, .2f, 10f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);

        var posA = new Vector3(-2.4f, 3.2f, 0f);
        var posB = new Vector3(2.4f, 3.2f, 0f);
        var guides = new RouteGuideFrame[]
        {
            new(new(-2f, 6.4f, -.05f), Quaternion.Identity, 0.2f, new(0f, 0f, 0.05f)),
            new(new(2f, 6.0f, -.15f), Quaternion.Identity, 0.6f, new(0f, 0f, 0.15f))
        };
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, guides);
        var totalLength = evaluated.TotalLength;
        var compiled = scene.Admit().Scene;
        var span0 = (totalLength - 4f) * .5f;
        var spans = new TensileSpanDeclaration[]
        {
            new(new(1), new(1), 0, a, new(0, .2f, 0), g1, new(0, .2f, .05f), span0, true, 0.2f, new(0f, 0f, 0.05f)),
            new(new(2), new(1), 1, g1, new(0, .2f, .05f), g2, new(0, .6f, .15f), 4f, true, 0.6f, new(0f, 0f, 0.15f)),
            new(new(3), new(1), 2, g2, new(0, .6f, .15f), b, new(0, .2f, 0), span0, true),
        };
        var physicsScene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            compiled.Bodies, compiled.Colliders, compiled.Materials, [], [], tensileSpans: spans,
            ropeGuides: [new(new(1), g1, 0.2f, new(0f, 0f, 0.05f)), new(new(2), g2, 0.6f, new(0f, 0f, 0.15f))]);

        var initialScene = new WorkshopCompiledScene(new(1), new(1), WorkshopCadenceSettings.Default(),
            new PhysicsSceneDeclaration(compiled.Document, 1, [], [], [], [], []),
            new ActivationNetwork([], []), [], [], []);
        var compiledScene = new WorkshopCompiledScene(new(2), new(2), WorkshopCadenceSettings.Default(),
            physicsScene, new ActivationNetwork([], []), [], [], []);

        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), compiled.Document);
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(),
            initialScene, new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);

        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(compiledScene)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);

        for (var i = 0; i < 30; i++)
        {
            var result = await simulation.Advance();
            Assert.Equal(WorkshopCommandOutcome.Applied, result.Outcome);
        }

        var publishedGuides = simulation.Committed.Presentation.Guides.ToArray();
        Assert.Equal(2, publishedGuides.Length);

        var guideA = publishedGuides.First(g => g.Guide == new RopeGuideId(1));
        var guideB = publishedGuides.First(g => g.Guide == new RopeGuideId(2));

        // Travel must be positive and equal on the same route
        Assert.True(guideA.Travel > 0.05f, $"Guide A travel must be positive, got {guideA.Travel}");
        Assert.InRange(MathF.Abs(guideA.Travel - guideB.Travel), 0f, 1e-4f);

        // Angles scale in inverse proportion to radii: theta = travel / r
        var expectedAngleA = ((guideA.Travel / 0.2f) % MathF.Tau + MathF.Tau) % MathF.Tau;
        var expectedAngleB = ((guideB.Travel / 0.6f) % MathF.Tau + MathF.Tau) % MathF.Tau;
        Assert.InRange(MathF.Abs(guideA.Angle - expectedAngleA), 0f, 1e-3f);
        Assert.InRange(MathF.Abs(guideB.Angle - expectedAngleB), 0f, 1e-3f);


    }

    [Fact]
    public async Task DecoupledPresentationPublishesGuidesWithoutPresentationTracks()
    {
        // Construct a scene without any cosmetic Presentation tracks and ensure guides are still published
        var scene = new LawScene(bench: false);
        var g = scene.StaticCylinder(new(0f, 6f, -.12f), .4f, .17f, Quaternion.Identity);
        var a = scene.Cylinder(new(-2f, 3f, 0f), .2f, .2f, 5f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);
        var b = scene.Cylinder(new(2f, 3f, 0f), .2f, .2f, 15f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);

        var posA = new Vector3(-2f, 3.2f, 0f);
        var posB = new Vector3(2f, 3.2f, 0f);
        var guideFrame = new RouteGuideFrame(new(0f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f));
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, [guideFrame]);
        var spanLen = evaluated.TotalLength * 0.5f;

        var spans = new TensileSpanDeclaration[]
        {
            new(new(1), new(1), 0, a, new(0, .2f, 0), g, new(0, .4f, .12f), spanLen, true, 0.4f, new(0f, 0f, 0.12f)),
            new(new(2), new(1), 1, g, new(0, .4f, .12f), b, new(0, .2f, 0), spanLen, true),
        };
        var compiled = scene.Admit().Scene;
        var physicsScene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            compiled.Bodies, compiled.Colliders, compiled.Materials, [], [], tensileSpans: spans,
            ropeGuides: [new(new(1), g, 0.4f, new(0f, 0f, 0.12f))]);

        // Explicitly empty presentation tracks
        var initialScene = new WorkshopCompiledScene(new(1), new(1), WorkshopCadenceSettings.Default(),
            new PhysicsSceneDeclaration(compiled.Document, 1, [], [], [], [], []),
            new ActivationNetwork([], []), [], [], []);
        var compiledScene = new WorkshopCompiledScene(new(2), new(2), WorkshopCadenceSettings.Default(),
            physicsScene, new ActivationNetwork([], []), [], [], []);

        Assert.True(compiledScene.Presentation.IsEmpty);

        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), compiled.Document);
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(),
            initialScene, new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);

        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(compiledScene)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);

        // Check committed presentation immediately has the guide with 0 travel and 0 angle
        var guides = simulation.Committed.Presentation.Guides.ToArray();
        Assert.Single(guides);
        Assert.Equal(new RopeGuideId(1), guides[0].Guide);
        Assert.Equal(0f, guides[0].Travel);
        Assert.Equal(0f, guides[0].Angle);

        // Advance under dynamic simulation to verify guide observations update and publish under motion without cosmetic tracks
        for (var step = 0; step < 20; step++)
        {
            var adv = await simulation.Advance();
            Assert.Equal(WorkshopCommandOutcome.Applied, adv.Outcome);
        }

        var dynamicGuides = simulation.Committed.Presentation.Guides.ToArray();
        Assert.Single(dynamicGuides);
        Assert.Equal(new RopeGuideId(1), dynamicGuides[0].Guide);
        Assert.True(dynamicGuides[0].Travel > 0.01f, $"Guide travel should increase under motion, got {dynamicGuides[0].Travel}");
        Assert.True(dynamicGuides[0].Angle > 0.01f, $"Guide angle should rotate under motion, got {dynamicGuides[0].Angle}");
    }

    [Fact]
    public async Task StraightRouteControlHasZeroGuidesAndLineSegmentLength()
    {
        var posA = new Vector3(-2f, 3f, 0f);
        var posB = new Vector3(2f, 3f, 0f);

        // 0 guides evaluate to line segment length
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, []);
        Assert.Empty(evaluated.Arcs);
        Assert.InRange(MathF.Abs(evaluated.TotalLength - Vector3.Distance(posA, posB)), 0f, 1e-5f);

        // In simulation, 1 direct span produces empty Guides read
        var anchor = WorkshopInput.Generic(new("rope_anchor"), new(1), 0f, 6f, -.18f, 0f, 0f, 0f, 1f);
        var weight = WeightTests.Placed(2, 0f, 3f, 0f, 4f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(anchor, weight),
            Connections: new(new WorkshopConnection(new(1), WorkshopSocket.Tie, new(2), WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, 3f)));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        Assert.Empty(harness.Committed.Presentation.Guides.ToArray());
    }

    [Fact]
    public void BoundaryWrap180DegreeTurnAndSmallDeflection()
    {
        // 180-degree U-turn wrap over the top of a guide of radius 0.5
        var guide = new RouteGuideFrame(new(0f, 0f, 0f), Quaternion.Identity, 0.5f);
        var posA = new Vector3(-0.5f, -3f, 0f);
        var posB = new Vector3(0.5f, -3f, 0f);

        var wrap = RopeGeometry.EvaluateRoute(posA, posB, [guide]);
        Assert.Single(wrap.Arcs);
        Assert.InRange(MathF.Abs(wrap.Arcs[0].Sweep), MathF.PI - 0.05f, MathF.PI + 0.05f);
        Assert.InRange(wrap.Arcs[0].Length, 0.5f * MathF.PI - 0.05f, 0.5f * MathF.PI + 0.05f);

        // Small grazing deflection (deflection angle < 0.3 rad)
        var grazingGuide = new RouteGuideFrame(new(0f, 0.55f, 0f), Quaternion.Identity, 0.5f);
        var grazingA = new Vector3(-5f, 0.5f, 0f);
        var grazingB = new Vector3(5f, 0.5f, 0f);

        var grazing = RopeGeometry.EvaluateRoute(grazingA, grazingB, [grazingGuide]);
        Assert.Single(grazing.Arcs);
        Assert.True(MathF.Abs(grazing.Arcs[0].Sweep) > 0f);
        Assert.True(MathF.Abs(grazing.Arcs[0].Sweep) < 0.3f);
        Assert.InRange(MathF.Abs(grazing.Arcs[0].Length - 0.5f * MathF.Abs(grazing.Arcs[0].Sweep)), 0f, 1e-4f);
    }

    [Fact]
    public void StructuralValidationRefusesInvalidGuideDeclarations()
    {
        var pulleyEntry = TestBundle.Entries.Single(e => e.Metadata.Id == "pulley");
        var pulleyPort = pulleyEntry.Ports[0];
        var weightEntry = TestBundle.Entries.Single(e => e.Metadata.Id == "weight");
        var weightPort = weightEntry.Ports[0];

        // 1. BundleValidator rejects non-positive or non-finite GuideRadius
        Assert.NotEmpty(BundleValidator.Validate([new("pulley", pulleyEntry with { Ports = [pulleyPort with { GuideRadius = 0f }] })]));
        Assert.NotEmpty(BundleValidator.Validate([new("pulley", pulleyEntry with { Ports = [pulleyPort with { GuideRadius = -0.4f }] })]));
        Assert.NotEmpty(BundleValidator.Validate([new("pulley", pulleyEntry with { Ports = [pulleyPort with { GuideRadius = float.NaN }] })]));

        // 2. BundleValidator rejects non-guide port declaring GuideRadius or GuideOffset
        Assert.NotEmpty(BundleValidator.Validate([new("weight", weightEntry with { Ports = [weightPort with { GuideRadius = 0.4f }] })]));
        Assert.NotEmpty(BundleValidator.Validate([new("weight", weightEntry with { Ports = [weightPort with { GuideOffset = new(0f, 0f, 1f) }] })]));

        // 3. ElementPortCatalogue.Declare typed refusal: OutOfRange
        var ex = Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("fixture"), [pulleyPort with { GuideRadius = -1f }])]));
        Assert.Equal(ElementPortRejection.OutOfRange, ex.Reason);

        // 4. TensileSpanDeclaration.Validate rejects invalid GuideRadius or GuideOffset
        Assert.Throws<ArgumentException>(() =>
            new TensileSpanDeclaration(new(1), new(1), 0, new(1), Vector3.Zero, new(2), Vector3.Zero, 1f, true, GuideRadius: -0.5f).Validate());
        Assert.Throws<ArgumentException>(() =>
            new TensileSpanDeclaration(new(1), new(1), 0, new(1), Vector3.Zero, new(2), Vector3.Zero, 1f, true, GuideRadius: float.NaN).Validate());
        Assert.Throws<ArgumentException>(() =>
            new TensileSpanDeclaration(new(1), new(1), 0, new(1), Vector3.Zero, new(2), Vector3.Zero, 1f, true, GuideOffset: new(float.NaN, 0, 0)).Validate());

        // 5. RopeGuideDeclaration.Validate rejects invalid identity, body, radius or offset
        Assert.Throws<ArgumentException>(() => new RopeGuideDeclaration(new(0), new(1), 0.4f, Vector3.Zero).Validate());
        Assert.Throws<ArgumentException>(() => new RopeGuideDeclaration(new(1), new(0), 0.4f, Vector3.Zero).Validate());
        Assert.Throws<ArgumentException>(() => new RopeGuideDeclaration(new(1), new(1), 0f, Vector3.Zero).Validate());
        Assert.Throws<ArgumentException>(() => new RopeGuideDeclaration(new(1), new(1), -0.2f, Vector3.Zero).Validate());
        Assert.Throws<ArgumentException>(() => new RopeGuideDeclaration(new(1), new(1), float.NaN, Vector3.Zero).Validate());
        Assert.Throws<ArgumentException>(() => new RopeGuideDeclaration(new(1), new(1), 0.4f, new(float.NaN, 0, 0)).Validate());

        // 6. PhysicsSceneDeclaration rejects intermediate tensile span without positive GuideRadius
        var scene = new LawScene(bench: false);
        var p1 = scene.StaticCylinder(new(-2f, 6f, 0f), .4f, .17f, Quaternion.Identity);
        var a = scene.Cylinder(new(-2f, 3f, 0f), .2f, .2f, 5f, LawMaterial.Domino, Quaternion.Identity);
        var b = scene.Cylinder(new(2f, 3f, 0f), .2f, .2f, 5f, LawMaterial.Domino, Quaternion.Identity);
        var compiled = scene.Admit().Scene;
        var intermediateZeroRadius = new TensileSpanDeclaration[]
        {
            new(new(1), new(1), 0, a, Vector3.Zero, p1, Vector3.Zero, 4f, true, GuideRadius: 0f),
            new(new(2), new(1), 1, p1, Vector3.Zero, b, Vector3.Zero, 4f, true),
        };
        Assert.Throws<ArgumentException>(() =>
            new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
                compiled.Bodies, compiled.Colliders, compiled.Materials, [], [], tensileSpans: intermediateZeroRadius));

        // 7. PhysicsSceneDeclaration rejects rope guide referencing missing body
        Assert.Throws<ArgumentException>(() =>
            new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
                compiled.Bodies, compiled.Colliders, compiled.Materials, [], [],
                ropeGuides: [new(new(1), new GpuBodyId(999), 0.4f, Vector3.Zero)]));

        // 8. PhysicsSceneDeclaration rejects duplicate RopeGuideId
        Assert.Throws<ArgumentException>(() =>
            new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
                compiled.Bodies, compiled.Colliders, compiled.Materials, [], [],
                ropeGuides: [new(new(1), p1, 0.4f, Vector3.Zero), new(new(1), p1, 0.4f, Vector3.Zero)]));
    }

    [Fact]
    public async Task ResetRestoresInitialRouteStateAndZeroGuideObservations()
    {
        // 2-pulley Atwood setup
        var a = WeightTests.Placed(1, -2.4f, 2.5f, 0f, 2f);
        var p1 = WorkshopInput.Generic(new("pulley"), new(2), -2f, 6f, -.12f, 0, 0, 0, 1);
        var p2 = WorkshopInput.Generic(new("pulley"), new(3), 2f, 6f, -.12f, 0, 0, 0, 1);
        var b = WeightTests.Placed(4, 2.4f, 3.5f, 0f, 6f);
        var tieA = DeclaredPorts.Current.LocalPosition(a, WorkshopSocket.Tie);
        var tieB = DeclaredPorts.Current.LocalPosition(b, WorkshopSocket.Tie);
        var posA = new Vector3(-2.4f, 2.5f, 0f) + tieA;
        var posB = new Vector3(2.4f, 3.5f, 0f) + tieB;
        var guides = new RouteGuideFrame[]
        {
            new(new(-2f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f)),
            new(new(2f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f))
        };
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, guides);
        var len = evaluated.TotalLength;
        var span1 = 4.0f;
        var remaining = MathF.Max(0.1f, len - span1);
        var span0 = remaining * 0.5f;
        var span2 = remaining * 0.5f;
        var link0 = new WorkshopConnection(a.Id, WorkshopSocket.Tie, p1.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span0);
        var link1 = new WorkshopConnection(p1.Id, WorkshopSocket.Tie, p2.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span1);
        var link2 = new WorkshopConnection(p2.Id, WorkshopSocket.Tie, b.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span2);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(a, p1, p2, b),
            Connections: new(link0, link1, link2));

        await using var world = await WorkshopIntegrationHarness.Running(construction);
        var initialA = BodyY(world.Committed, a.Id);
        var initialB = BodyY(world.Committed, b.Id);

        await world.Advance(60);

        // Motion occurred
        Assert.True(MathF.Abs(BodyY(world.Committed, a.Id) - initialA) > 0.05f);
        Assert.True(MathF.Abs(BodyY(world.Committed, b.Id) - initialB) > 0.05f);
        Assert.All(world.Committed.Presentation.Guides.ToArray(), g =>
        {
            Assert.True(g.Travel > 0.05f);
            Assert.True(g.Angle > 0.1f);
        });

        // Reset
        await world.Reset();

        // Post-reset state restores travel 0, angle 0, and original poses
        Assert.All(world.Committed.Presentation.Guides.ToArray(), g =>
        {
            Assert.Equal(0f, g.Travel);
            Assert.Equal(0f, g.Angle);
        });
        Assert.InRange(MathF.Abs(BodyY(world.Committed, a.Id) - initialA), 0f, 1e-3f);
        Assert.InRange(MathF.Abs(BodyY(world.Committed, b.Id) - initialB), 0f, 1e-3f);
    }


    [Fact]
    public async Task MultipleGuidePortsOnSameBodyHaveDistinctIdsAndPublishBothObservations()
    {
        var scene = new LawScene(bench: false);
        // Single static body hosting two distinct guide sheaves
        var body = scene.StaticCylinder(new(0f, 6f, 0f), .8f, .17f, Quaternion.Identity);
        var a = scene.Cylinder(new(-2.4f, 3f, 0f), .2f, .2f, 5f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);
        var anchor1 = scene.StaticCylinder(new(-0.3f, 4.5f, 0.05f), .2f, .17f, Quaternion.Identity);
        var b = scene.Cylinder(new(2.4f, 3f, 0f), .2f, .2f, 15f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);
        var anchor2 = scene.StaticCylinder(new(0.3f, 4.5f, 0.05f), .2f, .17f, Quaternion.Identity);

        var guideId1 = new RopeGuideId(10);
        var guideId2 = new RopeGuideId(20);

        var compiled = scene.Admit().Scene;

        // Two distinct routes through distinct guide ports on the same body
        var spans = new TensileSpanDeclaration[]
        {
            new(new(1), new(1), 0, a, new(0, .2f, 0), body, new(-0.3f, .2f, .05f), 3.5f, true, 0.2f, new(-0.3f, 0f, 0.05f)),
            new(new(2), new(1), 1, body, new(-0.3f, .2f, .05f), anchor1, Vector3.Zero, 3.5f, true),

            new(new(3), new(2), 0, b, new(0, .2f, 0), body, new(0.3f, .4f, .05f), 3.5f, true, 0.4f, new(0.3f, 0f, 0.05f)),
            new(new(4), new(2), 1, body, new(0.3f, .4f, .05f), anchor2, Vector3.Zero, 3.5f, true)
        };

        var physicsScene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            compiled.Bodies, compiled.Colliders, compiled.Materials, [], [], tensileSpans: spans,
            ropeGuides: [
                new(guideId1, body, 0.2f, new(-0.3f, 0f, 0.05f)),
                new(guideId2, body, 0.4f, new(0.3f, 0f, 0.05f))
            ]);

        var initialScene = new WorkshopCompiledScene(new(1), new(1), WorkshopCadenceSettings.Default(),
            new PhysicsSceneDeclaration(compiled.Document, 1, [], [], [], [], []),
            new ActivationNetwork([], []), [], [], []);
        var compiledScene = new WorkshopCompiledScene(new(2), new(2), WorkshopCadenceSettings.Default(),
            physicsScene, new ActivationNetwork([], []), [], [], []);

        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), compiled.Document);
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(),
            initialScene, new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);

        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(compiledScene)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);

        // Before motion, both sheaves exist with 0 travel and angle
        var initialGuides = simulation.Committed.Presentation.Guides.ToArray();
        Assert.Equal(2, initialGuides.Length);
        Assert.Equal(guideId1, initialGuides[0].Guide);
        Assert.Equal(guideId2, initialGuides[1].Guide);
        Assert.Equal(0f, initialGuides[0].Travel);
        Assert.Equal(0f, initialGuides[1].Travel);
        Assert.Equal(0f, initialGuides[0].Angle);
        Assert.Equal(0f, initialGuides[1].Angle);
    }
}
