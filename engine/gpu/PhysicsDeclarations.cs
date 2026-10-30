using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public enum RigidMotionKind : uint { Static, Dynamic }
/// <summary>The closed collider set. A cylinder's axis is its local +Y; it is declared by radius and half-width (Story 10.1). Tube,
/// Torus and Frustum are the true hollow kinds (engine-hollow-shapes), static only, their lanes named on
/// <see cref="ColliderDeclaration"/>.</summary>
public enum ColliderShapeKind : uint { Sphere, Box, Plane, Cylinder, Tube, Torus, Frustum }
public enum SensorParticipation : uint { Disabled, Enabled }
public readonly record struct PhysicsDocumentId(ulong Low, ulong High);
public readonly record struct GpuColliderId(ulong Value);
public readonly record struct GpuMaterialId(ulong Value);
public readonly record struct GpuSensorId(ulong Value);
public readonly record struct GpuForceId(ulong Value);
public readonly record struct FrictionCoefficient(float Value);
/// <summary>Dimensionless rolling-resistance coefficient Crr: at every sphere contact and every curved-side cylinder contact the solver
/// opposes the relative rolling with an angular impulse of at most Crr · normal impulse · radius, equal and opposite on both
/// bodies.</summary>
public readonly record struct RollingResistance(float Value);
public readonly record struct LinearSpeed(float Value);
public readonly record struct DurationSeconds(float Value);
public readonly record struct MetreVector(float X, float Y, float Z);
public readonly record struct AccelerationVector(float X, float Y, float Z);
/// <summary>Committed angular velocity in radians per second as IEEE-754 f32, bounded by the game-grade 128 rad/s envelope.</summary>
public readonly record struct AngularVelocity(float X, float Y, float Z)
{
    public const float MaximumSpeed = 128;
    public void Validate() => PhysicsDeclarationBounds.Magnitude(X, Y, Z, MaximumSpeed);
    public bool IsPositiveZero => F32Bits.IsPositiveZero(X, Y, Z);
    public bool HasSameBits(AngularVelocity other) =>
        F32Bits.Equal(X, other.X) && F32Bits.Equal(Y, other.Y) && F32Bits.Equal(Z, other.Z);
}
public readonly record struct RigidLocalPose(MetreVector Translation, CanonicalRotation Rotation)
{
    public static RigidLocalPose Identity => new(default, CanonicalRotation.Identity);
    public void Validate()
    {
        PhysicsDeclarationBounds.Vector(Translation.X, Translation.Y, Translation.Z, 16f);
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
        PhysicsDeclarationBounds.Range(Restitution.Value, 0f,1f);
        PhysicsDeclarationBounds.Range(BounceThreshold.Value, 0f,64f);
        PhysicsDeclarationBounds.Range(Friction.Value, 0f,1f);
        PhysicsDeclarationBounds.Range(RollingResistance.Value, 0f,0.1f);
    }
}

/// <summary>Initial primary state and force parameters; the GPU owns all motion and force evaluation.</summary>
public readonly record struct RigidBodyDeclaration(
    GpuBodyId Id, RigidMotionKind Motion, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, LinearVelocity Velocity, AngularVelocity AngularVelocity,
    Kilograms Mass, AccelerationVector Gravity, InverseSeconds LinearDrag, RigidMassProperties MassProperties, PayloadPolicy Payload = PayloadPolicy.None)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Motion) || !Enum.IsDefined(Payload) || (Motion==RigidMotionKind.Static && Payload!=PayloadPolicy.None))
            throw new ArgumentException("Undefined rigid motion or payload policy.");
        new CanonicalBody(Id, 0, 0, Cell, Local, Velocity).Validate();
        Rotation.Validate();
        AngularVelocity.Validate();
        PhysicsDeclarationBounds.Vector(Gravity.X, Gravity.Y, Gravity.Z, 16f);
        PhysicsDeclarationBounds.Range(LinearDrag.Value, 0f,0.125f);
        if (Motion == RigidMotionKind.Static)
        {
            if (MassProperties != default || !PhysicsDeclarationBounds.Zero(Mass.Value) || !Velocity.IsPositiveZero || !AngularVelocity.IsPositiveZero ||
                !PhysicsDeclarationBounds.Zero(Gravity.X, Gravity.Y, Gravity.Z) ||
                !PhysicsDeclarationBounds.Zero(LinearDrag.Value))
                throw new ArgumentException("Static bodies require zero motion, mass and forces.");
            return;
        }
        PhysicsDeclarationBounds.Range(Mass.Value, 1f / 1024, 1024f);
        MassProperties.Validate();
        // Admission uses the same f32 arithmetic as the declared game values.
        if (Squared(Gravity.X, Gravity.Y, Gravity.Z) > 256)
            throw new ArgumentException("Gravity exceeds the admitted vector magnitude.");
    }

    private static float Squared(float x, float y, float z) =>
        x * x + y * y + z * z;
}

/// <summary>Shape parameters are explicit, including canonical zero in unused lanes. The hollow kinds' lanes (engine-hollow-shapes):
/// <list type="bullet">
/// <item>Tube about local +Y: <see cref="Radius"/> the outer radius r_o; <see cref="HalfExtents"/> (inner radius r_i, half-length h, 0).</item>
/// <item>Frustum about local +Y: <see cref="Radius"/> the outer radius r_0 at −h; <see cref="HalfExtents"/> (radial wall t, h, outer
/// radius r_1 at +h); the inner radii are r_0 − t and r_1 − t.</item>
/// <item>Torus segment about local +Y, centreline R (cos a, 0, sin a) for a in [0, θ]: <see cref="Radius"/> R; <see cref="HalfExtents"/>
/// (tube inner radius r_i, zero for a solid tube; tube outer radius r_o; sweep θ).</item>
/// </list>
/// Admission refuses a dimension outside its range, a tube or frustum without a bore, a frustum without a taper (a tube), a torus
/// whose tube reaches its axis (R ≤ r_o, self-intersecting) and a sweep outside (0, π] that is not the full turn
/// <see cref="FullSweep"/>.</summary>
public readonly record struct ColliderDeclaration(
    GpuColliderId Id, GpuBodyId Body, GpuMaterialId Material, ColliderShapeKind Shape,
    RigidLocalPose Pose, Metres Radius, MetreVector HalfExtents)
{
    /// <summary>The full turn of a whole torus: 2π in f32.</summary>
    public const float FullSweep = MathF.Tau;
    /// <summary>The thinnest admitted hollow wall (m).</summary>
    public const float MinimumThickness = 1f / 1024;

    public void Validate()
    {
        if (Id.Value == 0 || Body.Value == 0 || Material.Value == 0 || !Enum.IsDefined(Shape))
            throw new ArgumentException("Invalid collider identity or shape.");
        Pose.Validate();
        switch (Shape)
        {
            case ColliderShapeKind.Sphere:
                PhysicsDeclarationBounds.Range(Radius.Value, 1f / 32, 16f);
                if (!PhysicsDeclarationBounds.Zero(HalfExtents.X, HalfExtents.Y, HalfExtents.Z))
                    throw new ArgumentException("Sphere extents must be zero.");
                break;
            case ColliderShapeKind.Box:
                if (!PhysicsDeclarationBounds.Zero(Radius.Value)) throw new ArgumentException("Box radius must be zero.");
                PhysicsDeclarationBounds.Range(HalfExtents.X, 1f / 1024, 16f);
                PhysicsDeclarationBounds.Range(HalfExtents.Y, 1f / 1024, 16f);
                PhysicsDeclarationBounds.Range(HalfExtents.Z, 1f / 1024, 16f);
                break;
            case ColliderShapeKind.Plane:
                if (!PhysicsDeclarationBounds.Zero(Radius.Value) ||
                    !PhysicsDeclarationBounds.Zero(HalfExtents.X, HalfExtents.Y, HalfExtents.Z))
                    throw new ArgumentException("Plane shape lanes must be zero.");
                break;
            case ColliderShapeKind.Cylinder:
                // Radius r in the sphere range, half-width h = w/2 along local +Y in the box range; X and Z are canonical zero.
                PhysicsDeclarationBounds.Range(Radius.Value, 1f / 16, 16f);
                if (!PhysicsDeclarationBounds.Zero(HalfExtents.X) || !PhysicsDeclarationBounds.Zero(HalfExtents.Z))
                    throw new ArgumentException("Cylinder X and Z extents must be zero.");
                PhysicsDeclarationBounds.Range(HalfExtents.Y, 1f / 1024, 16f);
                break;
            case ColliderShapeKind.Tube:
                // A tube with no bore is a cylinder: the inner radius is at least 1/64 m and the wall at least MinimumThickness.
                PhysicsDeclarationBounds.Range(Radius.Value, 1f / 16, 16f);
                PhysicsDeclarationBounds.Range(HalfExtents.X, 1f / 64, Radius.Value - MinimumThickness);
                PhysicsDeclarationBounds.Range(HalfExtents.Y, 1f / 1024, 16f);
                if (!PhysicsDeclarationBounds.Zero(HalfExtents.Z)) throw new ArgumentException("Tube Z lane must be zero.");
                break;
            case ColliderShapeKind.Frustum:
                // Both outer radii in the tube range, a wall that leaves each end a bore of at least 1/64 m, and a taper (equal end
                // radii are a tube).
                PhysicsDeclarationBounds.Range(Radius.Value, 1f / 16, 16f);
                PhysicsDeclarationBounds.Range(HalfExtents.Z, 1f / 16, 16f);
                PhysicsDeclarationBounds.Range(HalfExtents.X, MinimumThickness, MathF.Min(Radius.Value, HalfExtents.Z) - 1f / 64);
                PhysicsDeclarationBounds.Range(HalfExtents.Y, 1f / 1024, 16f);
                if (Radius.Value == HalfExtents.Z) throw new ArgumentException("A frustum with equal end radii is a tube.");
                break;
            case ColliderShapeKind.Torus:
                // The tube's outer radius at least 1/512 m and inside the centreline radius (no self-intersection); a hollow tube's
                // wall at least MinimumThickness (a solid tube declares zero); a sweep in (0, π] or the full turn.
                PhysicsDeclarationBounds.Range(HalfExtents.Y, 1f / 512, 16f);
                PhysicsDeclarationBounds.Range(Radius.Value, 1f / 16, 16f);
                if (!(Radius.Value > HalfExtents.Y)) throw new ArgumentException("A torus tube must not reach its axis.");
                if (!PhysicsDeclarationBounds.Zero(HalfExtents.X))
                    PhysicsDeclarationBounds.Range(HalfExtents.X, 1f / 256, HalfExtents.Y - MinimumThickness);
                if (HalfExtents.Z != FullSweep) PhysicsDeclarationBounds.Range(HalfExtents.Z, MinimumThickness, MathF.PI);
                break;
            default:
                throw new ArgumentException("Unsupported shape.");
        }
    }
}

/// <summary>Open-box residence against one named body in an explicit frame; no scene overlap authority.</summary>
public readonly record struct ResidenceSensorDeclaration(
    GpuSensorId Id, GpuBodyId Frame, BodyTargetSet Targets, RigidLocalPose Pose,
    MetreVector Minimum, MetreVector Maximum, LinearSpeed SpeedLimit, DurationSeconds Dwell,
    SensorParticipation Participation)
{
    public void Validate()
    {
        Targets.Validate();
        if (Id.Value == 0 || Frame.Value == 0 || Frame == Targets.Body ||
            !Enum.IsDefined(Participation))
            throw new ArgumentException("Invalid residence identity or participation.");
        Pose.Validate();
        PhysicsDeclarationBounds.Vector(Minimum.X, Minimum.Y, Minimum.Z, 16f);
        PhysicsDeclarationBounds.Vector(Maximum.X, Maximum.Y, Maximum.Z, 16f);
        if (Minimum.X >= Maximum.X || Minimum.Y >= Maximum.Y || Minimum.Z >= Maximum.Z)
            throw new ArgumentException("Residence bounds must enclose an open volume.");
        PhysicsDeclarationBounds.Range(SpeedLimit.Value, 1f / 1024, 64f);
        PhysicsDeclarationBounds.Range(Dwell.Value, 1f / 1024, 30f);
    }
}

/// <summary>Bounded descending-only planar centring in a declared static frame; the GPU evaluates the force.</summary>
public readonly record struct PlanarGuideDeclaration(
    GpuForceId Id, GpuBodyId Frame, BodyTargetSet Targets, RigidLocalPose Pose,
    MetreVector Minimum, MetreVector Maximum, Metres SupportHeight, Metres SupportMargin, Acceleration MaximumAcceleration)
{
    public void Validate()
    {
        Targets.Validate();
        if (Id.Value == 0 || Frame.Value == 0 || Frame == Targets.Body)
            throw new ArgumentException("Invalid guide identity.");
        Pose.Validate();
        PhysicsDeclarationBounds.Vector(Minimum.X, Minimum.Y, Minimum.Z, 16f);
        PhysicsDeclarationBounds.Vector(Maximum.X, Maximum.Y, Maximum.Z, 16f);
        if (Minimum.X >= Maximum.X || Minimum.Y >= Maximum.Y || Minimum.Z >= Maximum.Z)
            throw new ArgumentException("Guide bounds must enclose a volume.");
        PhysicsDeclarationBounds.Range(SupportHeight.Value, -16f, 16f);
        PhysicsDeclarationBounds.Range(SupportMargin.Value, 0f,1f);
        PhysicsDeclarationBounds.Range(MaximumAcceleration.Value, 0f,12f);
    }
}

/// <summary>One immutable owner of compiled physical declarations. Array order is not public identity.
/// Story 9R.5b (owner decision, 20 Oct 2026): no table has a fixed capacity; every table is sized from this scene at Run.</summary>
public sealed class PhysicsSceneDeclaration
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
    private readonly PrismaticDriveDeclaration[] _prismaticDrives;
    /// <summary>The supplied drives, at most one per prismatic, sorted by joint identity (C-Gate).</summary>
    public ReadOnlySpan<PrismaticDriveDeclaration> PrismaticDrives => _prismaticDrives;
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
        ReadOnlySpan<RevoluteConstraintDeclaration> revolutes = default)
    {
        if ((document.Low == 0 && document.High == 0) || nextIdentity == 0)
            throw new ArgumentException("Invalid physics document.");
        var ids = new HashSet<ulong>(64);
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
        var owners = new HashSet<GpuBodyId>(4);
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
        var sensed = new HashSet<GpuBodyId>(4);
        foreach (var sensor in orientationSensors)
        {
            sensor.Validate(); Identity(sensor.Id.Value, nextIdentity, ids);
            if (!bodyMap.TryGetValue(sensor.Body, out var body) || body.Motion != RigidMotionKind.Dynamic ||
                !dynamicColliders.Contains(sensor.Body) || !sensed.Add(sensor.Body))
                throw new ArgumentException("Orientation sensor requires one uniquely sensed dynamic body with its collider.");
            // "Angle from the admitted pose" is a document invariant, not a compiler courtesy.
            if (body.Rotation != sensor.Initial) throw new ArgumentException("Orientation sensor initial pose differs from its body's admitted rotation.");
        }
        var loadFrames = new HashSet<GpuBodyId>(4);
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
            if (!bodyMap.TryGetValue(sensor.Frame, out var frame) || frame.Motion != RigidMotionKind.Static || !faceOwned ||
                !loadFrames.Add(sensor.Frame))
                throw new ArgumentException("A contact-load row requires one row per static frame and a sensing face that frame owns.");
        }
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
        var spanIds = new HashSet<RopeSpanId>();
        var routeOrders = new HashSet<(RopeRouteId, uint)>();
        foreach (var span in tensileSpans)
        {
            span.Validate();
            if (!spanIds.Add(span.Id) || !routeOrders.Add((span.Route, span.Order)) ||
                !bodyMap.TryGetValue(span.BodyA, out var a) || !bodyMap.TryGetValue(span.BodyB, out var b) ||
                a.Motion is not (RigidMotionKind.Static or RigidMotionKind.Dynamic) ||
                b.Motion is not (RigidMotionKind.Static or RigidMotionKind.Dynamic))
                throw new ArgumentException("Tensile span requires unique identities and admitted participants.");
            if (span.Complete && a.Motion == RigidMotionKind.Static && b.Motion == RigidMotionKind.Static &&
                System.Numerics.Vector3.Distance(Attachment(a, span.LocalA), Attachment(b, span.LocalB)) > span.MaximumLength)
                throw new ArgumentException("An immovable rope exceeds its authored length.");
        }
        _tensileSpans = tensileSpans.ToArray();
        Array.Sort(_tensileSpans, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        var orderedRoutes = new Dictionary<RopeRouteId, SortedDictionary<uint, TensileSpanDeclaration>>();
        foreach (var span in _tensileSpans)
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
                    (prior.BodyB != span.BodyA || prior.LocalB != span.LocalA)) ||
                    !visited.Add(span.BodyA))
                    throw new ArgumentException("Tensile route must be one ordered acyclic chain.");
                previous = span;
            }
            if (previous is { } last && !visited.Add(last.BodyB)) throw new ArgumentException("Tensile route closes a cycle.");
        }
        _prismatics = prismatics.ToArray();
        Array.Sort(_prismatics, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        _revolutes = revolutes.ToArray();
        Array.Sort(_revolutes, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
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
        foreach (var source in electricalSources) Identity(source.Id.Value, nextIdentity, ids);
        Electrical = new(electricalSources, electricalBindings, bodies, contactWorks);
        SupplyGraph = new(supplyNodes, supplyEdges, Electrical.Sources, _prismatics, _prismaticDrives, bodies);
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
            Electrical.Bindings.Length, _prismatics.Length, _contactLoadSensors.Length, _tensileSpans.Length, _revolutes.Length);
        _ = World.ByteLength;
        Layout = CompiledMemoryLayout.Build(World, _bodies, _colliders, _materials, _sensors, _guides, _triggers, _contactWorks, _orientationSensors, Electrical.Sources, _prismatics, _revolutes);
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

internal static class PhysicsDeclarationBounds
{
    /// <summary>Admits a finite f32 value inside the closed declared range, compared at the exact f32 bounds.</summary>
    internal static void Range(float value, float minimum, float maximum)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentException("Physical declaration exceeds its admitted domain.");
    }
    internal static void Vector(float x, float y, float z, float magnitude)
    {
        Range(x, -magnitude, magnitude);
        Range(y, -magnitude, magnitude);
        Range(z, -magnitude, magnitude);
    }
    /// <summary>Rejects a non-finite f32 vector or one whose magnitude exceeds the bound (wide arithmetic, admission only).</summary>
    internal static void Magnitude(float x, float y, float z, float maximum)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) ||
            x * x + y * y + z * z > maximum * maximum)
            throw new ArgumentException("Physical vector is non-finite or exceeds its admitted magnitude.");
    }
    /// <summary>Canonical positive zero: the all-zero f32 bit pattern (−0 is not zero here).</summary>
    internal static bool Zero(float value) => BitConverter.SingleToUInt32Bits(value) == 0;
    internal static bool Zero(float x, float y, float z) => Zero(x) && Zero(y) && Zero(z);
}
