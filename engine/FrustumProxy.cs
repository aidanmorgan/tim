using Godot;

namespace CuriousContraptions;

/// <summary>Annular frustum declaration along local X; collision is supplied by shared compound geometry.</summary>
public readonly record struct FrustumProxy(Transform3D Pose,float HalfLength,float InletRadius,float OutletRadius,float Thickness);
