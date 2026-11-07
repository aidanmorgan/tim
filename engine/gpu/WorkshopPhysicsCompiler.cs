using CuriousContraptions.Abstractions.Signal;
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

    public static (Vector3 Position, Quaternion Rotation) SliderMovingTransform(
        Vector3 ownerPosition, Quaternion ownerRotation,
        ConstraintFrame baseFrame, ConstraintFrame movingFrame, float compression)
    {
        var rJointA = Quaternion.Multiply(ownerRotation, baseFrame.Rotation);
        var rB = Quaternion.Normalize(Quaternion.Multiply(rJointA, Quaternion.Conjugate(movingFrame.Rotation)));
        var axisWorld = Vector3.Transform(Vector3.UnitY, rJointA);
        var localAnchorAInWorld = Vector3.Transform(baseFrame.Position, ownerRotation);
        var localAnchorBInWorld = Vector3.Transform(movingFrame.Position, rB);
        var pB = ownerPosition + (localAnchorAInWorld - axisWorld * compression - localAnchorBInWorld);
        return (pB, rB);
    }

    public static (Vector3 Position, Quaternion Rotation) HingeMovingTransform(
        Vector3 ownerPosition, Quaternion ownerRotation,
        ConstraintFrame baseFrame, ConstraintFrame movingFrame, float initialAngleRad)
    {
        var rJointA = Quaternion.Multiply(ownerRotation, baseFrame.Rotation);
        var rAngle = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, initialAngleRad);
        var rB = Quaternion.Normalize(Quaternion.Multiply(Quaternion.Multiply(rJointA, rAngle), Quaternion.Conjugate(movingFrame.Rotation)));
        var localAnchorAInWorld = Vector3.Transform(baseFrame.Position, ownerRotation);
        var localAnchorBInWorld = Vector3.Transform(movingFrame.Position, rB);
        var pB = ownerPosition + (localAnchorAInWorld - localAnchorBInWorld);
        return (pB, rB);
    }

    public static IPhysicsSceneDeclaration Compile(ICoreServices core, IBundleServices bundle, WorkshopConstruction construction, PhysicsDocumentId document) =>
        new Compilation(core, bundle, construction, document).Build();

    private sealed class Compilation
    {
        private readonly WorkshopConstruction construction;
        private readonly PhysicsDocumentId document;
        private readonly CompiledIdentityMap identities;
        private readonly ISupplyTopology topology;
        private readonly ICoreServices core;
        private readonly IBundleServices bundle;
        private readonly SupplyNodeDeclaration[] supplyNodes;
        private readonly List<RigidBodyDeclaration> bodies = new(16);
        private readonly List<ColliderDeclaration> colliders = new(32);
        private readonly List<ContactMaterialDeclaration> materials = new(32);
        private readonly List<ResidenceSensorDeclaration> sensors = new(4);
        private readonly List<PlanarGuideDeclaration> guides = new(4);
        private readonly List<ContactTriggerDeclaration> triggers = new(4);
        private readonly List<ContactWorkDeclaration> contactWorks = new(4);
        private readonly List<PrismaticConstraintDeclaration> prismatics = new(4);
        private readonly List<PrismaticDriveDeclaration> drives = new(4);
        private readonly List<RevoluteConstraintDeclaration> revolutes = new(4);
        private readonly List<RevoluteDriveDeclaration> revoluteDrives = new(4);
        private readonly List<CompliantSurfaceDeclaration> compliantSurfaces = new(4);
        private readonly List<DrivenSurfaceDeclaration> drivenSurfaces = new();
        private readonly List<ElectricalSourceDeclaration> electricalSources = new(4);
        private readonly List<OrientationSensorDeclaration> orientationSensors = new(4);
        private readonly List<ContactLoadSensorDeclaration> contactLoads = new(4);

        public Compilation(ICoreServices core, IBundleServices bundle, WorkshopConstruction construction, PhysicsDocumentId document)
        {
            construction.Validate(core.Supply, bundle);
            this.core = core; this.bundle = bundle;
            this.construction = construction;
            this.document = document;
            identities = construction.Instances.Identities;
            topology = WorkshopSupplyCompiler.Compile(core.Supply, construction, DeclaredPhysics.Of);
            supplyNodes = topology.Nodes.ToArray();
        }

        public IPhysicsSceneDeclaration Build()
        {
            var plane = identities.Plane;
            var planeMaterial = identities.PlaneMaterial;
            var height = WorkshopInput.Position(WorkshopConstruction.WorkbenchSurface.Value);
            bodies.Add(new(plane, RigidMotionKind.Static, new(0, height.Cell, 0),
                new(0f, height.Local, 0f), CanonicalRotation.Identity, default, default, new(0f), default, new(0f), default));
            // Static surfaces and boxes declare zero rolling resistance; only a ball material carries a coefficient.
            materials.Add(new(planeMaterial, new(1f), new(.1f), new(.3f), new(0f)));
            colliders.Add(new(identities.PlaneCollider, plane, planeMaterial,
                ColliderShapeKind.Plane, RigidLocalPose.Identity, new(0f), default));
            CompileBodies();
            CompileSensors();
            ValidateHollowPlacement();
            var (tensile, ropeGuides, transmissions) = CompileConnections();
            return core.CreatePhysicsScene(document, identities.NextIdentity, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray(), guides.ToArray(), triggers.ToArray(), contactWorks.ToArray(), orientationSensors.ToArray(), electricalSources.OrderBy(source => source.Id.Value).ToArray(),
                WorkshopSupplyCompiler.StorageBindings(topology), prismatics.ToArray(),
                supplyNodes, topology.Edges.ToArray(), contactLoads.ToArray(), drives.ToArray(), tensile.ToArray(), revolutes.ToArray(),
                compliantSurfaces.ToArray(), revoluteDrives.ToArray(),
                ropeGuides: ropeGuides.ToArray(),
                transmissions: transmissions.ToArray(), drivenSurfaces: drivenSurfaces.ToArray());
        }

        private void AddGeometry(GpuBodyId owner, GpuMaterialId material, IReadOnlyList<GpuColliderId> colliderIdentities,
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

        private GpuBodyId ResolvePortBody(GpuBodyId instanceId, CuriousContraptions.Abstractions.PackageBodyIndex bodyIndex)
        {
            if (bodyIndex.Value == 0) return instanceId;
            var partIds = identities[instanceId];
            if (bodyIndex.Value <= partIds.Sliders.Length)
                return partIds.Sliders[bodyIndex.Value - 1].Body;
            var hingeIndex = bodyIndex.Value - 1 - partIds.Sliders.Length;
            if (hingeIndex < partIds.Hinges.Length)
                return partIds.Hinges[hingeIndex].Body;
            throw new ArgumentException($"Invalid port body index {bodyIndex.Value} for {instanceId}");
        }

        private void CompileSliders(IWorkshopInstance instance, CompiledPartIdentities rows, IReadOnlyDictionary<string, float> parameters, IReadOnlyDictionary<ParameterId, float> values, IReadOnlyList<PrismaticSliderDescriptor> sliders)
        {
            for (var i = 0; i < sliders.Count; i++)
            {
                var slider = sliders[i];
                var moving = rows.Sliders[i].Body;
                var compression = slider.Compression.Evaluate(parameters);
                var rA = new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W);
                var pA = new Vector3((instance.Cell.X + instance.Local.X) / 16f, (instance.Cell.Y + instance.Local.Y) / 16f, (instance.Cell.Z + instance.Local.Z) / 16f);
                var (pB, rB) = SliderMovingTransform(pA, rA,
                    new(slider.BaseFrame.Position, slider.BaseFrame.Rotation),
                    new(slider.MovingFrame.Position, slider.MovingFrame.Rotation),
                    compression);
                var x = WorkshopInput.Position(pB.X);
                var y = WorkshopInput.Position(pB.Y);
                var z = WorkshopInput.Position(pB.Z);
                var body = slider.MovingBody;
                var movingMass = body.MassFrame.Evaluate(values);
                movingMass.Validate(KinematicMode.Dynamic);
                bodies.Add(new(moving, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                    CanonicalRotation.Normalize(rB.X, rB.Y, rB.Z, rB.W), default, default, new(movingMass.Mass),
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
            }
        }

        private void CompileHinges(IWorkshopInstance instance, CompiledPartIdentities rows, IReadOnlyDictionary<string, float> parameters, IReadOnlyDictionary<ParameterId, float> values, IReadOnlyList<RevoluteHingeDescriptor> hinges)
        {
            for (var i = 0; i < hinges.Count; i++)
            {
                var hinge = hinges[i];
                var moving = rows.Hinges[i].Body;
                var initialAngleDeg = hinge.InitialAngle.Evaluate(parameters);
                var initialAngleRad = initialAngleDeg * (MathF.PI / 180f);
                var rA = new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W);
                var pA = new Vector3((instance.Cell.X + instance.Local.X) / 16f, (instance.Cell.Y + instance.Local.Y) / 16f, (instance.Cell.Z + instance.Local.Z) / 16f);
                var (pB, rB) = HingeMovingTransform(pA, rA,
                    new(hinge.BaseFrame.Position, hinge.BaseFrame.Rotation),
                    new(hinge.MovingFrame.Position, hinge.MovingFrame.Rotation),
                    initialAngleRad);
                var x = WorkshopInput.Position(pB.X);
                var y = WorkshopInput.Position(pB.Y);
                var z = WorkshopInput.Position(pB.Z);

                var body = hinge.MovingBody;
                var movingMass = body.MassFrame.Evaluate(values);
                movingMass.Validate(KinematicMode.Dynamic);
                bodies.Add(new(moving, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                    CanonicalRotation.Normalize(rB.X, rB.Y, rB.Z, rB.W), default, default, new(movingMass.Mass),
                    new(movingMass.Gravity.X, movingMass.Gravity.Y, movingMass.Gravity.Z), new(movingMass.LinearDrag), MassProperties(movingMass)));
                AddGeometry(moving, rows.Hinges[i].Material, rows.Hinges[i].Colliders, body.Colliders);
                var joint = rows.Hinges[i].Joint;
                revolutes.Add(new(joint, instance.Id, moving,
                    new(hinge.BaseFrame.Position, hinge.BaseFrame.Rotation), new(hinge.MovingFrame.Position, hinge.MovingFrame.Rotation),
                    hinge.LowerLimit, hinge.UpperLimit, hinge.Travel, hinge.Stiffness.Evaluate(parameters), hinge.Damping.Evaluate(parameters),
                    hinge.FrictionTorque.Evaluate(parameters),
                    hinge.BodiesCollide ? ConnectedCollision.Enabled : ConnectedCollision.Disabled));
                if (hinge.Drive is not { } rotary) continue;
                revoluteDrives.Add(new(joint, rotary.TargetSpeed.Evaluate(parameters), rotary.MaxTorque.Evaluate(parameters)));
            }
        }
        private void CompileBodies()
        {
            foreach (var instance in construction.Instances)
            {
                var entry = DeclaredPhysics.Of(instance.PartId);
                if (entry.Footprint.Bodies == 0)
                {
                    ValidateBodylessGeometry(entry);
                    continue;
                }
                var rows = identities[instance.Id];
                var parameters = DeclaredPhysics.Parameters(instance);
                var shapes = bundle.Colliders(entry, parameters);
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
                var hinges = mechanical?.Hinges ?? [];
                if (entry.Footprint.Bodies != 1 + sliders.Count + hinges.Count ||
                    entry.Footprint.MovingBodies != sliders.Count + hinges.Count + (mode == RigidMotionKind.Dynamic ? 1 : 0) ||
                    entry.Footprint.PrismaticConstraints != sliders.Count ||
                    entry.Footprint.RevoluteConstraints != hinges.Count)
                    throw new ArgumentException("Physical declaration disagrees with its compiled footprint.");
                bodies.Add(new(instance.Id, mode, instance.Cell, instance.Local, instance.Rotation, default, default,
                    new(mass.Mass), new(mass.Gravity.X, mass.Gravity.Y, mass.Gravity.Z), new(mass.LinearDrag),
                    mode == RigidMotionKind.Dynamic ? MassProperties(mass) : default, entry.Mechanical?.Payload ?? PayloadPolicy.None));
                AddGeometry(instance.Id, rows.Material, rows.Colliders, shapes);
                CompileSliders(instance, rows, parameters, values, sliders);
                CompileHinges(instance, rows, parameters, values, hinges);
                if (mechanical is not null)
                    for (var index = 0; index < mechanical.DrivenSurfaces.Count; index++)
                    {
                        var surface = mechanical.DrivenSurfaces[index];
                        var carrier = ResolvePortBody(instance.Id, surface.Carrier);
                        var colliderRows = surface.Carrier.Value == 0 ? rows.Colliders :
                            surface.Carrier.Value <= rows.Sliders.Length ? rows.Sliders[surface.Carrier.Value - 1].Colliders :
                            rows.Hinges[surface.Carrier.Value - 1 - rows.Sliders.Length].Colliders;
                        drivenSurfaces.Add(new(rows.DrivenSurfaces[index], carrier, colliderRows[surface.Collider.Value],
                            surface.Face, rows.Hinges[surface.Source.Index].Joint, surface.LocalDirection,
                            surface.TravelPerRadian.Evaluate(parameters)));
                    }
                if (mechanical?.CompliantSurface is { } compliant)
                {
                    compliantSurfaces.Add(new CompliantSurfaceDeclaration(
                        rows.CompliantSurface,
                        instance.Id,
                        compliant.HalfExtents.X,
                        compliant.HalfExtents.Y,
                        compliant.RestHeight,
                        compliant.MaxStroke,
                        compliant.ArrivalDepth,
                        compliant.Tension.Evaluate(parameters),
                        compliant.DampingRatio.Evaluate(parameters)));
                }
            }
        }

        private static void ValidateBodylessGeometry(BundleEntry entry)
        {
            if (entry.Mechanical is not null || entry.Colliders.Count != 0 || entry.ColliderBindings.Count != 0 ||
                entry.Presentation.Bodies.Count != 0 || entry.Footprint.MovingBodies != 0 ||
                entry.Footprint.PrismaticConstraints != 0 || entry.Footprint.RevoluteConstraints != 0)
                throw new ArgumentException("Logical owner declares inconsistent physical geometry.");
        }

        private void CompileSensors()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var orientation in DeclaredPhysics.Of(owner.PartId).Signal?.Nodes.OfType<SignalOrientationDescriptor>() ?? [])
                {
                    var body = bodies.Single(row => row.Id == identities.Body(owner.Id, orientation.Body));
                    orientationSensors.Add(new(identities[owner.Id].Signals[orientation.Id].Orientation, body.Id,
                        body.Rotation, OrientationThreshold.Degrees(orientation.ThresholdDegrees)));
                }
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
                foreach (var source in (DeclaredPhysics.Of(owner.PartId).Electrical?.SupplyGraph?.Nodes ?? []).OfType<SupplySourceDescriptor>())
                    electricalSources.Add(ElectricalSourceFor(identities[owner.Id].Supply[source.Id].Source, owner.Id, source.Store, owner.Parameters));
            foreach (var trigger in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var contact in DeclaredPhysics.Of(trigger.PartId).Signal?.Nodes.OfType<SignalContactDescriptor>() ?? [])
                    triggers.Add(new(identities[trigger.Id].Signals[contact.Id].Trigger, identities.Body(trigger.Id, contact.Body),
                        ResolveTarget(contact.Target, construction.Puzzle, construction.Instances),
                        new(trigger.Parameters[contact.ThresholdParameter])));
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                if (DeclaredPhysics.Of(owner.PartId).Mechanical?.ContactWork is { } work)
                    contactWorks.Add(ContactWorkFor(identities[owner.Id].Work, owner.Id,
                        ResolveTarget(work.Target, construction.Puzzle, construction.Instances),
                        work, owner.Parameters));
            // A declared contact-load sensor compiles its row for every owner, wired or not, in instance order.
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var load in DeclaredPhysics.Of(owner.PartId).Signal?.Nodes.OfType<SignalContactLoadDescriptor>() ?? [])
                    contactLoads.Add(ContactLoadSensorFor(bundle, owner, load, identities));

        }

        private void ValidateHollowPlacement()
        {
            // Only independently authored dynamic partners are checked. A constrained
            // child belongs to its parent's mechanism and retains its declared joint.
            var bodyById = bodies.ToDictionary(body => body.Id);
            var hollowColliders = colliders.Where(c => core.Geometry.IsHollow(c.Shape)).ToArray();
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
                    var solid = new HollowSolidDeclaration(hollowCollider.Shape, centre,
                        Vector3.Transform(Vector3.UnitX, rotation), Vector3.Transform(Vector3.UnitY, rotation),
                        Vector3.Transform(Vector3.UnitZ, rotation), hollowCollider.Radius.Value,
                        hollowCollider.HalfExtents.X, hollowCollider.HalfExtents.Y, hollowCollider.HalfExtents.Z);
                    if (core.Geometry.HollowOverlaps(convex, solid, core.Geometry.PlacementSlop))
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
        }

        private (List<TensileSpanDeclaration> Tensile, List<RopeGuideDeclaration> Guides, List<MechanicalTransmissionDeclaration> Transmissions) CompileConnections()
        {
            var routes = RopeTopology.Compile(construction.Connections, endpoint =>
                DeclaredPorts.Current.Of(construction.Instances.Single(p => p.Id == endpoint.Body).PartId).ToArray()
                    .Single(port => port.Id == endpoint.Socket).AttachmentRole);
            var tensile = new List<TensileSpanDeclaration>();
            for (var routeIndex = 0; routeIndex < routes.Length; routeIndex++)
            {
                var route = routes[routeIndex];
                for (var order = 0; order < route.Spans.Length; order++)
                {
                    var a = route.Attachments[order]; var b = route.Attachments[order + 1];
                    var instanceA = (WorkshopGenericInstance)construction.Instances.Single(p => p.Id == a.Body);
                    var instanceB = (WorkshopGenericInstance)construction.Instances.Single(p => p.Id == b.Body);
                    var portA = DeclaredPorts.Current.Of(instanceA.PartId).ToArray().Single(p => p.Id == a.Socket);
                    var portB = DeclaredPorts.Current.Of(instanceB.PartId).ToArray().Single(p => p.Id == b.Socket);
                    var bodyA = ResolvePortBody(instanceA.Id, portA.Body);
                    var bodyB = ResolvePortBody(instanceB.Id, portB.Body);
                    var isGuide = order < route.Spans.Length - 1 && portB.AttachmentRole == RopeAttachmentRole.Guide;
                    var guideRadius = isGuide ? portB.GuideRadius : 0f;
                    var guideOffset = isGuide ? portB.GuideOffset : Vector3.Zero;
                    tensile.Add(new(new((ulong)tensile.Count + 1), new((ulong)routeIndex + 1), (uint)order,
                        bodyA, DeclaredPorts.Current.LocalPosition(instanceA, a.Socket),
                        bodyB, DeclaredPorts.Current.LocalPosition(instanceB, b.Socket),
                        route.Spans[order].RopeLength!.Value, route.Complete,
                        guideRadius, guideOffset, isGuide ? identities[instanceB.Id].Guides[portB.Id] : default));
                }
            }
            var ropeGuides = new List<RopeGuideDeclaration>();
            foreach (var instance in construction.Instances.OfType<WorkshopGenericInstance>().OrderBy(i => i.Id.Value))
            {
                var ports = DeclaredPorts.Current.Of(instance.PartId);
                foreach (var port in ports.ToArray().OrderBy(port => port.Id))
                {
                    if (port.AttachmentRole == RopeAttachmentRole.Guide)
                    {
                        var guideId = identities[instance.Id].Guides[port.Id];
                        var guideBody = ResolvePortBody(instance.Id, port.Body);
                        ropeGuides.Add(new(guideId, guideBody, port.GuideRadius, port.GuideOffset));
                    }
                }
            }
            var transmissions = new List<MechanicalTransmissionDeclaration>();
            foreach (var link in construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Mechanical))
            {
                var sourceInstance = (WorkshopGenericInstance)construction.Instances.Single(p => p.Id == link.Source);
                var targetInstance = (WorkshopGenericInstance)construction.Instances.Single(p => p.Id == link.Target);
                var sourcePort = DeclaredPorts.Current.Of(sourceInstance.PartId).ToArray().Single(p => p.Id == link.Output);
                var targetPort = DeclaredPorts.Current.Of(targetInstance.PartId).ToArray().Single(p => p.Id == link.Input);

                var sourceCoord = WorkshopConnections.MechanicalCoordinate(sourceInstance.PartId, sourcePort);
                var targetCoord = WorkshopConnections.MechanicalCoordinate(targetInstance.PartId, targetPort);

                var drivingJoint = identities[sourceInstance.Id].Hinges[sourceCoord.Index].Joint;
                var drivenJoint = identities[targetInstance.Id].Hinges[targetCoord.Index].Joint;
                var ratio = sourcePort.CoordinatePerRadian / targetPort.CoordinatePerRadian;

                var transmissionId = new MechanicalTransmissionId((ulong)transmissions.Count + 1);
                transmissions.Add(new(transmissionId, drivingJoint, drivenJoint, ratio));
            }
            return (tensile, ropeGuides, transmissions);
        }

    }

    /// <summary>The finite electrical store a declared Source compiles for one owner, from its own store parameters as f32: the
    /// initial energy is capacity × initial charge. The enable state is the Choice index, 0 Disabled and 1 Enabled.</summary>
    internal static ElectricalSourceDeclaration ElectricalSourceFor(ElectricalSourceId identity, GpuBodyId owner, SupplySourceStoreDescriptor store,
        InstanceParameterCollection parameters) =>
        new(identity, owner, new(parameters[store.Capacity]),
            new(parameters[store.PowerLimit]), parameters[store.InitialCharge],
            parameters[store.Enable] switch
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

    /// <summary>The contact-load row a declared sensor compiles to on its owner's frame: the face is the owner's baked collider
    /// <c>RegionCollider</c> (resolved through the owner's compiled collider identities); the x–z footprint is that box's
    /// f32 half-extents about its offset, widened by the contact distance in x and the depth margin in z; the minimum is the owner's
    /// declared parameter.</summary>
    public static ContactLoadSensorDeclaration ContactLoadSensorFor(IBundleServices bundle, WorkshopGenericInstance owner,
        CuriousContraptions.Abstractions.Signal.SignalContactLoadDescriptor load, CompiledIdentityMap identities)
    {
        var face = bundle.Colliders(DeclaredPhysics.Of(owner.PartId), owner.Parameters.ToBundleValues())[load.RegionCollider];
        var (centre, half) = (face.Offset, face.HalfExtents);
        return new(identities[owner.Id].Signals[load.Id].ContactLoad, identities.Body(owner.Id, load.Frame), identities[owner.Id].Colliders[load.RegionCollider],
            centre.X - half.X - load.ContactDistance, centre.X + half.X + load.ContactDistance,
            centre.Z - half.Z - load.DepthMargin, centre.Z + half.Z + load.DepthMargin,
            load.LocalNormal, load.MinimumAlignment, load.ContactDistance, owner.Parameters[load.MinimumMassParameter],
            [.. load.SupportingColliders.Select(index => identities[owner.Id].Colliders[index])]);
    }

    internal static BodyTargetSet ResolveTarget(TargetSelector selector, WorkshopPuzzle puzzle, WorkshopInstances instances)
    {
        return selector.Kind switch
        {
            TargetSelectorKind.AllDynamic => new(BodyTargetKind.AllDynamic, default),
            TargetSelectorKind.AllPayloads => new(BodyTargetKind.AllPayloads, default),
            TargetSelectorKind.Named => ResolveNamedTarget(selector.NamedTarget, puzzle, instances),
            _ => throw new ArgumentException("Undefined target selector kind.")
        };
    }

    internal static BodyTargetSet ResolveNamedTarget(string? namedTarget, WorkshopPuzzle puzzle, WorkshopInstances instances)
    {
        if (string.IsNullOrWhiteSpace(namedTarget))
            throw new ArgumentException("Named target selector requires a non-empty name.");

        if (ulong.TryParse(namedTarget, out var id) && id > 0)
        {
            var targetId = new GpuBodyId(id);
            if (!instances.Any(i => i.Id == targetId))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.PuzzleSettings, $"Unknown named target body '{id}'.");
            return new(BodyTargetKind.NamedBody, targetId);
        }

        if (namedTarget is "puzzle_target" or "interaction_target")
        {
            if (puzzle.InteractionTarget != default && instances.Any(i => i.Id == puzzle.InteractionTarget))
                return new(BodyTargetKind.NamedBody, puzzle.InteractionTarget);
        }

        throw new WorkshopEditRejectedException(WorkshopEditRejection.PuzzleSettings, $"Unknown named target '{namedTarget}'.");
    }

}
