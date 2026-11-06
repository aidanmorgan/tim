using System.Numerics;
using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public sealed class PhysicsSceneDeclaration : IPhysicsSceneDeclaration
{
    private readonly TensileSpanDeclaration[] _tensileSpans;
    public ReadOnlySpan<TensileSpanDeclaration> TensileSpans => _tensileSpans;
    private readonly ContactLoadSensorDeclaration[] _contactLoadSensors;
    /// <summary>The compiled per-plate contact-load rows (Story 9R.4), sorted by identity.</summary>
    public ReadOnlySpan<ContactLoadSensorDeclaration> ContactLoadSensors => _contactLoadSensors;
    private readonly PrismaticConstraintDeclaration[] _prismatics;
    public ReadOnlySpan<PrismaticConstraintDeclaration> Prismatics => _prismatics;
    private readonly RevoluteConstraintDeclaration[] _revolutes;
    public ReadOnlySpan<RevoluteConstraintDeclaration> Revolutes => _revolutes;
    private readonly CompliantSurfaceDeclaration[] _compliantSurfaces;
    public ReadOnlySpan<CompliantSurfaceDeclaration> CompliantSurfaces => _compliantSurfaces;
    private readonly PrismaticDriveDeclaration[] _prismaticDrives;
    /// <summary>The supplied drives, at most one per prismatic, sorted by joint identity (C-Gate).</summary>
    public ReadOnlySpan<PrismaticDriveDeclaration> PrismaticDrives => _prismaticDrives;
    private readonly RevoluteDriveDeclaration[] _revoluteDrives;
    /// <summary>The supplied velocity drives, at most one per revolute, sorted by joint identity.</summary>
    public ReadOnlySpan<RevoluteDriveDeclaration> RevoluteDrives => _revoluteDrives;
    private readonly RopeGuideDeclaration[] _ropeGuides;
    public ReadOnlySpan<RopeGuideDeclaration> RopeGuides => _ropeGuides;
    private readonly MechanicalTransmissionDeclaration[] _transmissions;
    public ReadOnlySpan<MechanicalTransmissionDeclaration> Transmissions => _transmissions;
    private readonly DrivenSurfaceDeclaration[] _drivenSurfaces;
    public ReadOnlySpan<DrivenSurfaceDeclaration> DrivenSurfaces => _drivenSurfaces;
    public CompiledSupplyGraph SupplyGraph { get; }
    private readonly RigidBodyDeclaration[] _bodies;
    private readonly ColliderDeclaration[] _colliders;
    private readonly ContactMaterialDeclaration[] _materials;
    private readonly ResidenceSensorDeclaration[] _sensors;
    private readonly PlanarGuideDeclaration[] _guides;
    private readonly ContactTriggerDeclaration[] _triggers;
    private readonly ContactWorkDeclaration[] _contactWorks;
    private readonly OrientationSensorDeclaration[] _orientationSensors;
    public ElectricalSupplyPlan Electrical { get; }
    public CompiledMemoryLayout Layout { get; }
    /// <summary>The world this scene admits: every table exactly its compiled population.</summary>
    public PhysicsWorldLayout World { get; }
    public PhysicsDocumentId Document { get; }
    public ulong NextIdentity { get; }
    public ReadOnlySpan<RigidBodyDeclaration> Bodies => _bodies;
    public ReadOnlySpan<ColliderDeclaration> Colliders => _colliders;
    public ReadOnlySpan<ContactMaterialDeclaration> Materials => _materials;
    public ReadOnlySpan<ResidenceSensorDeclaration> Sensors => _sensors;
    public ReadOnlySpan<PlanarGuideDeclaration> Guides => _guides;
    public ReadOnlySpan<ContactTriggerDeclaration> Triggers => _triggers;
    public ReadOnlySpan<ContactWorkDeclaration> ContactWorks => _contactWorks;
    public ReadOnlySpan<OrientationSensorDeclaration> OrientationSensors => _orientationSensors;


    ICompiledSupplyGraph IPhysicsSceneDeclaration.SupplyGraph => SupplyGraph;
    IElectricalSupplyPlan IPhysicsSceneDeclaration.Electrical => Electrical;
    ICompiledMemoryLayout IPhysicsSceneDeclaration.Layout => Layout;
    IPhysicsWorldLayout IPhysicsSceneDeclaration.World => _worldView;
    private readonly IPhysicsWorldLayout _worldView;

    public PhysicsSceneDeclaration(PhysicsDocumentId document, ulong nextIdentity,
        ReadOnlySpan<RigidBodyDeclaration> bodies, ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<ContactMaterialDeclaration> materials, ReadOnlySpan<ResidenceSensorDeclaration> sensors,
        ReadOnlySpan<PlanarGuideDeclaration> guides, ReadOnlySpan<ContactTriggerDeclaration> triggers = default,
        ReadOnlySpan<ContactWorkDeclaration> contactWorks = default, ReadOnlySpan<OrientationSensorDeclaration> orientationSensors = default,
        ReadOnlySpan<ElectricalSourceDeclaration> electricalSources = default, ReadOnlySpan<ElectricalStorageBinding> electricalBindings = default,
        ReadOnlySpan<PrismaticConstraintDeclaration> prismatics = default,
        ReadOnlySpan<SupplyNodeDeclaration> supplyNodes = default,
        ReadOnlySpan<SupplyEdge> supplyEdges = default,
        ReadOnlySpan<ContactLoadSensorDeclaration> contactLoadSensors = default,
        ReadOnlySpan<PrismaticDriveDeclaration> prismaticDrives = default,
        ReadOnlySpan<TensileSpanDeclaration> tensileSpans = default,
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes = default,
        ReadOnlySpan<CompliantSurfaceDeclaration> compliantSurfaces = default,
        ReadOnlySpan<RevoluteDriveDeclaration> revoluteDrives = default,
        ReadOnlySpan<RopeGuideDeclaration> ropeGuides = default,
        ReadOnlySpan<MechanicalTransmissionDeclaration> transmissions = default,
        ReadOnlySpan<DrivenSurfaceDeclaration> drivenSurfaces = default)
    {
        if ((document.Low == 0 && document.High == 0) || nextIdentity == 0)
            throw new ArgumentException("Invalid physics document.");
        var ids = new HashSet<ulong>(64);
        var (bodyMap, dynamicCount, dynamicColliders) = ValidateGeometry(bodies, colliders, materials, nextIdentity, ids);
        ValidateSpatialPolicies(sensors, guides, bodyMap, nextIdentity, ids);
        ValidateContactPolicies(triggers, contactWorks, colliders, bodyMap, nextIdentity, ids);
        ValidateObservations(orientationSensors, contactLoadSensors, colliders, dynamicColliders, bodyMap, nextIdentity, ids);
        ValidateJoints(prismatics, revolutes, bodyMap, nextIdentity, ids);
        _tensileSpans = ValidateTensile(tensileSpans, bodyMap, ropeGuides);
        var guideIds = new HashSet<RopeGuideId>();
        foreach (var guide in ropeGuides)
        {
            guide.Validate();
            if (!guideIds.Add(guide.Id))
                throw new ArgumentException("Rope guides require unique identities.");
            if (!bodyMap.ContainsKey(guide.Body))
                throw new ArgumentException("Rope guide requires an admitted body.");
        }
        _ropeGuides = ropeGuides.ToArray();
        Array.Sort(_ropeGuides, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        _prismatics = prismatics.ToArray();
        Array.Sort(_prismatics, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        _revolutes = revolutes.ToArray();
        Array.Sort(_revolutes, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        var surfaceBodies = new HashSet<GpuBodyId>(4);
        foreach (var surface in compliantSurfaces)
        {
            surface.Validate(); Identity(surface.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(surface.Body, out var body) || body.Motion != RigidMotionKind.Static ||
                !surfaceBodies.Add(surface.Body))
                throw new ArgumentException("Compliant surface requires a unique static owner body.");
        }
        _compliantSurfaces = compliantSurfaces.ToArray();
        Array.Sort(_compliantSurfaces, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        var driven = new HashSet<PrismaticConstraintId>(4);
        foreach (var drive in prismaticDrives)
        {
            var joint = Array.Find(_prismatics, candidate => candidate.Id == drive.Joint);
            if (drive.Joint.Value == 0 || joint.Id != drive.Joint || !driven.Add(drive.Joint) || bodyMap[joint.BodyB].Motion != RigidMotionKind.Dynamic)
                throw new ArgumentException("A prismatic drive requires one admitted joint whose moving body is dynamic.");
            drive.Validate(joint);
        }
        _prismaticDrives = prismaticDrives.ToArray();
        Array.Sort(_prismaticDrives, (a, b) => a.Joint.Value.CompareTo(b.Joint.Value));
        var rotated = new HashSet<RevoluteConstraintId>(4);
        foreach (var drive in revoluteDrives)
        {
            var joint = Array.Find(_revolutes, candidate => candidate.Id == drive.Joint);
            if (drive.Joint.Value == 0 || joint.Id != drive.Joint || !rotated.Add(drive.Joint) || bodyMap[joint.BodyB].Motion != RigidMotionKind.Dynamic)
                throw new ArgumentException("A revolute drive requires one admitted joint whose moving body is dynamic.");
            drive.Validate(joint);
        }
        _revoluteDrives = revoluteDrives.ToArray();
        Array.Sort(_revoluteDrives, (a, b) => a.Joint.Value.CompareTo(b.Joint.Value));
        PhysicsDeclarationValidation.ValidateAll(drivenSurfaces, colliders, _revolutes);
        foreach (var surface in drivenSurfaces) Identity(surface.Id.Value, nextIdentity, ids);
        _drivenSurfaces = drivenSurfaces.ToArray();
        Array.Sort(_drivenSurfaces, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        PhysicsDeclarationValidation.ValidateAll(transmissions, _revolutes.Select(joint => joint.Id).ToHashSet());
        _transmissions = transmissions.ToArray();
        Array.Sort(_transmissions, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        foreach (var source in electricalSources) Identity(source.Id.Value, nextIdentity, ids);
        Electrical = new(electricalSources, electricalBindings, bodies, contactWorks);
        SupplyGraph = new(supplyNodes, supplyEdges, Electrical.Sources, _prismatics, _prismaticDrives, _revolutes, _revoluteDrives);
        Document = document; NextIdentity = nextIdentity;
        _bodies = bodies.ToArray(); _colliders = colliders.ToArray();
        _materials = materials.ToArray(); _sensors = sensors.ToArray(); _guides = guides.ToArray(); _triggers = triggers.ToArray();
        _contactWorks = contactWorks.ToArray(); _orientationSensors = orientationSensors.ToArray();
        Array.Sort(_orientationSensors, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        _contactLoadSensors = contactLoadSensors.ToArray();
        Array.Sort(_contactLoadSensors, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_bodies, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_colliders, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_materials, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_sensors, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_guides, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_triggers, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_contactWorks, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        World = new(_bodies.Length, _colliders.Length, _materials.Length, _sensors.Length, dynamicCount, _guides.Length,
            _triggers.Length, _contactWorks.Length, _orientationSensors.Length, Electrical.Sources.Length,
            Electrical.Bindings.Length, _prismatics.Length, _contactLoadSensors.Length, _tensileSpans.Length, _revolutes.Length,
            _compliantSurfaces.Length);
        _worldView = World;
        _ = World.ByteLength;
        Layout = CompiledMemoryLayout.Build(World, _bodies, _colliders, _materials, _sensors, _guides, _triggers, _contactWorks, _orientationSensors, Electrical.Sources, _prismatics, _revolutes, _compliantSurfaces);
    }

    private static (Dictionary<GpuBodyId, RigidBodyDeclaration> Bodies, int DynamicCount, HashSet<GpuBodyId> DynamicColliders) ValidateGeometry(ReadOnlySpan<RigidBodyDeclaration> bodies, ReadOnlySpan<ColliderDeclaration> colliders, ReadOnlySpan<ContactMaterialDeclaration> materials, ulong nextIdentity, HashSet<ulong> ids)
    {
        var bodyMap = new Dictionary<GpuBodyId, RigidBodyDeclaration>(16);
        var materialIds = new HashSet<GpuMaterialId>(32);
        var dynamicCount = 0;
        foreach (var body in bodies)
        {
            body.Validate(); Identity(body.Id.Value, nextIdentity, ids);
            bodyMap.Add(body.Id, body);
            if (body.Motion == RigidMotionKind.Dynamic) dynamicCount++;
        }
        foreach (var material in materials)
        {
            material.Validate(); Identity(material.Id.Value, nextIdentity, ids);
            materialIds.Add(material.Id);
        }
        var dynamicColliders = new HashSet<GpuBodyId>(16);
        foreach (var collider in colliders)
        {
            collider.Validate(); Identity(collider.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(collider.Body, out var body) || !materialIds.Contains(collider.Material))
                throw new ArgumentException("Collider refers to an absent body or material.");
            if (body.Motion == RigidMotionKind.Dynamic)
            {
                if (collider.Shape == ColliderShapeKind.Plane)
                    throw new ArgumentException("A dynamic body cannot own an infinite plane.");
                dynamicColliders.Add(body.Id);
            }
        }
        if (dynamicColliders.Count != dynamicCount)
            throw new ArgumentException("Every dynamic body requires collision geometry.");
        return (bodyMap, dynamicCount, dynamicColliders);
    }

    private static void ValidateSpatialPolicies(ReadOnlySpan<ResidenceSensorDeclaration> sensors, ReadOnlySpan<PlanarGuideDeclaration> guides, Dictionary<GpuBodyId, RigidBodyDeclaration> bodyMap, ulong nextIdentity, HashSet<ulong> ids)
    {
        var sensorOwners = new HashSet<(GpuBodyId,BodyTargetSet)>(4);
        foreach (var sensor in sensors)
        {
            sensor.Validate(); Identity(sensor.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(sensor.Frame, out var frame) || frame.Motion != RigidMotionKind.Static ||
                !sensorOwners.Add((sensor.Frame,sensor.Targets)) || (sensor.Targets.Kind==BodyTargetKind.NamedBody &&
                    (!bodyMap.TryGetValue(sensor.Targets.Body,out var target) || target.Motion!=RigidMotionKind.Dynamic)))
                throw new ArgumentException("Residence requires an admitted static frame and dynamic target.");
        }
        var guidedBodies = new HashSet<(GpuBodyId,BodyTargetSet)>(4);
        foreach (var guide in guides)
        {
            guide.Validate(); Identity(guide.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(guide.Frame, out var frame) || frame.Motion != RigidMotionKind.Static ||
                !guidedBodies.Add((guide.Frame,guide.Targets)) || (guide.Targets.Kind==BodyTargetKind.NamedBody &&
                    (!bodyMap.TryGetValue(guide.Targets.Body,out var target) || target.Motion!=RigidMotionKind.Dynamic)))
                throw new ArgumentException("Guide requires a static frame and unique admitted target policy.");
        }
    }

    private static void ValidateContactPolicies(ReadOnlySpan<ContactTriggerDeclaration> triggers, ReadOnlySpan<ContactWorkDeclaration> contactWorks, ReadOnlySpan<ColliderDeclaration> colliders, Dictionary<GpuBodyId, RigidBodyDeclaration> bodyMap, ulong nextIdentity, HashSet<ulong> ids)
    {
        foreach (var trigger in triggers)
        {
            trigger.Validate(); Identity(trigger.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(trigger.Owner, out var owner) || owner.Motion != RigidMotionKind.Static ||
                (trigger.Targets.Kind == BodyTargetKind.NamedBody &&
                    (!bodyMap.TryGetValue(trigger.Targets.Body, out var target) || target.Motion != RigidMotionKind.Dynamic)))
                throw new ArgumentException("Contact trigger requires a static owner and dynamic target.");
            var colliderFound = false;
            foreach (var collider in colliders) if (collider.Body == trigger.Owner) colliderFound = true;
            if (!colliderFound) throw new ArgumentException("Contact trigger owner has no physical collider.");
        }
        var workOwners = new HashSet<GpuBodyId>(4);
        foreach (var work in contactWorks)
        {
            work.Validate(); Identity(work.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(work.Owner, out var owner) || owner.Motion != RigidMotionKind.Static ||
                (work.Targets.Kind == BodyTargetKind.NamedBody &&
                    (!bodyMap.TryGetValue(work.Targets.Body, out var target) || target.Motion != RigidMotionKind.Dynamic)) ||
                !workOwners.Add(work.Owner))
                throw new ArgumentException("Contact work requires one reservoir per static owner and a dynamic target.");
            var colliderFound = false;
            foreach (var collider in colliders) if (collider.Body == work.Owner) colliderFound = true;
            if (!colliderFound) throw new ArgumentException("Contact work owner has no physical collider.");
        }
    }

    private static void ValidateObservations(ReadOnlySpan<OrientationSensorDeclaration> orientationSensors, ReadOnlySpan<ContactLoadSensorDeclaration> contactLoadSensors, ReadOnlySpan<ColliderDeclaration> colliders, HashSet<GpuBodyId> dynamicColliders, Dictionary<GpuBodyId, RigidBodyDeclaration> bodyMap, ulong nextIdentity, HashSet<ulong> ids)
    {
        foreach (var sensor in orientationSensors)
        {
            sensor.Validate(); Identity(sensor.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(sensor.Body, out var body) || body.Motion != RigidMotionKind.Dynamic ||
                !dynamicColliders.Contains(sensor.Body))
                throw new ArgumentException("Orientation sensor requires a dynamic body with its collider.");
            // "Angle from the admitted pose" is a document invariant, not a compiler courtesy.
            if (body.Rotation != sensor.Initial) throw new ArgumentException("Orientation sensor initial pose differs from its body's admitted rotation.");
        }
        foreach (var sensor in contactLoadSensors)
        {
            sensor.Validate(); Identity(sensor.Id.Value, nextIdentity, ids);
            foreach (var member in sensor.SupportingColliders)
            {
                var owned = false;
                foreach (var collider in colliders) if (collider.Id == member && collider.Body == sensor.Frame) owned = true;
                if (!owned) throw new ArgumentException("Contact-load member is absent or owned by another body.");
            }
            var faceOwned = false;
            foreach (var collider in colliders) if (collider.Id == sensor.Region && collider.Body == sensor.Frame) faceOwned = true;
            if (!bodyMap.TryGetValue(sensor.Frame, out var frame) || frame.Motion != RigidMotionKind.Static || !faceOwned)
                throw new ArgumentException("A contact-load row requires a static frame and a sensing face that frame owns.");
        }
    }

    private static void ValidateJoints(ReadOnlySpan<PrismaticConstraintDeclaration> prismatics, ReadOnlySpan<RevoluteConstraintDeclaration> revolutes, Dictionary<GpuBodyId, RigidBodyDeclaration> bodyMap, ulong nextIdentity, HashSet<ulong> ids)
    {
        var pairs = new HashSet<(GpuBodyId, GpuBodyId)>(16);
        foreach (var joint in prismatics)
        {
            joint.Validate(); Identity(joint.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(joint.BodyA, out var a) || !bodyMap.TryGetValue(joint.BodyB, out var b) ||
                (a.Motion != RigidMotionKind.Dynamic && b.Motion != RigidMotionKind.Dynamic) ||
                !pairs.Add(joint.BodyA.Value < joint.BodyB.Value ? (joint.BodyA, joint.BodyB) : (joint.BodyB, joint.BodyA)))
                throw new ArgumentException("A prismatic joint requires a unique admitted pair with a dynamic body.");
        }
        foreach (var joint in revolutes)
        {
            joint.Validate(); Identity(joint.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(joint.BodyA, out var a) || !bodyMap.TryGetValue(joint.BodyB, out var b) ||
                (a.Motion != RigidMotionKind.Dynamic && b.Motion != RigidMotionKind.Dynamic) ||
                !pairs.Add(joint.BodyA.Value < joint.BodyB.Value ? (joint.BodyA, joint.BodyB) : (joint.BodyB, joint.BodyA)))
                throw new ArgumentException("A revolute joint requires a unique admitted pair with a dynamic body.");
        }
    }

    private static TensileSpanDeclaration[] ValidateTensile(ReadOnlySpan<TensileSpanDeclaration> tensileSpans, Dictionary<GpuBodyId, RigidBodyDeclaration> bodyMap, ReadOnlySpan<RopeGuideDeclaration> guides)
    {
        var spanIds = new HashSet<RopeSpanId>();
        var routeOrders = new HashSet<(RopeRouteId, uint)>();
        var guideUses = new HashSet<RopeGuideId>();
        foreach (var span in tensileSpans)
        {
            span.Validate();
            ValidateTensileGuide(span, guides, guideUses);
            if (!spanIds.Add(span.Id) || !routeOrders.Add((span.Route, span.Order)) ||
                !bodyMap.TryGetValue(span.BodyA, out var a) || !bodyMap.TryGetValue(span.BodyB, out var b) ||
                a.Motion is not (RigidMotionKind.Static or RigidMotionKind.Dynamic) ||
                b.Motion is not (RigidMotionKind.Static or RigidMotionKind.Dynamic))
                throw new ArgumentException("Tensile span requires unique identities and admitted participants.");
            if (span.Complete && a.Motion == RigidMotionKind.Static && b.Motion == RigidMotionKind.Static &&
                System.Numerics.Vector3.Distance(Attachment(a, span.LocalA), Attachment(b, span.LocalB)) > span.MaximumLength)
                throw new ArgumentException("An immovable rope exceeds its authored length.");
        }
        var sortedSpans = tensileSpans.ToArray();
        Array.Sort(sortedSpans, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        ValidateTensileRoutes(sortedSpans);
        return sortedSpans;
    }

    private static void ValidateTensileGuide(TensileSpanDeclaration span, ReadOnlySpan<RopeGuideDeclaration> guides,
        HashSet<RopeGuideId> guideUses)
    {
        if (span.Guide.Value != 0)
        {
            var found = false;
            foreach (var guide in guides)
                if (guide.Id == span.Guide && guide.Body == span.BodyB && guide.Radius == span.GuideRadius && guide.Offset == span.GuideOffset)
                    found = true;
            if (!found || !guideUses.Add(span.Guide))
                throw new ArgumentException("A route junction requires its exact, uniquely used declared guide.");
        }
    }

    private static void ValidateTensileRoutes(TensileSpanDeclaration[] sortedSpans)
    {
        var orderedRoutes = new Dictionary<RopeRouteId, SortedDictionary<uint, TensileSpanDeclaration>>();
        foreach (var span in sortedSpans)
        {
            if (!orderedRoutes.TryGetValue(span.Route, out var ordered)) orderedRoutes.Add(span.Route, ordered = new());
            ordered.Add(span.Order, span);
        }
        foreach (var route in orderedRoutes.Values)
        {
            uint order = 0;
            TensileSpanDeclaration? previous = null;
            var visited = new HashSet<GpuBodyId>();
            foreach (var span in route.Values)
            {
                if (span.Order != order++ || (previous is { } prior &&
                    (prior.BodyB != span.BodyA || prior.LocalB != span.LocalA || prior.GuideRadius <= 0f || !float.IsFinite(prior.GuideRadius))) ||
                    !visited.Add(span.BodyA))
                    throw new ArgumentException("Tensile route must be one ordered acyclic chain.");
                previous = span;
            }
            if (previous is { } last && (!visited.Add(last.BodyB) || last.Guide.Value != 0))
                throw new ArgumentException("Tensile route must end at a terminal without a guide.");
        }
    }

    private static void Identity(ulong id, ulong next, HashSet<ulong> identities)
    {
        if (id == 0 || id >= next || !identities.Add(id))
            throw new ArgumentException("Physical identities must be allocated once in the document.");
    }

    private static System.Numerics.Vector3 Attachment(RigidBodyDeclaration body, System.Numerics.Vector3 local) =>
        new System.Numerics.Vector3((body.Cell.X + body.Local.X) / 16f, (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f) +
        System.Numerics.Vector3.Transform(local, new System.Numerics.Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W));
}
