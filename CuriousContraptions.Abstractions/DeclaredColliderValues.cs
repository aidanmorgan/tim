using System;

namespace CuriousContraptions.Gpu;

/// <summary>A baked collider admitted into the runtime's physical lanes.</summary>
public readonly record struct ExtractedCollider(
    ColliderShapeKind ShapeKind,
    RigidLocalPose LocalPose,
    float Radius,
    MetreVector HalfExtents);
