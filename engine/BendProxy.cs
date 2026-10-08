using Godot;

namespace CuriousContraptions;

/// <summary>Finite hollow circular bend in XY. Centreline is R*(sin(a),cos(a),0), 0..Sweep.</summary>
public readonly record struct BendProxy(Transform3D Pose, float Radius, float Sweep, float InnerRadius, float OuterRadius)
{
    public Vector3 Centre(float angle) => new(Mathf.Sin(angle) * Radius, Mathf.Cos(angle) * Radius, 0);
    public static Vector3 Radial(float angle) => new(Mathf.Sin(angle), Mathf.Cos(angle), 0);
    public static Vector3 Tangent(float angle) => new(Mathf.Cos(angle), -Mathf.Sin(angle), 0);

}
