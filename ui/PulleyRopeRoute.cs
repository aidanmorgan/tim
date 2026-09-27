using Godot;

namespace CuriousContraptions;

public enum RopeWinding { Clockwise = -1, CounterClockwise = 1 }

/// <summary>Presentation geometry for a rope seated outside a pulley rim.
/// The fixed-point constraint solver remains separate from this finite-radius artwork.</summary>
public readonly record struct PulleyRopeRoute(Transform3D Transform, float StartAngle, float Sweep)
{
    // Wheel radius .4 + cream rim .035 + rope thickness .035, with chord clearance.
    public const float Radius = RopeGeometry.PulleyRadius + .08f;
    public Vector3 Point(float t) => Transform * new Vector3(
        Mathf.Cos(StartAngle + Sweep * t) * Radius,
        Mathf.Sin(StartAngle + Sweep * t) * Radius, RopeGeometry.PulleySocketDepth);

    public static PulleyRopeRoute Create(Transform3D transform, Vector3 from, Vector3 to, RopeWinding winding)
    {
        var inverse = transform.AffineInverse();
        var a = inverse * from;
        var b = inverse * to;
        var sign = (int)winding;
        float Tangent(Vector3 p, int direction)
        {
            var distance = new Vector2(p.X, p.Y).Length();
            // A projection inside the rim meets its nearest radial point. Such
            // out-of-plane approaches are not a physical grooved-wheel model.
            var opening = Mathf.Acos(Mathf.Clamp(Radius / Mathf.Max(distance, .00001f), 0, 1));
            return Mathf.Atan2(p.Y, p.X) + direction * opening;
        }
        var start = Tangent(a, sign);
        var end = Tangent(b, -sign);
        var sweep = sign * Mathf.PosMod(sign * (end - start), Mathf.Tau);
        return new(transform, start, sweep);
    }

    public static RopeWinding Choose(Transform3D transform, Vector3 from, Vector3 to)
    {
        var clockwise = Create(transform, from, to, RopeWinding.Clockwise);
        var counter = Create(transform, from, to, RopeWinding.CounterClockwise);
        float MeanHeight(PulleyRopeRoute route) => Mathf.Abs(route.Sweep) < .00001f
            ? Mathf.Sin(route.StartAngle)
            : (Mathf.Cos(route.StartAngle) - Mathf.Cos(route.StartAngle + route.Sweep)) / route.Sweep;
        // Thread over the local upper rim, including when a nearby load's
        // projected tie starts inside the rim. Retain that side as it moves.
        var heightDifference = MeanHeight(clockwise) - MeanHeight(counter);
        if (Mathf.Abs(heightDifference) > .0001f)
            return heightDifference > 0 ? RopeWinding.Clockwise : RopeWinding.CounterClockwise;
        return Mathf.Abs(clockwise.Sweep) <= Mathf.Abs(counter.Sweep)
            ? RopeWinding.Clockwise : RopeWinding.CounterClockwise;
    }
}
