using Godot;

namespace CuriousContraptions;

/// <summary>Finite hollow circular bend in XY. Centreline is R*(sin(a),cos(a),0), 0..Sweep.</summary>
public readonly record struct BendProxy(Transform3D Pose, float Radius, float Sweep, float InnerRadius, float OuterRadius)
{
    public Vector3 Centre(float angle) => new(Mathf.Sin(angle) * Radius, Mathf.Cos(angle) * Radius, 0);
    public static Vector3 Radial(float angle) => new(Mathf.Sin(angle), Mathf.Cos(angle), 0);
    public static Vector3 Tangent(float angle) => new(Mathf.Cos(angle), -Mathf.Sin(angle), 0);

    private Vector3 EndClosest(Vector3 point, float angle)
    {
        var radial = Radial(angle);
        var offset = point - Centre(angle);
        var cross = radial * offset.Dot(radial) + Vector3.Back * point.Z;
        var radius = cross.Length();
        var direction = radius > .000001f ? cross / radius : radial;
        return Centre(angle) + direction * Mathf.Clamp(radius, InnerRadius, OuterRadius);
    }

    public (Vector3 Normal, float Distance) Surface(Vector3 point)
    {
        var angle = Mathf.Atan2(point.X, point.Y);
        var insideAngle = angle >= 0 && angle <= Sweep;
        var start = EndClosest(point, 0);
        var end = EndClosest(point, Sweep);
        if (!insideAngle)
        {
            var closest = point.DistanceSquaredTo(start) <= point.DistanceSquaredTo(end) ? start : end;
            var offset = point - closest;
            var distance = offset.Length();
            return (distance > .000001f ? offset / distance : -Tangent(0), distance);
        }
        var radial = Radial(angle);
        var cross = point - Centre(angle);
        var radius = cross.Length();
        var direction = radius > .000001f ? cross / radius : radial;
        if (radius < InnerRadius) return (-direction, InnerRadius - radius);
        if (radius > OuterRadius) return (direction, radius - OuterRadius);
        // Inside the solid shell: choose the shortest exit, including either annular end.
        var inner = radius - InnerRadius;
        var outer = OuterRadius - radius;
        var normal = inner < outer ? -direction : direction;
        var depth = Mathf.Min(inner, outer);
        foreach (var closest in new[] { start, end })
        {
            var offset = closest - point;
            var distance = offset.Length();
            if (distance < depth && distance > .000001f) { depth = distance; normal = offset / distance; }
        }
        return (normal, -depth);
    }
}
