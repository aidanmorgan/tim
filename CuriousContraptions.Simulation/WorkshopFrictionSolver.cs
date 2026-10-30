using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Simulation;

internal readonly record struct ContactFeatureIdentity(int A, int B, int Feature);

/// <summary>Exact collider-pair feature membership, independent of enumeration and normal rotation, for contact warm starts.</summary>
internal sealed class ContactFeatureSetKey : IEquatable<ContactFeatureSetKey>
{
    private readonly ContactFeatureIdentity[] _features;
    public ContactFeatureSetKey(List<ContactPoint> points)
    {
        _features = new ContactFeatureIdentity[points.Count];
        for (var i = 0; i < points.Count; i++)
            _features[i] = new(points[i].ColA.Index, points[i].ColB.Index, points[i].Feature);
        Array.Sort(_features, (a, b) =>
        {
            var ownerA = a.A.CompareTo(b.A); if (ownerA != 0) return ownerA;
            var ownerB = a.B.CompareTo(b.B); if (ownerB != 0) return ownerB;
            return a.Feature.CompareTo(b.Feature);
        });
    }
    public bool Equals(ContactFeatureSetKey? other) => other is not null &&
        _features.AsSpan().SequenceEqual(other._features);
    public override bool Equals(object? other) => other is ContactFeatureSetKey key && Equals(key);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var feature in _features) hash.Add(feature);
        return hash.ToHashCode();
    }
}

internal readonly record struct FrictionWarmStart(Vector3 ForceImpulse, Vector3 TorqueImpulse);

internal sealed class ContactPatch
{
    public readonly List<ContactPoint> Points = new(64);
    public ContactFeatureSetKey Key = null!;
    public Vector3 AnchorA, AnchorB, Normal, T1, T2;
    public float Radius;
    public bool Supported;
    public Vector3 Lambda;
    public float[] RollingCoefficients = new float[64];
    public Vector3 RelaxedLambda;
    public float RelaxedLambdaR1, RelaxedLambdaR2;
    public Vector3[] ProjectedSupport = new Vector3[64];
    public bool[] SupportMembers = new bool[64];
    public readonly List<ContactPoint> Supporting = new(64);

    public void Reserve(int count)
    {
        if (count > Array.MaxLength) throw new WorkshopStorageException("Contact patch scratch is not representable.");
        if (count <= 3L * RollingCoefficients.Length / 4) return;
        var capacity = (int)Math.Min(Array.MaxLength, Math.Max(2L * RollingCoefficients.Length, (4L * count + 2) / 3));
        if (capacity == RollingCoefficients.Length) return;
        var coefficients = new float[capacity]; var projected = new Vector3[capacity]; var members = new bool[capacity];
        RollingCoefficients = coefficients; ProjectedSupport = projected; SupportMembers = members;
    }

    public void Reset()
    {
        Points.Clear(); Supporting.Clear(); Key = null!;
        AnchorA = AnchorB = Normal = T1 = T2 = Lambda = Roll1 = Roll2 = Vector3.Zero;
        Radius = RollingMass1 = RollingMass2 = LambdaR1 = LambdaR2 = 0f; Supported = false;
        RelaxedLambda = Vector3.Zero; RelaxedLambdaR1 = RelaxedLambdaR2 = 0f;
        Array.Clear(Linear); Array.Clear(AngularA); Array.Clear(AngularB);
        Array.Clear(Mass); Array.Clear(Eigenvectors); Array.Clear(Eigenvalues);
    }
    public Vector3 Roll1, Roll2;
    public float RollingMass1, RollingMass2;
    public float LambdaR1, LambdaR2;
    public readonly Vector3[] Linear = new Vector3[3];
    public readonly Vector3[] AngularA = new Vector3[3];
    public readonly Vector3[] AngularB = new Vector3[3];
    public readonly float[] Mass = new float[9];
    public readonly float[] Eigenvectors = new float[9];
    public readonly float[] Eigenvalues = new float[3];
}

/// <summary>One central force/twist Coulomb ball for each physically compatible normal-frame patch.</summary>
internal static class WorkshopFrictionSolver
{
    private static int Compare(Vector3 a, Vector3 b)
    {
        var x = a.X.CompareTo(b.X);
        if (x != 0) return x;
        var y = a.Y.CompareTo(b.Y);
        return y != 0 ? y : a.Z.CompareTo(b.Z);
    }

    public static void Prepare(ContactManifold manifold, Dictionary<ContactFeatureSetKey, FrictionWarmStart> cache)
    {
        manifold.Patches.Clear();
        foreach (var patch in manifold.PatchPool) patch.Reset();
        var ordered = manifold.OrderedScratch;
        ordered.Clear(); ordered.AddRange(manifold.Points);
        ordered.Sort((a, b) =>
        {
            var normal = Compare(a.Normal, b.Normal);
            if (normal != 0) return normal;
            var point = Compare(a.Point, b.Point);
            return point != 0 ? point : a.Feature.CompareTo(b.Feature);
        });
        foreach (var c in ordered)
        {
            ContactPatch? match = null;
            foreach (var patch in manifold.Patches)
            {
                var compatible = true;
                foreach (var member in patch.Points)
                    compatible &= PhysicalTolerance.CompatibleContactNormals(c.Normal, member.Normal,
                        Vector3.Distance(c.Point, member.Point), WorkshopConstraintSolver.ContactSlop);
                if (!compatible) continue;
                match = patch;
                break;
            }
            if (match is null)
            {
                if (manifold.PatchPool.Count == manifold.Patches.Count) manifold.PatchPool.Add(new ContactPatch());
                match = manifold.PatchPool[manifold.Patches.Count];
                manifold.Patches.Add(match);
            }
            match.Points.Add(c);
        }
        foreach (var p in manifold.Patches)
        {
            p.Reserve(p.Points.Count);
            var mean = Vector3.Zero;
            foreach (var point in p.Points) mean += point.Normal / p.Points.Count;
            p.Normal = Vector3.Normalize(mean);
            var n = Vector3.Abs(p.Normal);
            var axis = n.X <= n.Y && n.X <= n.Z ? Vector3.UnitX : n.Y <= n.Z ? Vector3.UnitY : Vector3.UnitZ;
            p.T1 = Vector3.Normalize(Vector3.Cross(p.Normal, axis));
            p.T2 = Vector3.Cross(p.Normal, p.T1);
            p.Key = new(p.Points);
            RefreshSupport(p);
            if (cache.TryGetValue(p.Key, out var warm))
                p.Lambda = Project(new(Vector3.Dot(warm.ForceImpulse, p.T1), Vector3.Dot(warm.ForceImpulse, p.T2),
                    p.Radius > 0f ? Vector3.Dot(warm.TorqueImpulse, p.Normal) / p.Radius : 0f), Capacity(p));
        }
    }

    private static void RefreshSupport(ContactPatch p)
    {
        var total = 0f;
        var count = 0;
        var members = p.SupportMembers.AsSpan(0, p.Points.Count);
        for (var i = 0; i < p.Points.Count; i++)
        {
            var point = p.Points[i];
            total += MathF.Max(point.LambdaN, 0f);
            members[i] = point.LambdaN > 0f || WorkshopConstraintSolver.CurrentSeparation(point) <= 0f;
            if (members[i]) count++;
        }
        if (!(total > 0f))
        {
            if (p.Lambda != Vector3.Zero) Apply(p, -p.Lambda);
            p.Lambda = Vector3.Zero;
            p.Supported = false;
            p.Radius = 0f;
            return;
        }
        var anchorA = Vector3.Zero; var anchorB = Vector3.Zero;
        for (var i = 0; i < p.Points.Count; i++)
        {
            if (!members[i]) continue;
            var point = p.Points[i];
            anchorA += new Vector3(point.RAx, point.RAy, point.RAz);
            anchorB += new Vector3(point.RBx, point.RBy, point.RBz);
        }
        anchorA /= count; anchorB /= count;
        var c = p.Points[0];
        var useA = c.A.Motion == RigidMotionKind.Dynamic;
        var squared = 0f;
        for (var i = 0; i < p.Points.Count; i++)
        {
            if (!members[i]) continue;
            var point = p.Points[i];
            var offset = useA ? new Vector3(point.RAx, point.RAy, point.RAz) - anchorA :
                new Vector3(point.RBx, point.RBy, point.RBz) - anchorB;
            offset -= Vector3.Dot(offset, p.Normal) * p.Normal;
            squared += offset.LengthSquared();
        }
        var radius = MathF.Sqrt(squared / count);
        if (p.Supported && p.AnchorA == anchorA && p.AnchorB == anchorB && p.Radius == radius) return;
        // The old wrench belongs to its old centre and radius. Remove it before
        // rebuilding the basis; no difference of coordinates spans two geometries.
        if (p.Lambda != Vector3.Zero) Apply(p, -p.Lambda);
        p.Lambda = Vector3.Zero;
        p.AnchorA = anchorA; p.AnchorB = anchorB; p.Radius = radius; p.Supported = true;
        p.Linear[0] = p.T1;
        p.Linear[1] = p.T2;
        p.Linear[2] = Vector3.Zero;
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
                    WorkshopCollisionKernels.InverseInertiaCoupling(c.A, p.AngularA[i], p.AngularA[j]);
                if (c.B.Motion == RigidMotionKind.Dynamic) k += c.B.InvMass * Vector3.Dot(p.Linear[i], p.Linear[j]) +
                    WorkshopCollisionKernels.InverseInertiaCoupling(c.B, p.AngularB[i], p.AngularB[j]);
                p.Mass[i * 3 + j] = p.Mass[j * 3 + i] = k;
            }
        Diagonalize(p);
    }

    private static float Capacity(ContactPatch p)
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
        foreach (var p in manifold.Patches) Apply(p, p.Lambda);
    }

    public static void Store(ContactManifold manifold, Dictionary<ContactFeatureSetKey, FrictionWarmStart> cache)
    {
        foreach (var p in manifold.Patches)
            cache[p.Key] = new(p.Lambda.X * p.T1 + p.Lambda.Y * p.T2, p.Lambda.Z * p.Radius * p.Normal);
    }

    private static void Apply(ContactPatch p, Vector3 delta)
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
    private static void Diagonalize(ContactPatch p)
    {
        Span<float> a = stackalloc float[9];
        p.Mass.CopyTo(a);
        var q = p.Eigenvectors;
        Array.Clear(q);
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
        foreach (var p in manifold.Patches)
        {
            RefreshSupport(p);
            if (!p.Supported) continue;
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

    private static Vector3 AtShift(ContactPatch p, Vector3 rhs, float shift) => new(
        p.Eigenvalues[0] > 0f ? rhs.X / (p.Eigenvalues[0] + shift) : 0f,
        p.Eigenvalues[1] > 0f ? rhs.Y / (p.Eigenvalues[1] + shift) : 0f,
        p.Eigenvalues[2] > 0f ? rhs.Z / (p.Eigenvalues[2] + shift) : 0f);
}

