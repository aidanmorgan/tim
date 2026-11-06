using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class IndependentSignalCompositionTests
{
    public enum Composition { Bodyless, Dynamic, Jointed }
    private static readonly PartId Clock = new("independent_clock"), Counter = new("independent_count"),
        Memory = new("independent_memory"), Delay = new("independent_delay");

    private static BundleEntry Logical(string original, PartId id, int order, bool electrical = false)
    {
        var entry = TestBundle.Entry(new(original));
        return entry with
        {
            Metadata = entry.Metadata with { Id = id.Value, PaletteOrder = order },
            Mechanical = null, Colliders = [], ColliderBindings = [], Footprint = default,
            Electrical = electrical ? entry.Electrical : null,
            Ports = entry.Ports.Where(port => electrical || port.Domain != SimulationDomain.Electrical).ToArray(),
            Presentation = entry.Presentation with { Bodies = [], Spans = [], Parameters = [] }
        };
    }

    internal static BundleEntry Compose(BundleEntry entry, Composition composition)
    {
        if (composition == Composition.Bodyless) return entry;
        var mechanical = TestBundle.Entry(new(composition == Composition.Dynamic ? "weight" : "motor")).Mechanical!;
        mechanical = mechanical with
        {
            MassFrame = composition == Composition.Jointed ? mechanical.MassFrame :
                MassFrameDeclaration.Constant(1f, Vector3.Zero, new(.1f), Quaternion.Identity, 0f, new(0, -1, 0)),
            Hinges = mechanical.Hinges.Select(hinge => hinge with { Drive = null,
                MovingBody = hinge.MovingBody with { MassFrame =
                    MassFrameDeclaration.Constant(1f, new(.1f, 0, 0), new(.1f), Quaternion.Identity, 0f, new(0, -1, 0)) } }).ToArray()
        };
        return entry with
        {
            Mechanical = mechanical,
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Box, new(.2f), Vector3.Zero,
                Quaternion.Identity, new(0f, .3f, 0f, .1f), 0f)],
            Footprint = composition == Composition.Dynamic ? new(1, 1, 0, 0) : new(2, 1, 0, 1),
            Presentation = entry.Presentation with { Bodies = composition == Composition.Jointed ?
                [new("Shaft", new(1), Vector3.Zero)] : [] }
        };
    }

    private static BundleEntry[] Entries(Composition composition) =>
    [
        Logical("clock", Clock, 301, true),
        Compose(Logical("counter", Counter, 302), composition),
        Compose(Logical("latch", Memory, 303), composition),
        Logical("delay", Delay, 304)
    ];

    private static WorkshopGenericInstance Instance(PartId part, ulong id, float x, string? parameter = null, float value = 0f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, part, new(id), x, 4f, 0f, 0f, 0f, 0f, 1f,
            parameter is null ? null : new InstanceParameterCollection([new(new(parameter), value)]));

    private static WorkshopConstruction Bench(bool supplied) => new(new(2), WorkshopCadenceSettings.Default(),
        new(PlacedParts.Battery(new(1), -6, 4, 0, enabled: supplied ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
            Instance(Clock, 2, -3, "interval_seconds", .1f), Instance(Counter, 3, 0, "target_count", 2f),
            Instance(Delay, 4, 3, "delay_seconds", .1f), Instance(Memory, 5, 6)),
        Connections: new(
            new(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(2), PortId.ActivationOut, new(3), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
            new(new(3), PortId.ActivationOut, new(4), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
            new(new(4), PortId.ActivationOut, new(5), PortId.SetIn, WorkshopConnectionDomain.Activation)));

    private static ActivationLatch Latch(WorkshopRead read, ulong owner) =>
        Assert.Single(Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]), row => row.Owner == new GpuBodyId(owner));

    [Theory]
    [InlineData(Composition.Bodyless, true)]
    [InlineData(Composition.Dynamic, true)]
    [InlineData(Composition.Jointed, true)]
    [InlineData(Composition.Bodyless, false)]
    [InlineData(Composition.Dynamic, false)]
    [InlineData(Composition.Jointed, false)]
    public async Task IndependentLogicCompilesRunsAndRetainsNodeProvenanceAcrossTheCascade(Composition composition, bool supplied)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        await using var replay = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = PartRegistry.Read(RuntimeTestServices.Bundle, BundleSchema.WriteEntries(Entries(composition)));
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        var authored = Bench(supplied);
        await harness.Require(WorkshopCommandKind.Construct, authored);
        var scene = harness.Simulation.Scene;
        Assert.Contains(new GpuBodyId(2), scene.Owners);
        Assert.DoesNotContain(scene.Physics.Bodies.ToArray(), body => body.Id == new GpuBodyId(2) || body.Id == new GpuBodyId(4));
        if (composition == Composition.Bodyless)
            Assert.DoesNotContain(scene.Physics.Bodies.ToArray(), body => body.Id == new GpuBodyId(3) || body.Id == new GpuBodyId(5));
        else
        {
            Assert.Contains(scene.Physics.Bodies.ToArray(), body => body.Id == new GpuBodyId(3));
            Assert.Equal(composition == Composition.Jointed ? 2 : 0, scene.Physics.Revolutes.Length);
        }
        using (var projected = WorkshopObservationProjection.Construction<WorkshopObservationTests.ExternalObject>(
            new(scene.Identity, harness.Command(WorkshopCommandKind.Run), authored)))
        {
            var instances = Assert.IsType<WorkshopObservationTests.ExternalObject>(projected.Fields["instances"]);
            var clock = Assert.Single(instances.Items, item => Equals(item.Fields["id"], "2"));
            var local = Assert.IsType<WorkshopObservationTests.ExternalObject>(clock.Fields["local"]);
            Assert.Equal(authored.Instances.Single(instance => instance.Id == new GpuBodyId(2)).Local.X, local.Fields["x"]);
        }
        var movingBody = composition == Composition.Jointed
            ? authored.Instances.Identities[new(5)].Hinges[0].Body : new GpuBodyId(5);
        var initialFound = harness.Committed.Bodies.TryGet(movingBody, out var initialBody);
        if (composition != Composition.Bodyless) Assert.True(initialFound);
        await harness.Run(); await harness.Advance(60);
        if (composition != Composition.Bodyless)
        {
            Assert.True(harness.Committed.Bodies.TryGet(movingBody, out var moved));
            // Over half a second, a 5 cm fall or 0.03 rad hinge turn demonstrates actual motion,
            // excluding admitted-but-frozen logic owners without pinning the solver trajectory.
            if (composition == Composition.Dynamic)
                Assert.True((initialBody.Body.Cell.Y + initialBody.Body.Local.Y - moved.Body.Cell.Y - moved.Body.Local.Y) / 16f > .05f);
            else
            {
                var before = new Quaternion(initialBody.Rotation.X, initialBody.Rotation.Y, initialBody.Rotation.Z, initialBody.Rotation.W);
                var after = new Quaternion(moved.Rotation.X, moved.Rotation.Y, moved.Rotation.Z, moved.Rotation.W);
                Assert.True(2 * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(before, after)), 0, 1)) > .03f);
            }
        }
        Assert.Equal(supplied ? ActivationPhase.Latched : ActivationPhase.Clear, Latch(harness.Committed, 5).Phase);
        var network = scene.Activation;
        Assert.Equal(supplied ? 5U : 0U, network.Memory(harness.Committed.Memory, SignalTestNodes.Only(network, new(3))).Count);
        if (supplied)
        {
            foreach (var id in new ulong[] { 2, 3, 4, 5 })
            {
                var row = Latch(harness.Committed, id);
                Assert.Equal(ActivationCauseKind.Node, row.CauseKind);
                Assert.Equal(new GpuEventSourceId(harness.Construction.Instances.Identities[new(2)].Signals[new("signal")].Node.Value), row.Source);
                Assert.Equal(new GpuBodyId(2), row.Body);
                Assert.Equal(default, row.Collider);
                Assert.Equal(0f, row.ApproachSpeed.Value);
            }
            Assert.True(Latch(harness.Committed, 4).Occurrence.Time > Latch(harness.Committed, 4).Occurrence.Cause.Time);
            Assert.Contains(harness.Records, record => record.Kind == SimulationEventKind.MemoryChanged && record.Source.Value == harness.Construction.Instances.Identities[new(5)].Signals[new("signal")].Node.Value && record.Level == 1);
            Assert.Equal(5, harness.Records.Count(record => record.Kind == SimulationEventKind.TriggerActivated && record.Source.Value == harness.Construction.Instances.Identities[new(2)].Signals[new("signal")].Node.Value));
            Assert.Contains(harness.Presentation!.Advance(new(240)), sample => sample.Owner == new GpuBodyId(2));
            AssertProjectedProvenance(harness.Latest, ActivationCauseKind.Node, new(harness.Construction.Instances.Identities[new(2)].Signals[new("signal")].Node.Value), new(2), default);
        }
        else Assert.DoesNotContain(harness.Records, record => record.Kind == SimulationEventKind.TriggerActivated && record.Source.Value == harness.Construction.Instances.Identities[new(2)].Signals[new("signal")].Node.Value);
        await harness.Require(WorkshopCommandKind.Pause);
        var paused = harness.Committed.Tick;
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(paused.Value + 1, harness.Committed.Tick.Value);
        await harness.Require(WorkshopCommandKind.Resume); await harness.Advance(2);
        await harness.Reset();
        Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, 5).Phase);
        Assert.Equal(authored.Instances, harness.Construction.Instances);
        var saved = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Concat(entries.Select(entry => new PartId(entry.Metadata.Id))).ToHashSet(),
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(authored, new(42))));
        Assert.Equal(authored.Instances, saved.Construction.Instances);
        await replay.Require(WorkshopCommandKind.Construct, saved.Construction);
        await replay.Run(); await replay.Advance(60);
        Assert.Equal(supplied ? ActivationPhase.Latched : ActivationPhase.Clear, Latch(replay.Committed, 5).Phase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ElectricalFreeCountAcceptsTwelveAndCrossesOnceFromExternalPulses(bool connected)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = Entries(Composition.Bodyless);
        entries[1] = entries[1] with { Metadata = entries[1].Metadata with
        {
            Parameters = entries[1].Metadata.Parameters.Select(parameter => parameter.Key == "target_count"
                ? parameter with { Maximum = 12, Default = 12 } : parameter).ToArray()
        } };
        Assert.Null(entries[1].Electrical);
        Assert.DoesNotContain(entries[1].Ports, port => port.Domain == SimulationDomain.Electrical);
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        var bench = Bench(true);
        bench = bench with
        {
            Instances = new(bench.Instances.Select(instance => instance.Id == new GpuBodyId(3)
                ? Instance(Counter, 3, 0, "target_count", 12) : instance).ToArray()),
            Connections = new(bench.Connections.Where(link => connected || link.Target != new GpuBodyId(3)).ToArray())
        };
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        var node = bench.Instances.Identities[new(3)].Signals[new("signal")].Node;
        uint Count() => harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, node).Count;
        Assert.Equal(0u, Count());
        await harness.Advance(132); // Eleven deliberate 0.1-second input periods.
        Assert.Equal(connected ? 11u : 0u, Count());
        Assert.Empty(harness.Of(SimulationEventKind.ThresholdReached));
        await harness.Advance(12);
        Assert.Equal(connected ? 12u : 0u, Count());
        if (connected)
        {
            var crossed = Assert.Single(harness.Of(SimulationEventKind.ThresholdReached));
            Assert.Equal(new GpuEventSourceId(node.Value), crossed.Source);
            Assert.Equal(12u, crossed.Count);
            var cause = Latch(harness.Committed, 3);
            Assert.Equal(ActivationCauseKind.Node, cause.CauseKind);
            Assert.Equal(new GpuBodyId(2), cause.Body);
            Assert.Equal(default, cause.Collider);
        }
        await harness.Advance(24);
        Assert.Equal(connected ? 14u : 0u, Count());
        Assert.Equal(connected ? 1 : 0, harness.Of(SimulationEventKind.ThresholdReached).Count());
        await harness.Reset();
        Assert.Equal(0u, Count());
        Assert.Equal(bench.Instances, harness.Construction.Instances);
    }

    [Fact]
    public async Task BodylessOscillatorLosesSupplyAndRestartsAFreshInterval()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = Entries(Composition.Bodyless);
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        await harness.Require(WorkshopCommandKind.Construct, Bench(true));
        await harness.Run(); await harness.Advance(11);
        async Task Enable(ElectricalEnable enabled) => await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, new(1), RunControlValue.Of(enabled)));
        await Enable(ElectricalEnable.Disabled); await harness.Advance(13);
        var network = harness.Simulation.Scene.Activation;
        Assert.Equal(0U, network.Memory(harness.Committed.Memory, SignalTestNodes.Only(network, new(2))).Count);
        Assert.Equal(OscillatorPhase.Stopped, network.Memory(harness.Committed.Memory, SignalTestNodes.Only(network, new(2))).Phase);
        await Enable(ElectricalEnable.Enabled); await harness.Advance(1);
        var restarted = harness.Committed.Tick.Value;
        Assert.Equal(restarted + 12, network.Memory(harness.Committed.Memory, SignalTestNodes.Only(network, new(2))).DueTick);
        await harness.Advance(11);
        Assert.Equal(0U, network.Memory(harness.Committed.Memory, SignalTestNodes.Only(network, new(2))).Count);
        await harness.Advance(1);
        Assert.Equal(1U, network.Memory(harness.Committed.Memory, SignalTestNodes.Only(network, new(2))).Count);
        Assert.Single(harness.Records, record => record.Source.Value == harness.Construction.Instances.Identities[new(2)].Signals[new("signal")].Node.Value && record.Kind == SimulationEventKind.TriggerActivated);
    }

    public enum CauseDefect { AbsentKind, UnknownKind, Collider, Speed, UnknownNode, WrongNode, UnknownOwner, PhysicalWithoutCollider, AbsentTimerKind, ReadyTimerWithCause, FractionalNodeTime, NonboundaryNodeOrdinal }
    [Theory]
    [InlineData(CauseDefect.AbsentKind)]
    [InlineData(CauseDefect.UnknownKind)]
    [InlineData(CauseDefect.Collider)]
    [InlineData(CauseDefect.Speed)]
    [InlineData(CauseDefect.UnknownNode)]
    [InlineData(CauseDefect.WrongNode)]
    [InlineData(CauseDefect.UnknownOwner)]
    [InlineData(CauseDefect.PhysicalWithoutCollider)]
    [InlineData(CauseDefect.AbsentTimerKind)]
    [InlineData(CauseDefect.ReadyTimerWithCause)]
    [InlineData(CauseDefect.FractionalNodeTime)]
    [InlineData(CauseDefect.NonboundaryNodeOrdinal)]
    public async Task MalformedNodeProvenanceAndForeignInstalledOwnersRefuseAtomically(CauseDefect defect)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = Entries(Composition.Bodyless);
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        await harness.Require(WorkshopCommandKind.Construct, Bench(true));
        await harness.Run(); await harness.Advance(60);
        var before = harness.Committed;
        var bytes = WorkshopWireFlatBuffers.EncodeResponse(harness.Latest with { Scene = harness.Simulation.Scene });
        var buffer = new Google.FlatBuffers.ByteBuffer(bytes);
        var vector = Field(buffer, 4 + buffer.GetInt(4), 16);
        var origin = vector + buffer.GetInt(vector) + 4; // First ordered ActivationRecord, the clock.
        switch (defect)
        {
            case CauseDefect.AbsentTimerKind:
            case CauseDefect.ReadyTimerWithCause:
                var timers = Field(buffer, 4 + buffer.GetInt(4), 17);
                var timer = timers + buffer.GetInt(timers) + 4;
                if (defect == CauseDefect.AbsentTimerKind) buffer.PutUint(timer + 88, 0);
                else buffer.PutUint(timer + 8, (uint)ActivationTimerPhase.Ready);
                break;
            case CauseDefect.FractionalNodeTime:
                buffer.PutFloat(origin + 48, .5f); buffer.PutFloat(origin + 76, .5f); break;
            case CauseDefect.NonboundaryNodeOrdinal:
                buffer.PutUint(origin + 44, 47); buffer.PutUint(origin + 72, 47); break;
            case CauseDefect.AbsentKind: buffer.PutUint(origin + 80, 0); break;
            case CauseDefect.UnknownKind: buffer.PutUint(origin + 80, 99); break;
            case CauseDefect.Collider: buffer.PutUlong(origin + 32, 1); break;
            case CauseDefect.Speed: buffer.PutFloat(origin + 52, 1f); break;
            case CauseDefect.UnknownNode: buffer.PutUlong(origin + 16, 999); break;
            case CauseDefect.WrongNode: buffer.PutUlong(origin + 16, 3); break;
            case CauseDefect.UnknownOwner: buffer.PutUlong(origin + 24, 999); break;
            case CauseDefect.PhysicalWithoutCollider: buffer.PutUint(origin + 80, (uint)ActivationCauseKind.Physical); break;
        }
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(bytes)).Reason);
        var command = harness.Command(WorkshopCommandKind.Construct, Bench(true) with { Revision = new(3) });
        var request = WorkshopWireFlatBuffers.EncodeCommand(command);
        buffer = new(request);
        var sceneSlot = Field(buffer, 4 + buffer.GetInt(4), 11);
        var scene = sceneSlot + buffer.GetInt(sceneSlot);
        var nodes = Field(buffer, scene, 22);
        var first = nodes + buffer.GetInt(nodes) + 4;
        var node = first + buffer.GetInt(first);
        buffer.PutUlong(Field(buffer, node, 1), 999); // Foreign logical owner, even without a physical-body prerequisite.
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(request)).Reason);
        Assert.Equal(before.Revision, harness.Committed.Revision);
        Assert.Equal(before.Epoch, harness.Committed.Epoch);
        Assert.Equal(before.Tick, harness.Committed.Tick);
        await harness.Advance(12);
        Assert.Equal(6U, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, SignalTestNodes.Only(harness.Simulation.Scene.Activation, new(3))).Count);
    }

    private static int Field(Google.FlatBuffers.ByteBuffer buffer, int table, int field)
    {
        var offset = buffer.GetUshort(table - buffer.GetInt(table) + 4 + 2 * field);
        Assert.NotEqual(0, offset);
        return table + offset;
    }

    [Fact]
    public void LogicalAdmissionStillRefusesMissingPhysicalGeometryAndContradictoryFootprints()
    {
        var logical = Logical("clock", Clock, 301, true);
        Assert.Empty(BundleValidator.Validate([new("logical", logical)]));
        Assert.Contains(BundleValidator.Validate([new("physical", logical with { Footprint = new(1, 0, 0, 0) })]),
            error => error.Code == BundleErrorCode.BodyWithoutCollider);
        Assert.Contains(BundleValidator.Validate([new("contradictory", logical with { Colliders = TestBundle.Entry(new("clock")).Colliders })]),
            error => error.Code == BundleErrorCode.OutOfRange);
        Assert.Throws<ArgumentException>(() => BundleGeometry.Colliders(logical with { Footprint = new(1, 0, 0, 0) }));
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ASeparatelyDeclaredBodylessMemoryContactControlsItsElectricalConsumer(bool connected)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = Entries(Composition.Bodyless);
        entries[2] = Logical("latch", Memory, 303, true);
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        var bench = Bench(true);
        var links = bench.Connections.Where(link => connected || link.Target != new GpuBodyId(5)).ToList();
        links.Add(new(new(1), PortId.Supply, new(5), PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        links.Add(new(new(5), PortId.Supply, new(6), PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        bench = bench with
        {
            Instances = new([.. bench.Instances, WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(6), 9, 4, 0, 0, 0, 0, 1)]),
            Connections = new(links.ToArray())
        };
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(90);
        Assert.Equal(connected ? ActivationPhase.Latched : ActivationPhase.Clear, Latch(harness.Committed, 5).Phase);
        Assert.True(harness.Committed.Bodies.TryGet(bench.Instances.Identities[new(6)].Hinges[0].Body, out var shaft));
        if (connected) Assert.True(MathF.Sqrt(shaft.AngularVelocity.X * shaft.AngularVelocity.X + shaft.AngularVelocity.Y * shaft.AngularVelocity.Y + shaft.AngularVelocity.Z * shaft.AngularVelocity.Z) > 1f, "The explicitly declared contact must supply its motor.");
        else Assert.InRange(MathF.Sqrt(shaft.AngularVelocity.X * shaft.AngularVelocity.X + shaft.AngularVelocity.Y * shaft.AngularVelocity.Y + shaft.AngularVelocity.Z * shaft.AngularVelocity.Z), 0f, .2f);
    }


    private static void AssertProjectedProvenance(WorkshopResponse response, ActivationCauseKind kind,
        GpuEventSourceId source, GpuBodyId body, GpuColliderId collider)
    {
        using var projected = WorkshopObservationProjection.Response<WorkshopObservationTests.ExternalObject>(RuntimeTestServices.Core.Presentation, response);
        void Check(WorkshopObservationTests.ExternalObject row, ActivationCause cause)
        {
            Assert.Equal((int)kind, row.Fields["causeKind"]);
            Assert.Equal(source.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Fields["source"]);
            Assert.Equal(body.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Fields["body"]);
            Assert.Equal(collider.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Fields["collider"]);
            Assert.Equal(cause.Time.Ordinal, row.Fields["causeOrdinal"]);
            Assert.Equal(cause.Time.Phase, row.Fields["causePhase"]);
            Assert.Equal(cause.ApproachSpeed.Value, row.Fields["approachSpeed"]);
        }
        var activations = Assert.IsType<WorkshopObservationTests.ExternalObject>(projected.Fields["activations"]);
        for (var i = 0; i < response.Read.Activations.Count; i++)
            if (response.Read.Activations[i].Phase == ActivationPhase.Latched)
                Check(activations.Items[i], response.Read.Activations[i].Occurrence.Cause);
        var timers = Assert.IsType<WorkshopObservationTests.ExternalObject>(projected.Fields["timers"]);
        Assert.NotEmpty(timers.Items);
        for (var i = 0; i < response.Read.Timers.Count; i++)
            if (response.Read.Timers[i].Phase != ActivationTimerPhase.Ready)
                Check(Assert.IsType<WorkshopObservationTests.ExternalObject>(timers.Items[i].Fields["input"]),
                    response.Read.Timers[i].Input.Cause);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task APhysicalPassageCanCompleteADelayWithoutChangingItsOrigin(bool connected)
    {
        var floor = WorkshopConstruction.WorkbenchSurface.Value;
        var half = MathF.Sqrt(.5f);
        var detector = PlacedParts.Detector(new(1), 0, floor + .62f, 0, 0, 0, -half, half);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(3), 0, floor + 3.34f, 0, 0, 0, 0, 1);
        var delay = Instance(new("delay"), 4, 4, "delay_seconds", .1f);
        var lamp = PlacedParts.Lamp(new(5), 7, 4, 0, 0, 0, 0, 1);
        var links = new List<WorkshopConnection>
        {
            new(new(4), PortId.ActivationOut, new(5), PortId.ActivationIn, WorkshopConnectionDomain.Activation)
        };
        if (connected) links.Add(new(new(1), PortId.ActivationOut, new(4), PortId.ActivationIn, WorkshopConnectionDomain.Activation));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(detector, ball, delay, lamp), Connections: new(links.ToArray()));
        await using var harness = await WorkshopIntegrationHarness.Running(bench);
        await harness.Advance(180); // A generous 1.5 seconds for the drop and authored 0.1-second delay.
        var passage = Latch(harness.Committed, 1);
        Assert.Equal(ActivationOccurrenceKind.Passage, passage.Kind);
        Assert.Equal(ActivationCauseKind.Physical, passage.CauseKind);
        Assert.Equal(new GpuBodyId(3), passage.Body);
        Assert.Equal(connected ? ActivationPhase.Latched : ActivationPhase.Clear, Latch(harness.Committed, 5).Phase);
        if (!connected)
        {
            Assert.Equal(ActivationTimerPhase.Ready, harness.Committed.Timers[0].Phase);
            return;
        }
        var completed = Latch(harness.Committed, 4);
        Assert.Equal(ActivationOccurrenceKind.TimerElapsed, completed.Kind);
        Assert.Equal(passage.Occurrence.Cause, completed.Occurrence.Cause);
        Assert.True(completed.Occurrence.Time > passage.Occurrence.Time);
        Assert.Equal(completed.Occurrence, Latch(harness.Committed, 5).Occurrence);
        Assert.Equal(ActivationTimerPhase.Finished, harness.Committed.Timers[0].Phase);
        AssertProjectedProvenance(harness.Latest, ActivationCauseKind.Physical,
            new(bench.Instances.Identities[new(1)].Signals[new("signal")].Passage.Value), new(3), bench.Instances.Identities[new(3)].Colliders[0]);
    }

}
