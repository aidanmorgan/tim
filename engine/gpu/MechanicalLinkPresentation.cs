using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Linq;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Animation-owned circular-rim route. It cannot affect the mechanical relation.</summary>
public static class MechanicalLinkPresentation
{
    // One centimetre is less than the existing 2.5 cm strand radius: endpoints must visibly meet their rims.
    public const float PositionAllowance = .01f;
    private const float ChordAllowance = .006f; // Leaves .004m of the visible allowance for f32 arithmetic.
    private readonly record struct Rim(Vector3 Centre, Vector3 Normal, Vector3 Radial, float Radius, float Scale, float Coordinate);
    private static float Length(Vector3 value)
    {
        var scale = MathF.Max(MathF.Abs(value.X), MathF.Max(MathF.Abs(value.Y), MathF.Abs(value.Z)));
        return scale == 0f ? 0f : scale * (value / scale).Length();
    }
    private static Vector3 Unit(Vector3 value)
    {
        var scale = MathF.Max(MathF.Abs(value.X), MathF.Max(MathF.Abs(value.Y), MathF.Abs(value.Z)));
        var scaled = value / scale;
        return scaled / scaled.Length();
    }
    public static MechanicalLinkGeometry Evaluate(CompiledMechanicalLink link, WorkshopRead read, IPhysicsSceneDeclaration physics)
    {
        var a = Resolve(link.Source, read, physics); var b = Resolve(link.TargetRim, read, physics);
        return Route(link.Target, read.Tick, a, b);
    }
    private static Rim Resolve(CompiledMechanicalRim rim, WorkshopRead read, IPhysicsSceneDeclaration physics)
    {
        var declaration = physics.Bodies.ToArray().Single(body => body.Id == rim.Support);
        var cell = declaration.Cell; var local = declaration.Local; var rotation = declaration.Rotation;
        if (declaration.Motion == RigidMotionKind.Dynamic)
        {
            if (!read.Bodies.TryGet(rim.Support, out var body)) throw new ArgumentException("Absent moving rim support.");
            cell = body.Body.Cell; local = body.Body.Local; rotation = body.Rotation;
        }
        var q = Quaternion.Normalize(new(rotation.X, rotation.Y, rotation.Z, rotation.W));
        var position = new Vector3((cell.X + local.X) / 16f, (cell.Y + local.Y) / 16f, (cell.Z + local.Z) / 16f);
        var frame = Quaternion.Normalize(Quaternion.Multiply(q, rim.Frame));
        var normal = Unit(Vector3.Transform(Vector3.UnitZ, frame));
        var radial = Vector3.Transform(Vector3.UnitX, frame);
        radial = Unit(radial - normal * Vector3.Dot(radial, normal));
        foreach (var row in read.Presentation.Actuators)
            if (row.Owner == rim.CoordinateSupport && row.Drive.Kind == SupplyConsumerKind.RevoluteDrive &&
                row.Drive.RevoluteJoint == rim.Joint)
                return new(position + Vector3.Transform(rim.Centre, q), normal, radial,
                    rim.Radius, rim.AnglePerCoordinate, row.Coordinate);
        throw new ArgumentException("Absent rim coordinate.");
    }
    private static Vector3 Toward(Rim rim, Vector3 direction)
    {
        if (direction == Vector3.Zero) return rim.Radial;
        var transverse = Vector3.Cross(rim.Normal, Unit(direction));
        return transverse == Vector3.Zero ? rim.Radial : Unit(Vector3.Cross(Unit(transverse), rim.Normal));
    }
    private static MechanicalLinkGeometry Route(CuriousContraptions.Presentation.AnimationTargetId target, SimulationTick tick, Rim a, Rim b)
    {
        var ua = Toward(a, b.Centre - a.Centre); var ub = Toward(b, a.Centre - b.Centre);
        var va = Unit(Vector3.Cross(a.Normal, ua)); var vb = Unit(Vector3.Cross(b.Normal, ub));
        var ap = a.Centre + a.Radius * va; var am = a.Centre - a.Radius * va;
        var bp = b.Centre + b.Radius * vb; var bm = b.Centre - b.Radius * vb;
        var points = new List<Vector3> { ap };
        Connector(points, ap, bm, ua, ub, a.Radius + b.Radius);
        var targetIndex = points.Count - 1;
        Arc(points, b, ub, vb);
        Connector(points, bp, am, ub, ua, a.Radius + b.Radius);
        Arc(points, a, ua, va);
        points[^1] = ap;
        var lengths = new float[points.Count];
        var compensation = 0f;
        for (var i = 1; i < points.Count; i++)
        {
            var increment = Length(points[i] - points[i - 1]) - compensation;
            var sum = lengths[i - 1] + increment;
            compensation = (sum - lengths[i - 1]) - increment;
            lengths[i] = sum;
        }
        var total = lengths[^1];
        if (!float.IsFinite(total) || total <= 0f) throw new ArgumentException("Unrepresentable mechanical route.");
        var result = new MechanicalLinkGeometry(target, tick, points.ToImmutableArray(),
            Marker(points, lengths, MechanicalLinkTravel.Distance(a.Radius, -a.Scale, a.Coordinate, total)),
            Marker(points, lengths, lengths[targetIndex] - MechanicalLinkTravel.Distance(b.Radius, b.Scale, b.Coordinate, total)));
        result.Validate();
        return result;
    }
    private static void Connector(List<Vector3> points, Vector3 start, Vector3 end, Vector3 uStart, Vector3 uEnd, float radii)
    {
        var h = MathF.Max(Length(end - start), radii) / 3f;
        Cubic(points, start, start + h * uStart, end + h * uEnd, end);
    }
    private static void Cubic(List<Vector3> points, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        // The Bezier convex hull bounds departure from the chord by the farthest control point.
        var excess = MathF.Max(ChordDistance(b, a, d), ChordDistance(c, a, d));
        if (!float.IsFinite(excess)) throw new ArgumentException("Unrepresentable route controls.");
        if (excess <= ChordAllowance)
        { points.Add(d); return; }
        var ab = a * .5f + b * .5f; var bc = b * .5f + c * .5f; var cd = c * .5f + d * .5f;
        var abc = ab * .5f + bc * .5f; var bcd = bc * .5f + cd * .5f; var middle = abc * .5f + bcd * .5f;
        Cubic(points, a, ab, abc, middle);
        Cubic(points, middle, bcd, cd, d);
    }
    private static float ChordDistance(Vector3 point, Vector3 a, Vector3 b)
    {
        var delta = b - a;
        var length = Length(delta);
        var t = length > 0f ? Math.Clamp(Vector3.Dot(point - a, delta / length) / length, 0f, 1f) : 0f;
        return Length(point - (a + t * delta));
    }
    private static void Arc(List<Vector3> points, Rim rim, Vector3 u, Vector3 v)
    {
        // Chord sag <= one centimetre. sqrt bound avoids acos rounding to zero for large admitted radii.
        var steps = Math.Max(2, checked((int)MathF.Ceiling(MathF.PI * MathF.Sqrt(rim.Radius / (8f * ChordAllowance)))));
        for (var i = 1; i <= steps; i++)
        {
            var angle = MathF.PI * i / steps;
            points.Add(rim.Centre - rim.Radius * (u * MathF.Sin(angle) + v * MathF.Cos(angle)));
        }
    }
    private static Vector3 Marker(List<Vector3> points, float[] lengths, float travel)
    {
        if (!float.IsFinite(travel)) throw new ArgumentException("Unrepresentable marker travel.");
        var distance = travel % lengths[^1];
        if (distance < 0f) distance += lengths[^1];
        for (var i = 1; i < lengths.Length; i++)
            if (distance <= lengths[i] && lengths[i] > lengths[i - 1])
                return Vector3.Lerp(points[i - 1], points[i], (distance - lengths[i - 1]) / (lengths[i] - lengths[i - 1]));
        return points[0];
    }
}
