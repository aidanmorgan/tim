using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class CounterTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversCounterDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.True(registry.Definitions.ContainsKey(PartId.Counter));
        var def = registry.Definitions[PartId.Counter];
        Assert.Equal(PartId.Counter, def.PartId);
        Assert.Equal("counter", def.Id);
        Assert.Equal("Counter", def.Title);
        Assert.Equal("Control", def.Category);
        Assert.NotNull(def.Scene);
        Assert.Empty(def.Parameters);

        var part = registry.Create(PartId.Counter);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.Counter, part.PartId);
            Assert.Equal(1.2f, part.PickRadius);

            var visual = part.FindChild("Visual", recursive: false) as Node3D;
            Assert.NotNull(visual);
            Assert.NotNull(visual.FindChild("Housing", recursive: false));
            Assert.NotNull(visual.FindChild("Foot", recursive: false));
            Assert.NotNull(visual.FindChild("Face", recursive: false));
            for (var i = 0; i < 9; i++)
            {
                Assert.NotNull(visual.FindChild($"DotLamp_{i}", recursive: false));
            }
            Assert.NotNull(visual.FindChild("Visual_Socket_ActivationIn", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_ActivationOut", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_PowerIn", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_Supply", recursive: false));

            // Test lamp update
            part.UpdateCounterLamps(3);
            var lamp0 = visual.FindChild("DotLamp_0", recursive: false) as MeshInstance3D;
            var lamp4 = visual.FindChild("DotLamp_4", recursive: false) as MeshInstance3D;
            Assert.NotNull(lamp0);
            Assert.NotNull(lamp4);
            var mat0 = lamp0.MaterialOverride as StandardMaterial3D;
            var mat4 = lamp4.MaterialOverride as StandardMaterial3D;
            Assert.NotNull(mat0);
            Assert.NotNull(mat4);
            Assert.Equal(new Color("#f7cb52"), mat0.AlbedoColor);
            Assert.Equal(new Color("#556573"), mat4.AlbedoColor);

            part.ResetPhysicalVisuals();
            Assert.Equal(new Color("#556573"), ((StandardMaterial3D)lamp0.MaterialOverride).AlbedoColor);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void CounterScene_ExtractsExactTwoCollidersAndPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://parts/scenes/counter.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(instance);

            // Exactly 2 colliders: Housing + Foot
            Assert.Equal(2, colliders.Count);

            var housing = colliders[0];
            Assert.Equal(ColliderShapeKind.Box, housing.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.825f, (Half)0.625f, (Half)0.325f), housing.HalfExtents);
            Assert.Equal((Half)0, housing.LocalPose.Translation.X);
            Assert.Equal((Half)0, housing.LocalPose.Translation.Y);
            Assert.Equal((Half)0, housing.LocalPose.Translation.Z);

            var foot = colliders[1];
            Assert.Equal(ColliderShapeKind.Box, foot.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.9f, (Half)0.08f, (Half)0.425f), foot.HalfExtents);
            Assert.Equal((Half)0, foot.LocalPose.Translation.X);
            Assert.Equal((Half)(-0.72f), foot.LocalPose.Translation.Y);
            Assert.Equal((Half)0, foot.LocalPose.Translation.Z);

            Assert.True(ports.ContainsKey("activation_in"));
            Assert.True(ports.ContainsKey("activation_out"));
            Assert.True(ports.ContainsKey("power_in"));
            Assert.True(ports.ContainsKey("supply"));

            Assert.Equal(new Godot.Vector3(0, 0.72f, 0.1f), ports["activation_in"].Position);
            Assert.Equal(new Godot.Vector3(0, -0.72f, 0.1f), ports["activation_out"].Position);
            Assert.Equal(new Godot.Vector3(-0.93f, 0, 0), ports["power_in"].Position);
            Assert.Equal(new Godot.Vector3(0.93f, 0, 0), ports["supply"].Position);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void CanonicalColliders_MatchSceneExtractedColliders()
    {
        var canonical = WorkshopCanonicalColliders.For(PartId.Counter);
        Assert.Equal(2, canonical.Count);
        Assert.Same(canonical, WorkshopCanonicalColliders.CounterColliders);
    }

    [Fact]
    public void Ports_DeclareExactSocketsAndDirections()
    {
        var ports = WorkshopPorts.For(PartId.Counter).ToArray();
        Assert.Equal(4, ports.Length);

        var actIn = ports.First(p => p.Socket == WorkshopSocket.ActivationIn);
        Assert.Equal(WorkshopConnectionDomain.Activation, actIn.Domain);
        Assert.Equal(WorkshopPortDirection.Input, actIn.Direction);

        var actOut = ports.First(p => p.Socket == WorkshopSocket.ActivationOut);
        Assert.Equal(WorkshopConnectionDomain.Activation, actOut.Domain);
        Assert.Equal(WorkshopPortDirection.Output, actOut.Direction);

        var powerIn = ports.First(p => p.Socket == WorkshopSocket.PowerIn);
        Assert.Equal(WorkshopConnectionDomain.Electrical, powerIn.Domain);
        Assert.Equal(WorkshopPortDirection.Input, powerIn.Direction);

        var supply = ports.First(p => p.Socket == WorkshopSocket.Supply);
        Assert.Equal(WorkshopConnectionDomain.Electrical, supply.Domain);
        Assert.Equal(WorkshopPortDirection.Output, supply.Direction);

        Assert.Equal(new System.Numerics.Vector3(0f, 0.72f, 0.1f), WorkshopPorts.LocalPosition(PartId.Counter, WorkshopSocket.ActivationIn));
        Assert.Equal(new System.Numerics.Vector3(0f, -0.72f, 0.1f), WorkshopPorts.LocalPosition(PartId.Counter, WorkshopSocket.ActivationOut));
        Assert.Equal(new System.Numerics.Vector3(-0.93f, 0f, 0f), WorkshopPorts.LocalPosition(PartId.Counter, WorkshopSocket.PowerIn));
        Assert.Equal(new System.Numerics.Vector3(0.93f, 0f, 0f), WorkshopPorts.LocalPosition(PartId.Counter, WorkshopSocket.Supply));
    }

    [Fact]
    public void WorkshopCounter_Validation()
    {
        // Admitted range [1, 9]
        for (var t = 1; t <= 9; t++)
        {
            new WorkshopCounter(new(1), default, default, CanonicalRotation.Identity, t).Validate();
        }

        // Story 9.4b (D4): the target count is always declared; only a newly placed counter starts from its placement value.
        Assert.Equal(3, WorkshopCounter.PlacedTargetCount);
        Assert.False(typeof(WorkshopCounter).GetConstructors().Single().GetParameters()
            .Single(parameter => parameter.Name == nameof(WorkshopCounter.TargetCount)).HasDefaultValue);

        Assert.Throws<ArgumentException>(() => new WorkshopCounter(new(1), default, default, CanonicalRotation.Identity, 0).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopCounter(new(1), default, default, CanonicalRotation.Identity, -1).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopCounter(new(1), default, default, CanonicalRotation.Identity, 10).Validate());
    }

    [Fact]
    public void Connections_AcceptsValidActivationAndElectricalLinks()
    {
        var counter = WorkshopInput.Counter(new(1), 0, 0, 0, 0, 0, 0, 1, 3);
        var detector = WorkshopInput.BallDetector(new(2), -2, 0, 0, 0, 0, 0, 1);
        var lamp = WorkshopInput.Lamp(new(3), 2, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(4), 0, -2, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(5), 0, 2, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);

        var instances = new WorkshopInstances(counter, detector, lamp, battery, gate);

        var conn1 = new WorkshopConnection(detector.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        var conn2 = new WorkshopConnection(counter.Id, WorkshopSocket.ActivationOut, lamp.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        var conn3 = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, counter.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var conn4 = new WorkshopConnection(counter.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

        var connections = new WorkshopConnections(conn1, conn2, conn3, conn4);
        connections.Validate(instances);

        // Invalid: Activation on electrical socket
        var invalidConn = new WorkshopConnection(detector.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Activation);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(invalidConn).Validate(instances));
    }

    [Fact]
    public void PhysicsCompiler_CompilesCollidersAndCounterPower()
    {
        var counter = WorkshopInput.Counter(new(1), 0, 0, 0, 0, 0, 0, 1, 3);
        var battery = WorkshopInput.Battery(new(2), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);

        // Without battery connected: gate blade drag is 0
        var connNoBattery = new WorkshopConnection(counter.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var constructionUnpowered = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([counter, battery, gate]),
            Connections: new([connNoBattery]));

        var sceneUnpowered = WorkshopPhysicsCompiler.Compile(constructionUnpowered, new(1, 1));
        var bladeUnpowered = sceneUnpowered.Bodies.ToArray().First(b => b.Id == WorkshopPhysicsCompiler.GateBlade(gate.Id));
        Assert.Equal((Half)0f, bladeUnpowered.LinearDrag.Value);

        // Counter static body has 2 box colliders
        var counterColliders = sceneUnpowered.Colliders.ToArray().Where(c => c.Body == counter.Id).ToArray();
        Assert.Equal(2, counterColliders.Length);
        Assert.Equal(new MetreVector((Half)0.825f, (Half)0.625f, (Half)0.325f), counterColliders[0].HalfExtents);
        Assert.Equal(new MetreVector((Half)0.9f, (Half)0.08f, (Half)0.425f), counterColliders[1].HalfExtents);

        // With battery connected to counter PowerIn and counter Supply to gate PowerIn the gate is a switched route resolved per tick from
        // the Counter's committed contact; the blade carries no mode-encoding drag.
        var connBattery = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, counter.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var constructionPowered = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([counter, battery, gate]),
            Connections: new([connBattery, connNoBattery]));

        var scenePowered = WorkshopPhysicsCompiler.Compile(constructionPowered, new(1, 1));
        var bladePowered = scenePowered.Bodies.ToArray().First(b => b.Id == WorkshopPhysicsCompiler.GateBlade(gate.Id));
        Assert.Equal((Half)0f, bladePowered.LinearDrag.Value);
        var route = Assert.Single(scenePowered.SwitchedSupplies.ToArray());
        Assert.Equal(counter.Id, route.Switch);
        Assert.Equal(battery.Id, scenePowered.Electrical.Sources[route.Source].Owner);
    }

    [Fact]
    public void ActivationNetwork_CountingProgression_SingleEmission_AndSaturation()
    {
        var detector = WorkshopInput.BallDetector(new(1), -2, 0, 0, 0, 0, 0, 1);
        var counter = WorkshopInput.Counter(new(2), 0, 0, 0, 0, 0, 0, 1, 3);
        var lamp = WorkshopInput.Lamp(new(3), 2, 0, 0, 0, 0, 0, 1);

        var conn1 = new WorkshopConnection(detector.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        var conn2 = new WorkshopConnection(counter.Id, WorkshopSocket.ActivationOut, lamp.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([detector, counter, lamp]),
            Connections: new([conn1, conn2]));

        var network = WorkshopActivationCompiler.Compile(construction);
        var activations = network.Clear();
        var timers = network.ClearTimers();

        Assert.Equal(0, network.GetCounterCount(counter.Id));
        Assert.Equal(3, network.GetCounterTarget(counter.Id));
        Assert.False(network.IsCounterReached(counter.Id));

        var sensorId = WorkshopPhysicsCompiler.PassageSensor(detector.Id);
        var ballBodyId = new GpuBodyId(10);
        var ballColliderId = new GpuColliderId(11);

        // Tick 1: passage 1
        var passageRead1 = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, 1, (Half)0, new((Half)2f));
        var cp1 = network.Consume(activations, timers, [], new(1), [], [passageRead1]);
        activations = cp1.Activations; timers = cp1.Timers;

        Assert.Equal(1, network.GetCounterCount(counter.Id));
        Assert.False(network.IsCounterReached(counter.Id));
        var lampSlot = network.Nodes.ToArray().ToList().FindIndex(n => n.Id.Value == lamp.Id.Value);
        Assert.Equal(ActivationPhase.Clear, activations[lampSlot].Phase);

        // Same timestamp passage in next tick should be ignored (held / duplicate occurrence)
        var cpHeld = network.Consume(activations, timers, [], new(2), [], [passageRead1]);
        Assert.Equal(1, network.GetCounterCount(counter.Id));

        // Tick 2: passage 2 (distinct time: ordinal 2)
        var passageRead2 = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, 2, (Half)0, new((Half)2f));
        var cp2 = network.Consume(activations, timers, [], new(2), [], [passageRead2]);
        activations = cp2.Activations; timers = cp2.Timers;

        Assert.Equal(2, network.GetCounterCount(counter.Id));
        Assert.False(network.IsCounterReached(counter.Id));

        // Tick 3: passage 3 (target reached!)
        var passageRead3 = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, 3, (Half)0, new((Half)2f));
        var cp3 = network.Consume(activations, timers, [], new(3), [], [passageRead3]);
        activations = cp3.Activations; timers = cp3.Timers;

        Assert.Equal(3, network.GetCounterCount(counter.Id));
        Assert.True(network.IsCounterReached(counter.Id));

        // Lamp must be latched now!
        Assert.Equal(ActivationPhase.Latched, activations[lampSlot].Phase);
        Assert.Equal(ActivationOccurrenceKind.CounterReached, activations[lampSlot].Occurrence.Kind);

        // Saturation: 20 extra passages
        for (uint i = 4; i < 24; i++)
        {
            var passageReadExtra = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, i, (Half)0, new((Half)2f));
            var cpExtra = network.Consume(activations, timers, [], new(i), [], [passageReadExtra]);
            activations = cpExtra.Activations; timers = cpExtra.Timers;
        }

        // Remains reached and saturated
        Assert.True(network.IsCounterReached(counter.Id));
        Assert.Equal(ActivationPhase.Latched, activations[lampSlot].Phase);

        // Clear() restores counter to 0 and lamp to Clear
        activations = network.Clear();
        timers = network.ClearTimers();
        Assert.Equal(0, network.GetCounterCount(counter.Id));
        Assert.False(network.IsCounterReached(counter.Id));
        Assert.Equal(ActivationPhase.Clear, activations[lampSlot].Phase);
    }

    [Fact]
    public void WorkshopWire_SerializesAndDeserializesCounter()
    {
        var counter = new WorkshopCounter(new(10), default, new LocalPosition(0.1f, 0.2f, -0.3f), CanonicalRotation.Identity, 7, false);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new([counter]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(0), new(1), construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWire.Encode(cmd);
        var decodedCmd = WorkshopWire.DecodeCommand(bytes);
        Assert.NotNull(decodedCmd.Construction);
        var decodedCounter = Assert.IsType<WorkshopCounter>(Assert.Single(decodedCmd.Construction!.Value.Instances));
        Assert.Equal(counter.Id, decodedCounter.Id);
        Assert.Equal(counter.TargetCount, decodedCounter.TargetCount);
        Assert.Equal(counter.PartId, decodedCounter.PartId);
    }

    [Fact]
    public void WorkshopWireFlatBuffers_SerializesAndDeserializesCounter()
    {
        var counter = new WorkshopCounter(new(10), default, new LocalPosition(0.1f, 0.2f, -0.3f), CanonicalRotation.Identity, 5, false);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new([counter]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(0), new(1), construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decodedCmd = WorkshopWireFlatBuffers.DecodeCommand(bytes);
        Assert.NotNull(decodedCmd.Construction);
        var decodedCounter = Assert.IsType<WorkshopCounter>(Assert.Single(decodedCmd.Construction!.Value.Instances));
        Assert.Equal(counter.Id, decodedCounter.Id);
        Assert.Equal(counter.TargetCount, decodedCounter.TargetCount);
    }

    [Fact]
    public void WorkbenchCapacity_ValidatesCounterFootprint()
    {
        var fp = WorkbenchFootprint.Of(PartId.Counter);
        Assert.Equal(1, fp.Bodies);
        Assert.Equal(0, fp.DynamicBodies);
        Assert.Equal(2, fp.Colliders);
        Assert.Equal(0, fp.Triggers);
        Assert.Equal(0, fp.ContactWorks);
        Assert.Equal(1, fp.ActivationNodes);
        Assert.Equal(0, fp.ElectricalSources);
        Assert.Equal(0, fp.PrismaticConstraints);

        var counter = WorkshopInput.Counter(new(1), 0, 0, 0, 0, 0, 0, 1, 3);
        IWorkshopInstance[] instances = [counter];
        WorkbenchCapacity.Validate(instances);
    }
}
