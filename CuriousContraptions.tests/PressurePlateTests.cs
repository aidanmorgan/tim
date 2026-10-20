using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
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
        var plate = WorkshopInput.PressurePlate(new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
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
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => invalidConnections.Validate(instances));
    }

    [Fact]
    public void WorkshopPressurePlate_Validation()
    {
        // Admitted range [0.1, 16.0]
        new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 0.1f).Validate();
        new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 0.5f).Validate();
        new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 16.0f).Validate();

        Assert.Throws<ArgumentException>(() => new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 0.05f).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, 16.1f).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, -1.0f).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, float.NaN).Validate());
        Assert.Throws<ArgumentException>(() => new WorkshopPressurePlate(new(1), default, default, CanonicalRotation.Identity, float.PositiveInfinity).Validate());
    }

    // ---- Story 9R.4: the compiled contact-load row and ContactLoadSensorTracker (D1, D4) ----
    private static readonly Vector3 Up = Vector3.UnitY;
    private static ContactLoadSensorDeclaration Row(float minimum = .5f) => new(new(1), new(10), new(11),
        -.9f - WorkshopPressurePlate.ContactDistance, .9f + WorkshopPressurePlate.ContactDistance,
        -.9f - WorkshopPressurePlate.FootprintDepthMargin, .9f + WorkshopPressurePlate.FootprintDepthMargin,
        Up, WorkshopPressurePlate.MinimumAlignment, WorkshopPressurePlate.ContactDistance, minimum);
    /// <summary>A resting contact on the row's face (frame 10, collider 11) at local (x, 0.08, z), gap −0.00027 m as the engine reports.</summary>
    private static ContactPointRecord On(ulong body, float mass, float x = 0, float z = 0, float gap = -.00027f, ulong collider = 11,
        Vector3? normal = null) => new(new(10), new(collider), new(body), mass, new(x, .08f, z), normal ?? Up, gap);
    private static float Measure(ContactLoadSensorDeclaration row, params ContactPointRecord[] contacts) =>
        ContactLoadSensorTracker.Measure(row, Vector3.Zero, Quaternion.Identity, contacts, new GpuBodyId[contacts.Length]);

    [Fact]
    public void ContactLoadSensorDeclaration_KeepsItsF32ValuesAndRejectsInvalidRows()
    {
        var row = Row(.3f);
        row.Validate();
        // D1: the f32 row keeps CAT-052's alignment (1 − 10⁻⁶, which binary16 rounds to 1) and a non-dyadic minimum exactly.
        Assert.NotEqual(1f, row.MinimumAlignment);
        Assert.Equal(1f - 1e-6f, row.MinimumAlignment);
        Assert.Equal(.3f, row.MinimumMass);
        Assert.Equal((Half)1, (Half)row.MinimumAlignment); // control: the deleted Half field lost the instance value

        Assert.Throws<ArgumentException>(() => (row with { Id = default }).Validate());
        Assert.Throws<ArgumentException>(() => (row with { Frame = default }).Validate());
        Assert.Throws<ArgumentException>(() => (row with { Face = default }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumX = row.MaximumX }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MaximumZ = float.NaN }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { LocalNormal = new(0, 2, 0) }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumAlignment = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumAlignment = 1.5f }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumAlignment = float.NaN }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MaximumGap = -1e-6f }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MaximumGap = float.PositiveInfinity }).Validate());
        // No default threshold stands in for a missing or zero value.
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumMass = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumMass = -1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumMass = float.NaN }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (row with { MinimumMass = float.PositiveInfinity }).Validate());
    }

    [Fact]
    public void ContactLoadPhase_IsTheInclusiveF32Comparison()
    {
        var row = Row(1f);
        Assert.Equal(ContactLoadPhase.Empty, row.PhaseOf(0));
        Assert.Equal(ContactLoadPhase.Underweight, row.PhaseOf(MathF.BitDecrement(1f)));
        Assert.Equal(ContactLoadPhase.Loaded, row.PhaseOf(1f)); // exactly at the minimum (inclusive ≥)
        Assert.Equal(ContactLoadPhase.Underweight, Row(MathF.BitIncrement(1f)).PhaseOf(1f)); // the next f32 above stays Underweight
    }

    [Fact]
    public void ContactLoadSensorTracker_CountsEachBodyOnceAndSumsBodies()
    {
        var domino = (float)(Half).4f;
        Assert.Equal(0f, Measure(Row()));
        Assert.Equal(domino, Measure(Row(), On(20, domino, -.2f, -.3f), On(20, domino, .2f, -.3f), On(20, domino, .2f, .3f), On(20, domino, -.2f, .3f)));
        Assert.Equal(ContactLoadPhase.Underweight, Row().PhaseOf(Measure(Row(), On(20, domino, -.2f), On(20, domino, .2f))));
        Assert.Equal(2 * domino, Measure(Row(), On(20, domino, -.4f), On(21, domino, .4f)));
        Assert.Equal(ContactLoadPhase.Loaded, Row().PhaseOf(Measure(Row(), On(20, domino, -.4f), On(21, domino, .4f))));
    }

    [Fact]
    public void ContactLoadSensorTracker_CountsOnlyRestingContactsOnTheFace()
    {
        var row = Row();
        Assert.Equal(1f, Measure(row, On(22, 1)));
        Assert.Equal(1f, Measure(row, On(22, 1, gap: WorkshopPressurePlate.ContactDistance))); // gap = d still rests
        // Lifted: a ball held 0.002 m above the face is a speculative contact, not a load (coordinator item 3).
        Assert.Equal(0f, Measure(row, On(22, 1, gap: .002f)));
        Assert.Equal(0f, Measure(row, On(22, 1, gap: MathF.BitIncrement(WorkshopPressurePlate.ContactDistance))));
        Assert.Equal(0f, Measure(row, On(22, 1, x: .9f + 2 * WorkshopPressurePlate.ContactDistance)));  // outside x
        Assert.Equal(0f, Measure(row, On(22, 1, z: .9f + 2e-6f)));                                         // outside z
        Assert.Equal(0f, Measure(row, On(22, 1, normal: Vector3.UnitX)));                                   // side
        Assert.Equal(0f, Measure(row, On(22, 1, normal: -Up)));                                             // underside
        Assert.Equal(0f, Measure(row, On(22, 1, normal: Vector3.Normalize(new(.01f, 1, 0)))));            // below 1 − 10⁻⁶
        Assert.Equal(0f, Measure(row, On(22, 1, collider: 12)));                                            // the base, not the face
        Assert.Equal(0f, Measure(row, On(22, 0)));                                                          // a static other
        Assert.Equal(0f, Measure(row, new ContactPointRecord(new(30), new(11), new(22), 1, new(0, .08f, 0), Up, 0))); // another frame
    }

    [Fact]
    public void ContactLoadSensorTracker_TransformsARotatedFrame()
    {
        var position = new Vector3(5, 2, -3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6); // CAT-052 P12: a 30° plate
        var point = position + Vector3.Transform(new Vector3(.4f, .08f, 0), rotation);
        var contact = new ContactPointRecord(new(10), new(11), new(22), 4, point, Vector3.Transform(Up, rotation), -.0002f);
        Assert.Equal(4f, ContactLoadSensorTracker.Measure(Row(), position, rotation, [contact], new GpuBodyId[1]));
        Assert.Equal(0f, ContactLoadSensorTracker.Measure(Row(), position, Quaternion.Identity, [contact], new GpuBodyId[1]));
    }

    [Fact]
    public void ContactLoadSensorTracker_RequiresOneScratchSlotPerContact() =>
        Assert.Throws<ArgumentException>(() => ContactLoadSensorTracker.Measure(Row(), Vector3.Zero, Quaternion.Identity,
            [On(20, 1), On(21, 1)], new GpuBodyId[1]));

    [Fact]
    public void ContactLoadSensorTracker_MeasuresWithoutAllocating()
    {
        ContactPointRecord[] contacts = [On(20, 1, -.4f), On(20, 1, -.3f), On(21, 1, .4f)];
        var scratch = new GpuBodyId[contacts.Length];
        var row = Row();
        ContactLoadSensorTracker.Measure(row, Vector3.Zero, Quaternion.Identity, contacts, scratch);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) ContactLoadSensorTracker.Measure(row, Vector3.Zero, Quaternion.Identity, contacts, scratch);
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    // AC1/AC2: each plate compiles its own row from its instance (CAT-052 §2) and its supplied contact is a Switched route.
    [Fact]
    public void PhysicsCompiler_EmitsEachPlatesOwnRowAndASwitchedRouteThroughIt()
    {
        var battery = WorkshopInput.Battery(new(1), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var plate = WorkshopInput.PressurePlate(new(2), 0, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
        var heavy = WorkshopInput.PressurePlate(new(4), 0, 0, 4, 0, 0, 0, 1, 2f);
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new([battery, plate, gate, heavy]), Connections: new([Supply(battery.Id, plate.Id), Supply(plate.Id, gate.Id)])), new(1, 1));

        Assert.Equal(2, scene.Colliders.ToArray().Count(c => c.Body == plate.Id));
        var rows = scene.ContactLoadSensors.ToArray();
        Assert.Equal(2, rows.Length);
        foreach (var (instance, row) in new[] { (plate, rows.Single(r => r.Frame == plate.Id)), (heavy, rows.Single(r => r.Frame == heavy.Id)) })
        {
            Assert.Equal(WorkshopPhysicsCompiler.ContactLoadSensor(instance.Id), row.Id);
            var face = scene.Colliders.ToArray().Single(c => c.Id == row.Face);
            Assert.Equal((instance.Id, (Half).03f, new MetreVector((Half).9f, (Half).05f, (Half).9f)), (face.Body, face.Pose.Translation.Y, face.HalfExtents));
            Assert.Equal((-.9001f, .9001f, -.900001f, .900001f), (row.MinimumX, row.MaximumX, row.MinimumZ, row.MaximumZ));
            Assert.Equal((Up, 1f - 1e-6f, .0001f), (row.LocalNormal, row.MinimumAlignment, row.MaximumGap));
            Assert.Equal(instance.MinimumMass, row.MinimumMass);
        }
        var route = Assert.Single(scene.SupplyRoutes.ToArray());
        Assert.Equal((SupplyRouteKind.Switched, plate.Id), (route.Kind, route.Switch));
        Assert.Equal(battery.Id, scene.Electrical.Sources[route.Source].Owner);
        Assert.Equal(gate.Id, scene.Prismatics[route.Prismatic].BodyA);
        // The deleted drag encoding: the blade declares no drag whatever feeds it.
        Assert.Equal((Half)0, scene.Bodies.ToArray().Single(b => b.Id == WorkshopPhysicsCompiler.GateBlade(gate.Id)).LinearDrag.Value);
    }

    [Fact]
    public void PhysicsAbi_AdmitsEachRowAsF32WithAnEmptyCommittedState()
    {
        var plate = WorkshopInput.PressurePlate(new(2), 0, 0, 0, 0, 0, 0, 1, .3f);
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([plate])), new(1, 1));
        var world = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(1, PhysicsGpuAbi.ReadContactLoadCount(world));
        Assert.Equal(scene.ContactLoadSensors[0], PhysicsGpuAbi.ReadContactLoadDeclaration(world, 0));
        var read = PhysicsGpuAbi.ReadContactLoadSensor(world, 0);
        Assert.Equal((ContactLoadPhase.Empty, 0f, plate.Id), (read.Phase, read.MeasuredMass, read.Frame));
        Assert.True(PhysicsGpuAbi.CollidersOffset >= PhysicsGpuAbi.ContactLoadRecordsOffset +
            PhysicsSceneDeclaration.ContactLoadSensorCapacity * PhysicsGpuAbi.ContactLoadSensorBytes);
        // A committed phase that disagrees with its f32 mass is rejected.
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(world.AsSpan(PhysicsGpuAbi.ContactLoadRecordsOffset + PhysicsGpuAbi.ContactLoadPhaseOffset),
            (uint)ContactLoadPhase.Loaded);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadContactLoadSensor(world, 0));
    }

    private static WorkshopConnection Supply(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

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
        Assert.Equal(1, fp.ActivationNodes); // Story 9R.4 D3: the plate's ContactLoadSource level
        Assert.Equal(0, fp.ElectricalSources);
        Assert.Equal(0, fp.PrismaticConstraints);

        var plate = WorkshopInput.PressurePlate(new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
        IWorkshopInstance[] instances = [plate];
        WorkbenchCapacity.Validate(instances);
    }

    // ---- Story 9R.4: the live path ----
    // These benches run the production WorkshopSimulation over the worker's own transport (WorkshopPhysicsEngineTests.RunPhases):
    // compiler → WorkshopPhysicsEngine (ContactLoadSensorTracker on each plate's own compiled row) → WorkshopGpuDevice →
    // ActivationNetwork (the plate's committed ContactLoadSource level) → that gate's own switched supply route. No tick input is
    // set by hand: the only player input is the authored construction.
    private const double GateHeight = 3;
    private const float ClosedTravel = .2f, OpenTravel = 1.3f;
    private const int Ticks = 3 * 120;
    private static readonly double PlateY = (double)WorkshopConstruction.WorkbenchSurface.Value + .22; // base bottom on the bench
    private static readonly double Face = PlateY + .08;

    private static WorkshopGenericInstance Gate(ulong id, double z) =>
        WorkshopInput.Generic(PartId.PoweredGate, new(id), 0, GateHeight, z, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
    private static WorkshopBattery Battery(ulong id, double z) =>
        WorkshopInput.Battery(new(id), 3, GateHeight, z, 0, 0, 0, 1, ElectricalSourceSettings.Default with { Enabled = ElectricalEnable.Enabled });
    private static WorkshopPressurePlate Plate(ulong id, double x, double z, float minimum) =>
        WorkshopInput.PressurePlate(new(id), x, PlateY, z, 0, 0, 0, 1, minimum);
    private static WorkshopBall BallOn(ulong id, double x, double z) => WorkshopInput.Basketball(new(id), x, Face + .35, z, 0, 0, 0, 1);
    private static WorkshopDomino StandingOn(ulong id, double x, double z) => WorkshopInput.Domino(new(id), x, Face + .551, z, 0, 0, 0, 1);
    /// <summary>A Domino lying on its broad face (1.1 m along x, 0.65 m along z, 0.25 m tall) with its underside at <paramref name="under"/>.</summary>
    private static WorkshopDomino FlatOn(ulong id, double x, double under, double z) =>
        WorkshopInput.Domino(new(id), x, under + .126, z, 0, 0, Math.Sqrt(.5), Math.Sqrt(.5));
    private static WorkshopConnection[] Circuit(WorkshopBattery battery, WorkshopPressurePlate plate, WorkshopGenericInstance gate) =>
        [Supply(battery.Id, plate.Id), Supply(plate.Id, gate.Id)];
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] connections) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(connections));
    private static async Task<List<WorkshopRead>> RunBench(WorkshopConstruction construction, int ticks = Ticks) =>
        (await WorkshopPhysicsEngineTests.RunPhases(construction, ([], ticks)))[0];

    private static float Travel(WorkshopRead read, WorkshopGenericInstance gate)
    {
        Assert.True(read.Bodies.TryGet(WorkshopPhysicsCompiler.GateBlade(gate.Id), out var blade));
        return (blade.Body.Cell.Y + blade.Body.Local.Y) / 16f - (float)GateHeight;
    }
    /// <summary>The plate's committed contact (its ContactLoadSource level): closed exactly while the committed phase is Loaded.</summary>
    private static bool ContactClosed(WorkshopRead read, WorkshopPressurePlate plate)
    {
        for (var i = 0; i < read.Activations.Count; i++)
            if (read.Activations[i].Owner == plate.Id) return read.Activations[i].Phase == ActivationPhase.Latched;
        throw new InvalidOperationException($"Plate {plate.Id.Value} commits no contact-load level.");
    }
    private static void AssertGateClosedThroughout(List<WorkshopRead> reads, WorkshopPressurePlate plate, WorkshopGenericInstance gate, string label) =>
        Assert.All(reads, read => Assert.True(!ContactClosed(read, plate) && Travel(read, gate) < ClosedTravel,
            $"{label}: contact {ContactClosed(read, plate)}, travel {Travel(read, gate)} at tick {read.Tick.Value}"));
    private static void AssertGateOpened(List<WorkshopRead> reads, WorkshopPressurePlate plate, WorkshopGenericInstance gate, string label) =>
        Assert.True(ContactClosed(reads[^1], plate) && Travel(reads[^1], gate) > OpenTravel,
            $"{label}: contact {ContactClosed(reads[^1], plate)}, travel {Travel(reads[^1], gate)}");

    // Matrix rows Loaded, Exactly at minimum, Instance threshold (1 ball then 2 balls side by side at 2.0 kg) and the f32 boundary:
    // the instance minimum_mass, compared inclusively in f32 before any rounding, decides the plate's own gate.
    [Theory]
    [InlineData(.5f, 1, true)]
    [InlineData(1f, 1, true)]          // a 1 kg Basketball exactly at the minimum (inclusive ≥)
    [InlineData(2f, 1, false)]         // the instance's 2.0 kg, not a default, governs
    [InlineData(2f, 2, true)]          // two Basketballs side by side sum to 2 kg
    public async Task TheInstanceMinimumMassDecidesItsOwnGate(float minimum, int balls, bool opens)
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, minimum);
        IWorkshopInstance[] load = balls == 1 ? [BallOn(4, -4, 0)] : [BallOn(4, -4.4, 0), BallOn(5, -3.6, 0)];
        var bench = Bench([gate, battery, plate, .. load], Circuit(battery, plate, gate));
        if (minimum == 2f)
        {
            // AC7 (Murdoch-10 B1): the non-default 2.0 kg is saved and loaded through the real save codec, reloads bit for bit,
            // and the reloaded construction is what runs.
            var reloaded = WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(7)))).Construction;
            var reloadedPlate = Assert.Single(reloaded.Instances.OfType<WorkshopPressurePlate>());
            Assert.Equal(BitConverter.SingleToUInt32Bits(2f), BitConverter.SingleToUInt32Bits(reloadedPlate.MinimumMass));
            bench = reloaded;
        }
        var reads = await RunBench(bench);
        if (opens) AssertGateOpened(reads, plate, gate, $"{balls} ball(s) at {minimum} kg");
        else AssertGateClosedThroughout(reads, plate, gate, $"{balls} ball(s) at {minimum} kg");
    }

    [Fact]
    public async Task AMinimumOneF32AboveTheBodyMassStaysUnderweight()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, MathF.BitIncrement(1f));
        var reads = await RunBench(Bench([gate, battery, plate, BallOn(4, -4, 0)], Circuit(battery, plate, gate)));
        AssertGateClosedThroughout(reads, plate, gate, "1 kg on a minimum one f32 above 1 kg");
    }

    // Matrix rows Underweight and Sum: a resting Domino counts (the region is the face's upward normal and the contact gap,
    // not a y band a resting box straddles); one 0.4 kg Domino is Underweight at 0.5 kg, two sum to Loaded.
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task OneDominoIsUnderweightAndTwoDominoesSum(int dominoes, bool opens)
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, .5f);
        IWorkshopInstance[] load = dominoes == 1 ? [StandingOn(4, -4, 0)] : [StandingOn(4, -4.4, 0), StandingOn(5, -3.6, 0)];
        var reads = await RunBench(Bench([gate, battery, plate, .. load], Circuit(battery, plate, gate)));
        if (opens) AssertGateOpened(reads, plate, gate, $"{dominoes} domino(es)");
        else AssertGateClosedThroughout(reads, plate, gate, $"{dominoes} domino(es)");
    }

    // Matrix row Stacked: only a body in direct contact with the face counts. Two 0.4 kg Dominoes stacked flat read 0.4 kg.
    [Fact]
    public async Task OnlyDirectContactCountsForAStack()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, .5f);
        var reads = await RunBench(Bench([gate, battery, plate, FlatOn(4, -4, Face, 0), FlatOn(5, -4, Face + .251, 0)],
            Circuit(battery, plate, gate)));
        AssertGateClosedThroughout(reads, plate, gate, "two stacked dominoes");
    }

    // Matrix row Two plates (negative control for cross-talk): loading P1 opens only G1.
    [Fact]
    public async Task TwoPlatesSwitchOnlyTheirOwnGates()
    {
        var gate1 = Gate(1, -3); var battery1 = Battery(2, -3); var plate1 = Plate(3, -4, -3, .5f);
        var gate2 = Gate(4, 3); var battery2 = Battery(5, 3); var plate2 = Plate(6, -4, 3, .5f);
        var reads = await RunBench(Bench([gate1, battery1, plate1, gate2, battery2, plate2, BallOn(7, -4, -3)],
            [.. Circuit(battery1, plate1, gate1), .. Circuit(battery2, plate2, gate2)]));
        AssertGateOpened(reads, plate1, gate1, "loaded P1");
        AssertGateClosedThroughout(reads, plate2, gate2, "empty P2");
    }

    // Matrix row Bridge: one flat Domino resting across two adjacent plates is in direct contact with both faces, so each plate
    // counts its full 0.4 kg against its own 0.1 kg minimum and both gates open.
    [Fact]
    public async Task ABoxBridgingTwoPlatesLoadsBoth()
    {
        var gate1 = Gate(1, -3); var battery1 = Battery(2, -3); var plate1 = Plate(3, -5.1, 0, .1f);
        var gate2 = Gate(4, 3); var battery2 = Battery(5, 3); var plate2 = Plate(6, -2.9, 0, .1f);
        var reads = await RunBench(Bench([gate1, battery1, plate1, gate2, battery2, plate2, FlatOn(7, -4, Face, 0)],
            [.. Circuit(battery1, plate1, gate1), .. Circuit(battery2, plate2, gate2)]));
        AssertGateOpened(reads, plate1, gate1, "bridged P1");
        AssertGateOpened(reads, plate2, gate2, "bridged P2");
    }

    // Matrix row Size impostor (negative control for size detection): a static box with the plate face's exact half-extents and
    // pose but no plate declaration never switches a gate. A real 2.0 kg plate is wired to the gate beside it; the 1 kg ball
    // dropped on the impostor would have loaded the deleted 0.5 kg size-recognised plate.
    [Fact]
    public async Task ASizeImpostorNeverSwitchesAGate()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, 2f);
        var impostor = WorkshopInput.Generic(PartId.Pipe, new(4), -4, PlateY, 3, 0, 0, 0, 1,
            [new ExtractedCollider(ColliderShapeKind.Box, new(new((Half)0, (Half).03f, (Half)0), CanonicalRotation.Identity), (Half)0,
                new MetreVector((Half).9f, (Half).05f, (Half).9f))]);
        var reads = await RunBench(Bench([gate, battery, plate, impostor, BallOn(5, -4, 3)], Circuit(battery, plate, gate)));
        Assert.True(reads[^1].Bodies.TryGet(new(5), out var ball) && (ball.Body.Cell.Y + ball.Body.Local.Y) / 16f > Face - .01,
            "control: the ball rests on the impostor's face");
        AssertGateClosedThroughout(reads, plate, gate, "ball on the impostor");
    }

    // Matrix rows Removal and the rotated plate (CAT-052 P12): a Basketball dropped on a plate tilted 30° rests on the face, then
    // rolls off. The committed level follows that same tick's measurement, so the route opens on the next tick: closed on every
    // tick the ball clearly rests on the face, open on every tick it is clearly off it, and the gate finally closes.
    [Fact]
    public async Task ALoadRolledOffATiltedPlateOpensItsContactOnTheNextTick()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0);
        var half = Math.PI / 12; // 30° about z
        const double plateHeight = 2;
        var plate = WorkshopInput.PressurePlate(new(3), -4, plateHeight, 0, 0, 0, Math.Sin(half), Math.Cos(half), .5f);
        var rotation = new System.Numerics.Quaternion(0, 0, (float)Math.Sin(half), (float)Math.Cos(half));
        var drop = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(-.2f, .08f + .345f, 0), rotation);
        var ball = WorkshopInput.Basketball(new(4), -4 + drop.X, plateHeight + drop.Y, drop.Z, 0, 0, 0, 1);
        var reads = await RunBench(Bench([gate, battery, plate, ball], Circuit(battery, plate, gate)), 4 * 120);
        int resting = 0, off = 0;
        var touchedBefore = false; // a landing tick's substeps measure the approach before the solve closes the gap
        foreach (var read in reads)
        {
            Assert.True(read.Bodies.TryGet(ball.Id, out var body));
            var world = new System.Numerics.Vector3((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
                (body.Body.Cell.Z + body.Body.Local.Z) / 16f) - new System.Numerics.Vector3(-4, (float)plateHeight, 0);
            var local = System.Numerics.Vector3.Transform(world, System.Numerics.Quaternion.Conjugate(rotation));
            var clearance = local.Y - .34f - .08f;
            var touching = MathF.Abs(clearance) < .002f && MathF.Abs(local.X) < .8f && MathF.Abs(local.Z) < .8f;
            if (touching && touchedBefore)
            {
                resting++;
                Assert.True(ContactClosed(read, plate), $"resting on the face at tick {read.Tick.Value} (local {local})");
            }
            else if (MathF.Abs(local.X) > .9f + .34f || clearance > .05f)
            {
                off++;
                Assert.False(ContactClosed(read, plate), $"off the face at tick {read.Tick.Value} (local {local})");
            }
            touchedBefore = touching;
        }
        Assert.True(resting > 5 && off > 5, $"the ball rested ({resting} ticks) and then left ({off} ticks)");
        Assert.True(Travel(reads[^1], gate) < ClosedTravel, $"the gate closes after removal, travel {Travel(reads[^1], gate)}");
    }

    // D3 and the removal timing: the plate's level commits from the same tick's phase (tick N); the next Advance composes that gate's
    // route from it (tick N+1). An unloaded tick clears the level at once (Story 9R.5 later adds its release hold).
    [Fact]
    public void TheContactLoadLevelFollowsEachTicksPhaseAndItsRouteReadsItNextTick()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, .5f);
        var construction = Bench([gate, battery, plate], Circuit(battery, plate, gate));
        var network = WorkshopActivationCompiler.Compile(construction);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        Assert.Equal(ActivationNodeKind.ContactLoadSource, Assert.Single(network.Nodes.ToArray(), node => node.Owner == plate.Id).Kind);
        var row = Assert.Single(scene.ContactLoadSensors.ToArray());
        var route = Assert.Single(scene.SupplyRoutes.ToArray());
        var state = new ActivationCheckpoint(network.Clear(), network.ClearTimers(), network.ClearMemory());
        ActivationCheckpoint Step(ulong tick, ContactLoadPhase phase, float mass) => network.Consume(state.Activations, state.Timers, [],
            new(tick), [], memory: state.Memory, contactLoads: [new(row.Id, row.Frame, row.Face, mass, phase)]);
        bool Next(ActivationCheckpoint committed) => PhysicsTickInputs.Compose(scene.SupplyRoutes, scene.Prismatics.Length,
            new([ElectricalEnable.Enabled]), owner => network.ContactClosed(committed.Activations, owner)).SupplyClosed[route.Prismatic];
        ActivationLatch Level(ActivationCheckpoint committed)
        {
            for (var i = 0; i < committed.Activations.Count; i++) if (committed.Activations[i].Owner == plate.Id) return committed.Activations[i];
            throw new InvalidOperationException("no plate level");
        }

        state = Step(1, ContactLoadPhase.Underweight, .4f);
        Assert.False(network.ContactClosed(state.Activations, plate.Id)); Assert.False(Next(state));
        state = Step(2, ContactLoadPhase.Loaded, 1);
        Assert.True(network.ContactClosed(state.Activations, plate.Id)); Assert.True(Next(state));
        var onset = Level(state);
        Assert.Equal((ActivationOccurrenceKind.ContactLoad, row.Face, plate.Id), (onset.Kind, onset.Collider, onset.Body));
        state = Step(3, ContactLoadPhase.Loaded, 1);
        Assert.Equal(onset, Level(state)); // the level holds its onset while Loaded
        state = Step(4, ContactLoadPhase.Empty, 0);
        Assert.False(network.ContactClosed(state.Activations, plate.Id)); Assert.False(Next(state));
        // A load present for one tick only: closed on that tick, open again on the very next one.
        state = Step(5, ContactLoadPhase.Loaded, 1);
        Assert.True(Next(state));
        state = Step(6, ContactLoadPhase.Empty, 0);
        Assert.False(network.ContactClosed(state.Activations, plate.Id)); Assert.False(Next(state));
        // Every row must be read each tick: a missing read would silently freeze the gate.
        Assert.Throws<ArgumentException>(() => network.Consume(state.Activations, state.Timers, [], new(5), [], memory: state.Memory));
    }

    // Matrix controls: a loaded plate with no battery supplies no route (gate closed, yet the plate still senses), and a plate
    // with no gate still compiles its row and commits its level.
    [Fact]
    public async Task ALoadedPlateWithoutSupplyLeavesItsGateClosed()
    {
        var gate = Gate(1, 0); var plate = Plate(3, -4, 0, .5f);
        var reads = await RunBench(Bench([gate, plate, BallOn(4, -4, 0)], Supply(plate.Id, gate.Id)));
        Assert.True(ContactClosed(reads[^1], plate), "the unsupplied plate still senses its load");
        Assert.All(reads, read => Assert.True(Travel(read, gate) < ClosedTravel, $"travel {Travel(read, gate)} at tick {read.Tick.Value}"));
    }

    [Fact]
    public async Task APlateWithNoGateStillCommitsItsLevel()
    {
        var plate = Plate(3, -4, 0, .5f);
        var reads = await RunBench(Bench([plate, BallOn(4, -4, 0)]), 120);
        Assert.False(ContactClosed(reads[0], plate), "Empty at admission");
        Assert.True(ContactClosed(reads[^1], plate), "Loaded once the ball rests");
    }
}
