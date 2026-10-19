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

    public static void PrepareContact(ContactPoint c, Dictionary<ulong, (float LambdaN, float LambdaT1, float LambdaT2, float Approach)> cache)
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

        var vA = WorkshopCollisionKernels.PointVelocity(A, new Vector3(c.RAx, c.RAy, c.RAz));
        var vB = WorkshopCollisionKernels.PointVelocity(B, new Vector3(c.RBx, c.RBy, c.RBz));
        var vn = Vector3.Dot(vA - vB, n);

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
            c.RelVel = MathF.Min(vn, warm.Approach);
        }
        else
        {
            c.LambdaN = 0f;
            c.LambdaT1 = 0f;
            c.LambdaT2 = 0f;
            c.RelVel = vn;
        }

        c.MaxImpulse = 0f;
        c.Bounced = false;
    }

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
    /// collider's substep-start position); any other shape's support is the material contact anchor.</summary>
    private static Vector3 SupportDisplacement(SimBody b, SimCollider collider, Vector3 anchor) =>
        collider.Shape == ShapeKind.Sphere
            ? AnchorDisplacement(b, new Vector3(collider.Px, collider.Py, collider.Pz) - b.Cm0)
            : AnchorDisplacement(b, anchor);

    public static float CurrentSeparation(ContactPoint c)
    {
        var dA = SupportDisplacement(c.A, c.ColA, new Vector3(c.RAx, c.RAy, c.RAz));
        var dB = SupportDisplacement(c.B, c.ColB, new Vector3(c.RBx, c.RBy, c.RBz));
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
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
        var radiusA = c.ColA.Shape == ShapeKind.Sphere ? c.ColA.Radius : 0f;
        var radiusB = c.ColB.Shape == ShapeKind.Sphere ? c.ColB.Radius : 0f;
        m.Rolling = MathF.Max(c.ColA.RollingResistance, c.ColB.RollingResistance) * MathF.Max(radiusA, radiusB);
        m.RollingMass1 = m.Rolling > 0f ? AngularMass(c, c.T1) : 0f;
        m.RollingMass2 = m.Rolling > 0f ? AngularMass(c, c.T2) : 0f;
        m.LambdaR1 = 0f;
        m.LambdaR2 = 0f;
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
        var n1 = m.LambdaR1 - m.RollingMass1 * Vector3.Dot(w, c.T1);
        var n2 = m.LambdaR2 - m.RollingMass2 * Vector3.Dot(w, c.T2);

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

        var t = d1 * c.T1 + d2 * c.T2;
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

    public static void SolveNormalRow(ContactPoint c, bool useBias, in SoftCoefficients soft, float h)
    {
        var v = RelativeVelocity(c);
        var n = new Vector3(c.Nx, c.Ny, c.Nz);
        var vn = Vector3.Dot(v, n);
        var s = CurrentSeparation(c) + ContactSlop;

        float bias = 0f, massScale = 1f, impulseScale = 0f;
        if (s > 0f) bias = s / h;
        else if (useBias)
        {
            bias = MathF.Max(soft.BiasRate * s, -MaxPushoutSpeed);
            massScale = soft.MassScale;
            impulseScale = soft.ImpulseScale;
        }

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
        if (n == 1) { SolveNormalRow(group[0], useBias, soft, h); return; }

        Span<float> rhs = stackalloc float[n];
        Span<float> scale = stackalloc float[n];
        Span<float> impulseScale = stackalloc float[n];
        Span<float> matrix = stackalloc float[n * n];

        for (var i = 0; i < n; i++)
        {
            var c = group[i];
            var v = RelativeVelocity(c);
            var normal = new Vector3(c.Nx, c.Ny, c.Nz);
            var vn = Vector3.Dot(v, normal);
            var s = CurrentSeparation(c) + ContactSlop;

            float bias = 0f;
            scale[i] = 1f;
            impulseScale[i] = 0f;
            if (s > 0f) bias = s / h;
            else if (useBias)
            {
                bias = MathF.Max(soft.BiasRate * s, -MaxPushoutSpeed);
                scale[i] = soft.MassScale;
                impulseScale[i] = soft.ImpulseScale;
            }

            rhs[i] = -(vn + bias);
            for (var j = 0; j < n; j++) matrix[i * n + j] = CouplingMass(c, group[j]);
        }

        var trace = 0f;
        for (var i = 0; i < n; i++) trace += matrix[i * n + i];
        for (var i = 0; i < n; i++) matrix[i * n + i] += BlockRegularization * trace / n;

        Span<float> solution = stackalloc float[n];
        if (!SolveLinear(matrix, rhs, solution, n))
        {
            for (var i = 0; i < n; i++) SolveNormalRow(group[i], useBias, soft, h);
            return;
        }

        Span<float> deltas = stackalloc float[n];
        for (var i = 0; i < n; i++)
        {
            deltas[i] = scale[i] * solution[i] - impulseScale[i] * group[i].LambdaN;
            if (group[i].LambdaN + deltas[i] < 0f)
            {
                for (var j = 0; j < n; j++) SolveNormalRow(group[j], useBias, soft, h);
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

    public static void ApplyRestitution(ContactPoint c)
    {
        if (c.Restitution == 0f || c.RelVel > -c.Threshold || c.MaxImpulse == 0f || CurrentSeparation(c) > 0f) return;
        c.Bounced = true;
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
