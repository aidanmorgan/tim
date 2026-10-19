using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Catalogue authoring boundary: compiles geometry/material values, never selects a numerical law.</summary>
public static class WorkshopPhysicsCompiler
{
    private const ulong InternalIdentityBase = 1UL << 32;
    private const ulong PartIdentityStride = 256;
    private static readonly (MetreVector Centre, MetreVector HalfExtents)[] ReceiverWalls =
    [
        (new((Half)0, (Half)(-.45), (Half)0), new((Half).75, (Half).075, (Half).75)),
        (new((Half)(-.75), (Half)0, (Half)0), new((Half).06, (Half).5, (Half).8)),
        (new((Half).75, (Half)0, (Half)0), new((Half).06, (Half).5, (Half).8)),
        (new((Half)0, (Half)0, (Half)(-.75)), new((Half).75, (Half).5, (Half).06)),
        (new((Half)0, (Half)(-.2), (Half).75), new((Half).75, (Half).3, (Half).06))
    ];
    public static PhysicsSceneDeclaration Compile(WorkshopConstruction construction, PhysicsDocumentId document)
    {
        construction.Validate();
        var bodies = new List<RigidBodyDeclaration>();
        var colliders = new List<ColliderDeclaration>();
        var materials = new List<ContactMaterialDeclaration>();
        var sensors = new List<ResidenceSensorDeclaration>();
        var guides = new List<PlanarGuideDeclaration>();
        var triggers = new List<ContactTriggerDeclaration>();
        var contactWorks = new List<ContactWorkDeclaration>();
        var prismatics = new List<PrismaticConstraintDeclaration>();
        var electricalSources = new List<ElectricalSourceDeclaration>();
        var orientationSensors = new List<OrientationSensorDeclaration>();
        var plane = new GpuBodyId(InternalIdentityBase);
        var planeMaterial = new GpuMaterialId(InternalIdentityBase + 1);
        var height = WorkshopInput.Position((double)WorkshopConstruction.WorkbenchSurface.Value);
        bodies.Add(new(plane, RigidMotionKind.Static, new(0, height.Cell, 0),
            new(0f, height.Local, 0f), CanonicalRotation.Identity, default, default, new((Half)0), default, new((Half)0)));
        // Static surfaces and boxes declare zero rolling resistance; only a ball material carries a coefficient.
        materials.Add(new(planeMaterial, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
        colliders.Add(new(new(InternalIdentityBase + 2), plane, planeMaterial,
            ColliderShapeKind.Plane, RigidLocalPose.Identity, new((Half)0), default));
        var next = InternalIdentityBase + 3;
        foreach (var ball in construction.Instances.OfType<WorkshopBall>().OrderBy(b => b.Id.Value))
        {
            var first = PartIdentities(ball.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(ball.Id, RigidMotionKind.Dynamic, ball.Cell, ball.Local, ball.Rotation,
                default, default, ball.Material.Mass,
                new((Half)0, (Half)(-WorkshopConstruction.Gravity.Value), (Half)0), ball.Material.Drag));
            materials.Add(new(material, ball.Material.Bounce, ball.Material.BounceThreshold, ball.Material.Friction, ball.Material.Rolling));
            colliders.Add(new(new(first + 1), ball.Id, material, ColliderShapeKind.Sphere,
                RigidLocalPose.Identity, ball.Material.Radius, default));
        }
        foreach (var domino in construction.Instances.OfType<WorkshopDomino>().OrderBy(d => d.Id.Value))
        {
            var first = PartIdentities(domino.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(domino.Id, RigidMotionKind.Dynamic, domino.Cell, domino.Local, domino.Rotation,
                default, default, domino.Material.Mass,
                new((Half)0, (Half)(-WorkshopConstruction.Gravity.Value), (Half)0), new((Half)0)));
            materials.Add(new(material, domino.Material.Bounce, DominoMaterial.BounceThreshold, domino.Material.Friction, new((Half)0)));
            colliders.Add(new(new(first + 1), domino.Id, material, ColliderShapeKind.Box,
                RigidLocalPose.Identity, new((Half)0), domino.Material.HalfExtents));
            // The sensor exists only for a wired tile; its initial pose is the admitted placement, so Reset rearms it.
            if (construction.Connections.HasSource(domino.Id))
                orientationSensors.Add(new(OrientationSensor(domino.Id), domino.Id, domino.Rotation, WorkshopDomino.ActivationThreshold));
        }
        foreach (var receiver in construction.Instances.OfType<WorkshopReceiver>().OrderBy(r => r.Id.Value))
        {
            var first = PartIdentities(receiver.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(receiver.Id, RigidMotionKind.Static, receiver.Cell, receiver.Local, receiver.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half).12), new((Half).1), new((Half).3), new((Half)0)));
            var walls = ReceiverWalls;
            for (var i = 0; i < walls.Length; i++) AddBox(first + 1 + (ulong)i, walls[i].Centre, walls[i].HalfExtents);
            var targetIndex = 0UL;
            foreach (var target in construction.Instances.OfType<WorkshopBall>().OrderBy(b => b.Id.Value))
            {
                var sensorId = construction.Puzzle.Id == WorkshopPuzzleId.Free ? first + 16 + targetIndex * 2 : first + 6;
                sensors.Add(new(new(sensorId), receiver.Id, target.Id, RigidLocalPose.Identity,
                    new((Half)(-.66), (Half)(-.4), (Half)(-.66)),
                    new((Half).66, (Half)((Half).45 + receiver.Capture.Margin.Value), (Half).66),
                    receiver.Capture.SpeedLimit, receiver.Capture.Dwell, receiver.Capture.Participation));
                var region = receiver.ForceRegion;
                guides.Add(new(new(sensorId + 1), receiver.Id, target.Id, RigidLocalPose.Identity,
                    region.Minimum, region.Maximum, region.SupportHeight, region.SupportMargin, region.MaximumAcceleration));
                targetIndex++;
            }
            void AddBox(ulong id, MetreVector position, MetreVector half) =>
                colliders.Add(new(new(id), receiver.Id, material, ColliderShapeKind.Box,
                    new(position, CanonicalRotation.Identity), new((Half)0), half));
        }
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopRamp or WorkshopWall)) continue;
            var first = PartIdentities(instance.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(instance.Id, RigidMotionKind.Static, instance.Cell, instance.Local, instance.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
            var halfExtents = instance switch
            {
                WorkshopRamp ramp => new MetreVector((Half)(ramp.Dimensions.Length.Value * (Half).5),
                    (Half)(RampDimensions.Thickness.Value * (Half).5), (Half)(ramp.Dimensions.Width.Value * (Half).5)),
                WorkshopWall wall => new MetreVector((Half)(wall.Dimensions.Width.Value * (Half).5),
                    (Half)(wall.Dimensions.Height.Value * (Half).5), (Half)(wall.Dimensions.Thickness.Value * (Half).5)),
                _ => throw new ArgumentException("Unsupported static box declaration.")
            };
            colliders.Add(new(new(first + 1), instance.Id, material, ColliderShapeKind.Box,
                RigidLocalPose.Identity, new((Half)0), halfExtents));
        }
        foreach (var instance in construction.Instances)
        {
            if (instance is not (WorkshopSwitch or WorkshopLamp or WorkshopDelay or WorkshopBumper or WorkshopBattery)) continue;
            var first = PartIdentities(instance.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(instance.Id, RigidMotionKind.Static, instance.Cell, instance.Local, instance.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            // Source MachinePart.InitialContactMaterial: all these static surfaces use (1,.1,.3).
            materials.Add(new(material, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
            if (instance is WorkshopBattery battery)
            {
                AddStaticBox(first + 1, default, new((Half).425f, (Half).525f, (Half).375f));
                electricalSources.Add(new(ElectricalSource(battery.Id), battery.Id, battery.Settings.Capacity,
                    battery.Settings.MaximumPower, battery.Settings.InitialFraction, battery.Settings.Enabled));
            }
            else if (instance is WorkshopSwitch trigger)
            {
                AddStaticBox(first + 1, new((Half)0, (Half)(-.15), (Half)0), new((Half).55, (Half).125, (Half).5));
                // Source AddBox(draw:false) suppresses art, not this second physical box.
                AddStaticBox(first + 2, new((Half)0, (Half).05, (Half)0), new((Half).4, (Half).09, (Half).375));
                if (construction.Puzzle.Id == WorkshopPuzzleId.Free || construction.Ball.HasValue)
                    triggers.Add(new(ContactTrigger(trigger.Id), trigger.Id, Targets(construction), trigger.Trigger.Threshold));
            }
            else if (instance is WorkshopBumper bumper)
            {
                colliders.Add(new(new(first + 1), bumper.Id, material, ColliderShapeKind.Sphere,
                    RigidLocalPose.Identity, new((Half).65), default));
                if (construction.Puzzle.Id == WorkshopPuzzleId.Free || construction.Ball.HasValue)
                    contactWorks.Add(new(ContactWork(bumper.Id), bumper.Id, Targets(construction),
                        bumper.Work.Strength, bumper.Work.Preload, new(.05f), 72));
            }
            else if (instance is WorkshopDelay)
                AddStaticBox(first + 1, new((Half)0, (Half)(-.7), (Half)0), new((Half).675, (Half).075, (Half).4));
            else AddStaticBox(first + 1, new((Half)0, (Half)(-.35), (Half)0), new((Half).425, (Half).1, (Half).425));
            void AddStaticBox(ulong id, MetreVector position, MetreVector half) =>
                colliders.Add(new(new(id), instance.Id, material, ColliderShapeKind.Box,
                    new(position, CanonicalRotation.Identity), new((Half)0), half));
        }
        foreach (var spring in construction.Instances.OfType<WorkshopSpringboard>().OrderBy(value => value.Id.Value))
        {
            var first = PartIdentities(spring.Id); next = Math.Max(next, first + PartIdentityStride);
            var baseMaterial = new GpuMaterialId(first); var plateMaterial = new GpuMaterialId(first + 3);
            var plate = SpringboardPlate(spring.Id);
            bodies.Add(new(spring.Id, RigidMotionKind.Static, spring.Cell, spring.Local, spring.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(baseMaterial, new((Half)0), new((Half).05), new((Half).1), new((Half)0)));
            materials.Add(new(plateMaterial, new((Half)0), new((Half).05), new((Half).1), new((Half)0)));
            colliders.Add(new(new(first + 1), spring.Id, baseMaterial, ColliderShapeKind.Box,
                new(new((Half)0, (Half)(-.36), (Half)0), CanonicalRotation.Identity), new((Half)0),
                new((Half).65, (Half).06, (Half).6)));
            var rotation = new System.Numerics.Quaternion((float)spring.Rotation.X, (float)spring.Rotation.Y,
                (float)spring.Rotation.Z, (float)spring.Rotation.W);
            var displacement = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, .14f - spring.Settings.InitialCompression, 0), rotation);
            var x = WorkshopInput.Position((spring.Cell.X + (float)spring.Local.X) / 16f + displacement.X);
            var y = WorkshopInput.Position((spring.Cell.Y + (float)spring.Local.Y) / 16f + displacement.Y);
            var z = WorkshopInput.Position((spring.Cell.Z + (float)spring.Local.Z) / 16f + displacement.Z);
            bodies.Add(new(plate, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                spring.Rotation, default, default, new((Half).25),
                new((Half)0, (Half)(-WorkshopConstruction.Gravity.Value), (Half)0), new((Half)0)));
            colliders.Add(new(new(first + 4), plate, plateMaterial, ColliderShapeKind.Box,
                RigidLocalPose.Identity, new((Half)0), new((Half).65, (Half).075, (Half).6)));
            prismatics.Add(new(new(first + 5), spring.Id, plate,
                new(new System.Numerics.Vector3(0, .14f, 0), System.Numerics.Quaternion.Identity), ConstraintFrame.Identity,
                -.25f, 0, spring.Settings.Stiffness, spring.Settings.Damping, ConnectedCollision.Disabled));
        }
        var contactWorkOwners = contactWorks.Select(cw => cw.Owner).ToHashSet();
        foreach (var instance in construction.Instances.OfType<IWorkshopDeclaredColliders>().OrderBy(i => i.Id.Value))
        {
            var first = PartIdentities(instance.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(instance.Id, RigidMotionKind.Static, instance.Cell, instance.Local, instance.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
            if (instance.PartId == PartId.PoweredGate)
            {
                var bladeMaterial = new GpuMaterialId(first + 3);
                materials.Add(new(bladeMaterial, new((Half).1), new((Half).1), new((Half).3), new((Half)0)));
                var blade = GateBlade(instance.Id);
                var isDirectlyPowered = construction.Connections.Any(link =>
                    link.Domain == WorkshopConnectionDomain.Electrical &&
                    link.Target == instance.Id &&
                    construction.Instances.OfType<WorkshopBattery>().Any(b => b.Id == link.Source && b.Settings.Enabled == ElectricalEnable.Enabled));
                var isPlatePowered = construction.Connections.Any(link =>
                    link.Domain == WorkshopConnectionDomain.Electrical &&
                    link.Target == instance.Id &&
                    construction.Instances.OfType<WorkshopPressurePlate>().Any(p => p.Id == link.Source &&
                        construction.Connections.Any(bLink =>
                            bLink.Domain == WorkshopConnectionDomain.Electrical &&
                            bLink.Target == p.Id &&
                            bLink.Input == WorkshopSocket.PowerIn &&
                            construction.Instances.OfType<WorkshopBattery>().Any(b => b.Id == bLink.Source && b.Settings.Enabled == ElectricalEnable.Enabled))));
                // Latch- and Counter-switched supplies compile once below into PhysicsSceneDeclaration.SwitchedSupplies; each tick closes
                // them from committed contact state via PhysicsTickInputs, so their blades carry no mode-encoding drag.
                var bladeDrag = isDirectlyPowered ? (Half)0.0625f : isPlatePowered ? (Half)0.09375f : (Half)0f;
                bodies.Add(new(blade, RigidMotionKind.Dynamic, instance.Cell, instance.Local, instance.Rotation,
                    default, default, new((Half)1f),
                    new((Half)0, (Half)(-WorkshopConstruction.Gravity.Value), (Half)0), new InverseSeconds(bladeDrag)));
                colliders.Add(new(new(first + 4), blade, bladeMaterial, ColliderShapeKind.Box,
                    RigidLocalPose.Identity, new((Half)0), new MetreVector((Half)0.06f, (Half)0.72f, (Half)0.72f)));
                prismatics.Add(new(new(first + 5), instance.Id, blade,
                    ConstraintFrame.Identity, ConstraintFrame.Identity,
                    0f, 1.55f, 14f, 4f, ConnectedCollision.Disabled));
                for (var i = 0; i < instance.DeclaredColliders.Count; i++)
                {
                    var extracted = instance.DeclaredColliders[i];
                    colliders.Add(new(new(first + 6 + (ulong)i), instance.Id, material, extracted.ShapeKind,
                        extracted.LocalPose, new Metres(extracted.Radius), extracted.HalfExtents));
                }
            }
            else
            {
                for (var i = 0; i < instance.DeclaredColliders.Count; i++)
                {
                    var extracted = instance.DeclaredColliders[i];
                    colliders.Add(new(new(first + 1 + (ulong)i), instance.Id, material, extracted.ShapeKind,
                        extracted.LocalPose, new Metres(extracted.Radius), extracted.HalfExtents));
                }
            }
        }
        foreach (var plate in construction.Instances.OfType<WorkshopPressurePlate>().OrderBy(p => p.Id.Value))
        {
            var first = PartIdentities(plate.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(plate.Id, RigidMotionKind.Static, plate.Cell, plate.Local, plate.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half)0.1), new((Half)0.1), new((Half)0.3), new((Half)0)));
            colliders.Add(new(new(first + 1), plate.Id, material, ColliderShapeKind.Box,
                new(new((Half)0, (Half)(-0.12f), (Half)0), CanonicalRotation.Identity), new((Half)0),
                new((Half)1.1f, (Half)0.1f, (Half)1.1f)));
            colliders.Add(new(new(first + 2), plate.Id, material, ColliderShapeKind.Box,
                new(new((Half)0, (Half)0.03f, (Half)0), CanonicalRotation.Identity), new((Half)0),
                new((Half)0.9f, (Half)0.05f, (Half)0.9f)));
        }
        foreach (var counter in construction.Instances.OfType<WorkshopCounter>().OrderBy(c => c.Id.Value))
        {
            var first = PartIdentities(counter.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(counter.Id, RigidMotionKind.Static, counter.Cell, counter.Local, counter.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
            colliders.Add(new(new(first + 1), counter.Id, material, ColliderShapeKind.Box,
                RigidLocalPose.Identity, new((Half)0),
                new((Half)0.825f, (Half)0.625f, (Half)0.325f)));
            colliders.Add(new(new(first + 2), counter.Id, material, ColliderShapeKind.Box,
                new(new((Half)0, (Half)(-0.72f), (Half)0), CanonicalRotation.Identity), new((Half)0),
                new((Half)0.9f, (Half)0.08f, (Half)0.425f)));
        }
        foreach (var clock in construction.Instances.OfType<WorkshopClock>().OrderBy(c => c.Id.Value))
        {
            var first = PartIdentities(clock.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(clock.Id, RigidMotionKind.Static, clock.Cell, clock.Local, clock.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
            colliders.Add(new(new(first + 1), clock.Id, material, ColliderShapeKind.Box,
                RigidLocalPose.Identity, new((Half)0),
                new((Half)0.7f, (Half)0.85f, (Half)0.3f)));
            colliders.Add(new(new(first + 2), clock.Id, material, ColliderShapeKind.Box,
                new(new((Half)0, (Half)(-0.95f), (Half)0), CanonicalRotation.Identity), new((Half)0),
                new((Half)0.8f, (Half)0.1f, (Half)0.425f)));
        }
        foreach (var latch in construction.Instances.OfType<WorkshopLatch>().OrderBy(l => l.Id.Value))
        {
            var first = PartIdentities(latch.Id); next = Math.Max(next, first + PartIdentityStride);
            var material = new GpuMaterialId(first);
            bodies.Add(new(latch.Id, RigidMotionKind.Static, latch.Cell, latch.Local, latch.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
            var declared = WorkshopCanonicalColliders.LatchColliders;
            for (var i = 0; i < declared.Count; i++)
                colliders.Add(new(new(first + 1 + (ulong)i), latch.Id, material, declared[i].ShapeKind,
                    declared[i].LocalPose, new Metres(declared[i].Radius), declared[i].HalfExtents));
        }
        // Switched supplies compile once here (authoring → compile → physics); the tick reads only these indices and committed switch state.
        var switchedSupplies = new List<SwitchedSupplyRouteDeclaration>();
        foreach (var gate in construction.Instances.OfType<IWorkshopDeclaredColliders>().Where(i => i.PartId == PartId.PoweredGate).OrderBy(i => i.Id.Value))
            if (SwitchedSupplyFeed(construction, gate.Id) is { } feed)
                switchedSupplies.Add(new(new(checked(PartIdentities(gate.Id) + 5)), ElectricalSource(feed.Battery), feed.Switch));
        return new(document, next, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray(), guides.ToArray(), triggers.ToArray(), contactWorks.ToArray(), orientationSensors.ToArray(), electricalSources.OrderBy(source => source.Id.Value).ToArray(),
            construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Electrical && contactWorkOwners.Contains(link.Target))
                .Select(link => new ElectricalStorageBinding(ElectricalSource(link.Source), ContactWork(link.Target))).ToArray(), prismatics.ToArray(),
            switchedSupplies.ToArray());
    }
    /// <summary>Consumer PowerIn ← State Latch or Counter Supply, whose PowerIn ← a Battery. Null when either link is absent.</summary>
    private static (GpuBodyId Switch, GpuBodyId Battery)? SwitchedSupplyFeed(WorkshopConstruction construction, GpuBodyId consumer)
    {
        GpuBodyId Source(GpuBodyId target, Func<IWorkshopInstance, bool> admitted)
        {
            foreach (var link in construction.Connections)
                if (link.Domain == WorkshopConnectionDomain.Electrical && link.Target == target && link.Input == WorkshopSocket.PowerIn &&
                    construction.Instances.FirstOrDefault(instance => instance.Id == link.Source) is { } source && admitted(source))
                    return link.Source;
            return default;
        }
        var contact = Source(consumer, instance => instance is WorkshopLatch or WorkshopCounter);
        if (contact.Value == 0) return null;
        var battery = Source(contact, instance => instance is WorkshopBattery);
        return battery.Value == 0 ? null : (contact, battery);
    }
    public static bool IsSecondaryBodyIdentity(GpuBodyId owner, GpuBodyId body)
    {
        return body == SpringboardPlate(owner) || body == GateBlade(owner);
    }

    public static GpuBodyId SpringboardPlate(GpuBodyId owner) => new(checked(PartIdentities(owner) + 2));

    public static GpuBodyId GateBlade(GpuBodyId owner) => new(checked(PartIdentities(owner) + 2));

    public static ElectricalSourceId ElectricalSource(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuSensorId CaptureSensor(WorkshopReceiver receiver) => new(checked(PartIdentities(receiver.Id) + 6));

    public static GpuContactTriggerId ContactTrigger(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuOrientationSensorId OrientationSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuPassageSensorId PassageSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuContactWorkId ContactWork(GpuBodyId owner) => new(checked(PartIdentities(owner) + 8));

    private static BodyTargetSet Targets(WorkshopConstruction construction) => construction.Puzzle.Id == WorkshopPuzzleId.Free
        ? new(BodyTargetKind.AllDynamic, default)
        : new(BodyTargetKind.NamedBody, construction.Ball?.Id ?? throw new ArgumentException("Missing named puzzle body."));

    internal static ulong PartIdentities(GpuBodyId id)
    {
        if (id.Value == 0 || id.Value >= InternalIdentityBase)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.IdentityExhausted, "Authored body identity exceeds its domain.");
        return checked(InternalIdentityBase + PartIdentityStride + id.Value * PartIdentityStride);
    }
}
