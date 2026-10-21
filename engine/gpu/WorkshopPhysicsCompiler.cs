using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

/// <summary>Compiles the bundle's declared bodies, geometry and materials; domain bindings remain separate.</summary>
public static class WorkshopPhysicsCompiler
{
    private const ulong InternalIdentityBase = 1UL << 32;
    private const ulong PartIdentityStride = 256;

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
        var topology = SupplyTopology.Create(construction, DeclaredPhysics.Of);
        var supplyNodes = topology.Nodes.ToArray();
        var electricalSources = new List<ElectricalSourceDeclaration>();
        var orientationSensors = new List<OrientationSensorDeclaration>();
        var contactLoads = new List<ContactLoadSensorDeclaration>();
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
        foreach (var instance in construction.Instances)
        {
            var entry = DeclaredPhysics.Of(instance.PartId);
            var first = PartIdentities(instance.Id);
            next = Math.Max(next, first + PartIdentityStride);
            var parameters = DeclaredPhysics.Parameters(instance);
            var shapes = BundleGeometry.Colliders(entry, parameters);
            var mechanical = entry.Mechanical;
            var mass = mechanical?.MassProperties ?? default;
            var mode = (mechanical?.KinematicMode ?? KinematicMode.Static) switch
            {
                KinematicMode.Static => RigidMotionKind.Static,
                KinematicMode.Dynamic => RigidMotionKind.Dynamic,
                _ => throw new ArgumentException("Unsupported authored body mode.")
            };
            var sliders = mechanical?.Sliders ?? [];
            if (entry.Footprint.Bodies != 1 + sliders.Count ||
                entry.Footprint.MovingBodies != sliders.Count + (mode == RigidMotionKind.Dynamic ? 1 : 0) ||
                entry.Footprint.PrismaticConstraints != sliders.Count)
                throw new ArgumentException("Physical declaration disagrees with its compiled footprint.");
            bodies.Add(new(instance.Id, mode, instance.Cell, instance.Local, instance.Rotation, default, default,
                new((Half)mass.Mass), new((Half)mass.Gravity.X, (Half)mass.Gravity.Y, (Half)mass.Gravity.Z), new((Half)mass.LinearDrag)));
            AddGeometry(instance.Id, first, first + (sliders.Count != 0 && shapes.Count > 1 ? 6UL : 1UL), shapes);
            for (var i = 0; i < sliders.Count; i++)
            {
                var slider = sliders[i];
                var identity = first + (ulong)i * 16;
                var moving = new GpuBodyId(identity + 2);
                var compression = slider.Compression.Evaluate(parameters);
                var offset = slider.BaseFrame.Position - slider.MovingFrame.Position - Vector3.UnitY * compression;
                var rotation = new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W);
                var displacement = Vector3.Transform(offset, rotation);
                var x = WorkshopInput.Position((instance.Cell.X + instance.Local.X) / 16f + displacement.X);
                var y = WorkshopInput.Position((instance.Cell.Y + instance.Local.Y) / 16f + displacement.Y);
                var z = WorkshopInput.Position((instance.Cell.Z + instance.Local.Z) / 16f + displacement.Z);
                var body = slider.MovingBody;
                bodies.Add(new(moving, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                    instance.Rotation, default, default, new((Half)body.Mass),
                    new((Half)body.Gravity.X, (Half)body.Gravity.Y, (Half)body.Gravity.Z), new((Half)body.LinearDrag)));
                AddGeometry(moving, identity + 3, identity + 4, body.Colliders);
                prismatics.Add(new(new(identity + 5), instance.Id, moving,
                    new(slider.BaseFrame.Position, slider.BaseFrame.Rotation), new(slider.MovingFrame.Position, slider.MovingFrame.Rotation),
                    slider.LowerLimit, slider.UpperLimit, slider.Stiffness.Evaluate(parameters), slider.Damping.Evaluate(parameters),
                    slider.BodiesCollide ? ConnectedCollision.Enabled : ConnectedCollision.Disabled));
            }
            if (instance.PartId == PartId.PoweredGate)
            {
                var slot=Array.FindIndex(supplyNodes,node=>node.Owner==instance.Id);
                if(slot<0) throw new ArgumentException("Powered actuator requires its declared electrical input.");
                supplyNodes[slot]=supplyNodes[slot] with {Consumer=new(first+5)};
            }
        }
        foreach (var domino in construction.Instances.OfType<WorkshopDomino>())
            if (construction.Connections.HasSource(domino.Id))
                orientationSensors.Add(new(OrientationSensor(domino.Id), domino.Id, domino.Rotation, WorkshopDomino.ActivationThreshold));
        foreach (var receiver in construction.Instances.OfType<WorkshopReceiver>())
        {
            var first = PartIdentities(receiver.Id);
            var targetIndex = 0UL;
            foreach (var target in construction.Instances.OfType<WorkshopBall>().OrderBy(b => b.Id.Value))
            {
                var sensorId = !construction.Puzzle.Id.IsAuthored ? first + 16 + targetIndex * 2 : first + 6;
                sensors.Add(new(new(sensorId), receiver.Id, target.Id, RigidLocalPose.Identity,
                    new((Half)(-.66), (Half)(-.4), (Half)(-.66)),
                    new((Half).66, (Half)((Half).45 + receiver.Capture.Margin.Value), (Half).66),
                    receiver.Capture.SpeedLimit, receiver.Capture.Dwell, receiver.Capture.Participation));
                var region = receiver.ForceRegion;
                guides.Add(new(new(sensorId + 1), receiver.Id, target.Id, RigidLocalPose.Identity,
                    region.Minimum, region.Maximum, region.SupportHeight, region.SupportMargin, region.MaximumAcceleration));
                targetIndex++;
            }
        }
        foreach (var battery in construction.Instances.OfType<WorkshopBattery>())
            electricalSources.Add(new(ElectricalSource(battery.Id), battery.Id, battery.Settings.Capacity,
                battery.Settings.MaximumPower, battery.Settings.InitialFraction, battery.Settings.Enabled));
        foreach (var trigger in construction.Instances.OfType<WorkshopGenericInstance>())
            if (DeclaredPhysics.Of(trigger.PartId).Signal?.Contact is { } contact &&
                (!construction.Puzzle.Id.IsAuthored || construction.Ball.HasValue))
                triggers.Add(new(ContactTrigger(trigger.Id), trigger.Id, Targets(construction),
                    new(trigger.Parameters[new ParameterId(contact.ThresholdParameter)])));
        foreach (var bumper in construction.Instances.OfType<WorkshopBumper>())
            if (!construction.Puzzle.Id.IsAuthored || construction.Ball.HasValue)
                contactWorks.Add(new(ContactWork(bumper.Id), bumper.Id, Targets(construction), bumper.Work.Strength,
                    bumper.Work.Preload, new(.05f), 72));
        foreach (var plate in construction.Instances.OfType<WorkshopPressurePlate>())
            contactLoads.Add(new(ContactLoadSensor(plate.Id), plate.Id, new(PartIdentities(plate.Id) + 2),
                -.9f - WorkshopPressurePlate.ContactDistance, .9f + WorkshopPressurePlate.ContactDistance,
                -.9f - WorkshopPressurePlate.FootprintDepthMargin, .9f + WorkshopPressurePlate.FootprintDepthMargin,
                Vector3.UnitY, WorkshopPressurePlate.MinimumAlignment, WorkshopPressurePlate.ContactDistance, plate.MinimumMass));
        var contactWorkOwners = contactWorks.Select(cw => cw.Owner).ToHashSet();
        foreach(var link in construction.Connections.Where(link=>link.Domain==WorkshopConnectionDomain.Electrical && contactWorkOwners.Contains(link.Target)))
        {
            var sources=topology.UpstreamSources(link.Target);
            if(sources.Length!=1 || sources[0]!=link.Source)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied,
                    "Finite-work storage requires one direct declared source; switched storage routing is unsupported.");
        }


        void AddGeometry(GpuBodyId owner, ulong materialIdentity, ulong colliderIdentity,
            IReadOnlyList<CuriousContraptions.Abstractions.ColliderDescriptor> shapes)
        {
            var surface = shapes[0].Material;
            if (shapes.Any(shape => shape.Material != surface))
                throw new ArgumentException("One compiled body must declare one contact material.");
            var material = new GpuMaterialId(materialIdentity);
            materials.Add(new(material, new((Half)surface.Restitution), new((Half)surface.BounceThreshold),
                new((Half)surface.Friction), new((Half)surface.RollingResistance)));
            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = DeclaredPhysics.Admit(shapes[i]);
                colliders.Add(new(new(colliderIdentity + (ulong)i), owner, material, shape.ShapeKind,
                    shape.LocalPose, new(shape.Radius), shape.HalfExtents));
            }
        }
        return new(document, next, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray(), guides.ToArray(), triggers.ToArray(), contactWorks.ToArray(), orientationSensors.ToArray(), electricalSources.OrderBy(source => source.Id.Value).ToArray(),
            construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Electrical && contactWorkOwners.Contains(link.Target))
                .Select(link => new ElectricalStorageBinding(ElectricalSource(link.Source), ContactWork(link.Target))).ToArray(), prismatics.ToArray(),
            supplyNodes, topology.Edges, contactLoads.ToArray());
    }
    public static bool IsSecondaryBodyIdentity(GpuBodyId owner, GpuBodyId body)
    {
        return body == SpringboardPlate(owner) || body == GateBlade(owner);
    }

    public static GpuBodyId SpringboardPlate(GpuBodyId owner) => new(checked(PartIdentities(owner) + 2));

    public static GpuBodyId GateBlade(GpuBodyId owner) => new(checked(PartIdentities(owner) + 2));

    public static ElectricalSourceId ElectricalSource(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuSensorId CaptureSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuContactTriggerId ContactTrigger(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuOrientationSensorId OrientationSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuPassageSensorId PassageSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuContactLoadSensorId ContactLoadSensor(GpuBodyId owner) => new(checked(PartIdentities(owner) + 6));

    public static GpuContactWorkId ContactWork(GpuBodyId owner) => new(checked(PartIdentities(owner) + 8));

    private static BodyTargetSet Targets(WorkshopConstruction construction) => !construction.Puzzle.Id.IsAuthored
        ? new(BodyTargetKind.AllDynamic, default)
        : new(BodyTargetKind.NamedBody, construction.Ball?.Id ?? throw new ArgumentException("Missing named puzzle body."));

    internal static ulong PartIdentities(GpuBodyId id)
    {
        if (id.Value == 0 || id.Value >= InternalIdentityBase)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.IdentityExhausted, "Authored body identity exceeds its domain.");
        return checked(InternalIdentityBase + PartIdentityStride + id.Value * PartIdentityStride);
    }
}
