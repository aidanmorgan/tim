using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal readonly record struct FrictionColliderPair(int A, int B);

/// <summary>Exact feature membership, independent of contact enumeration and normal rotation.</summary>
internal sealed class FrictionPatchKey : IEquatable<FrictionPatchKey>
{
    private readonly FrictionColliderPair _pair;
    private readonly int[] _features;
    public FrictionPatchKey(List<ContactPoint> points)
    {
        _pair = new(points[0].ColA.Index, points[0].ColB.Index);
        _features = new int[points.Count];
        for (var i = 0; i < points.Count; i++) _features[i] = points[i].Feature;
        Array.Sort(_features);
    }
    public bool Equals(FrictionPatchKey? other) => other is not null && _pair == other._pair &&
        _features.AsSpan().SequenceEqual(other._features);
    public override bool Equals(object? other) => other is FrictionPatchKey key && Equals(key);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_pair);
        foreach (var feature in _features) hash.Add(feature);
        return hash.ToHashCode();
    }
}

internal readonly record struct FrictionWarmStart(Vector3 ForceImpulse, Vector3 TorqueImpulse);

internal sealed class FrictionPatch
{
    public readonly List<ContactPoint> Points = new();
    public FrictionPatchKey Key = null!;
    public Vector3 AnchorA, AnchorB, Normal, T1, T2;
    public float Radius;
    public Vector3 Lambda;
    public readonly Vector3[] Linear = new Vector3[3];
    public readonly Vector3[] AngularA = new Vector3[3];
    public readonly Vector3[] AngularB = new Vector3[3];
    public readonly float[] Mass = new float[9];
    public readonly float[] Eigenvectors = new float[9];
    public readonly float[] Eigenvalues = new float[3];
}

/// <summary>One central force/twist Coulomb ball for each exact oriented-normal patch.</summary>
internal static class WorkshopFrictionSolver
{
    private static int Compare(Vector3 a, Vector3 b)
    {
        var x = a.X.CompareTo(b.X);
        if (x != 0) return x;
        var y = a.Y.CompareTo(b.Y);
        return y != 0 ? y : a.Z.CompareTo(b.Z);
    }

    public static void Prepare(ContactManifold manifold, Dictionary<FrictionPatchKey, FrictionWarmStart> cache)
    {
        manifold.FrictionPatches.Clear();
        var ordered = new List<ContactPoint>(manifold.Points);
        ordered.Sort((a, b) =>
        {
            var normal = Compare(a.Normal, b.Normal);
            if (normal != 0) return normal;
            var point = Compare(a.Point, b.Point);
            return point != 0 ? point : a.Feature.CompareTo(b.Feature);
        });
        foreach (var c in ordered)
        {
            if (manifold.FrictionPatches.Count == 0 || manifold.FrictionPatches[^1].Normal != c.Normal)
                manifold.FrictionPatches.Add(new FrictionPatch { Normal = c.Normal, T1 = c.T1, T2 = c.T2 });
            manifold.FrictionPatches[^1].Points.Add(c);
        }
        foreach (var p in manifold.FrictionPatches)
        {
            var c = p.Points[0];
            var centre = Vector3.Zero;
            foreach (var member in p.Points) centre += member.Point;
            centre /= p.Points.Count;
            var squaredRadius = 0f;
            foreach (var member in p.Points)
            {
                var d = member.Point - centre;
                d -= Vector3.Dot(d, p.Normal) * p.Normal;
                squaredRadius += d.LengthSquared();
            }
            p.Radius = MathF.Sqrt(squaredRadius / p.Points.Count);
            p.AnchorA = centre - c.A.CentreOfMass;
            p.AnchorB = centre - c.B.CentreOfMass;
            p.Key = new(p.Points);
            p.Linear[0] = p.T1;
            p.Linear[1] = p.T2;
            for (var i = 0; i < 3; i++)
            {
                p.AngularA[i] = Vector3.Cross(p.AnchorA, p.Linear[i]);
                p.AngularB[i] = Vector3.Cross(p.AnchorB, p.Linear[i]);
            }
            p.AngularA[2] = p.AngularB[2] = p.Radius * p.Normal;
            for (var i = 0; i < 3; i++)
                for (var j = i; j < 3; j++)
                {
                    var k = 0f;
                    if (c.A.Motion == RigidMotionKind.Dynamic) k += c.A.InvMass * Vector3.Dot(p.Linear[i], p.Linear[j]) +
                        Vector3.Dot(p.AngularA[i], WorkshopCollisionKernels.ApplyInvInertia(c.A, p.AngularA[j]));
                    if (c.B.Motion == RigidMotionKind.Dynamic) k += c.B.InvMass * Vector3.Dot(p.Linear[i], p.Linear[j]) +
                        Vector3.Dot(p.AngularB[i], WorkshopCollisionKernels.ApplyInvInertia(c.B, p.AngularB[j]));
                    p.Mass[i * 3 + j] = p.Mass[j * 3 + i] = k;
                }
            Diagonalize(p);
            if (cache.TryGetValue(p.Key, out var warm))
                p.Lambda = Project(new(Vector3.Dot(warm.ForceImpulse, p.T1), Vector3.Dot(warm.ForceImpulse, p.T2),
                    p.Radius > 0f ? Vector3.Dot(warm.TorqueImpulse, p.Normal) / p.Radius : 0f), Capacity(p));
        }
    }

    private static float Capacity(FrictionPatch p)
    {
        var normal = 0f;
        foreach (var c in p.Points) normal += MathF.Max(c.LambdaN, 0f);
        return p.Points[0].Friction * normal;
    }

    private static Vector3 Project(Vector3 value, float capacity)
    {
        var length = value.Length();
        return length > capacity ? value * (capacity / length) : value;
    }

    public static void WarmStart(ContactManifold manifold)
    {
        foreach (var p in manifold.FrictionPatches) Apply(p, p.Lambda);
    }

    public static void Store(ContactManifold manifold, Dictionary<FrictionPatchKey, FrictionWarmStart> cache)
    {
        foreach (var p in manifold.FrictionPatches)
            cache[p.Key] = new(p.Lambda.X * p.T1 + p.Lambda.Y * p.T2, p.Lambda.Z * p.Radius * p.Normal);
    }

    private static void Apply(FrictionPatch p, Vector3 delta)
    {
        var c = p.Points[0];
        var force = delta.X * p.T1 + delta.Y * p.T2;
        WorkshopCollisionKernels.ApplyImpulse(c.A, force, p.AnchorA);
        WorkshopCollisionKernels.ApplyImpulse(c.B, -force, p.AnchorB);
        Angular(c.A, delta.Z * p.Radius * p.Normal);
        Angular(c.B, -delta.Z * p.Radius * p.Normal);
    }

    private static void Angular(SimBody body, Vector3 torque)
    {
        if (body.Motion != RigidMotionKind.Dynamic) return;
        var dw = WorkshopCollisionKernels.ApplyInvInertia(body, torque);
        body.Wx += dw.X; body.Wy += dw.Y; body.Wz += dw.Z;
    }

    // Cyclic Jacobi diagonalization of the symmetric PSD effective mass. Twelve sweeps
    // bound work; a rotation stops below one f32 relative epsilon of the two diagonals.
    // Nonpositive eigenvalues are the numerical nullspace, never a different friction law.
    private static void Diagonalize(FrictionPatch p)
    {
        Span<float> a = stackalloc float[9];
        p.Mass.CopyTo(a);
        var q = p.Eigenvectors;
        q[0] = q[4] = q[8] = 1f;
        for (var sweep = 0; sweep < 12; sweep++)
            for (var i = 0; i < 2; i++)
                for (var j = i + 1; j < 3; j++)
                {
                    var off = a[i * 3 + j];
                    if (MathF.Abs(off) <= 1.1920929e-7f * MathF.Sqrt(MathF.Abs(a[i * 3 + i] * a[j * 3 + j]))) continue;
                    var gap = a[j * 3 + j] - a[i * 3 + i];
                    var scale = MathF.Max(MathF.Abs(gap), MathF.Abs(2f * off));
                    var g = gap / scale;
                    var o = 2f * off / scale;
                    var tangent = o / (g + MathF.CopySign(MathF.Sqrt(g * g + o * o), g));
                    var cos = 1f / MathF.Sqrt(1f + tangent * tangent);
                    var sin = tangent * cos;
                    for (var k = 0; k < 3; k++)
                    {
                        var ai = a[k * 3 + i]; var aj = a[k * 3 + j];
                        a[k * 3 + i] = cos * ai - sin * aj;
                        a[k * 3 + j] = sin * ai + cos * aj;
                        var qi = q[k * 3 + i]; var qj = q[k * 3 + j];
                        q[k * 3 + i] = cos * qi - sin * qj;
                        q[k * 3 + j] = sin * qi + cos * qj;
                    }
                    for (var k = 0; k < 3; k++)
                    {
                        var ai = a[i * 3 + k]; var aj = a[j * 3 + k];
                        a[i * 3 + k] = cos * ai - sin * aj;
                        a[j * 3 + k] = sin * ai + cos * aj;
                    }
                    a[i * 3 + j] = a[j * 3 + i] = 0f;
                }
        for (var i = 0; i < 3; i++) p.Eigenvalues[i] = MathF.Max(0f, a[i * 3 + i]);
    }

    public static void Solve(ContactManifold manifold)
    {
        Span<float> free = stackalloc float[3];
        foreach (var p in manifold.FrictionPatches)
        {
            var c = p.Points[0];
            for (var i = 0; i < 3; i++)
            {
                free[i] = Vector3.Dot(p.Linear[i], c.A.LinearVelocity - c.B.LinearVelocity) +
                    Vector3.Dot(p.AngularA[i], c.A.AngularVelocity) - Vector3.Dot(p.AngularB[i], c.B.AngularVelocity) -
                    p.Mass[i * 3] * p.Lambda.X - p.Mass[i * 3 + 1] * p.Lambda.Y - p.Mass[i * 3 + 2] * p.Lambda.Z;
            }
            var q = p.Eigenvectors;
            var rhs = new Vector3(
                -(q[0] * free[0] + q[3] * free[1] + q[6] * free[2]),
                -(q[1] * free[0] + q[4] * free[1] + q[7] * free[2]),
                -(q[2] * free[0] + q[5] * free[1] + q[8] * free[2]));
            var capacity = Capacity(p);
            var eigenImpulse = AtShift(p, rhs, 0f);
            if (capacity <= 0f) eigenImpulse = Vector3.Zero;
            else if (eigenImpulse.LengthSquared() > capacity * capacity)
            {
                // ||b||/capacity brackets the PSD trust-region multiplier. Thirty-two
                // bisections exceed f32's 24-bit significand; retain the feasible endpoint.
                var low = 0f;
                var high = rhs.Length() / capacity;
                for (var iteration = 0; iteration < 32; iteration++)
                {
                    var middle = (low + high) * .5f;
                    if (AtShift(p, rhs, middle).LengthSquared() > capacity * capacity) low = middle;
                    else high = middle;
                }
                eigenImpulse = AtShift(p, rhs, high);
            }
            var next = new Vector3(q[0] * eigenImpulse.X + q[1] * eigenImpulse.Y + q[2] * eigenImpulse.Z,
                q[3] * eigenImpulse.X + q[4] * eigenImpulse.Y + q[5] * eigenImpulse.Z,
                q[6] * eigenImpulse.X + q[7] * eigenImpulse.Y + q[8] * eigenImpulse.Z);
            Apply(p, next - p.Lambda);
            p.Lambda = next;
        }
    }

    private static Vector3 AtShift(FrictionPatch p, Vector3 rhs, float shift) => new(
        p.Eigenvalues[0] > 0f ? rhs.X / (p.Eigenvalues[0] + shift) : 0f,
        p.Eigenvalues[1] > 0f ? rhs.Y / (p.Eigenvalues[1] + shift) : 0f,
        p.Eigenvalues[2] > 0f ? rhs.Z / (p.Eigenvalues[2] + shift) : 0f);
}

