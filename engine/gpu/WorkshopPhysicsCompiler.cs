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
public static partial class WorkshopPhysicsCompiler
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

    private sealed partial class Compilation
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
        private readonly List<EnergyStoreDeclaration> energyStores = new(4);
        private readonly List<ChamberRegionDeclaration> chamberRegions = new();
        private readonly List<FiniteWorkActuatorDeclaration> finiteWorkActuators = new();
        private readonly List<PrismaticConstraintDeclaration> prismatics = new(4);
        private readonly List<PrismaticDriveDeclaration> drives = new(4);
        private readonly List<RevoluteConstraintDeclaration> revolutes = new(4);
        private readonly List<RevoluteDriveDeclaration> revoluteDrives = new(4);
        private readonly List<CompliantSurfaceDeclaration> compliantSurfaces = new(4);
        private readonly List<DrivenSurfaceDeclaration> drivenSurfaces = new();
        private readonly List<MechanicalTransmissionDeclaration> internalTransmissions = new();
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
            bodies.Add(new(plane, plane, RigidMotionKind.Static, new(0, height.Cell, 0),
                new(0f, height.Local, 0f), CanonicalRotation.Identity, default, default, new(0f), default, new(0f), default));
            // Static surfaces and boxes declare zero rolling resistance; only a ball material carries a coefficient.
            materials.Add(new(planeMaterial, new(1f), new(.1f), new(.3f), new(0f)));
            colliders.Add(new(identities.PlaneCollider, plane, planeMaterial,
                ColliderShapeKind.Plane, RigidLocalPose.Identity, new(0f), default));
            CompileBodies();
            CompileGas();
            ValidatePreparedGasPlacement();
            CompileSensors();
            CompileAirflow();
            CompileCoordinateTravel();
            ValidateHollowPlacement();
            var (tensile, ropeGuides, transmissions) = CompileConnections();
            return core.CreatePhysicsScene(document, identities.NextIdentity, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray(), guides.ToArray(), triggers.ToArray(), contactWorks.ToArray(), orientationSensors.ToArray(), electricalSources.OrderBy(source => source.Id.Value).ToArray(),
                WorkshopSupplyCompiler.StorageBindings(topology), prismatics.ToArray(),
                supplyNodes, topology.Edges.ToArray(), contactLoads.ToArray(), drives.ToArray(), tensile.ToArray(), revolutes.ToArray(),
                compliantSurfaces.ToArray(), revoluteDrives.ToArray(),
                ropeGuides: ropeGuides.ToArray(),
                transmissions: transmissions.ToArray(), drivenSurfaces: drivenSurfaces.ToArray(), energyStores: energyStores.ToArray(),
                chamberRegions: chamberRegions.ToArray(), finiteWorkActuators: finiteWorkActuators.ToArray(),
                gasStores: gasStores.ToArray(), gasShapes: gasShapes.ToArray(), inventoryMasses: inventoryMasses.ToArray(),
                ambientLoads: ambientLoads.ToArray(), integrityObservers: integrityObservers.ToArray(),
                airflowSources: airflowSources.ToArray(), airflowReceivers: airflowReceivers.ToArray(),
                rotaryAirflowCaptures: rotaryAirflowCaptures.ToArray(), coordinateTravelObservers: coordinateTravelObservers.ToArray());
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
                    shape.LocalPose, new(shape.Radius), shape.HalfExtents) { Participation = shape.Participation });
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
                var movingMass = body.MassFrame.Evaluate(values, GasMassReferences.Resolve(new(1 + i),
                    DeclaredPhysics.Of(instance.PartId).Mechanical!.GasStores, values));
                movingMass.Validate(KinematicMode.Dynamic);
                bodies.Add(new(moving, instance.Id, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                    CanonicalRotation.Normalize(rB.X, rB.Y, rB.Z, rB.W), default, default, new(movingMass.Mass),
                    new(movingMass.Gravity.X, movingMass.Gravity.Y, movingMass.Gravity.Z), new(movingMass.LinearDrag), MassProperties(movingMass)));
                AddGeometry(moving, rows.Sliders[i].Material, rows.Sliders[i].Colliders, bundle.BodyColliders(DeclaredPhysics.Of(instance.PartId), new(1 + i), parameters));
                var joint = rows.Sliders[i].Joint;
                prismatics.Add(new(joint, instance.Id, moving,
                    new(slider.BaseFrame.Position, slider.BaseFrame.Rotation), new(slider.MovingFrame.Position, slider.MovingFrame.Rotation),
                    slider.LowerLimit.Evaluate(parameters), slider.UpperLimit.Evaluate(parameters), slider.Stiffness.Evaluate(parameters), slider.Damping.Evaluate(parameters),
                    slider.BodiesCollide ? ConnectedCollision.Enabled : ConnectedCollision.Disabled));
                if (slider.Drive is not { } drive) continue;
                drives.Add(new(joint, drive.Target.Evaluate(parameters), drive.MaxSpeed.Evaluate(parameters),
                    drive.StoppingAcceleration.Evaluate(parameters), drive.MaxEffort.Evaluate(parameters), drive.MaxPower.Evaluate(parameters),
                    drive.Selection, drive.Inactive,
                    drive.Positive is { } positive ? rows.Supply[positive].Node : default,
                    drive.Negative is { } negative ? rows.Supply[negative].Node : default));
            }
        }

        private void CompileHinges(IWorkshopInstance instance, CompiledPartIdentities rows, IReadOnlyDictionary<string, float> parameters, IReadOnlyDictionary<ParameterId, float> values, IReadOnlyList<RevoluteHingeDescriptor> hinges)
        {
            foreach (var i in HingeSupportOrder.Resolve(rows.Sliders.Length, hinges))
            {
                var hinge = hinges[i];
                var moving = rows.Hinges[i].Body;
                var initialAngleDeg = hinge.InitialAngle.Evaluate(parameters);
                var initialAngleRad = initialAngleDeg * (MathF.PI / 180f);
                var support = bodies.Single(body => body.Id == identities.Body(instance.Id, hinge.Support));
                var rA = new Quaternion(support.Rotation.X, support.Rotation.Y, support.Rotation.Z, support.Rotation.W);
                var pA = new Vector3((support.Cell.X + support.Local.X) / 16f, (support.Cell.Y + support.Local.Y) / 16f, (support.Cell.Z + support.Local.Z) / 16f);
                var (pB, rB) = HingeMovingTransform(pA, rA,
                    new(hinge.BaseFrame.Position, hinge.BaseFrame.Rotation),
                    new(hinge.MovingFrame.Position, hinge.MovingFrame.Rotation),
                    initialAngleRad);
                var x = WorkshopInput.Position(pB.X);
                var y = WorkshopInput.Position(pB.Y);
                var z = WorkshopInput.Position(pB.Z);

                var body = hinge.MovingBody;
                var movingMass = body.MassFrame.Evaluate(values, GasMassReferences.Resolve(new(1 + rows.Sliders.Length + i),
                    DeclaredPhysics.Of(instance.PartId).Mechanical!.GasStores, values));
                movingMass.Validate(KinematicMode.Dynamic);
                bodies.Add(new(moving, instance.Id, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
                    CanonicalRotation.Normalize(rB.X, rB.Y, rB.Z, rB.W), default, default, new(movingMass.Mass),
                    new(movingMass.Gravity.X, movingMass.Gravity.Y, movingMass.Gravity.Z), new(movingMass.LinearDrag), MassProperties(movingMass)));
                AddGeometry(moving, rows.Hinges[i].Material, rows.Hinges[i].Colliders, bundle.BodyColliders(DeclaredPhysics.Of(instance.PartId), new(1 + rows.Sliders.Length + i), parameters));
                var joint = rows.Hinges[i].Joint;
                revolutes.Add(new(joint, support.Id, moving,
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
                var mass = mechanical is { } declared ? declared.MassFrame.Evaluate(values,
                    GasMassReferences.Resolve(new(0), declared.GasStores, values)) : default;
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
                bodies.Add(new(instance.Id, instance.Id, mode, instance.Cell, instance.Local, instance.Rotation, default, default,
                    new(mass.Mass), new(mass.Gravity.X, mass.Gravity.Y, mass.Gravity.Z), new(mass.LinearDrag),
                    mode == RigidMotionKind.Dynamic ? MassProperties(mass) : default, entry.Mechanical?.Payload ?? PayloadPolicy.None));
                AddGeometry(instance.Id, rows.Material, rows.Colliders, shapes);
                CompileSliders(instance, rows, parameters, values, sliders);
                CompileHinges(instance, rows, parameters, values, hinges);
                if (mechanical is not null) CompileExtrusions(instance.Id, rows, mechanical.Extrusions);
                if (mechanical is not null) CompileDrivenSurfaces(instance.Id, rows, parameters, mechanical.DrivenSurfaces);
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
                if (mechanical is not null) CompileTransmissions(instance.Id, rows, parameters, mechanical.Transmissions, mechanical.Retentions, mechanical.Sliders);
            }
        }

        private void CompileDrivenSurfaces(GpuBodyId owner, CompiledPartIdentities rows,
            IReadOnlyDictionary<string, float> parameters, IReadOnlyList<DrivenSurfaceDescriptor> surfaces)
        {
            for (var index = 0; index < surfaces.Count; index++)
            {
                var surface = surfaces[index];
                var carrier = ResolvePortBody(owner, surface.Carrier);
                var colliderRows = surface.Carrier.Value == 0 ? rows.Colliders :
                    surface.Carrier.Value <= rows.Sliders.Length ? rows.Sliders[surface.Carrier.Value - 1].Colliders :
                    rows.Hinges[surface.Carrier.Value - 1 - rows.Sliders.Length].Colliders;
                drivenSurfaces.Add(new(rows.DrivenSurfaces[index], carrier, colliderRows[surface.Collider.Value],
                    surface.Face, rows.Hinges[surface.Source.Index].Joint, surface.LocalDirection,
                    surface.TravelPerRadian.Evaluate(parameters)));
            }
        }

        private void CompileExtrusions(GpuBodyId owner, CompiledPartIdentities rows, IReadOnlyList<ClippedExtrusionDescriptor> extrusions)
        {
            foreach (var extrusion in extrusions)
            {
                var joint = rows.Sliders[extrusion.Coordinate.Index];
                var colliderId = joint.Colliders[extrusion.Collider.Value];
                var index = colliders.FindIndex(collider => collider.Id == colliderId);
                colliders[index] = colliders[index] with
                {
                    Extrusion = new(joint.Joint, ResolvePortBody(owner, extrusion.MaterialBody),
                        ResolvePortBody(owner, extrusion.ClippingBody), extrusion.Cut, extrusion.DistalOffset, extrusion.HalfExtents)
                };
            }
        }

        private void CompileTransmissions(GpuBodyId owner, CompiledPartIdentities rows,
            IReadOnlyDictionary<string, float> parameters, IReadOnlyList<MechanicalTransmissionDescriptor> transmissions,
            IReadOnlyList<PrismaticRetentionDescriptor> retentions, IReadOnlyList<PrismaticSliderDescriptor> sliders)
        {
            for (var index = 0; index < transmissions.Count; index++)
            {
                var item = transmissions[index];
                var driving = ResolveCoordinate(rows, item.Driving);
                var driven = ResolveCoordinate(rows, item.Driven);
                var ratio = item.Ratio.Evaluate(parameters);
                var closeSeconds = item.CloseSeconds?.Evaluate(parameters) ?? 0f;
                PrismaticRetentionId? retentionId = null;
                var initial = 0f;
                foreach (var retention in retentions)
                    if (retention.WindingTransmission == item.Id)
                    {
                        retentionId = rows.Retentions[retention.Id];
                        // 0f - c keeps an uncompressed store at +0, the value the wire's default elision decodes,
                        // so local and decoded declarations agree bit for bit.
                        initial = 0f - sliders[retention.Coordinate.Index].Compression.Evaluate(parameters);
                    }
                internalTransmissions.Add(new(rows.Transmissions[item.Id], driving, driven, ratio, item.ConstraintKind, closeSeconds, owner, retentionId, initial));
            }
        }

        private static ConstraintCoordinateReference ResolveCoordinate(CompiledPartIdentities rows, CoordinateReference coordinate) =>
            coordinate.Kind switch
            {
                CoordinateKind.Prismatic => new(rows.Sliders[coordinate.Index].Joint),
                CoordinateKind.Revolute => new(rows.Hinges[coordinate.Index].Joint),
                _ => throw new ArgumentException("Unsupported constraint coordinate.")
            };

        private static void ValidateBodylessGeometry(BundleEntry entry)
        {
            if (entry.Mechanical is not null || entry.Colliders.Count != 0 || entry.ColliderBindings.Count != 0 ||
                entry.Presentation.Bodies.Count != 0 || entry.Footprint.MovingBodies != 0 ||
                entry.Footprint.PrismaticConstraints != 0 || entry.Footprint.RevoluteConstraints != 0)
                throw new ArgumentException("Logical owner declares inconsistent physical geometry.");
        }

        private void CompileSensors()
        {
            CompileOrientationSensors();
            CompileResidenceSensors();
            CompilePlanarGuides();
            CompileElectricalSources();
            CompileContactTriggers();
            CompileContactWorks();
            CompileEnergyStores();
            CompileChambersAndActuators();
            CompileContactLoads();
        }

        private void CompileOrientationSensors()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var orientation in DeclaredPhysics.Of(owner.PartId).Signal?.Nodes.OfType<SignalOrientationDescriptor>() ?? [])
                {
                    var body = bodies.Single(row => row.Id == identities.Body(owner.Id, orientation.Body));
                    orientationSensors.Add(new(identities[owner.Id].Signals[orientation.Id].Orientation, body.Id,
                        body.Rotation, OrientationThreshold.Degrees(orientation.ThresholdDegrees)));
                }
        }

        private void CompileResidenceSensors()
        {
            foreach (var frame in construction.Instances.OfType<WorkshopGenericInstance>())
            {
                var entry = DeclaredPhysics.Of(frame.PartId);
                var parameters = frame.Parameters.ToBundleValues();
                if (entry.Mechanical?.Residence is { } residence)
                {
                    var margin = residence.UpperMargin.Evaluate(parameters);
                    var speed = new LinearSpeed(residence.SpeedLimit.Evaluate(parameters));
                    var dwell = new DurationSeconds(residence.Dwell.Evaluate(parameters));
                    var participation = parameters[residence.ParticipationParameter] switch
                    {
                        0f => SensorParticipation.Disabled,
                        1f => SensorParticipation.Enabled,
                        _ => throw new ArgumentException("Undefined declared capture participation.")
                    };
                    var targets = ResolveTarget(residence.Target, construction.Puzzle, construction.Instances);
                    var pose = new RigidLocalPose(new(residence.Position.X, residence.Position.Y, residence.Position.Z),
                        CanonicalRotation.Normalize(residence.Rotation.X, residence.Rotation.Y, residence.Rotation.Z, residence.Rotation.W));
                    var captureRows = identities[frame.Id].Capture;
                    sensors.Add(new(captureRows.Sensor, frame.Id, targets, pose,
                        new(residence.Minimum.X, residence.Minimum.Y, residence.Minimum.Z),
                        new(residence.Maximum.X, residence.Maximum.Y + margin, residence.Maximum.Z),
                        speed, dwell, participation));
                }
            }
        }

        private void CompilePlanarGuides()
        {
            foreach (var frame in construction.Instances.OfType<WorkshopGenericInstance>())
            {
                var entry = DeclaredPhysics.Of(frame.PartId);
                var parameters = frame.Parameters.ToBundleValues();
                if (entry.Mechanical?.PlanarGuide is { } guide)
                {
                    var targets = ResolveTarget(guide.Target, construction.Puzzle, construction.Instances);
                    var pose = new RigidLocalPose(new(guide.Position.X, guide.Position.Y, guide.Position.Z),
                        CanonicalRotation.Normalize(guide.Rotation.X, guide.Rotation.Y, guide.Rotation.Z, guide.Rotation.W));
                    var captureRows = identities[frame.Id].Capture;
                    guides.Add(new(captureRows.Guide, frame.Id, targets, pose,
                        new(guide.Minimum.X, guide.Minimum.Y, guide.Minimum.Z),
                        new(guide.Maximum.X, guide.Maximum.Y, guide.Maximum.Z),
                        new(guide.SupportHeight), new(guide.SupportMargin.Evaluate(parameters)),
                        new(guide.MaximumAcceleration.Evaluate(parameters))));
                }
            }
        }

        private void CompileElectricalSources()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var source in (DeclaredPhysics.Of(owner.PartId).Electrical?.SupplyGraph?.Nodes ?? []).OfType<SupplySourceDescriptor>())
                    electricalSources.Add(ElectricalSourceFor(identities[owner.Id].Supply[source.Id].Source, owner.Id, source.Store, owner.Parameters));
        }

        private void CompileContactTriggers()
        {
            foreach (var trigger in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var contact in DeclaredPhysics.Of(trigger.PartId).Signal?.Nodes.OfType<SignalContactDescriptor>() ?? [])
                    triggers.Add(new(identities[trigger.Id].Signals[contact.Id].Trigger, identities.Body(trigger.Id, contact.Body),
                        ResolveTarget(contact.Target, construction.Puzzle, construction.Instances),
                        new(trigger.Parameters[contact.ThresholdParameter])));
        }

        private void CompileEnergyStores()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var store in DeclaredPhysics.Of(owner.PartId).Mechanical?.EnergyStores ?? [])
                {
                    var parameters = owner.Parameters.ToBundleValues();
                    if (store.Selection is { } selection && parameters[selection.Parameter.Value] != selection.Option) continue;
                    energyStores.Add(new(identities[owner.Id].EnergyStores[store.Id], identities.Body(owner.Id, store.Owner),
                        new(store.Capacity.Evaluate(parameters)), new(store.InitialEnergy.Evaluate(parameters)),
                        new(store.MaximumChargePower.Evaluate(parameters))));
                }
        }

        private void CompileChambersAndActuators()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
            {
                var mechanical = DeclaredPhysics.Of(owner.PartId).Mechanical;
                var rows = identities[owner.Id];
                foreach (var region in mechanical?.ChamberRegions ?? [])
                {
                    var pose = new RigidLocalPose(new(region.Position.X, region.Position.Y, region.Position.Z),
                        CanonicalRotation.Normalize(region.Rotation.X, region.Rotation.Y, region.Rotation.Z, region.Rotation.W));
                    chamberRegions.Add(new(rows.ChamberRegions[region.Id], identities.Body(owner.Id, region.Frame),
                        identities.Collider(owner.Id, region.Frame, region.ParticipationCollider),
                        ResolveTarget(region.Target, construction.Puzzle, construction.Instances), pose,
                        region.Radius, region.HalfLength, region.MinimumWidth, region.MaximumWidth, region.ContainmentAllowance,
                        region.MaximumRelativeSpeed, region.Participation));
                }
                foreach (var actuator in mechanical?.FiniteWorkActuators ?? [])
                    finiteWorkActuators.Add(new(rows.FiniteWorkActuators[actuator.Id], rows.EnergyStores[actuator.Store],
                        rows.ChamberRegions[actuator.Region], new(actuator.TargetSpeed.Evaluate(owner.Parameters.ToBundleValues())),
                        actuator.ExitPadding));
            }
        }

        private void CompileContactWorks()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var work in DeclaredPhysics.Of(owner.PartId).Mechanical?.ContactWorks ?? [])
                    contactWorks.Add(ContactWorkFor(identities[owner.Id].Works[work.Id], identities.Body(owner.Id, work.Owner), identities[owner.Id].EnergyStores[work.Store],
                        ResolveTarget(work.Target, construction.Puzzle, construction.Instances),
                        work, owner.Parameters));
        }

        private void CompileContactLoads()
        {
            foreach (var owner in construction.Instances.OfType<WorkshopGenericInstance>())
                foreach (var load in DeclaredPhysics.Of(owner.PartId).Signal?.Nodes.OfType<SignalContactLoadDescriptor>() ?? [])
                    contactLoads.Add(ContactLoadSensorFor(bundle, owner, load, identities));
        }

        private void ValidateHollowPlacement()
        {
            // Only independently authored dynamic partners are checked. A constrained
            // child belongs to its parent's mechanism and retains its declared joint.
            var bodyById = bodies.ToDictionary(body => body.Id);
            var hollowColliders = colliders.Where(c => c.Participation == CollisionParticipation.Enabled && core.Geometry.IsHollow(c.Shape)).ToArray();
            foreach (var partner in colliders.Where(c => c.Participation == CollisionParticipation.Enabled && bodyById[c.Body].Motion == RigidMotionKind.Dynamic))
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
                        throw new WorkshopEditRejectedException(WorkshopEditRejection.InitialOverlap,
                            $"A dynamic payload initially overlaps a hollow wall. Partner body {partner.Body.Value}, collider {partner.Id.Value}, {partner.Shape} at {partnerCentre}; hollow body {hollowCollider.Body.Value}, collider {hollowCollider.Id.Value}, {hollowCollider.Shape} at {centre}.");
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
            var transmissions = new List<MechanicalTransmissionDeclaration>(internalTransmissions);
            var reservedIds = new HashSet<ulong>(internalTransmissions.Select(t => t.Id.Value));
            var nextExternalId = 1UL;
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

                while (reservedIds.Contains(nextExternalId)) nextExternalId++;
                var transmissionId = new MechanicalTransmissionId(nextExternalId++);
                transmissions.Add(new(transmissionId, new(drivingJoint), new(drivenJoint), ratio, TransmissionConstraintKind.Bilateral));
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

    /// <summary>Resolve contact discharge data against its exact independently compiled store.</summary>
    internal static ContactWorkDeclaration ContactWorkFor(GpuContactWorkId identity, GpuBodyId owner, EnergyStoreId store, BodyTargetSet targets,
        ContactWorkDescriptor work, InstanceParameterCollection parameters)
    {
        var speed = work.TargetSpeed.Evaluate(parameters.ToBundleValues());
        return new(identity, owner, targets, new(speed), store,
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
