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

    public float RayDistance(Vector3 origin, Vector3 ray, float maximum)
    {
        var radialSquared = origin.Y * origin.Y + origin.Z * origin.Z;
        if (Mathf.Abs(origin.X) <= HalfLength && radialSquared >= InnerRadius * InnerRadius
            && radialSquared <= OuterRadius * OuterRadius) return 0;
        var nearest = maximum;
        var innerSquared = InnerRadius * InnerRadius;
        var outerSquared = OuterRadius * OuterRadius;
        var halfLength = HalfLength;
        void Candidate(float t, bool end)
        {
            if (t < 0 || t >= nearest) return;
            var point = origin + ray * t;
            var radiusSquared = point.Y * point.Y + point.Z * point.Z;
            if (end ? radiusSquared >= innerSquared - .00001f && radiusSquared <= outerSquared + .00001f
                : Mathf.Abs(point.X) <= halfLength + .00001f) nearest = t;
        }
        if (Mathf.Abs(ray.X) > .000001f)
        {
            Candidate((HalfLength - origin.X) / ray.X, true);
            Candidate((-HalfLength - origin.X) / ray.X, true);
        }
        var a = ray.Y * ray.Y + ray.Z * ray.Z;
        if (a > .000001f)
        {
            var b = origin.Y * ray.Y + origin.Z * ray.Z;
            foreach (var radius in new[] { InnerRadius, OuterRadius })
            {
                var discriminant = b * b - a * (radialSquared - radius * radius);
                if (discriminant < 0) continue;
                var root = Mathf.Sqrt(discriminant);
                Candidate((-b - root) / a, false);
                Candidate((-b + root) / a, false);
            }
        }
        return nearest;
    }
}
