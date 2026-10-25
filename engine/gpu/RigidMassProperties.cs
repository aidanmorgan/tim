using System;

namespace CuriousContraptions.Gpu;

/// <summary>Immutable compiled SI principal moment in kg m², one IEEE-754 f32.</summary>
public readonly record struct PrincipalInertia(float Moment)
{
    /// <summary>The compiled domain: the smallest admitted box (1/1024 kg, 1/1024 m half-extents) is above 2^-32 kg m², and the
    /// largest admitted body (1024 kg, 16 m half-extents) is below 2^19 kg m².</summary>
    public const float Minimum = 1f / 4294967296f;
    public const float Maximum = 524288f;

    public void Validate()
    {
        if (!float.IsFinite(Moment) || Moment < Minimum || Moment >= Maximum)
            throw new ArgumentException("Principal inertia exceeds the compiled domain.");
    }
}

/// <summary>Homogeneous primitive mass properties in the authored body frame.</summary>
public readonly record struct RigidMassProperties(MetreVector LocalCentreOfMass,
    CanonicalRotation PrincipalFrame, PrincipalInertia X, PrincipalInertia Y, PrincipalInertia Z)
{
    public void Validate()
    {
        PhysicsDeclarationBounds.Vector(LocalCentreOfMass.X, LocalCentreOfMass.Y, LocalCentreOfMass.Z, 16f);
        PrincipalFrame.Validate(); X.Validate(); Y.Validate(); Z.Validate();
    }

    /// <summary>The f32 closed forms of a homogeneous primitive's principal moments about its own frame. Each is at most a few f32
    /// operations, so its relative error is within 6 · 2^-24 of the exact moment of the declared f32 inputs.</summary>
    public static RigidMassProperties Compile(RigidBodyDeclaration body, ColliderDeclaration collider)
    {
        body.Validate(); collider.Validate();
        if (body.Motion != RigidMotionKind.Dynamic || collider.Body != body.Id ||
            collider.Shape is not (ColliderShapeKind.Sphere or ColliderShapeKind.Box or ColliderShapeKind.Cylinder))
            throw new ArgumentException("Mass properties require one dynamic homogeneous primitive.");
        var mass = body.Mass.Value;
        float x, y, z;
        if (collider.Shape == ColliderShapeKind.Sphere)
        {
            // Solid sphere: (2/5) m r².
            var radius = collider.Radius.Value;
            x = y = z = 0.4f * mass * (radius * radius);
        }
        else if (collider.Shape == ColliderShapeKind.Cylinder)
        {
            // Solid cylinder about its own frame (axis local +Y, half-width h = w/2): axial m r²/2, transverse
            // m (3 r² + w²)/12 = m (r²/4 + h²/3).
            var radius = collider.Radius.Value;
            var half = collider.HalfExtents.Y;
            var squared = radius * radius;
            y = 0.5f * mass * squared;
            x = z = mass * (0.25f * squared + half * half / 3f);
        }
        else
        {
            // Solid box of half-extents (a, b, c): m (b² + c²)/3 about X, and cyclically.
            var a = collider.HalfExtents.X;
            var b = collider.HalfExtents.Y;
            var c = collider.HalfExtents.Z;
            var thirdMass = mass / 3f;
            x = thirdMass * (b * b + c * c);
            y = thirdMass * (a * a + c * c);
            z = thirdMass * (a * a + b * b);
        }
        var result = new RigidMassProperties(collider.Pose.Translation, collider.Pose.Rotation, new(x), new(y), new(z));
        result.Validate(); return result;
    }
}
