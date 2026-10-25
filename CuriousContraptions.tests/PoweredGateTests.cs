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
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.PoweredGate));
        var def = registry.Definitions[PartId.PoweredGate];
        Assert.Equal(PartId.PoweredGate, def.PartId);
        Assert.Equal("powered_gate", def.Id);
        Assert.Equal("Powered gate", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Motion, def.Category);
        Assert.Equal("Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes.", def.Description);
        Assert.NotNull(def.Scene);

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
    public void PoweredGateScene_ExtractsItsSleeveCollarsFrameSocketsAndThreePortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/PoweredGate/powered_gate.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

            // Eight frame colliders: the sleeve Tube and two collar Tubes (engine-hollow-shapes), the housing, two rails, and
            // (elements-visible-solid) the PowerIn socket and the indicator spheres.
            Assert.Equal(8, colliders.Count);

            // The sleeve: the 0.65 m bore to the drawn 0.70 m wall over its 0.4 m half-length; each collar: the bore to the drawn
            // 0.78 m over its 0.09 m half-length at x ∓0.4, reaching the mouths at ∓0.49. All about local X.
            var x = System.Numerics.Vector3.UnitX;
            TestBundle.AssertHollow(colliders[0], ColliderShapeKind.Tube, .7f, new(.65f, .4f, 0f), System.Numerics.Vector3.Zero, x);
            TestBundle.AssertHollow(colliders[1], ColliderShapeKind.Tube, .78f, new(.65f, .09f, 0f), new(-.4f, 0, 0), x);
            TestBundle.AssertHollow(colliders[2], ColliderShapeKind.Tube, .78f, new(.65f, .09f, 0f), new(.4f, 0, 0), x);

            var housing = colliders[3];
            Assert.Equal(ColliderShapeKind.Box, housing.ShapeKind);
            Assert.Equal(new MetreVector(0.4f, 0.325f, 0.9f), housing.HalfExtents);
            Assert.Equal(0f, housing.LocalPose.Translation.X);
            Assert.Equal(1.65f, housing.LocalPose.Translation.Y);
            Assert.Equal(0f, housing.LocalPose.Translation.Z);

            var railStart = colliders[4];
            Assert.Equal(ColliderShapeKind.Box, railStart.ShapeKind);
            Assert.Equal(new MetreVector(0.12f, 0.85f, 0.06f), railStart.HalfExtents);
            Assert.Equal(-0.82f, railStart.LocalPose.Translation.Z);

            var railEnd = colliders[5];
            Assert.Equal(ColliderShapeKind.Box, railEnd.ShapeKind);
            Assert.Equal(new MetreVector(0.12f, 0.85f, 0.06f), railEnd.HalfExtents);
            Assert.Equal(0.82f, railEnd.LocalPose.Translation.Z);

            Assert.Equal(3, ports.Count);
            Assert.True(ports.ContainsKey("PowerIn"));
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            Assert.Equal(new Vector3(0, 1.65f, 0.92f), ports["PowerIn"].Position);
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
        var canonical = DeclaredPhysics.Colliders(PartId.PoweredGate);
        Assert.Equal(8, canonical.Count);
        Assert.Equal(canonical, DeclaredPhysics.Colliders(PartId.PoweredGate));
    }

    [Fact]
    public void Ports_DeclareSinglePowerInInput()
    {
        var ports = DeclaredPorts.Current.Of(PartId.PoweredGate);
        Assert.Equal(1, ports.Length);
        var port = ports[0];
        Assert.Equal(WorkshopSocket.PowerIn, port.Socket);
        Assert.Equal(WorkshopConnectionDomain.Electrical, port.Domain);
        Assert.Equal(WorkshopPortDirection.Input, port.Direction);

        var localPos = DeclaredPorts.Current.LocalPosition(PartId.PoweredGate, WorkshopSocket.PowerIn);
        Assert.Equal(new System.Numerics.Vector3(0f, 1.65f, 0.92f), localPos);
    }

    [Fact]
    public void Connections_AcceptsElectricalAndRejectsActivation()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 2, 0, 0);
        var switchPart = WorkshopInput.Generic(PartId.ImpactSwitch, new(3), -2, 0, 0, 0, 0, 0, 1);

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
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new(2), 0, 3, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(1),
            WorkshopCadenceSettings.Default(),
            new([gate, ball]));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));

        // Bodies: plane + ball + gate frame + gate blade = 4 bodies
        Assert.Equal(4, scene.Bodies.Length);
        var gateFrame = scene.Bodies.ToArray().Single(b => b.Id == gate.Id);
        Assert.Equal(RigidMotionKind.Static, gateFrame.Motion);

        var bladeId = WorkshopPhysicsCompiler.SliderBody(gate.Id, 0);
        var gateBlade = scene.Bodies.ToArray().Single(b => b.Id == bladeId);
        Assert.Equal(RigidMotionKind.Dynamic, gateBlade.Motion);
        Assert.Equal(1f, gateBlade.Mass.Value);
        Assert.Equal(0f, gateBlade.LinearDrag.Value); // Unwired -> 0 drag

        // Colliders: plane (1) + ball (1) + blade (1) + frame (8) = 11 colliders
        Assert.Equal(11, scene.Colliders.Length);
        var bladeCollider = scene.Colliders.ToArray().Single(c => c.Body == bladeId);
        Assert.Equal(ColliderShapeKind.Box, bladeCollider.Shape);
        Assert.Equal(new MetreVector(0.06f, 0.72f, 0.72f), bladeCollider.HalfExtents);

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
        WorkshopInput.Generic(PartId.PoweredGate, new(id), 0, 0, z, 0, 0, 0, 1);
    private static WorkshopGenericInstance Battery(ulong id, ElectricalEnable enabled, double z = 0) =>
        PlacedParts.Battery(new(id), 2, 0, z, enabled: enabled);

    // Story 9R.3: a battery wired straight to the gate compiles one Direct route row whatever its authored enable; the blade
    // carries no mode-encoding drag and the gate (an actuator, not a contact work store) has no electrical binding.
    [Theory]
    [InlineData(ElectricalEnable.Disabled)]
    [InlineData(ElectricalEnable.Enabled)]
    public void DeclaredDirectGraphFollowsOnlyItsSource(ElectricalEnable authored)
    {
        var gate=Gate(1); var battery=Battery(2,authored);
        var scene=WorkshopPhysicsCompiler.Compile(new(new(1),WorkshopCadenceSettings.Default(),new(gate,battery),
            Connections:new(Supply(battery.Id,gate.Id))),new(1,1));
        Assert.Equal(2,scene.SupplyGraph.Nodes.Length);
        Assert.Equal(battery.Id,Assert.Single(scene.SupplyGraph.Nodes.ToArray(),node=>node.Kind==SupplyNodeKind.Source).Owner);
        Assert.Equal(gate.Id,Assert.Single(scene.SupplyGraph.Nodes.ToArray(),node=>node.Kind==SupplyNodeKind.Consumer).Owner);
        bool NoContact(GpuBodyId owner)=>throw new InvalidOperationException("Direct supply consulted a contact.");
        Assert.True(PhysicsTickInputs.Compose(scene.SupplyGraph,scene.Prismatics.Length,new([ElectricalEnable.Enabled]),NoContact).SupplyClosed[0]);
        Assert.False(PhysicsTickInputs.Compose(scene.SupplyGraph,scene.Prismatics.Length,new([ElectricalEnable.Disabled]),NoContact).SupplyClosed[0]);
        Assert.Empty(scene.Electrical.Bindings.ToArray());
        Assert.Equal(0f,scene.Bodies.ToArray().Single(body=>body.Id==WorkshopPhysicsCompiler.SliderBody(gate.Id, 0)).LinearDrag.Value);
    }

    [Fact]
    public void TwoDirectCircuitsFollowTheirOwnSources()
    {
        var gate1=Gate(1,-3);var gate2=Gate(2,3);
        var battery1=Battery(3,ElectricalEnable.Enabled,-3);var battery2=Battery(4,ElectricalEnable.Enabled,3);
        var scene=WorkshopPhysicsCompiler.Compile(new(new(1),WorkshopCadenceSettings.Default(),new(gate1,gate2,battery1,battery2),
            Connections:new(Supply(battery1.Id,gate1.Id),Supply(battery2.Id,gate2.Id))),new(1,1));
        var enables=scene.Electrical.Sources.ToArray().Select(source=>source.Owner==battery1.Id?ElectricalEnable.Disabled:ElectricalEnable.Enabled).ToArray();
        var closed=PhysicsTickInputs.Compose(scene.SupplyGraph,scene.Prismatics.Length,new(enables),_=>true).SupplyClosed;
        Assert.False(closed[Array.FindIndex(scene.Prismatics.ToArray(),joint=>joint.BodyA==gate1.Id)]);
        Assert.True(closed[Array.FindIndex(scene.Prismatics.ToArray(),joint=>joint.BodyA==gate2.Id)]);
    }

    [Theory]
    [InlineData(ElectricalEnable.Disabled)]
    [InlineData(ElectricalEnable.Enabled)]
    public void DeclaredPlateContactRequiresBothSupplyAndCommittedClosure(ElectricalEnable authored)
    {
        var gate=Gate(1);var battery=Battery(2,authored);
        var plate=PlacedParts.Plate(new(3),-2,0,0,.5f);
        var construction=new WorkshopConstruction(new(1),WorkshopCadenceSettings.Default(),new(gate,battery,plate),
            Connections:new(Supply(battery.Id,plate.Id),Supply(plate.Id,gate.Id)));
        var scene=WorkshopPhysicsCompiler.Compile(construction,new(1,1));
        Assert.Equal(plate.Id,Assert.Single(scene.SupplyGraph.Nodes.ToArray(),node=>node.Kind==SupplyNodeKind.Contact).Owner);
        bool Closed(ElectricalEnable enable,bool contact)=>PhysicsTickInputs.Compose(scene.SupplyGraph,scene.Prismatics.Length,
            new([enable]),owner=>owner==plate.Id?contact:throw new InvalidOperationException()).SupplyClosed[0];
        Assert.True(Closed(ElectricalEnable.Enabled,true));Assert.False(Closed(ElectricalEnable.Enabled,false));Assert.False(Closed(ElectricalEnable.Disabled,true));
        var unsupplied=WorkshopPhysicsCompiler.Compile(construction with {Connections=new(Supply(plate.Id,gate.Id))},new(1,1));
        Assert.False(PhysicsTickInputs.Compose(unsupplied.SupplyGraph,1,new([ElectricalEnable.Enabled]),_=>true).SupplyClosed[0]);
        var ungated=WorkshopPhysicsCompiler.Compile(new(new(1),WorkshopCadenceSettings.Default(),new(battery,plate),Connections:new(Supply(battery.Id,plate.Id))),new(1,1));
        Assert.Empty(PhysicsTickInputs.Compose(ungated.SupplyGraph,0,new([ElectricalEnable.Enabled]),_=>true).SupplyClosed);
        Assert.Equal(plate.Id,Assert.Single(ungated.ContactLoadSensors.ToArray()).Frame);
    }

    [Fact]
    public void SceneAdmissionRejectsMalformedGraphNodesAndEdges()
    {
        var gate=Gate(1);var battery=Battery(2,ElectricalEnable.Enabled);
        var scene=WorkshopPhysicsCompiler.Compile(new(new(1),WorkshopCadenceSettings.Default(),new(gate,battery)),new(1,1));
        CompiledSupplyGraph Admit(SupplyNodeDeclaration[] nodes,params SupplyEdge[] edges)=>new(nodes,edges,
            scene.Electrical.Sources,scene.Prismatics,scene.PrismaticDrives,scene.Bodies);
        var source=new SupplyNodeDeclaration(battery.Id,SupplyNodeKind.Source,scene.Electrical.Sources[0].Id);
        var consumer=new SupplyNodeDeclaration(gate.Id,SupplyNodeKind.Consumer,Consumer:scene.Prismatics[0].Id);
        Assert.Equal(2,Admit([source,consumer],new SupplyEdge(battery.Id,gate.Id,WorkshopSocket.PowerIn)).Nodes.Length);
        Assert.Throws<ArgumentException>(()=>Admit([source with {Kind=(SupplyNodeKind)99}]));
        Assert.Throws<ArgumentException>(()=>Admit([source with {Source=default}]));
        Assert.Throws<ArgumentException>(()=>Admit([source,source]));
        Assert.Throws<ArgumentException>(()=>Admit([consumer with {Consumer=new(999)}]));
        Assert.Throws<ArgumentException>(()=>Admit([new(WorkshopPhysicsCompiler.SliderBody(gate.Id, 0),SupplyNodeKind.Contact)]));
        Assert.Throws<ArgumentException>(()=>Admit([source,consumer],new SupplyEdge(gate.Id,battery.Id,WorkshopSocket.PowerIn)));
        Assert.Throws<ArgumentException>(()=>Admit([source,consumer],new SupplyEdge(battery.Id,gate.Id,WorkshopSocket.PowerIn),new(battery.Id,gate.Id,WorkshopSocket.PowerIn)));
        Assert.Throws<ArgumentException>(()=>Admit([source,consumer],new SupplyEdge(battery.Id,new(999),WorkshopSocket.PowerIn)));
    }

    [Fact]
    public void WorkbenchCapacity_ValidatesPoweredGateFootprint()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 0, 0, 0, 0, 0, 1);
        IWorkshopInstance[] instances = [gate];
        WorkbenchCapacity.Validate(instances);

        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(gate)), new(1, 2)).World;
        Assert.Equal(2 + 1, world.Bodies);
        Assert.Equal(1, world.Dynamic);
        Assert.Equal(9 + 1, world.Colliders); // the frame's 8 (with the socket and indicator spheres) and the blade's 1, plus the plane
        Assert.Equal(0, world.Triggers);
        Assert.Equal(0, world.ContactWorks);
        Assert.Equal(0, world.ElectricalSources);
        Assert.Equal(1, world.Prismatics);
    }
}
