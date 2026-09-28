using Godot;

namespace CuriousContraptions;

/// <summary>A finite hollow cylinder along local X, including its annular end faces.</summary>
public readonly record struct TubeProxy(Transform3D Pose, float HalfLength, float InnerRadius, float OuterRadius, bool Opaque)
{
    public (Vector3 Normal, float Distance) Surface(Vector3 point)
    {
        var radial = new Vector3(0, point.Y, point.Z);
        var radius = radial.Length();
        var direction = radius > .000001f ? radial / radius : Vector3.Up;
        var closest = new Vector3(Mathf.Clamp(point.X, -HalfLength, HalfLength), 0, 0)
            + direction * Mathf.Clamp(radius, InnerRadius, OuterRadius);
        var offset = point - closest;
        var distance = offset.Length();
        if (distance > .000001f) return (offset / distance, distance);
        var inner = radius - InnerRadius;
        var outer = OuterRadius - radius;
        var end = HalfLength - Mathf.Abs(point.X);
        if (end < inner && end < outer) return (point.X >= 0 ? Vector3.Right : Vector3.Left, -end);
        return inner < outer ? (-direction, -inner) : (direction, -outer);
    }

}
