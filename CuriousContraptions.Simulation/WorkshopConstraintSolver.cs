using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

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
    public const float ContactSlop = 0.0002f;
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

    public static void PrepareContact(ContactPoint c, Dictionary<ulong, float> cache)
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

        var lo = Math.Min(c.ColA.Index, c.ColB.Index);
        var hi = Math.Max(c.ColA.Index, c.ColB.Index);
        c.Key = ((ulong)(uint)lo << 40) | ((ulong)(uint)hi << 20) | (ulong)(uint)c.Feature;

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

    public static void PrepareRolling(ContactManifold m)
    {
        var c = m.Points[0];
        var (radiusA, sideA) = RollingArm(m, c.ColA, true);
        var (radiusB, sideB) = RollingArm(m, c.ColB, false);
        m.Rolling = MathF.Max(c.ColA.RollingResistance, c.ColB.RollingResistance) * MathF.Max(radiusA, radiusB);
        var line = RollingAxis(c, sideA, sideB);
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
    private static Vector3? RollingAxis(ContactPoint c, ContactPoint? sideA, ContactPoint? sideB)
    {
        if (c.ColA.Shape == ShapeKind.Sphere || c.ColB.Shape == ShapeKind.Sphere) return null;
        Vector3 axis, n;
        if (sideA is { } a && sideB is not null)
        {
            var dot = Vector3.Dot(c.ColA.U1, c.ColB.U1);
            if (MathF.Abs(dot) < WorkshopCollisionKernels.LineAlignment) return null;
            (axis, n) = (Vector3.Normalize(dot >= 0f ? c.ColA.U1 + c.ColB.U1 : c.ColA.U1 - c.ColB.U1), a.Normal);
        }
        else if (sideA is { } onlyA) (axis, n) = (c.ColA.U1, onlyA.Normal);
        else if (sideB is { } onlyB) (axis, n) = (c.ColB.U1, onlyB.Normal);
        else return null;
        // Every axis-support point's normal is within the side tolerance of its axis's perpendicular plane (|n·a| ≤ 0.199: the pair
        // normal the narrowphase classified it by, or a radial normal of that cylinder's own axis), so the projection keeps at least
        // √(1 − 0.199²) = 0.98 of one side's axis, and of two parallel sides' bisector (within 1° of each axis: |n·b| ≤ sin(asin 0.199 +
        // 1°) = 0.216) at least 0.976.
        return Vector3.Normalize(axis - Vector3.Dot(axis, n) * n);
    }

    /// <summary>The rolling arm one side brings to its manifold, with its curved-side support: a sphere's radius at every contact (no
    /// side), a cylinder's radius and its first axis-support point when the manifold rolls on its curved side (as the narrowphase
    /// classified it), otherwise none (flat faces, rims and caps do not roll).</summary>
    private static (float Arm, ContactPoint? Side) RollingArm(ContactManifold m, SimCollider collider, bool sideA)
    {
        if (collider.Shape == ShapeKind.Sphere) return (collider.Radius, null);
        if (collider.Shape != ShapeKind.Cylinder) return (0f, null);
        foreach (var p in m.Points)
            if ((sideA ? p.SupportA : p.SupportB) == ContactSupport.Axis) return (collider.Radius, p);
        return (0f, null);
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
        var A = ci.A;
        var B = ci.B;
        var ni = new Vector3(ci.Nx, ci.Ny, ci.Nz);
        var nj = new Vector3(cj.Nx, cj.Ny, cj.Nz);
        // Contact normals are unit directions. The chord identity evaluates their
        // dot product without self-dot roundoff for identical normals, keeping the
        // block diagonal consistent with EffectiveMass while coupling nonparallel
        // rows by the same rule: n_i dot n_j = 1 - |n_i - n_j|² / 2.
        var cosine = 1f - .5f * Vector3.DistanceSquared(ni, nj);
        var k = ((A.Motion == RigidMotionKind.Dynamic ? A.InvMass : 0f) + (B.Motion == RigidMotionKind.Dynamic ? B.InvMass : 0f)) * cosine;

        if (A.Motion == RigidMotionKind.Dynamic)
        {
            var ri = Vector3.Cross(new Vector3(ci.RAx, ci.RAy, ci.RAz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RAx, cj.RAy, cj.RAz), nj);
            k += WorkshopCollisionKernels.InverseInertiaCoupling(A, ri, rj);
        }
        if (B.Motion == RigidMotionKind.Dynamic)
        {
            var ri = Vector3.Cross(new Vector3(ci.RBx, ci.RBy, ci.RBz), ni);
            var rj = Vector3.Cross(new Vector3(cj.RBx, cj.RBy, cj.RBz), nj);
            k += WorkshopCollisionKernels.InverseInertiaCoupling(B, ri, rj);
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
            SolveNormalBlock(m.Points, pass, soft, h);
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
        WorkshopFrictionSolver.Solve(m);
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
        if (b.Motion != RigidMotionKind.Dynamic || b.RestitutionStamp == stamp) return;
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
            if (c0.A.Motion == RigidMotionKind.Dynamic && c0.B.Motion == RigidMotionKind.Dynamic)
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

    /// <summary>The bouncing points use the same accumulated active-set rule with massScale 1,
    /// impulseScale 0, and target -e*RelVel. Existing support is included in b; the substep
    /// energy limiter remains authoritative after this correction.</summary>
    public static void ApplyRestitution(List<ContactPoint> points)
    {
        var count = points.Count;
        Span<int> index = stackalloc int[count];
        var n = 0;
        for (var i = 0; i < count; i++)
            if (Bounces(points[i])) index[n++] = i;
        if (n == 0) return;
        Span<float> matrix = stackalloc float[n * n];
        Span<float> rhs = stackalloc float[n];
        Span<float> solution = stackalloc float[n];
        for (var i = 0; i < n; i++)
        {
            var c = points[index[i]];
            var vn = Vector3.Dot(RelativeVelocity(c), new Vector3(c.Nx, c.Ny, c.Nz));
            rhs[i] = -(vn + c.Restitution * c.RelVel);
            for (var j = 0; j < n; j++)
            {
                matrix[i * n + j] = CouplingMass(c, points[index[j]]);
                rhs[i] += matrix[i * n + j] * points[index[j]].LambdaN;
            }
        }
        if (!SolveActiveSet(matrix, rhs, solution, n, SubstepSeconds)) return;
        for (var i = 0; i < n; i++)
            if (!float.IsFinite(solution[i] - points[index[i]].LambdaN)) return;
        for (var i = 0; i < n; i++) ApplyNormalImpulse(points[index[i]], solution[i]);
    }
}
