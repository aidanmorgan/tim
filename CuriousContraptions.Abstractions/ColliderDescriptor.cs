using System.Numerics;

namespace CuriousContraptions.Abstractions;

public enum ColliderShapeKind : byte
{
    Sphere = 0,
    Box = 1,
    Plane = 2,
    Cylinder = 3,
    Capsule = 4,
    Mesh = 5,
    Tube = 6,
    Torus = 7,
    Frustum = 8
}

public readonly record struct MaterialProperties(
    float Restitution,
    float Friction,
    float RollingResistance,
    float BounceThreshold);

/// <summary>One collider in the element's local frame. Its lanes, by kind (engine-hollow-shapes, owner decision "true hollow shapes",
/// 25 Oct 2026):
/// <list type="bullet">
/// <item>Sphere: <see cref="HalfExtents"/> (r, r, r). Box: the half sizes. Cylinder: (r, h, r) about local +Y. <see cref="Radius"/> is
/// zero for all three.</item>
/// <item>Tube (an annular cylinder about local +Y): <see cref="Radius"/> the outer radius r_o; <see cref="HalfExtents"/> (the inner
/// radius r_i, the half-length h, 0).</item>
/// <item>Frustum (a tapered tube wall about local +Y): <see cref="Radius"/> the outer radius r_0 at −h; <see cref="HalfExtents"/>
/// (the radial wall thickness t, the half-length h, the outer radius r_1 at +h). The inner radii are r_0 − t and r_1 − t.</item>
/// <item>Torus (a torus segment about local +Y, its centreline R (cos a, 0, sin a) for a in [0, θ]): <see cref="Radius"/> the
/// centreline radius R; <see cref="HalfExtents"/> (the tube's inner radius r_i, zero for a solid tube; its outer radius r_o; the
/// sweep θ in radians, 2π for a full torus).</item>
/// </list></summary>
public readonly record struct ColliderDescriptor(
    ColliderShapeKind ShapeKind,
    Vector3 HalfExtents,
    Vector3 Offset,
    Quaternion Rotation,
    MaterialProperties Material,
    float Radius)
{
    /// <summary>Whether the kind is one of the true hollow kinds (Tube, Torus, Frustum).</summary>
    public static bool Hollow(ColliderShapeKind kind) => kind is ColliderShapeKind.Tube or ColliderShapeKind.Torus or ColliderShapeKind.Frustum;
}
