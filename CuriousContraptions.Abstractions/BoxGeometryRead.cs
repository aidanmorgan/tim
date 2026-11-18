using System;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public enum BoxQueryRefusal : byte { UnknownCollider, NonBoxCollider, NonFiniteSegment, ZeroLengthSegment }

public sealed class BoxQueryException(BoxQueryRefusal reason) : ArgumentException("Invalid committed box query.")
{
    public BoxQueryRefusal Reason { get; } = reason;
}

public readonly record struct BoxSegmentHit(float Fraction, float Distance, Vector3 Point, Vector3 Normal, BoxFace Face,
    GpuColliderId Collider, GpuMaterialId Material, GpuBodyId PhysicalOwner);

/// <summary>One evaluated committed box. A clipped extrusion's proximal face belongs to ClippingOwner.</summary>
public readonly record struct BoxGeometryRead(GpuColliderId Collider, GpuMaterialId Material, GpuBodyId MaterialOwner,
    GpuBodyId? ClippingOwner, Vector3 Centre, Quaternion Rotation, Vector3 HalfExtents)
{
    public BoxSegmentHit? IntersectSegment(Vector3 from, Vector3 to)
    {
        var delta = to - from;
        if (!Finite(from) || !Finite(to) || !Finite(delta))
            throw new BoxQueryException(BoxQueryRefusal.NonFiniteSegment);
        if (delta == Vector3.Zero) throw new BoxQueryException(BoxQueryRefusal.ZeroLengthSegment);
        var inverse = Quaternion.Conjugate(Rotation);
        var start = Vector3.Transform(from - Centre, inverse);
        var direction = Vector3.Transform(delta, inverse);
        if (!Finite(start) || !Finite(direction)) throw new BoxQueryException(BoxQueryRefusal.NonFiniteSegment);
        var near = float.NegativeInfinity;
        var far = float.PositiveInfinity;
        var enter = BoxFace.NegativeX;
        var exit = BoxFace.PositiveX;
        BoxFace? boundary = null;
        var inside = true;
        for (var axis = 0; axis < 3; axis++)
        {
            var p = Component(start, axis);
            var d = Component(direction, axis);
            var half = Component(HalfExtents, axis);
            inside &= p >= -half && p <= half;
            if (boundary is null && (p == -half || p == half)) boundary = Face(axis, p >= 0f);
            if (d == 0f)
            {
                if (p < -half || p > half) return null;
                continue;
            }
            var low = (-half - p) / d;
            var high = (half - p) / d;
            var lowFace = Face(axis, false);
            var highFace = Face(axis, true);
            if (low > high) { (low, high) = (high, low); (lowFace, highFace) = (highFace, lowFace); }
            if (low > near) { near = low; enter = lowFace; }
            if (high < far) { far = high; exit = highFace; }
            if (near > far) return null;
        }
        if (FirstBoundary(near, far, enter, exit, inside, boundary) is not { } selected) return null;
        var (fraction, face) = selected;
        var owner = face == BoxFace.NegativeY && ClippingOwner is { } clipping ? clipping : MaterialOwner;
        return new(fraction, SegmentDistance(delta, fraction), from + delta * fraction, Vector3.Transform(Normal(face), Rotation), face, Collider, Material, owner);
    }

    private static float SegmentDistance(Vector3 delta, float fraction)
    {
        var displacement = delta * fraction;
        var scale = MathF.Max(MathF.Abs(displacement.X), MathF.Max(MathF.Abs(displacement.Y), MathF.Abs(displacement.Z)));
        return scale == 0f ? 0f : scale * (displacement / scale).Length();
    }

    private static (float Fraction, BoxFace Face)? FirstBoundary(float near, float far, BoxFace enter, BoxFace exit,
        bool inside, BoxFace? boundary)
    {
        if (far < 0f || near > 1f) return null;
        var fraction = inside ? boundary.HasValue ? 0f : far : near;
        var face = inside ? boundary ?? exit : enter;
        return fraction < 0f || fraction > 1f || !float.IsFinite(fraction) ? null : (fraction, face);
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static float Component(Vector3 value, int axis) => axis == 0 ? value.X : axis == 1 ? value.Y : value.Z;
    private static BoxFace Face(int axis, bool positive) => axis switch
    {
        0 => positive ? BoxFace.PositiveX : BoxFace.NegativeX,
        1 => positive ? BoxFace.PositiveY : BoxFace.NegativeY,
        _ => positive ? BoxFace.PositiveZ : BoxFace.NegativeZ
    };
    private static Vector3 Normal(BoxFace face) => face switch
    {
        BoxFace.PositiveX => Vector3.UnitX, BoxFace.NegativeX => -Vector3.UnitX,
        BoxFace.PositiveY => Vector3.UnitY, BoxFace.NegativeY => -Vector3.UnitY,
        BoxFace.PositiveZ => Vector3.UnitZ, BoxFace.NegativeZ => -Vector3.UnitZ,
        _ => throw new ArgumentException("Undefined box face.")
    };
}
