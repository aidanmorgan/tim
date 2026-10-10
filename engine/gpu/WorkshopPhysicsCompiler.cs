using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Catalogue authoring boundary: compiles geometry/material values, never selects a numerical law.</summary>
public static class WorkshopPhysicsCompiler
{
    private const ulong InternalIdentityBase = 1UL << 32;
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
        var electricalSources = new List<ElectricalSourceDeclaration>();
        var orientationSensors = new List<OrientationSensorDeclaration>();
        var plane = new GpuBodyId(InternalIdentityBase);
        var planeMaterial = new GpuMaterialId(InternalIdentityBase + 1);
        var height = WorkshopInput.Position((double)WorkshopConstruction.WorkbenchSurface.Value);
        bodies.Add(new(plane, RigidMotionKind.Static, new(0, height.Cell, 0),
            new((Half)0, height.Local, (Half)0), CanonicalRotation.Identity, default, default, new((Half)0), default, new((Half)0)));
        // Static surfaces and boxes declare zero rolling resistance; only a ball material carries a coefficient.
        materials.Add(new(planeMaterial, new((Half)1), new((Half).1), new((Half).3), new((Half)0)));
        colliders.Add(new(new(InternalIdentityBase + 2), plane, planeMaterial,
            ColliderShapeKind.Plane, RigidLocalPose.Identity, new((Half)0), default));
        var next = InternalIdentityBase + 3;
        foreach (var ball in construction.Instances.OfType<WorkshopBall>().OrderBy(b => b.Id.Value))
        {
            var first = PartIdentities(ball.Id); next = Math.Max(next, first + 64);
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
            var first = PartIdentities(domino.Id); next = Math.Max(next, first + 64);
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
            var first = PartIdentities(receiver.Id); next = Math.Max(next, first + 64);
            var material = new GpuMaterialId(first);
            bodies.Add(new(receiver.Id, RigidMotionKind.Static, receiver.Cell, receiver.Local, receiver.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half).12), new((Half).1), new((Half).3), new((Half)0)));
            var walls = ReceiverGeometry.Walls;
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
            var first = PartIdentities(instance.Id); next = Math.Max(next, first + 64);
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
            var first = PartIdentities(instance.Id); next = Math.Max(next, first + 64);
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
        return new(document, next, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray(), guides.ToArray(), triggers.ToArray(), contactWorks.ToArray(), orientationSensors.ToArray(), electricalSources.OrderBy(source => source.Id.Value).ToArray(),
            construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Electrical)
                .Select(link => new ElectricalStorageBinding(ElectricalSource(link.Source), ContactWork(link.Target))).ToArray());
    }
    public static ElectricalSourceId ElectricalSource(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuSensorId CaptureSensor(WorkshopReceiver receiver) => new(checked(PartIdentities(receiver.Id) + 6));

    public static GpuContactTriggerId ContactTrigger(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuOrientationSensorId OrientationSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuContactWorkId ContactWork(GpuBodyId owner) => new(checked(PartIdentities(owner) + 8));

    private static BodyTargetSet Targets(WorkshopConstruction construction) => construction.Puzzle.Id == WorkshopPuzzleId.Free
        ? new(BodyTargetKind.AllDynamic, default)
        : new(BodyTargetKind.NamedBody, construction.Ball?.Id ?? throw new ArgumentException("Missing named puzzle body."));

    private static ulong PartIdentities(GpuBodyId id)
    {
        if (id.Value == 0 || id.Value >= InternalIdentityBase) throw new ArgumentException("Authored body identity exceeds its domain.");
        return checked(InternalIdentityBase + 64 + id.Value * 64);
    }
}
