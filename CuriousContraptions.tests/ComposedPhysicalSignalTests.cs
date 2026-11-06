using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ComposedPhysicalSignalTests
{
    private static SignalEdge Edge(string source, string target) =>
        new(new(new(source), SignalTerminal.Output), new(new(target), SignalTerminal.Input));
    private static ActivationNodeId Node(WorkshopConstruction bench, string local) =>
        bench.Instances.Identities[new(2)].Signals[new(local)].Node;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoAperturesOnOneFrameDispatchEachPhysicalPassOnce(bool reverse)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        PartId part = new("twin_aperture");
        var original = TestBundle.Entry(new("ball_detector"));
        var first = Assert.IsType<SignalPassageDescriptor>(Assert.Single(original.Signal!.Nodes));
        SignalNodeDescriptor[] nodes = [first, first with { Id = new("second") }, new SignalPulseCountDescriptor(new("count"), new("target"))];
        var entry = original with
        {
            Metadata = original.Metadata with { Id = part.Value, Parameters = [new("target", ParameterUnit.None, ParameterKind.WholeNumber, 1, 8, 4, [])] },
            Signal = original.Signal with { Nodes = reverse ? nodes.Reverse().ToArray() : nodes, Edges = [Edge("signal", "count"), Edge("second", "count")] },
            Events = [new(SimulationEventKind.Passage, new("signal")), new(SimulationEventKind.Passage, new("second")),
                new(SimulationEventKind.ThresholdReached, new("count"))],
            Presentation = original.Presentation with { Bindings = [] }
        };
        Assert.Empty(BundleValidator.Validate([new("apertures", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var quarter = MathF.Sqrt(.5f);
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(part, new(2), 0, .16f, 0, 0, 0, -quarter, quarter),
            WorkshopInput.Generic(new("ball"), new(3), 0, 2.88f, 0, 0, 0, 0, 1)));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        await harness.Advance(480); // Generous four-second guard for two bouncing forward passes.
        var network = harness.Simulation.Scene.Activation;
        Assert.Equal(4u, network.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        foreach (var local in new[] { "signal", "second" })
        {
            var events = harness.Of(SimulationEventKind.Passage).Where(row => row.Source == new GpuEventSourceId(Node(bench, local).Value)).ToArray();
            Assert.Equal([1u, 2u], events.Select(row => row.Count));
            Assert.All(events, row => Assert.Equal(3UL, row.Other));
            Assert.True(events[1].Ordinal > events[0].Ordinal);
            var cursor = network.Memory(harness.Committed.Memory, Node(bench, local)).Dispatched;
            Assert.Equal(events[1].Ordinal, cursor.Time.Ordinal);
        }
        Assert.Single(harness.Of(SimulationEventKind.ThresholdReached));
        var rows = harness.Committed.Memory;
        Assert.NotEqual(network.Memory(rows, Node(bench, "signal")).Node, network.Memory(rows, Node(bench, "second")).Node);
        await harness.Reset();
        Assert.All(Enumerable.Range(0, harness.Committed.Memory.Count), index => Assert.Equal(default, harness.Committed.Memory[index].Dispatched));
        await harness.Run(); await harness.Advance(480);
        Assert.Equal(4u, network.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TwoOrientationNodesObserveTheExplicitMovingChild(bool supplied)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        PartId part = new("sensed_rotor");
        var original = TestBundle.Entry(new("motor"));
        var entry = original with
        {
            Metadata = original.Metadata with { Id = part.Value, Parameters = [.. original.Metadata.Parameters,
                new("target", ParameterUnit.None, ParameterKind.WholeNumber, 1, 4, 2, [])] },
            Domains = [SimulationDomain.Mechanical, SimulationDomain.Electrical, SimulationDomain.Signal],
            Signal = new([new SignalOrientationDescriptor(new("early"), new(1), 10),
                new SignalOrientationDescriptor(new("later"), new(1), 90),
                new SignalPulseCountDescriptor(new("count"), new("target"))],
                [Edge("early", "count"), Edge("later", "count")], []),
            Events = [new(SimulationEventKind.TriggerActivated, new("early")), new(SimulationEventKind.TriggerActivated, new("later")),
                new(SimulationEventKind.ThresholdReached, new("count"))]
        };
        Assert.Empty(BundleValidator.Validate([new("rotor", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            PlacedParts.Battery(new(1), -4, 3, 0, enabled: supplied ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
            WorkshopInput.Generic(part, new(2), 0, 3, 0, 0, 0, 0, 1)),
            Connections: new(new WorkshopConnection(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        var child = bench.Instances.Identities[new(2)].Hinges[0].Body;
        Assert.Equal(2, harness.Simulation.Scene.Physics.OrientationSensors.Length);
        Assert.All(harness.Simulation.Scene.Physics.OrientationSensors.ToArray(), sensor => Assert.Equal(child, sensor.Body));
        await harness.Advance(240);
        Assert.Equal(supplied ? 2u : 0u, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        if (supplied)
        {
            foreach (var local in new[] { "early", "later" })
            {
                var activation = Assert.Single(Enumerable.Range(0, harness.Committed.Activations.Count).Select(i => harness.Committed.Activations[i]),
                    row => row.Node == Node(bench, local));
                Assert.Equal(child, activation.Body);
                Assert.Equal(new GpuBodyId(2), activation.Owner);
                Assert.Single(harness.Of(SimulationEventKind.TriggerActivated), row => row.Source == new GpuEventSourceId(activation.Node.Value));
            }
            Assert.Single(harness.Of(SimulationEventKind.ThresholdReached));
        }
        else Assert.Empty(harness.Of(SimulationEventKind.TriggerActivated));
    }
    [Theory]
    [InlineData(-2f, "left", "right")]
    [InlineData(2f, "right", "left")]
    public async Task TwoLoadFacesOnOneStaticFrameKeepTheirOwnState(float ballX, string loaded, string empty)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        PartId part = new("twin_load_faces");
        var original = TestBundle.Entry(new("pressure_plate"));
        var sensor = Assert.IsType<SignalContactLoadDescriptor>(Assert.Single(original.Signal!.Nodes));
        var geometry = BundleGeometry.Colliders(original).ToArray();
        var entry = original with
        {
            Metadata = original.Metadata with { Id = part.Value },
            Colliders = [.. geometry.Select(collider => collider with { Offset = collider.Offset - new Vector3(2, 0, 0) }),
                .. geometry.Select(collider => collider with { Offset = collider.Offset + new Vector3(2, 0, 0) })],
            ColliderBindings = [],
            Ports = original.Ports.Where(port => port.Domain == SimulationDomain.Electrical).ToArray(),
            Electrical = original.Electrical! with { Supply = original.Electrical!.Supply!.Value with
                { Control = new(new("left"), SignalContactState.Latched) } },
            Signal = new([sensor with { Id = new("left") },
                sensor with { Id = new("right"), RegionCollider = sensor.RegionCollider + geometry.Length,
                    SupportingColliders = sensor.SupportingColliders.Select(index => index + geometry.Length).ToArray() }], [], []),
            Events = [new(SimulationEventKind.ContactLoad, new("left")), new(SimulationEventKind.ContactLoad, new("right"))],
            Presentation = original.Presentation with { Bindings = [], Bodies = [], Parameters = [] }
        };
        Assert.Empty(BundleValidator.Validate([new("load_faces", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(part, new(2), 0, WorkshopConstruction.WorkbenchSurface.Value + .22f, 0, 0, 0, 0, 1,
                new([new(new("minimum_mass"), .1f)])),
            WorkshopInput.Generic(new("ball"), new(3), ballX, 2, 0, 0, 0, 0, 1)));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        await harness.Advance(240); // Two seconds permits the falling ball to settle on its selected face.
        var rows = harness.Committed.Presentation.Loads.ToArray();
        Assert.Equal(2, rows.Length);
        var identities = bench.Instances.Identities[new(2)].Signals;
        Assert.Equal(ContactLoadPhase.Loaded, Assert.Single(rows, row => row.Sensor == identities[new(loaded)].ContactLoad).Phase);
        Assert.Equal(ContactLoadPhase.Empty, Assert.Single(rows, row => row.Sensor == identities[new(empty)].ContactLoad).Phase);
        Assert.All(rows, row => Assert.Equal(new GpuBodyId(2), row.Frame));
        var activations = Enumerable.Range(0, harness.Committed.Activations.Count).Select(index => harness.Committed.Activations[index]).ToArray();
        Assert.Equal(ActivationPhase.Latched, Assert.Single(activations, row => row.Node == Node(bench, loaded)).Phase);
        Assert.Equal(ActivationPhase.Clear, Assert.Single(activations, row => row.Node == Node(bench, empty)).Phase);
        Assert.Equal(loaded == "left", harness.Simulation.Scene.Activation.ContactClosed(
            harness.Committed.Activations, harness.Committed.Timers, new(2)));
        var prior = harness.Committed;
        var foreign = rows.ToArray();
        foreign[0] = foreign[0] with { Frame = new(3) };
        var invalid = prior with { Presentation = new(prior.Presentation.Actuators, foreign, prior.Presentation.Logic,
            prior.Presentation.Ropes, prior.Presentation.Guides, prior.Presentation.Surfaces) };
        Assert.Throws<ArgumentException>(() => WorkshopMessages.ValidatePresentationOwners(invalid, harness.Simulation.Scene));
        WorkshopMessages.ValidatePresentationOwners(prior, harness.Simulation.Scene);
        Assert.Equal(ContactLoadPhase.Loaded, Assert.Single(harness.Committed.Presentation.Loads.ToArray(),
            row => row.Sensor == identities[new(loaded)].ContactLoad).Phase);
        await harness.Reset();
        Assert.All(harness.Committed.Presentation.Loads.ToArray(), row => Assert.Equal(ContactLoadPhase.Empty, row.Phase));
    }

}
