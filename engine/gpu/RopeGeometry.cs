using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public enum RopeWinding { Clockwise = -1, CounterClockwise = 1 }

public readonly record struct PulleyArc(
    Vector3 Entry,
    Vector3 Exit,
    float StartAngle,
    float Sweep,
    float Length);

public readonly record struct EvaluatedRopeRoute(
    Vector3 TangentA,
    Vector3 TangentB,
    Vector3 UnitA,
    Vector3 UnitB,
    PulleyArc[] Arcs,
    float TotalLength);

/// <summary>Pure cosmetic and kinematic polyline evaluation.</summary>
public static class RopeGeometry
{
    public const int Segments = 24;
    public const float PulleyRadius = 0.4f;
    public const float PulleySocketDepth = 0.12f;

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

    public static EvaluatedRopeRoute EvaluateRoute(
        Vector3 terminalA,
        Vector3 terminalB,
        ReadOnlySpan<(Vector3 Position, Quaternion Rotation)> guides)
    {
        if (guides.Length == 0)
        {
            var delta = terminalB - terminalA;
            var dist = delta.Length();
            var dir = dist > 1e-6f ? delta / dist : Vector3.UnitY;
            return new(terminalB, terminalA, dir, -dir, [], dist);
        }

        var k = guides.Length;
        var centers = new Vector3[k];
        var axisX = new Vector3[k];
        var axisY = new Vector3[k];
        var axisZ = new Vector3[k];
        var entries = new Vector3[k];
        var exits = new Vector3[k];
        var startAngles = new float[k];
        var sweeps = new float[k];
        var arcs = new PulleyArc[k];

        for (var i = 0; i < k; i++)
        {
            var g = guides[i];
            centers[i] = g.Position + Vector3.Transform(new Vector3(0f, 0f, PulleySocketDepth), g.Rotation);
            axisX[i] = Vector3.Transform(Vector3.UnitX, g.Rotation);
            axisY[i] = Vector3.Transform(Vector3.UnitY, g.Rotation);
            axisZ[i] = Vector3.Transform(Vector3.UnitZ, g.Rotation);
            entries[i] = centers[i] + axisY[i] * PulleyRadius;
            exits[i] = entries[i];
        }

        for (var pass = 0; pass < 4; pass++)
        {
            for (var i = 0; i < k; i++)
            {
                var prevPoint = i == 0 ? terminalA : exits[i - 1];
                var nextPoint = i == k - 1 ? terminalB : entries[i + 1];

                var toPrev = prevPoint - centers[i];
                var toNext = nextPoint - centers[i];

                var inLocal = new Vector2(Vector3.Dot(toPrev, axisX[i]), Vector3.Dot(toPrev, axisY[i]));
                var outLocal = new Vector2(Vector3.Dot(toNext, axisX[i]), Vector3.Dot(toNext, axisY[i]));

                var winding = ChooseWinding(inLocal, outLocal);
                var (entryAngle, exitAngle, sweep) = TangentAngles(inLocal, outLocal, winding);

                startAngles[i] = entryAngle;
                sweeps[i] = sweep;

                var entryLocal = new Vector2(PulleyRadius * MathF.Cos(entryAngle), PulleyRadius * MathF.Sin(entryAngle));
                var exitLocal = new Vector2(PulleyRadius * MathF.Cos(exitAngle), PulleyRadius * MathF.Sin(exitAngle));

                entries[i] = centers[i] + axisX[i] * entryLocal.X + axisY[i] * entryLocal.Y;
                exits[i] = centers[i] + axisX[i] * exitLocal.X + axisY[i] * exitLocal.Y;
            }
        }

        var legA = Vector3.Distance(terminalA, entries[0]);
        var legB = Vector3.Distance(terminalB, exits[k - 1]);
        var totalLength = legA + legB;

        for (var i = 0; i < k; i++)
        {
            var arcLen = PulleyRadius * MathF.Abs(sweeps[i]);
            totalLength += arcLen;
            if (i < k - 1) totalLength += Vector3.Distance(exits[i], entries[i + 1]);
            arcs[i] = new(entries[i], exits[i], startAngles[i], sweeps[i], arcLen);
        }

        var uA = legA > 1e-6f ? (entries[0] - terminalA) / legA : Vector3.UnitY;
        var uB = legB > 1e-6f ? (exits[k - 1] - terminalB) / legB : Vector3.UnitY;

        return new(entries[0], exits[k - 1], uA, uB, arcs, totalLength);
    }

    private static (float EntryAngle, float ExitAngle, float Sweep) TangentAngles(
        Vector2 inLocal, Vector2 outLocal, RopeWinding winding)
    {
        var sign = (int)winding;
        var dIn = inLocal.Length();
        var dOut = outLocal.Length();
        var alphaIn = MathF.Acos(Math.Clamp(PulleyRadius / MathF.Max(dIn, PulleyRadius), 0f, 1f));
        var alphaOut = MathF.Acos(Math.Clamp(PulleyRadius / MathF.Max(dOut, PulleyRadius), 0f, 1f));
        var thetaIn = MathF.Atan2(inLocal.Y, inLocal.X);
        var thetaOut = MathF.Atan2(outLocal.Y, outLocal.X);
        var entryAngle = thetaIn + sign * alphaIn;
        var exitAngle = thetaOut - sign * alphaOut;
        var sweep = sign * PosMod(sign * (exitAngle - entryAngle), MathF.Tau);
        return (entryAngle, exitAngle, sweep);
    }

    private static float PosMod(float val, float mod) => ((val % mod) + mod) % mod;

    private static RopeWinding ChooseWinding(Vector2 inLocal, Vector2 outLocal)
    {
        var (cwIn, _, cwSweep) = TangentAngles(inLocal, outLocal, RopeWinding.Clockwise);
        var (ccwIn, _, ccwSweep) = TangentAngles(inLocal, outLocal, RopeWinding.CounterClockwise);
        float MeanHeight(float start, float sweep) => MathF.Abs(sweep) < 1e-5f
            ? MathF.Sin(start)
            : (-MathF.Cos(start + sweep) + MathF.Cos(start)) / sweep;
        var cwHeight = MeanHeight(cwIn, cwSweep);
        var ccwHeight = MeanHeight(ccwIn, ccwSweep);
        if (MathF.Abs(cwHeight - ccwHeight) > 1e-4f)
            return cwHeight > ccwHeight ? RopeWinding.Clockwise : RopeWinding.CounterClockwise;
        return MathF.Abs(cwSweep) <= MathF.Abs(ccwSweep) ? RopeWinding.Clockwise : RopeWinding.CounterClockwise;
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
