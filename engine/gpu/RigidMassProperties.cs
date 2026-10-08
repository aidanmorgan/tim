using System;

namespace CuriousContraptions.Gpu;

/// <summary>Immutable compiled SI moment: Mantissa * 2^Exponent kg m².</summary>
public readonly record struct PrincipalInertia(Half Mantissa, int Exponent)
{
    public void Validate()
    {
        if (!Half.IsFinite(Mantissa) || Mantissa < (Half).5 || Mantissa >= (Half)1 ||
            Exponent is < -31 or > 19)
            throw new ArgumentException("Principal inertia exceeds the compiled domain.");
    }
}

/// <summary>Homogeneous primitive mass properties in the authored body frame.</summary>
public readonly record struct RigidMassProperties(MetreVector LocalCentreOfMass,
    CanonicalRotation PrincipalFrame, PrincipalInertia X, PrincipalInertia Y, PrincipalInertia Z)
{
    public void Validate()
    {
        PhysicsDeclarationBounds.Vector(LocalCentreOfMass.X, LocalCentreOfMass.Y, LocalCentreOfMass.Z, (Half)16);
        PrincipalFrame.Validate(); X.Validate(); Y.Validate(); Z.Validate();
    }

    public static RigidMassProperties Compile(RigidBodyDeclaration body, ColliderDeclaration collider)
    {
        body.Validate(); collider.Validate();
        if (body.Motion != RigidMotionKind.Dynamic || collider.Body != body.Id ||
            collider.Shape is not (ColliderShapeKind.Sphere or ColliderShapeKind.Box))
            throw new ArgumentException("Mass properties require one dynamic homogeneous primitive.");
        var mass = Normalize(body.Mass.Value, 0);
        PrincipalInertia x, y, z;
        if (collider.Shape == ColliderShapeKind.Sphere)
        {
            var radius = Normalize(collider.Radius.Value, 0);
            x = y = z = Product(Product(mass, Product(radius, radius)), Normalize((Half).4, 0));
        }
        else
        {
            var a = Normalize(collider.HalfExtents.X, 0);
            var b = Normalize(collider.HalfExtents.Y, 0);
            var c = Normalize(collider.HalfExtents.Z, 0);
            var thirdMass = Product(mass, Normalize((Half)(1.0 / 3), 0));
            x = Product(thirdMass, Sum(Product(b, b), Product(c, c)));
            y = Product(thirdMass, Sum(Product(a, a), Product(c, c)));
            z = Product(thirdMass, Sum(Product(a, a), Product(b, b)));
        }
        var result = new RigidMassProperties(collider.Pose.Translation, collider.Pose.Rotation, x, y, z);
        result.Validate(); return result;
    }

    // Compilation uses canonical Half operations. Integer shifts extend range, not precision.
    // These helpers derive immutable moments only; they are not an evolving physical model.
    private static PrincipalInertia Normalize(Half mantissa, int exponent)
    {
        if (!Half.IsFinite(mantissa) || mantissa <= (Half)0)
            throw new ArgumentException("Inertia requires a positive finite operand.");
        while (mantissa < (Half).5) { mantissa = (Half)(mantissa * (Half)2); exponent--; }
        while (mantissa >= (Half)1) { mantissa = (Half)(mantissa * (Half).5); exponent++; }
        return new(mantissa, exponent);
    }
    private static PrincipalInertia Product(PrincipalInertia a, PrincipalInertia b) =>
        Normalize((Half)(a.Mantissa * b.Mantissa), checked(a.Exponent + b.Exponent));

    private static PrincipalInertia Sum(PrincipalInertia a, PrincipalInertia b)
    {
        if (a.Exponent < b.Exponent) (a, b) = (b, a);
        var small = b.Mantissa;
        for (var shift = b.Exponent; shift < a.Exponent; shift++)
            small = (Half)(small * (Half).5);
        return Normalize((Half)(a.Mantissa + small), a.Exponent);
    }
}
