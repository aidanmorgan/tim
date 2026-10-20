using System;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class PoweredGateTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversPoweredGateDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.True(registry.Definitions.ContainsKey(PartId.PoweredGate));
        var def = registry.Definitions[PartId.PoweredGate];
        Assert.Equal(PartId.PoweredGate, def.PartId);
        Assert.Equal("powered_gate", def.Id);
        Assert.Equal("Powered gate", def.Title);
        Assert.Equal("Motion", def.Category);
        Assert.Equal("Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes.", def.Description);
        Assert.NotNull(def.Scene);
        Assert.Empty(def.Parameters);

        var part = registry.Create(PartId.PoweredGate);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.PoweredGate, part.PartId);
            Assert.Equal(1.5f, part.PickRadius);

            var visual = part.FindChild("Visual", recursive: false) as Node3D;
            Assert.NotNull(visual);
            Assert.NotNull(visual.FindChild("Visual_Sleeve", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Collar_Start", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Collar_End", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Housing", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Rail_Start", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Rail_End", recursive: false));
            Assert.NotNull(visual.FindChild("Blade", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Blade", recursive: true));
            Assert.NotNull(visual.FindChild("Visual_Socket", recursive: false));
            Assert.NotNull(visual.FindChild("Visual_Indicator", recursive: false));
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void PoweredGateScene_ExtractsExact19CollidersAndThreePortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://parts/scenes/powered_gate.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(instance);

            // Exactly 19 colliders: 16 circumference + 1 housing + 2 rails
            Assert.Equal(19, colliders.Count);

            var circumference = colliders.Take(16).ToArray();
            var expectedCircHalfExtents = new MetreVector((Half)0.4f, (Half)0.025f, (Half)0.14f);
            foreach (var col in circumference)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
                Assert.Equal(expectedCircHalfExtents.X, col.HalfExtents.X);
                Assert.Equal(expectedCircHalfExtents.Y, col.HalfExtents.Y);
                Assert.Equal(expectedCircHalfExtents.Z, col.HalfExtents.Z);
            }

            var housing = colliders[16];
            Assert.Equal(ColliderShapeKind.Box, housing.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.4f, (Half)0.325f, (Half)0.9f), housing.HalfExtents);
            Assert.Equal((Half)0, housing.LocalPose.Translation.X);
            Assert.Equal((Half)1.65f, housing.LocalPose.Translation.Y);
            Assert.Equal((Half)0, housing.LocalPose.Translation.Z);

            var railStart = colliders[17];
            Assert.Equal(ColliderShapeKind.Box, railStart.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.12f, (Half)0.85f, (Half)0.06f), railStart.HalfExtents);
            Assert.Equal((Half)(-0.82f), railStart.LocalPose.Translation.Z);

            var railEnd = colliders[18];
            Assert.Equal(ColliderShapeKind.Box, railEnd.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.12f, (Half)0.85f, (Half)0.06f), railEnd.HalfExtents);
            Assert.Equal((Half)0.82f, railEnd.LocalPose.Translation.Z);

            Assert.Equal(3, ports.Count);
            Assert.True(ports.ContainsKey("power_in"));
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            Assert.Equal(new Vector3(0, 1.65f, 0.92f), ports["power_in"].Position);
            Assert.Equal(new Vector3(-0.49f, 0, 0), ports["mouth_start"].Position);
            Assert.Equal(new Vector3(0.49f, 0, 0), ports["mouth_end"].Position);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void CanonicalColliders_MatchSceneExtractedColliders()
    {
        var canonical = WorkshopCanonicalColliders.For(PartId.PoweredGate);
        Assert.Equal(19, canonical.Count);
        Assert.Same(canonical, WorkshopCanonicalColliders.PoweredGateColliders);
    }

    [Fact]
    public void Ports_DeclareSinglePowerInInput()
    {
        var ports = WorkshopPorts.For(PartId.PoweredGate);
        Assert.Equal(1, ports.Length);
        var port = ports[0];
        Assert.Equal(WorkshopSocket.PowerIn, port.Socket);
        Assert.Equal(WorkshopConnectionDomain.Electrical, port.Domain);
        Assert.Equal(WorkshopPortDirection.Input, port.Direction);

        var localPos = WorkshopPorts.LocalPosition(PartId.PoweredGate, WorkshopSocket.PowerIn);
        Assert.Equal(new System.Numerics.Vector3(0f, 1.65f, 0.92f), localPos);
    }

    [Fact]
    public void Connections_AcceptsElectricalAndRejectsActivation()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
        var battery = WorkshopInput.Battery(new(2), 2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var switchPart = WorkshopInput.Switch(new(3), -2, 0, 0, 0, 0, 0, 1, ContactTriggerSettings.Default);

        var instances = new WorkshopInstances(gate, battery, switchPart);

        // Valid electrical connection from battery Supply to gate PowerIn
        var validConn = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var validConnections = new WorkshopConnections(validConn);
        validConnections.Validate(instances);

        // Invalid: Activation connection to gate PowerIn must be rejected
        var invalidConn = new WorkshopConnection(switchPart.Id, WorkshopSocket.ActivationOut, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Activation);
        var invalidConnections = new WorkshopConnections(invalidConn);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => invalidConnections.Validate(instances));
    }

    [Fact]
    public void PhysicsCompiler_CompilesGateWithDynamicBladeAndPrismaticJoint()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
        var ball = WorkshopInput.Basketball(new(2), 0, 3, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([gate, ball]));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));

        // Bodies: plane + ball + gate frame + gate blade = 4 bodies
        Assert.Equal(4, scene.Bodies.Length);
        var gateFrame = scene.Bodies.ToArray().Single(b => b.Id == gate.Id);
        Assert.Equal(RigidMotionKind.Static, gateFrame.Motion);

        var bladeId = WorkshopPhysicsCompiler.GateBlade(gate.Id);
        var gateBlade = scene.Bodies.ToArray().Single(b => b.Id == bladeId);
        Assert.Equal(RigidMotionKind.Dynamic, gateBlade.Motion);
        Assert.Equal((Half)1f, gateBlade.Mass.Value);
        Assert.Equal((Half)0f, gateBlade.LinearDrag.Value); // Unwired -> 0 drag

        // Colliders: plane (1) + ball (1) + blade (1) + frame (19) = 22 colliders
        Assert.Equal(22, scene.Colliders.Length);
        var bladeCollider = scene.Colliders.ToArray().Single(c => c.Body == bladeId);
        Assert.Equal(ColliderShapeKind.Box, bladeCollider.Shape);
        Assert.Equal(new MetreVector((Half)0.06f, (Half)0.72f, (Half)0.72f), bladeCollider.HalfExtents);

        // Prismatic joint
        var joint = Assert.Single(scene.Prismatics.ToArray());
        Assert.Equal(gate.Id, joint.BodyA);
        Assert.Equal(bladeId, joint.BodyB);
        Assert.Equal(0f, joint.Lower);
        Assert.Equal(1.55f, joint.Upper);
        Assert.Equal(14f, joint.Stiffness);
        Assert.Equal(4f, joint.Damping);
        Assert.Equal(ConnectedCollision.Disabled, joint.Collision);

        // Electrical bindings: gate is not a contact work store, so bindings must be empty
        Assert.Empty(scene.Electrical.Bindings.ToArray());
    }

    private static WorkshopConnection Supply(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopGenericInstance Gate(ulong id, double z = 0) =>
        WorkshopInput.Generic(PartId.PoweredGate, new(id), 0, 0, z, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
    private static WorkshopBattery Battery(ulong id, ElectricalEnable enabled, double z = 0) =>
        WorkshopInput.Battery(new(id), 2, 0, z, 0, 0, 0, 1, ElectricalSourceSettings.Default with { Enabled = enabled });

    // Story 9R.3: a battery wired straight to the gate compiles one Direct route row whatever its authored enable; the blade
    // carries no mode-encoding drag and the gate (an actuator, not a contact work store) has no electrical binding.
    [Theory]
    [InlineData(ElectricalEnable.Disabled)]
    [InlineData(ElectricalEnable.Enabled)]
    public void PhysicsCompiler_DirectBatteryCompilesOneDirectRouteWhateverItsAuthoredEnable(ElectricalEnable authored)
    {
        var gate = Gate(1); var battery = Battery(2, authored);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([gate, battery]),
            Connections: new([Supply(battery.Id, gate.Id)]));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));

        var route = Assert.Single(scene.SupplyRoutes.ToArray());
        Assert.Equal((SupplyRouteKind.Direct, default(GpuBodyId)), (route.Kind, route.Switch));
        Assert.Equal(gate.Id, scene.Prismatics[route.Prismatic].BodyA);
        Assert.Equal(battery.Id, scene.Electrical.Sources[route.Source].Owner);
        Assert.Equal((Half)0f, scene.Bodies.ToArray().Single(b => b.Id == WorkshopPhysicsCompiler.GateBlade(gate.Id)).LinearDrag.Value);
        Assert.Empty(scene.Electrical.Bindings.ToArray());
    }

    // A direct route closes only while its own source's player control is Enabled, and never consults a switch contact.
    [Fact]
    public void Compose_ClosesADirectRouteOnlyWhileItsOwnSourceIsEnabled()
    {
        var gate = Gate(1); var battery = Battery(2, ElectricalEnable.Disabled);
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([gate, battery]),
            Connections: new([Supply(battery.Id, gate.Id)])), new(1, 1));
        var route = Assert.Single(scene.SupplyRoutes.ToArray());
        bool NoContact(GpuBodyId owner) => throw new InvalidOperationException($"A direct route asked for the contact of {owner.Value}.");
        Assert.True(PhysicsTickInputs.Compose(scene.SupplyRoutes, scene.Prismatics.Length, new([ElectricalEnable.Enabled]), NoContact)
            .SupplyClosed[route.Prismatic]);
        Assert.False(PhysicsTickInputs.Compose(scene.SupplyRoutes, scene.Prismatics.Length, new([ElectricalEnable.Disabled]), NoContact)
            .SupplyClosed[route.Prismatic]);
    }

    // Two batteries each wired to their own gate compile two Direct rows with distinct sources; each closes from its own enable.
    [Fact]
    public void PhysicsCompiler_TwoCircuitsCompileTwoDirectRoutesWithDistinctSources()
    {
        var gate1 = Gate(1, -3); var gate2 = Gate(2, 3);
        var battery1 = Battery(3, ElectricalEnable.Enabled, -3); var battery2 = Battery(4, ElectricalEnable.Enabled, 3);
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new([gate1, gate2, battery1, battery2]), Connections: new([Supply(battery1.Id, gate1.Id), Supply(battery2.Id, gate2.Id)])), new(1, 1));
        var routes = scene.SupplyRoutes.ToArray();
        Assert.Equal(2, routes.Length);
        Assert.All(routes, route => Assert.Equal(SupplyRouteKind.Direct, route.Kind));
        var route1 = Assert.Single(routes, route => scene.Prismatics[route.Prismatic].BodyA == gate1.Id);
        var route2 = Assert.Single(routes, route => scene.Prismatics[route.Prismatic].BodyA == gate2.Id);
        Assert.Equal(battery1.Id, scene.Electrical.Sources[route1.Source].Owner);
        Assert.Equal(battery2.Id, scene.Electrical.Sources[route2.Source].Owner);

        var enables = new ElectricalEnable[2];
        enables[route1.Source] = ElectricalEnable.Disabled; enables[route2.Source] = ElectricalEnable.Enabled;
        var closed = PhysicsTickInputs.Compose(scene.SupplyRoutes, scene.Prismatics.Length, new(enables), _ => true).SupplyClosed;
        Assert.False(closed[route1.Prismatic]);
        Assert.True(closed[route2.Prismatic]);
    }

    // Story 9R.4: a Battery → Plate → Gate chain compiles a Switched row from the plate's battery through the plate's own contact,
    // whatever the battery's authored enable; it closes only while that battery is enabled and that plate's contact is closed.
    [Theory]
    [InlineData(ElectricalEnable.Disabled)]
    [InlineData(ElectricalEnable.Enabled)]
    public void PhysicsCompiler_PlateChainCompilesASwitchedRowThroughThePlate(ElectricalEnable authored)
    {
        var gate = Gate(1); var battery = Battery(2, authored);
        var plate = WorkshopInput.PressurePlate(new(3), -2, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([gate, battery, plate]),
            Connections: new([Supply(battery.Id, plate.Id), Supply(plate.Id, gate.Id)])), new(1, 1));
        var route = Assert.Single(scene.SupplyRoutes.ToArray());
        Assert.Equal((SupplyRouteKind.Switched, plate.Id), (route.Kind, route.Switch));
        Assert.Equal(battery.Id, scene.Electrical.Sources[route.Source].Owner);
        Assert.Equal((Half)0, scene.Bodies.ToArray().Single(b => b.Id == WorkshopPhysicsCompiler.GateBlade(gate.Id)).LinearDrag.Value);
        bool Closed(ElectricalEnable enable, bool contact) => PhysicsTickInputs.Compose(scene.SupplyRoutes, scene.Prismatics.Length,
            new([enable]), owner => owner == plate.Id ? contact : throw new InvalidOperationException($"asked for {owner.Value}")).SupplyClosed[route.Prismatic];
        Assert.True(Closed(ElectricalEnable.Enabled, true));
        Assert.False(Closed(ElectricalEnable.Enabled, false));
        Assert.False(Closed(ElectricalEnable.Disabled, true));
    }

    // Controls: a plate with no battery supplies no route, and a plate wired to no gate compiles no route but still its own row.
    [Fact]
    public void PhysicsCompiler_APlateWithoutABatteryOrWithoutAGateCompilesNoRoute()
    {
        var gate = Gate(1);
        var plate = WorkshopInput.PressurePlate(new(3), -2, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass);
        var unsupplied = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([gate, plate]),
            Connections: new([Supply(plate.Id, gate.Id)])), new(1, 1));
        Assert.Empty(unsupplied.SupplyRoutes.ToArray());
        var battery = Battery(2, ElectricalEnable.Enabled);
        var ungated = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([battery, plate]),
            Connections: new([Supply(battery.Id, plate.Id)])), new(1, 1));
        Assert.Empty(ungated.SupplyRoutes.ToArray());
        Assert.Equal(plate.Id, Assert.Single(ungated.ContactLoadSensors.ToArray()).Frame);
    }

    // Per-kind admission: a Direct row names no switch, a Switched row names a static switch, an undefined kind is rejected.
    [Fact]
    public void SceneAdmission_RejectsASupplyRouteThatDoesNotMatchItsKind()
    {
        var gate = Gate(1); var battery = Battery(2, ElectricalEnable.Enabled);
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([gate, battery]),
            Connections: new([Supply(battery.Id, gate.Id)])), new(1, 1));
        var prismatic = scene.Prismatics[0].Id;
        var source = scene.Electrical.Sources[0].Id;
        PhysicsSceneDeclaration Admit(SupplyRouteDeclaration route) => new(scene.Document, scene.NextIdentity, scene.Bodies, scene.Colliders,
            scene.Materials, scene.Sensors, scene.Guides, scene.Triggers, scene.ContactWorks, scene.OrientationSensors,
            scene.Electrical.Sources, scene.Electrical.Bindings, scene.Prismatics, [route]);
        Assert.Equal(SupplyRouteKind.Direct, Assert.Single(Admit(new(prismatic, source, SupplyRouteKind.Direct, default)).SupplyRoutes.ToArray()).Kind);
        Assert.Throws<ArgumentException>(() => Admit(new(prismatic, source, SupplyRouteKind.Direct, gate.Id)));
        Assert.Throws<ArgumentException>(() => Admit(new(prismatic, source, SupplyRouteKind.Switched, default)));
        Assert.Throws<ArgumentException>(() => Admit(new(prismatic, source, SupplyRouteKind.Switched, WorkshopPhysicsCompiler.GateBlade(gate.Id))));
        Assert.Equal(gate.Id, Assert.Single(Admit(new(prismatic, source, SupplyRouteKind.Switched, gate.Id)).SupplyRoutes.ToArray()).Switch);
        Assert.Throws<ArgumentException>(() => Admit(new(prismatic, source, (SupplyRouteKind)3, default)));
    }

    [Fact]
    public void WorkbenchCapacity_ValidatesPoweredGateFootprint()
    {
        var fp = WorkbenchFootprint.Of(PartId.PoweredGate);
        Assert.Equal(2, fp.Bodies);
        Assert.Equal(1, fp.DynamicBodies);
        Assert.Equal(20, fp.Colliders);
        Assert.Equal(0, fp.Triggers);
        Assert.Equal(0, fp.ContactWorks);
        Assert.Equal(0, fp.ElectricalSources);
        Assert.Equal(1, fp.PrismaticConstraints);

        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
        IWorkshopInstance[] instances = [gate];
        WorkbenchCapacity.Validate(instances);
    }
}
