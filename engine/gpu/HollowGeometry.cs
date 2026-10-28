using System;
using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Which surface of a hollow solid a closest point lies on: the end rim at −h (a torus's end face at a = 0), the outer wall,
/// the end rim at +h (a torus's end face at a = θ) or the bore.</summary>
public enum HollowSurface : byte { Start = 0, Outer = 1, End = 2, Bore = 3 }

/// <summary>One posed hollow collider (engine-hollow-shapes, owner decision "true hollow shapes", 25 Oct 2026) and its exact signed
/// distance field: the single geometric definition the narrowphase and placement both read. Every kind is a 2D profile revolved about
/// the local axis <see cref="Y"/>:
/// <list type="bullet">
/// <item><see cref="ColliderShapeKind.Tube"/>: the rectangle ρ ∈ [r_i, r_o], y ∈ [−h, h], a full turn (lanes: radius r_o; r_i, h, 0).</item>
/// <item><see cref="ColliderShapeKind.Frustum"/>: the quadrilateral (r_0 − t, −h), (r_0, −h), (r_1, h), (r_1 − t, h), a full turn (radius
/// r_0; t, h, r_1).</item>
/// <item><see cref="ColliderShapeKind.Torus"/>: the annulus r_i ≤ |(ρ − R, y)| ≤ r_o (a disc when r_i is zero) revolved over a ∈ [0, θ]
/// from <see cref="X"/> toward <see cref="Z"/>, with flat ring-shaped end faces (radius R; r_i, r_o, θ; θ in (0, π] or
/// <see cref="ColliderDeclaration.FullSweep"/>).</item>
/// </list>
/// The bore and the ends are open. Every evaluation is a fixed sequence of f32 operations: its error is a few roundings of the
/// coordinates (surface-error bound, docs/hollow-geometry-design.md).</summary>
public readonly struct HollowSolid(ColliderShapeKind kind, Vector3 position, Vector3 x, Vector3 y, Vector3 z, float radius,
    float laneX, float laneY, float laneZ)
{
    public ColliderShapeKind Kind { get; } = kind;
    public Vector3 Position { get; } = position;
    public Vector3 X { get; } = x;
    public Vector3 Y { get; } = y;
    public Vector3 Z { get; } = z;
    public float Radius { get; } = radius;
    public float LaneX { get; } = laneX;
    public float LaneY { get; } = laneY;
    public float LaneZ { get; } = laneZ;

    public static bool IsHollow(ColliderShapeKind kind) =>
        kind is ColliderShapeKind.Tube or ColliderShapeKind.Torus or ColliderShapeKind.Frustum;

    /// <summary>The radius of a ball about <see cref="Position"/> that contains the solid.</summary>
    public float Reach => Kind switch
    {
        ColliderShapeKind.Tube => MathF.Sqrt(Radius * Radius + LaneY * LaneY),
        ColliderShapeKind.Frustum => MathF.Sqrt(MathF.Max(Radius, LaneZ) * MathF.Max(Radius, LaneZ) + LaneY * LaneY),
        _ => Radius + LaneY
    };

    /// <summary>Analytic incident outward feature normals at a measured surface witness.
    /// Distances are nonnegative distances to bounded features, including end annuli.</summary>
    internal void FeatureProjections(Vector3 point, Span<float> distances, Span<Vector3> directions, Span<Vector3> witnesses)
    {
        distances.Fill(float.PositiveInfinity);
        directions.Clear(); witnesses.Clear();
        var offset = point - Position;
        var x = Vector3.Dot(offset, X); var y = Vector3.Dot(offset, Y); var z = Vector3.Dot(offset, Z);
        var rho = MathF.Sqrt(x * x + z * z);
        if (!(rho > 0f)) return;
        if (Kind != ColliderShapeKind.Torus)
        {
            var top = Kind == ColliderShapeKind.Frustum ? LaneZ : Radius;
            var innerBottom = Kind == ColliderShapeKind.Frustum ? Radius - LaneX : LaneX;
            var innerTop = Kind == ColliderShapeKind.Frustum ? top - LaneX : LaneX;
            Span<Vector2> vertices = [new(innerBottom, -LaneY), new(Radius, -LaneY), new(top, LaneY), new(innerTop, LaneY)];
            var p = new Vector2(rho, y);
            var radial = (x * X + z * Z) / rho;
            for (var i = 0; i < 4; i++)
            {
                var a = vertices[i]; var edge = vertices[(i + 1) & 3] - a;
                var t = Math.Clamp(Vector2.Dot(p - a, edge) / edge.LengthSquared(), 0f, 1f);
                var projected = a + t * edge;
                witnesses[i] = Position + projected.X * radial + projected.Y * Y;
                distances[i] = Vector3.Distance(point, witnesses[i]);
                var n = Vector2.Normalize(new(edge.Y, -edge.X));
                directions[i] = n.X * radial + n.Y * Y;
            }
        }
        else
        {
            var phi = MathF.Atan2(z, x);
            if (!Full && !InSweep(phi, LaneZ)) phi = phi > LaneZ || phi < .5f * LaneZ - MathF.PI ? LaneZ : 0f;
            var radial = MathF.Cos(phi) * X + MathF.Sin(phi) * Z;
            var meridian = new Vector2(Vector3.Dot(offset, radial) - Radius, y);
            var length = meridian.Length();
            if (length > 0f)
            {
                var n = (meridian.X * radial + meridian.Y * Y) / length;
                witnesses[0] = Position + Radius * radial + LaneY * n;
                distances[0] = Vector3.Distance(point, witnesses[0]);
                directions[0] = n;
                if (LaneX > 0f)
                {
                    witnesses[1] = Position + Radius * radial + LaneX * n;
                    distances[1] = Vector3.Distance(point, witnesses[1]);
                    directions[1] = -n;
                }
            }
            if (!Full)
                for (var end = 0; end < 2; end++)
                {
                    var angle = end == 0 ? 0f : LaneZ;
                    var r = MathF.Cos(angle) * X + MathF.Sin(angle) * Z;
                    var across = new Vector2(Vector3.Dot(offset, r) - Radius, y);
                    var reach = across.Length();
                    var projected = across;
                    if (reach > LaneY) projected *= LaneY / reach;
                    else if (reach < LaneX && reach > 0f) projected *= LaneX / reach;
                    // At an annulus centre with a bore, every inner rim is equally
                    // near; this point is not an end-face surface witness.
                    else if (reach == 0f && LaneX > 0f) continue;
                    witnesses[2 + end] = Position + (Radius + projected.X) * r + projected.Y * Y;
                    distances[2 + end] = Vector3.Distance(point, witnesses[2 + end]);
                    directions[2 + end] = end == 0 ? -Z : -MathF.Sin(angle) * X + MathF.Cos(angle) * Z;
                }
        }
    }

    internal int IncidentNormals(Vector3 point, float window, Span<Vector3> normals, Span<float> allowances)
    {
        Span<float> distances = stackalloc float[4];
        Span<Vector3> directions = stackalloc Vector3[4];
        Span<Vector3> witnesses = stackalloc Vector3[4];
        FeatureProjections(point, distances, directions, witnesses);
        var nearest = float.PositiveInfinity;
        for (var i = 0; i < 4; i++) nearest = MathF.Min(nearest, distances[i]);
        var offset = point - Position;
        var x = Vector3.Dot(offset, X); var y = Vector3.Dot(offset, Y); var z = Vector3.Dot(offset, Z);
        var radial = MathF.Sqrt(x * x + z * z);
        var meridian = MathF.Sqrt((radial - Radius) * (radial - Radius) + y * y);
        var count = 0;
        for (var i = 0; i < 4; i++)
            if (float.IsFinite(distances[i]) && distances[i] <= nearest + window && directions[i].LengthSquared() > 0f)
            {
                var curved = Kind == ColliderShapeKind.Torus ? i < 2 : i is 1 or 3;
                var radius = Kind == ColliderShapeKind.Torus ? MathF.Min(radial, meridian) : radial;
                var allowance = curved ? PhysicalTolerance.IncidentNormalAllowance(window, radius) : 128f / 16777216f;
                if (!(allowance > 0f)) continue;
                normals[count] = Vector3.Normalize(directions[i]);
                allowances[count++] = allowance;
            }
        return count;
    }

    /// <summary>Whether a torus sweeps the full turn.</summary>
    public bool Full => Kind == ColliderShapeKind.Torus && LaneZ == ColliderDeclaration.FullSweep;

    /// <summary>Whether an angle (any turn) lies in [0, sweep] once taken in [0, 2π).</summary>
    public static bool InSweep(float angle, float sweep)
    {
        var a = angle % MathF.Tau;
        if (a < 0f) a += MathF.Tau;
        return a <= sweep;
    }

    private Vector3 World(float x, float y, float z) => x * X + y * Y + z * Z;

    /// <summary>The exact signed distance from p to the solid (negative inside its wall), its closest surface point and the unit
    /// direction leaving the solid there (outside: from the surface toward p; inside: p's shortest exit).</summary>
    public float Distance(Vector3 p, out Vector3 surface, out Vector3 outward, out HollowSurface on)
    {
        var d = p - Position;
        var x = Vector3.Dot(d, X);
        var y = Vector3.Dot(d, Y);
        var z = Vector3.Dot(d, Z);
        return Kind == ColliderShapeKind.Torus
            ? TorusDistance(x, y, z, out surface, out outward, out on)
            : ProfileDistance(x, y, z, out surface, out outward, out on);
    }

    /// <summary>A conservative smooth closest-wall domain, excluding profile/end junctions and medial ties.</summary>
    public bool SmoothClosestWall(Vector3 point, Vector3 surface, HollowSurface on)
    {
        if (on is not (HollowSurface.Outer or HollowSurface.Bore)) return false;
        var p = point - Position;
        var x = Vector3.Dot(p, X); var y = Vector3.Dot(p, Y); var z = Vector3.Dot(p, Z);
        var rho = MathF.Sqrt(x * x + z * z);
        if (!(rho > 0f)) return false;
        var s = surface - Position;
        if (Kind != ColliderShapeKind.Torus)
        {
            var sy = Vector3.Dot(s, Y);
            if (!(MathF.Abs(y) < LaneY && MathF.Abs(sy) < LaneY)) return false;
            var slope = Kind == ColliderShapeKind.Tube ? 0f : (LaneZ - Radius) / (2f * LaneY);
            var outer = Radius + slope * (y + LaneY);
            var inner = Kind == ColliderShapeKind.Tube ? LaneX : outer - LaneX;
            var divisor = MathF.Sqrt(1f + slope * slope);
            var outerDistance = MathF.Abs(rho - outer) / divisor;
            var innerDistance = MathF.Abs(rho - inner) / divisor;
            var chosen = on == HollowSurface.Outer ? outerDistance : innerDistance;
            var other = on == HollowSurface.Outer ? innerDistance : outerDistance;
            return chosen < other && chosen < LaneY - MathF.Abs(y);
        }
        var radial = rho - Radius;
        var meridian = MathF.Sqrt(radial * radial + y * y);
        if (!(meridian > 0f)) return false;
        var wallDistance = MathF.Abs(meridian - (on == HollowSurface.Outer ? LaneY : LaneX));
        if (LaneX > 0f && !(wallDistance < MathF.Abs(meridian - (on == HollowSurface.Outer ? LaneX : LaneY)))) return false;
        if (Full) return true;
        var angle = MathF.Atan2(z, x);
        var surfaceAngle = MathF.Atan2(Vector3.Dot(s, Z), Vector3.Dot(s, X));
        if (!(angle > 0f && angle < LaneZ && surfaceAngle > 0f && surfaceAngle < LaneZ)) return false;
        return wallDistance < MathF.Abs(z) &&
            wallDistance < MathF.Abs(-x * MathF.Sin(LaneZ) + z * MathF.Cos(LaneZ));
    }

    public float Distance(Vector3 p) => Distance(p, out _, out _, out _);


    /// <summary>The exact signed distance from a point (u, y) of a meridian plane (u along the plane's radial direction, which may be
    /// negative: the opposite half-plane) to the meridian profile, its closest profile point and the profile's outward direction
    /// there: a tube's or frustum's quadrilateral, a torus's annulus about (R, 0) (a disc when its tube is solid).</summary>
    public float Meridian(Vector2 point, out Vector2 closest, out Vector2 normal, out HollowSurface on)
    {
        if (Kind == ColliderShapeKind.Torus)
        {
            var offset = point - new Vector2(Radius, 0f);
            var tube = offset.Length();
            var direction = tube > 1e-7f ? offset / tube : Vector2.UnitY;
            if (LaneX > 0f && tube < 0.5f * (LaneX + LaneY))
            {
                closest = new Vector2(Radius, 0f) + LaneX * direction;
                normal = -direction;
                on = HollowSurface.Bore;
                return LaneX - tube;
            }
            closest = new Vector2(Radius, 0f) + LaneY * direction;
            normal = direction;
            on = HollowSurface.Outer;
            return tube - LaneY;
        }
        return Quad(point, out closest, out normal, out on);
    }

    /// <summary>The exact distance from p to the surface points lying in one meridian half-plane (unit radial direction e, perpendicular
    /// to the axis): the meridian profile's distance from p's projection (u = p·e, y), combined with p's distance w off that plane,
    /// √(d² + w²); its closest point and the unit direction from it toward p. For a torus segment, the half-plane must lie within the
    /// sweep.</summary>
    public float MeridianDistance(Vector3 p, Vector3 e, out Vector3 surface, out Vector3 outward)
    {
        var d = p - Position;
        var y = Vector3.Dot(d, Y);
        var q = d - y * Y;
        var u = Vector3.Dot(q, e);
        Meridian(new Vector2(u, y), out var closest, out var normal, out _);
        surface = Position + closest.X * e + closest.Y * Y;
        var diff = p - surface;
        var distance = diff.Length();
        outward = distance > 1e-9f ? diff / distance : normal.X * e + normal.Y * Y;
        return distance;
    }

    /// <summary>A tube or frustum: the exact distance from (ρ, y) to its convex profile quadrilateral, revolved.</summary>
    private float ProfileDistance(float x, float y, float z, out Vector3 surface, out Vector3 outward, out HollowSurface on)
    {
        var rho = MathF.Sqrt(x * x + z * z);
        var distance = Quad(new Vector2(rho, y), out var closest, out var normal, out on);
        var radialX = rho > 1e-6f ? x / rho : 1f;
        var radialZ = rho > 1e-6f ? z / rho : 0f;
        surface = Position + World(closest.X * radialX, closest.Y, closest.X * radialZ);
        outward = World(normal.X * radialX, normal.Y, normal.X * radialZ);
        return distance;
    }

    /// <summary>The exact signed distance from a meridian point to the tube's or frustum's convex profile quadrilateral (end rim at −h,
    /// outer wall, end rim at +h, bore). Outside, the distance and direction are to the nearest edge point; inside, to the nearest edge
    /// along its outward normal.</summary>
    private float Quad(Vector2 point, out Vector2 closest, out Vector2 normal, out HollowSurface on)
    {
        var h = LaneY;
        float innerBottom, outerBottom, innerTop, outerTop;
        if (Kind == ColliderShapeKind.Tube) { outerBottom = outerTop = Radius; innerBottom = innerTop = LaneX; }
        else { outerBottom = Radius; outerTop = LaneZ; innerBottom = Radius - LaneX; innerTop = LaneZ - LaneX; }
        Span<Vector2> v = [new(innerBottom, -h), new(outerBottom, -h), new(outerTop, h), new(innerTop, h)];

        var best = float.PositiveInfinity;
        closest = Vector2.Zero;
        var edgeNormal = Vector2.Zero;
        var edge = 0;
        var inside = true;
        for (var i = 0; i < 4; i++)
        {
            var a = v[i];
            var e = v[(i + 1) & 3] - a;
            var w = point - a;
            if (e.X * w.Y - e.Y * w.X < 0f) inside = false;
            var t = Math.Clamp(Vector2.Dot(w, e) / Vector2.Dot(e, e), 0f, 1f);
            var q = a + t * e;
            var squared = Vector2.DistanceSquared(point, q);
            if (squared < best)
            {
                best = squared;
                closest = q;
                edge = i;
                edgeNormal = Vector2.Normalize(new Vector2(e.Y, -e.X));
            }
        }
        var distance = MathF.Sqrt(best);
        on = (HollowSurface)edge;
        if (inside) { normal = edgeNormal; return -distance; }
        normal = distance > 1e-9f ? (point - closest) / distance : edgeNormal;
        return distance;
    }

    /// <summary>A torus segment. Within the sweep, the exact distance to the revolved annulus in p's own meridian (the nearest point of
    /// the full revolution lies in that meridian, inside the sweep); a point inside the wall also exits through an end face when that is
    /// nearer (the sweep is the intersection of the two end faces' half-spaces for θ ≤ π). Outside the sweep, the distance to the nearer
    /// end face's planar annulus: the end on p's side of the bisector opposite the sweep's middle, whose meridian is then the nearest
    /// for every point of the revolved profile (θ ≤ π).</summary>
    private float TorusDistance(float x, float y, float z, out Vector3 surface, out Vector3 outward, out HollowSurface on)
    {
        var bend = Radius;
        var inner = LaneX;
        var outer = LaneY;
        var sweep = LaneZ;
        var full = sweep == ColliderDeclaration.FullSweep;
        var phi = MathF.Atan2(z, x);
        if (full || (phi >= 0f && phi <= sweep))
        {
            var rho = MathF.Sqrt(x * x + z * z);
            var radialX = rho > 1e-6f ? x / rho : 1f;
            var radialZ = rho > 1e-6f ? z / rho : 0f;
            var distance = Meridian(new Vector2(rho, y), out var closest, out var normal, out on);
            if (!full && distance < 0f)
            {
                // Inside the wall: an end face's half-space may be nearer (signed distances −(p·t0) and p·tθ).
                var start = -z;
                var end = -x * MathF.Sin(sweep) + z * MathF.Cos(sweep);
                if (start > distance && start >= end)
                {
                    outward = -Z;
                    surface = Position + World(x, y, z) - start * outward;
                    on = HollowSurface.Start;
                    return start;
                }
                if (end > distance)
                {
                    outward = World(-MathF.Sin(sweep), 0f, MathF.Cos(sweep));
                    surface = Position + World(x, y, z) - end * outward;
                    on = HollowSurface.End;
                    return end;
                }
            }
            surface = Position + World(closest.X * radialX, closest.Y, closest.X * radialZ);
            outward = World(normal.X * radialX, normal.Y, normal.X * radialZ);
            return distance;
        }

        var atEnd = phi > sweep || phi < 0.5f * sweep - MathF.PI;
        var alpha = atEnd ? sweep : 0f;
        var cos = MathF.Cos(alpha);
        var sin = MathF.Sin(alpha);
        var u = x * cos + z * sin;
        var across = new Vector2(u - bend, y);
        var reach = across.Length();
        var along = reach > 1e-7f ? across / reach : Vector2.UnitY;
        Vector2 onFace;
        if (reach > outer) onFace = new Vector2(bend, 0f) + outer * along;
        else if (inner > 0f && reach < inner) onFace = new Vector2(bend, 0f) + inner * along;
        else onFace = new Vector2(u, y);
        surface = Position + World(onFace.X * cos, onFace.Y, onFace.X * sin);
        var diff = Position + World(x, y, z) - surface;
        var gap = diff.Length();
        outward = gap > 1e-9f ? diff / gap : (atEnd ? World(-sin, 0f, cos) : -Z);
        on = atEnd ? HollowSurface.End : HollowSurface.Start;
        return gap;
    }

    /// <summary>A point of the solid's support set in direction d (its outer surface's: a tube's outer cylinder, a frustum's outer rims,
    /// the torus centreline arc's extreme point plus r_o d̂); a flat or ruled support set gives its point nearest
    /// <paramref name="target"/>.</summary>
    public Vector3 SupportNear(Vector3 d, Vector3 target)
    {
        if (Kind == ColliderShapeKind.Torus)
        {
            var dx = Vector3.Dot(d, X);
            var dz = Vector3.Dot(d, Z);
            float angle;
            if (dx * dx + dz * dz > 1e-12f) angle = MathF.Atan2(dz, dx);
            else
            {
                var r = target - Position;
                angle = MathF.Atan2(Vector3.Dot(r, Z), Vector3.Dot(r, X));
            }
            if (!Full && !InSweep(angle, LaneZ))
                angle = dx >= dx * MathF.Cos(LaneZ) + dz * MathF.Sin(LaneZ) ? 0f : LaneZ;
            var meridianRadial = MathF.Cos(angle) * X + MathF.Sin(angle) * Z;
            // The tube cross-section lies in this meridian. At a segment endpoint the
            // tangential component points outside its flat cap and cannot contribute.
            var projected = Vector3.Dot(d, meridianRadial) * meridianRadial + Vector3.Dot(d, Y) * Y;
            var unit = projected.LengthSquared() > 1e-18f ? Vector3.Normalize(projected) : Y;
            return Position + Radius * meridianRadial + LaneY * unit;
        }
        var t = Vector3.Dot(d, Y);
        var radial = RadialUnit(d, RadialUnit(target - Position, X));
        if (Kind == ColliderShapeKind.Tube)
        {
            var axial = MathF.Abs(t) > 1e-6f ? (t > 0f ? LaneY : -LaneY) : Math.Clamp(Vector3.Dot(target - Position, Y), -LaneY, LaneY);
            return Position + axial * Y + Radius * radial;
        }
        var bottom = Position - LaneY * Y + Radius * radial;
        var top = Position + LaneY * Y + LaneZ * radial;
        return Vector3.Dot(top - bottom, d) >= 0f ? top : bottom;
    }

    /// <summary>Reconstructs a curved feature's support from its initial profile and radial branch.
    /// The reference radial direction is world-fixed, so pure revolution-axis spin leaves a full solid unchanged.</summary>
    public Vector3 CurvedSupport(Vector3 facing, Vector3 referenceRadial, float radialSign, float axial, float profileRadius,
        HollowSurface surface)
    {
        var reference = RadialUnit(referenceRadial, X);
        var projected = facing - Vector3.Dot(facing, Y) * Y;
        var length = projected.Length();
        var radial = length > 1e-6f ? radialSign * projected / length : reference;
        if (Kind == ColliderShapeKind.Torus)
        {
            if (!Full)
            {
                var angle = MathF.Atan2(Vector3.Dot(radial, Z), Vector3.Dot(radial, X));
                if (!InSweep(angle, LaneZ))
                {
                    var end = MathF.Cos(LaneZ) * X + MathF.Sin(LaneZ) * Z;
                    radial = Vector3.Dot(radial, X) >= Vector3.Dot(radial, end) ? X : end;
                }
            }
            var meridian = Vector3.Dot(facing, radial) * radial + Vector3.Dot(facing, Y) * Y;
            var unit = meridian.LengthSquared() > 1e-18f ? Vector3.Normalize(meridian) : Y;
            var radius = surface == HollowSurface.Bore ? -LaneX : LaneY;
            return Position + Radius * radial + radius * unit;
        }
        return Position + axial * Y + profileRadius * radial;
    }

    /// <summary>The unit direction of v perpendicular to the axis, or the fallback when v lies (nearly) along it.</summary>
    public Vector3 RadialUnit(Vector3 v, Vector3 fallback)
    {
        var p = v - Vector3.Dot(v, Y) * Y;
        var length = p.Length();
        return length > 1e-6f * MathF.Max(1f, v.Length()) ? p / length : fallback;
    }

    /// <summary>The solid's rim circles (centre, plane normal, radius): a tube's or frustum's outer and inner circles at both ends; a
    /// torus segment's end-face circles (outer, and inner when its tube is hollow). A full torus has none. Returns the count (at most
    /// four).</summary>
    public int Rims(Span<(Vector3 Centre, Vector3 Normal, float Radius)> rims)
    {
        var count = 0;
        if (Kind == ColliderShapeKind.Torus)
        {
            if (Full) return 0;
            for (var end = 0; end < 2; end++)
            {
                var angle = end == 0 ? 0f : LaneZ;
                var centre = Position + Radius * (MathF.Cos(angle) * X + MathF.Sin(angle) * Z);
                var normal = -MathF.Sin(angle) * X + MathF.Cos(angle) * Z;
                rims[count++] = (centre, normal, LaneY);
                if (LaneX > 0f) rims[count++] = (centre, normal, LaneX);
            }
            return count;
        }
        var start = Position - LaneY * Y;
        var finish = Position + LaneY * Y;
        var (outerStart, innerStart, outerEnd, innerEnd) = Kind == ColliderShapeKind.Tube
            ? (Radius, LaneX, Radius, LaneX) : (Radius, Radius - LaneX, LaneZ, LaneZ - LaneX);
        rims[count++] = (start, Y, outerStart);
        rims[count++] = (start, Y, innerStart);
        rims[count++] = (finish, Y, outerEnd);
        rims[count++] = (finish, Y, innerEnd);
        return count;
    }

}
