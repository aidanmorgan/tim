using System.Numerics;
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
public readonly record struct CompliantSurfaceId(ulong Value);
public readonly record struct MechanicalTransmissionId(ulong Value);
public readonly record struct MechanicalTransmissionDeclaration(
    MechanicalTransmissionId Id,
    RevoluteConstraintId DrivingJoint,
    RevoluteConstraintId DrivenJoint,
    float Ratio)
{
    public void Validate()
    {
        if (Id.Value == 0) throw new ArgumentException("Mechanical transmission identity is required.");
        if (DrivingJoint.Value == 0 || DrivenJoint.Value == 0 || DrivingJoint == DrivenJoint)
            throw new ArgumentException("A mechanical transmission requires distinct admitted revolute joints.");
        if (!float.IsFinite(Ratio) || Ratio == 0f)
            throw new ArgumentException("Transmission ratio must be finite and nonzero.");
    }


}

/// <summary>A compiled compliant surface. <see cref="ArrivalDepth"/> is its declared arrival depth a (finite, 0 &lt; a &lt; MaxStroke / 2).</summary>
public readonly record struct CompliantSurfaceDeclaration(
    CompliantSurfaceId Id,
    GpuBodyId Body,
    float HalfX,
    float HalfZ,
    float RestHeight,
    float MaxStroke,
    float ArrivalDepth,
    float Tension,
    float DampingRatio)
{
    public void Validate()
    {
        if (Id.Value == 0) throw new ArgumentException("Compliant surface identity is required.");
        PhysicsDeclarationBounds.Range(HalfX, 1f / 1024f, 16f);
        PhysicsDeclarationBounds.Range(HalfZ, 1f / 1024f, 16f);
        PhysicsDeclarationBounds.Range(RestHeight, -16f, 16f);
        PhysicsDeclarationBounds.Range(MaxStroke, 1f / 1024f, 16f);
        if (!float.IsFinite(ArrivalDepth) || ArrivalDepth <= 0f || ArrivalDepth >= MaxStroke / 2f)
            throw new ArgumentException("A compliant surface's arrival depth must be finite, positive and under half its stroke.");
        PhysicsDeclarationBounds.Range(Tension, 120f, 1200f);
        PhysicsDeclarationBounds.Range(DampingRatio, 0.08f, 0.8f);
    }
}

/// <summary>The committed contact state of one support sample of one dynamic body on one compliant surface (CAT-065 §2, Story 10.5
/// review loop 4). <see cref="Outside"/>: not over the bed (the frame carries it). <see cref="OnTop"/>: it reached the footprint from
/// above the rest plane, or crossed its edge no deeper than the arrival depth, and bears the bed while over it and below the plane.
/// <see cref="Under"/>: it entered the footprint deeper than the arrival depth and is never pushed until it rises to the plane. Zero is
/// Outside, so every sample starts Outside at Run and Reset and classifies itself on the first update.</summary>
public enum CompliantSampleState : uint { Outside = 0, OnTop = 1, Under = 2 }

/// <summary>One committed support sample on a compliant surface: its contact state and its offset, the depth below the rest plane the
/// bed has not been paid for (the bed pushes with (k/4)·(d − offset)); the offset is zero unless the sample is OnTop.</summary>
public readonly record struct CompliantSampleRead(CompliantSampleState State, float Offset);
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
public static class PhysicsDeclarationBounds
{
    /// <summary>Admits a finite f32 value inside the closed declared range, compared at the exact f32 bounds.</summary>
    public static void Range(float value, float minimum, float maximum)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentException("Physical declaration exceeds its admitted domain.");
    }
    public static void Vector(float x, float y, float z, float magnitude)
    {
        Range(x, -magnitude, magnitude);
        Range(y, -magnitude, magnitude);
        Range(z, -magnitude, magnitude);
    }
    /// <summary>Rejects a non-finite f32 vector or one whose magnitude exceeds the bound (wide arithmetic, admission only).</summary>
    public static void Magnitude(float x, float y, float z, float maximum)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) ||
            x * x + y * y + z * z > maximum * maximum)
            throw new ArgumentException("Physical vector is non-finite or exceeds its admitted magnitude.");
    }
    /// <summary>Canonical positive zero: the all-zero f32 bit pattern (−0 is not zero here).</summary>
    public static bool Zero(float value) => BitConverter.SingleToUInt32Bits(value) == 0;
    public static bool Zero(float x, float y, float z) => Zero(x) && Zero(y) && Zero(z);
}

public readonly record struct DrivenSurfaceId(ulong Value);

public readonly record struct DrivenSurfaceDeclaration(
    DrivenSurfaceId Id, GpuBodyId Carrier, GpuColliderId Collider, BoxFace Face,
    RevoluteConstraintId Source, Vector3 LocalDirection, float TravelPerRadian)
{
    public void Validate()
    {
        if (Id.Value == 0 || Carrier.Value == 0 || Collider.Value == 0 || Source.Value == 0 || !Enum.IsDefined(Face))
            throw new ArgumentException("Invalid driven surface identity or face.");
        var length = LocalDirection.LengthSquared();
        if (!float.IsFinite(length) || MathF.Abs(length - 1f) > 1e-4f ||
            !float.IsFinite(TravelPerRadian) || TravelPerRadian == 0f)
            throw new ArgumentException("Driven surface requires a unit tangent and finite nonzero travel.");
        // A contact has at most six bodies and two surface contributions. Bound its
        // physical wrench using the admitted 16 m geometry/COM domain and minimum
        // principal inertia, so neither the unnormalised mass nor its cross terms overflow.
        var angularBound = 48f * MathF.Sqrt(3f) + 2f * MathF.Abs(TravelPerRadian);
        var massBound = 6f * angularBound * angularBound / PrincipalInertia.Minimum;
        // The existing eigensolve also multiplies diagonal mass entries.
        if (!float.IsFinite(massBound * massBound))
            throw new ArgumentException("Driven surface coupled inertia is not representable.");
    }


}
