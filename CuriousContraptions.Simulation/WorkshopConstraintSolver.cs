using System;
using System.Collections.Generic;
using System.Numerics;

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

internal static class WorkshopConstraintSolver
{
    public const float SubstepSeconds = 1.0f / 480.0f;
    public const float ContactSlop = 0.0002f;
    public const float SpeculativeSlop = 0.004f;
    public const float MaxLinearSpeed = 64f;
    public const float MaxAngularSpeed = 128f;
    public const float SpeedClamp = 64f * (1f - (1f / 1048576f));
    public const float SpinClamp = 128f * (1f - (1f / 1048576f));
    public const float ContactHertz = 60f;
    public const float ContactDampingRatio = 10f;
    public const float MaxPushoutSpeed = 3f;
    public const int SolverIterations = 4;
    public const int RelaxIterations = 4;
    public const float BlockRegularization = 1e-3f;

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
        var k = (A.Motion == 1 ? A.InvMass : 0f) + (B.Motion == 1 ? B.InvMass : 0f);
        if (A.Motion == 1)
        {
            var r = Vector3.Cross(new Vector3(c.RAx, c.RAy, c.RAz), d);
            var w = WorkshopCollisionKernels.ApplyInvInertia(A, r);
            k += Vector3.Dot(r, w);
        }
        if (B.Motion == 1)
        {
            var r = Vector3.Cross(new Vector3(c.RBx, c.RBy, c.RBz), d);
            var w = WorkshopCollisionKernels.ApplyInvInertia(B, r);
            k += Vector3.Dot(r, w);
        }
        return k > 0f ? 1f / k : 0f;
    }

    public static void PrepareContact(ContactPoint c, Dictionary<ulong, (float LambdaN, float LambdaT1, float LambdaT2)> cache)
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

        c.NormalMass = EffectiveMass(c, n);
        c.TangentMass1 = EffectiveMass(c, c.T1);
        c.TangentMass2 = EffectiveMass(c, c.T2);

        // The approach before this substep's external velocity changes (guides, supplied drives, gravity, drag).
        var vA = StartVelocity(A, new Vector3(c.RAx, c.RAy, c.RAz));
        var vB = StartVelocity(B, new Vector3(c.RBx, c.RBy, c.RBz));
        c.RelVel = Vector3.Dot(vA - vB, n);

        c.Restitution = c.ColA.Restitution * c.ColB.Restitution;
        c.Threshold = MathF.Max(c.ColA.BounceThreshold, c.ColB.BounceThreshold);
        c.Friction = MathF.Sqrt(c.ColA.Friction * c.ColB.Friction);

        var lo = Math.Min(c.ColA.Index, c.ColB.Index);
        var hi = Math.Max(c.ColA.Index, c.ColB.Index);
        c.Key = ((ulong)(uint)lo << 40) | ((ulong)(uint)hi << 20) | (ulong)(uint)c.Feature;

        if (cache.TryGetValue(c.Key, out var warm))
        {
            c.LambdaN = warm.LambdaN;
            c.LambdaT1 = warm.LambdaT1;
            c.LambdaT2 = warm.LambdaT2;
        }
        else
        {
            c.LambdaN = 0f;
            c.LambdaT1 = 0f;
            c.LambdaT2 = 0f;
        }

        c.MaxImpulse = 0f;
    }

    private static Vector3 StartVelocity(SimBody b, Vector3 r) => b.Motion == 1 ? b.V0 + Vector3.Cross(b.W0, r) : Vector3.Zero;

    public static void WarmStartContact(ContactPoint c)
    {
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        var p = c.LambdaN * n + c.LambdaT1 * c.T1 + c.LambdaT2 * c.T2;
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
        if (b.Motion != 1) return Vector3.Zero;
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
        if (support == ContactSupport.Axis && b.Motion == 1)
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

    public static void SolveFriction(ContactPoint c)
    {
        var v = RelativeVelocity(c);
        var vt1 = Vector3.Dot(v, c.T1);
        var vt2 = Vector3.Dot(v, c.T2);

        var n1 = c.LambdaT1 - c.TangentMass1 * vt1;
        var n2 = c.LambdaT2 - c.TangentMass2 * vt2;
        var maxFriction = c.Friction * c.LambdaN;
        var length = MathF.Sqrt(n1 * n1 + n2 * n2);
        if (length > maxFriction)
        {
            var scale = length > 0f ? maxFriction / length : 0f;
            n1 *= scale;
            n2 *= scale;
        }

        var d1 = n1 - c.LambdaT1;
        var d2 = n2 - c.LambdaT2;
        c.LambdaT1 = n1;
        c.LambdaT2 = n2;

        var p = d1 * c.T1 + d2 * c.T2;
        WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
        WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
    }

    public static void PrepareRolling(ContactManifold m)
    {
        var c = m.Points[0];
        var radiusA = RollingArm(m, c.ColA, true);
        var radiusB = RollingArm(m, c.ColB, false);
        m.Rolling = MathF.Max(c.ColA.RollingResistance, c.ColB.RollingResistance) * MathF.Max(radiusA, radiusB);
        var line = RollingAxis(m, radiusA > 0f, radiusB > 0f);
        m.Roll1 = line ?? c.T1;
        m.Roll2 = line is null ? c.T2 : Vector3.Zero;
        m.RollingMass1 = m.Rolling > 0f ? AngularMass(c, m.Roll1) : 0f;
        m.RollingMass2 = m.Rolling > 0f && line is null ? AngularMass(c, m.Roll2) : 0f;
        m.LambdaR1 = 0f;
        m.LambdaR2 = 0f;
    }

    /// <summary>The single rolling axis of a manifold whose curved supports roll about one line, or null when it rolls about both
    /// tangents. A sphere rolls about every tangent direction. A curved cylinder side (an axis-support side, <see cref="RollingArm"/>)
    /// rolls only about its own axis, projected into the tangent plane of an axis-support point: the other tangent is pitch, a rocking of
    /// the line contact that its normal rows carry. Two curved sides whose raw axes are parallel within the narrowphase's 2°
    /// (<see cref="WorkshopCollisionKernels.LineAlignment"/>, the same test) roll about their common axis, the sign-aligned bisector;
    /// crossed sides touch at a point and roll about both tangents.</summary>
    private static Vector3? RollingAxis(ContactManifold m, bool curvedA, bool curvedB)
    {
        var c = m.Points[0];
        if (c.ColA.Shape == ShapeKind.Sphere || c.ColB.Shape == ShapeKind.Sphere) return null;
        if (!curvedA && !curvedB) return null;
        Vector3 axis;
        if (curvedA && curvedB)
        {
            var dot = Vector3.Dot(c.ColA.U1, c.ColB.U1);
            if (MathF.Abs(dot) < WorkshopCollisionKernels.LineAlignment) return null;
            axis = Vector3.Normalize(dot >= 0f ? c.ColA.U1 + c.ColB.U1 : c.ColA.U1 - c.ColB.U1);
        }
        else axis = curvedA ? c.ColA.U1 : c.ColB.U1;
        // An axis-support point's own normal is within the side tolerance of its axis's perpendicular plane (|n·a| ≤ 0.199), so the
        // projection keeps at least √(1 − 0.199²) = 0.98 of the unit axis. A projection under half of it is impossible for a classified
        // side; it would carry no rolling line, so the row then rolls about both tangents and no NaN reaches it.
        var n = AxisSupportNormal(m, curvedA);
        var t = axis - Vector3.Dot(axis, n) * n;
        var length = t.Length();
        return length >= .5f ? t / length : null;
    }

    /// <summary>The normal of the manifold's first axis-support point on side A (or on side B).</summary>
    private static Vector3 AxisSupportNormal(ContactManifold m, bool sideA)
    {
        foreach (var p in m.Points)
            if ((sideA ? p.SupportA : p.SupportB) == ContactSupport.Axis) return p.Normal;
        return m.Points[0].Normal;
    }

    /// <summary>The rolling arm one side brings to its manifold: a sphere's radius at every contact, a cylinder's radius when the
    /// manifold rolls on its curved side (any axis-support point, as the narrowphase classified it), otherwise none (flat faces,
    /// rims and caps do not roll).</summary>
    private static float RollingArm(ContactManifold m, SimCollider collider, bool sideA)
    {
        if (collider.Shape == ShapeKind.Sphere) return collider.Radius;
        if (collider.Shape != ShapeKind.Cylinder) return 0f;
        foreach (var p in m.Points)
            if ((sideA ? p.SupportA : p.SupportB) == ContactSupport.Axis) return collider.Radius;
        return 0f;
    }

    private static float AngularMass(ContactPoint c, Vector3 t)
    {
        var k = 0f;
        if (c.A.Motion == 1)
        {
            var w = WorkshopCollisionKernels.ApplyInvInertia(c.A, t);
            k += Vector3.Dot(t, w);
        }
        if (c.B.Motion == 1)
        {
            var w = WorkshopCollisionKernels.ApplyInvInertia(c.B, t);
            k += Vector3.Dot(t, w);
        }
        return k > 0f ? 1f / k : 0f;
    }

    public static void SolveRolling(ContactManifold m)
    {
        if (m.RollingMass1 == 0f && m.RollingMass2 == 0f) return;
        var c = m.Points[0];
        var normal = 0f;
        foreach (var p in m.Points) normal += p.LambdaN;

        var w = new Vector3(c.A.Wx - c.B.Wx, c.A.Wy - c.B.Wy, c.A.Wz - c.B.Wz);
        var n1 = m.LambdaR1 - m.RollingMass1 * Vector3.Dot(w, m.Roll1);
        var n2 = m.LambdaR2 - m.RollingMass2 * Vector3.Dot(w, m.Roll2);

        var maxRolling = m.Rolling * normal;
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
        if (b.Motion != 1) return;
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
    private static (float Bias, float MassScale, float ImpulseScale) NormalBranch(float separation, bool manifoldSoft, bool useBias,
        in SoftCoefficients soft, float h)
    {
        var s = separation + ContactSlop;
        if (s > 0f && !(manifoldSoft && separation <= 0f)) return (s / h, 1f, 0f);
        if (!useBias) return (0f, 1f, 0f);
        return (MathF.Max(soft.BiasRate * s, -MaxPushoutSpeed), soft.MassScale, soft.ImpulseScale);
    }

    /// <summary>One normal row, solved alone. <paramref name="manifoldSoft"/>: some point of its manifold is soft (s ≤ 0); false for a
    /// single-point manifold.</summary>
    public static void SolveNormalRow(ContactPoint c, bool manifoldSoft, bool useBias, in SoftCoefficients soft, float h)
    {
        var v = RelativeVelocity(c);
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        var vn = Vector3.Dot(v, n);
        var (bias, massScale, impulseScale) = NormalBranch(CurrentSeparation(c), manifoldSoft, useBias, soft, h);

        var impulse = -c.NormalMass * massScale * (vn + bias) - impulseScale * c.LambdaN;
        var total = MathF.Max(c.LambdaN + impulse, 0f);
        impulse = total - c.LambdaN;
        c.LambdaN = total;
        if (total > c.MaxImpulse) c.MaxImpulse = total;

        var p = impulse * n;
        WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
        WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
    }

    public static float CouplingMass(ContactPoint ci, ContactPoint cj)
    {
        var A = ci.A;
        var B = ci.B;
        var k = (A.Motion == 1 ? A.InvMass : 0f) + (B.Motion == 1 ? B.InvMass : 0f);

        var ni = new Vector3(ci.Nx, ci.Ny, ci.Nz);
        var nj = new Vector3(cj.Nx, cj.Ny, cj.Nz);

        if (A.Motion == 1)
        {
            var ri = Vector3.Cross(new Vector3(ci.RAx, ci.RAy, ci.RAz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RAx, cj.RAy, cj.RAz), nj);
            var w = WorkshopCollisionKernels.ApplyInvInertia(A, rj);
            k += Vector3.Dot(ri, w);
        }
        if (B.Motion == 1)
        {
            var ri = Vector3.Cross(new Vector3(ci.RBx, ci.RBy, ci.RBz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RBx, cj.RBy, cj.RBz), nj);
            var w = WorkshopCollisionKernels.ApplyInvInertia(B, rj);
            k += Vector3.Dot(ri, w);
        }
        return k;
    }

    public static bool SolveLinear(ReadOnlySpan<float> matrix, ReadOnlySpan<float> rhs, Span<float> x, int n)
    {
        Span<float> a = stackalloc float[n * n];
        Span<float> b = stackalloc float[n];
        matrix.CopyTo(a);
        rhs.CopyTo(b);

        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var row = col + 1; row < n; row++)
                if (MathF.Abs(a[row * n + col]) > MathF.Abs(a[pivot * n + col])) pivot = row;

            if (MathF.Abs(a[pivot * n + col]) < 1e-12f) return false;

            if (pivot != col)
            {
                for (var k = 0; k < n; k++)
                {
                    var t = a[col * n + k];
                    a[col * n + k] = a[pivot * n + k];
                    a[pivot * n + k] = t;
                }
                var tb = b[col]; b[col] = b[pivot]; b[pivot] = tb;
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
        }
        return true;
    }

    public static void SolveNormalBlock(List<ContactPoint> group, bool useBias, in SoftCoefficients soft, float h)
    {
        var n = group.Count;
        if (n == 1) { SolveNormalRow(group[0], false, useBias, soft, h); return; }

        Span<float> rhs = stackalloc float[n];
        Span<float> scale = stackalloc float[n];
        Span<float> impulseScale = stackalloc float[n];
        Span<float> matrix = stackalloc float[n * n];
        Span<float> separation = stackalloc float[n];
        var manifoldSoft = false;
        for (var i = 0; i < n; i++)
        {
            separation[i] = CurrentSeparation(group[i]);
            manifoldSoft |= separation[i] + ContactSlop <= 0f;
        }

        for (var i = 0; i < n; i++)
        {
            var c = group[i];
            var v = RelativeVelocity(c);
            var normal = new Vector3(c.Nx, c.Ny, c.Nz);
            var vn = Vector3.Dot(v, normal);
            var (bias, massScale, rowImpulseScale) = NormalBranch(separation[i], manifoldSoft, useBias, soft, h);
            scale[i] = massScale;
            impulseScale[i] = rowImpulseScale;

            rhs[i] = -(vn + bias);
            for (var j = 0; j < n; j++) matrix[i * n + j] = CouplingMass(c, group[j]);
        }

        var trace = 0f;
        for (var i = 0; i < n; i++) trace += matrix[i * n + i];
        for (var i = 0; i < n; i++) matrix[i * n + i] += BlockRegularization * trace / n;

        Span<float> solution = stackalloc float[n];
        if (!SolveLinear(matrix, rhs, solution, n))
        {
            for (var i = 0; i < n; i++) SolveNormalRow(group[i], manifoldSoft, useBias, soft, h);
            return;
        }

        Span<float> deltas = stackalloc float[n];
        for (var i = 0; i < n; i++)
        {
            deltas[i] = scale[i] * solution[i] - impulseScale[i] * group[i].LambdaN;
            if (group[i].LambdaN + deltas[i] < 0f)
            {
                for (var j = 0; j < n; j++) SolveNormalRow(group[j], manifoldSoft, useBias, soft, h);
                return;
            }
        }

        for (var i = 0; i < n; i++)
        {
            var c = group[i];
            c.LambdaN += deltas[i];
            if (c.LambdaN > c.MaxImpulse) c.MaxImpulse = c.LambdaN;

            var normal = new Vector3(c.Nx, c.Ny, c.Nz);
            var p = deltas[i] * normal;
            WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
            WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
        }
    }

    public static void SweepManifolds(List<ContactManifold> manifolds, bool useBias, in SoftCoefficients soft, float h)
    {
        foreach (var m in manifolds)
        {
            foreach (var c in m.Points) SolveFriction(c);
            SolveNormalBlock(m.Points, useBias, soft, h);
            SolveRolling(m);
        }
    }

    /// <summary>A point bounces when it carried normal impulse this substep (touching or speculative) and approached faster than
    /// the pair's bounce threshold at the substep start (Box2D v3 b2ApplyRestitution).</summary>
    private static bool Bounces(ContactPoint c) => c.Restitution != 0f && c.RelVel < -c.Threshold && c.MaxImpulse > 0f;

    /// <summary>The restitution pass for one manifold, once per substep after relax: the bouncing points' restitution
    /// (<see cref="ApplyRestitution"/>), then one friction and rolling pass whose cones now include the restitution impulse, so a
    /// bounce keeps its Coulomb share of tangential impulse (each 480 Hz substep is a whole Box2D step, so friction had only the
    /// stop's impulse before).</summary>
    public static void Restitute(ContactManifold m)
    {
        var any = false;
        foreach (var c in m.Points) any |= Bounces(c);
        if (!any) return;
        ApplyRestitution(m.Points);
        foreach (var c in m.Points) SolveFriction(c);
        SolveRolling(m);
    }

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
        if (b.Motion != 1 || b.RestitutionStamp == stamp) return;
        b.RestitutionStamp = stamp;
        b.RestitutionRoot = b;
        b.RelaxedV = b.LinearVelocity;
        b.RelaxedW = b.AngularVelocity;
        touched.Add(b);
    }

    /// <summary>The substep's restitution pass over every manifold (<see cref="Restitute"/>), bounded by energy. The bodies joined by
    /// this substep's bouncing manifolds form groups; a group's kinetic energy after the pass may not exceed the larger of its
    /// kinetic energy before the contact solve and after the relax sweeps, so restitution returns at most what this substep's
    /// contacts removed. Independent Newton targets on a body shared by several impacts (a box landing on a box resting on the
    /// bench, which bounces on the bench in the same substep) can otherwise return more than was removed. Such a group's change is
    /// scaled back along the straight line from its relaxed velocities to the largest fraction the bound allows.</summary>
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
            if (c0.A.Motion == 1 && c0.B.Motion == 1)
            {
                var ra = Root(c0.A);
                var rb = Root(c0.B);
                if (ra != rb) rb.RestitutionRoot = ra;
            }
        }
        if (touched.Count == 0) return;
        foreach (var m in manifolds) Restitute(m);

        foreach (var b in touched) { b.EnergyA = 0f; b.EnergyB = 0f; b.EnergyC = 0f; b.EnergyBudget = 0f; }
        foreach (var b in touched)
        {
            var r = Root(b);
            var dv = b.LinearVelocity - b.RelaxedV;
            var dw = b.AngularVelocity - b.RelaxedW;
            var relaxed = Kinetic(b, b.RelaxedV, b.RelaxedW);
            r.EnergyA += Kinetic(b, dv, dw);
            r.EnergyB += b.Mass * Vector3.Dot(b.RelaxedV, dv) + InertiaDot(b, b.RelaxedW, dw);
            r.EnergyC += relaxed;
            r.EnergyBudget += b.PreSolveKinetic - relaxed;
        }
        foreach (var b in touched)
        {
            if (Root(b) != b) continue;
            // Along the relaxed → restituted line the group's energy is c + bα + aα²; the bound is max(pre-solve, relaxed).
            float a = b.EnergyA, lin = b.EnergyB, c = b.EnergyC, bound = c + MathF.Max(0f, b.EnergyBudget);
            b.EnergyScale = 1f;
            if (a <= 0f || a + lin + c <= bound * (1f + 1e-5f)) continue;
            var disc = lin * lin + 4f * a * (bound - c);
            b.EnergyScale = Math.Clamp((-lin + MathF.Sqrt(MathF.Max(0f, disc))) / (2f * a), 0f, 1f);
        }
        foreach (var b in touched)
        {
            var scale = Root(b).EnergyScale;
            if (scale >= 1f) continue;
            var v = b.RelaxedV + scale * (b.LinearVelocity - b.RelaxedV);
            var w = b.RelaxedW + scale * (b.AngularVelocity - b.RelaxedW);
            b.Vx = v.X; b.Vy = v.Y; b.Vz = v.Z;
            b.Wx = w.X; b.Wy = w.Y; b.Wz = w.Z;
        }
    }

    /// <summary>Every bouncing point's relative normal velocity is driven to −e·(its own substep-start approach) in one simultaneous
    /// solve, so a face landing flat leaves flat; when that solve is singular or would pull a point (accumulated impulse below
    /// zero) the points are applied one at a time, each clamped. No soft-contact or penetration bias enters the target.</summary>
    public static void ApplyRestitution(List<ContactPoint> points)
    {
        var count = points.Count;
        Span<int> index = stackalloc int[count];
        var n = 0;
        for (var i = 0; i < count; i++)
            if (Bounces(points[i])) index[n++] = i;
        if (n == 0) return;
        if (n == 1) { ApplyRestitutionRow(points[index[0]]); return; }

        Span<float> matrix = stackalloc float[n * n];
        Span<float> rhs = stackalloc float[n];
        Span<float> solution = stackalloc float[n];
        for (var i = 0; i < n; i++)
        {
            var c = points[index[i]];
            var vn = Vector3.Dot(RelativeVelocity(c), new Vector3(c.Nx, c.Ny, c.Nz));
            rhs[i] = -(vn + c.Restitution * c.RelVel);
            for (var j = 0; j < n; j++) matrix[i * n + j] = CouplingMass(c, points[index[j]]);
        }
        var trace = 0f;
        for (var i = 0; i < n; i++) trace += matrix[i * n + i];
        for (var i = 0; i < n; i++) matrix[i * n + i] += BlockRegularization * trace / n;

        var solved = SolveLinear(matrix, rhs, solution, n);
        for (var i = 0; solved && i < n; i++) solved = points[index[i]].LambdaN + solution[i] >= 0f;
        if (!solved)
        {
            for (var i = 0; i < n; i++) ApplyRestitutionRow(points[index[i]]);
            return;
        }

        for (var i = 0; i < n; i++)
        {
            var c = points[index[i]];
            c.LambdaN += solution[i];
            if (c.LambdaN > c.MaxImpulse) c.MaxImpulse = c.LambdaN;
            var p = solution[i] * new Vector3(c.Nx, c.Ny, c.Nz);
            WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
            WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
        }
    }

    private static void ApplyRestitutionRow(ContactPoint c)
    {
        var v = RelativeVelocity(c);
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        var vn = Vector3.Dot(v, n);
        var impulse = -c.NormalMass * (vn + c.Restitution * c.RelVel);
        var total = MathF.Max(c.LambdaN + impulse, 0f);
        impulse = total - c.LambdaN;
        c.LambdaN = total;
        if (total > c.MaxImpulse) c.MaxImpulse = total;

        var p = impulse * n;
        WorkshopCollisionKernels.ApplyImpulse(c.A, p, new Vector3(c.RAx, c.RAy, c.RAz));
        WorkshopCollisionKernels.ApplyImpulse(c.B, -p, new Vector3(c.RBx, c.RBy, c.RBz));
    }
}
