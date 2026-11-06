using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ComposedPhysicalSignalTests
{
    private static SignalEdge Edge(string source, string target, SignalTerminal input = SignalTerminal.Input) =>
        new(new(new(source), SignalTerminal.Output), new(new(target), input));
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
            var firstLatch = SelectedLatch(harness, bench, local);
            Assert.Equal(events[0].Ordinal, firstLatch.EventOrdinal);
            Assert.Equal(events[0].Phase, firstLatch.EventPhase);
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
    [InlineData(-2f, "left", "right", false)]
    [InlineData(2f, "right", "left", false)]
    [InlineData(-2f, "left", "right", true)]
    [InlineData(2f, "right", "left", true)]
    public async Task TwoLoadFacesOnOneStaticFrameKeepTheirOwnState(float ballX, string loaded, string empty, bool reverse)
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
            Presentation = original.Presentation with { Bindings =
                [LoadBinding("LeftLoad", "left", -3, 7), LoadBinding("RightLoad", "right", -5, 11),
                 ActivationBinding("LeftActivation", "left", 13, 17), ActivationBinding("RightActivation", "right", 19, 23)],
                Bodies = [], Parameters = [] }
        };
        if (reverse) entry = entry with { Signal = entry.Signal! with { Nodes = entry.Signal!.Nodes.Reverse().ToArray() },
            Presentation = entry.Presentation with { Bindings = entry.Presentation.Bindings.Reverse().ToArray() } };
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
        AssertLoadVisuals(harness, entry, loaded);
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
        AssertLoadVisuals(harness, entry, null);
    }

    private static PresentationBinding LoadBinding(string target, string signal, float from, float to) =>
        new(target, PresentationProperty.LocalX, PresentationDriver.CommittedContactLoadPhase, PresentationValueMapping.Identity,
            from, to, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0, PresentationTimerPhase.None, SignalNode: new(signal));

    private static PresentationBinding ActivationBinding(string target, string signal, float from, float to) =>
        new(target, PresentationProperty.LocalX, PresentationDriver.CommittedActivation, PresentationValueMapping.Identity,
            from, to, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0, PresentationTimerPhase.None, SignalNode: new(signal));

    private static void AssertLoadVisuals(WorkshopIntegrationHarness harness, BundleEntry entry, string? loaded)
    {
        var samples = harness.Presentation!.Advance(new(harness.Committed.Tick.Value * 4));
        for (var i = 0; i < entry.Presentation.Bindings.Count; i++)
        {
            var binding = entry.Presentation.Bindings[i];
            var value = BitConverter.UInt32BitsToSingle(Assert.Single(samples,
                sample => sample.Owner == new GpuBodyId(2) && sample.Binding == i).Value.CanonicalBits);
            Assert.Equal(loaded is not null && binding.SignalNode == new SignalNodeId(loaded) ? binding.To : binding.From, value);
        }
    }

    private static BundleEntry DelayedPassageEntry(PartId part, bool reverse)
    {
        var original = TestBundle.Entry(new("ball_detector"));
        var sensor = Assert.IsType<SignalPassageDescriptor>(Assert.Single(original.Signal!.Nodes));
        SignalNodeDescriptor[] nodes =
        [
            sensor,
            new SignalTimerDescriptor(new("delay"), new("delay_seconds"), TimerCompletionPolicy.Latch,
                TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation),
            new SignalPulseCountDescriptor(new("count"), new("target")),
            new SignalMemoryDescriptor(new("selected")),
            new SignalMemoryDescriptor(new("quiet"))
        ];
        SignalEdge[] edges = [Edge("delay", "count"), Edge("count", "selected", SignalTerminal.Set)];
        return original with
        {
            Metadata = original.Metadata with { Id = part.Value, Parameters =
                [.. original.Metadata.Parameters, new("delay_seconds", ParameterUnit.Second, ParameterKind.Real, .01f, 4f, .25f, []),
                 new("target", ParameterUnit.None, ParameterKind.WholeNumber, 1, 8, 1, [])] },
            Ports = [.. original.Ports, new(new("delay_in"), SimulationDomain.Signal, PortDirection.Input,
                PortRole.ActivationIn, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY)],
            Signal = original.Signal with { Nodes = reverse ? nodes.Reverse().ToArray() : nodes,
                Edges = reverse ? edges.Reverse().ToArray() : edges,
                Ports = [.. original.Signal.Ports, new(new("delay_in"), new(new("delay"), SignalTerminal.Input))] },
            Events = [new(SimulationEventKind.Passage, new("signal")), new(SimulationEventKind.TriggerActivated, new("delay")),
                new(SimulationEventKind.ThresholdReached, new("count")),
                new(SimulationEventKind.MemoryChanged, new("selected")), new(SimulationEventKind.MemoryChanged, new("quiet"))],
            Presentation = original.Presentation with { Bindings = [] }
        };
    }

    private static ActivationLatch SelectedLatch(WorkshopIntegrationHarness harness, WorkshopConstruction bench, string local) =>
        Assert.Single(Enumerable.Range(0, harness.Committed.Activations.Count).Select(i => harness.Committed.Activations[i]),
            row => row.Node == Node(bench, local));

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task PhysicalSensorDelaysOnlyItsSelectedMemoryInOneElement(bool reverse, bool connected)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        PartId part = new("delayed_passage_memory");
        var entry = DelayedPassageEntry(part, reverse);
        Assert.Empty(BundleValidator.Validate([new("delayed_passage", entry)]));
        var entries = PartRegistry.Read(BundleSchema.WriteEntries([entry]));
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var half = MathF.Sqrt(.5f);
        var floor = WorkshopConstruction.WorkbenchSurface.Value;
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(part, new(2), 0, floor + .62f, 0, 0, 0, -half, half),
            WorkshopInput.Generic(new("ball"), new(3), 0, floor + 3.34f, 0, 0, 0, 0, 1)),
            Connections: connected ? new(new WorkshopConnection(new(2), PortId.ActivationOut, new(2), new("delay_in"),
                WorkshopConnectionDomain.Activation)) : WorkshopConnections.Empty);
        await harness.Require(WorkshopCommandKind.Construct, bench);
        for (var run = 0; run < 2; run++)
        {
            await harness.Run();
            await AssertDelayedPassage(harness, bench, connected);
            await harness.Reset();
            Assert.Equal(bench.Instances, harness.Construction.Instances);
            foreach (var local in new[] { "signal", "delay", "count", "selected", "quiet" })
                Assert.Equal(ActivationPhase.Clear, SelectedLatch(harness, bench, local).Phase);
            Assert.Equal(ActivationTimerState.Ready(Node(bench, "delay")), harness.Committed.Timers[0]);
            Assert.Equal(0u, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
            Assert.Equal(default, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "signal")).Dispatched);
        }
    }

    private static async Task AssertDelayedPassage(WorkshopIntegrationHarness harness, WorkshopConstruction bench, bool connected)
    {
        var recordStart = harness.Records.Count;
        // Finite 1.5-second stall guard; the incidental arrival tick is not an acceptance value.
        for (var tick = 0; tick < 180 && SelectedLatch(harness, bench, "signal").Phase == ActivationPhase.Clear; tick++)
            await harness.Advance(1);
        var sensor = SelectedLatch(harness, bench, "signal");
        Assert.Equal(ActivationPhase.Latched, sensor.Phase);
        Assert.Equal(ActivationOccurrenceKind.Passage, sensor.Kind);
        Assert.Equal(ActivationCauseKind.Physical, sensor.CauseKind);
        Assert.Equal(new GpuBodyId(3), sensor.Body);
        Assert.Equal(bench.Instances.Identities[new(3)].Colliders[0], sensor.Collider);
        Assert.Equal(new GpuEventSourceId(bench.Instances.Identities[new(2)].Signals[new("signal")].Passage.Value), sensor.Source);
        Assert.Equal(ActivationPhase.Clear, SelectedLatch(harness, bench, "selected").Phase);
        var timer = harness.Committed.Timers[0];
        Assert.Equal(Node(bench, "delay"), timer.Node);
        if (connected)
        {
            Assert.Equal(ActivationTimerPhase.Counting, timer.Phase);
            Assert.Equal(30UL, timer.DueTick - timer.StartedTick); // Authored quarter-second delay at 120 Hz.
            await harness.Advance(checked((int)(timer.DueTick - harness.Committed.Tick.Value - 1)));
            Assert.Equal(ActivationPhase.Clear, SelectedLatch(harness, bench, "selected").Phase);
            await harness.Advance(1);
            var elapsed = SelectedLatch(harness, bench, "delay");
            var memory = SelectedLatch(harness, bench, "selected");
            Assert.Equal(ActivationTimerPhase.Finished, harness.Committed.Timers[0].Phase);
            Assert.Equal(ActivationPhase.Latched, memory.Phase);
            Assert.Equal(new GpuBodyId(2), memory.Owner);
            Assert.Equal(ActivationOccurrenceKind.TimerElapsed, elapsed.Kind);
            Assert.True(elapsed.Occurrence.Time > sensor.Occurrence.Time);
            Assert.Equal(sensor.Occurrence.Cause, elapsed.Occurrence.Cause);
            var threshold = SelectedLatch(harness, bench, "count");
            Assert.Equal(ActivationOccurrenceKind.ThresholdReached, threshold.Kind);
            Assert.Equal(elapsed.Occurrence.Time, threshold.Occurrence.Time);
            Assert.Equal(sensor.Occurrence.Cause, threshold.Occurrence.Cause);
            Assert.True(threshold.Occurrence.Time > threshold.Occurrence.Cause.Time);
            Assert.Throws<ArgumentException>(() => (threshold.Occurrence with
                { Time = new(threshold.Occurrence.Cause.Time.Ordinal - 1, 0) }).Validate());
            Assert.Equal(threshold.Occurrence, memory.Occurrence);
            var emitted = Assert.Single(harness.Records.Skip(recordStart), row => row.Kind == SimulationEventKind.ThresholdReached);
            Assert.Equal(new GpuEventSourceId(Node(bench, "count").Value), emitted.Source);
            Assert.Equal(elapsed.EventOrdinal, emitted.Ordinal);
            Assert.Equal(elapsed.EventPhase, emitted.Phase);
            Assert.Equal(1u, emitted.Count);
            Assert.Single(harness.Records.Skip(recordStart),
                row => row.Kind == SimulationEventKind.MemoryChanged && row.Source == new GpuEventSourceId(Node(bench, "selected").Value));
        }
        else
        {
            await harness.Advance(120); // More than the declared delay; unwired downstream nodes must remain quiet.
            Assert.Equal(ActivationTimerState.Ready(Node(bench, "delay")), harness.Committed.Timers[0]);
            Assert.Equal(ActivationPhase.Clear, SelectedLatch(harness, bench, "selected").Phase);
            Assert.Empty(harness.Of(SimulationEventKind.MemoryChanged));
            Assert.Empty(harness.Of(SimulationEventKind.ThresholdReached));
            Assert.Equal(0u, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        }
        Assert.Equal(ActivationPhase.Clear, SelectedLatch(harness, bench, "quiet").Phase);
        Assert.DoesNotContain(harness.Of(SimulationEventKind.MemoryChanged),
            row => row.Source == new GpuEventSourceId(Node(bench, "quiet").Value));
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoBodiesInOneApertureCommitBothOccurrencesAndPreserveReplayBoundaries(bool reverse)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        PartId part = new("same_commit_aperture");
        var original = TestBundle.Entry(new("ball_detector"));
        var sensor = Assert.IsType<SignalPassageDescriptor>(Assert.Single(original.Signal!.Nodes)) with { ApertureRadius = 2 };
        SignalNodeDescriptor[] nodes = [sensor, new SignalPulseCountDescriptor(new("count"), new("target")),
            new SignalPulseCountDescriptor(new("quiet"), new("target"))];
        var entry = original with
        {
            Metadata = original.Metadata with { Id = part.Value,
                Parameters = [new("target", ParameterUnit.None, ParameterKind.WholeNumber, 1, 8, 2, [])] },
            Colliders = BundleGeometry.Colliders(original).Select(c => c with { Offset = c.Offset + new Vector3(0, 5, 0) }).ToArray(),
            ColliderBindings = [],
            Signal = original.Signal with { Nodes = reverse ? nodes.Reverse().ToArray() : nodes, Edges = [Edge("signal", "count")] },
            Events = [new(SimulationEventKind.Passage, new("signal")), new(SimulationEventKind.ThresholdReached, new("count"))],
            Presentation = original.Presentation with { Bindings = [] }
        };
        Assert.Empty(BundleValidator.Validate([new("same_commit", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var q = MathF.Sqrt(.5f);
        IWorkshopInstance[] instances =
        [
            WorkshopInput.Generic(part, new(2), 0, 2, 0, 0, 0, -q, q),
            WorkshopInput.Generic(new("ball"), new(3), -.5f, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new("ball"), new(4), .5f, 4, 0, 0, 0, 0, 1)
        ];
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(reverse ? instances.Reverse().ToArray() : instances));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        var before = harness.Committed;
        var retried = false;
        for (var tick = 0; !harness.Of(SimulationEventKind.Passage).Any() && tick < 120; tick++)
        {
            before = harness.Committed;
            var node = Node(bench, "count");
            var networkAtTick = harness.Simulation.Scene.Activation;
            var staged = await harness.Device.Advance(before, new(900001), WorkshopTickControls.For(before, []));
            var stagedCount = networkAtTick.Memory(staged.Read.Memory, node).Count;
            harness.Device.Discard(staged.Sequence);
            if (stagedCount == 2)
            {
                var retry = await harness.Device.Advance(before, new(900002), WorkshopTickControls.For(before, []));
                Assert.Equal(2u, networkAtTick.Memory(retry.Read.Memory, node).Count);
                Assert.Equal(networkAtTick.Memory(staged.Read.Memory, Node(bench, "signal")),
                    networkAtTick.Memory(retry.Read.Memory, Node(bench, "signal")));
                harness.Device.Discard(retry.Sequence);
                Assert.Equal(0u, networkAtTick.Memory(harness.Committed.Memory, node).Count);
                retried = true;
            }
            var result = await harness.AdvanceOne();
            Assert.True(result.Outcome == WorkshopCommandOutcome.Applied,
                string.Join("\n", harness.Simulation.Diagnostics.Snapshot().OfType<WorkshopFaultDiagnostic>().Select(row => row.Detail)));
        }
        var events = harness.Of(SimulationEventKind.Passage).OrderBy(row => row.Other).ToArray();
        Assert.Equal(2, events.Length);
        Assert.True(retried);
        Assert.Equal(new ulong[] { 3, 4 }, events.Select(row => row.Other));
        Assert.Equal(events[0].Tick, events[1].Tick); // The interface must retain both occurrences in one commit.
        var network = harness.Simulation.Scene.Activation;
        var source = Node(bench, "signal");
        Assert.Equal(2u, network.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        Assert.Equal(0u, network.Memory(harness.Committed.Memory, Node(bench, "quiet")).Count);
        Assert.Single(harness.Of(SimulationEventKind.ThresholdReached));
        var first = SelectedLatch(harness, bench, "signal").Occurrence;
        var cursor = network.Memory(harness.Committed.Memory, source);
        Assert.Equal(new GpuBodyId(3), first.Cause.Body);
        Assert.Equal(new GpuBodyId(4), cursor.Dispatched.Cause.Body);
        Assert.Equal(new PassageSequence(2), cursor.PassageSequence);
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(harness.Latest));
        Assert.Equal(cursor, network.Memory(decoded.Read.Memory, source));
        Assert.Throws<ArgumentException>(() => (cursor with { PassageSequence = default }).Validate());
        Assert.Throws<ArgumentException>(() => (cursor with { Dispatched = default }).Validate());
        PassageSensorRead Read(ActivationOccurrence occurrence, ulong sequence) => new(
            bench.Instances.Identities[new(2)].Signals[new("signal")].Passage, new(2),
            occurrence.Cause.Body, occurrence.Cause.Collider, PassageSensorPhase.Fired,
            occurrence.Time.Ordinal, occurrence.Time.Phase, new(occurrence.Cause.ApproachSpeed.Value), new(sequence));
        var latest = Read(cursor.Dispatched, 2);
        ActivationCheckpoint Consume(WorkshopRead prior, params PassageSensorRead[] reads) =>
            network.Consume(prior.Activations, prior.Timers, [], harness.Committed.Tick, [], reads, memory: prior.Memory);
        var replay = Consume(before, Read(first, 1), latest);
        Assert.Equal(2u, network.Memory(replay.Memory, Node(bench, "count")).Count);
        Assert.Throws<ArgumentException>(() => Consume(before, latest, Read(first, 1)));
        var exhausted = new PhysicsMemoryRead(Enumerable.Range(0, harness.Committed.Memory.Count)
            .Select(i => harness.Committed.Memory[i].Node == source
                ? cursor with { PassageSequence = new(ulong.MaxValue) } : harness.Committed.Memory[i]).ToArray());
        foreach (var wrapped in new[] { latest with { Sequence = default }, latest with { Sequence = new(1) } })
            Assert.Throws<ArgumentException>(() => network.Consume(harness.Committed.Activations, harness.Committed.Timers,
                [], harness.Committed.Tick, [], [wrapped], memory: exhausted));
        Assert.Equal(new PassageSequence(ulong.MaxValue), network.Memory(exhausted, source).PassageSequence);
        var held = Consume(harness.Committed, latest);
        Assert.Equal(2u, network.Memory(held.Memory, Node(bench, "count")).Count);
        foreach (var invalid in new[] { latest with { Sequence = new(1) }, latest with { Body = new(3) },
            latest with { Sequence = new(3) }, latest with { Sequence = new(3), EventOrdinal = 0 },
            latest with { Sequence = new(3), Id = new(999999) }, latest with { Sequence = new(3), Frame = new(3) },
            latest with { Sequence = new(4) }, latest with { Sequence = new(3), EventOrdinal = checked((uint)(harness.Committed.Tick.Value * 4 + 4)) } })
            Assert.Throws<ArgumentException>(() => Consume(harness.Committed, invalid));
        Assert.Equal(2u, network.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        Assert.Equal(first, SelectedLatch(harness, bench, "signal").Occurrence);
        await harness.Reset();
        Assert.All(Enumerable.Range(0, harness.Committed.Memory.Count), i =>
        {
            Assert.Equal(default, harness.Committed.Memory[i].PassageSequence);
            Assert.Equal(default, harness.Committed.Memory[i].Dispatched);
        });
        await harness.Run();
        await harness.Advance(100);
        Assert.Equal(2u, network.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
    }
}
