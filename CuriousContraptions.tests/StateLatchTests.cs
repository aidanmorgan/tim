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

    [Fact]
    public void IdenticalSocketsWithoutDeclaredSupplyCapabilityNeverSupply()
    {
        var battery = PlacedParts.Battery(new(1), -4, 0, 0);
        var latch = PlacedParts.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 4, 0, 0, 0, 0, 0, 1);
        var routed = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery, latch, gate),
            Connections: new(Power(battery.Id, latch.Id), Power(latch.Id, gate.Id)));
        Assert.Equal(new[]{battery.Id}, SupplyTopology.Create(routed,DeclaredPhysics.Of).UpstreamSources(gate.Id));
        foreach (var removed in new[] { battery.PartId, latch.PartId })
        {
            CuriousContraptions.Bundle.BundleEntry Without(PartId part)
            {
                var entry = DeclaredPhysics.Of(part);
                return part == removed ? entry with { Electrical = entry.Electrical! with { Supply = null } } : entry;
            }
            Assert.Empty(SupplyTopology.Create(routed,Without).UpstreamSources(gate.Id));
            Assert.Equal(DeclaredPhysics.Of(removed).Ports, Without(removed).Ports);
        }
        var direct = routed with { Connections = new(Power(battery.Id, gate.Id)) };
        Assert.Equal(new[]{battery.Id},SupplyTopology.Create(direct,DeclaredPhysics.Of).UpstreamSources(gate.Id));
        Assert.Empty(SupplyTopology.Create(direct, part =>
        {
            var entry = DeclaredPhysics.Of(part);
            return entry.Electrical is { } electrical ? entry with { Electrical = electrical with { Supply = null } } : entry;
        }).UpstreamSources(gate.Id));
    }
    private static readonly Color Slate = new("#556573");
    private static readonly Color Gold = new("#f7cb52");

    [Fact]
    public void PartRegistry_DiscoversLatchAndPresentationRestoresRestOnReset()
    {
        var registry = new PartRegistry();
        registry.Load();
        var def = registry.Definitions[PartId.Latch];
        Assert.Equal("latch", def.Id);
        Assert.Equal("Set/Reset latch", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Control, def.Category);

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

            // Latch memory is a level (Story 9A.25b): no binding of the latch is cued by a ring record.
            Assert.DoesNotContain(part.Definition.Presentation.Bindings, binding => binding.Driver == CuriousContraptions.Abstractions.PresentationDriver.Event);
            Assert.All(part.Definition.Presentation.Bindings, binding =>
            {
                Assert.Equal(CuriousContraptions.Abstractions.PresentationDriver.CommittedActivation, binding.Driver);
                Assert.Equal(.125f, binding.Duration);
                Assert.Equal(CuriousContraptions.Abstractions.PresentationCurve.SmoothStep, binding.Curve);
            });
            // Icon-only socket choices: two distinct authored pictograms.
            Assert.NotSame(WorkshopIcons.Pictogram("socket_set"), WorkshopIcons.Pictogram("socket_reset"));

            for (var i = 0; i < part.Definition.Presentation.Bindings.Count; i++)
            {
                var binding = part.Definition.Presentation.Bindings[i];
                part.ApplyDeclaredProperty(i, WorkshopDeclaredPresentation.Value(binding.Property, binding.To));
            }
            AssertColour(Gold, ((StandardMaterial3D)lamp.MaterialOverride).AlbedoColor);
            Assert.Equal(0.25f, rocker.Rotation.Z, 3);

            part.ResetPhysicalVisuals();
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
        var instance = ResourceLoader.Load<PackedScene>("res://elements/StateLatch/latch.tscn").Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);
            Assert.Equal(2, colliders.Count);
            Assert.Equal(new MetreVector((Half)0.825f, (Half)0.625f, (Half)0.325f), colliders[0].HalfExtents);
            Assert.Equal(new MetreVector((Half)0.9f, (Half)0.08f, (Half)0.425f), colliders[1].HalfExtents);
            Assert.Equal((Half)(-0.72f), colliders[1].LocalPose.Translation.Y);
            Assert.Equal(colliders.Select(c => c.HalfExtents), DeclaredPhysics.Colliders(PartId.Latch).Select(c => c.HalfExtents));
            Assert.Equal(new Godot.Vector3(-0.45f, 0.72f, 0), ports["SetIn"].Position);
            Assert.Equal(new Godot.Vector3(0.45f, 0.72f, 0), ports["ResetIn"].Position);
            Assert.Equal(new Godot.Vector3(-0.93f, 0, 0), ports["PowerIn"].Position);
            Assert.Equal(new Godot.Vector3(0.93f, 0, 0), ports["Supply"].Position);
            Assert.Equal(new Godot.Vector3(0, -0.56f, 0.38f), ports["ActivationOut"].Position);
        }
        finally { instance.Free(); }
    }

    [Fact]
    public void Ports_DeclareDistinctSetAndResetInputsAndSwitchedSupply()
    {
        var ports = DeclaredPorts.Current.Of(PartId.Latch).ToArray();
        Assert.Equal(
            [new WorkshopPort(WorkshopSocket.SetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input, WorkshopSignalKind.Pulse, new(-.45f, .72f, 0f)),
             new WorkshopPort(WorkshopSocket.ResetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input, WorkshopSignalKind.Pulse, new(.45f, .72f, 0f)),
             new WorkshopPort(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input, null, new(-.93f, 0f, 0f)),
             new WorkshopPort(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output, WorkshopSignalKind.Level, new(0f, -.56f, .38f)),
             new WorkshopPort(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output, null, new(.93f, 0f, 0f))], ports);
        Assert.Equal(2u, (uint)WorkshopSocket.SetIn);
        Assert.Equal(3u, (uint)WorkshopSocket.ResetIn);
        Assert.Equal(new System.Numerics.Vector3(-0.45f, 0.72f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Latch, WorkshopSocket.SetIn));
        Assert.Equal(new System.Numerics.Vector3(0.45f, 0.72f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Latch, WorkshopSocket.ResetIn));
        Assert.Equal(new System.Numerics.Vector3(-0.93f, 0f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Latch, WorkshopSocket.PowerIn));
        Assert.Equal(new System.Numerics.Vector3(0.93f, 0f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Latch, WorkshopSocket.Supply));
        Assert.Equal(new System.Numerics.Vector3(0f, -0.56f, 0.38f), DeclaredPorts.Current.LocalPosition(PartId.Latch, WorkshopSocket.ActivationOut));
        Assert.Equal(ElementPortRejection.UndeclaredSocket,
            Assert.Throws<ElementPortRejectedException>(() => DeclaredPorts.Current.LocalPosition(PartId.Latch, WorkshopSocket.ActivationIn)).Reason);
    }

    [Fact]
    public void Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes()
    {
        var latch = PlacedParts.Latch(new(1), 0, 0, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(2), 3, 0, 0, 0, 0, 0, 1);
        var counter = PlacedParts.Counter(new(3), -3, 0, 0, 0, 0, 0, 1, 3);
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(4), 0, 3, 0, 0, 0, 0, 1);
        var other = PlacedParts.Latch(new(5), 0, -3, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(latch, lamp, counter, hit, other);
        var output = Signal(latch.Id, lamp.Id, WorkshopSocket.ActivationIn);
        new WorkshopConnections(output).Validate(instances);
        // Story 9R.5: the latch shares the plate's level-output rule and its exact message.
        foreach (var (target, input) in new[] { (counter.Id, WorkshopSocket.ActivationIn), (other.Id, WorkshopSocket.SetIn), (other.Id, WorkshopSocket.ResetIn) })
            Assert.Equal("A level output can only light a Signal lamp.", EditRefusal.Is(WorkshopEditRejection.LevelDrivesLevelInputOnly,
                () => new WorkshopConnections(Signal(latch.Id, target, input)).Validate(instances)).Message);
        EditRefusal.Is(WorkshopEditRejection.LevelInputFollowsOneLevel, () => new WorkshopConnections(output, Signal(hit.Id, lamp.Id, WorkshopSocket.ActivationIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.LevelInputFollowsOneLevel, () => new WorkshopConnections(output, Signal(other.Id, lamp.Id, WorkshopSocket.ActivationIn)).Validate(instances));

        // The save codec rejects an invalid stored shape atomically: a latch output aimed at a Counter.
        var valid = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instances, Connections: new(output));
        var bytes = WorkshopSaveCodec.Encode(new(valid, new(6)));
        var invalid = valid with { Connections = new(Signal(latch.Id, counter.Id, WorkshopSocket.ActivationIn)) };
        EditRefusal.Is(WorkshopEditRejection.LevelDrivesLevelInputOnly, () => WorkshopSaveCodec.Encode(new(invalid, new(6))));
        Assert.Equal(output, Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes).Construction.Connections.ToArray()));
    }

    [Fact]
    public void Network_LevelIndicatorMirrorsLatchOnAndOff()
    {
        var setClock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var resetClock = PlacedParts.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, 1.5f);
        var latch = PlacedParts.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -6, 0, 0);
        var lamp = PlacedParts.Lamp(new(5), 3, 0, 0, 0, 0, 0, 1);
        var sticky = PlacedParts.Lamp(new(6), 3, 3, 0, 0, 0, 0, 1);
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
        var source = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var latch = PlacedParts.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(3), 3, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -3, 2, 0);
        var second = PlacedParts.Battery(new(5), 3, 2, 0);
        var instances = new WorkshopInstances(source, latch, lamp, battery, second);
        var set = Signal(source.Id, latch.Id, WorkshopSocket.SetIn);
        var reset = Signal(source.Id, latch.Id, WorkshopSocket.ResetIn);
        var power = Power(battery.Id, latch.Id);
        new WorkshopConnections(set, reset, power).Validate(instances);

        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(Signal(source.Id, latch.Id, WorkshopSocket.ActivationIn)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(Signal(source.Id, lamp.Id, WorkshopSocket.SetIn)).Validate(instances));
        new WorkshopConnections(power, Power(second.Id, latch.Id)).Validate(instances); // declared contacts admit source-seeded cycles/fan-in
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(power, power).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied, () => new WorkshopConnections(Power(battery.Id, source.Id), Power(second.Id, source.Id)).Validate(instances));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(set with { Domain = WorkshopConnectionDomain.Electrical }).Validate(instances));
    }

    [Fact]
    public void Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff()
    {
        // Set clock fires every 1.0 s (ticks 120, 240, 360...), Reset clock every 1.5 s (ticks 180, 360...) at 120 Hz.
        var setClock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var resetClock = PlacedParts.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, 1.5f);
        var latch = PlacedParts.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -6, 0, 0);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(setClock, resetClock, latch, battery),
            Connections: new(Power(battery.Id, setClock.Id), Power(battery.Id, resetClock.Id),
                Signal(setClock.Id, latch.Id, WorkshopSocket.SetIn), Signal(resetClock.Id, latch.Id, WorkshopSocket.ResetIn)));
        construction.Validate();
        var network = WorkshopActivationCompiler.Compile(construction);
        Assert.Contains(network.Nodes.ToArray(), node => node.Owner == latch.Id && node.Kind == ActivationNodeKind.StateLatch);
        Assert.Equal(2, network.Edges.ToArray().Count(edge => edge.Target.Value == latch.Id.Value));

        var transitions = new List<(ulong Tick, ActivationTransition Transition)>();
        var phases = Run(network, battery.Id, 600, transitions);

        Assert.All(phases.Take(119), phase => Assert.Equal(SimulationLatchPhase.Off, phase));
        Assert.All(phases.Skip(119).Take(60), phase => Assert.Equal(SimulationLatchPhase.On, phase)); // ticks 120..179 held without input
        Assert.All(phases.Skip(179).Take(60), phase => Assert.Equal(SimulationLatchPhase.Off, phase)); // Reset alone at 180
        Assert.All(phases.Skip(239).Take(120), phase => Assert.Equal(SimulationLatchPhase.On, phase)); // Set at 240, held to 359
        Assert.Equal(SimulationLatchPhase.Off, phases[359]); // ticks 360: Set and Reset together, Reset dominates
        Assert.Equal(SimulationLatchPhase.On, phases[479]);
        Assert.Equal(SimulationLatchPhase.Off, phases[539]);

        // Story 9A.22: the memory node's committed level changes are the network's LevelChanged transitions, which the producer
        // publishes as MemoryChanged. On@120, Off@180, On@240, Off@360, On@480, Off@540, On@600: exactly one per transition, none for
        // redundant requests.
        var memory = transitions.Where(t => t.Transition.Kind == ActivationTransitionKind.LevelChanged).ToArray();
        Assert.Equal([120UL, 180UL, 240UL, 360UL, 480UL, 540UL, 600UL], memory.Select(t => t.Tick).ToArray());
        Assert.Equal([ActivationPhase.Latched, ActivationPhase.Clear, ActivationPhase.Latched, ActivationPhase.Clear,
            ActivationPhase.Latched, ActivationPhase.Clear, ActivationPhase.Latched], memory.Select(t => t.Transition.Level).ToArray());
        Assert.All(memory, t => Assert.Equal(new ActivationNodeId(latch.Id.Value), t.Transition.Node));

        Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(network.Clear(), latch.Id));
    }

    [Fact]
    public void Network_OneSourceWiredToBothInputsNeverTurnsOn()
    {
        var clock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var latch = PlacedParts.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(3), -6, 0, 0);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(clock, latch, battery),
            Connections: new(Power(battery.Id, clock.Id), Signal(clock.Id, latch.Id, WorkshopSocket.SetIn), Signal(clock.Id, latch.Id, WorkshopSocket.ResetIn)));
        construction.Validate();
        var phases = Run(WorkshopActivationCompiler.Compile(construction), battery.Id, 480, []);
        Assert.All(phases, phase => Assert.Equal(SimulationLatchPhase.Off, phase));

        // Control: the same source on SetIn alone turns the latch On at its first pulse.
        var control = construction with { Connections = new(Power(battery.Id, clock.Id), Signal(clock.Id, latch.Id, WorkshopSocket.SetIn)) };
        var transitions = new List<(ulong Tick, ActivationTransition Transition)>();
        var controlPhases = Run(WorkshopActivationCompiler.Compile(control), battery.Id, 480, transitions);
        Assert.Equal(SimulationLatchPhase.Off, controlPhases[118]);
        Assert.Equal(SimulationLatchPhase.On, controlPhases[119]);
        Assert.Equal(SimulationLatchPhase.On, controlPhases[479]);
        // Repeated Sets at 240, 360 and 480 while already On make no further memory change.
        Assert.Equal(120UL, Assert.Single(transitions, t => t.Transition.Kind == ActivationTransitionKind.LevelChanged).Tick);
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
        new(source.Trigger, source.Owner, new(99), 1, new(7), (uint)((tick - 1) * 4 + substep), (Half)0, new((float)2));
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
        var battery = PlacedParts.Battery(new(1), -4, 0, 0);
        var latch = PlacedParts.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 4, 0, 0, 0, 0, 0, 1);
        var routed = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery, latch, gate),
            Connections: new(Power(battery.Id, latch.Id), new(latch.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        routed.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(routed, new(1, 2));
        Assert.Equal(latch.Id,Assert.Single(scene.SupplyGraph.Nodes.ToArray(),node=>node.Kind==SupplyNodeKind.Contact).Owner);
        Assert.Equal(gate.Id,scene.Prismatics[0].BodyA);
        Assert.Equal(battery.Id,scene.Electrical.Sources[0].Owner);
        var on = new WorkshopTickControls([ElectricalEnable.Enabled]);
        var off = new WorkshopTickControls([ElectricalEnable.Disabled]);
        var prismatics = scene.Prismatics.Length;
        Assert.True(PhysicsTickInputs.Compose(scene.SupplyGraph, prismatics, on, _ => true).SupplyClosed[0]);
        Assert.False(PhysicsTickInputs.Compose(scene.SupplyGraph, prismatics, on, _ => false).SupplyClosed[0]);
        Assert.False(PhysicsTickInputs.Compose(scene.SupplyGraph, prismatics, off, _ => true).SupplyClosed[0]);
        Assert.Throws<ArgumentException>(() => new PhysicsTickInputs([ElectricalEnable.Enabled], []).Validate(1, prismatics));
        Assert.Throws<ArgumentException>(() => new WorkshopTickControls([(ElectricalEnable)9]).Validate(1));



        // Memory without a supply compiles no route; a battery wired straight to the gate compiles one Direct route (Story 9R.3).
        var unsupplied = routed with { Connections = new(new WorkshopConnection(latch.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) };
        var unsuppliedScene=WorkshopPhysicsCompiler.Compile(unsupplied,new(1,2));
        Assert.False(PhysicsTickInputs.Compose(unsuppliedScene.SupplyGraph,prismatics,on,_=>true).SupplyClosed[0]);
        var direct=routed with {Connections=new(Power(battery.Id,gate.Id))};
        var directScene=WorkshopPhysicsCompiler.Compile(direct,new(1,2));
        Assert.True(PhysicsTickInputs.Compose(directScene.SupplyGraph,prismatics,on,_=>false).SupplyClosed[0]);

        // A Counter's supply contact compiles the same route kind: closed only while its committed value is reached.
        var counter = PlacedParts.Counter(new(4), 0, 3, 0, 0, 0, 0, 1, 3);
        var counted = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery, counter, gate),
            Connections: new(Power(battery.Id, counter.Id), new(counter.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        counted.Validate();
        var countedScene=WorkshopPhysicsCompiler.Compile(counted,new(1,2));
        Assert.Equal(counter.Id,Assert.Single(countedScene.SupplyGraph.Nodes.ToArray(),node=>node.Kind==SupplyNodeKind.Contact).Owner);
    }

    [Fact]
    public void Compiler_TwoSwitchedGatesEachFollowOnlyTheirOwnRoute()
    {
        // An unswitched gate compiles first, so neither switched route sits at prismatic index 0.
        var plain = WorkshopInput.Generic(PartId.PoweredGate, new(1), -8, 0, 0, 0, 0, 0, 1);
        var batteryA = PlacedParts.Battery(new(2), -6, 3, 0);
        var batteryB = PlacedParts.Battery(new(3), -6, -3, 0, enabled: ElectricalEnable.Disabled);
        var latchA = PlacedParts.Latch(new(4), 0, 3, 0, 0, 0, 0, 1);
        var latchB = PlacedParts.Latch(new(5), 0, -3, 0, 0, 0, 0, 1);
        var gateA = WorkshopInput.Generic(PartId.PoweredGate, new(6), 6, 3, 0, 0, 0, 0, 1);
        var gateB = WorkshopInput.Generic(PartId.PoweredGate, new(7), 6, -3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(plain, batteryA, batteryB, latchA, latchB, gateA, gateB),
            Connections: new(Power(batteryA.Id, latchA.Id), Power(batteryB.Id, latchB.Id),
                new(latchA.Id, WorkshopSocket.Supply, gateA.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
                new(latchB.Id, WorkshopSocket.Supply, gateB.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        construction.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        Assert.Equal(2,scene.SupplyGraph.Nodes.ToArray().Count(node=>node.Kind==SupplyNodeKind.Contact));
        int PrismaticOf(GpuBodyId gate)=>Array.FindIndex(scene.Prismatics.ToArray(),joint=>joint.BodyA==gate);
        int SourceOf(GpuBodyId battery)=>Array.FindIndex(scene.Electrical.Sources.ToArray(),source=>source.Owner==battery);
        Assert.Equal(0,PrismaticOf(plain.Id));
        bool[] Closed(ElectricalEnable a, ElectricalEnable b, Func<GpuBodyId, bool> contact)
        {
            var enables = new ElectricalEnable[scene.Electrical.Sources.Length];
            enables[SourceOf(batteryA.Id)] = a; enables[SourceOf(batteryB.Id)] = b;
            return PhysicsTickInputs.Compose(scene.SupplyGraph, scene.Prismatics.Length, new(enables), contact).SupplyClosed;
        }
        // Both latches On, battery A enabled and battery B disabled (its authored state): only gate A is supplied; the plain gate never is.
        var both = Closed(ElectricalEnable.Enabled, ElectricalEnable.Disabled, _ => true);
        Assert.False(both[PrismaticOf(plain.Id)]); Assert.True(both[PrismaticOf(gateA.Id)]); Assert.False(both[PrismaticOf(gateB.Id)]);
        // Each gate follows its own latch: only latch B closed, both batteries enabled → only gate B.
        var onlyB = Closed(ElectricalEnable.Enabled, ElectricalEnable.Enabled, owner => owner == latchB.Id);
        Assert.False(onlyB[PrismaticOf(gateA.Id)]); Assert.True(onlyB[PrismaticOf(gateB.Id)]);
        // Each gate follows its own battery: latch A closed but battery A disabled, battery B enabled → neither gate.
        var crossed = Closed(ElectricalEnable.Disabled, ElectricalEnable.Enabled, owner => owner == latchA.Id);
        Assert.False(crossed[PrismaticOf(gateA.Id)]); Assert.False(crossed[PrismaticOf(gateB.Id)]);
    }

    [Fact]
    public void Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach()
    {
        var clock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var latch = PlacedParts.Latch(new(2), 0, 0, 0, 0, 0, 0, 1);
        var counter = PlacedParts.Counter(new(3), 0, 3, 0, 0, 0, 0, 1, 2);
        var battery = PlacedParts.Battery(new(4), -6, 0, 0);
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
            if (latchClosedAt == 0 && network.ContactClosed(activations, network.ClearTimers(), latch.Id)) latchClosedAt = tick;
            if (counterClosedAt == 0 && network.ContactClosed(activations, network.ClearTimers(), counter.Id)) counterClosedAt = tick;
        }
        Assert.Equal(120, latchClosedAt);   // first pulse sets the latch
        Assert.Equal(240, counterClosedAt); // the counter (target 2) closes on the second pulse, never on elapsed time alone
        Assert.Throws<ArgumentException>(() => network.ContactClosed(activations, network.ClearTimers(), clock.Id));
    }

    [Fact]
    public void Compiler_DeclaresStaticLatchBodyWithCanonicalColliders()
    {
        var latch = PlacedParts.Latch(new(7), 1, 0, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(latch));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == latch.Id);
        Assert.Equal(RigidMotionKind.Static, body.Motion);
        var colliders = scene.Colliders.ToArray().Where(c => c.Body == latch.Id).ToArray();
        Assert.Equal(DeclaredPhysics.Colliders(PartId.Latch).Select(c => c.HalfExtents), colliders.Select(c => c.HalfExtents));
        Assert.Equal(2, colliders.Length);
    }

    [Fact]
    public void Persistence_BinaryFlatBuffersAndSaveCodecPreserveDistinctSockets()
    {
        var setSwitch = WorkshopInput.Generic(PartId.ImpactSwitch, new(1), -3, 0, 0, 0, 0, 0, 1);
        var resetSwitch = WorkshopInput.Generic(PartId.ImpactSwitch, new(2), -3, 2, 0, 0, 0, 0, 1);
        var latch = PlacedParts.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -5, 0, 0);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(5), 4, 0, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(9), WorkshopCadenceSettings.Default(), new(setSwitch, resetSwitch, latch, battery, gate),
            Connections: new(Signal(setSwitch.Id, latch.Id, WorkshopSocket.SetIn), Signal(resetSwitch.Id, latch.Id, WorkshopSocket.ResetIn),
                Power(battery.Id, latch.Id), new(latch.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        construction.Validate();

        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, construction,
            Session: new RuntimeSessionId(1, 2), Cadence: new(1), Projection: new(1));
        AssertSame(construction, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Construction!.Value);

        var saved = new WorkshopSavedConstruction(construction, new(6));
        var restored = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        AssertSame(construction, restored.Construction);
        Assert.Equal(WorkshopSaveCodec.Encode(saved), WorkshopSaveCodec.Encode(restored));
    }

    // Cosmetic channels travel as f32 animation values, so colours preserve the authored palette.
    private static void AssertColour(Color expected, Color actual)
    {
        Assert.Equal(expected.R, actual.R, 2);
        Assert.Equal(expected.G, actual.G, 2);
        Assert.Equal(expected.B, actual.B, 2);
    }

    private static void AssertSame(WorkshopConstruction expected, WorkshopConstruction actual)
    {
        Assert.Equal(expected.Connections.ToArray(), actual.Connections.ToArray());
        var latch = Assert.Single(actual.Instances, instance => instance.PartId == PartId.Latch);
        Assert.Equal(expected.Instances.Single(instance => instance.PartId == PartId.Latch), latch);
    }

    private static SimulationLatchPhase[] Run(ActivationNetwork network, GpuBodyId battery, int ticks,
        List<(ulong Tick, ActivationTransition Transition)> transitions)
    {
        var latchOwner = network.Nodes.ToArray().Single(node => node.Kind == ActivationNodeKind.StateLatch).Owner;
        var activations = network.Clear();
        var timers = network.ClearTimers();
        var memory = network.ClearMemory();
        var electrical = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery, new Joules(3600f), default, ElectricalEnable.Enabled)]);
        var phases = new SimulationLatchPhase[ticks];
        for (var tick = 1; tick <= ticks; tick++)
        {
            var checkpoint = network.Consume(activations, timers, [], new((ulong)tick), [], [], electrical, memory);
            activations = checkpoint.Activations; timers = checkpoint.Timers; memory = checkpoint.Memory;
            for (var i = 0; i < checkpoint.Transitions.Count; i++) transitions.Add(((ulong)tick, checkpoint.Transitions[i]));
            phases[tick - 1] = network.StateLatchPhase(activations, latchOwner);
        }
        return phases;
    }

    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);

    private static WorkshopConnection Power(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
}
