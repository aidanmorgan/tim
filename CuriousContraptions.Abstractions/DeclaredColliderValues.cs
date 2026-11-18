using System;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>A baked collider admitted into the runtime's physical lanes.</summary>
public readonly record struct ExtractedCollider(
    ColliderShapeKind ShapeKind,
    RigidLocalPose LocalPose,
    float Radius,
    MetreVector HalfExtents)
{
    public CollisionParticipation Participation { get; init; } = CollisionParticipation.Enabled;
}
