using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Actual outer material support, shared by chamber extents and continuous plane clearance.</summary>
public static class MaterialSupport
{
    public static Vector3 Point(HollowPartner shape, Vector3 direction)
    {
        if (!(direction.LengthSquared() > 0f) || !float.IsFinite(direction.LengthSquared()))
            throw new ArgumentException("Support direction must be finite and nonzero.", nameof(direction));
        if (HollowSolid.IsHollow(shape.Shape))
            return new HollowSolid(shape.Shape, shape.Position, shape.U0, shape.U1, shape.U2,
                shape.Radius, shape.Hx, shape.Hy, shape.Hz).SupportNear(direction, shape.Position);
        var dx = Vector3.Dot(direction, shape.U0);
        var dy = Vector3.Dot(direction, shape.U1);
        var dz = Vector3.Dot(direction, shape.U2);
        return shape.Shape switch
        {
            ColliderShapeKind.Sphere => shape.Position + shape.Radius * Vector3.Normalize(direction),
            ColliderShapeKind.Box => shape.Position + Sign(dx) * shape.Hx * shape.U0 +
                Sign(dy) * shape.Hy * shape.U1 + Sign(dz) * shape.Hz * shape.U2,
            ColliderShapeKind.Cylinder => Cylinder(shape, dx, dy, dz),
            _ => throw new ArgumentException("Support requires a finite collider.", nameof(shape))
        };
    }

    private static Vector3 Cylinder(HollowPartner shape, float dx, float dy, float dz)
    {
        var radial = dx * shape.U0 + dz * shape.U2;
        if (radial.LengthSquared() > 0f) radial = Vector3.Normalize(radial);
        return shape.Position + Sign(dy) * shape.Hy * shape.U1 + shape.Radius * radial;
    }

    private static float Sign(float value) => value > 0f ? 1f : value < 0f ? -1f : 0f;
}
