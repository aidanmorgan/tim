using System.Numerics;

namespace CuriousContraptions.Abstractions;

public enum ColliderShapeKind : byte
{
    Sphere = 0,
    Box = 1,
    Plane = 2,
    Cylinder = 3,
    Capsule = 4,
    Mesh = 5
}

public readonly record struct MaterialProperties(
    float Restitution,
    float Friction,
    float RollingResistance,
    float BounceThreshold);

/// <summary>One collider in the element's local frame. A box's half-extents are its half sizes; a sphere's radius is its
/// half-extent on every axis.</summary>
public readonly record struct ColliderDescriptor(
    ColliderShapeKind ShapeKind,
    Vector3 HalfExtents,
    Vector3 Offset,
    Quaternion Rotation,
    MaterialProperties Material);
