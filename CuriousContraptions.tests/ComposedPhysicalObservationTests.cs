using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ComposedPhysicalObservationTests
{
    private static BundleEntry Actuators(bool reverse, bool removeFirst)
    {
        var template = TestBundle.Entry(new("powered_gate"));
        var shape = new ColliderDescriptor(CuriousContraptions.Abstractions.ColliderShapeKind.Box, new(.1f),
            Vector3.Zero, Quaternion.Identity, new(0, 0, 0, .1f), 0);
        PrismaticSliderDescriptor Slider(float x) => new(new(new(x, 0, 0), Quaternion.Identity),
            new(Vector3.Zero, Quaternion.Identity), -.2f, 2, MechanicalValue.Fixed(3), MechanicalValue.Fixed(.5f),
            MechanicalValue.Fixed(0), true,
            new(MassFrameDeclaration.Constant(1, Vector3.Zero, Vector3.One, Quaternion.Identity, 0, new(0, -9.81f, 0)), [shape]),
            new(MechanicalValue.Fixed(1.2f), MechanicalValue.Fixed(1.6f), MechanicalValue.Fixed(8),
                MechanicalValue.Fixed(20), MechanicalValue.Fixed(20)));
        PrismaticSliderDescriptor[] sliders = [Slider(-2), Slider(2)];
        PresentationBinding State(string target, int index, PresentationActuatorStateValues values) =>
            new(target, PresentationProperty.LocalX, PresentationDriver.CommittedActuatorState, PresentationValueMapping.ActuatorStateValues,
                0, 1, PresentationCurve.Linear, 0, PresentationOverlap.Replace, 0, PresentationTimerPhase.None,
                Actuator: new(CoordinateKind.Prismatic, index), StateValues: values);
        PresentationBinding[] bindings = [State("Blocked", reverse ? 1 : 0, new(.13f, .47f, .81f)),
            State("Free", reverse ? 0 : 1, new(-2, 3, 8))];
        if (removeFirst) bindings = [bindings[1]];
        return template with
        {
            Metadata = template.Metadata with { Id = "composed_actuators" },
            Footprint = new(3, 2, 2, 0),
            Colliders = [shape with { Offset = new(0, -2, 0) }],
            ColliderBindings = [], TubeMouths = [], TubeMouthBindings = [],
            Mechanical = template.Mechanical! with { Sliders = reverse ? sliders.Reverse().ToArray() : sliders },
            Electrical = template.Electrical! with
            {
                SupplyGraph = template.Electrical.SupplyGraph! with
                {
                    Nodes = [new CuriousContraptions.Abstractions.Electrical.SupplyConsumerDescriptor(new("consumer"), [
                        new CuriousContraptions.Abstractions.Electrical.SupplyDriveTarget(new(CoordinateKind.Prismatic, 0)),
                        new CuriousContraptions.Abstractions.Electrical.SupplyDriveTarget(new(CoordinateKind.Prismatic, 1))])]
                }
            },
            Presentation = new(1, PresentationAxes.None, reverse ? bindings.Reverse().ToArray() : bindings,
                [new("FirstBody", new(1), Vector3.Zero), new("SecondBody", new(2), Vector3.Zero)], [], [])
        };
    }

    private static float Sample(WorkshopIntegrationHarness harness, int binding)
    {
        var samples = harness.Presentation!.Advance(new(harness.Committed.Tick.Value * 4));
        return BitConverter.UInt32BitsToSingle(Assert.Single(samples,
            sample => sample.Owner == new GpuBodyId(2) && sample.Binding == binding).Value.CanonicalBits);
    }

    private static WorkshopCompiledScene WithTracks(ICompiledScene scene, IEnumerable<CompiledPresentationTrack> tracks) =>
        new(scene.Identity, scene.Revision, scene.Settings, scene.Physics, scene.Activation, tracks, scene.Owners, scene.RunControls);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExactActuatorsUseIndependentStateMapsAndRestoreTheirAuthoredInitialValues(bool reverse, bool removeFirst)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Actuators(reverse, removeFirst);
        Assert.Empty(BundleValidator.Validate([new("two_actuators", entry)]));
        var anchor = TestBundle.Entry(new("rope_anchor"));
        var obstacle = anchor with { Metadata = anchor.Metadata with { Id = "actuator_obstacle" },
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Box, new(.4f, .1f, .4f),
                Vector3.Zero, Quaternion.Identity, new(0, 0, 0, .1f), 0)], ColliderBindings = [] };
        Assert.Empty(BundleValidator.Validate([new("obstacle", obstacle)]));
        using var physics = DeclaredPhysics.Scoped([entry, obstacle]);
        PartId part = new(entry.Metadata.Id);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports), (new PartId("actuator_obstacle"), obstacle.Ports)]));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            PlacedParts.Battery(new(1), 6, 5, 0), WorkshopInput.Generic(RuntimeTestServices.Bundle, part, new(2), 0, 5, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("powered_gate"), new(3), 10, 5, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("actuator_obstacle"), new(4), -2, 5.7f, 0, 0, 0, 0, 1)),
            Connections: new(new WorkshopConnection(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        for (var i = 0; i < entry.Presentation.Bindings.Count; i++)
            Assert.Equal(entry.Presentation.Bindings[i].StateValues!.Value.Unpowered, Sample(harness, i));
        await harness.Run(); await harness.Advance(480); // Four-second guard for contact to hold one driven coordinate at rest.
        var scene = harness.Simulation.Scene;
        var owned = harness.Committed.Presentation.Actuators.ToArray().Where(row => row.Owner == new GpuBodyId(2)).ToArray();
        Assert.Equal(2, owned.Length);
        Assert.Single(owned, row => row.IsBlocked());
        Assert.Single(owned, row => !row.IsBlocked());
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(harness.Latest with { Scene = scene })).Scene!;
        for (var i = 0; i < entry.Presentation.Bindings.Count; i++)
        {
            var binding = entry.Presentation.Bindings[i];
            var selected = scene.Presentation.Single(track => track.Owner == new GpuBodyId(2) && track.Binding == i);
            var read = Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), row => row.Drive == selected.Actuator);
            Assert.True(read.SupplyClosed);
            Assert.Equal(binding.Node == "Blocked", read.IsBlocked());
            Assert.Equal(binding.Node == "Blocked" ? .81f : 3f, Sample(harness, i));
            Assert.Equal(selected.Actuator, decoded.Presentation.Single(track => track.Target == selected.Target).Actuator);
            Assert.Equal(binding.StateValues, decoded.Presentation.Single(track => track.Target == selected.Target).Declaration.StateValues);
        }
        var first = scene.Presentation.First(track => track.Owner == new GpuBodyId(2));
        var foreignDrive = scene.Presentation.First(track => track.Owner == new GpuBodyId(3)).Actuator!.Value;
        foreach (var invalidDrive in new[] { SupplyConsumer.Prismatic(new(999999)), foreignDrive })
            Assert.Throws<ArgumentException>(() => WithTracks(scene,
                scene.Presentation.Select(track => track.Target == first.Target ? track with { Actuator = invalidDrive } : track)));
        foreach (var invalid in new CoordinateReference?[] { null, new(CoordinateKind.Prismatic, 99), new(CoordinateKind.Revolute, 0) })
        {
            var bad = entry with { Presentation = entry.Presentation with
                { Bindings = [entry.Presentation.Bindings[0] with { Actuator = invalid }] } };
            Assert.NotEmpty(BundleValidator.Validate([new("invalid_actuator", bad)]));
        }
        await harness.Reset();
        Assert.All(harness.Committed.Presentation.Actuators.ToArray(), row => Assert.False(row.SupplyClosed));
        for (var i = 0; i < entry.Presentation.Bindings.Count; i++)
            Assert.Equal(entry.Presentation.Bindings[i].StateValues!.Value.Unpowered, Sample(harness, i));
    }

    private static BundleEntry Guides(bool reverse, bool removeFirst, bool sameGeometry)
    {
        var template = TestBundle.Entry(new("pulley"));
        var guide = Assert.Single(template.Ports);
        PortDescriptor[] ports =
        [
            guide with { Id = new("left"), LocalPosition = new(-2, .2f, .12f), GuideRadius = .2f, GuideOffset = new(-2, 0, .12f) },
            guide with { Id = new("right"), LocalPosition = new(2, .6f, .12f), GuideRadius = .6f, GuideOffset = new(2, 0, .12f) }
        ];
        if (sameGeometry) ports = ports.Select(port => port with
            { LocalPosition = new(0, .4f, .12f), GuideRadius = .4f, GuideOffset = new(0, 0, .12f) }).ToArray();
        PresentationBinding[] bindings =
        [
            template.Presentation.Bindings[0] with { Node = "Left", GuidePort = new("left") },
            template.Presentation.Bindings[0] with { Node = "Right", GuidePort = new("right") }
        ];
        if (removeFirst) bindings = [bindings[1]];
        return template with { Metadata = template.Metadata with { Id = "composed_guides" },
            Ports = reverse ? ports.Reverse().ToArray() : ports,
            Presentation = template.Presentation with { Bindings = reverse ? bindings.Reverse().ToArray() : bindings } };
    }

    private static WorkshopConstruction GuideBench(BundleEntry entry)
    {
        var owner = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(2), 0, 6, -.12f, 0, 0, 0, 1);
        var weights = new[] { WeightTests.Placed(3, -3, 3, 0, 2), WeightTests.Placed(5, 3, 3, 0, 6) };
        var partners = new[] { WeightTests.Placed(4, -1, 3, 0, 6), WeightTests.Placed(6, 1, 3, 0, 8) };
        var links = new List<WorkshopConnection>();
        var names = new[] { new PortId("left"), new PortId("right") };
        for (var i = 0; i < 2; i++)
        {
            var port = entry.Ports.Single(row => row.Id == names[i]);
            var start = new Vector3(i == 0 ? -3 : 3, 3, 0) + DeclaredPorts.Current.LocalPosition(weights[i], PortId.Tie);
            var end = new Vector3(i == 0 ? -1 : 1, 3, 0) + DeclaredPorts.Current.LocalPosition(partners[i], PortId.Tie);
            var length = RopeGeometry.EvaluateRoute(start, end,
                [new(new(0, 6, -.12f), Quaternion.Identity, port.GuideRadius, port.GuideOffset)]).TotalLength;
            links.Add(new(weights[i].Id, PortId.Tie, owner.Id, names[i], WorkshopConnectionDomain.Rope, length / 2));
            links.Add(new(owner.Id, names[i], partners[i].Id, PortId.Tie, WorkshopConnectionDomain.Rope, length / 2));
        }
        return new(new(2), WorkshopCadenceSettings.Default(), new(owner, weights[0], weights[1], partners[0], partners[1],
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("pulley"), new(7), 7, 6, 0, 0, 0, 0, 1)), Connections: new(links.ToArray()));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public async Task GuidePortsSelectDistinctMovingAnglesIndependentOfCosmeticOrderAndRemoval(bool reverse, bool removeFirst, bool sameGeometry)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Guides(reverse, removeFirst, sameGeometry);
        PartId part = new(entry.Metadata.Id);
        Assert.Empty(BundleValidator.Validate([new("two_guides", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var bench = GuideBench(entry);
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(60);
        var ids = bench.Instances.Identities[new(2)].Guides.Values.ToHashSet();
        var guides = harness.Committed.Presentation.Guides.ToArray().Where(guide => ids.Contains(guide.Guide)).ToArray();
        var foreignGuide = bench.Instances.Identities[new(7)].Guides[PortId.Tie];
        Assert.Equal(0f, Assert.Single(harness.Committed.Presentation.Guides.ToArray(), guide => guide.Guide == foreignGuide).Angle);
        Assert.Equal(2, guides.Length);
        Assert.All(guides, guide => Assert.True(MathF.Abs(guide.Angle) > .01f, "Falling loads must visibly turn both declared guides."));
        Assert.True(MathF.Abs(guides[0].Angle - guides[1].Angle) > .01f, "Independent routes must produce distinguishable visual angles.");
        for (var i = 0; i < entry.Presentation.Bindings.Count; i++)
        {
            var port = entry.Presentation.Bindings[i].GuidePort!.Value;
            var id = bench.Instances.Identities[new(2)].Guides[port];
            Assert.Equal(Assert.Single(guides, guide => guide.Guide == id).Angle, Sample(harness, i));
        }
        var scene = harness.Simulation.Scene;
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(harness.Latest with { Scene = scene })).Scene!;
        Assert.Equal(scene.Presentation.Select(track => track.Guide), decoded.Presentation.Select(track => track.Guide));
        Assert.Equal(scene.Physics.TensileSpans.ToArray().Select(span => span.Guide),
            decoded.Physics.TensileSpans.ToArray().Select(span => span.Guide));
        AssertGuideAdmission(scene.Physics, foreignGuide);
        var first = scene.Presentation.First(track => track.Owner == new GpuBodyId(2));
        foreach (var invalidGuide in new[] { new RopeGuideId(999999), foreignGuide })
            Assert.Throws<ArgumentException>(() => WithTracks(scene,
                scene.Presentation.Select(track => track.Target == first.Target ? track with { Guide = invalidGuide } : track)));
        foreach (var selector in new PortId?[] { null, new("missing") })
            Assert.NotEmpty(BundleValidator.Validate([new("invalid_guide", entry with { Presentation = entry.Presentation with
                { Bindings = [entry.Presentation.Bindings[0] with { GuidePort = selector }] } })]));
        var wrongKind = entry with { Ports = entry.Ports.Select(port => port with
            { AttachmentRole = RopeAttachmentRole.Anchor, GuideRadius = 0, GuideOffset = Vector3.Zero }).ToArray() };
        Assert.NotEmpty(BundleValidator.Validate([new("nonguide_port", wrongKind)]));
        await harness.Reset();
        Assert.All(harness.Committed.Presentation.Guides.ToArray(), guide => Assert.Equal(0f, guide.Angle));
        for (var i = 0; i < entry.Presentation.Bindings.Count; i++) Assert.Equal(0f, Sample(harness, i));
    }

    private static void AssertGuideAdmission(IPhysicsSceneDeclaration scene, RopeGuideId foreign)
    {
        var spans = scene.TensileSpans.ToArray();
        var first = Array.FindIndex(spans, span => span.Guide.Value != 0);
        var second = Array.FindIndex(spans, first + 1, span => span.Guide.Value != 0);
        PhysicsSceneDeclaration Declare(TensileSpanDeclaration[] values) => new(scene.Document, scene.NextIdentity,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides,
            tensileSpans: values, ropeGuides: scene.RopeGuides);
        Assert.NotNull(Declare(spans));
        foreach (var invalid in new[] { spans[first] with { Guide = default },
            spans[first] with { Guide = new(999999) }, spans[first] with { Guide = foreign },
            spans[first] with { GuideRadius = spans[first].GuideRadius + 1f } })
        {
            var changed = (TensileSpanDeclaration[])spans.Clone();
            changed[first] = invalid;
            Assert.Throws<ArgumentException>(() => Declare(changed));
        }
        var duplicate = (TensileSpanDeclaration[])spans.Clone();
        duplicate[second] = duplicate[second] with { Guide = spans[first].Guide,
            GuideRadius = spans[first].GuideRadius, GuideOffset = spans[first].GuideOffset };
        Assert.Throws<ArgumentException>(() => Declare(duplicate));
        var source = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        PhysicsGpuAbi.ValidateCandidate(source, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
        var forged = (byte[])source.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(
            forged.AsSpan(scene.World.TensileSpanOffset(first) + PhysicsGpuAbi.TensileGuideIdentityOffset, 8), foreign.Value);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(forged, source, new(0),
            PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
        Assert.Equal(spans[first], PhysicsGpuAbi.ReadTensileSpan(source, first));
        Assert.Equal(spans[second], PhysicsGpuAbi.ReadTensileSpan(source, second));
    }
}
