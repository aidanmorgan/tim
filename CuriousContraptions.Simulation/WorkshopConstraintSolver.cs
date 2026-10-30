using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Simulation;

internal readonly record struct SoftCoefficients(float BiasRate, float MassScale, float ImpulseScale)
{
    public static SoftCoefficients MakeSoft(float hertz, float zeta, float h)
    {
        var omega = 2f * MathF.PI * hertz;
        var a1 = 2f * zeta + h * omega;
        var a2 = h * omega * a1;
        var a3 = 1f / (1f + a2);
        return new SoftCoefficients(omega / a1, a2 * a3, a3);
    }
}

/// <summary>The TGS Soft pass a contact sweep belongs to: the biased solve, or the unbiased relax.</summary>
internal enum ContactPass { Solve, Relax }

/// <summary>Whether some point of a manifold is soft (s = separation + slop ≤ 0), which makes its slop-band points soft too.</summary>
internal enum ManifoldSoftness { NoSoftPoint, SoftPoint }

internal static class WorkshopConstraintSolver
{
    public const float SubstepSeconds = 1.0f / 480.0f;
    public const float ContactSlop = PhysicalTolerance.ContactSeparationMetres;
    public const float SpeculativeSlop = 0.004f;
    public const float MaxLinearSpeed = 64f;
    public const float MaxAngularSpeed = 128f;
    public const float SpeedClamp = 64f * (1f - (1f / 1048576f));
    public const float SpinClamp = 128f * (1f - (1f / 1048576f));
    public const float ContactHertz = 60f;
    public const float ContactDampingRatio = 10f;
    // One slop of requested correction over the longest supported committed interval (60 Hz).
    // The same physical rate applies at 120/240 Hz; this is not a whole-island energy bound.
    public const float MaxPushoutSpeed = ContactSlop / (1f / 60f);
    public const int SolverIterations = 4;
    public const int RelaxIterations = 4;

    /// <summary>The factor that brings (x, y, z) within <paramref name="limit"/>: 1 inside it, 0 when a lane is not finite
    /// (a NaN or infinite vector has no direction to keep).</summary>
    public static float EnvelopeScale(float x, float y, float z, float limit)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z)) return 0f;
        var length = MathF.Sqrt(x * x + y * y + z * z);
        if (!(length > limit)) return 1f;
        if (float.IsFinite(length)) return limit / length;
        // Finite lanes whose squared length overflows f32 still clamp along their direction: measure the length relative to the
        // largest lane.
        var largest = MathF.Max(MathF.Abs(x), MathF.Max(MathF.Abs(y), MathF.Abs(z)));
        x /= largest; y /= largest; z /= largest;
        return limit / largest / MathF.Sqrt(x * x + y * y + z * z);
    }

    public static void ClampVelocity(SimBody b)
    {
        // A zero scale assigns zero: NaN or infinity times 0 would stay NaN.
        var k = EnvelopeScale(b.Vx, b.Vy, b.Vz, SpeedClamp);
        if (k == 0f) { b.Vx = b.Vy = b.Vz = 0f; } else if (k != 1f) { b.Vx *= k; b.Vy *= k; b.Vz *= k; }
        var j = EnvelopeScale(b.Wx, b.Wy, b.Wz, SpinClamp);
        if (j == 0f) { b.Wx = b.Wy = b.Wz = 0f; } else if (j != 1f) { b.Wx *= j; b.Wy *= j; b.Wz *= j; }
    }

    public static void SettleVelocity(SimBody b)
    {
        if (float.IsFinite(b.Vx) && float.IsFinite(b.Vy) && float.IsFinite(b.Vz) &&
            float.IsFinite(b.Wx) && float.IsFinite(b.Wy) && float.IsFinite(b.Wz))
        {
            ClampVelocity(b);
        }
        else
        {
            b.Vx = b.Vy = b.Vz = b.Wx = b.Wy = b.Wz = 0f;
        }
    }

    public static float EffectiveMass(ContactPoint c, Vector3 d)
    {
        var A = c.A;
        var B = c.B;
        var k = (A.Motion == RigidMotionKind.Dynamic ? A.InvMass : 0f) + (B.Motion == RigidMotionKind.Dynamic ? B.InvMass : 0f);
        if (A.Motion == RigidMotionKind.Dynamic)
        {
            var r = Vector3.Cross(new Vector3(c.RAx, c.RAy, c.RAz), d);
            k += WorkshopCollisionKernels.InverseInertiaCoupling(A, r, r);
        }
        if (B.Motion == RigidMotionKind.Dynamic)
        {
            var r = Vector3.Cross(new Vector3(c.RBx, c.RBy, c.RBz), d);
            k += WorkshopCollisionKernels.InverseInertiaCoupling(B, r, r);
        }
        return k > 0f ? 1f / k : 0f;
    }

    public static void PrepareContact(ContactPoint c, Dictionary<ContactFeatureIdentity, float> cache)
    {
        var A = c.A;
        var B = c.B;
        c.RAx = c.Px - A.Cmx; c.RAy = c.Py - A.Cmy; c.RAz = c.Pz - A.Cmz;
        c.RBx = c.Px - B.Cmx; c.RBy = c.Py - B.Cmy; c.RBz = c.Pz - B.Cmz;

        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        Vector3 a;
        if (MathF.Abs(n.X) <= MathF.Abs(n.Y) && MathF.Abs(n.X) <= MathF.Abs(n.Z)) a = Vector3.UnitX;
        else if (MathF.Abs(n.Y) <= MathF.Abs(n.Z)) a = Vector3.UnitY;
        else a = Vector3.UnitZ;

        var t1 = Vector3.Cross(n, a);
        var l1 = t1.Length();
        c.T1 = l1 > 1e-6f ? t1 / l1 : Vector3.UnitX;
        c.T2 = Vector3.Cross(n, c.T1);


        // The approach before this substep's external velocity changes (guides, supplied drives, gravity, drag).
        var vA = StartVelocity(A, new Vector3(c.RAx, c.RAy, c.RAz));
        var vB = StartVelocity(B, new Vector3(c.RBx, c.RBy, c.RBz));
        c.RelVel = Vector3.Dot(vA - vB, n);

        c.Restitution = c.ColA.Restitution * c.ColB.Restitution;
        c.Threshold = MathF.Max(c.ColA.BounceThreshold, c.ColB.BounceThreshold);
        c.Friction = MathF.Sqrt(c.ColA.Friction * c.ColB.Friction);

        c.Key = new(c.ColA.Index, c.ColB.Index, c.Feature);

        c.LambdaN = cache.TryGetValue(c.Key, out var warm) ? warm : 0f;

        c.MaxImpulse = 0f;
    }

    private static Vector3 StartVelocity(SimBody b, Vector3 r) => b.Motion == RigidMotionKind.Dynamic ? b.V0 + Vector3.Cross(b.W0, r) : Vector3.Zero;

    public static void WarmStartContact(ContactPoint c)
    {
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        var p = c.LambdaN * n;
        WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
        WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
    }

    public static Vector3 RelativeVelocity(ContactPoint c)
    {
        var vA = WorkshopCollisionKernels.PointVelocity(c.A, new Vector3(c.RAx, c.RAy, c.RAz));
        var vB = WorkshopCollisionKernels.PointVelocity(c.B, new Vector3(c.RBx, c.RBy, c.RBz));
        return vA - vB;
    }

    public static Vector3 AnchorDisplacement(SimBody b, Vector3 r)
    {
        if (b.Motion != RigidMotionKind.Dynamic) return Vector3.Zero;
        var bq = new Quaternion(b.Qx, b.Qy, b.Qz, b.Qw);
        var dq = Quaternion.Multiply(bq, Quaternion.Conjugate(b.Q0));
        var rRot = Vector3.Transform(r, dq);
        return (new Vector3(b.Cmx, b.Cmy, b.Cmz) - b.Cm0) + (rRot - r);
    }

    /// <summary>The substep displacement of a contact's support on one body. A sphere's support is not a material point: it stays
    /// under the sphere's centre however the sphere turns, so it moves with the centre (whose arm from the centre of mass is the
    /// collider's substep-start position). A curved-side cylinder contact (axis support, Story 10.1) is the generator facing the
    /// partner at the anchor's axial coordinate: its support moves with that material axis point plus r times the turn of the radial
    /// direction facing the partner about the moved axis. Rolling about the axis leaves that direction unchanged (the sphere
    /// precedent); tumbling turns it, so the support stays on the facing generator. Any other support is the material anchor.</summary>
    private static Vector3 SupportDisplacement(SimBody b, SimCollider collider, ContactSupport support, Vector3 anchor, Vector3 facing)
    {
        if (collider.Shape == ShapeKind.Sphere)
            return AnchorDisplacement(b, new Vector3(collider.Px, collider.Py, collider.Pz) - b.Cm0);
        if (WorkshopCollisionKernels.IsHollow(collider.Shape) && b.Motion == RigidMotionKind.Dynamic)
        {
            var initial = WorkshopCollisionKernels.Hollow(collider);
            initial.Distance(b.Cm0 + anchor, out var witness, out var outward, out var feature);
            if (feature is HollowSurface.Outer or HollowSurface.Bore)
            {
                var offset = witness - initial.Position;
                var axial = Vector3.Dot(offset, initial.Y);
                var radial = offset - axial * initial.Y;
                var radius = radial.Length();
                if (radius > 0f)
                {
                    radial /= radius;
                    var sign = Vector3.Dot(outward, radial) < 0f ? -1f : 1f;
                    var turn = Quaternion.Multiply(b.Rotation, Quaternion.Conjugate(b.Q0));
                    var centre = initial.Position + AnchorDisplacement(b, initial.Position - b.Cm0);
                    var moved = new HollowSolid(initial.Kind, centre, Vector3.Transform(initial.X, turn),
                        Vector3.Transform(initial.Y, turn), Vector3.Transform(initial.Z, turn),
                        initial.Radius, initial.LaneX, initial.LaneY, initial.LaneZ);
                    return moved.CurvedSupport(facing, radial, sign, axial, radius, feature) -
                        initial.CurvedSupport(facing, radial, sign, axial, radius, feature);
                }
            }
        }
        if (support == ContactSupport.Axis && b.Motion == RigidMotionKind.Dynamic)
        {
            var centre = new Vector3(collider.Px, collider.Py, collider.Pz);
            var axis = collider.U1;
            var along = Math.Clamp(Vector3.Dot(b.Cm0 + anchor - centre, axis), -collider.Hy, collider.Hy);
            var turn = Quaternion.Multiply(new Quaternion(b.Qx, b.Qy, b.Qz, b.Qw), Quaternion.Conjugate(b.Q0));
            var moved = Vector3.Transform(axis, turn);
            return AnchorDisplacement(b, centre + along * axis - b.Cm0) +
                collider.Radius * (RadialUnit(facing, moved) - RadialUnit(facing, axis));
        }
        return AnchorDisplacement(b, anchor);
    }

    /// <summary>The unit component of <paramref name="v"/> perpendicular to the unit axis (zero when v lies along it).</summary>
    private static Vector3 RadialUnit(Vector3 v, Vector3 axis)
    {
        var p = v - Vector3.Dot(v, axis) * axis;
        var length = p.Length();
        return length > 1e-6f ? p / length : Vector3.Zero;
    }

    public static float CurrentSeparation(ContactPoint c)
    {
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        // A's support faces B (−n), B's faces A (+n).
        var dA = SupportDisplacement(c.A, c.ColA, c.SupportA, new Vector3(c.RAx, c.RAy, c.RAz), -n);
        var dB = SupportDisplacement(c.B, c.ColB, c.SupportB, new Vector3(c.RBx, c.RBy, c.RBz), n);
        return c.Gap + Vector3.Dot(dA - dB, n);
    }

    private enum RollingSupport : byte { Flat, Line, Point }
    private readonly record struct RollingSurface(float Arm, Vector3 Axis, RollingSupport Support);

    public static void PrepareRolling(ContactManifold manifold, float h)
    {
        foreach (var patch in manifold.Patches)
        {
            var supporting = patch.Supporting;
            supporting.Clear();
            var surface = default(RollingSurface);
            var maximumCoefficient = 0f;
            for (var i = 0; i < patch.Points.Count; i++)
            {
                var point = patch.Points[i];
                var local = CombineRolling(RollingGeometry(point, point.ColA, true), RollingGeometry(point, point.ColB, false));
                surface = CombineRolling(surface, local);
                var coefficient = MathF.Max(point.ColA.RollingResistance, point.ColB.RollingResistance) * local.Arm;
                patch.RollingCoefficients[i] = coefficient;
                maximumCoefficient = MathF.Max(maximumCoefficient, coefficient);
                if (point.Gap <= 0f || point.Gap + h * Vector3.Dot(RelativeVelocity(point), point.Normal) <= 0f)
                    supporting.Add(point);
            }
            var footprint = SupportFootprint(supporting, patch.Normal, patch.ProjectedSupport.AsSpan(0, supporting.Count));
            if (footprint.Support == RollingSupport.Flat) surface = default;
            else if (footprint.Support == RollingSupport.Line && surface.Support != RollingSupport.Flat)
                surface = surface.Support == RollingSupport.Line &&
                    MathF.Abs(Vector3.Dot(surface.Axis, footprint.Axis)) < WorkshopCollisionKernels.LineAlignment
                    ? default : new(surface.Arm, footprint.Axis, RollingSupport.Line);
            var c = patch.Points[0];
            Vector3? line = surface.Support == RollingSupport.Line ? surface.Axis : null;
            patch.Roll1 = line is { } axis ? Vector3.Normalize(axis - Vector3.Dot(axis, patch.Normal) * patch.Normal) : patch.T1;
            patch.Roll2 = line is null ? patch.T2 : Vector3.Zero;
            var active = maximumCoefficient > 0f && surface.Support != RollingSupport.Flat;
            patch.RollingMass1 = active ? AngularMass(c, patch.Roll1) : 0f;
            patch.RollingMass2 = active && line is null ? AngularMass(c, patch.Roll2) : 0f;
            patch.LambdaR1 = 0f;
            patch.LambdaR2 = 0f;
        }
    }

    private static RollingSurface SupportFootprint(List<ContactPoint> points, Vector3 normal, Span<Vector3> projected)
    {
        if (points.Count == 0) return new(0, Vector3.Zero, RollingSupport.Point);
        var scale = 0f;
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var p = Vector3.Abs(point.Point);
            scale = MathF.Max(scale, MathF.Max(p.X + p.Y + p.Z,
                MathF.Max(WorkshopCollisionKernels.ColliderExtent(point.ColA), WorkshopCollisionKernels.ColliderExtent(point.ColB))));
            var offset = point.Point - points[0].Point;
            projected[i] = offset - Vector3.Dot(offset, normal) * normal;
        }
        var resolution = PhysicalTolerance.ContactSupportResolutionMetres(scale, ContactSlop);
        var squaredResolution = resolution * resolution;
        var longest = 0f; var first = 0; var second = 0;
        for (var i = 0; i < projected.Length; i++)
            for (var j = i + 1; j < projected.Length; j++)
            {
                var squared = Vector3.DistanceSquared(projected[i], projected[j]);
                if (squared <= longest) continue;
                longest = squared; first = i; second = j;
            }
        if (longest <= squaredResolution) return new(0, Vector3.Zero, RollingSupport.Point);
        var axis = Vector3.Normalize(projected[second] - projected[first]);
        foreach (var point in projected)
        {
            var offset = point - projected[first];
            var across = offset - Vector3.Dot(offset, axis) * axis;
            if (across.LengthSquared() > squaredResolution) return default;
        }
        return new(0, axis, RollingSupport.Line);
    }

    private static RollingSurface CombineRolling(RollingSurface a, RollingSurface b)
    {
        if (a.Support == RollingSupport.Flat) return b;
        if (b.Support == RollingSupport.Flat) return a;
        var arm = MathF.Max(a.Arm, b.Arm);
        if (a.Support == RollingSupport.Point || b.Support == RollingSupport.Point)
            return new(arm, Vector3.Zero, RollingSupport.Point);
        var dot = Vector3.Dot(a.Axis, b.Axis);
        return MathF.Abs(dot) >= WorkshopCollisionKernels.LineAlignment
            ? new(arm, Vector3.Normalize(dot >= 0f ? a.Axis + b.Axis : a.Axis - b.Axis), RollingSupport.Line)
            : new(arm, Vector3.Zero, RollingSupport.Point);
    }

    // One geometry rule: a point support rolls about both tangents, a line about
    // its curved direction's perpendicular axis, and a flat support contributes none.
    private static RollingSurface RollingGeometry(ContactPoint p, SimCollider collider, bool sideA)
    {
        if (collider.Shape == ShapeKind.Sphere) return new(collider.Radius, Vector3.Zero, RollingSupport.Point);
        if (collider.Shape == ShapeKind.Cylinder)
            return (sideA ? p.SupportA : p.SupportB) == ContactSupport.Axis
                ? new(collider.Radius, collider.U1, RollingSupport.Line) : default;
        if (!WorkshopCollisionKernels.IsHollow(collider.Shape)) return default;
        var hollow = WorkshopCollisionKernels.Hollow(collider);
        hollow.Distance(p.Point, out var surface, out var outward, out var feature);
        if (feature is HollowSurface.Start or HollowSurface.End) return default;
        var delta = surface - collider.Position;
        var radial = delta - Vector3.Dot(delta, collider.U1) * collider.U1;
        var rho = radial.Length();
        if (!(rho > 0f)) return default;
        radial /= rho;
        var azimuth = Vector3.Cross(collider.U1, radial);
        var meridian = Vector3.Cross(outward, azimuth);
        var circumferential = Vector3.Dot(outward, radial) / rho;
        var tubeRadius = feature == HollowSurface.Bore ? collider.Hx : collider.Hy;
        var meridional = collider.Shape == ShapeKind.Torus ? (feature == HollowSurface.Bore ? -1f : 1f) / tubeRadius : 0f;
        if (circumferential < 0f || meridional < 0f) return default;
        float Arm(float curvature) => !(curvature > 0f) ? 0f :
            curvature * hollow.Reach <= 1f ? hollow.Reach : 1f / curvature;
        var first = Arm(circumferential); var second = Arm(meridional);
        if (first > 0f && second > 0f) return new(MathF.Max(first, second), Vector3.Zero, RollingSupport.Point);
        if (first > 0f) return new(first, collider.U1, RollingSupport.Line);
        if (second > 0f) return new(second, Vector3.Normalize(Vector3.Cross(meridian, p.Normal)), RollingSupport.Line);
        return default;
    }

    private static float AngularMass(ContactPoint c, Vector3 t)
    {
        var k = 0f;
        if (c.A.Motion == RigidMotionKind.Dynamic)
        {
            k += WorkshopCollisionKernels.InverseInertiaCoupling(c.A, t, t);
        }
        if (c.B.Motion == RigidMotionKind.Dynamic)
        {
            k += WorkshopCollisionKernels.InverseInertiaCoupling(c.B, t, t);
        }
        return k > 0f ? 1f / k : 0f;
    }

    public static void SolveRolling(ContactManifold manifold)
    {
        foreach (var patch in manifold.Patches) SolveRollingPatch(patch);
    }

    private static void SolveRollingPatch(ContactPatch m)
    {
        if (m.RollingMass1 == 0f && m.RollingMass2 == 0f) return;
        var c = m.Points[0];
        var maxRolling = 0f;
        for (var i = 0; i < m.Points.Count; i++)
            maxRolling += MathF.Max(m.Points[i].LambdaN, 0f) * m.RollingCoefficients[i];

        var w = new Vector3(c.A.Wx - c.B.Wx, c.A.Wy - c.B.Wy, c.A.Wz - c.B.Wz);
        var n1 = m.LambdaR1 - m.RollingMass1 * Vector3.Dot(w, m.Roll1);
        var n2 = m.LambdaR2 - m.RollingMass2 * Vector3.Dot(w, m.Roll2);

        var length = MathF.Sqrt(n1 * n1 + n2 * n2);
        if (length > maxRolling)
        {
            var scale = length > 0f ? maxRolling / length : 0f;
            n1 *= scale;
            n2 *= scale;
        }

        var d1 = n1 - m.LambdaR1;
        var d2 = n2 - m.LambdaR2;
        m.LambdaR1 = n1;
        m.LambdaR2 = n2;

        var t = d1 * m.Roll1 + d2 * m.Roll2;
        ApplyAngularImpulse(c.A, t);
        ApplyAngularImpulse(c.B, -t);
    }

    private static void ApplyAngularImpulse(SimBody b, Vector3 t)
    {
        if (b.Motion != RigidMotionKind.Dynamic) return;
        var dw = WorkshopCollisionKernels.ApplyInvInertia(b, t);
        b.Wx += dw.X;
        b.Wy += dw.Y;
        b.Wz += dw.Z;
    }

    /// <summary>The normal row's branch for a point at <paramref name="separation"/>, with s = separation + slop. Apart (separation
    /// &gt; 0), or inside the slop band (separation ≤ 0 &lt; s) with no soft sibling, it is rigid with a speculative allowance s/h in
    /// the solve and the relax pass (Box2D v3). Soft (s ≤ 0) it is soft in the solve pass, its push-out bounded. A deviation from
    /// Box2D's per-point rule: in a manifold where any point is soft, a point inside the band is soft too in the solve pass (bias
    /// biasRate·s) and gets no allowance in relax. Otherwise a line contact rests with one end on the rigid ledge and the other in
    /// the soft row's static sag, a tilt the friction rows turn into axial walking.</summary>
    private static (float Bias, float MassScale, float ImpulseScale) NormalBranch(float separation, ManifoldSoftness softness, ContactPass pass,
        in SoftCoefficients soft, float h)
    {
        var s = separation + ContactSlop;
        if (s > 0f && !(softness == ManifoldSoftness.SoftPoint && separation <= 0f)) return (s / h, 1f, 0f);
        if (pass == ContactPass.Relax) return (0f, 1f, 0f);
        return (MathF.Max(soft.BiasRate * s, -MaxPushoutSpeed), soft.MassScale, soft.ImpulseScale);
    }

    public static float CouplingMass(ContactPoint ci, ContactPoint cj)
    {
        var ni = new Vector3(ci.Nx, ci.Ny, ci.Nz);
        var nj = new Vector3(cj.Nx, cj.Ny, cj.Nz);
        // Contact normals are unit directions. The chord identity evaluates their
        // dot product without self-dot roundoff for identical normals, keeping the
        // block diagonal consistent with EffectiveMass while coupling nonparallel
        // rows by the same rule: n_i dot n_j = 1 - .5f * Vector3.DistanceSquared(ni, nj);
        var cosine = 1f - .5f * Vector3.DistanceSquared(ni, nj);
        var invMass = 0f;
        if (ci.A == cj.A) invMass += (ci.A.Motion == RigidMotionKind.Dynamic ? ci.A.InvMass : 0f);
        if (ci.B == cj.B) invMass += (ci.B.Motion == RigidMotionKind.Dynamic ? ci.B.InvMass : 0f);
        if (ci.A == cj.B) invMass -= (ci.A.Motion == RigidMotionKind.Dynamic ? ci.A.InvMass : 0f);
        if (ci.B == cj.A) invMass -= (ci.B.Motion == RigidMotionKind.Dynamic ? ci.B.InvMass : 0f);

        var k = invMass * cosine;

        if (ci.A == cj.A && ci.A.Motion == RigidMotionKind.Dynamic)
        {
            var ri = Vector3.Cross(new Vector3(ci.RAx, ci.RAy, ci.RAz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RAx, cj.RAy, cj.RAz), nj);
            k += WorkshopCollisionKernels.InverseInertiaCoupling(ci.A, ri, rj);
        }
        if (ci.B == cj.B && ci.B.Motion == RigidMotionKind.Dynamic)
        {
            var ri = Vector3.Cross(new Vector3(ci.RBx, ci.RBy, ci.RBz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RBx, cj.RBy, cj.RBz), nj);
            k += WorkshopCollisionKernels.InverseInertiaCoupling(ci.B, ri, rj);
        }
        if (ci.A == cj.B && ci.A.Motion == RigidMotionKind.Dynamic)
        {
            var ri = Vector3.Cross(new Vector3(ci.RAx, ci.RAy, ci.RAz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RBx, cj.RBy, cj.RBz), nj);
            k -= WorkshopCollisionKernels.InverseInertiaCoupling(ci.A, ri, rj);
        }
        if (ci.B == cj.A && ci.B.Motion == RigidMotionKind.Dynamic)
        {
            var ri = Vector3.Cross(new Vector3(ci.RBx, ci.RBy, ci.RBz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RAx, cj.RAy, cj.RAz), nj);
            k -= WorkshopCollisionKernels.InverseInertiaCoupling(ci.B, ri, rj);
        }
        return k;
    }

    /// <summary>Equilibrated principal solve. The pivot cutoff is dimensionless, relative to the
    /// normalized matrix scale, and rejects structural dependence without adding stiffness.</summary>
    private static bool SolvePrincipal(ReadOnlySpan<float> matrix, ReadOnlySpan<float> rhs,
        Span<float> x, int n)
    {
        Span<float> a = stackalloc float[n * n];
        Span<float> b = stackalloc float[n];
        Span<float> diagonal = stackalloc float[n];
        var magnitude = 0f;
        for (var i = 0; i < n; i++)
        {
            diagonal[i] = MathF.Sqrt(matrix[i * n + i]);
            if (!(diagonal[i] > 0f) || !float.IsFinite(diagonal[i])) return false;
        }
        for (var i = 0; i < n; i++)
        {
            b[i] = rhs[i] / diagonal[i];
            for (var j = 0; j < n; j++)
            {
                var value = matrix[i * n + j] / diagonal[i] / diagonal[j];
                if (!float.IsFinite(value)) return false;
                a[i * n + j] = value;
                magnitude = MathF.Max(magnitude, MathF.Abs(value));
            }
        }
        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var row = col + 1; row < n; row++)
                if (MathF.Abs(a[row * n + col]) > MathF.Abs(a[pivot * n + col])) pivot = row;
            if (MathF.Abs(a[pivot * n + col]) < (64f / 16777216f) * magnitude) return false;
            if (pivot != col)
            {
                for (var k = 0; k < n; k++)
                    (a[col * n + k], a[pivot * n + k]) = (a[pivot * n + k], a[col * n + k]);
                (b[col], b[pivot]) = (b[pivot], b[col]);
            }
            for (var row = col + 1; row < n; row++)
            {
                var f = a[row * n + col] / a[col * n + col];
                for (var k = col; k < n; k++) a[row * n + k] -= f * a[col * n + k];
                b[row] -= f * b[col];
            }
        }
        for (var row = n - 1; row >= 0; row--)
        {
            var sum = b[row];
            for (var k = row + 1; k < n; k++) sum -= a[row * n + k] * x[k];
            x[row] = sum / a[row * n + row];
            if (!float.IsFinite(x[row])) return false;
        }
        for (var i = 0; i < n; i++) x[i] /= diagonal[i];
        return true;
    }

    /// <summary>First admissible accumulated impulse, decreasing cardinality then lexicographic
    /// contact indices, empty last. All candidates are private until fully validated.
    /// z = lambdaNew / massScale; b = K * ((1 - impulseScale) / massScale * lambdaOld) - vn - bias.
    /// Thus K*z-b is the transformed soft/speculative residual, in m/s.
    /// The 64-u allowance bounds ordinary four-row rounding, not conditioning error or stopping speed.
    /// Failure discards this correction and continues; it does not choose another contact law.</summary>
    private static bool SolveActiveSet(ReadOnlySpan<float> matrix, ReadOnlySpan<float> rhs,
        Span<float> result, int n, float h)
    {
        Span<int> indices = stackalloc int[n];
        Span<float> principal = stackalloc float[n * n];
        Span<float> selectedRhs = stackalloc float[n];
        Span<float> selected = stackalloc float[n];
        Span<float> candidate = stackalloc float[n];
        for (var size = n; size >= 0; size--)
        {
            for (var i = 0; i < size; i++) indices[i] = i;
            while (true)
            {
                candidate.Clear();
                for (var i = 0; i < size; i++)
                {
                    selectedRhs[i] = rhs[indices[i]];
                    for (var j = 0; j < size; j++)
                        principal[i * size + j] = matrix[indices[i] * n + indices[j]];
                }
                var valid = SolvePrincipal(principal, selectedRhs, selected, size);
                for (var i = 0; valid && i < size; i++)
                {
                    valid = selected[i] >= 0f && float.IsFinite(selected[i]);
                    candidate[indices[i]] = selected[i];
                }
                var velocityScale = ContactSlop / h;
                for (var i = 0; valid && i < n; i++)
                {
                    var rowMagnitude = 0f;
                    for (var j = 0; j < n; j++)
                        rowMagnitude += MathF.Abs(matrix[i * n + j] * candidate[j]);
                    valid = float.IsFinite(rhs[i]) && float.IsFinite(rowMagnitude);
                    velocityScale = MathF.Max(velocityScale, MathF.Max(MathF.Abs(rhs[i]), rowMagnitude));
                }
                var tolerance = (64f / 16777216f) * velocityScale;
                for (var i = 0; valid && i < n; i++)
                {
                    var residual = -rhs[i];
                    for (var j = 0; j < n; j++) residual += matrix[i * n + j] * candidate[j];
                    var active = false;
                    for (var j = 0; j < size; j++) active |= indices[j] == i;
                    valid = float.IsFinite(residual) &&
                        (active ? MathF.Abs(residual) <= tolerance : residual >= -tolerance);
                }
                if (valid)
                {
                    candidate.CopyTo(result);
                    return true;
                }
                var next = size - 1;
                while (next >= 0 && indices[next] == n - size + next) next--;
                if (next < 0) break;
                indices[next]++;
                for (var i = next + 1; i < size; i++) indices[i] = indices[i - 1] + 1;
            }
        }
        return false;
    }

    private static void ApplyNormalImpulse(ContactPoint c, float total)
    {
        var delta = total - c.LambdaN;
        c.LambdaN = total;
        if (total > c.MaxImpulse) c.MaxImpulse = total;
        var p = delta * new Vector3(c.Nx, c.Ny, c.Nz);
        WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
        WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
    }

    public static void PrepareNormalBlocks(ContactManifold manifold, float h, HashSet<ContactFeatureSetKey> cache)
    {
        manifold.NormalBlockCount = 0;
        manifold.NormalWarmStartKey = new(manifold.Points);
        var closed = manifold.ClosedScratch;
        var speculative = manifold.SpeculativeScratch;
        closed.Clear(); speculative.Clear();
        foreach (var point in manifold.Points)
            (point.Gap <= 0f || point.Gap + h * Vector3.Dot(RelativeVelocity(point), point.Normal) <= 0f
                ? closed : speculative).Add(point);
        var changed = !cache.Contains(manifold.NormalWarmStartKey);
        Partition(closed);
        Partition(speculative);
        // A changed block must not inherit only part of a formerly coupled load.
        if (changed)
            foreach (var point in manifold.Points) point.LambdaN = 0f;

        void Partition(List<ContactPoint> pending)
        {
            while (pending.Count != 0)
            {
                if (manifold.NormalBlocks.Count == manifold.NormalBlockCount)
                    manifold.NormalBlocks.Add(new NormalContactBlock());
                var block = manifold.NormalBlocks[manifold.NormalBlockCount++];
                block.Points.Clear();
                var selected = manifold.SelectionScratch;
                selected.Clear(); selected.AddRange(pending);
                WorkshopCollisionKernels.ReduceSideManifold(selected, 0);
                block.Points.AddRange(selected);
                block.Key = new(block.Points);
                changed |= !cache.Contains(block.Key);
                foreach (var point in block.Points) pending.Remove(point);
            }
        }
    }

    public static void SolveNormalBlock(List<ContactPoint> group, ContactPass pass, in SoftCoefficients soft, float h)
    {
        var n = group.Count;
        Span<float> rhs = stackalloc float[n];
        Span<float> scale = stackalloc float[n];
        Span<float> retained = stackalloc float[n];
        Span<float> matrix = stackalloc float[n * n];
        Span<float> separation = stackalloc float[n];
        var softness = ManifoldSoftness.NoSoftPoint;
        for (var i = 0; i < n; i++)
        {
            separation[i] = CurrentSeparation(group[i]);
            if (separation[i] + ContactSlop <= 0f) softness = ManifoldSoftness.SoftPoint;
        }
        for (var i = 0; i < n; i++)
        {
            var c = group[i];
            var vn = Vector3.Dot(RelativeVelocity(c), new Vector3(c.Nx, c.Ny, c.Nz));
            var (bias, massScale, impulseScale) = NormalBranch(separation[i], softness, pass, soft, h);
            if (!(massScale > 0f) || !float.IsFinite(massScale)) return;
            scale[i] = massScale;
            retained[i] = (1f - impulseScale) / massScale * c.LambdaN;
            rhs[i] = -(vn + bias);
            for (var j = 0; j < n; j++) matrix[i * n + j] = CouplingMass(c, group[j]);
        }
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++) rhs[i] += matrix[i * n + j] * retained[j];
        Span<float> solution = stackalloc float[n];
        if (!SolveActiveSet(matrix, rhs, solution, n, h)) return;
        for (var i = 0; i < n; i++)
        {
            solution[i] *= scale[i];
            if (!float.IsFinite(solution[i]) || !float.IsFinite(solution[i] - group[i].LambdaN)) return;
        }
        for (var i = 0; i < n; i++) ApplyNormalImpulse(group[i], solution[i]);
    }

    public static void SweepManifolds(List<ContactManifold> manifolds, ContactPass pass, in SoftCoefficients soft, float h)
    {
        foreach (var m in manifolds)
        {
            WorkshopFrictionSolver.Solve(m);
            for (var block = 0; block < m.NormalBlockCount; block++)
                SolveNormalBlock(m.NormalBlocks[block].Points, pass, soft, h);
            SolveRolling(m);
        }
    }

    /// <summary>A point bounces when it carried normal impulse this substep (touching or speculative) and approached faster than
    /// the pair's bounce threshold at the substep start (Box2D v3 b2ApplyRestitution).</summary>
    private static bool Bounces(ContactPoint c) => c.Restitution != 0f && c.RelVel < -c.Threshold && c.MaxImpulse > 0f;

    /// <summary>a·I·c for the body's compiled principal moments in its current principal frame (an axis with zero inverse moment
    /// contributes nothing).</summary>
    private static float InertiaDot(SimBody b, Vector3 a, Vector3 c)
    {
        var toPrincipal = Quaternion.Conjugate(Quaternion.Multiply(b.Rotation, b.Principal));
        var ap = Vector3.Transform(a, toPrincipal);
        var cp = Vector3.Transform(c, toPrincipal);
        var sum = 0f;
        if (b.InvI.X > 0f) sum += ap.X * cp.X / b.InvI.X;
        if (b.InvI.Y > 0f) sum += ap.Y * cp.Y / b.InvI.Y;
        if (b.InvI.Z > 0f) sum += ap.Z * cp.Z / b.InvI.Z;
        return sum;
    }

    /// <summary>The body's kinetic energy at linear velocity v and angular velocity w.</summary>
    public static float Kinetic(SimBody b, Vector3 v, Vector3 w) => .5f * b.Mass * Vector3.Dot(v, v) + .5f * InertiaDot(b, w, w);

    private static SimBody Root(SimBody b)
    {
        while (b.RestitutionRoot != b)
        {
            b.RestitutionRoot = b.RestitutionRoot.RestitutionRoot;
            b = b.RestitutionRoot;
        }
        return b;
    }

    private static void Join(SimBody b, List<SimBody> touched, int stamp)
    {
        if (b.Motion != RigidMotionKind.Dynamic || b.RestitutionStamp == stamp) return;
        b.RestitutionStamp = stamp;
        b.RestitutionRoot = b;
        b.RelaxedV = b.LinearVelocity;
        b.RelaxedW = b.AngularVelocity;
        touched.Add(b);
    }

    /// <summary>An active supporting contact carried normal impulse this substep and approached slower than the bounce
    /// threshold at substep start (|v_rel| ≈ 0), resting on its support.</summary>
    private static bool Supports(ContactPoint c) => c.MaxImpulse > 0f && c.RelVel >= -c.Threshold;

    [ThreadStatic] private static List<ContactPoint>? s_islandPoints;
    [ThreadStatic] private static List<ContactManifold>? s_islandManifolds;

    /// <summary>The substep's restitution pass over bouncing islands, bounded by energy. Bouncing contacts are grouped with the active
    /// supporting contacts (c.MaxImpulse > 0, |v_rel| ≈ 0) of touched dynamic bodies. Contacts are coupled across manifolds using generalized
    /// multi-body CouplingMass in ApplyRestitution, then friction and rolling are updated. Finally, each group's kinetic energy is bounded
    /// to max(E_relaxed, E_pre-solve).</summary>
    public static void RestitutionPass(List<ContactManifold> manifolds, List<SimBody> touched, int stamp)
    {
        touched.Clear();
        foreach (var m in manifolds)
        {
            var any = false;
            foreach (var c in m.Points) any |= Bounces(c);
            if (!any) continue;
            var c0 = m.Points[0];
            Join(c0.A, touched, stamp);
            Join(c0.B, touched, stamp);
            if (c0.A.Motion == RigidMotionKind.Dynamic && c0.B.Motion == RigidMotionKind.Dynamic)
            {
                var ra = Root(c0.A);
                var rb = Root(c0.B);
                if (ra != rb) rb.RestitutionRoot = ra;
            }
        }
        if (touched.Count == 0) return;

        bool expanded;
        do
        {
            expanded = false;
            foreach (var m in manifolds)
            {
                if (m.Points.Count == 0) continue;
                var hasActiveSupport = false;
                foreach (var c in m.Points)
                {
                    if (Supports(c)) { hasActiveSupport = true; break; }
                }
                if (!hasActiveSupport) continue;

                var c0 = m.Points[0];
                var aTouched = c0.A.Motion == RigidMotionKind.Dynamic && c0.A.RestitutionStamp == stamp;
                var bTouched = c0.B.Motion == RigidMotionKind.Dynamic && c0.B.RestitutionStamp == stamp;
                if (!aTouched && !bTouched) continue;
                if (!((aTouched && c0.Ny > 0f) || (bTouched && c0.Ny < 0f))) continue;

                var oldCount = touched.Count;
                Join(c0.A, touched, stamp);
                Join(c0.B, touched, stamp);
                if (c0.A.Motion == RigidMotionKind.Dynamic && c0.B.Motion == RigidMotionKind.Dynamic)
                {
                    var ra = Root(c0.A);
                    var rb = Root(c0.B);
                    if (ra != rb) rb.RestitutionRoot = ra;
                }
                if (touched.Count > oldCount) expanded = true;
            }
        } while (expanded);

        s_islandPoints ??= new(32);
        s_islandManifolds ??= new(16);

        try
        {
            foreach (var b in touched)
            {
                if (Root(b) != b) continue;
                var root = b;
                s_islandPoints.Clear();
                s_islandManifolds.Clear();

                foreach (var m in manifolds)
                {
                    if (m.Points.Count == 0) continue;
                    var c0 = m.Points[0];
                    var aInIsland = c0.A.Motion == RigidMotionKind.Dynamic && c0.A.RestitutionStamp == stamp && Root(c0.A) == root;
                    var bInIsland = c0.B.Motion == RigidMotionKind.Dynamic && c0.B.RestitutionStamp == stamp && Root(c0.B) == root;
                    if (!aInIsland && !bInIsland) continue;

                    var hasBounce = false;
                    foreach (var c in m.Points) hasBounce |= Bounces(c);
                    var supportsIsland = (aInIsland && bInIsland) || (aInIsland && c0.Ny > 0f) || (bInIsland && c0.Ny < 0f);
                    var startCount = s_islandPoints.Count;
                    for (var block = 0; block < m.NormalBlockCount; block++)
                    {
                        var blockPoints = m.NormalBlocks[block].Points;
                        for (var i = 0; i < blockPoints.Count; i++)
                        {
                            var c = blockPoints[i];
                            if (Bounces(c) || (!hasBounce && supportsIsland && Supports(c)))
                                s_islandPoints.Add(c);
                        }
                    }
                    if (s_islandPoints.Count > startCount)
                        s_islandManifolds.Add(m);
                }

                if (s_islandPoints.Count > 8)
                {
                    s_islandPoints.Sort((x, y) => (Bounces(y) ? 1 : 0).CompareTo(Bounces(x) ? 1 : 0));
                    s_islandPoints.RemoveRange(8, s_islandPoints.Count - 8);
                }

                for (var i = 0; i < s_islandPoints.Count; i++)
                    s_islandPoints[i].RelaxedLambdaN = s_islandPoints[i].LambdaN;
                foreach (var m in s_islandManifolds)
                    foreach (var p in m.Patches)
                    {
                        p.RelaxedLambda = p.Lambda;
                        p.RelaxedLambdaR1 = p.LambdaR1;
                        p.RelaxedLambdaR2 = p.LambdaR2;
                    }

                ApplyRestitution(s_islandPoints);

                foreach (var m in s_islandManifolds)
                {
                    WorkshopFrictionSolver.Solve(m);
                    SolveRolling(m);
                }

                root.EnergyA = 0f; root.EnergyB = 0f; root.EnergyC = 0f; root.EnergyBudget = 0f;
                foreach (var body in touched)
                {
                    if (Root(body) != root) continue;
                    var dv = body.LinearVelocity - body.RelaxedV;
                    var dw = body.AngularVelocity - body.RelaxedW;
                    var relaxed = Kinetic(body, body.RelaxedV, body.RelaxedW);
                    root.EnergyA += Kinetic(body, dv, dw);
                    root.EnergyB += body.Mass * Vector3.Dot(body.RelaxedV, dv) + InertiaDot(body, body.RelaxedW, dw);
                    root.EnergyC += relaxed;
                    root.EnergyBudget += body.PreSolveKinetic - relaxed;
                }

                float a = root.EnergyA, lin = root.EnergyB, energyC = root.EnergyC, bound = energyC + MathF.Max(0f, root.EnergyBudget);
                root.EnergyScale = 1f;
                if (a > 0f && a + lin + energyC > bound * (1f + 1e-5f))
                {
                    var disc = lin * lin + 4f * a * (bound - energyC);
                    root.EnergyScale = Math.Clamp((-lin + MathF.Sqrt(MathF.Max(0f, disc))) / (2f * a), 0f, 1f);
                }

                var scale = root.EnergyScale;
                if (scale < 1f)
                {
                    foreach (var body in touched)
                    {
                        if (Root(body) != root) continue;
                        var v = body.RelaxedV + scale * (body.LinearVelocity - body.RelaxedV);
                        var w = body.RelaxedW + scale * (body.AngularVelocity - body.RelaxedW);
                        body.Vx = v.X; body.Vy = v.Y; body.Vz = v.Z;
                        body.Wx = w.X; body.Wy = w.Y; body.Wz = w.Z;
                    }
                    foreach (var m in s_islandManifolds)
                    {
                        foreach (var p in m.Patches)
                        {
                            p.Lambda = p.RelaxedLambda + scale * (p.Lambda - p.RelaxedLambda);
                            p.LambdaR1 = p.RelaxedLambdaR1 + scale * (p.LambdaR1 - p.RelaxedLambdaR1);
                            p.LambdaR2 = p.RelaxedLambdaR2 + scale * (p.LambdaR2 - p.RelaxedLambdaR2);
                        }
                    }
                }
                for (var i = 0; i < s_islandPoints.Count; i++)
                {
                    var pt = s_islandPoints[i];
                    if (Bounces(pt))
                        pt.LambdaN = pt.RelaxedLambdaN + scale * (pt.LambdaN - pt.RelaxedLambdaN);
                    else
                        pt.LambdaN = pt.RelaxedLambdaN;
                }
            }
        }
        finally
        {
            s_islandPoints.Clear();
            s_islandManifolds.Clear();
        }
    }

    /// <summary>The coupled restitution pass over an island of bouncing points and active supporting contacts. Bouncing contacts
    /// target -e*RelVel and supporting contacts target 0 (unilateral non-penetration), solved through the accumulated active-set
    /// rule with massScale 1 and impulseScale 0. Existing support is included in b; the substep energy limiter remains authoritative
    /// after this correction.</summary>
    public static void ApplyRestitution(List<ContactPoint> points)
    {
        var count = points.Count;
        Span<int> index = stackalloc int[count];
        var n = 0;
        var anyBounce = false;
        for (var i = 0; i < count; i++)
        {
            var c = points[i];
            if (Bounces(c)) anyBounce = true;
            if (Bounces(c) || Supports(c)) index[n++] = i;
        }
        if (n == 0 || !anyBounce) return;
        Span<float> matrix = stackalloc float[n * n];
        Span<float> rhs = stackalloc float[n];
        Span<float> solution = stackalloc float[n];
        for (var i = 0; i < n; i++)
        {
            var c = points[index[i]];
            var vn = Vector3.Dot(RelativeVelocity(c), new Vector3(c.Nx, c.Ny, c.Nz));
            var target = Bounces(c) ? c.Restitution * c.RelVel : 0f;
            rhs[i] = -(vn + target);
            for (var j = 0; j < n; j++)
            {
                var k = CouplingMass(c, points[index[j]]);
                matrix[i * n + j] = k;
                rhs[i] += k * points[index[j]].LambdaN;
            }
        }
        var solved = SolveActiveSet(matrix, rhs, solution, n, SubstepSeconds);
        if (!solved) return;
        for (var i = 0; i < n; i++)
        {
            if (!float.IsFinite(solution[i]) || solution[i] < 0f) return;
        }
        for (var i = 0; i < n; i++)
        {
            ApplyNormalImpulse(points[index[i]], solution[i]);
        }
    }
}
