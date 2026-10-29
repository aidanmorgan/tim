using CuriousContraptions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

/// <summary>An independently authored payload intersects a hollow collider at construction.</summary>

/// <summary>Compiles the bundle's declared bodies, geometry and materials; domain bindings remain separate.</summary>
public static class WorkshopPhysicsCompiler
{
    private static RigidMassProperties MassProperties(RigidBodyMassProperties mass) => new(
        new(mass.CentreOfMass.X, mass.CentreOfMass.Y, mass.CentreOfMass.Z),
        new(mass.PrincipalFrame.X, mass.PrincipalFrame.Y, mass.PrincipalFrame.Z, mass.PrincipalFrame.W),
        new(mass.PrincipalMoments.X), new(mass.PrincipalMoments.Y), new(mass.PrincipalMoments.Z));


    public static PhysicsSceneDeclaration Compile(WorkshopConstruction construction, PhysicsDocumentId document)
    {
        construction.Validate();
        var identities = construction.Instances.Identities;
        var bodies = new List<RigidBodyDeclaration>(16);
        var colliders = new List<ColliderDeclaration>(32);
        var materials = new List<ContactMaterialDeclaration>(32);
        var sensors = new List<ResidenceSensorDeclaration>(4);
        var guides = new List<PlanarGuideDeclaration>(4);
        var triggers = new List<ContactTriggerDeclaration>(4);
        var contactWorks = new List<ContactWorkDeclaration>(4);
        var prismatics = new List<PrismaticConstraintDeclaration>(4);
        var drives = new List<PrismaticDriveDeclaration>(4);
        var topology = WorkshopSupplyCompiler.Compile(construction, DeclaredPhysics.Of);
        var supplyNodes = topology.Nodes.ToArray();
        var electricalSources = new List<ElectricalSourceDeclaration>(4);
        var orientationSensors = new List<OrientationSensorDeclaration>(4);
        var contactLoads = new List<ContactLoadSensorDeclaration>(4);
        var plane = identities.Plane;
        var planeMaterial = identities.PlaneMaterial;
        var height = WorkshopInput.Position(WorkshopConstruction.WorkbenchSurface.Value);
        bodies.Add(new(plane, RigidMotionKind.Static, new(0, height.Cell, 0),
            new(0f, height.Local, 0f), CanonicalRotation.Identity, default, default, new(0f), default, new(0f), default));
        // Static surfaces and boxes declare zero rolling resistance; only a ball material carries a coefficient.
        materials.Add(new(planeMaterial, new(1f), new(.1f), new(.3f), new(0f)));
        colliders.Add(new(identities.PlaneCollider, plane, planeMaterial,
            ColliderShapeKind.Plane, RigidLocalPose.Identity, new(0f), default));
        foreach (var instance in construction.Instances)
        {
            var entry = DeclaredPhysics.Of(instance.PartId);
            var rows = identities[instance.Id];
            var parameters = DeclaredPhysics.Parameters(instance);
            var shapes = BundleGeometry.Colliders(entry, parameters);
            var mechanical = entry.Mechanical;
            var values = parameters.ToDictionary(pair => new ParameterId(pair.Key), pair => pair.Value);
            var mass = mechanical is { } declared ? declared.MassFrame.Evaluate(values) : default;
            var mode = (mechanical?.KinematicMode ?? KinematicMode.Static) switch
            {
                KinematicMode.Static => RigidMotionKind.Static,
                KinematicMode.Dynamic => RigidMotionKind.Dynamic,
                _ => throw new ArgumentException("Unsupported authored body mode.")
            };
            if (mechanical is not null) mass.Validate(mechanical.KinematicMode);
            var sliders = mechanical?.Sliders ?? [];
            if (entry.Footprint.Bodies != 1 + sliders.Count ||
                entry.Footprint.MovingBodies != sliders.Count + (mode == RigidMotionKind.Dynamic ? 1 : 0) ||
                entry.Footprint.PrismaticConstraints != sliders.Count)
                throw new ArgumentException("Physical declaration disagrees with its compiled footprint.");
            bodies.Add(new(instance.Id, mode, instance.Cell, instance.Local, instance.Rotation, default, default,
                new(mass.Mass), new(mass.Gravity.X, mass.Gravity.Y, mass.Gravity.Z), new(mass.LinearDrag),
                mode == RigidMotionKind.Dynamic ? MassProperties(mass) : default, entry.Mechanical?.Payload ?? PayloadPolicy.None));
            AddGeometry(instance.Id, rows.Material, rows.Colliders, shapes);
            for (var i = 0; i < sliders.Count; i++)
            {
                var slider = sliders[i];
                var moving = rows.Sliders[i].Body;
                var compression = slider.Compression.Evaluate(parameters);
                var offset = slider.BaseFrame.Position - slider.MovingFrame.Position - Vector3.UnitY * compression;
                var rotation = new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W);
                var displacement = Vector3.Transform(offset, rotation);
                var x = WorkshopInput.Position((instance.Cell.X + instance.Local.X) / 16f + displacement.X);
                var y = WorkshopInput.Position((instance.Cell.Y + instance.Local.Y) / 16f + displacement.Y);
                var z = WorkshopInput.Position((instance.Cell.Z + instance.Local.Z) / 16f + displacement.Z);
                var body = slider.MovingBody;
                var movingMass = body.MassFrame.Evaluate(values);
                movingMass.Validate(KinematicMode.Dynamic);
                bodies.Add(new(moving, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                    instance.Rotation, default, default, new(movingMass.Mass),
                    new(movingMass.Gravity.X, movingMass.Gravity.Y, movingMass.Gravity.Z), new(movingMass.LinearDrag), MassProperties(movingMass)));
                AddGeometry(moving, rows.Sliders[i].Material, rows.Sliders[i].Colliders, body.Colliders);
                var joint = rows.Sliders[i].Joint;
                prismatics.Add(new(joint, instance.Id, moving,
                    new(slider.BaseFrame.Position, slider.BaseFrame.Rotation), new(slider.MovingFrame.Position, slider.MovingFrame.Rotation),
                    slider.LowerLimit, slider.UpperLimit, slider.Stiffness.Evaluate(parameters), slider.Damping.Evaluate(parameters),
                    slider.BodiesCollide ? ConnectedCollision.Enabled : ConnectedCollision.Disabled));
                if (slider.Drive is not { } drive) continue;
                drives.Add(new(joint, drive.Target.Evaluate(parameters), drive.MaxSpeed.Evaluate(parameters),
                    drive.StoppingAcceleration.Evaluate(parameters), drive.MaxEffort.Evaluate(parameters), drive.MaxPower.Evaluate(parameters)));
                // The declared drive is enabled by the owner's PowerIn route: its consumer node drives this slider.
                var slot = Array.FindIndex(supplyNodes, node => node.Owner == instance.Id && node.Kind == SupplyNodeKind.Consumer);
                if (slot < 0) throw new ArgumentException("A supplied drive requires its owner's declared electrical input.");
                supplyNodes[slot] = supplyNodes[slot] with { Consumers = supplyNodes[slot].Consumers.IsDefault ? [joint] : supplyNodes[slot].Consumers.Add(joint) };
            }
        }
        // A declared orientation threshold compiles its sensor only while its owner is wired, in instance order.
        foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
            if (DeclaredPhysics.Of(owner.PartId).Signal?.Orientation is { } orientation && construction.Connections.HasSource(owner.Id))
                orientationSensors.Add(OrientationSensorFor(owner, orientation, identities));
        foreach (var frame in construction.Instances.OfType<WorkshopGenericInstance>())
        {
            if (DeclaredPhysics.Of(frame.PartId).Mechanical?.Capture is not { } capture) continue;
            var parameters = frame.Parameters.ToBundleValues();
            // The declared f32 capture and guide values compile exactly; the upper bound is Y + margin with one f32 rounding.
            var margin = capture.UpperMargin.Evaluate(parameters);
            var speed = new LinearSpeed(capture.SpeedLimit.Evaluate(parameters));
            var dwell = new DurationSeconds(capture.Dwell.Evaluate(parameters));
            var participation = parameters[capture.ParticipationParameter] switch
            {
                0f => SensorParticipation.Disabled,
                1f => SensorParticipation.Enabled,
                _ => throw new ArgumentException("Undefined declared capture participation.")
            };
            var captureRows = identities[frame.Id].Capture;
            sensors.Add(new(captureRows.Sensor, frame.Id, new(BodyTargetKind.AllPayloads,default), RigidLocalPose.Identity,
                new(capture.Minimum.X,capture.Minimum.Y,capture.Minimum.Z),
                new(capture.Maximum.X,capture.Maximum.Y+margin,capture.Maximum.Z),
                speed,dwell,participation));
            var guide = capture.Guide;
            guides.Add(new(captureRows.Guide,frame.Id,new(BodyTargetKind.AllPayloads,default),RigidLocalPose.Identity,
                new(guide.Minimum.X,guide.Minimum.Y,guide.Minimum.Z),
                new(guide.Maximum.X,guide.Maximum.Y,guide.Maximum.Z),
                new(guide.SupportHeight),new(guide.SupportMargin.Evaluate(parameters)),
                new(guide.MaximumAcceleration.Evaluate(parameters))));
        }
        foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
            if (DeclaredPhysics.SourceStore(owner.PartId) is { } store)
                electricalSources.Add(ElectricalSourceFor(identities[owner.Id].Source, owner.Id, store, owner.Parameters));
        foreach (var trigger in construction.Instances.OfType<WorkshopGenericInstance>())
            if (DeclaredPhysics.Of(trigger.PartId).Signal?.Contact is { } contact &&
                (!construction.Puzzle.Id.IsAuthored || FirstPayload(construction) is not null))
                triggers.Add(new(identities[trigger.Id].Trigger, trigger.Id, Targets(construction),
                    new(trigger.Parameters[new ParameterId(contact.ThresholdParameter)])));
        foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
            if (DeclaredPhysics.Of(owner.PartId).Mechanical?.ContactWork is { } work &&
                (!construction.Puzzle.Id.IsAuthored || FirstPayload(construction) is not null))
                contactWorks.Add(ContactWorkFor(identities[owner.Id].Work, owner.Id, Targets(construction), work, owner.Parameters));
        // A declared contact-load sensor compiles its row for every owner, wired or not, in instance order.
        foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
            if (DeclaredPhysics.Of(owner.PartId).Signal?.ContactLoad is { } load)
                contactLoads.Add(ContactLoadSensorFor(owner, load, identities));
        var contactWorkOwners = contactWorks.Select(cw => cw.Owner).ToHashSet();
        foreach(var link in construction.Connections.Where(link=>link.Domain==WorkshopConnectionDomain.Electrical && contactWorkOwners.Contains(link.Target)))
        {
            var sources=topology.UpstreamSources(link.Target);
            if(sources.Length!=1 || sources[0]!=link.Source)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied,
                    "Finite-work storage requires one direct declared source; switched storage routing is unsupported.");
        }


        void AddGeometry(GpuBodyId owner, GpuMaterialId material, IReadOnlyList<GpuColliderId> colliderIdentities,
            IReadOnlyList<CuriousContraptions.Abstractions.ColliderDescriptor> shapes)
        {
            var surface = shapes[0].Material;
            if (shapes.Any(shape => shape.Material != surface))
                throw new ArgumentException("One compiled body must declare one contact material.");
            materials.Add(new(material, new(surface.Restitution), new(surface.BounceThreshold),
                new(surface.Friction), new(surface.RollingResistance)));
            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = DeclaredPhysics.Admit(shapes[i]);
                colliders.Add(new(colliderIdentities[i], owner, material, shape.ShapeKind,
                    shape.LocalPose, new(shape.Radius), shape.HalfExtents));
            }
        }

        // Only independently authored dynamic partners are checked. A constrained
        // child belongs to its parent's mechanism and retains its declared joint.
        var bodyById = bodies.ToDictionary(body => body.Id);
        var hollowColliders = colliders.Where(c => HollowSolid.IsHollow(c.Shape)).ToArray();
        foreach (var partner in colliders.Where(c => bodyById[c.Body].Motion == RigidMotionKind.Dynamic))
        {
            var (partnerCentre, partnerTurn) = WorldPose(partner);
            var convex = new HollowPartner(partner.Shape, partnerCentre,
                Vector3.Transform(Vector3.UnitX, partnerTurn), Vector3.Transform(Vector3.UnitY, partnerTurn),
                Vector3.Transform(Vector3.UnitZ, partnerTurn), partner.Radius.Value,
                partner.HalfExtents.X, partner.HalfExtents.Y, partner.HalfExtents.Z);
            foreach (var hollowCollider in hollowColliders)
            {
                if (hollowCollider.Body == partner.Body || identities.IsChild(hollowCollider.Body, partner.Body)) continue;
                var (centre, rotation) = WorldPose(hollowCollider);
                var solid = new HollowSolid(hollowCollider.Shape, centre,
                    Vector3.Transform(Vector3.UnitX, rotation), Vector3.Transform(Vector3.UnitY, rotation),
                    Vector3.Transform(Vector3.UnitZ, rotation), hollowCollider.Radius.Value,
                    hollowCollider.HalfExtents.X, hollowCollider.HalfExtents.Y, hollowCollider.HalfExtents.Z);
                if (HollowContactQuery.Overlaps(convex, solid, HollowContactQuery.PlacementSlop))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InitialOverlap, "A dynamic payload initially overlaps a hollow wall.");
            }
        }

        (Vector3 Centre, Quaternion Rotation) WorldPose(ColliderDeclaration collider)
        {
            var body = bodyById[collider.Body];
            var bodyRotation = new Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W);
            var local = collider.Pose;
            var localRotation = new Quaternion(local.Rotation.X, local.Rotation.Y, local.Rotation.Z, local.Rotation.W);
            var centre = new Vector3((body.Cell.X + body.Local.X) / 16f, (body.Cell.Y + body.Local.Y) / 16f,
                (body.Cell.Z + body.Local.Z) / 16f);
            centre += Vector3.Transform(new Vector3(local.Translation.X, local.Translation.Y, local.Translation.Z), bodyRotation);
            return (centre, Quaternion.Normalize(bodyRotation * localRotation));
        }

        return new(document, identities.NextIdentity, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray(), guides.ToArray(), triggers.ToArray(), contactWorks.ToArray(), orientationSensors.ToArray(), electricalSources.OrderBy(source => source.Id.Value).ToArray(),
            construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Electrical && contactWorkOwners.Contains(link.Target))
                .Select(link => new ElectricalStorageBinding(identities[link.Source].Source, identities[link.Target].Work)).ToArray(), prismatics.ToArray(),
            supplyNodes, topology.Edges, contactLoads.ToArray(), drives.ToArray());
    }
    /// <summary>The finite electrical store a declared Source compiles for one owner, from its own store parameters as f32: the
    /// initial energy is capacity × initial charge. The enable state is the Choice index, 0 Disabled and 1 Enabled.</summary>
    internal static ElectricalSourceDeclaration ElectricalSourceFor(ElectricalSourceId identity, GpuBodyId owner, SupplySourceStoreDescriptor store,
        InstanceParameterCollection parameters) =>
        new(identity, owner, new(parameters[new ParameterId(store.Capacity)]),
            new(parameters[new ParameterId(store.PowerLimit)]), parameters[new ParameterId(store.InitialCharge)],
            parameters[new ParameterId(store.Enable)] switch
            {
                0f => ElectricalEnable.Disabled,
                1f => ElectricalEnable.Enabled,
                _ => throw new ArgumentException("Undefined declared electrical enable state.")
            });

    /// <summary>The finite store a declared contact-work capability compiles for one owner: its target speed is the strength
    /// parameter, its preload the reference mass's kinetic energy at that speed, (½·m)·s·s in f32.</summary>
    internal static ContactWorkDeclaration ContactWorkFor(GpuContactWorkId identity, GpuBodyId owner, BodyTargetSet targets,
        ContactWorkDescriptor work, InstanceParameterCollection parameters)
    {
        var strength = parameters[new ParameterId(work.StrengthParameter)];
        return new(identity, owner, targets, new(strength), new(.5f * work.ReferenceMass * strength * strength),
            new(work.Threshold), work.CooldownPhysicalSteps);
    }

    /// <summary>The orientation sensor a declared threshold compiles to on its owner's body, measured from the owner's admitted
    /// rotation. The declared f32 degrees compile to the f32 cosine of half the angle.</summary>
    public static OrientationSensorDeclaration OrientationSensorFor(IWorkshopInstance owner,
        CuriousContraptions.Abstractions.Signal.SignalOrientationDescriptor orientation, CompiledIdentityMap identities)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return new(identities[owner.Id].Orientation, owner.Id, owner.Rotation, OrientationThreshold.Degrees(orientation.ThresholdDegrees));
    }

    /// <summary>The contact-load row a declared sensor compiles to on its owner's frame: the face is the owner's baked collider
    /// <c>RegionCollider</c> (resolved through the owner's compiled collider identities); the x–z footprint is that box's
    /// f32 half-extents about its offset, widened by the contact distance in x and the depth margin in z; the minimum is the owner's
    /// declared parameter.</summary>
    public static ContactLoadSensorDeclaration ContactLoadSensorFor(WorkshopGenericInstance owner,
        CuriousContraptions.Abstractions.Signal.SignalContactLoadDescriptor load, CompiledIdentityMap identities)
    {
        var face = BundleGeometry.Colliders(DeclaredPhysics.Of(owner.PartId), owner.Parameters.ToBundleValues())[load.RegionCollider];
        var (centre, half) = (face.Offset, face.HalfExtents);
        return new(identities[owner.Id].ContactLoad, owner.Id, identities[owner.Id].Colliders[load.RegionCollider],
            centre.X - half.X - load.ContactDistance, centre.X + half.X + load.ContactDistance,
            centre.Z - half.Z - load.DepthMargin, centre.Z + half.Z + load.DepthMargin,
            load.LocalNormal, load.MinimumAlignment, load.ContactDistance, owner.Parameters[new ParameterId(load.MinimumMassParameter)],
            [.. load.SupportingColliders.Select(index => identities[owner.Id].Colliders[index])]);
    }

    private static BodyTargetSet Targets(WorkshopConstruction construction) => !construction.Puzzle.Id.IsAuthored
        ? new(BodyTargetKind.AllDynamic, default)
        : new(BodyTargetKind.NamedBody, FirstPayload(construction)?.Id ?? throw new ArgumentException("Missing named puzzle body."));

    /// <summary>The lesson's named body: the first payload in instance order.</summary>
    private static IWorkshopInstance? FirstPayload(WorkshopConstruction construction) =>
        construction.Instances.FirstOrDefault(instance => DeclaredPhysics.IsPayload(instance.PartId));

}
