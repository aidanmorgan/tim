using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class ClockTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversClockDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.True(registry.Definitions.ContainsKey(PartId.Clock));
        var def = registry.Definitions[PartId.Clock];
        Assert.Equal(PartId.Clock, def.PartId);
        Assert.Equal("clock", def.Id);
        Assert.Equal("Repeating clock", def.Title);
        Assert.Equal("Control", def.Category);
        Assert.NotNull(def.Scene);
        Assert.True(def.Parameters.ContainsKey("interval_seconds"));
        Assert.Equal(1.0f, (float)def.Parameters["interval_seconds"]);

        var part = registry.Create(PartId.Clock);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.Clock, part.PartId);
            Assert.Equal(1.1f, part.PickRadius);
            Assert.Equal(1.0f, part.CanonicalIntervalSeconds);

            var visual = part.FindChild("Visual", recursive: false) as Node3D;
            Assert.NotNull(visual);
            Assert.NotNull(visual.FindChild("Foot", recursive: false));
            Assert.NotNull(visual.FindChild("Housing", recursive: false));
            Assert.NotNull(visual.FindChild("Face", recursive: false));
            Assert.NotNull(visual.FindChild("Recess", recursive: false));
            Assert.NotNull(visual.FindChild("PendulumPivot", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Indicator", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_PowerIn", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_ActivationOut", recursive: false));

            // Test lamp update
            part.UpdateClockLamp(true);
            var indicator = visual.FindChild("Visual_Indicator", recursive: false) as MeshInstance3D;
            Assert.NotNull(indicator);
            var matActive = indicator.MaterialOverride as StandardMaterial3D;
            Assert.NotNull(matActive);
            Assert.Equal(new Color("#f7cb52"), matActive.AlbedoColor);

            part.UpdateClockLamp(false);
            var matIdle = indicator.MaterialOverride as StandardMaterial3D;
            Assert.NotNull(matIdle);
            Assert.Equal(new Color("#556573"), matIdle.AlbedoColor);

            part.ResetPhysicalVisuals();
            Assert.Equal(new Color("#556573"), ((StandardMaterial3D)indicator.MaterialOverride).AlbedoColor);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void ClockScene_ExtractsExactTwoCollidersAndPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://parts/scenes/clock.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(instance);

            // Exactly 2 colliders: Housing + Foot
            Assert.Equal(2, colliders.Count);

            var housing = colliders[0];
            Assert.Equal(ColliderShapeKind.Box, housing.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.7f, (Half)0.85f, (Half)0.3f), housing.HalfExtents);
            Assert.Equal((Half)0, housing.LocalPose.Translation.X);
            Assert.Equal((Half)0, housing.LocalPose.Translation.Y);
            Assert.Equal((Half)0, housing.LocalPose.Translation.Z);

            var foot = colliders[1];
            Assert.Equal(ColliderShapeKind.Box, foot.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.8f, (Half)0.1f, (Half)0.425f), foot.HalfExtents);
            Assert.Equal((Half)0, foot.LocalPose.Translation.X);
            Assert.Equal((Half)(-0.95f), foot.LocalPose.Translation.Y);
            Assert.Equal((Half)0, foot.LocalPose.Translation.Z);

            Assert.True(ports.ContainsKey("power_in"));
            Assert.True(ports.ContainsKey("activation_out"));

            Assert.Equal(new Godot.Vector3(-0.78f, 0, 0), ports["power_in"].Position);
            Assert.Equal(new Godot.Vector3(0.78f, 0, 0), ports["activation_out"].Position);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void CanonicalColliders_MatchSceneExtractedColliders()
    {
        var canonical = WorkshopCanonicalColliders.For(PartId.Clock);
        Assert.Equal(2, canonical.Count);
        Assert.Same(canonical, WorkshopCanonicalColliders.ClockColliders);
    }

    [Fact]
    public void Ports_DeclareExactSocketsAndDirections()
    {
        var ports = WorkshopPorts.For(PartId.Clock).ToArray();
        Assert.Equal(2, ports.Length);

        var powerIn = ports.First(p => p.Socket == WorkshopSocket.PowerIn);
        Assert.Equal(WorkshopConnectionDomain.Electrical, powerIn.Domain);
        Assert.Equal(WorkshopPortDirection.Input, powerIn.Direction);

        var actOut = ports.First(p => p.Socket == WorkshopSocket.ActivationOut);
        Assert.Equal(WorkshopConnectionDomain.Activation, actOut.Domain);
        Assert.Equal(WorkshopPortDirection.Output, actOut.Direction);

        Assert.Equal(new System.Numerics.Vector3(-0.78f, 0f, 0f), WorkshopPorts.LocalPosition(PartId.Clock, WorkshopSocket.PowerIn));
        Assert.Equal(new System.Numerics.Vector3(0.78f, 0f, 0f), WorkshopPorts.LocalPosition(PartId.Clock, WorkshopSocket.ActivationOut));
    }

    [Fact]
    public void WorkshopClock_ValidationAndTicks()
    {
        // Admitted range [0.1, 12.0]
        new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 0.1f).Validate();
        new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 1.0f).Validate();
        new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 12.0f).Validate();

        // Story 9.4b (D4): the interval is always declared; only a newly placed clock starts from its placement value.
        Assert.Equal(1.0f, WorkshopClock.PlacedIntervalSeconds);
        Assert.False(typeof(WorkshopClock).GetConstructors().Single().GetParameters()
            .Single(parameter => parameter.Name == nameof(WorkshopClock.IntervalSeconds)).HasDefaultValue);

        Assert.Throws<ArgumentException>(() => new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 0.05f).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 12.1f).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, float.NaN).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, float.PositiveInfinity).Validate());

        // Ticks math
        var clock1s = new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 1.0f);
        Assert.Equal(60u, clock1s.Ticks(SimulationCadence.Hz60));
        Assert.Equal(120u, clock1s.Ticks(SimulationCadence.Hz120));
        Assert.Equal(240u, clock1s.Ticks(SimulationCadence.Hz240));

        var clockHalfS = new WorkshopClock(new(1), default, default, CanonicalRotation.Identity, 0.5f);
        Assert.Equal(60u, clockHalfS.Ticks(SimulationCadence.Hz120));
    }

    [Fact]
    public void Connections_AcceptsValidActivationAndElectricalLinks()
    {
        var clock = WorkshopInput.Clock(new(1), 0, 0, 0, 0, 0, 0, 1, 1.0f);
        var battery = WorkshopInput.Battery(new(2), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var counter = WorkshopInput.Counter(new(3), 2, 0, 0, 0, 0, 0, 1, 5);

        var instances = new WorkshopInstances(clock, battery, counter);

        var conn1 = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var conn2 = new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);

        var connections = new WorkshopConnections(conn1, conn2);
        connections.Validate(instances);

        // Invalid: Activation on electrical socket
        var invalidConn = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Electrical);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(invalidConn).Validate(instances));
    }

    [Fact]
    public void ActivationNetwork_ClockPoweredPeriodicPulses_5PulsesIn5Seconds()
    {
        var battery = WorkshopInput.Battery(new(1), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var clock = WorkshopInput.Clock(new(2), 0, 0, 0, 0, 0, 0, 1, 1.0f);
        var counter = WorkshopInput.Counter(new(3), 2, 0, 0, 0, 0, 0, 1, 5);

        var conn1 = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var conn2 = new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([battery, clock, counter]),
            Connections: new([conn1, conn2]));

        var network = WorkshopActivationCompiler.Compile(construction);
        var activeActivations = network.Clear();
        var activeTimers = network.ClearTimers();
        var electrical = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery.Id, new Joules(3600f), default, ElectricalEnable.Enabled)]);

        // 600 ticks = 5.0 seconds at 120 Hz
        var pulseTicks = new List<ulong>();
        for (ulong tick = 1; tick <= 600; tick++)
        {
            var priorPulseCount = network.GetOscillatorPulseCount(clock.Id);
            var checkpoint = network.Consume(activeActivations, activeTimers, [], new(tick), [], [], electrical);
            activeActivations = checkpoint.Activations;
            activeTimers = checkpoint.Timers;

            var newPulseCount = network.GetOscillatorPulseCount(clock.Id);
            if (newPulseCount > priorPulseCount)
            {
                pulseTicks.Add(tick);
            }
        }

        // Exactly 5 pulses at ticks 120, 240, 360, 480, 600 (exact 1.0, 2.0, 3.0, 4.0, 5.0 s)
        Assert.Equal(5, pulseTicks.Count);
        Assert.Equal([120UL, 240UL, 360UL, 480UL, 600UL], pulseTicks);

        // Oscillator state
        Assert.True(network.IsOscillatorRunning(clock.Id));
        Assert.Equal(5, network.GetOscillatorPulseCount(clock.Id));

        // Downstream Counter reached 5
        Assert.Equal(5, network.GetCounterCount(counter.Id));
        Assert.True(network.IsCounterReached(counter.Id));
    }

    [Fact]
    public void ActivationNetwork_UnpoweredClock_EmitsZeroPulses()
    {
        var clock = WorkshopInput.Clock(new(1), 0, 0, 0, 0, 0, 0, 1, 1.0f);
        var counter = WorkshopInput.Counter(new(2), 2, 0, 0, 0, 0, 0, 1, 5);

        var conn = new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([clock, counter]),
            Connections: new([conn]));

        var network = WorkshopActivationCompiler.Compile(construction);
        var activeActivations = network.Clear();
        var activeTimers = network.ClearTimers();

        // 600 ticks without power
        for (ulong tick = 1; tick <= 600; tick++)
        {
            var checkpoint = network.Consume(activeActivations, activeTimers, [], new(tick), [], []);
            activeActivations = checkpoint.Activations;
            activeTimers = checkpoint.Timers;
        }

        Assert.False(network.IsOscillatorRunning(clock.Id));
        Assert.Equal(0, network.GetOscillatorPulseCount(clock.Id));
        Assert.Equal(0, network.GetCounterCount(counter.Id));
        Assert.False(network.IsCounterReached(counter.Id));
    }

    [Fact]
    public void ActivationNetwork_PowerLossAtDeadlineAndRestoration()
    {
        var battery = WorkshopInput.Battery(new(1), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var clock = WorkshopInput.Clock(new(2), 0, 0, 0, 0, 0, 0, 1, 1.0f);

        var conn = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([battery, clock]),
            Connections: new([conn]));

        var network = WorkshopActivationCompiler.Compile(construction);
        var activeActivations = network.Clear();
        var activeTimers = network.ClearTimers();
        var powered = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery.Id, new Joules(3600f), default, ElectricalEnable.Enabled)]);
        var unpowered = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), battery.Id, new Joules(3600f), default, ElectricalEnable.Disabled)]);

        // Run ticks 1 to 119 with power
        for (ulong tick = 1; tick < 120; tick++)
        {
            var checkpoint = network.Consume(activeActivations, activeTimers, [], new(tick), [], [], powered);
            activeActivations = checkpoint.Activations;
            activeTimers = checkpoint.Timers;
        }
        Assert.Equal(0, network.GetOscillatorPulseCount(clock.Id));

        // At deadline tick 120, cut power! Pulse MUST be suppressed!
        {
            var checkpoint = network.Consume(activeActivations, activeTimers, [], new(120), [], [], unpowered);
            activeActivations = checkpoint.Activations;
            activeTimers = checkpoint.Timers;
            Assert.Equal(0, network.GetOscillatorPulseCount(clock.Id));
            Assert.False(network.IsOscillatorRunning(clock.Id));
        }

        // Keep unpowered until tick 150
        for (ulong tick = 121; tick <= 150; tick++)
        {
            var checkpoint = network.Consume(activeActivations, activeTimers, [], new(tick), [], [], unpowered);
            activeActivations = checkpoint.Activations;
            activeTimers = checkpoint.Timers;
            Assert.Equal(0, network.GetOscillatorPulseCount(clock.Id));
        }

        // Restore power at tick 151: fresh full interval of 120 ticks -> due tick = 151 + 120 = 271
        ulong restoredPulseTick = 0;
        for (ulong tick = 151; tick <= 300; tick++)
        {
            var prior = network.GetOscillatorPulseCount(clock.Id);
            var checkpoint = network.Consume(activeActivations, activeTimers, [], new(tick), [], [], powered);
            activeActivations = checkpoint.Activations;
            activeTimers = checkpoint.Timers;
            if (network.GetOscillatorPulseCount(clock.Id) > prior)
            {
                restoredPulseTick = tick;
                break;
            }
        }

        Assert.Equal(271UL, restoredPulseTick);
        Assert.Equal(1, network.GetOscillatorPulseCount(clock.Id));
    }

    [Fact]
    public void WireSerialization_BinaryAndFlatBuffersRoundtrip()
    {
        var clock = WorkshopInput.Clock(new(10), 1, 2, 3, 0, 0, 0, 1, 2.5f);
        var battery = WorkshopInput.Battery(new(20), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var conn = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

        var construction = new WorkshopConstruction(
            new(5),
            WorkshopCadenceSettings.Default(),
            new([clock, battery]),
            Connections: new([conn]));

        // 1. Binary WorkshopWire roundtrip
        Span<byte> binaryBuffer = stackalloc byte[WorkshopWire.ConstructionBytes];
        WorkshopWire.WriteConstruction(binaryBuffer, construction);
        var decodedBinary = WorkshopWire.ReadConstruction(binaryBuffer);
        var decodedClock = decodedBinary.Instances.OfType<WorkshopClock>().Single();
        Assert.Equal(10UL, decodedClock.Id.Value);
        Assert.Equal(2.5f, decodedClock.IntervalSeconds);
        Assert.Single(decodedBinary.Connections);

        // 2. FlatBuffers roundtrip
        var session = new RuntimeSessionId(0x1122334455667788UL, 0x8877665544332211UL);
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, construction,
            Session: session, Cadence: new(1), Projection: new(1));
        var fbBytes = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decodedFb = WorkshopWireFlatBuffers.DecodeCommand(fbBytes);
        Assert.NotNull(decodedFb.Construction);
        var decodedFbClock = decodedFb.Construction!.Value.Instances.OfType<WorkshopClock>().Single();
        Assert.Equal(10UL, decodedFbClock.Id.Value);
        Assert.Equal(2.5f, decodedFbClock.IntervalSeconds);
        Assert.Single(decodedFb.Construction!.Value.Connections);
    }
}
