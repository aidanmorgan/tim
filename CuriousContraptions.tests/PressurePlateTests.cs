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
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.PressurePlate));
        var def = registry.Definitions[PartId.PressurePlate];
        Assert.Equal(PartId.PressurePlate, def.PartId);
        Assert.Equal("pressure_plate", def.Id);
        Assert.Equal("Pressure plate", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Control, def.Category);
        Assert.NotNull(def.Scene);

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
    public void PressurePlateScene_ExtractsExactTwoCollidersAndThreePortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/PressurePlate/pressure_plate.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

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

            Assert.Equal(3, ports.Count);
            Assert.Equal(new Godot.Vector3(-1.18f, -0.08f, 0), ports["PowerIn"].Position);
            Assert.Equal(new Godot.Vector3(1.18f, -0.08f, 0), ports["Supply"].Position);
            // Story 9R.5 D3: the level ActivationOut sits on the +Z edge, level with the ±X supply sockets.
            Assert.Equal(new Godot.Vector3(0, -0.08f, 1.18f), ports["ActivationOut"].Position);
            var socket = (MeshInstance3D)instance.FindChild("Visual", recursive: false)!.FindChild("Visual_Socket_ActivationOut", recursive: false)!;
            Assert.Equal(new Godot.Vector3(0, -0.08f, 1.18f), socket.Position);
            var sphere = Assert.IsType<SphereMesh>(socket.Mesh);
            Assert.Equal(.075f, sphere.Radius);
            Assert.Equal(new Color("#e8b764").ToHtml(false), ((StandardMaterial3D)sphere.Material).AlbedoColor.ToHtml(false));
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void CanonicalColliders_MatchSceneExtractedColliders()
    {
        var canonical = DeclaredPhysics.Colliders(PartId.PressurePlate);
        Assert.Equal(2, canonical.Count);
        Assert.Equal(canonical, DeclaredPhysics.Colliders(PartId.PressurePlate));
    }

    [Fact]
    public void Ports_DeclarePowerInALevelActivationOutAndSupply()
    {
        // Story 9R.5 D4: ActivationOut (a level) is listed before Supply, as on the latch, so the drawer keeps the Supply row.
        Assert.Equal(
            [new WorkshopPort(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input, null, new(-1.18f, -.08f, 0f)),
             new WorkshopPort(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output, WorkshopSignalKind.Level, new(0f, -.08f, 1.18f)),
             new WorkshopPort(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output, null, new(1.18f, -.08f, 0f))],
            DeclaredPorts.Current.Of(PartId.PressurePlate).ToArray());
        Assert.Equal(new Vector3(-1.18f, -0.08f, 0f), DeclaredPorts.Current.LocalPosition(PartId.PressurePlate, WorkshopSocket.PowerIn));
        Assert.Equal(new Vector3(1.18f, -0.08f, 0f), DeclaredPorts.Current.LocalPosition(PartId.PressurePlate, WorkshopSocket.Supply));
        Assert.Equal(new Vector3(0f, -0.08f, 1.18f), DeclaredPorts.Current.LocalPosition(PartId.PressurePlate, WorkshopSocket.ActivationOut));
    }

    [Fact]
    public void Connections_AcceptsElectricalAndRejectsActivation()
    {
        var plate = WorkshopInput.PressurePlate(new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
        var battery = WorkshopInput.Battery(new(2), 2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var switchPart = WorkshopInput.Generic(PartId.ImpactSwitch, new(3), -2, 0, 0, 0, 0, 0, 1);

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
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(3), 2, 0, 0, 0, 0, 0, 1);
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
        Assert.Contains(scene.SupplyGraph.Nodes.ToArray(),node=>node.Owner==plate.Id && node.Kind==SupplyNodeKind.Contact);
        Assert.Equal(battery.Id,scene.Electrical.Sources[0].Owner);
        Assert.Equal(gate.Id,scene.Prismatics[0].BodyA);
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
        // Story 9R.5b: the contact-load table is exactly its population, directly before the colliders.
        var layout = PhysicsGpuAbi.Layout(world);
        Assert.Equal(1, layout.ContactLoads);
        Assert.Equal(layout.CollidersOffset, layout.ContactLoadsOffset + PhysicsGpuAbi.ContactLoadSensorBytes);
        // A committed phase that disagrees with its f32 mass is rejected.
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(world.AsSpan(layout.ContactLoadOffset(0) + PhysicsGpuAbi.ContactLoadPhaseOffset),
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
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decodedCmd = WorkshopWireFlatBuffers.DecodeCommand(bytes);
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
        var plate = WorkshopInput.PressurePlate(new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
        IWorkshopInstance[] instances = [plate];
        WorkbenchCapacity.Validate(instances);

        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(plate)), new(1, 2)).World;
        Assert.Equal(1 + 1, world.Bodies);
        Assert.Equal(0, world.Dynamic);
        Assert.Equal(2 + 1, world.Colliders);
        Assert.Equal(0, world.Triggers);
        Assert.Equal(0, world.ContactWorks);
        Assert.Equal(0, world.ElectricalSources);
        Assert.Equal(0, world.Prismatics);
        Assert.Equal(1, world.ContactLoads);
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
        WorkshopInput.Generic(PartId.PoweredGate, new(id), 0, GateHeight, z, 0, 0, 0, 1);
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
            var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(7)))).Construction;
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
        // Story 9R.7: the pipe is a typed instance, so another generic declared-collider part carries the plate-sized boxes.
        var impostor = WorkshopInput.Generic(PartId.PipeBend45, new(4), -4, PlateY, 3, 0, 0, 0, 1);
        var reads = await RunBench(Bench([gate, battery, plate, impostor, BallOn(5, -4, 3)], Circuit(battery, plate, gate)));
        Assert.True(reads[^1].Bodies.TryGet(new(5), out var ball) && (ball.Body.Cell.Y + ball.Body.Local.Y) / 16f > Face - .01,
            "control: the ball rests on the impostor's face");
        AssertGateClosedThroughout(reads, plate, gate, "ball on the impostor");
    }

    // Matrix rows Removal and the rotated plate (CAT-052 P12): a Basketball dropped on a plate tilted 30° rests on the face, then
    // rolls off. Story 9R.5: the committed level is high on every qualifying tick and is held for 11 more; it is low from the 12th
    // tick after the last qualifying one. A Signal lamp on the plate's ActivationOut follows that same level on every tick.
    [Fact]
    public async Task ALoadRolledOffATiltedPlateReleasesItsLevelTwelveTicksAfterItsLastQualifyingTick()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0);
        var half = Math.PI / 12; // 30° about z
        const double plateHeight = 2;
        var plate = WorkshopInput.PressurePlate(new(3), -4, plateHeight, 0, 0, 0, Math.Sin(half), Math.Cos(half), .5f);
        var rotation = new System.Numerics.Quaternion(0, 0, (float)Math.Sin(half), (float)Math.Cos(half));
        var drop = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(-.2f, .08f + .345f, 0), rotation);
        var ball = WorkshopInput.Basketball(new(4), -4 + drop.X, plateHeight + drop.Y, drop.Z, 0, 0, 0, 1);
        var lamp = LampAt(5);
        var reads = await RunBench(Bench([gate, battery, plate, ball, lamp], [.. Circuit(battery, plate, gate), Signal(plate.Id, lamp.Id)]), 4 * 120);
        int resting = 0, off = 0, held = 0, released = 0; ulong? lastQualifying = null;
        var touchedBefore = false; // a landing tick's substeps measure the approach before the solve closes the gap
        foreach (var read in reads)
        {
            var qualifying = Qualifying(read, plate);
            if (qualifying) lastQualifying = read.Tick.Value;
            Assert.Equal(ContactClosed(read, plate), Latched(read.Activations, lamp.Id));
            if (lastQualifying is { } last)
            {
                var n = read.Tick.Value - last;
                Assert.True(ContactClosed(read, plate) == n <= 11, $"tick {read.Tick.Value}: n = {n}, level {ContactClosed(read, plate)}");
                if (n is > 0 and <= 11) held++; else if (n >= 12) released++;
            }
            else Assert.False(ContactClosed(read, plate), $"no qualifying tick yet at {read.Tick.Value}");
            Assert.True(read.Bodies.TryGet(ball.Id, out var body));
            var world = new System.Numerics.Vector3((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
                (body.Body.Cell.Z + body.Body.Local.Z) / 16f) - new System.Numerics.Vector3(-4, (float)plateHeight, 0);
            var local = System.Numerics.Vector3.Transform(world, System.Numerics.Quaternion.Conjugate(rotation));
            var clearance = local.Y - .34f - .08f;
            var touching = MathF.Abs(clearance) < .002f && MathF.Abs(local.X) < .8f && MathF.Abs(local.Z) < .8f;
            if (touching && touchedBefore)
            {
                resting++;
                Assert.True(qualifying, $"resting on the face qualifies at tick {read.Tick.Value} (local {local})");
            }
            else if (MathF.Abs(local.X) > .9f + .34f || clearance > .05f)
            {
                off++;
                Assert.False(qualifying, $"off the face does not qualify at tick {read.Tick.Value} (local {local})");
            }
            touchedBefore = touching;
        }
        Assert.True(resting > 5 && off > 5, $"the ball rested ({resting} ticks) and then left ({off} ticks)");
        Assert.True(held >= 11 && released > 12, $"the level was held ({held} ticks) and then released ({released} ticks)");
        Assert.True(Travel(reads[^1], gate) < ClosedTravel, $"the gate closes after removal, travel {Travel(reads[^1], gate)}");
    }

    // D3 and the removal timing: the plate's level commits from the same tick's phase (tick N); the next Advance composes that gate's
    // route from it (tick N+1). Story 9R.5: release waits for the committed 12-tick hold.
    [Fact]
    public void TheContactLoadLevelFollowsEachTicksPhaseAndItsRouteReadsItNextTick()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, .5f);
        var construction = Bench([gate, battery, plate], Circuit(battery, plate, gate));
        var network = WorkshopActivationCompiler.Compile(construction);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        Assert.Equal(ActivationNodeKind.ContactLoadSource, Assert.Single(network.Nodes.ToArray(), node => node.Owner == plate.Id).Kind);
        var row = Assert.Single(scene.ContactLoadSensors.ToArray());
        var prismatic=Array.FindIndex(scene.Prismatics.ToArray(),joint=>joint.BodyA==gate.Id);
        var state = new ActivationCheckpoint(network.Clear(), network.ClearTimers(), network.ClearMemory());
        ActivationCheckpoint Step(ulong tick, ContactLoadPhase phase, float mass) => network.Consume(state.Activations, state.Timers, [],
            new(tick), [], memory: state.Memory, contactLoads: [new(row.Id, row.Frame, row.Face, mass, phase)]);
        bool Next(ActivationCheckpoint committed) => PhysicsTickInputs.Compose(scene.SupplyGraph, scene.Prismatics.Length,
            new([ElectricalEnable.Enabled]), owner => network.ContactClosed(committed.Activations, network.ClearTimers(), owner)).SupplyClosed[prismatic];
        ActivationLatch Level(ActivationCheckpoint committed)
        {
            for (var i = 0; i < committed.Activations.Count; i++) if (committed.Activations[i].Owner == plate.Id) return committed.Activations[i];
            throw new InvalidOperationException("no plate level");
        }

        state = Step(1, ContactLoadPhase.Underweight, .4f);
        Assert.False(network.ContactClosed(state.Activations, network.ClearTimers(), plate.Id)); Assert.False(Next(state));
        state = Step(2, ContactLoadPhase.Loaded, 1);
        Assert.True(network.ContactClosed(state.Activations, network.ClearTimers(), plate.Id)); Assert.True(Next(state));
        var onset = Level(state);
        Assert.Equal((ActivationOccurrenceKind.ContactLoad, row.Face, plate.Id), (onset.Kind, onset.Collider, onset.Body));
        state = Step(3, ContactLoadPhase.Loaded, 1);
        Assert.Equal(onset, Level(state)); // the level holds its onset while Loaded
        // Story 9R.5: after the last qualifying tick (3) the level is held through n = 11 (tick 14) and released at n = 12 (tick 15).
        for (ulong tick = 4; tick <= 14; tick++)
        {
            state = Step(tick, ContactLoadPhase.Empty, 0);
            Assert.True(network.ContactClosed(state.Activations, network.ClearTimers(), plate.Id), $"held at tick {tick}"); Assert.True(Next(state));
            Assert.Equal(onset, Level(state)); // the held level keeps its onset
        }
        state = Step(15, ContactLoadPhase.Empty, 0);
        Assert.False(network.ContactClosed(state.Activations, network.ClearTimers(), plate.Id)); Assert.False(Next(state));
        // A load present for one tick only is still held for the release hold; an Underweight tick does not qualify.
        state = Step(16, ContactLoadPhase.Loaded, 1);
        Assert.True(Next(state));
        state = Step(17, ContactLoadPhase.Underweight, .4f);
        Assert.True(network.ContactClosed(state.Activations, network.ClearTimers(), plate.Id)); Assert.True(Next(state));
        // Every row must be read each tick: a missing read would silently freeze the gate.
        Assert.Throws<ArgumentException>(() => network.Consume(state.Activations, state.Timers, [], new(18), [], memory: state.Memory));
    }

    // Owner decision, 20 Oct 2026: no activation-slot limit. A bench of 28 activation parts (13 plates, 5 counters, 5 clocks,
    // 5 latches; Build used to refuse the 9th) plus one Battery → last plate → gate circuit, the most the fixed physics tables
    // hold (33 bodies, 31 instances), compiles, runs, Resets exactly and survives Save/Load. Non-vacuous: the last plate's own gate
    // opens under its own load while every other plate stays Clear.
    private static WorkshopConstruction ActivationBench()
    {
        var instances = new List<IWorkshopInstance>();
        ulong id = 0;
        for (var i = 0; i < 13; i++) instances.Add(Plate(++id, -16 + 3 * (i % 7), -6 + 3 * (i / 7), .5f));
        for (var i = 0; i < 5; i++) instances.Add(WorkshopInput.Counter(new(++id), -16 + 3 * i, 3, 6, 0, 0, 0, 1, 3));
        for (var i = 0; i < 5; i++) instances.Add(WorkshopInput.Clock(new(++id), -16 + 3 * i, 3, 9, 0, 0, 0, 1, 1f));
        for (var i = 0; i < 5; i++) instances.Add(WorkshopInput.Latch(new(++id), -16 + 3 * i, 3, 12, 0, 0, 0, 1));
        var last = (WorkshopPressurePlate)instances[12];
        var gate = Gate(++id, -6); var battery = Battery(++id, -6);
        var ball = WorkshopInput.Basketball(new(++id), (last.Cell.X + (double)last.Local.X) / 16, Face + .35,
            (last.Cell.Z + (double)last.Local.Z) / 16, 0, 0, 0, 1);
        instances.AddRange([gate, battery, ball]);
        return Bench([.. instances], Circuit(battery, last, gate));
    }

    internal static async Task<(WorkshopRead Built, List<WorkshopRead> Run, WorkshopRead Reset)> RunAndReset(WorkshopConstruction construction, int ticks)
    {
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        var built = simulation.Committed;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        var reads = new List<WorkshopRead>(ticks);
        for (var tick = 0; tick < ticks; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            reads.Add(simulation.Committed);
        }
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        return (built, reads, simulation.Committed);
    }

    [Fact]
    public async Task ABenchOfManyActivationPartsRunsResetsAndSavesWithNoActivationLimit()
    {
        var bench = ActivationBench();
        bench.Validate();
        var nodes = WorkshopActivationCompiler.Compile(bench).Nodes.ToArray();
        Assert.Equal(28, nodes.Length);
        Assert.Equal(13, nodes.Count(node => node.Kind == ActivationNodeKind.ContactLoadSource));
        Assert.Equal(13, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).ContactLoadSensors.Length);
        var plates = bench.Instances.OfType<WorkshopPressurePlate>().ToArray();
        var gate = bench.Instances.OfType<WorkshopGenericInstance>().Single();

        var (built, reads, reset) = await RunAndReset(bench, Ticks);
        AssertGateOpened(reads, plates[^1], gate, "the 13th plate under its own load");
        foreach (var plate in plates[..^1]) Assert.All(reads, read => Assert.False(ContactClosed(read, plate), $"plate {plate.Id.Value} stays Clear"));
        Assert.Equal(28, reads[^1].Activations.Count);
        // 10 Counter and Clock rows plus each plate's release-hold row (Story 9R.5 D6).
        Assert.True(reads[^1].Memory.Count == 10 + 13, $"counter, clock and plate memory rows: {reads[^1].Memory.Count}");
        // Reset restores the Build-mode committed state exactly (every field the 9R.1 snapshot compares; the world epoch is
        // a new identity by design).
        AssertSameState(built, reset, "Reset");

        // Save/Load through the real codec: the reloaded bench is identical and replays the same committed run.
        var saved = new WorkshopSavedConstruction(bench, new((ulong)bench.Instances.Count + 1));
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, reloaded);
        var (_, replay, _) = await RunAndReset(reloaded.Construction, Ticks);
        AssertSameState(reads[^1], replay[^1], "the reloaded bench's run");
    }

    /// <summary>Bit-equal committed state, the world epoch aside: tick, bodies, activations, timers, memory and electrical.</summary>
    private static void AssertSameState(WorkshopRead expected, WorkshopRead actual, string label)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Bodies.Count, actual.Bodies.Count);
        for (var i = 0; i < expected.Bodies.Count; i++)
        {
            var (e, a) = (expected.Bodies[i], actual.Bodies[i]);
            Assert.True(e.Body.Id == a.Body.Id && e.Body.Tick == a.Body.Tick && e.Body.Cell == a.Body.Cell &&
                F32Bits.Equal(e.Body.Local, a.Body.Local) && e.Body.Velocity.HasSameBits(a.Body.Velocity) &&
                e.Rotation == a.Rotation && e.AngularVelocity.HasSameBits(a.AngularVelocity), $"{label}: body {e.Body.Id.Value} differs");
        }
        Assert.Equal(expected.Activations.Count, actual.Activations.Count);
        for (var i = 0; i < expected.Activations.Count; i++) Assert.Equal(expected.Activations[i], actual.Activations[i]);
        Assert.Equal(expected.Timers.Count, actual.Timers.Count);
        for (var i = 0; i < expected.Timers.Count; i++) Assert.Equal(expected.Timers[i], actual.Timers[i]);
        Assert.True(expected.Memory.HasSameBits(actual.Memory), $"{label}: memory rows differ");
        Assert.True(expected.Electrical.HasSameBits(actual.Electrical), $"{label}: electrical state differs");
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

    // ---- Story 9R.5: the plate's level ActivationOut and its committed 0.1 s (12-tick) release hold ----
    private const int HoldTicks = 12; // ticks(0.1 s, 120 Hz); stated by the spec, not read back from the declaration under test
    private const string LevelRefusal = "A level output can only light a Signal lamp.";
    private static WorkshopLamp LampAt(ulong id, double z = 0) => WorkshopInput.Lamp(new(id), 6, GateHeight, z, 0, 0, 0, 1);
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input = WorkshopSocket.ActivationIn) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);
    private static bool Latched(PhysicsActivationRead values, GpuBodyId owner)
    {
        for (var i = 0; i < values.Count; i++) if (values[i].Owner == owner) return values[i].Phase == ActivationPhase.Latched;
        throw new InvalidOperationException($"No activation node for {owner.Value}.");
    }
    private static ActivationMemory Hold(PhysicsMemoryRead memory, WorkshopPressurePlate plate)
    {
        for (var i = 0; i < memory.Count; i++) if (memory[i].Node == new ActivationNodeId(plate.Id.Value)) return memory[i];
        throw new InvalidOperationException($"Plate {plate.Id.Value} commits no release hold.");
    }
    /// <summary>A qualifying (Loaded) tick commits the level high with its release due exactly 12 ticks later.</summary>
    private static bool Qualifying(WorkshopRead read, WorkshopPressurePlate plate) =>
        ContactClosed(read, plate) && Hold(read.Memory, plate).DueTick == read.Tick.Value + HoldTicks;

    [Fact]
    public void APlateActivationOutLightsOnlyASignalLamp()
    {
        var plate = Plate(1, -4, 0, .5f); var lamp = LampAt(2);
        var counter = WorkshopInput.Counter(new(3), 0, GateHeight, -3, 0, 0, 0, 1, 3);
        var delay = WorkshopInput.Generic(PartId.Delay, new(4), 0, GateHeight, 3, 0, 0, 0, 1);
        var latch = WorkshopInput.Latch(new(5), 3, GateHeight, -3, 0, 0, 0, 1);
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(6), 3, GateHeight, 3, 0, 0, 0, 1);
        var instances = new WorkshopInstances(plate, lamp, counter, delay, latch, hit);
        new WorkshopConnections(Signal(plate.Id, lamp.Id)).Validate(instances);
        foreach (var (target, input) in new[] { (counter.Id, WorkshopSocket.ActivationIn), (delay.Id, WorkshopSocket.ActivationIn),
                     (latch.Id, WorkshopSocket.SetIn), (latch.Id, WorkshopSocket.ResetIn) })
            Assert.Equal(LevelRefusal, EditRefusal.Is(WorkshopEditRejection.LevelDrivesLampOnly,
                () => new WorkshopConnections(Signal(plate.Id, target, input)).Validate(instances)).Message);
        // A lamp that follows the plate's level takes no other signal; a pulse source alone still drives a (sticky) lamp.
        Assert.Equal("A lamp that follows a level output takes no other signal.", EditRefusal.Is(WorkshopEditRejection.LampFollowsOneLevel,
            () => new WorkshopConnections(Signal(plate.Id, lamp.Id), Signal(hit.Id, lamp.Id)).Validate(instances)).Message);
        new WorkshopConnections(Signal(hit.Id, lamp.Id)).Validate(instances);
    }

    // Every activation port declares its signal kind (an output what it emits, an input what it admits); no electrical port has one.
    // Exactly the latch and the plate emit a level, and only the Signal lamp admits one.
    [Fact]
    public void EveryActivationPortDeclaresItsSignalKind()
    {
        var levelOutputs = new List<PartId>(); var levelInputs = new List<PartId>();
        foreach (var id in new[] { PartId.ImpactSwitch, PartId.SignalLamp, PartId.Delay, PartId.Domino, PartId.Battery, PartId.PinballBumper,
                     PartId.BallDetector, PartId.PoweredGate, PartId.PressurePlate, PartId.Counter, PartId.Clock, PartId.Latch })
            foreach (var port in DeclaredPorts.Current.Of(id))
            {
                Assert.Equal(port.Domain == WorkshopConnectionDomain.Activation, port.Signal is { } kind && Enum.IsDefined(kind));
                if (port.Signal != WorkshopSignalKind.Level) continue;
                (port.Direction == WorkshopPortDirection.Output ? levelOutputs : levelInputs).Add(id);
            }
        Assert.Equal([PartId.PressurePlate, PartId.Latch], levelOutputs);
        Assert.Equal([PartId.SignalLamp], levelInputs);
    }

    [Fact]
    public void ThePlateCompilesItsLampAsALevelIndicatorWithATwelveTickReleaseHold()
    {
        var plate = Plate(1, -4, 0, .5f); var lamp = LampAt(2); var sticky = LampAt(3, 3);
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(4), 3, GateHeight, 3, 0, 0, 0, 1);
        var nodes = WorkshopActivationCompiler.Compile(Bench([plate, lamp, sticky, hit], Signal(plate.Id, lamp.Id), Signal(hit.Id, sticky.Id)))
            .Nodes.ToArray();
        Assert.Equal(ActivationNodeKind.LevelIndicator, nodes.Single(node => node.Owner == lamp.Id).Kind);
        Assert.Equal(ActivationNodeKind.StickyFirstInput, nodes.Single(node => node.Owner == sticky.Id).Kind);
        var source = nodes.Single(node => node.Owner == plate.Id);
        Assert.Equal((ActivationNodeKind.ContactLoadSource, 12u), (source.Kind, source.ReleaseHoldTicks));
        Assert.All(nodes.Where(node => node.Owner != plate.Id), node => Assert.Equal(0u, node.ReleaseHoldTicks));
        Assert.Equal((6u, 12u, 24u), (WorkshopPressurePlate.ReleaseHoldTicks(SimulationCadence.Hz60),
            WorkshopPressurePlate.ReleaseHoldTicks(SimulationCadence.Hz120), WorkshopPressurePlate.ReleaseHoldTicks(SimulationCadence.Hz240)));
    }

    [Fact]
    public void TheNetworkAdmitsAContactLoadLevelOnlyIntoOneLevelIndicator()
    {
        var level = new ActivationNodeDeclaration(new(1), new(1), ActivationNodeKind.ContactLoadSource, default, ContactLoad: new(9), ReleaseHoldTicks: 12);
        var lamp = new ActivationNodeDeclaration(new(2), new(2), ActivationNodeKind.LevelIndicator, default);
        var timer = new ActivationNodeDeclaration(new(3), new(3), ActivationNodeKind.Timer, default, 5);
        var hit = new ActivationNodeDeclaration(new(4), new(4), ActivationNodeKind.ContactSource, new(8));
        _ = new ActivationNetwork([level, lamp], [new(level.Id, lamp.Id, WorkshopSocket.ActivationIn)]);
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([level, timer], [new(level.Id, timer.Id, WorkshopSocket.ActivationIn)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([level, lamp, hit],
            [new(level.Id, lamp.Id, WorkshopSocket.ActivationIn), new(hit.Id, lamp.Id, WorkshopSocket.ActivationIn)]));
        // The hold is declared exactly on a contact-load level.
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([level with { ReleaseHoldTicks = 0 }], []));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([timer with { ReleaseHoldTicks = 12 }], []));
    }

    /// <summary>Drives the real compiled network (battery → plate → gate, plate ActivationOut → lamp) one committed tick per entry
    /// with that tick's contact-load phase, and records the plate level, the lamp, and the route the next Advance composes.</summary>
    private static List<(bool Plate, bool Lamp, bool NextRoute)> HoldTrace(params bool[] loaded)
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0); var plate = Plate(3, -4, 0, .5f); var lamp = LampAt(4);
        var construction = Bench([gate, battery, plate, lamp], [.. Circuit(battery, plate, gate), Signal(plate.Id, lamp.Id)]);
        var network = WorkshopActivationCompiler.Compile(construction);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var row = Assert.Single(scene.ContactLoadSensors.ToArray());
        var prismatic=Array.FindIndex(scene.Prismatics.ToArray(),joint=>joint.BodyA==gate.Id);
        var state = new ActivationCheckpoint(network.Clear(), network.ClearTimers(), network.ClearMemory());
        var trace = new List<(bool, bool, bool)>();
        for (var i = 0; i < loaded.Length; i++)
        {
            state = network.Consume(state.Activations, state.Timers, [], new((ulong)i + 1), [], memory: state.Memory,
                contactLoads: [new(row.Id, row.Frame, row.Face, loaded[i] ? 1f : 0f, loaded[i] ? ContactLoadPhase.Loaded : ContactLoadPhase.Empty)]);
            var committed = state;
            var next = PhysicsTickInputs.Compose(scene.SupplyGraph, scene.Prismatics.Length, new([ElectricalEnable.Enabled]),
                owner => network.ContactClosed(committed.Activations, network.ClearTimers(), owner)).SupplyClosed[prismatic];
            trace.Add((network.ContactClosed(state.Activations, network.ClearTimers(), plate.Id), Latched(state.Activations, lamp.Id), next));
        }
        return trace;
    }
    private static bool[] Phases(params (bool Loaded, int Count)[] runs) => [.. runs.SelectMany(run => Enumerable.Repeat(run.Loaded, run.Count))];

    // Matrix row Rest (no dwell before activation): the plate, its lamp and the next route are high on the first Loaded tick.
    [Fact]
    public void TheLevelIsHighOnTheFirstQualifyingTick()
    {
        var trace = HoldTrace(Phases((false, 2), (true, 1)));
        Assert.Equal([(false, false, false), (false, false, false), (true, true, true)], trace);
    }

    // Matrix row Hold boundary: with n ticks since the last qualifying tick, lit while n ≤ 11, dark from n = 12.
    [Theory]
    [InlineData(1, true)]
    [InlineData(11, true)]
    [InlineData(12, false)]
    [InlineData(13, false)]
    public void TheLevelIsHeldThroughElevenTicksAndDarkFromTwelve(int n, bool lit) =>
        Assert.Equal((lit, lit, lit), HoldTrace(Phases((true, 1), (false, n)))[^1]);

    // Matrix rows Leave and Both domains: on every tick the plate level, its lamp and the gate route composed for the next Advance
    // are one committed value.
    [Fact]
    public void TheLampAndTheSuppliedContactShareOneHeldLevelOnEveryTick()
    {
        var trace = HoldTrace(Phases((true, 3), (false, 14)));
        Assert.Equal([.. Enumerable.Repeat(true, 3 + 11), .. Enumerable.Repeat(false, 3)], trace.Select(tick => tick.Plate).ToArray());
        Assert.All(trace, tick => Assert.True(tick.Plate == tick.Lamp && tick.Plate == tick.NextRoute, $"{tick}"));
    }

    // Matrix row Bounce: re-contact within 11 non-qualifying ticks keeps the level steadily high; a gap of 12 shows one dark tick.
    [Theory]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    public void ABounceGapOfElevenStaysLitAndTwelveShowsOneDarkTick(int gap, int dark)
    {
        var trace = HoldTrace(Phases((true, 2), (false, gap), (true, 2), (false, gap), (true, 1)));
        Assert.Equal(2 * dark, trace.Count(tick => !tick.Plate));
        Assert.All(trace, tick => Assert.True(tick.Plate == tick.Lamp && tick.Plate == tick.NextRoute, $"{tick}"));
    }

    // The hold is committed checkpoint state: a second compiled network continuing from the committed checkpoint (as after a
    // superseded candidate, or a fresh worker) releases on the same tick; Clear() admits it released.
    [Fact]
    public void TheReleaseHoldIsCommittedCheckpointState()
    {
        var plate = Plate(3, -4, 0, .5f); var lamp = LampAt(4);
        var construction = Bench([plate, lamp], Signal(plate.Id, lamp.Id));
        var networks = new[] { WorkshopActivationCompiler.Compile(construction), WorkshopActivationCompiler.Compile(construction) };
        var row = Assert.Single(WorkshopPhysicsCompiler.Compile(construction, new(1, 2)).ContactLoadSensors.ToArray());
        var state = new ActivationCheckpoint(networks[0].Clear(), networks[0].ClearTimers(), networks[0].ClearMemory());
        Assert.Equal(0ul, Hold(state.Memory, plate).DueTick);
        var lit = new List<bool>();
        for (ulong tick = 1; tick <= 14; tick++)
        {
            var loaded = tick == 1;
            var network = networks[tick % 2];
            var candidate = network.Consume(state.Activations, state.Timers, [], new(tick), [], memory: state.Memory,
                contactLoads: [new(row.Id, row.Frame, row.Face, loaded ? 1f : 0f, loaded ? ContactLoadPhase.Loaded : ContactLoadPhase.Empty)]);
            // A superseded candidate (a Loaded read never committed) leaves the committed hold unchanged.
            _ = network.Consume(state.Activations, state.Timers, [], new(tick), [], memory: state.Memory,
                contactLoads: [new(row.Id, row.Frame, row.Face, 1f, ContactLoadPhase.Loaded)]);
            state = candidate;
            lit.Add(Latched(state.Activations, lamp.Id));
            Assert.Equal(lit[^1] ? 1ul + HoldTicks : 0ul, Hold(state.Memory, plate).DueTick);
        }
        Assert.Equal([.. Enumerable.Repeat(true, 12), false, false], lit);
    }

    // Matrix rows Rest and Underweight through the real compile and Advance path: a resting Basketball lights the lamp on the plate's
    // first Loaded tick; a 0.4 kg Domino leaves both dark.
    [Fact]
    public async Task ABasketballRestingOnThePlateLightsItsLampOnItsFirstLoadedTick()
    {
        var plate = Plate(3, -4, 0, .5f); var lamp = LampAt(5);
        var reads = await RunBench(Bench([plate, BallOn(4, -4, 0), lamp], Signal(plate.Id, lamp.Id)), 120);
        var first = reads.FindIndex(read => ContactClosed(read, plate));
        Assert.True(first > 0, "the plate loads during the run");
        Assert.True(Qualifying(reads[first], plate), "the first high tick is a qualifying tick (no dwell)");
        Assert.All(reads.Take(first), read => Assert.False(Latched(read.Activations, lamp.Id), $"dark before load at tick {read.Tick.Value}"));
        Assert.All(reads.Skip(first), read => Assert.True(Latched(read.Activations, lamp.Id), $"lit while the ball rests at tick {read.Tick.Value}"));
    }

    // Matrix row Bounce through the real path: a Basketball dropped 2 m onto the plate rebounds (a gap of non-qualifying ticks
    // between contacts, 7 in evidence-9r-5/probe-bounce-native.txt) yet the lamp stays lit from the first contact on.
    [Fact]
    public async Task ABasketballBouncingOnThePlateKeepsItsLampSteadilyLit()
    {
        var plate = Plate(3, -4, 0, .5f); var lamp = LampAt(5);
        var ball = WorkshopInput.Basketball(new(4), -4, Face + .34 + 2, 0, 0, 0, 0, 1);
        var reads = await RunBench(Bench([plate, ball, lamp], Signal(plate.Id, lamp.Id)), 240);
        var first = reads.FindIndex(read => Qualifying(read, plate));
        Assert.True(first > 0, "the ball lands on the plate");
        var landed = reads.Skip(first).ToList();
        Assert.Contains(landed, read => !Qualifying(read, plate)); // control: the ball left the face between contacts
        Assert.All(landed, read => Assert.True(Latched(read.Activations, lamp.Id), $"lit through the bounce at tick {read.Tick.Value}"));
    }

    [Fact]
    public async Task AnUnderweightDominoLeavesThePlateLampDark()
    {
        var plate = Plate(3, -4, 0, .5f); var lamp = LampAt(5);
        var reads = await RunBench(Bench([plate, StandingOn(4, -4, 0), lamp], Signal(plate.Id, lamp.Id)), 120);
        Assert.All(reads, read => Assert.False(ContactClosed(read, plate) || Latched(read.Activations, lamp.Id), $"dark at tick {read.Tick.Value}"));
    }
}
