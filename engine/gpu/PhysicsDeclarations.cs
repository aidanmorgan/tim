using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

public enum RigidMotionKind : uint { Static, Dynamic }
public enum ColliderShapeKind : uint { Sphere, Box, Plane }
public enum SensorParticipation : uint { Disabled, Enabled }
public readonly record struct PhysicsDocumentId(ulong Low, ulong High);
public readonly record struct GpuColliderId(ulong Value);
public readonly record struct GpuMaterialId(ulong Value);
public readonly record struct GpuSensorId(ulong Value);
public readonly record struct GpuForceId(ulong Value);
public readonly record struct FrictionCoefficient(Half Value);
/// <summary>Dimensionless rolling-resistance coefficient Crr: at every sphere contact the solver opposes the relative rolling with an
/// angular impulse of at most Crr · normal impulse · sphere radius, equal and opposite on both bodies.</summary>
public readonly record struct RollingResistance(Half Value);
public readonly record struct LinearSpeed(Half Value);
public readonly record struct DurationSeconds(Half Value);
public readonly record struct MetreVector(Half X, Half Y, Half Z);
public readonly record struct AccelerationVector(Half X, Half Y, Half Z);
public readonly record struct AngularVelocity(Half X, Half Y, Half Z);
public readonly record struct RigidLocalPose(MetreVector Translation, CanonicalRotation Rotation)
{
    public static RigidLocalPose Identity => new(default, CanonicalRotation.Identity);
    public void Validate()
    {
        PhysicsDeclarationBounds.Vector(Translation.X, Translation.Y, Translation.Z, (Half)16);
        Rotation.Validate();
    }
}

/// <summary>Canonical physical parameters. No catalogue identity selects a material law.</summary>
public readonly record struct ContactMaterialDeclaration(
    GpuMaterialId Id, Restitution Restitution, LinearSpeed BounceThreshold, FrictionCoefficient Friction,
    RollingResistance RollingResistance)
{
    public void Validate()
    {
        if (Id.Value == 0) throw new ArgumentException("Material identity is required.");
        PhysicsDeclarationBounds.Range(Restitution.Value, (Half)0, (Half)1);
        PhysicsDeclarationBounds.Range(BounceThreshold.Value, (Half)0, (Half)64);
        PhysicsDeclarationBounds.Range(Friction.Value, (Half)0, (Half)1);
        PhysicsDeclarationBounds.Range(RollingResistance.Value, (Half)0, (Half)0.1);
    }
}

/// <summary>Initial primary state and force parameters; the GPU owns all motion and force evaluation.</summary>
public readonly record struct RigidBodyDeclaration(
    GpuBodyId Id, RigidMotionKind Motion, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, CellVelocity Velocity, AngularVelocity AngularVelocity,
    Kilograms Mass, AccelerationVector Gravity, InverseSeconds LinearDrag)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Motion)) throw new ArgumentException("Undefined rigid motion.");
        new CanonicalBody(Id, 0, 0, Cell, Local, Velocity).Validate();
        Rotation.Validate();
        PhysicsDeclarationBounds.Vector(AngularVelocity.X, AngularVelocity.Y, AngularVelocity.Z, (Half)128);
        PhysicsDeclarationBounds.Vector(Gravity.X, Gravity.Y, Gravity.Z, (Half)16);
        PhysicsDeclarationBounds.Range(LinearDrag.Value, (Half)0, (Half)0.125);
        if (Motion == RigidMotionKind.Static)
        {
            if (!PhysicsDeclarationBounds.Zero(Mass.Value) || !HalfBits.IsPositiveZero(Velocity) ||
                !PhysicsDeclarationBounds.Zero(AngularVelocity.X, AngularVelocity.Y, AngularVelocity.Z) ||
                !PhysicsDeclarationBounds.Zero(Gravity.X, Gravity.Y, Gravity.Z) ||
                !PhysicsDeclarationBounds.Zero(LinearDrag.Value))
                throw new ArgumentException("Static bodies require zero motion, mass and forces.");
            return;
        }
        PhysicsDeclarationBounds.Range(Mass.Value, (Half)(1.0 / 1024), (Half)1024);
        // Wide arithmetic is admission-only: it rejects an out-of-domain declaration, never integrates it.
        if (Squared(Velocity.X, Velocity.Y, Velocity.Z) > 4 ||
            Squared(Gravity.X, Gravity.Y, Gravity.Z) > 256 ||
            Squared(AngularVelocity.X, AngularVelocity.Y, AngularVelocity.Z) > 16384)
            throw new ArgumentException("Linear motion, angular motion or gravity exceeds the admitted vector magnitude.");
    }

    private static double Squared(Half x, Half y, Half z) =>
        (double)x * (double)x + (double)y * (double)y + (double)z * (double)z;
}

/// <summary>Shape parameters are explicit, including canonical zero in unused lanes.</summary>
public readonly record struct ColliderDeclaration(
    GpuColliderId Id, GpuBodyId Body, GpuMaterialId Material, ColliderShapeKind Shape,
    RigidLocalPose Pose, Metres Radius, MetreVector HalfExtents)
{
    public void Validate()
    {
        if (Id.Value == 0 || Body.Value == 0 || Material.Value == 0 || !Enum.IsDefined(Shape))
            throw new ArgumentException("Invalid collider identity or shape.");
        Pose.Validate();
        switch (Shape)
        {
            case ColliderShapeKind.Sphere:
                PhysicsDeclarationBounds.Range(Radius.Value, (Half)(1.0 / 16), (Half)16);
                if (!PhysicsDeclarationBounds.Zero(HalfExtents.X, HalfExtents.Y, HalfExtents.Z))
                    throw new ArgumentException("Sphere extents must be zero.");
                break;
            case ColliderShapeKind.Box:
                if (!PhysicsDeclarationBounds.Zero(Radius.Value)) throw new ArgumentException("Box radius must be zero.");
                PhysicsDeclarationBounds.Range(HalfExtents.X, (Half)(1.0 / 1024), (Half)16);
                PhysicsDeclarationBounds.Range(HalfExtents.Y, (Half)(1.0 / 1024), (Half)16);
                PhysicsDeclarationBounds.Range(HalfExtents.Z, (Half)(1.0 / 1024), (Half)16);
                break;
            case ColliderShapeKind.Plane:
                if (!PhysicsDeclarationBounds.Zero(Radius.Value) ||
                    !PhysicsDeclarationBounds.Zero(HalfExtents.X, HalfExtents.Y, HalfExtents.Z))
                    throw new ArgumentException("Plane shape lanes must be zero.");
                break;
            default:
                throw new ArgumentException("Unsupported shape.");
        }
    }
}

/// <summary>Open-box residence against one named body in an explicit frame; no scene overlap authority.</summary>
public readonly record struct ResidenceSensorDeclaration(
    GpuSensorId Id, GpuBodyId Frame, GpuBodyId Target, RigidLocalPose Pose,
    MetreVector Minimum, MetreVector Maximum, LinearSpeed SpeedLimit, DurationSeconds Dwell,
    SensorParticipation Participation)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0 || Target.Value == 0 || Frame == Target ||
            !Enum.IsDefined(Participation))
            throw new ArgumentException("Invalid residence identity or participation.");
        Pose.Validate();
        PhysicsDeclarationBounds.Vector(Minimum.X, Minimum.Y, Minimum.Z, (Half)16);
        PhysicsDeclarationBounds.Vector(Maximum.X, Maximum.Y, Maximum.Z, (Half)16);
        if (Minimum.X >= Maximum.X || Minimum.Y >= Maximum.Y || Minimum.Z >= Maximum.Z)
            throw new ArgumentException("Residence bounds must enclose an open volume.");
        PhysicsDeclarationBounds.Range(SpeedLimit.Value, (Half)(1.0 / 1024), (Half)64);
        PhysicsDeclarationBounds.Range(Dwell.Value, (Half)(1.0 / 1024), (Half)30);
    }
}

/// <summary>Bounded descending-only planar centring in a declared static frame; the GPU evaluates the force.</summary>
public readonly record struct PlanarGuideDeclaration(
    GpuForceId Id, GpuBodyId Frame, GpuBodyId Target, RigidLocalPose Pose,
    MetreVector Minimum, MetreVector Maximum, Metres SupportHeight, Metres SupportMargin, Acceleration MaximumAcceleration)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0 || Target.Value == 0 || Frame == Target)
            throw new ArgumentException("Invalid guide identity.");
        Pose.Validate();
        PhysicsDeclarationBounds.Vector(Minimum.X, Minimum.Y, Minimum.Z, (Half)16);
        PhysicsDeclarationBounds.Vector(Maximum.X, Maximum.Y, Maximum.Z, (Half)16);
        if (Minimum.X >= Maximum.X || Minimum.Y >= Maximum.Y || Minimum.Z >= Maximum.Z)
            throw new ArgumentException("Guide bounds must enclose a volume.");
        PhysicsDeclarationBounds.Range(SupportHeight.Value, (Half)(-16), (Half)16);
        PhysicsDeclarationBounds.Range(SupportMargin.Value, (Half)0, (Half)1);
        PhysicsDeclarationBounds.Range(MaximumAcceleration.Value, (Half)0, (Half)12);
    }
}

/// <summary>One immutable owner of compiled physical declarations. Array order is not public identity.</summary>
public sealed class PhysicsSceneDeclaration
{
    public const int BodyCapacity = 33;
    public const int ColliderCapacity = 64;
    public const int MaterialCapacity = 33;
    public const int SensorCapacity = 16;
    public const int GuideCapacity = 16;
    public const int TriggerCapacity = 8;
    public const int ContactWorkCapacity = 8;
    public const int OrientationSensorCapacity = PhysicsBodyReadSet.Capacity;
    private readonly RigidBodyDeclaration[] _bodies;
    private readonly ColliderDeclaration[] _colliders;
    private readonly ContactMaterialDeclaration[] _materials;
    private readonly ResidenceSensorDeclaration[] _sensors;
    private readonly PlanarGuideDeclaration[] _guides;
    private readonly ContactTriggerDeclaration[] _triggers;
    private readonly ContactWorkDeclaration[] _contactWorks;
    private readonly OrientationSensorDeclaration[] _orientationSensors;
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

    public PhysicsSceneDeclaration(PhysicsDocumentId document, ulong nextIdentity,
        ReadOnlySpan<RigidBodyDeclaration> bodies, ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<ContactMaterialDeclaration> materials, ReadOnlySpan<ResidenceSensorDeclaration> sensors,
        ReadOnlySpan<PlanarGuideDeclaration> guides, ReadOnlySpan<ContactTriggerDeclaration> triggers = default,
        ReadOnlySpan<ContactWorkDeclaration> contactWorks = default, ReadOnlySpan<OrientationSensorDeclaration> orientationSensors = default)
    {
        if ((document.Low == 0 && document.High == 0) || nextIdentity == 0 ||
            bodies.Length > BodyCapacity || colliders.Length > ColliderCapacity ||
            materials.Length > MaterialCapacity || sensors.Length > SensorCapacity || guides.Length > GuideCapacity || triggers.Length > TriggerCapacity ||
            contactWorks.Length > ContactWorkCapacity || orientationSensors.Length > OrientationSensorCapacity)
            throw new ArgumentException("Invalid physics document or capacity.");
        var ids = new HashSet<ulong>();
        var bodyMap = new Dictionary<GpuBodyId, RigidBodyDeclaration>();
        var materialIds = new HashSet<GpuMaterialId>();
        var dynamicCount = 0;
        foreach (var body in bodies)
        {
            body.Validate(); Identity(body.Id.Value, nextIdentity, ids);
            bodyMap.Add(body.Id, body);
            if (body.Motion == RigidMotionKind.Dynamic) dynamicCount++;
        }
        if (dynamicCount > PhysicsBodyReadSet.Capacity) throw new ArgumentException("Dynamic body capacity exceeded.");
        foreach (var material in materials)
        {
            material.Validate(); Identity(material.Id.Value, nextIdentity, ids);
            materialIds.Add(material.Id);
        }
        var dynamicColliders = new HashSet<GpuBodyId>();
        foreach (var collider in colliders)
        {
            collider.Validate(); Identity(collider.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(collider.Body, out var body) || !materialIds.Contains(collider.Material))
                throw new ArgumentException("Collider refers to an absent body or material.");
            if (body.Motion == RigidMotionKind.Dynamic)
            {
                if (collider.Shape is not (ColliderShapeKind.Sphere or ColliderShapeKind.Box) || !dynamicColliders.Add(body.Id))
                    throw new ArgumentException("Dynamic bodies require one homogeneous sphere or box.");
                RigidMassProperties.Compile(body, collider).Validate();
            }
        }
        if (dynamicColliders.Count != dynamicCount)
            throw new ArgumentException("Every dynamic body requires its homogeneous collider.");
        foreach (var sensor in sensors)
        {
            sensor.Validate(); Identity(sensor.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(sensor.Frame, out var frame) || frame.Motion != RigidMotionKind.Static ||
                !bodyMap.TryGetValue(sensor.Target, out var target) || target.Motion != RigidMotionKind.Dynamic)
                throw new ArgumentException("Residence requires an admitted static frame and dynamic target.");
        }
        var guidedBodies = new HashSet<GpuBodyId>();
        foreach (var guide in guides)
        {
            guide.Validate(); Identity(guide.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(guide.Frame, out var frame) || frame.Motion != RigidMotionKind.Static ||
                !bodyMap.TryGetValue(guide.Target, out var target) || target.Motion != RigidMotionKind.Dynamic ||
                !dynamicColliders.Contains(guide.Target) || !guidedBodies.Add(guide.Target))
                throw new ArgumentException("Guide requires a static frame and one uniquely guided dynamic body.");
        }
        var owners = new HashSet<GpuBodyId>();
        foreach (var trigger in triggers)
        {
            trigger.Validate(); Identity(trigger.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(trigger.Owner, out var owner) || owner.Motion != RigidMotionKind.Static ||
                (trigger.Targets.Kind == BodyTargetKind.NamedBody &&
                    (!bodyMap.TryGetValue(trigger.Targets.Body, out var target) || target.Motion != RigidMotionKind.Dynamic)) ||
                !owners.Add(trigger.Owner))
                throw new ArgumentException("Contact trigger requires a unique static owner and dynamic target.");
            var colliderFound = false;
            foreach (var collider in colliders) if (collider.Body == trigger.Owner) colliderFound = true;
            if (!colliderFound) throw new ArgumentException("Contact trigger owner has no physical collider.");
        }
        var workOwners = new HashSet<GpuBodyId>();
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
        var sensed = new HashSet<GpuBodyId>();
        foreach (var sensor in orientationSensors)
        {
            sensor.Validate(); Identity(sensor.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(sensor.Body, out var body) || body.Motion != RigidMotionKind.Dynamic ||
                !dynamicColliders.Contains(sensor.Body) || !sensed.Add(sensor.Body))
                throw new ArgumentException("Orientation sensor requires one uniquely sensed dynamic body with its collider.");
            // "Angle from the admitted pose" is a document invariant, not a compiler courtesy.
            if (body.Rotation != sensor.Initial) throw new ArgumentException("Orientation sensor initial pose differs from its body's admitted rotation.");
        }
        Document = document; NextIdentity = nextIdentity;
        _bodies = bodies.ToArray(); _colliders = colliders.ToArray();
        _materials = materials.ToArray(); _sensors = sensors.ToArray(); _guides = guides.ToArray(); _triggers = triggers.ToArray();
        _contactWorks = contactWorks.ToArray(); _orientationSensors = orientationSensors.ToArray();
        Array.Sort(_orientationSensors, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_bodies, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_colliders, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_materials, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_sensors, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_guides, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_triggers, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        Array.Sort(_contactWorks, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
    }

    private static void Identity(ulong id, ulong next, HashSet<ulong> identities)
    {
        if (id == 0 || id >= next || !identities.Add(id))
            throw new ArgumentException("Physical identities must be allocated once in the document.");
    }
}

internal static class PhysicsDeclarationBounds
{
    internal static void Range(Half value, Half minimum, Half maximum)
    {
        if (!Half.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentException("Physical declaration exceeds its admitted domain.");
    }
    internal static void Vector(Half x, Half y, Half z, Half magnitude)
    {
        Range(x, (Half)(-magnitude), magnitude);
        Range(y, (Half)(-magnitude), magnitude);
        Range(z, (Half)(-magnitude), magnitude);
    }
    internal static bool Zero(Half value) => BitConverter.HalfToUInt16Bits(value) == 0;
    internal static bool Zero(Half x, Half y, Half z) => Zero(x) && Zero(y) && Zero(z);
}
