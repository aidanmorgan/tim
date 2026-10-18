using System;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
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
        Assert.Throws<ArgumentException>(() => invalidConnections.Validate(instances));
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

    [Fact]
    public void PhysicsCompiler_WiredToBattery_SetsBladeLinearDragAndExcludesFromElectricalBindings()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
        var battery = WorkshopInput.Battery(new(2), 2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var link = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([gate, battery]),
            Connections: new([link]));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));

        var bladeId = WorkshopPhysicsCompiler.GateBlade(gate.Id);
        var gateBlade = scene.Bodies.ToArray().Single(b => b.Id == bladeId);
        Assert.Equal((Half)0.0625f, gateBlade.LinearDrag.Value); // Wired to enabled battery -> 0.0625

        // Electrical source exists (battery)
        Assert.Single(scene.Electrical.Sources.ToArray());
        // But Electrical bindings must NOT include gate (gate is an actuator, not a contact work store)
        Assert.Empty(scene.Electrical.Bindings.ToArray());
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
        Assert.Equal(0, fp.ActivationNodes);
        Assert.Equal(0, fp.ElectricalSources);
        Assert.Equal(1, fp.PrismaticConstraints);

        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders);
        IWorkshopInstance[] instances = [gate];
        WorkbenchCapacity.Validate(instances);
    }
}
