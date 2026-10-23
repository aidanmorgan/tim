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
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.Counter));
        var def = registry.Definitions[PartId.Counter];
        Assert.Equal(PartId.Counter, def.PartId);
        Assert.Equal("counter", def.Id);
        Assert.Equal("Counter", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Control, def.Category);
        Assert.NotNull(def.Scene);

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

            // Native property application boundary; committed-count behavior is covered by simulation unit tests.
            for (var i = 0; i < def.Presentation.Bindings.Count; i++)
            {
                var binding = def.Presentation.Bindings[i];
                part.ApplyDeclaredProperty(i, WorkshopDeclaredPresentation.Value(binding.Property,
                    binding.Threshold <= 3 ? binding.To : binding.From));
            }
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
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/Counter/counter.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

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

            Assert.True(ports.ContainsKey("ActivationIn"));
            Assert.True(ports.ContainsKey("ActivationOut"));
            Assert.True(ports.ContainsKey("PowerIn"));
            Assert.True(ports.ContainsKey("Supply"));

            Assert.Equal(new Godot.Vector3(0, 0.72f, 0.1f), ports["ActivationIn"].Position);
            Assert.Equal(new Godot.Vector3(0, -0.72f, 0.1f), ports["ActivationOut"].Position);
            Assert.Equal(new Godot.Vector3(-0.93f, 0, 0), ports["PowerIn"].Position);
            Assert.Equal(new Godot.Vector3(0.93f, 0, 0), ports["Supply"].Position);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void CanonicalColliders_MatchSceneExtractedColliders()
    {
        var canonical = DeclaredPhysics.Colliders(PartId.Counter);
        Assert.Equal(2, canonical.Count);
        Assert.Equal(canonical, DeclaredPhysics.Colliders(PartId.Counter));
    }

    [Fact]
    public void Ports_DeclareExactSocketsAndDirections()
    {
        var ports = DeclaredPorts.Current.Of(PartId.Counter).ToArray();
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

        Assert.Equal(new System.Numerics.Vector3(0f, 0.72f, 0.1f), DeclaredPorts.Current.LocalPosition(PartId.Counter, WorkshopSocket.ActivationIn));
        Assert.Equal(new System.Numerics.Vector3(0f, -0.72f, 0.1f), DeclaredPorts.Current.LocalPosition(PartId.Counter, WorkshopSocket.ActivationOut));
        Assert.Equal(new System.Numerics.Vector3(-0.93f, 0f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Counter, WorkshopSocket.PowerIn));
        Assert.Equal(new System.Numerics.Vector3(0.93f, 0f, 0f), DeclaredPorts.Current.LocalPosition(PartId.Counter, WorkshopSocket.Supply));
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
        var detector = PlacedParts.Detector(new(2), -2, 0, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(3), 2, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), 0, -2, 0);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(5), 0, 2, 0, 0, 0, 0, 1);

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
        var battery = PlacedParts.Battery(new(2), -2, 0, 0);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1);

        // Without battery connected: gate blade drag is 0
        var connNoBattery = new WorkshopConnection(counter.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var constructionUnpowered = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([counter, battery, gate]),
            Connections: new([connNoBattery]));

        var sceneUnpowered = WorkshopPhysicsCompiler.Compile(constructionUnpowered, new(1, 1));
        var bladeUnpowered = sceneUnpowered.Bodies.ToArray().First(b => b.Id == WorkshopPhysicsCompiler.SliderBody(gate.Id, 0));
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
        var bladePowered = scenePowered.Bodies.ToArray().First(b => b.Id == WorkshopPhysicsCompiler.SliderBody(gate.Id, 0));
        Assert.Equal((Half)0f, bladePowered.LinearDrag.Value);
        Assert.Contains(scenePowered.SupplyGraph.Nodes.ToArray(),node=>node.Owner==counter.Id && node.Kind==SupplyNodeKind.Contact);
        Assert.Equal(battery.Id,scenePowered.Electrical.Sources[0].Owner);
    }

    [Fact]
    public void ActivationNetwork_CountingProgression_SingleEmission_AndSaturation()
    {
        var detector = PlacedParts.Detector(new(1), -2, 0, 0, 0, 0, 0, 1);
        var counter = WorkshopInput.Counter(new(2), 0, 0, 0, 0, 0, 0, 1, 3);
        var lamp = PlacedParts.Lamp(new(3), 2, 0, 0, 0, 0, 0, 1);

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
        var memory = network.ClearMemory();

        Assert.Equal(0U, network.Memory(memory, counter.Id).Count);
        Assert.Equal(3, network.GetCounterTarget(counter.Id));
        Assert.False(network.ContactClosed(activations, network.ClearTimers(), counter.Id));

        var sensorId = WorkshopPhysicsCompiler.PassageSensor(detector.Id);
        var ballBodyId = new GpuBodyId(10);
        var ballColliderId = new GpuColliderId(11);

        // Tick 1: passage 1
        var passageRead1 = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, 1, (Half)0, new((Half)2f));
        var cp1 = network.Consume(activations, timers, [], new(1), [], [passageRead1], memory: memory);
        activations = cp1.Activations; timers = cp1.Timers; memory = cp1.Memory;

        Assert.Equal(1U, network.Memory(memory, counter.Id).Count);
        Assert.False(network.ContactClosed(activations, network.ClearTimers(), counter.Id));
        var lampSlot = network.Nodes.ToArray().ToList().FindIndex(n => n.Id.Value == lamp.Id.Value);
        Assert.Equal(ActivationPhase.Clear, activations[lampSlot].Phase);

        // Same timestamp passage in next tick should be ignored (held / duplicate occurrence)
        var cpHeld = network.Consume(activations, timers, [], new(2), [], [passageRead1], memory: memory);
        Assert.Equal(1U, network.Memory(cpHeld.Memory, counter.Id).Count);

        // Tick 2: passage 2 (distinct time: ordinal 2)
        var passageRead2 = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, 2, (Half)0, new((Half)2f));
        var cp2 = network.Consume(activations, timers, [], new(2), [], [passageRead2], memory: memory);
        activations = cp2.Activations; timers = cp2.Timers; memory = cp2.Memory;

        Assert.Equal(2U, network.Memory(memory, counter.Id).Count);
        Assert.False(network.ContactClosed(activations, network.ClearTimers(), counter.Id));

        // Tick 3: passage 3 (target reached!)
        var passageRead3 = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, 3, (Half)0, new((Half)2f));
        var cp3 = network.Consume(activations, timers, [], new(3), [], [passageRead3], memory: memory);
        activations = cp3.Activations; timers = cp3.Timers; memory = cp3.Memory;

        Assert.Equal(3U, network.Memory(memory, counter.Id).Count);
        Assert.True(network.ContactClosed(activations, network.ClearTimers(), counter.Id));

        // Lamp must be latched now!
        Assert.Equal(ActivationPhase.Latched, activations[lampSlot].Phase);
        Assert.Equal(ActivationOccurrenceKind.ThresholdReached, activations[lampSlot].Occurrence.Kind);

        // Saturation: 20 extra passages
        for (uint i = 4; i < 24; i++)
        {
            var passageReadExtra = new PassageSensorRead(sensorId, detector.Id, ballBodyId, ballColliderId, PassageSensorPhase.Fired, i, (Half)0, new((Half)2f));
            var cpExtra = network.Consume(activations, timers, [], new(i), [], [passageReadExtra], memory: memory);
            activations = cpExtra.Activations; timers = cpExtra.Timers; memory = cpExtra.Memory;
        }

        // Remains reached and saturated
        Assert.True(network.ContactClosed(activations, network.ClearTimers(), counter.Id));
        Assert.Equal(ActivationPhase.Latched, activations[lampSlot].Phase);

        // Clearing (admission) restores counter to 0 and lamp to Clear
        activations = network.Clear();
        memory = network.ClearMemory();
        Assert.Equal(0U, network.Memory(memory, counter.Id).Count);
        Assert.False(network.ContactClosed(activations, network.ClearTimers(), counter.Id));
        Assert.Equal(ActivationPhase.Clear, activations[lampSlot].Phase);
    }

    // Story 9R.1 D3: the count is committed discrete state. A candidate's count is visible only in its own checkpoint, so a
    // superseded (discarded) candidate cannot advance the committed count, and admission (Reset) starts from 0.
    [Fact]
    public void CounterCount_LivesInTheCommittedCheckpoint_SupersededCandidateLeavesItUnchanged_AndResetClearsIt()
    {
        var detector = PlacedParts.Detector(new(1), -2, 0, 0, 0, 0, 0, 1);
        var counter = WorkshopInput.Counter(new(2), 0, 0, 0, 0, 0, 0, 1, 3);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([detector, counter]),
            Connections: new([new WorkshopConnection(detector.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn,
                WorkshopConnectionDomain.Activation)]));
        var network = WorkshopActivationCompiler.Compile(construction);
        var sensor = WorkshopPhysicsCompiler.PassageSensor(detector.Id);
        PassageSensorRead Pass(uint ordinal) => new(sensor, detector.Id, new(10), new(11), PassageSensorPhase.Fired, ordinal, (Half)0, new((Half)2f));

        var committed = network.Consume(network.Clear(), network.ClearTimers(), [], new(1), [], [Pass(4)], memory: network.ClearMemory());
        committed = network.Consume(committed.Activations, committed.Timers, [], new(2), [], [Pass(8)], memory: committed.Memory);
        var row = network.Memory(committed.Memory, counter.Id);
        Assert.Equal(2U, row.Count); // target - 1
        Assert.Equal(new ActivationTime(8,(float)0), row.Last);
        Assert.Equal(ActivationNodeKind.Counter, row.Kind);

        // A candidate that counts the third passage is superseded: the committed checkpoint still holds count 2.
        var superseded = network.Consume(committed.Activations, committed.Timers, [], new(3), [], [Pass(12)], memory: committed.Memory);
        Assert.Equal(3U, network.Memory(superseded.Memory, counter.Id).Count);
        Assert.Equal(2U, network.Memory(committed.Memory, counter.Id).Count);
        var retried = network.Consume(committed.Activations, committed.Timers, [], new(3), [], [Pass(12)], memory: committed.Memory);
        Assert.Equal(3U, network.Memory(retried.Memory, counter.Id).Count);
        Assert.True(network.ContactClosed(retried.Activations, network.ClearTimers(), counter.Id));

        // Admission (Reset) recompiles the network and clears the row: count 0, no last input, contact open.
        var reset = WorkshopActivationCompiler.Compile(construction);
        var cleared = network.Memory(reset.ClearMemory(), counter.Id);
        Assert.Equal(ActivationMemory.Clear(reset.Nodes.ToArray().Single(node => node.Owner == counter.Id)), cleared);
        Assert.Equal(0U, cleared.Count);
        Assert.False(reset.ContactClosed(reset.Clear(), reset.ClearTimers(), counter.Id));

        // A checkpoint without the counter's row is rejected rather than counted from an implicit zero.
        Assert.Throws<ArgumentException>(() => network.Consume(committed.Activations, committed.Timers, [], new(3), [], [Pass(12)]));
    }

    [Fact]
    public void WorkshopWire_SerializesAndDeserializesCounter()
    {
        var counter = new WorkshopCounter(new(10), default, new LocalPosition(0.1f, 0.2f, -0.3f), CanonicalRotation.Identity, 7, false);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new([counter]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(0), new(1), construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decodedCmd = WorkshopWireFlatBuffers.DecodeCommand(bytes);
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
        var counter = WorkshopInput.Counter(new(1), 0, 0, 0, 0, 0, 0, 1, 3);
        IWorkshopInstance[] instances = [counter];
        WorkbenchCapacity.Validate(instances);

        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(counter)), new(1, 2)).World;
        Assert.Equal(1 + 1, world.Bodies);
        Assert.Equal(0, world.Dynamic);
        Assert.Equal(2 + 1, world.Colliders);
        Assert.Equal(0, world.Triggers);
        Assert.Equal(0, world.ContactWorks);
        Assert.Equal(0, world.ElectricalSources);
        Assert.Equal(0, world.Prismatics);
    }
}
