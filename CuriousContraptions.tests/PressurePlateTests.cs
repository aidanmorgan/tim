using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class PressurePlateTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversPressurePlateDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.True(registry.Definitions.ContainsKey(PartId.PressurePlate));
        var def = registry.Definitions[PartId.PressurePlate];
        Assert.Equal(PartId.PressurePlate, def.PartId);
        Assert.Equal("pressure_plate", def.Id);
        Assert.Equal("Pressure plate", def.Title);
        Assert.Equal("Control", def.Category);
        Assert.NotNull(def.Scene);
        Assert.Empty(def.Parameters);

        var part = registry.Create(PartId.PressurePlate);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.PressurePlate, part.PartId);
            Assert.Equal(1.5f, part.PickRadius);

            var visual = part.FindChild("Visual", recursive: false) as Node3D;
            Assert.NotNull(visual);
            Assert.NotNull(visual.FindChild("Visual_Base", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Top", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_PowerIn", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Socket_Supply", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Ring", recursive: false));
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void PressurePlateScene_ExtractsExactTwoCollidersAndTwoPortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://parts/scenes/pressure_plate.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(instance);

            // Exactly 2 colliders: base + top
            Assert.Equal(2, colliders.Count);

            var baseCol = colliders[0];
            Assert.Equal(ColliderShapeKind.Box, baseCol.ShapeKind);
            Assert.Equal(new MetreVector((Half)1.1f, (Half)0.1f, (Half)1.1f), baseCol.HalfExtents);
            Assert.Equal((Half)0, baseCol.LocalPose.Translation.X);
            Assert.Equal((Half)(-0.12f), baseCol.LocalPose.Translation.Y);
            Assert.Equal((Half)0, baseCol.LocalPose.Translation.Z);

            var topCol = colliders[1];
            Assert.Equal(ColliderShapeKind.Box, topCol.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.9f, (Half)0.05f, (Half)0.9f), topCol.HalfExtents);
            Assert.Equal((Half)0, topCol.LocalPose.Translation.X);
            Assert.Equal((Half)0.03f, topCol.LocalPose.Translation.Y);
            Assert.Equal((Half)0, topCol.LocalPose.Translation.Z);

            Assert.Equal(2, ports.Count);
            Assert.True(ports.ContainsKey("power_in"));
            Assert.True(ports.ContainsKey("supply"));

            Assert.Equal(new Godot.Vector3(-1.18f, -0.08f, 0), ports["power_in"].Position);
            Assert.Equal(new Godot.Vector3(1.18f, -0.08f, 0), ports["supply"].Position);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void CanonicalColliders_MatchSceneExtractedColliders()
    {
        var canonical = WorkshopCanonicalColliders.For(PartId.PressurePlate);
        Assert.Equal(2, canonical.Count);
        Assert.Same(canonical, WorkshopCanonicalColliders.PressurePlateColliders);
    }

    [Fact]
    public void Ports_DeclarePowerInAndSupplySockets()
    {
        var ports = WorkshopPorts.For(PartId.PressurePlate).ToArray();
        Assert.Equal(2, ports.Length);

        var powerIn = ports.First(p => p.Socket == WorkshopSocket.PowerIn);
        Assert.Equal(WorkshopConnectionDomain.Electrical, powerIn.Domain);
        Assert.Equal(WorkshopPortDirection.Input, powerIn.Direction);

        var supply = ports.First(p => p.Socket == WorkshopSocket.Supply);
        Assert.Equal(WorkshopConnectionDomain.Electrical, supply.Domain);
        Assert.Equal(WorkshopPortDirection.Output, supply.Direction);

        var posIn = WorkshopPorts.LocalPosition(PartId.PressurePlate, WorkshopSocket.PowerIn);
        Assert.Equal(new System.Numerics.Vector3(-1.18f, -0.08f, 0f), posIn);

        var posOut = WorkshopPorts.LocalPosition(PartId.PressurePlate, WorkshopSocket.Supply);
        Assert.Equal(new System.Numerics.Vector3(1.18f, -0.08f, 0f), posOut);
    }

    [Fact]
    public void Connections_AcceptsElectricalAndRejectsActivation()
    {
        var plate = WorkshopInput.PressurePlate(new(1), 0, 0, 0, 0, 0, 0, 1);
        var battery = WorkshopInput.Battery(new(2), 2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var switchPart = WorkshopInput.Switch(new(3), -2, 0, 0, 0, 0, 0, 1, ContactTriggerSettings.Default);

        var instances = new WorkshopInstances(plate, battery, switchPart);

        // Valid electrical connection from battery Supply to plate PowerIn
        var validConn = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, plate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var validConnections = new WorkshopConnections(validConn);
        validConnections.Validate(instances);

        // Invalid: Activation connection to plate PowerIn must be rejected
        var invalidConn = new WorkshopConnection(switchPart.Id, WorkshopSocket.ActivationOut, plate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Activation);
        var invalidConnections = new WorkshopConnections(invalidConn);
        Assert.Throws<ArgumentException>(() => invalidConnections.Validate(instances));
    }

    [Fact]
    public void WorkshopPressurePlate_Validation()
    {
        // Admitted range [0.1, 16.0]
        new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 0.1f).Validate();
        new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 0.5f).Validate();
        new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 16.0f).Validate();

        Assert.Throws<ArgumentException>(() =>
            new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 0.05f).Validate());
        Assert.Throws<ArgumentException>(() =>
            new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 16.1f).Validate());
        Assert.Throws<ArgumentException>(() =>
            new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, -1.0f).Validate());
        Assert.Throws<ArgumentException>(() =>
            new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, float.NaN).Validate());
        Assert.Throws<ArgumentException>(() =>
            new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, float.PositiveInfinity).Validate());
    }

    [Fact]
    public void ContactLoadSensorDeclaration_Validation()
    {
        var decl = new ContactLoadSensorDeclaration(
            new(1), new(10),
            new MetreVector((Half)(-0.9f), (Half)0.02f, (Half)(-0.9f)),
            new MetreVector((Half)0.9f, (Half)0.12f, (Half)0.9f),
            new MetreVector((Half)0, (Half)1, (Half)0),
            (Half)0.7f,
            new Kilograms((Half)0.5f));
        decl.Validate();

        // Invalid id / frame
        Assert.Throws<ArgumentException>(() => (decl with { Id = default }).Validate());
        Assert.Throws<ArgumentException>(() => (decl with { Frame = default }).Validate());

        // Inverted bounds
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { RegionMinimum = decl.RegionMaximum }).Validate());

        // Invalid alignment
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { MinimumAlignment = (Half)0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { MinimumAlignment = (Half)1.5f }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { MinimumAlignment = Half.NaN }).Validate());

        // Invalid mass
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { MinimumMass = new(Half.Zero) }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { MinimumMass = new(Half.NegativeOne) }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (decl with { MinimumMass = new(Half.NaN) }).Validate());
    }

    [Fact]
    public void ContactLoadSensorTracker_MeasuresPhasesAndRestingMass()
    {
        var sensor = new ContactLoadSensorDeclaration(
            new(1), new(10),
            new MetreVector((Half)(-0.9f), (Half)0.02f, (Half)(-0.9f)),
            new MetreVector((Half)0.9f, (Half)0.12f, (Half)0.9f),
            new MetreVector((Half)0, (Half)1, (Half)0),
            (Half)0.7f,
            new Kilograms((Half)0.5f));
        var tracker = ContactLoadSensorTracker.Create([sensor]);

        var framePos = System.Numerics.Vector3.Zero;
        var frameRot = System.Numerics.Quaternion.Identity;
        var dynamicMasses = new Dictionary<GpuBodyId, float>
        {
            [new(20)] = 0.2f,
            [new(21)] = 0.35f,
            [new(22)] = 5.0f
        };

        // 1. Empty when no contacts
        var emptyRead = tracker.Measure(sensor.Id, framePos, frameRot, [], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Empty, emptyRead.Phase);
        Assert.Equal((Half)0, emptyRead.MeasuredMass.Value);
        Assert.Empty(emptyRead.TouchingBodies);

        // 2. Underweight when load < minimum mass
        var underweightContact = new ContactPointRecord(
            sensor.Frame, new(20),
            new System.Numerics.Vector3(0, 0.08f, 0),
            new System.Numerics.Vector3(0, 1, 0));
        var underweightRead = tracker.Measure(sensor.Id, framePos, frameRot, [underweightContact], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Underweight, underweightRead.Phase);
        Assert.Equal((Half)0.2f, underweightRead.MeasuredMass.Value);
        Assert.Single(underweightRead.TouchingBodies, new GpuBodyId(20));

        // 3. Loaded when single body meets/exceeds minimum mass
        var loadedContact = new ContactPointRecord(
            sensor.Frame, new(22),
            new System.Numerics.Vector3(0.2f, 0.08f, -0.1f),
            new System.Numerics.Vector3(0, 1, 0));
        var loadedRead = tracker.Measure(sensor.Id, framePos, frameRot, [loadedContact], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Loaded, loadedRead.Phase);
        Assert.Equal((Half)5.0f, loadedRead.MeasuredMass.Value);
        Assert.Single(loadedRead.TouchingBodies, new GpuBodyId(22));

        // 4. Sums multiple bodies resting on plate
        var contactA = new ContactPointRecord(sensor.Frame, new(20), new System.Numerics.Vector3(-0.3f, 0.08f, 0), System.Numerics.Vector3.UnitY);
        var contactB = new ContactPointRecord(sensor.Frame, new(21), new System.Numerics.Vector3(0.3f, 0.08f, 0), System.Numerics.Vector3.UnitY);
        var multiRead = tracker.Measure(sensor.Id, framePos, frameRot, [contactA, contactB], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Loaded, multiRead.Phase);
        Assert.Equal((Half)0.55f, multiRead.MeasuredMass.Value);
        Assert.Equal(2, multiRead.TouchingBodies.Count);
    }

    [Fact]
    public void ContactLoadSensorTracker_RejectsOffFaceAndUndersideContacts()
    {
        var sensor = new ContactLoadSensorDeclaration(
            new(1), new(10),
            new MetreVector((Half)(-0.9f), (Half)0.02f, (Half)(-0.9f)),
            new MetreVector((Half)0.9f, (Half)0.12f, (Half)0.9f),
            new MetreVector((Half)0, (Half)1, (Half)0),
            (Half)0.7f,
            new Kilograms((Half)0.5f));
        var tracker = ContactLoadSensorTracker.Create([sensor]);
        var dynamicMasses = new Dictionary<GpuBodyId, float> { [new(22)] = 5.0f };

        // Outside X
        var outsideX = new ContactPointRecord(sensor.Frame, new(22), new System.Numerics.Vector3(1.2f, 0.08f, 0), System.Numerics.Vector3.UnitY);
        Assert.Equal(ContactLoadPhase.Empty, tracker.Measure(sensor.Id, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, [outsideX], dynamicMasses).Phase);

        // Outside Z
        var outsideZ = new ContactPointRecord(sensor.Frame, new(22), new System.Numerics.Vector3(0, 0.08f, 1.2f), System.Numerics.Vector3.UnitY);
        Assert.Equal(ContactLoadPhase.Empty, tracker.Measure(sensor.Id, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, [outsideZ], dynamicMasses).Phase);

        // Underside (y = -0.15)
        var underside = new ContactPointRecord(sensor.Frame, new(22), new System.Numerics.Vector3(0, -0.15f, 0), -System.Numerics.Vector3.UnitY);
        Assert.Equal(ContactLoadPhase.Empty, tracker.Measure(sensor.Id, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, [underside], dynamicMasses).Phase);

        // Normal pointing wrong direction (normal = -UnitY)
        var wrongNormal = new ContactPointRecord(sensor.Frame, new(22), new System.Numerics.Vector3(0, 0.08f, 0), -System.Numerics.Vector3.UnitY);
        Assert.Equal(ContactLoadPhase.Empty, tracker.Measure(sensor.Id, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, [wrongNormal], dynamicMasses).Phase);
    }

    [Fact]
    public void ContactLoadSensorTracker_TransformsRotatedFrameCoordinates()
    {
        var sensor = new ContactLoadSensorDeclaration(
            new(1), new(10),
            new MetreVector((Half)(-0.9f), (Half)0.02f, (Half)(-0.9f)),
            new MetreVector((Half)0.9f, (Half)0.12f, (Half)0.9f),
            new MetreVector((Half)0, (Half)1, (Half)0),
            (Half)0.7f,
            new Kilograms((Half)0.5f));
        var tracker = ContactLoadSensorTracker.Create([sensor]);
        var dynamicMasses = new Dictionary<GpuBodyId, float> { [new(22)] = 5.0f };

        // Frame translated to (5, 2, -3) and rotated 90 degrees around Y:
        var framePos = new System.Numerics.Vector3(5, 2, -3);
        var frameRot = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, MathF.PI * 0.5f);

        var worldPoint = framePos + System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0.4f, 0.08f, 0), frameRot);
        var worldNormal = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, frameRot);

        var contact = new ContactPointRecord(sensor.Frame, new(22), worldPoint, worldNormal);
        var read = tracker.Measure(sensor.Id, framePos, frameRot, [contact], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Loaded, read.Phase);
        Assert.Equal((Half)5.0f, read.MeasuredMass.Value);
    }

    [Fact]
    public void ContactLoadSensorTracker_RemovalRestoresEmptyPhase()
    {
        var sensor = new ContactLoadSensorDeclaration(
            new(1), new(10),
            new MetreVector((Half)(-0.9f), (Half)0.02f, (Half)(-0.9f)),
            new MetreVector((Half)0.9f, (Half)0.12f, (Half)0.9f),
            new MetreVector((Half)0, (Half)1, (Half)0),
            (Half)0.7f,
            new Kilograms((Half)0.5f));
        var tracker = ContactLoadSensorTracker.Create([sensor]);
        var dynamicMasses = new Dictionary<GpuBodyId, float> { [new(22)] = 5.0f };

        var contact = new ContactPointRecord(sensor.Frame, new(22), new System.Numerics.Vector3(0, 0.08f, 0), System.Numerics.Vector3.UnitY);
        var loadedRead = tracker.Measure(sensor.Id, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, [contact], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Loaded, loadedRead.Phase);

        var emptyRead = tracker.Measure(sensor.Id, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, [], dynamicMasses);
        Assert.Equal(ContactLoadPhase.Empty, emptyRead.Phase);
        Assert.Equal((Half)0, emptyRead.MeasuredMass.Value);
    }

    [Fact]
    public void PhysicsCompiler_CompilesPressurePlateCollidersAndConnectsToPoweredGate()
    {
        var battery = WorkshopInput.Battery(new(1), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var plate = WorkshopInput.PressurePlate(new(2), 0, 0, 0, 0, 0, 0, 1);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);

        // Wiring: Battery.Supply -> Plate.PowerIn, Plate.Supply -> Gate.PowerIn
        var wire1 = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, plate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var wire2 = new WorkshopConnection(plate.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([battery, plate, gate]),
            Connections: new([wire1, wire2]));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));

        // Pressure plate colliders (base + top)
        var plateColliders = scene.Colliders.ToArray().Where(c => c.Body == plate.Id).ToArray();
        Assert.Equal(2, plateColliders.Length);

        // Powered gate blade has drag 0.09375f when supplied via plate
        var bladeId = WorkshopPhysicsCompiler.GateBlade(gate.Id);
        var blade = scene.Bodies.ToArray().Single(b => b.Id == bladeId);
        Assert.Equal((Half)0.09375f, blade.LinearDrag.Value);
    }

    [Fact]
    public void WorkshopWire_SerializesAndDeserializesPressurePlate()
    {
        var plate = new WorkshopPressurePlate(new(10), default, new LocalPosition(0.1f, 0.2f, -0.3f), CanonicalRotation.Identity, 3.5f, false);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new([plate]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(0), new(1), construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWire.Encode(cmd);
        var decodedCmd = WorkshopWire.DecodeCommand(bytes);
        Assert.NotNull(decodedCmd.Construction);
        var decodedPlate = Assert.IsType<WorkshopPressurePlate>(Assert.Single(decodedCmd.Construction!.Value.Instances));
        Assert.Equal(plate.Id, decodedPlate.Id);
        Assert.Equal(plate.MinimumMass, decodedPlate.MinimumMass);
        Assert.Equal(plate.PartId, decodedPlate.PartId);
    }

    [Fact]
    public void WorkshopWireFlatBuffers_SerializesAndDeserializesPressurePlate()
    {
        var plate = new WorkshopPressurePlate(new(10), default, new LocalPosition(0.1f, 0.2f, -0.3f), CanonicalRotation.Identity, 4.25f, false);
        var construction = new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(), new([plate]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(0), new(1), construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decodedCmd = WorkshopWireFlatBuffers.DecodeCommand(bytes);
        Assert.NotNull(decodedCmd.Construction);
        var decodedPlate = Assert.IsType<WorkshopPressurePlate>(Assert.Single(decodedCmd.Construction!.Value.Instances));
        Assert.Equal(plate.Id, decodedPlate.Id);
        Assert.Equal(plate.MinimumMass, decodedPlate.MinimumMass);
    }

    [Fact]
    public void WorkbenchCapacity_ValidatesPressurePlateFootprint()
    {
        var fp = WorkbenchFootprint.Of(PartId.PressurePlate);
        Assert.Equal(1, fp.Bodies);
        Assert.Equal(0, fp.DynamicBodies);
        Assert.Equal(2, fp.Colliders);
        Assert.Equal(0, fp.Triggers);
        Assert.Equal(0, fp.ContactWorks);
        Assert.Equal(0, fp.ActivationNodes);
        Assert.Equal(0, fp.ElectricalSources);
        Assert.Equal(0, fp.PrismaticConstraints);

        var plate = WorkshopInput.PressurePlate(new(1), 0, 0, 0, 0, 0, 0, 1);
        IWorkshopInstance[] instances = [plate];
        WorkbenchCapacity.Validate(instances);
    }
}
