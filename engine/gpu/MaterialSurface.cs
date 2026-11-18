using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Analytic material boundary patches and derivative bounds for finite admitted collider shapes.</summary>
internal readonly struct MaterialSurface(HollowPartner shape, int face)
{
    public static int Count(HollowPartner s) => s.Shape switch
    {
        ColliderShapeKind.Sphere => 1,
        ColliderShapeKind.Box => 6,
        ColliderShapeKind.Cylinder => 3,
        ColliderShapeKind.Tube or ColliderShapeKind.Frustum => 4,
        ColliderShapeKind.Torus => s.Hz == ColliderDeclaration.FullSweep ? 2 : 4,
        _ => throw new ArgumentException("A material surface must be finite.", nameof(s))
    };

    public Vector2 DerivativeBounds
    {
        get
        {
            var s = shape;
            if (s.Shape == ColliderShapeKind.Box)
            {
                var sizes = new Vector3(s.Hx, s.Hy, s.Hz);
                return (face / 2) switch { 0 => new(2f * sizes.Y, 2f * sizes.Z),
                    1 => new(2f * sizes.X, 2f * sizes.Z), _ => new(2f * sizes.X, 2f * sizes.Y) };
            }
            if (s.Shape == ColliderShapeKind.Sphere) return new(MathF.Tau * s.Radius, MathF.PI * s.Radius);
            if (s.Shape == ColliderShapeKind.Torus)
            {
                var sweep = s.Hz == ColliderDeclaration.FullSweep ? MathF.Tau : s.Hz;
                return face < 2 ? new(sweep * (s.Radius + s.Hy), MathF.Tau * (face == 0 ? s.Hy : s.Hx))
                    : new(MathF.Tau * s.Hy, s.Hy - s.Hx);
            }
            var top = s.Shape == ColliderShapeKind.Frustum ? s.Hz : s.Radius;
            var maximum = MathF.Max(s.Radius, top);
            var cap = s.Shape == ColliderShapeKind.Cylinder ? face > 0 : face > 1;
            return new(MathF.Tau * maximum, cap ? maximum : MathF.Sqrt(4f * s.Hy * s.Hy + (top - s.Radius) * (top - s.Radius)));
        }
    }

    public Vector3 Point(float u, float v)
    {
        var s = shape;
        if (s.Shape == ColliderShapeKind.Box)
        {
            var a = 2f * u - 1f; var b = 2f * v - 1f; var sign = face % 2 == 0 ? -1f : 1f;
            return s.Position + ((face / 2) switch
            {
                0 => sign * s.Hx * s.U0 + a * s.Hy * s.U1 + b * s.Hz * s.U2,
                1 => a * s.Hx * s.U0 + sign * s.Hy * s.U1 + b * s.Hz * s.U2,
                _ => a * s.Hx * s.U0 + b * s.Hy * s.U1 + sign * s.Hz * s.U2
            });
        }
        if (s.Shape == ColliderShapeKind.Sphere)
        {
            var latitude = MathF.PI * (v - .5f);
            return s.Position + s.Radius * (MathF.Cos(latitude) * Radial(MathF.Tau * u) + MathF.Sin(latitude) * s.U1);
        }
        if (s.Shape == ColliderShapeKind.Torus)
        {
            var sweep = s.Hz == ColliderDeclaration.FullSweep ? MathF.Tau : s.Hz;
            var azimuth = face < 2 ? sweep * u : face == 2 ? 0f : sweep;
            var profile = MathF.Tau * (face < 2 ? v : u);
            var tube = face < 2 ? face == 0 ? s.Hy : s.Hx : s.Hx + v * (s.Hy - s.Hx);
            return s.Position + (s.Radius + tube * MathF.Cos(profile)) * Radial(azimuth) + tube * MathF.Sin(profile) * s.U1;
        }
        var top = s.Shape == ColliderShapeKind.Frustum ? s.Hz : s.Radius;
        var innerBottom = s.Shape == ColliderShapeKind.Frustum ? s.Radius - s.Hx : s.Hx;
        var innerTop = s.Shape == ColliderShapeKind.Frustum ? top - s.Hx : s.Hx;
        float radius, height;
        if (s.Shape == ColliderShapeKind.Cylinder)
        {
            radius = face == 0 ? s.Radius : v * s.Radius;
            height = face == 0 ? (2f * v - 1f) * s.Hy : face == 1 ? -s.Hy : s.Hy;
        }
        else if (face < 2)
        {
            radius = face == 0 ? s.Radius + v * (top - s.Radius) : innerBottom + v * (innerTop - innerBottom);
            height = (2f * v - 1f) * s.Hy;
        }
        else
        {
            var inner = face == 2 ? innerBottom : innerTop;
            radius = inner + v * ((face == 2 ? s.Radius : top) - inner);
            height = face == 2 ? -s.Hy : s.Hy;
        }
        return s.Position + radius * Radial(MathF.Tau * u) + height * s.U1;
    }

    private Vector3 Radial(float angle) => MathF.Cos(angle) * shape.U0 + MathF.Sin(angle) * shape.U2;
}
