using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Pure cosmetic polyline evaluation. No forces or simulated state are written.</summary>
public static class RopeGeometry
{
    public const int Segments = 24;
    public static Vector3[] Evaluate(Vector3 from, Vector3 to, float maximum, RopeState state)
    {
        if (!Finite(from) || !Finite(to) || !float.IsFinite(maximum) || maximum < .05f || maximum > 200f || !Enum.IsDefined(state))
            throw new ArgumentException("Invalid rope artwork input.");
        var points = new Vector3[Segments + 1];
        var chord = to - from;
        var distance = chord.Length();
        if (state != RopeState.Slack || maximum <= distance)
        {
            Fill(0f, Vector3.Zero);
            return points;
        }
        var direction = -Vector3.UnitY;
        if (distance > 0f)
        {
            var axis = chord / distance;
            direction -= axis * Vector3.Dot(direction, axis);
            if (direction.LengthSquared() < .0001f) direction = Vector3.UnitX;
            else direction = Vector3.Normalize(direction);
        }
        var low = 0f; var high = maximum;
        // Bisection is bounded cosmetic tessellation, not a physics tolerance or population limit.
        for (var iteration = 0; iteration < 24; iteration++)
        {
            var middle = (low + high) * .5f;
            Fill(middle, direction);
            var length = 0f;
            for (var i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
            if (length < maximum) low = middle; else high = middle;
        }
        Fill((low + high) * .5f, direction);
        return points;

        void Fill(float sag, Vector3 down)
        {
            for (var i = 0; i < points.Length; i++)
            {
                var t = (float)i / Segments;
                points[i] = Vector3.Lerp(from, to, t) + down * (sag * 4f * t * (1f - t));
            }
        }
    }
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
