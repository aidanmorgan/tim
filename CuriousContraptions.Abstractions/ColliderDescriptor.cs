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

public readonly record struct ColliderDescriptor(
    ColliderShapeKind ShapeKind,
    Vector3 Dimensions,
    Vector3 Offset,
    Quaternion Rotation,
    MaterialProperties Material);
