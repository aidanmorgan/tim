using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>CAT-037 State Latch: catalogue, typed sockets, Reset-dominant bistable memory, switched supply route and persistence.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class StateLatchTests(NativeSceneFixture godot)
{
    private static readonly Color Slate = new("#556573");
    private static readonly Color Gold = new("#f7cb52");

    [Fact]
    public void PartRegistry_DiscoversLatchAndPresentationRestoresRestOnReset()
    {
        var registry = new PartRegistry();
        registry.Discover();
        var def = registry.Definitions[PartId.Latch];
        Assert.Equal("latch", def.Id);
        Assert.Equal("Set/Reset latch", def.Title);
        Assert.Equal("Control", def.Category);
        Assert.Empty(def.Parameters);
        Assert.Equal(24u, PartId.Latch.ToNumericId());
        Assert.Equal(PartId.Latch, PartId.FromNumericId(24));

        var part = registry.Create(PartId.Latch);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.Equal(PartId.Latch, part.PartId);
            Assert.Equal(1.2f, part.PickRadius);
            var visual = (Node3D)part.FindChild("Visual", recursive: false)!;
            foreach (var name in new[] { "Housing", "Foot", "Face", "Rocker", "IndicatorLamp", "SetMarker", "ResetMarker",
                         "Visual_Socket_SetIn", "Visual_Socket_ResetIn", "Visual_Socket_PowerIn", "Visual_Socket_Supply", "Visual_Socket_ActivationOut", "OutMarker" })
                Assert.NotNull(visual.FindChild(name, recursive: false));
            Assert.Equal(12, ((Node3D)visual.FindChild("ResetMarker", recursive: false)!).GetChildCount());
            var rocker = (Node3D)visual.FindChild("Rocker", recursive: false)!;
            var lamp = (MeshInstance3D)visual.FindChild("IndicatorLamp", recursive: false)!;

            // The latch binds exactly the Memory declaration its instance forwards; blend 1 is On, the neutral sample is Off.
            Assert.True(part.HasCosmeticBindings);
            Assert.Equal(CosmeticCurves.StateLatch, part.Cosmetic);
            Assert.Equal(CosmeticCurves.StateLatch, WorkshopInput.Latch(new(1), 0, 0, 0, 0, 0, 0, 1).Cosmetic);
            Assert.Equal(AnimationFeedbackSource.Memory, part.Cosmetic.Source);
            // CAT-037 §2: 0.125 s SmoothStep in both directions.
            Assert.Equal((Half)0.125f, part.Cosmetic.Duration);
            Assert.Equal(CuriousContraptions.Presentation.AnimationCurve.SmoothStep, part.Cosmetic.Curve);
            // Icon-only socket choices: two distinct authored pictograms.
            Assert.NotSame(WorkshopIcons.Pictogram("socket_set"), WorkshopIcons.Pictogram("socket_reset"));

            part.ApplyCosmetic(new((Half)1, CuriousContraptions.Presentation.AnimationTimerPhase.None));
            AssertColour(Gold, ((StandardMaterial3D)lamp.MaterialOverride).AlbedoColor);
            Assert.Equal(0.25f, rocker.Rotation.Z, 3);

            part.ApplyCosmetic(WorkshopCosmeticSample.Neutral);
            AssertColour(Slate, ((StandardMaterial3D)lamp.MaterialOverride).AlbedoColor);
            Assert.Equal(-0.25f, rocker.Rotation.Z, 3);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void LatchScene_ExtractsHousingFootAndFivePortLocators()
    {
        var instance = ResourceLoader.Load<PackedScene>("res://parts/scenes/latch.tscn").Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(instance);
            Assert.Equal(2, colliders.Count);
            Assert.Equal(new MetreVector((Half)0.825f, (Half)0.625f, (Half)0.325f), colliders[0].HalfExtents);
            Assert.Equal(new MetreVector((Half)0.9f, (Half)0.08f, (Half)0.425f), colliders[1].HalfExtents);
            Assert.Equal((Half)(-0.72f), colliders[1].LocalPose.Translation.Y);
            Assert.Equal(colliders.Select(c => c.HalfExtents), WorkshopCanonicalColliders.For(PartId.Latch).Select(c => c.HalfExtents));
            Assert.Equal(new Godot.Vector3(-0.45f, 0.72f, 0), ports["set_in"].Position);
            Assert.Equal(new Godot.Vector3(0.45f, 0.72f, 0), ports["reset_in"].Position);
            Assert.Equal(new Godot.Vector3(-0.93f, 0, 0), ports["power_in"].Position);
            Assert.Equal(new Godot.Vector3(0.93f, 0, 0), ports["supply"].Position);
            Assert.Equal(new Godot.Vector3(0, -0.56f, 0.38f), ports["activation_out"].Position);
        }
        finally { instance.Free(); }
    }

    [Fact]
    public void Ports_DeclareDistinctSetAndResetInputsAndSwitchedSupply()
    {
        var ports = WorkshopPorts.For(PartId.Latch).ToArray();
        Assert.Equal(
            [new WorkshopPort(WorkshopSocket.SetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
             new WorkshopPort(WorkshopSocket.ResetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
             new WorkshopPort(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input),
             new WorkshopPort(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output),
             new WorkshopPort(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output)], ports);
        Assert.Equal(2u, (uint)WorkshopSocket.SetIn);
        Assert.Equal(3u, (uint)WorkshopSocket.ResetIn);
        Assert.Equal(new System.Numerics.Vector3(-0.45f, 0.72f, 0f), WorkshopPorts.LocalPosition(PartId.Latch, WorkshopSocket.SetIn));
        Assert.Equal(new System.Numerics.Vector3(0.45f, 0.72f, 0f), WorkshopPorts.LocalPosition(PartId.Latch, WorkshopSocket.ResetIn));
        Assert.Equal(new System.Numerics.Vector3(-0.93f, 0f, 0f), WorkshopPorts.LocalPosition(PartId.Latch, WorkshopSocket.PowerIn));
        Assert.Equal(new System.Numerics.Vector3(0.93f, 0f, 0f), WorkshopPorts.LocalPosition(PartId.Latch, WorkshopSocket.Supply));
        Assert.Equal(new System.Numerics.Vector3(0f, -0.56f, 0.38f), WorkshopPorts.LocalPosition(PartId.Latch, WorkshopSocket.ActivationOut));
        Assert.Throws<ArgumentException>(() => WorkshopPorts.LocalPosition(PartId.Latch, WorkshopSocket.ActivationIn));
    }

    [Fact]
    public void Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes()
    {
        var latch = WorkshopInput.Latch(new(1), 0, 0, 0, 0, 0, 0, 1);
        var lamp = WorkshopInput.Lamp(new(2), 3, 0, 0, 0, 0, 0, 1);
        var counter = WorkshopInput.Counter(new(3), -3, 0, 0, 0, 0, 0, 1, 3);
        var hit = WorkshopInput.Switch(new(4), 0, 3, 0, 0, 0, 0, 1, ContactTriggerSettings.Default);
        var other = WorkshopInput.Latch(new(5), 0, -3, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(latch, lamp, counter, hit, other);
        var output = Signal(latch.Id, lamp.Id, WorkshopSocket.ActivationIn);
        new WorkshopConnections(output).Validate(instances);
        EditRefusal.Is(WorkshopEditRejection.LatchDrivesLampOnly, () => new WorkshopConnections(Signal(latch.Id, counter.Id, WorkshopSocket.ActivationIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.LatchDrivesLampOnly, () => new WorkshopConnections(Signal(latch.Id, other.Id, WorkshopSocket.SetIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.LampFollowsOneLatch, () => new WorkshopConnections(output, Signal(hit.Id, lamp.Id, WorkshopSocket.ActivationIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.LampFollowsOneLatch, () => new WorkshopConnections(output, Signal(other.Id, lamp.Id, WorkshopSocket.ActivationIn)).Validate(instances));

        // The save codec rejects an invalid stored shape atomically: a latch output aimed at a Counter.
        var valid = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instances, Connections: new(output));
        var bytes = WorkshopSaveCodec.Encode(new(valid, new(6)));
        var invalid = valid with { Connections = new(Signal(latch.Id, counter.Id, WorkshopSocket.ActivationIn)) };
        EditRefusal.Is(WorkshopEditRejection.LatchDrivesLampOnly, () => WorkshopSaveCodec.Encode(new(invalid, new(6))));
        Assert.Equal(output, Assert.Single(WorkshopSaveCodec.Decode(bytes).Construction.Connections.ToArray()));
    }

    [Fact]
    public void Network_LevelIndicatorMirrorsLatchOnAndOff()
    {
        var setClock = WorkshopInput.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var resetClock = WorkshopInput.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, 1.5f);
        var latch = WorkshopInput.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(4), -6, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var lamp = WorkshopInput.Lamp(new(5), 3, 0, 0, 0, 0, 0, 1);
        var sticky = WorkshopInput.Lamp(new(6), 3, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(setClock, resetClock, latch, battery, lamp, sticky),
            Connections: new(Power(battery.Id, setClock.Id), Power(battery.Id, resetClock.Id),
                Signal(setClock.Id, latch.Id, WorkshopSocket.SetIn), Signal(resetClock.Id, latch.Id, WorkshopSocket.ResetIn),
                Signal(latch.Id, lamp.Id, WorkshopSocket.ActivationIn), Signal(setClock.Id, sticky.Id, WorkshopSocket.ActivationIn)));
        construction.Validate();
        var network = WorkshopActivationCompiler.Compile(construction);
        var nodes = network.Nodes.ToArray();
        Assert.Equal(ActivationNodeKind.LevelIndicator, nodes.Single(node => node.Owner == lamp.Id).Kind);
        Assert.Equal(ActivationNodeKind.StickyFirstInput, nodes.Single(node => node.Owner == sticky.Id).Kind);
        var activations = network.Clear(); var timers = network.ClearTimers(); var memory = network.ClearMemory();
        var electrical = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery.Id, new Joules(3600f), default, ElectricalEnable.Enabled)]);
        bool Lit(GpuBodyId owner)
        {
            for (var i = 0; i < activations.Count; i++) if (activations[i].Owner == owner) return activations[i].Phase == ActivationPhase.Latched;
            throw new ArgumentException("No node.");
        }
        for (ulong tick = 1; tick <= 200; tick++)
        {
            var checkpoint = network.Consume(activations, timers, [], new(tick), [], [], electrical, memory: memory);
            activations = checkpoint.Activations; timers = checkpoint.Timers; memory = checkpoint.Memory;
            var on = network.StateLatchPhase(activations, latch.Id) == SimulationLatchPhase.On;
            Assert.Equal(on, Lit(lamp.Id)); // the level indicator mirrors the latch every tick
            if (tick == 150) { Assert.True(on); }
            if (tick == 190) { Assert.False(on); Assert.True(Lit(sticky.Id)); } // Reset at 180 darkens the follower; the sticky lamp holds
        }
    }

    [Fact]
    public void Connections_AdmitOneSourceOnBothInputsAndRejectForeignSockets()
    {
        var source = WorkshopInput.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var latch = WorkshopInput.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var lamp = WorkshopInput.Lamp(new(3), 3, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(4), -3, 2, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var second = WorkshopInput.Battery(new(5), 3, 2, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var instances = new WorkshopInstances(source, latch, lamp, battery, second);
        var set = Signal(source.Id, latch.Id, WorkshopSocket.SetIn);
        var reset = Signal(source.Id, latch.Id, WorkshopSocket.ResetIn);
        var power = Power(battery.Id, latch.Id);
        new WorkshopConnections(set, reset, power).Validate(instances);

        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(Signal(source.Id, latch.Id, WorkshopSocket.ActivationIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(Signal(source.Id, lamp.Id, WorkshopSocket.SetIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied, () => new WorkshopConnections(power, Power(second.Id, latch.Id)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(set with { Domain = WorkshopConnectionDomain.Electrical }).Validate(instances));
    }

    [Fact]
    public void Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff()
    {
        // Set clock fires every 1.0 s (ticks 120, 240, 360...), Reset clock every 1.5 s (ticks 180, 360...) at 120 Hz.
        var setClock = WorkshopInput.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var resetClock = WorkshopInput.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, 1.5f);
        var latch = WorkshopInput.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(4), -6, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(setClock, resetClock, latch, battery),
            Connections: new(Power(battery.Id, setClock.Id), Power(battery.Id, resetClock.Id),
                Signal(setClock.Id, latch.Id, WorkshopSocket.SetIn), Signal(resetClock.Id, latch.Id, WorkshopSocket.ResetIn)));
        construction.Validate();
        var network = WorkshopActivationCompiler.Compile(construction);
        Assert.Contains(network.Nodes.ToArray(), node => node.Owner == latch.Id && node.Kind == ActivationNodeKind.StateLatch);
        Assert.Equal(2, network.Edges.ToArray().Count(edge => edge.Target.Value == latch.Id.Value));

        var events = new byte[EventRingBuffer.TotalByteLength];
        EventRingBuffer.Initialize(events);
        var phases = Run(network, battery.Id, 600, events);

        Assert.All(phases.Take(119), phase => Assert.Equal(SimulationLatchPhase.Off, phase));
        Assert.All(phases.Skip(119).Take(60), phase => Assert.Equal(SimulationLatchPhase.On, phase)); // ticks 120..179 held without input
        Assert.All(phases.Skip(179).Take(60), phase => Assert.Equal(SimulationLatchPhase.Off, phase)); // Reset alone at 180
        Assert.All(phases.Skip(239).Take(120), phase => Assert.Equal(SimulationLatchPhase.On, phase)); // Set at 240, held to 359
        Assert.Equal(SimulationLatchPhase.Off, phases[359]); // ticks 360: Set and Reset together, Reset dominates
        Assert.Equal(SimulationLatchPhase.On, phases[479]);
        Assert.Equal(SimulationLatchPhase.Off, phases[539]);

        var consumer = new EventRingConsumer();
        var transitions = new List<(ulong Source, float State)>();
        while (consumer.TryRead(events, SimulationDomain.Signal, out var record, out _))
            if (record.EventKind == SimulationEventKind.LatchSet) transitions.Add((record.SourceBodyId, record.P0));
        // On@120, Off@180, On@240, Off@360, On@480, Off@540, On@600: exactly one LatchSet per transition, none for redundant requests.
        Assert.Equal([1f, 0f, 1f, 0f, 1f, 0f, 1f], transitions.Select(t => t.State).ToArray());
        Assert.All(transitions, t => Assert.Equal(latch.Id.Value, t.Source));

        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(network.Clear(), latch.Id));
    }

    [Fact]
    public void Network_OneSourceWiredToBothInputsNeverTurnsOn()
    {
        var clock = WorkshopInput.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var latch = WorkshopInput.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(3), -6, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(clock, latch, battery),
            Connections: new(Power(battery.Id, clock.Id), Signal(clock.Id, latch.Id, WorkshopSocket.SetIn), Signal(clock.Id, latch.Id, WorkshopSocket.ResetIn)));
        construction.Validate();
        var phases = Run(WorkshopActivationCompiler.Compile(construction), battery.Id, 480, default);
        Assert.All(phases, phase => Assert.Equal(SimulationLatchPhase.Off, phase));

        // Control: the same source on SetIn alone turns the latch On at its first pulse.
        var control = construction with { Connections = new(Power(battery.Id, clock.Id), Signal(clock.Id, latch.Id, WorkshopSocket.SetIn)) };
        var events = new byte[EventRingBuffer.TotalByteLength];
        EventRingBuffer.Initialize(events);
        var controlPhases = Run(WorkshopActivationCompiler.Compile(control), battery.Id, 480, events);
        Assert.Equal(SimulationLatchPhase.Off, controlPhases[118]);
        Assert.Equal(SimulationLatchPhase.On, controlPhases[119]);
        Assert.Equal(SimulationLatchPhase.On, controlPhases[479]);
        // Repeated Sets at 240, 360 and 480 while already On emit no further LatchSet.
        var consumer = new EventRingConsumer();
        var latchEvents = 0;
        while (consumer.TryRead(events, SimulationDomain.Signal, out var record, out _))
            if (record.EventKind == SimulationEventKind.LatchSet) latchEvents++;
        Assert.Equal(1, latchEvents);
    }

    [Fact]
    public void Network_SetThenResetOnAdjacentTicks_TurnsOnThenOff()
    {
        var set = Source(1, 11); var reset = Source(2, 12); var latch = Memory(3);
        var network = new ActivationNetwork([set, reset, latch],
            [new(set.Id, latch.Id, WorkshopSocket.SetIn), new(reset.Id, latch.Id, WorkshopSocket.ResetIn)]);
        var activations = network.Clear(); var timers = network.ClearTimers();
        for (ulong tick = 1; tick <= 9; tick++) Step(network, ref activations, ref timers, tick);
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(activations, latch.Owner));
        Step(network, ref activations, ref timers, 10, Hit(set, 10));
        Assert.Equal(SimulationLatchPhase.On, network.StateLatchPhase(activations, latch.Owner));
        Step(network, ref activations, ref timers, 11, Hit(reset, 11));
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(activations, latch.Owner));
        Step(network, ref activations, ref timers, 12);
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(activations, latch.Owner));
    }

    [Fact]
    public void Network_SameTickConflictResolvesOffWhicheverSourceIsOrderedFirst()
    {
        // The Reset source has the lower node id, so it propagates first; every substep and event-array order resolves Off.
        var reset = Source(1, 11); var set = Source(2, 12); var latch = Memory(3);
        foreach (var (resetOrdinal, setOrdinal) in new[] { (2u, 3u), (3u, 2u), (2u, 2u) })
            foreach (var resetFirstInBatch in new[] { true, false })
            {
                var network = new ActivationNetwork([reset, set, latch],
                    [new(reset.Id, latch.Id, WorkshopSocket.ResetIn), new(set.Id, latch.Id, WorkshopSocket.SetIn)]);
                var activations = network.Clear(); var timers = network.ClearTimers();
                var r = Hit(reset, 5, resetOrdinal); var s = Hit(set, 5, setOrdinal);
                Step(network, ref activations, ref timers, 5, resetFirstInBatch ? [r, s] : [s, r]);
                Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(activations, latch.Owner));
            }
    }

    [Fact]
    public void Network_FailedOrDiscardedTickNeverCommitsAPendingSet()
    {
        var set = Source(1, 11); var other = Source(2, 12); var latch = Memory(3);
        var network = new ActivationNetwork([set, other, latch],
            [new(set.Id, latch.Id, WorkshopSocket.SetIn), new(other.Id, latch.Id, WorkshopSocket.ResetIn)]);
        var committed = network.Clear(); var timers = network.ClearTimers();
        for (ulong tick = 1; tick <= 4; tick++) Step(network, ref committed, ref timers, tick);

        // Failed tick: the Set is accumulated, then a future occurrence in the same batch faults the whole Consume.
        Assert.Throws<ArgumentException>(() => network.Consume(committed, timers, [Hit(set, 5), Hit(other, 9)], new(5), [], []));
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(committed, latch.Owner));

        // Discarded tick: a candidate holding the Set is never committed; retrying the boundary without it stays Off.
        var candidate = network.Consume(committed, timers, [Hit(set, 5)], new(5), [], []);
        Assert.Equal(SimulationLatchPhase.On, network.StateLatchPhase(candidate.Activations, latch.Owner));
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(committed, latch.Owner));
        var retried = network.Consume(committed, timers, [], new(5), [], []);
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(retried.Activations, latch.Owner));
        var next = network.Consume(retried.Activations, retried.Timers, [], new(6), [], []);
        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(next.Activations, latch.Owner));
    }

    private static ActivationNodeDeclaration Source(ulong id, ulong trigger) =>
        new(new(id), new(id), ActivationNodeKind.ContactSource, new(trigger));
    private static ActivationNodeDeclaration Memory(ulong id) => new(new(id), new(id), ActivationNodeKind.StateLatch, default);
    // One impact inside tick `tick` (4 substeps per tick): ordinal (tick − 1) · 4 + substep.
    private static ContactTriggerRead Hit(ActivationNodeDeclaration source, ulong tick, uint substep = 2) =>
        new(source.Trigger, source.Owner, new(99), 1, new(7), (uint)((tick - 1) * 4 + substep), (Half)0, new((Half)2));
    private static void Step(ActivationNetwork network, ref PhysicsActivationRead activations, ref PhysicsTimerRead timers, ulong tick,
        params ContactTriggerRead[] hits)
    {
        var checkpoint = network.Consume(activations, timers, hits, new(tick), [], []);
        activations = checkpoint.Activations; timers = checkpoint.Timers;
    }

    [Fact]
    public void Network_RejectsUntypedOrForeignLatchEdges()
    {
        var source = new ActivationNodeDeclaration(new(1), new(1), ActivationNodeKind.ContactSource, new(9));
        var latch = new ActivationNodeDeclaration(new(2), new(2), ActivationNodeKind.StateLatch, default);
        var lamp = new ActivationNodeDeclaration(new(3), new(3), ActivationNodeKind.StickyFirstInput, default);
        _ = new ActivationNetwork([source, latch, lamp], [new(source.Id, latch.Id, WorkshopSocket.SetIn), new(source.Id, latch.Id, WorkshopSocket.ResetIn), new(source.Id, lamp.Id, WorkshopSocket.ActivationIn)]);
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, latch], [new(source.Id, latch.Id, WorkshopSocket.ActivationIn)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, lamp], [new(source.Id, lamp.Id, WorkshopSocket.SetIn)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([latch, lamp], [new(latch.Id, lamp.Id, WorkshopSocket.ActivationIn)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, latch], [new(source.Id, latch.Id, WorkshopSocket.SetIn), new(source.Id, latch.Id, WorkshopSocket.SetIn)]));
        var unrouted = new ActivationNetwork([source, latch], []);
        Assert.Throws<ArgumentException>(() => unrouted.StateLatchPhase(unrouted.Clear(), new(1)));
    }

    [Fact]
    public void Compiler_SwitchedSupplyRoutesCompileOnceAndTickInputsCloseOnlyWithContactAndEnabledBattery()
    {
        var battery = WorkshopInput.Battery(new(1), -4, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var latch = WorkshopInput.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 4, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(PartId.PoweredGate));
        var routed = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery, latch, gate),
            Connections: new(Power(battery.Id, latch.Id), new(latch.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        routed.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(routed, new(1, 2));
        var route = Assert.Single(scene.SwitchedSupplies.ToArray());
        Assert.Equal(latch.Id, route.Switch);
        Assert.Equal(gate.Id, scene.Prismatics[route.Prismatic].BodyA);
        Assert.Equal(battery.Id, scene.Electrical.Sources[route.Source].Owner);

        var on = new WorkshopTickControls([ElectricalEnable.Enabled]);
        var off = new WorkshopTickControls([ElectricalEnable.Disabled]);
        var prismatics = scene.Prismatics.Length;
        Assert.True(PhysicsTickInputs.Compose(scene.SwitchedSupplies, prismatics, on, _ => true).SwitchedSupplyClosed[route.Prismatic]);
        Assert.False(PhysicsTickInputs.Compose(scene.SwitchedSupplies, prismatics, on, _ => false).SwitchedSupplyClosed[route.Prismatic]);
        Assert.False(PhysicsTickInputs.Compose(scene.SwitchedSupplies, prismatics, off, _ => true).SwitchedSupplyClosed[route.Prismatic]);
        Assert.Throws<ArgumentException>(() => new PhysicsTickInputs([ElectricalEnable.Enabled], []).Validate(1, prismatics));
        Assert.Throws<ArgumentException>(() => new WorkshopTickControls([(ElectricalEnable)9]).Validate(1));

        // Memory without a supply, or a battery wired straight to the gate, compiles no switched route.
        var unsupplied = routed with { Connections = new(new WorkshopConnection(latch.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) };
        Assert.Empty(WorkshopPhysicsCompiler.Compile(unsupplied, new(1, 2)).SwitchedSupplies.ToArray());
        var direct = routed with { Connections = new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) };
        Assert.Empty(WorkshopPhysicsCompiler.Compile(direct, new(1, 2)).SwitchedSupplies.ToArray());

        // A Counter's supply contact compiles the same route kind: closed only while its committed value is reached.
        var counter = WorkshopInput.Counter(new(4), 0, 3, 0, 0, 0, 0, 1, 3);
        var counted = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery, counter, gate),
            Connections: new(Power(battery.Id, counter.Id), new(counter.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        counted.Validate();
        Assert.Equal(counter.Id, Assert.Single(WorkshopPhysicsCompiler.Compile(counted, new(1, 2)).SwitchedSupplies.ToArray()).Switch);
    }

    [Fact]
    public void Compiler_TwoSwitchedGatesEachFollowOnlyTheirOwnRoute()
    {
        // An unswitched gate compiles first, so neither switched route sits at prismatic index 0.
        var plain = WorkshopInput.Generic(PartId.PoweredGate, new(1), -8, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(PartId.PoweredGate));
        var batteryA = WorkshopInput.Battery(new(2), -6, 3, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var batteryB = WorkshopInput.Battery(new(3), -6, -3, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default with { Enabled = ElectricalEnable.Disabled });
        var latchA = WorkshopInput.Latch(new(4), 0, 3, 0, 0, 0, 0, 1);
        var latchB = WorkshopInput.Latch(new(5), 0, -3, 0, 0, 0, 0, 1);
        var gateA = WorkshopInput.Generic(PartId.PoweredGate, new(6), 6, 3, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(PartId.PoweredGate));
        var gateB = WorkshopInput.Generic(PartId.PoweredGate, new(7), 6, -3, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(PartId.PoweredGate));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(plain, batteryA, batteryB, latchA, latchB, gateA, gateB),
            Connections: new(Power(batteryA.Id, latchA.Id), Power(batteryB.Id, latchB.Id),
                new(latchA.Id, WorkshopSocket.Supply, gateA.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
                new(latchB.Id, WorkshopSocket.Supply, gateB.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        construction.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var routes = scene.SwitchedSupplies.ToArray();
        Assert.Equal(2, routes.Length);
        int PrismaticOf(GpuBodyId gate) => Array.FindIndex(scene.Prismatics.ToArray(), joint => joint.BodyA == gate);
        int SourceOf(GpuBodyId battery) => Array.FindIndex(scene.Electrical.Sources.ToArray(), source => source.Owner == battery);
        Assert.Equal(0, PrismaticOf(plain.Id));
        var routeA = Assert.Single(routes, route => route.Switch == latchA.Id);
        var routeB = Assert.Single(routes, route => route.Switch == latchB.Id);
        Assert.Equal(PrismaticOf(gateA.Id), routeA.Prismatic); Assert.NotEqual(0, routeA.Prismatic);
        Assert.Equal(PrismaticOf(gateB.Id), routeB.Prismatic); Assert.NotEqual(0, routeB.Prismatic);
        Assert.Equal(SourceOf(batteryA.Id), routeA.Source);
        Assert.Equal(SourceOf(batteryB.Id), routeB.Source);

        bool[] Closed(ElectricalEnable a, ElectricalEnable b, Func<GpuBodyId, bool> contact)
        {
            var enables = new ElectricalEnable[scene.Electrical.Sources.Length];
            enables[SourceOf(batteryA.Id)] = a; enables[SourceOf(batteryB.Id)] = b;
            return PhysicsTickInputs.Compose(scene.SwitchedSupplies, scene.Prismatics.Length, new(enables), contact).SwitchedSupplyClosed;
        }
        // Both latches On, battery A enabled and battery B disabled (its authored state): only gate A is supplied; the plain gate never is.
        var both = Closed(ElectricalEnable.Enabled, ElectricalEnable.Disabled, _ => true);
        Assert.False(both[PrismaticOf(plain.Id)]); Assert.True(both[routeA.Prismatic]); Assert.False(both[routeB.Prismatic]);
        // Each gate follows its own latch: only latch B closed, both batteries enabled → only gate B.
        var onlyB = Closed(ElectricalEnable.Enabled, ElectricalEnable.Enabled, owner => owner == latchB.Id);
        Assert.False(onlyB[routeA.Prismatic]); Assert.True(onlyB[routeB.Prismatic]);
        // Each gate follows its own battery: latch A closed but battery A disabled, battery B enabled → neither gate.
        var crossed = Closed(ElectricalEnable.Disabled, ElectricalEnable.Enabled, owner => owner == latchA.Id);
        Assert.False(crossed[routeA.Prismatic]); Assert.False(crossed[routeB.Prismatic]);
    }

    [Fact]
    public void Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach()
    {
        var clock = WorkshopInput.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var latch = WorkshopInput.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var counter = WorkshopInput.Counter(new(3), 0, 3, 0, 0, 0, 0, 1, 2);
        var battery = WorkshopInput.Battery(new(4), -6, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(clock, latch, counter, battery),
            Connections: new(Power(battery.Id, clock.Id), Signal(clock.Id, latch.Id, WorkshopSocket.SetIn),
                Signal(clock.Id, counter.Id, WorkshopSocket.ActivationIn)));
        var network = WorkshopActivationCompiler.Compile(construction);
        var activations = network.Clear(); var timers = network.ClearTimers(); var memory = network.ClearMemory();
        var electrical = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery.Id, new Joules(3600f), default, ElectricalEnable.Enabled)]);
        var latchClosedAt = 0; var counterClosedAt = 0;
        for (var tick = 1; tick <= 300; tick++)
        {
            var checkpoint = network.Consume(activations, timers, [], new((ulong)tick), [], [], electrical, memory: memory);
            activations = checkpoint.Activations; timers = checkpoint.Timers; memory = checkpoint.Memory;
            if (latchClosedAt == 0 && network.ContactClosed(activations, latch.Id)) latchClosedAt = tick;
            if (counterClosedAt == 0 && network.ContactClosed(activations, counter.Id)) counterClosedAt = tick;
        }
        Assert.Equal(120, latchClosedAt);   // first pulse sets the latch
        Assert.Equal(240, counterClosedAt); // the counter (target 2) closes on the second pulse, never on elapsed time alone
        Assert.Throws<ArgumentException>(() => network.ContactClosed(activations, clock.Id));
    }

    [Fact]
    public void Compiler_DeclaresStaticLatchBodyWithCanonicalColliders()
    {
        var latch = WorkshopInput.Latch(new(7), 1, 0, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(latch));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == latch.Id);
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        var colliders = scene.Colliders.ToArray().Where(c => c.Body == latch.Id).ToArray();
        Assert.Equal(WorkshopCanonicalColliders.LatchColliders.Select(c => c.HalfExtents), colliders.Select(c => c.HalfExtents));
        Assert.Equal(WorkbenchFootprint.Of(PartId.Latch).Colliders, colliders.Length);
    }

    [Fact]
    public void Persistence_BinaryFlatBuffersAndSaveCodecPreserveDistinctSockets()
    {
        var setSwitch = WorkshopInput.Switch(new(1), -3, 0, 0, 0, 0, 0, 1, ContactTriggerSettings.Default);
        var resetSwitch = WorkshopInput.Switch(new(2), -3, 2, 0, 0, 0, 0, 1, ContactTriggerSettings.Default);
        var latch = WorkshopInput.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(4), -5, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(5), 4, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(PartId.PoweredGate));
        var construction = new WorkshopConstruction(new(9), WorkshopCadenceSettings.Default(), new(setSwitch, resetSwitch, latch, battery, gate),
            Connections: new(Signal(setSwitch.Id, latch.Id, WorkshopSocket.SetIn), Signal(resetSwitch.Id, latch.Id, WorkshopSocket.ResetIn),
                Power(battery.Id, latch.Id), new(latch.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        construction.Validate();

        Span<byte> binary = stackalloc byte[WorkshopWire.ConstructionBytes];
        WorkshopWire.WriteConstruction(binary, construction);
        AssertSame(construction, WorkshopWire.ReadConstruction(binary));

        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, construction,
            Session: new RuntimeSessionId(1, 2), Cadence: new(1), Projection: new(1));
        AssertSame(construction, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Construction!.Value);

        var saved = new WorkshopSavedConstruction(construction, new(6));
        var restored = WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved));
        AssertSame(construction, restored.Construction);
        Assert.Equal(WorkshopSaveCodec.Encode(saved), WorkshopSaveCodec.Encode(restored));
    }

    // Cosmetic channels travel as binary16 animation values, so colours match the palette to Half precision.
    private static void AssertColour(Color expected, Color actual)
    {
        Assert.Equal(expected.R, actual.R, 2);
        Assert.Equal(expected.G, actual.G, 2);
        Assert.Equal(expected.B, actual.B, 2);
    }

    private static void AssertSame(WorkshopConstruction expected, WorkshopConstruction actual)
    {
        Assert.Equal(expected.Connections.ToArray(), actual.Connections.ToArray());
        var latch = Assert.Single(actual.Instances.OfType<WorkshopLatch>());
        Assert.Equal(expected.Instances.OfType<WorkshopLatch>().Single(), latch);
    }

    private static SimulationLatchPhase[] Run(ActivationNetwork network, GpuBodyId battery, int ticks, byte[]? events)
    {
        var latchOwner = network.Nodes.ToArray().Single(node => node.Kind == ActivationNodeKind.StateLatch).Owner;
        var activations = network.Clear();
        var timers = network.ClearTimers();
        var memory = network.ClearMemory();
        var electrical = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery, new Joules(3600f), default, ElectricalEnable.Enabled)]);
        var phases = new SimulationLatchPhase[ticks];
        for (var tick = 1; tick <= ticks; tick++)
        {
            var checkpoint = network.Consume(activations, timers, [], new((ulong)tick), [], [], electrical, events, memory);
            activations = checkpoint.Activations; timers = checkpoint.Timers; memory = checkpoint.Memory;
            phases[tick - 1] = network.StateLatchPhase(activations, latchOwner);
        }
        return phases;
    }

    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);

    private static WorkshopConnection Power(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
}
