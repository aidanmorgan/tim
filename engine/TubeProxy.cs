using Godot;

namespace CuriousContraptions;

/// <summary>A finite hollow cylinder along local X, including its annular end faces.</summary>
public readonly record struct TubeProxy(Transform3D Pose, float HalfLength, float InnerRadius, float OuterRadius, bool Opaque);
