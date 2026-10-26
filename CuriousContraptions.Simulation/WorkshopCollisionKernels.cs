using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal enum ShapeKind : uint
{
    Sphere = 0,
    Box = 1,
    Plane = 2,
    Cylinder = 3,
    Tube = 4,
    Torus = 5,
    Frustum = 6
}

/// <summary>How one body's side of a contact moves within a substep. A material support is the contact anchor itself; an axis
/// support is a rolling curved-side cylinder contact, which stays beside the anchor's closest point on the cylinder axis however
/// the cylinder turns about it (the sphere precedent), so it moves with that axis point.</summary>
internal enum ContactSupport : byte
{
    Material = 0,
    Axis = 1
}

internal sealed class SimBody
{
    public int BodySlot;
    public int BodyOffset;
    public uint BodyIdLow;
    public uint BodyIdHigh;
    public ulong Id;
    public int CellX;
    public int CellY;
    public int CellZ;
    public float LocalX;
    public float LocalY;
    public float LocalZ;
    public float Px, Py, Pz;
    public float Qx, Qy, Qz, Qw;
    public float Vx, Vy, Vz;
    public float Wx, Wy, Wz;
    public float Mass;
    public float InvMass;
    public float Gx, Gy, Gz;
    public float Drag;
    public Vector3 ComLocal;
    public Quaternion Principal;
    public Vector3 InvI;
    public float[] InvIWorld = new float[9];
    public float Cmx, Cmy, Cmz;
    public float SupportExtent;
    public int DynamicIndex;
    public RigidMotionKind Motion;
    /// <summary>The body's velocities at the start of the substep, before guides, supplied drives, gravity and drag: the
    /// approach a restitution target reflects (Box2D v3 prepares contacts before integrating velocities).</summary>
    public Vector3 V0, W0;
    /// <summary>The substep's restitution energy bound: the kinetic energy before the contact solve (after gravity and drag), the
    /// velocities after the relax sweeps, and the group root and accumulators of the bodies joined by bouncing manifolds.</summary>
    public float PreSolveKinetic;
    public Vector3 RelaxedV, RelaxedW;
    public SimBody RestitutionRoot = null!;
    public int RestitutionStamp;
    public float EnergyA, EnergyB, EnergyC, EnergyBudget, EnergyScale;

    public Vector3 Cm0;
    public Quaternion Q0;

    public SimCollider? Collider;

    public Vector3 Position => new(Px, Py, Pz);
    public Quaternion Rotation => new(Qx, Qy, Qz, Qw);
    public Vector3 LinearVelocity => new(Vx, Vy, Vz);
    public Vector3 AngularVelocity => new(Wx, Wy, Wz);
    public Vector3 CentreOfMass => new(Cmx, Cmy, Cmz);
}

internal sealed class SimCollider
{
    public int Index;
    public ulong Id;
    public int BodySlot;
    public SimBody Body = null!;
    public int MaterialSlot;
    public ShapeKind Shape;
    public float Tx, Ty, Tz;
    public Quaternion Rq;
    public float Px, Py, Pz;
    public float Qx, Qy, Qz, Qw;
    public Vector3 U0 = Vector3.UnitX;
    public Vector3 U1 = Vector3.UnitY;
    public Vector3 U2 = Vector3.UnitZ;
    public float Radius;
    public float Hx, Hy, Hz;
    public float Restitution;
    public float BounceThreshold;
    public float Friction;
    public float RollingResistance;

    public Vector3 Position => new(Px, Py, Pz);
    public Quaternion Rotation => new(Qx, Qy, Qz, Qw);
}

internal sealed class ContactPoint
{
    public SimBody A = null!;
    public SimBody B = null!;
    public SimCollider ColA = null!;
    public SimCollider ColB = null!;
    public float Nx, Ny, Nz;
    public float Px, Py, Pz;
    public float Gap;
    public int Feature;
    /// <summary>Set once by the narrowphase that classified the contact; read by the support displacement and the rolling row.</summary>
    public ContactSupport SupportA;
    public ContactSupport SupportB;

    public float RAx, RAy, RAz;
    public float RBx, RBy, RBz;
    public Vector3 T1;
    public Vector3 T2;
    public float NormalMass;
    public float Restitution;
    public float Threshold;
    public float Friction;
    public ulong Key;
    public float LambdaN;
    /// <summary>The substep-start relative normal velocity at this point (negative approaching): the restitution target's and the
    /// contact events' approach.</summary>
    public float RelVel;
    public float MaxImpulse;

    public Vector3 Normal => new(Nx, Ny, Nz);
    public Vector3 Point => new(Px, Py, Pz);
}

internal sealed class ContactManifold
{
    public List<ContactPoint> Points = new();
    public readonly List<FrictionPatch> FrictionPatches = new();
    public float Rolling;
    /// <summary>The rolling-resistance row's directions, set by <see cref="WorkshopConstraintSolver.PrepareRolling"/> from the
    /// manifold's curved supports (each point's <see cref="ContactPoint.SupportA"/>/<see cref="ContactPoint.SupportB"/> and its
    /// colliders' shapes): both contact tangents for a sphere or crossed curved sides, or one rolling axis (Roll2 zero, with zero
    /// mass) for one curved side or two parallel ones.</summary>
    public Vector3 Roll1;
    public Vector3 Roll2;
    public float RollingMass1;
    public float RollingMass2;
    public float LambdaR1;
    public float LambdaR2;
}

internal static partial class WorkshopCollisionKernels
{
    private const float EdgeFaceAlignment = 0.98f;

    public static Vector3 RotateVector(Quaternion q, Vector3 v) => Vector3.Transform(v, q);
    public static Vector3 InvRotateVector(Quaternion q, Vector3 v) => Vector3.Transform(v, Quaternion.Conjugate(q));

    public static void UpdateDynamicFrame(SimBody b)
    {
        var q = Quaternion.Multiply(new Quaternion(b.Qx, b.Qy, b.Qz, b.Qw), b.Principal);
        var e0 = Vector3.Transform(Vector3.UnitX, q);
        var e1 = Vector3.Transform(Vector3.UnitY, q);
        var e2 = Vector3.Transform(Vector3.UnitZ, q);

        var m = b.InvIWorld;
        for (var i = 0; i < 3; i++)
        {
            var e0_i = i == 0 ? e0.X : (i == 1 ? e0.Y : e0.Z);
            var e1_i = i == 0 ? e1.X : (i == 1 ? e1.Y : e1.Z);
            var e2_i = i == 0 ? e2.X : (i == 1 ? e2.Y : e2.Z);

            for (var j = 0; j < 3; j++)
            {
                var e0_j = j == 0 ? e0.X : (j == 1 ? e0.Y : e0.Z);
                var e1_j = j == 0 ? e1.X : (j == 1 ? e1.Y : e1.Z);
                var e2_j = j == 0 ? e2.X : (j == 1 ? e2.Y : e2.Z);

                m[i * 3 + j] = b.InvI.X * e0_i * e0_j + b.InvI.Y * e1_i * e1_j + b.InvI.Z * e2_i * e2_j;
            }
        }

        var c = Vector3.Transform(b.ComLocal, new Quaternion(b.Qx, b.Qy, b.Qz, b.Qw));
        b.Cmx = b.Px + c.X;
        b.Cmy = b.Py + c.Y;
        b.Cmz = b.Pz + c.Z;
    }

    public static Vector3 ApplyInvInertia(SimBody b, Vector3 t)
    {
        var m = b.InvIWorld;
        return new Vector3(
            m[0] * t.X + m[1] * t.Y + m[2] * t.Z,
            m[3] * t.X + m[4] * t.Y + m[5] * t.Z,
            m[6] * t.X + m[7] * t.Y + m[8] * t.Z);
    }

    public static void ApplyImpulse(SimBody b, Vector3 p, Vector3 r)
    {
        if (b.Motion != RigidMotionKind.Dynamic) return;
        b.Vx += p.X * b.InvMass;
        b.Vy += p.Y * b.InvMass;
        b.Vz += p.Z * b.InvMass;
        var t = Vector3.Cross(r, p);
        var dw = ApplyInvInertia(b, t);
        b.Wx += dw.X;
        b.Wy += dw.Y;
        b.Wz += dw.Z;
    }

    public static Vector3 PointVelocity(SimBody b, Vector3 r)
    {
        if (b.Motion != RigidMotionKind.Dynamic) return Vector3.Zero;
        return new Vector3(
            b.Vx + (b.Wy * r.Z - b.Wz * r.Y),
            b.Vy + (b.Wz * r.X - b.Wx * r.Z),
            b.Vz + (b.Wx * r.Y - b.Wy * r.X));
    }

    public static void RefreshCollider(SimCollider c)
    {
        var body = c.Body;
        var bq = new Quaternion(body.Qx, body.Qy, body.Qz, body.Qw);
        if (c.Tx == 0f && c.Ty == 0f && c.Tz == 0f)
        {
            c.Px = body.Px; c.Py = body.Py; c.Pz = body.Pz;
        }
        else
        {
            var rot = Vector3.Transform(new Vector3(c.Tx, c.Ty, c.Tz), bq);
            c.Px = body.Px + rot.X;
            c.Py = body.Py + rot.Y;
            c.Pz = body.Pz + rot.Z;
        }

        if (c.Rq.X == 0f && c.Rq.Y == 0f && c.Rq.Z == 0f)
        {
            c.Qx = body.Qx; c.Qy = body.Qy; c.Qz = body.Qz; c.Qw = body.Qw;
        }
        else
        {
            var q = Quaternion.Multiply(bq, c.Rq);
            c.Qx = q.X; c.Qy = q.Y; c.Qz = q.Z; c.Qw = q.W;
        }

        var cq = new Quaternion(c.Qx, c.Qy, c.Qz, c.Qw);
        c.U0 = Vector3.Transform(Vector3.UnitX, cq);
        c.U1 = Vector3.Transform(Vector3.UnitY, cq);
        c.U2 = Vector3.Transform(Vector3.UnitZ, cq);
    }

    /// <summary>The farthest surface point from the collider centre (the spin arm of fattening and speculative margins). A plane
    /// is static and has none; a hollow collider's is its containing ball's radius.</summary>
    public static float ColliderExtent(SimCollider c) => c.Shape switch
    {
        ShapeKind.Sphere => c.Radius,
        ShapeKind.Box => MathF.Sqrt(c.Hx * c.Hx + c.Hy * c.Hy + c.Hz * c.Hz),
        ShapeKind.Cylinder => MathF.Sqrt(c.Radius * c.Radius + c.Hy * c.Hy),
        ShapeKind.Tube or ShapeKind.Torus or ShapeKind.Frustum => HollowReach(c),
        ShapeKind.Plane => 0f,
        _ => 0f
    };

    /// <summary>The half-extent of a cylinder along unit <paramref name="axis"/> about its centre, exactly: h |u·a| + r √(1 − (u·a)²).</summary>
    public static float CylinderProjection(SimCollider c, Vector3 axis)
    {
        var t = MathF.Abs(Vector3.Dot(axis, c.U1));
        return c.Hy * t + c.Radius * MathF.Sqrt(MathF.Max(0f, 1f - t * t));
    }

    public static Vector3 BoxVertex(SimCollider box, float sx, float sy, float sz) =>
        new Vector3(box.Px, box.Py, box.Pz) +
        sx * box.Hx * box.U0 +
        sy * box.Hy * box.U1 +
        sz * box.Hz * box.U2;

    public static ContactPoint MakeContact(SimBody a, SimBody b, SimCollider colA, SimCollider colB,
        Vector3 n, Vector3 p, float gap, int feature)
    {
        return new ContactPoint
        {
            A = a,
            B = b,
            ColA = colA,
            ColB = colB,
            Nx = n.X,
            Ny = n.Y,
            Nz = n.Z,
            Px = p.X,
            Py = p.Y,
            Pz = p.Z,
            Gap = gap,
            Feature = feature
        };
    }

    public static void CollideSpherePlane(SimCollider sphere, SimCollider plane, float margin, List<ContactPoint> outList)
    {
        var n = plane.U1;
        var sp = new Vector3(sphere.Px, sphere.Py, sphere.Pz);
        var pp = new Vector3(plane.Px, plane.Py, plane.Pz);
        var gap = Vector3.Dot(sp - pp, n) - sphere.Radius;
        if (gap > margin) return;
        outList.Add(MakeContact(sphere.Body, plane.Body, sphere, plane, n, sp - sphere.Radius * n, gap, 0));
    }

    public static void CollideSphereSphere(SimCollider colA, SimCollider colB, float margin, List<ContactPoint> outList)
    {
        var pa = new Vector3(colA.Px, colA.Py, colA.Pz);
        var pb = new Vector3(colB.Px, colB.Py, colB.Pz);
        var d = pa - pb;
        var distSq = d.LengthSquared();
        if (distSq <= 1e-12f) return;
        var dist = MathF.Sqrt(distSq);
        var gap = dist - (colA.Radius + colB.Radius);
        if (gap > margin) return;
        var n = d / dist;
        outList.Add(MakeContact(colA.Body, colB.Body, colA, colB, n, pb + n * (colB.Radius + gap * 0.5f), gap, 0));
    }

    public static void CollideBoxSphere(SimCollider box, SimCollider sphere, float margin, List<ContactPoint> outList)
    {
        var bp = new Vector3(box.Px, box.Py, box.Pz);
        var sp = new Vector3(sphere.Px, sphere.Py, sphere.Pz);
        var d = sp - bp;

        var lx = Vector3.Dot(d, box.U0);
        var limitX = box.Hx + sphere.Radius + margin;
        if (MathF.Abs(lx) > limitX) return;

        var ly = Vector3.Dot(d, box.U1);
        var limitY = box.Hy + sphere.Radius + margin;
        if (MathF.Abs(ly) > limitY) return;

        var lz = Vector3.Dot(d, box.U2);
        var limitZ = box.Hz + sphere.Radius + margin;
        if (MathF.Abs(lz) > limitZ) return;

        var cx = Math.Clamp(lx, -box.Hx, box.Hx);
        var cy = Math.Clamp(ly, -box.Hy, box.Hy);
        var cz = Math.Clamp(lz, -box.Hz, box.Hz);

        var diff = new Vector3(lx - cx, ly - cy, lz - cz);
        var distSq = diff.LengthSquared();

        float nlx, nly, nlz, gap, contactX = cx, contactY = cy, contactZ = cz;
        if (distSq > 1e-12f)
        {
            var dist = MathF.Sqrt(distSq);
            gap = dist - sphere.Radius;
            if (gap > margin) return;
            nlx = diff.X / dist;
            nly = diff.Y / dist;
            nlz = diff.Z / dist;
        }
        else
        {
            var dFaceX = box.Hx - MathF.Abs(lx);
            var dFaceY = box.Hy - MathF.Abs(ly);
            var dFaceZ = box.Hz - MathF.Abs(lz);
            if (dFaceX <= dFaceY && dFaceX <= dFaceZ)
            {
                nlx = lx >= 0f ? 1f : -1f; nly = 0f; nlz = 0f;
                contactX = nlx * box.Hx;
                gap = -(dFaceX + sphere.Radius);
            }
            else if (dFaceY <= dFaceZ)
            {
                nlx = 0f; nly = ly >= 0f ? 1f : -1f; nlz = 0f;
                contactY = nly * box.Hy;
                gap = -(dFaceY + sphere.Radius);
            }
            else
            {
                nlx = 0f; nly = 0f; nlz = lz >= 0f ? 1f : -1f;
                contactZ = nlz * box.Hz;
                gap = -(dFaceZ + sphere.Radius);
            }
        }

        var n = nlx * box.U0 + nly * box.U1 + nlz * box.U2;
        var p = bp + contactX * box.U0 + contactY * box.U1 + contactZ * box.U2;

        var fx = contactX == box.Hx ? 1 : (contactX == -box.Hx ? 2 : 0);
        var fy = contactY == box.Hy ? 1 : (contactY == -box.Hy ? 2 : 0);
        var fz = contactZ == box.Hz ? 1 : (contactZ == -box.Hz ? 2 : 0);
        var featureId = fx | (fy << 2) | (fz << 4);

        outList.Add(MakeContact(sphere.Body, box.Body, sphere, box, n, p, gap, featureId));
    }

    public static void ReduceManifold(List<ContactPoint> points, Vector3 n)
    {
        if (points.Count <= 4) return;

        var p1 = points[0];
        foreach (var p in points)
            if (p.Gap < p1.Gap) p1 = p;

        ContactPoint? p2 = null;
        var best = -1f;
        var p1Pos = new Vector3(p1.Px, p1.Py, p1.Pz);
        foreach (var p in points)
        {
            if (ReferenceEquals(p, p1)) continue;
            var d = (new Vector3(p.Px, p.Py, p.Pz) - p1Pos).LengthSquared();
            if (d > best) { best = d; p2 = p; }
        }

        if (p2 == null) { points.Clear(); points.Add(p1); return; }
        var p2Pos = new Vector3(p2.Px, p2.Py, p2.Pz);
        var e = p2Pos - p1Pos;

        ContactPoint? p3 = null;
        best = -1f;
        var sign3 = 0f;
        foreach (var p in points)
        {
            if (ReferenceEquals(p, p1) || ReferenceEquals(p, p2)) continue;
            var pPos = new Vector3(p.Px, p.Py, p.Pz);
            var c = Vector3.Cross(pPos - p1Pos, e);
            var signed = Vector3.Dot(c, n);
            if (MathF.Abs(signed) > best)
            {
                best = MathF.Abs(signed);
                p3 = p;
                sign3 = MathF.Sign(signed);
            }
        }

        if (p3 == null) { points.Clear(); points.Add(p1); points.Add(p2); return; }

        ContactPoint? p4 = null;
        best = -1f;
        foreach (var p in points)
        {
            if (ReferenceEquals(p, p1) || ReferenceEquals(p, p2) || ReferenceEquals(p, p3)) continue;
            var pPos = new Vector3(p.Px, p.Py, p.Pz);
            var c = Vector3.Cross(pPos - p1Pos, e);
            var signed = Vector3.Dot(c, n);
            if (MathF.Sign(signed) == sign3 && sign3 != 0f) continue;
            if (MathF.Abs(signed) > best)
            {
                best = MathF.Abs(signed);
                p4 = p;
            }
        }

        if (p4 == null)
        {
            var p3Pos = new Vector3(p3.Px, p3.Py, p3.Pz);
            foreach (var p in points)
            {
                if (ReferenceEquals(p, p1) || ReferenceEquals(p, p2) || ReferenceEquals(p, p3)) continue;
                var d = (new Vector3(p.Px, p.Py, p.Pz) - p3Pos).LengthSquared();
                if (d > best) { best = d; p4 = p; }
            }
        }

        points.Clear();
        points.Add(p1);
        points.Add(p2);
        points.Add(p3);
        if (p4 != null) points.Add(p4);
    }

    public static void CollideBoxPlane(SimCollider box, SimCollider plane, float margin, List<ContactPoint> outList)
    {
        var n = plane.U1;
        var pp = new Vector3(plane.Px, plane.Py, plane.Pz);
        var candidates = new List<ContactPoint>(8);
        var index = 0;
        Span<float> signs = [-1f, 1f];
        for (var ix = 0; ix < 2; ix++)
        {
            var sx = signs[ix];
            for (var iy = 0; iy < 2; iy++)
            {
                var sy = signs[iy];
                for (var iz = 0; iz < 2; iz++)
                {
                    var sz = signs[iz];
                    var v = BoxVertex(box, sx, sy, sz);
                    var gap = Vector3.Dot(v - pp, n);
                    if (gap <= margin)
                    {
                        candidates.Add(MakeContact(box.Body, plane.Body, box, plane, n, v - gap * 0.5f * n, gap, index));
                    }
                    index++;
                }
            }
        }

        if (candidates.Count == 0) return;
        ReduceManifold(candidates, n);
        outList.AddRange(candidates);
    }

    private struct Edge
    {
        public Vector3 P;
        public Vector3 D;
    }

    private static Edge SupportEdge(SimCollider box, Vector3 d)
    {
        var d0 = MathF.Abs(Vector3.Dot(d, box.U0));
        var d1 = MathF.Abs(Vector3.Dot(d, box.U1));
        var d2 = MathF.Abs(Vector3.Dot(d, box.U2));

        var along = 0;
        if (d1 < d0 && d1 < d2) along = 1;
        else if (d2 < d0) along = 2;

        var bp = new Vector3(box.Px, box.Py, box.Pz);
        if (along != 0) bp += (Vector3.Dot(d, box.U0) >= 0f ? box.Hx : -box.Hx) * box.U0;
        if (along != 1) bp += (Vector3.Dot(d, box.U1) >= 0f ? box.Hy : -box.Hy) * box.U1;
        if (along != 2) bp += (Vector3.Dot(d, box.U2) >= 0f ? box.Hz : -box.Hz) * box.U2;

        var dir = along == 0 ? box.U0 : (along == 1 ? box.U1 : box.U2);
        var h = along == 0 ? box.Hx : (along == 1 ? box.Hy : box.Hz);
        return new Edge { P = bp - h * dir, D = 2f * h * dir };
    }

    private static (float S, float T) ClosestSegmentParameters(in Edge ea, in Edge eb)
    {
        var r = ea.P - eb.P;
        var aa = Vector3.Dot(ea.D, ea.D);
        var bb = Vector3.Dot(eb.D, eb.D);
        var ab = Vector3.Dot(ea.D, eb.D);
        var ar = Vector3.Dot(ea.D, r);
        var br = Vector3.Dot(eb.D, r);
        var denom = aa * bb - ab * ab;

        static float Clamp(float v) => Math.Clamp(v, 0f, 1f);
        var s = denom > 1e-12f * aa * bb ? Clamp((ab * br - ar * bb) / denom) : 0f;
        var t = (ab * s + br) / bb;
        if (t < 0f) { t = 0f; s = Clamp(-ar / aa); }
        else if (t > 1f) { t = 1f; s = Clamp((ab - ar) / aa); }
        return (s, t);
    }

    private static void SupportContact(SimCollider boxA, SimCollider boxB, Vector3 n, float separation, int feature, List<ContactPoint> outList)
    {
        var ea = SupportEdge(boxA, -n);
        var eb = SupportEdge(boxB, n);
        var (s, t) = ClosestSegmentParameters(in ea, in eb);
        var a = ea.P + s * ea.D;
        var b = eb.P + t * eb.D;
        outList.Add(MakeContact(boxA.Body, boxB.Body, boxA, boxB, n, (a + b) * 0.5f, separation, feature));
    }

    private struct PolyVertex
    {
        public int Id;
        public Vector3 P;
    }

    public static void CollideBoxBox(SimCollider boxA, SimCollider boxB, float margin, List<ContactPoint> outList)
    {
        var dp = new Vector3(boxA.Px - boxB.Px, boxA.Py - boxB.Py, boxA.Pz - boxB.Pz);

        Vector3 bestAxis = Vector3.Zero;
        var bestSeparation = float.NegativeInfinity;
        var bestType = -1;
        var bestIndex = -1;

        bool Consider(Vector3 L, int type, int index)
        {
            var rA = boxA.Hx * MathF.Abs(Vector3.Dot(boxA.U0, L)) +
                     boxA.Hy * MathF.Abs(Vector3.Dot(boxA.U1, L)) +
                     boxA.Hz * MathF.Abs(Vector3.Dot(boxA.U2, L));
            var rB = boxB.Hx * MathF.Abs(Vector3.Dot(boxB.U0, L)) +
                     boxB.Hy * MathF.Abs(Vector3.Dot(boxB.U1, L)) +
                     boxB.Hz * MathF.Abs(Vector3.Dot(boxB.U2, L));
            var dist = Vector3.Dot(dp, L);
            var separation = MathF.Abs(dist) - (rA + rB);
            if (separation > margin) return false;
            if (separation > bestSeparation)
            {
                bestSeparation = separation;
                bestAxis = dist >= 0f ? L : -L;
                bestType = type;
                bestIndex = index;
            }
            return true;
        }

        if (!Consider(boxA.U0, 0, 0)) return;
        if (!Consider(boxA.U1, 0, 1)) return;
        if (!Consider(boxA.U2, 0, 2)) return;
        if (!Consider(boxB.U0, 1, 0)) return;
        if (!Consider(boxB.U1, 1, 1)) return;
        if (!Consider(boxB.U2, 1, 2)) return;

        ReadOnlySpan<Vector3> uAxes = [boxA.U0, boxA.U1, boxA.U2];
        ReadOnlySpan<Vector3> vAxes = [boxB.U0, boxB.U1, boxB.U2];

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                var c = Vector3.Cross(uAxes[i], vAxes[j]);
                var l = c.Length();
                if (l > 1e-5f && !Consider(c / l, 2, i * 3 + j)) return;
            }
        }

        if (bestType == -1 || bestSeparation > margin) return;
        var n = bestAxis;
        var axisFeature = (bestType << 4) | bestIndex;

        bool referenceIsA;
        int refAxis;
        if (bestType == 0) { referenceIsA = true; refAxis = bestIndex; }
        else if (bestType == 1) { referenceIsA = false; refAxis = bestIndex; }
        else
        {
            var bestDot = -1f;
            referenceIsA = true;
            refAxis = 0;
            for (var i = 0; i < 3; i++)
            {
                var da = MathF.Abs(Vector3.Dot(uAxes[i], n));
                if (da > bestDot) { bestDot = da; referenceIsA = true; refAxis = i; }
                var db = MathF.Abs(Vector3.Dot(vAxes[i], n));
                if (db > bestDot) { bestDot = db; referenceIsA = false; refAxis = i; }
            }
            if (bestDot < EdgeFaceAlignment)
            {
                SupportContact(boxA, boxB, n, bestSeparation, axisFeature * 256 + 255, outList);
                return;
            }
        }

        var rBox = referenceIsA ? boxA : boxB;
        var incBox = referenceIsA ? boxB : boxA;
        var refAxes = referenceIsA ? uAxes : vAxes;
        var incAxes = referenceIsA ? vAxes : uAxes;
        Span<float> ea = [boxA.Hx, boxA.Hy, boxA.Hz];
        Span<float> eb = [boxB.Hx, boxB.Hy, boxB.Hz];
        var refExt = referenceIsA ? ea : eb;
        var incExt = referenceIsA ? eb : ea;

        var outward = referenceIsA ? -n : n;
        var refDot = Vector3.Dot(refAxes[refAxis], outward);
        var refSign = refDot >= 0f ? 1f : -1f;
        var refN = refSign * refAxes[refAxis];
        var refCentre = new Vector3(rBox.Px, rBox.Py, rBox.Pz) + refExt[refAxis] * refN;

        var incAxis = 0;
        var incDot = -1f;
        for (var i = 0; i < 3; i++)
        {
            var d = MathF.Abs(Vector3.Dot(incAxes[i], refN));
            if (d > incDot) { incDot = d; incAxis = i; }
        }
        var incAlign = Vector3.Dot(incAxes[incAxis], refN);
        var incSign = incAlign <= 0f ? 1f : -1f;
        var incCentre = new Vector3(incBox.Px, incBox.Py, incBox.Pz) + incSign * incExt[incAxis] * incAxes[incAxis];
        var ia = (incAxis + 1) % 3;
        var ib = (incAxis + 2) % 3;

        Span<PolyVertex> polyIn = stackalloc PolyVertex[16];
        Span<PolyVertex> polyOut = stackalloc PolyVertex[16];
        var inCount = 4;

        ReadOnlySpan<float> saVals = [-1f, 1f, 1f, -1f];
        ReadOnlySpan<float> sbVals = [-1f, -1f, 1f, 1f];
        for (var corner = 0; corner < 4; corner++)
        {
            polyIn[corner] = new PolyVertex
            {
                Id = corner,
                P = incCentre + saVals[corner] * incExt[ia] * incAxes[ia] + sbVals[corner] * incExt[ib] * incAxes[ib]
            };
        }

        var featureBase = (axisFeature << 8) | ((referenceIsA ? 0 : 1) << 7) | (refAxis << 5) | ((refSign > 0f ? 0 : 1) << 4) | (incAxis << 2) | ((incSign > 0f ? 0 : 1) << 1);

        var plane = 0;
        Span<int> sides = [(refAxis + 1) % 3, (refAxis + 2) % 3];
        for (var sIdx = 0; sIdx < 2; sIdx++)
        {
            var side = sides[sIdx];
            for (var sSign = 0; sSign < 2; sSign++)
            {
                var sign = sSign == 0 ? 1f : -1f;
                var axis = refAxes[side];
                var limit = refExt[side];
                var outCount = 0;

                for (var i = 0; i < inCount; i++)
                {
                    var p = polyIn[i];
                    var q = polyIn[(i + 1) % inCount];
                    var dpp = sign * Vector3.Dot(p.P - refCentre, axis) - limit;
                    var dq = sign * Vector3.Dot(q.P - refCentre, axis) - limit;

                    if (dpp <= 0f) polyOut[outCount++] = p;
                    if ((dpp <= 0f) != (dq <= 0f))
                    {
                        var t = dpp / (dpp - dq);
                        polyOut[outCount++] = new PolyVertex
                        {
                            Id = (4 << plane) + p.Id,
                            P = p.P + t * (q.P - p.P)
                        };
                    }
                }

                if (outCount == 0)
                {
                    SupportContact(boxA, boxB, n, bestSeparation, featureBase * 256 + 255, outList);
                    return;
                }

                for (var i = 0; i < outCount; i++) polyIn[i] = polyOut[i];
                inCount = outCount;
                plane++;
            }
        }

        var candidates = new List<ContactPoint>(inCount);
        for (var i = 0; i < inCount; i++)
        {
            var vert = polyIn[i];
            var gap = Vector3.Dot(vert.P - refCentre, refN);
            if (gap > margin) continue;
            var mid = vert.P - gap * 0.5f * refN;
            candidates.Add(MakeContact(boxA.Body, boxB.Body, boxA, boxB, n, mid, gap, featureBase * 256 + vert.Id));
        }

        if (candidates.Count == 0)
        {
            SupportContact(boxA, boxB, n, bestSeparation, featureBase * 256 + 255, outList);
            return;
        }

        ReduceManifold(candidates, n);
        outList.AddRange(candidates);
    }

    // ---- Story 10.1: the solid cylinder (axis U1, radius Radius, half-width Hy), closed-form against every admitted shape ----

    /// <summary>The side tolerance mirrors the cap tolerance (EdgeFaceAlignment): a normal within the same angle of the plane
    /// perpendicular to the axis (|n·a| ≤ √(1 − 0.98²)) meets the curved side along a generator line.</summary>
    internal const float SideAlignment = 0.19899749f;
    /// <summary>Relative slack of the in-disc and in-face tests, so equal coaxial radii keep their shared rim.</summary>
    private const float InsideTolerance = 1e-4f;
    /// <summary>A SAT axis that is no face normal must beat the best face axis by this many metres, so a resting face keeps one
    /// stable normal.</summary>
    private const float FacePreference = 1e-4f;
    private const float RimDiagonal = 0.70710677f;
    /// <summary>Two cylinder sides touch along a stretch only when their axes are parallel within 2° (cos 2°). Skewed further, they
    /// touch at one point: a two-point stretch on a skewed pair puts the rows at the rolling body's rim ends and pumps energy into it
    /// (review F4, measured on the reviewer's drop 17). The rolling row shares it: parallel curved sides roll about one common axis.</summary>
    internal const float LineAlignment = 0.99939083f;

    /// <summary>Which cylinder feature a contact came from; the feature id is (kind &lt;&lt; 12) | detail, distinct within a manifold,
    /// so a warm start carries over while that feature persists (stability while rolling is not separately qualified).</summary>
    private enum CylinderFeature
    {
        Cap = 1,
        CapSample = 2,
        PartnerVertex = 3,
        EdgeCrossing = 4,
        Side = 5,
        Rim = 6,
        PartnerSample = 7,
        RimCrossing = 8,
        Support = 9
    }

    private static int Feature(CylinderFeature kind, int detail) => ((int)kind << 12) | detail;

    // Eight rim samples at 45° steps in the cylinder frame (U0 at 0°, U2 at 90°).
    private static ReadOnlySpan<float> RimCos => [1f, RimDiagonal, 0f, -RimDiagonal, -1f, -RimDiagonal, 0f, RimDiagonal];
    private static ReadOnlySpan<float> RimSin => [0f, RimDiagonal, 1f, RimDiagonal, 0f, -RimDiagonal, -1f, -RimDiagonal];

    private static Vector3 RimDirection(SimCollider c, int k) => RimCos[k] * c.U0 + RimSin[k] * c.U2;

    /// <summary>The component of <paramref name="v"/> perpendicular to the unit <paramref name="axis"/>.</summary>
    private static Vector3 Radial(Vector3 v, Vector3 axis) => v - Vector3.Dot(v, axis) * axis;

    /// <summary>The unit radial direction of <paramref name="v"/> about the axis, or the fallback's when v lies (nearly) on it. An
    /// integrated orientation is not exactly unit: on a body turned over (qx² + qz² near 1) a squared-norm error ε leaves an axis of
    /// length ≈ 1 − 2ε, and v − (v·a)a then keeps an axial residue ≈ 2ε|v| that rivals a micro-radian radial part (a cap
    /// tilted 2e-6 rad read a 12% axial rim direction and a −24.8 mm bench gap). One re-orthogonalisation removes that residue; a
    /// radial part shorter than 1e-3 of |v| takes the fallback instead (a guard, not separately observable in tests).</summary>
    private static Vector3 RadialUnit(Vector3 v, Vector3 axis, Vector3 fallback)
    {
        var p = Radial(v, axis);
        if (!(p.Length() > 1e-3f * v.Length())) p = Radial(fallback, axis);
        p = Radial(p, axis);
        var length = p.Length();
        return length > 1e-6f ? p / length : fallback;
    }

    /// <summary>The cylinder's support point in direction d: the rim point of the cap facing d.</summary>
    private static Vector3 CylinderSupport(SimCollider c, Vector3 d)
    {
        var t = Vector3.Dot(d, c.U1);
        return c.Position + (t >= 0f ? c.Hy : -c.Hy) * c.U1 + c.Radius * RadialUnit(d, c.U1, Vector3.Zero);
    }

    /// <summary>The generator segment of the curved side facing direction d (start and full length).</summary>
    private static (Vector3 Start, Vector3 Length) Generator(SimCollider c, Vector3 d) =>
        (c.Position - c.Hy * c.U1 + c.Radius * RadialUnit(d, c.U1, c.U0), 2f * c.Hy * c.U1);

    private static Vector3 ClosestOnSegment(Vector3 start, Vector3 length, Vector3 x)
    {
        var ll = Vector3.Dot(length, length);
        return ll > 1e-12f ? start + Math.Clamp(Vector3.Dot(x - start, length) / ll, 0f, 1f) * length : start;
    }

    /// <summary>The closest points of segments p0 + s d0 and p1 + t d1 (s, t in [0, 1]); a degenerate segment is its point.</summary>
    private static (Vector3 A, Vector3 B) ClosestPoints(Vector3 p0, Vector3 d0, Vector3 p1, Vector3 d1)
    {
        var r = p0 - p1;
        var a = Vector3.Dot(d0, d0);
        var e = Vector3.Dot(d1, d1);
        var f = Vector3.Dot(d1, r);
        float s, t;
        if (a <= 1e-12f && e <= 1e-12f) return (p0, p1);
        if (a <= 1e-12f) { s = 0f; t = Math.Clamp(f / e, 0f, 1f); }
        else
        {
            var c = Vector3.Dot(d0, r);
            if (e <= 1e-12f) { t = 0f; s = Math.Clamp(-c / a, 0f, 1f); }
            else
            {
                var b = Vector3.Dot(d0, d1);
                var denom = a * e - b * b;
                s = denom > 1e-12f * a * e ? Math.Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
                t = (b * s + f) / e;
                if (t < 0f) { t = 0f; s = Math.Clamp(-c / a, 0f, 1f); }
                else if (t > 1f) { t = 1f; s = Math.Clamp((b - c) / a, 0f, 1f); }
            }
        }
        return (p0 + s * d0, p1 + t * d1);
    }

    /// <summary>Narrows [t0, t1] to the parameters where p + t d lies within [-limit, limit] on one coordinate.</summary>
    private static void ClipSlab(float p, float d, float limit, ref float t0, ref float t1)
    {
        if (MathF.Abs(d) < 1e-12f)
        {
            if (MathF.Abs(p) > limit) { t0 = 1f; t1 = 0f; }
            return;
        }
        var ta = (-limit - p) / d;
        var tb = (limit - p) / d;
        if (ta > tb) (ta, tb) = (tb, ta);
        t0 = MathF.Max(t0, ta);
        t1 = MathF.Min(t1, tb);
    }

    /// <summary>The parameters of p + t d (t in [0, 1]) inside the circle of radius r about the origin (2D), or an empty range.</summary>
    private static (float T0, float T1) ClipCircle(float px, float py, float dx, float dy, float radius)
    {
        var a = dx * dx + dy * dy;
        var c = px * px + py * py - radius * radius;
        if (a < 1e-12f) return c <= 0f ? (0f, 1f) : (1f, 0f);
        var b = 2f * (px * dx + py * dy);
        var disc = b * b - 4f * a * c;
        if (disc < 0f) return (1f, 0f);
        var root = MathF.Sqrt(disc);
        return (MathF.Max(0f, (-b - root) / (2f * a)), MathF.Min(1f, (-b + root) / (2f * a)));
    }

    /// <summary>One SAT step with exact support projections. False when <paramref name="axis"/> separates the pair beyond the
    /// margin; otherwise keeps the deepest axis, oriented from B toward A.</summary>
    private static bool SatAxis(Vector3 axis, float extentA, float extentB, Vector3 dp, float margin, bool face,
        ref float best, ref Vector3 bestAxis)
    {
        var dist = Vector3.Dot(dp, axis);
        var separation = MathF.Abs(dist) - (extentA + extentB);
        if (separation > margin) return false;
        if (separation > best + (face || float.IsNegativeInfinity(best) ? 0f : FacePreference))
        {
            best = separation;
            bestAxis = dist >= 0f ? axis : -axis;
        }
        return true;
    }

    private static ContactPoint CylinderContact(SimCollider colA, SimCollider colB, Vector3 n, Vector3 p, float gap, int feature,
        ContactSupport supportA, ContactSupport supportB)
    {
        var contact = MakeContact(colA.Body, colB.Body, colA, colB, n, p, gap, feature);
        contact.SupportA = supportA;
        contact.SupportB = supportB;
        return contact;
    }

    /// <summary>A cylinder on the workbench plane. Within the cap tolerance it rests on four rim points of the facing cap: the true
    /// deepest rim point and the points 90°, 180° and 270° from it. Otherwise each cap contributes its deepest rim point within the
    /// margin: two along the side, one when tilted.</summary>
    public static void CollideCylinderPlane(SimCollider cyl, SimCollider plane, float margin, List<ContactPoint> outList)
    {
        var n = plane.U1;
        var pp = plane.Position;
        var a = cyl.U1;
        var t = Vector3.Dot(a, n);
        if (MathF.Abs(t) >= EdgeFaceAlignment)
        {
            var s = t > 0f ? -1f : 1f;
            var cc = cyl.Position + s * cyl.Hy * a;
            var capIndex = s > 0f ? 0 : 1;
            // Ranked from the true deepest rim point (the radial direction of −n), then 90°, 180° and 270° from it, so a tilted cap
            // rests on its real lowest point and the manifold continues into the rim regime; upright, the square starts at U0.
            var deepest = RadialUnit(-n, a, cyl.U0);
            var across = Vector3.Cross(a, deepest);
            for (var k = 0; k < 4; k++)
            {
                var v = cc + cyl.Radius * (k switch { 0 => deepest, 1 => across, 2 => -deepest, _ => -across });
                var gap = Vector3.Dot(v - pp, n);
                if (gap <= margin)
                    outList.Add(CylinderContact(cyl, plane, n, v - gap * 0.5f * n, gap, Feature(CylinderFeature.Cap, (capIndex << 3) | k),
                        ContactSupport.Material, ContactSupport.Material));
            }
            return;
        }

        var side = MathF.Abs(t) <= SideAlignment;
        var u = RadialUnit(-n, a, cyl.U0);
        for (var capIndex = 0; capIndex < 2; capIndex++)
        {
            var v = cyl.Position + (capIndex == 0 ? cyl.Hy : -cyl.Hy) * a + cyl.Radius * u;
            var gap = Vector3.Dot(v - pp, n);
            if (gap <= margin)
                outList.Add(CylinderContact(cyl, plane, n, v - gap * 0.5f * n, gap,
                    Feature(side ? CylinderFeature.Side : CylinderFeature.Rim, capIndex),
                    side ? ContactSupport.Axis : ContactSupport.Material, ContactSupport.Material));
        }
    }

    /// <summary>A sphere against the solid cylinder through the exact closest point (axial and radial coordinates clamped). A centre
    /// inside leaves by the shallower feature, side or cap. One point; a side contact rolls on the axis support.</summary>
    public static void CollideCylinderSphere(SimCollider cyl, SimCollider sphere, float margin, List<ContactPoint> outList)
    {
        var a = cyl.U1;
        var h = cyl.Hy;
        var r = cyl.Radius;
        var d = sphere.Position - cyl.Position;
        var y = Vector3.Dot(d, a);
        if (MathF.Abs(y) > h + sphere.Radius + margin) return;
        var q = d - y * a;
        var rho = q.Length();
        if (rho > r + sphere.Radius + margin) return;
        var radial = rho > 1e-6f ? q / rho : cyl.U0;

        Vector3 n, x;
        float gap;
        CylinderFeature feature;
        var detail = 0;
        if (MathF.Abs(y) > h || rho > r)
        {
            var yc = Math.Clamp(y, -h, h);
            x = cyl.Position + yc * a + MathF.Min(rho, r) * radial;
            var diff = sphere.Position - x;
            var dist = diff.Length();
            gap = dist - sphere.Radius;
            if (gap > margin) return;
            feature = MathF.Abs(y) <= h ? CylinderFeature.Side : rho <= r ? CylinderFeature.Cap : CylinderFeature.Rim;
            if (feature != CylinderFeature.Side) detail = y >= 0f ? 0 : 1;
            n = dist > 1e-6f ? diff / dist : feature == CylinderFeature.Side ? radial : (y >= 0f ? a : -a);
        }
        else if (r - rho < h - MathF.Abs(y))
        {
            n = radial;
            x = cyl.Position + y * a + r * radial;
            gap = -(r - rho) - sphere.Radius;
            feature = CylinderFeature.Side;
        }
        else
        {
            var s = y >= 0f ? 1f : -1f;
            n = s * a;
            x = cyl.Position + s * h * a + q;
            gap = -(h - MathF.Abs(y)) - sphere.Radius;
            feature = CylinderFeature.Cap;
            detail = s > 0f ? 0 : 1;
        }

        outList.Add(CylinderContact(sphere, cyl, n, x, gap, Feature(feature, detail), ContactSupport.Material,
            feature == CylinderFeature.Side ? ContactSupport.Axis : ContactSupport.Material));
    }

    /// <summary>A cylinder (A) against a box (B). A SAT over the box faces, the cylinder axis, axis × box edge and the radial
    /// directions from the axis to the box vertices (exact support projections, face preference) proves separation early. The gap and
    /// normal come from the exact closest features (`CylinderBoxClosest`); a shallow overlap is measured on the cylinder's core, and a
    /// deeper one takes the deepest feature or the SAT, whichever is shallower. Contacts follow n·a: cap-face against an aligned box
    /// face (rim samples, the deepest rim point and box vertices inside the disc, edge crossings of the rim, reduced to four),
    /// side-line (the generator clipped to a face, or a box edge within the cap tolerance of parallel: at most two, each end's own
    /// distance to the side), or one point between the closest
    /// features. No point claims more depth than the pair.</summary>
    public static void CollideCylinderBox(SimCollider cyl, SimCollider box, float margin, List<ContactPoint> outList)
    {
        var ca = cyl.Position;
        var dp = ca - box.Position;
        var a = cyl.U1;
        var best = float.NegativeInfinity;
        var bestAxis = Vector3.Zero;
        bool Axis(Vector3 l, bool face) =>
            SatAxis(l, CylinderProjection(cyl, l), box.Hx * MathF.Abs(Vector3.Dot(box.U0, l)) + box.Hy * MathF.Abs(Vector3.Dot(box.U1, l)) +
                box.Hz * MathF.Abs(Vector3.Dot(box.U2, l)), dp, margin, face, ref best, ref bestAxis);
        bool Direction(Vector3 d)
        {
            var length = d.Length();
            return !(length > 1e-6f) || Axis(d / length, false);
        }

        if (!Axis(box.U0, true) || !Axis(box.U1, true) || !Axis(box.U2, true) || !Axis(a, true)) return;
        if (!Direction(Vector3.Cross(a, box.U0)) || !Direction(Vector3.Cross(a, box.U1)) || !Direction(Vector3.Cross(a, box.U2))) return;

        Span<Vector3> vertices = stackalloc Vector3[8];
        for (var v = 0; v < 8; v++)
            vertices[v] = BoxVertex(box, (v & 1) != 0 ? 1f : -1f, (v & 2) != 0 ? 1f : -1f, (v & 4) != 0 ? 1f : -1f);
        foreach (var vertex in vertices)
            if (!Direction(Radial(vertex - ca, a))) return;

        if (float.IsNegativeInfinity(best)) return;
        // The SAT axes above only prove separation; the gap and normal come from the exact closest features.
        var closest = CylinderBoxClosest(Solid.Of(cyl), box);
        if (closest.Gap > margin) return;
        // Apart, the closest features are the exact gap. In a shallow overlap the exact shapes' deepest point exits by the cap or the
        // side and flips between them round a rim (review F4), so the gap and normal come from the core shrunk by its margin, which is
        // still apart: exact on the cap, side and faces, rounded by at most 0.29 margin at a rim. Deeper, the deepest closest feature
        // or the SAT, whichever is shallower.
        var exact = closest.Gap > 0f;
        if (!exact)
        {
            var shrink = CoreMargin(cyl);
            var core = CylinderBoxClosest(Solid.Of(cyl, shrink), box);
            if (core.Gap > 0f)
            {
                closest = core;
                // The exact shapes overlap, so the gap is never positive: the core's rounding at a rim (up to the margin along a grazing
                // approach) may not report them apart (review N9 walk, cylinder–box).
                closest.Gap = MathF.Min(core.Gap - shrink, 0f);
                closest.OnA = core.OnA - shrink * core.Normal;
                exact = true;
            }
            else exact = closest.Gap > best;
        }
        var n = exact ? closest.Normal : bestAxis;
        var separation = exact ? closest.Gap : best;
        var t = MathF.Abs(Vector3.Dot(n, a));
        var first = outList.Count;
        if (t >= EdgeFaceAlignment) CylinderCapOnBox(cyl, box, n, margin, outList);
        else if (t <= SideAlignment) CylinderSideOnBox(cyl, box, n, separation, margin, outList);
        else
        {
            var rim = CylinderSupport(cyl, -n);
            outList.Add(CylinderContact(cyl, box, n, exact ? (closest.OnA + closest.OnB) * 0.5f : rim - separation * 0.5f * n, separation,
                Feature(CylinderFeature.Rim, Vector3.Dot(rim - ca, a) >= 0f ? 0 : 1), ContactSupport.Material, ContactSupport.Material));
        }

        // Every manifold is checked for depth coverage within 0.1 mm of the pair
        // gap, including when the shallow core crosses zero. If needed, the closest
        // witness joins with its own normal, surface points and gap as one tuple.
        KeepClosest(cyl, box, outList, first, closest, separation,
            MathF.Abs(Vector3.Dot(closest.Normal, a)) <= SideAlignment ? ContactSupport.Axis : ContactSupport.Material,
            ContactSupport.Material);
        NoDeeperThan(outList, first, separation);
    }

    /// <summary>Ensure depth coverage within 0.1 mm of the pair's gap: when existing points do not cover that depth, include the
    /// closest features' midpoint with its own normal and signed gap. Exact witness membership is not required when another point
    /// already covers the depth. The common pair-depth bound applies afterward; side reduction keeps the capacity of four.</summary>
    private static void KeepClosest(SimCollider a, SimCollider b, List<ContactPoint> points, int first, in Closest closest, float separation, ContactSupport supportA,
        ContactSupport supportB)
    {
        for (var i = first; i < points.Count; i++)
            if (points[i].Gap <= separation + 1e-4f) return;
        points.Add(CylinderContact(a, b, closest.Normal, (closest.OnA + closest.OnB) * 0.5f, closest.Gap, Feature(CylinderFeature.Support, 1),
            supportA, supportB));
        ReduceSideManifold(points, first);
    }

    /// <summary>Side manifolds keep the manifold capacity of four (review F4 round 4). With more points, the rule is deterministic: keep
    /// the deepest point (lowest gap, first on a tie), then the farthest point from it, then maximize summed triangle
    /// areas to retained pairs until four. Area ties maximize squared-distance sum (preserving collinear extent), then depth/order. Emit in original order.</summary>
    public static void ReduceSideManifold(List<ContactPoint> points, int first)
    {
        var count = points.Count - first;
        if (count <= 4) return;
        Span<bool> keep = stackalloc bool[count];
        keep.Clear();
        var deepest = 0;
        for (var i = 1; i < count; i++)
            if (points[first + i].Gap < points[first + deepest].Gap) deepest = i;
        keep[deepest] = true;
        var opposite = deepest;
        var farthest = -1f;
        for (var i = 0; i < count; i++)
        {
            if (i == deepest) continue;
            var distance = (points[first + i].Point - points[first + deepest].Point).LengthSquared();
            if (distance > farthest) { farthest = distance; opposite = i; }
        }
        keep[opposite] = true;
        var kept = 0;
        foreach (var k in keep) if (k) kept++;
        while (kept < 4)
        {
            var next = -1;
            var widest = -1f;
            var longest = -1f;
            for (var i = 0; i < count; i++)
            {
                if (keep[i]) continue;
                var area = 0f;
                var span = 0f;
                for (var j = 0; j < count; j++)
                {
                    if (!keep[j]) continue;
                    span += (points[first + j].Point - points[first + i].Point).LengthSquared();
                    for (var k = j + 1; k < count; k++)
                        if (keep[k]) area += Vector3.Cross(points[first + j].Point - points[first + i].Point,
                            points[first + k].Point - points[first + i].Point).Length();
                }
                if (area > widest || (area == widest && (span > longest ||
                    (span == longest && (next < 0 || points[first + i].Gap < points[first + next].Gap)))))
                {
                    widest = area;
                    longest = span;
                    next = i;
                }
            }
            keep[next] = true;
            kept++;
        }
        var write = first;
        for (var i = 0; i < count; i++)
            if (keep[i]) points[write++] = points[first + i];
        points.RemoveRange(write, points.Count - write);
    }

    /// <summary>No manifold point claims more penetration than the pair's own depth estimate (review F4/N5).</summary>
    private static void NoDeeperThan(List<ContactPoint> points, int first, float separation)
    {
        for (var i = first; i < points.Count; i++)
            if (points[i].Gap < separation) points[i].Gap = separation;
    }

    // ---- exact closest features (Story 10.1 review F2) ----
    // A curved rim has no closed-form closest point to an edge or another rim, so the exact distance is the minimum of the solid
    // cylinder's closed-form signed distance along the partner's features: along a straight edge that distance is convex, so a
    // fixed number of golden-section steps finds its minimum; along a rim it is sampled at fixed angles and the best bracket is
    // refined the same way. Every count is fixed: no simplex, no convergence test, no data-dependent iteration (not GJK/EPA/MPR).

    private const int SegmentSteps = 24;
    private const int RimSamples = 16;
    private const int RimSteps = 16;
    private const float GoldenSection = 0.618034f;

    /// <summary>The exact closest features of a pair: its signed gap, the unit normal from B toward A, and the closest point on each
    /// (overlapping: the deepest feature point and its exit point).</summary>
    private struct Closest
    {
        public float Gap;
        public Vector3 Normal;
        public Vector3 OnA;
        public Vector3 OnB;

        public void Keep(float gap, Vector3 normal, Vector3 onA, Vector3 onB)
        {
            if (!(gap < Gap)) return;
            Gap = gap; Normal = normal; OnA = onA; OnB = onB;
        }
    }

    /// <summary>A solid cylinder for the closest-feature measures: a collider's own (exact) shape, or its core shrunk by a margin on
    /// every face, so a shallow overlap of the true shapes is still a separation of the cores.</summary>
    private readonly struct Solid(Vector3 position, Vector3 u0, Vector3 u1, Vector3 u2, float radius, float hy)
    {
        public Vector3 Position { get; } = position;
        public Vector3 U0 { get; } = u0;
        public Vector3 U1 { get; } = u1;
        public Vector3 U2 { get; } = u2;
        public float Radius { get; } = radius;
        public float Hy { get; } = hy;

        public static Solid Of(SimCollider c, float shrink = 0f) => new(c.Position, c.U0, c.U1, c.U2, c.Radius - shrink, c.Hy - shrink);
    }

    /// <summary>The core margin of a cylinder: 1 mm, at most half its radius or half-width (Story 10.1 review F4).</summary>
    private static float CoreMargin(SimCollider c) => MathF.Min(CoreDepth, 0.5f * MathF.Min(c.Radius, c.Hy));

    /// <summary>Overlaps up to this depth are measured between the shrunk cores, whose closest features give a continuous normal
    /// (blending cap and side round a rim, where the exact shapes' exit direction flips between them).</summary>
    private const float CoreDepth = 0.001f;

    /// <summary>The closed-form signed distance from p to the solid cylinder (negative inside).</summary>
    private static float CylinderSignedDistance(in Solid c, Vector3 p)
    {
        var d = p - c.Position;
        var y = Vector3.Dot(d, c.U1);
        var dy = MathF.Abs(y) - c.Hy;
        var dr = (d - y * c.U1).Length() - c.Radius;
        var outAxial = MathF.Max(dy, 0f);
        var outRadial = MathF.Max(dr, 0f);
        return MathF.Sqrt(outAxial * outAxial + outRadial * outRadial) + MathF.Min(MathF.Max(dy, dr), 0f);
    }

    /// <summary>The signed distance from p to the solid cylinder, its closest surface point and the unit direction leaving the
    /// cylinder there (outside: from the surface toward p; inside: p's shortest exit).</summary>
    private static float CylinderDistance(in Solid c, Vector3 p, out Vector3 surface, out Vector3 outward)
    {
        var a = c.U1;
        var d = p - c.Position;
        var y = Vector3.Dot(d, a);
        var q = d - y * a;
        var rho = q.Length();
        var radial = rho > 1e-6f ? q / rho : c.U0;
        var dy = MathF.Abs(y) - c.Hy;
        var dr = rho - c.Radius;
        var cap = y >= 0f ? a : -a;
        if (dy > 0f || dr > 0f)
        {
            surface = c.Position + Math.Clamp(y, -c.Hy, c.Hy) * a + MathF.Min(rho, c.Radius) * radial;
            // Form the outside displacement in feature coordinates. Subtracting
            // nearly coincident world points loses its small radial component
            // near a face/edge boundary and can select the wrong support edge.
            var diff = MathF.Max(dy, 0f) * cap + MathF.Max(dr, 0f) * radial;
            var dist = diff.Length();
            outward = dist > 1e-9f ? diff / dist : dr > dy ? radial : cap;
            return dist;
        }
        if (dr > dy)
        {
            surface = c.Position + y * a + c.Radius * radial;
            outward = radial;
            return dr;
        }
        surface = p + (-dy) * cap;
        outward = cap;
        return dy;
    }

    /// <summary>The signed distance from p to the cylinder's curved side (its radial distance from the axis less the radius) and the
    /// side point radially nearest it: the measure of a side stretch, where p's axial position may lie on a cap plane.</summary>
    private static float SideDistance(in Solid c, Vector3 p, out Vector3 surface)
    {
        var d = p - c.Position;
        var y = Vector3.Dot(d, c.U1);
        var q = d - y * c.U1;
        var rho = q.Length();
        surface = c.Position + y * c.U1 + c.Radius * (rho > 1e-6f ? q / rho : c.U0);
        return rho - c.Radius;
    }

    /// <summary>A point of the cylinder's support set in direction d; a flat support set (a cap or a generator) gives its point
    /// nearest <paramref name="target"/>.</summary>
    private static Vector3 CylinderSupportNear(in Solid c, Vector3 d, Vector3 target)
    {
        var a = c.U1;
        var t = Vector3.Dot(d, a);
        var axial = MathF.Abs(t) > 1e-6f ? (t > 0f ? c.Hy : -c.Hy) : Math.Clamp(Vector3.Dot(target - c.Position, a), -c.Hy, c.Hy);
        var radialDirection = d - t * a;
        var length = radialDirection.Length();
        Vector3 radial;
        if (length > 1e-6f) radial = c.Radius / length * radialDirection;
        else
        {
            radial = Radial(target - c.Position, a);
            var reach = radial.Length();
            if (reach > c.Radius) radial *= c.Radius / reach;
        }
        return c.Position + axial * a + radial;
    }

    private interface IDistanceCurve
    {
        float Distance(float s);
    }

    private readonly struct SegmentCurve(Solid cylinder, Vector3 start, Vector3 length) : IDistanceCurve
    {
        public float Distance(float s) => CylinderSignedDistance(cylinder, start + s * length);
    }

    private readonly struct RimCurve(Solid cylinder, Vector3 centre, Vector3 u, Vector3 v, float radius) : IDistanceCurve
    {
        public float Distance(float s) => CylinderSignedDistance(cylinder, centre + radius * (MathF.Cos(s) * u + MathF.Sin(s) * v));
    }

    /// <summary>The parameter in [lo, hi] minimising a unimodal curve, by a fixed number of golden-section steps.</summary>
    private static float GoldenMinimum<T>(in T curve, float lo, float hi, int steps) where T : struct, IDistanceCurve
    {
        var x1 = hi - GoldenSection * (hi - lo);
        var x2 = lo + GoldenSection * (hi - lo);
        var f1 = curve.Distance(x1);
        var f2 = curve.Distance(x2);
        for (var step = 0; step < steps; step++)
        {
            if (f1 <= f2)
            {
                hi = x2; x2 = x1; f2 = f1;
                x1 = hi - GoldenSection * (hi - lo);
                f1 = curve.Distance(x1);
            }
            else
            {
                lo = x1; x1 = x2; f1 = f2;
                x2 = lo + GoldenSection * (hi - lo);
                f2 = curve.Distance(x2);
            }
        }
        return f1 <= f2 ? x1 : x2;
    }

    /// <summary>The angle of a rim (centre, unit u and v spanning its plane) whose point is nearest the solid cylinder: the best of
    /// fixed samples, refined in its bracket.</summary>
    private static float RimMinimum(in Solid cylinder, Vector3 centre, Vector3 u, Vector3 v, float radius)
    {
        var curve = new RimCurve(cylinder, centre, u, v, radius);
        const float step = 2f * MathF.PI / RimSamples;
        var bestAngle = 0f;
        var bestDistance = float.PositiveInfinity;
        for (var k = 0; k < RimSamples; k++)
        {
            var distance = curve.Distance(k * step);
            if (distance < bestDistance) { bestDistance = distance; bestAngle = k * step; }
        }
        return GoldenMinimum(in curve, bestAngle - step, bestAngle + step, RimSteps);
    }

    /// <summary>Cylinder (A) against box (B), exactly: a box face facing the cylinder whose plane the cylinder's support point lies
    /// over, and the deepest point of every box edge against the solid cylinder (which covers the cap, side and rim at once).</summary>
    private static Closest CylinderBoxClosest(in Solid cyl, SimCollider box)
    {
        var closest = new Closest { Gap = float.PositiveInfinity };
        ReadOnlySpan<Vector3> axes = [box.U0, box.U1, box.U2];
        ReadOnlySpan<float> extents = [box.Hx, box.Hy, box.Hz];
        for (var i = 0; i < 3; i++)
        {
            var j = (i + 1) % 3;
            var k = (i + 2) % 3;
            for (var side = 0; side < 2; side++)
            {
                var faceN = (side == 0 ? 1f : -1f) * axes[i];
                if (Vector3.Dot(cyl.Position - box.Position, faceN) < 0f) continue;
                var fc = box.Position + extents[i] * faceN;
                var support = CylinderSupportNear(cyl, -faceN, fc);
                var rel = support - fc;
                if (MathF.Abs(Vector3.Dot(rel, axes[j])) > extents[j] || MathF.Abs(Vector3.Dot(rel, axes[k])) > extents[k]) continue;
                var gap = Vector3.Dot(rel, faceN);
                closest.Keep(gap, faceN, support, support - gap * faceN);
            }
        }

        Span<Vector3> vertices = stackalloc Vector3[8];
        for (var v = 0; v < 8; v++)
            vertices[v] = BoxVertex(box, (v & 1) != 0 ? 1f : -1f, (v & 2) != 0 ? 1f : -1f, (v & 4) != 0 ? 1f : -1f);
        for (var v = 0; v < 8; v++)
            for (var bit = 1; bit < 8; bit <<= 1)
            {
                if ((v & bit) != 0) continue;
                var start = vertices[v];
                var length = vertices[v | bit] - start;
                var at = start + GoldenMinimum(new SegmentCurve(cyl, start, length), 0f, 1f, SegmentSteps) * length;
                var gap = CylinderDistance(cyl, at, out var surface, out var outward);
                closest.Keep(gap, -outward, surface, at);
            }
        return closest;
    }

    /// <summary>Two cylinders exactly: a cap facing the partner whose support point lies over the disc, the two sides through the
    /// axes' common perpendicular (or a parallel overlap), and every rim against the other solid (covering its cap, side and rims).</summary>
    private static Closest CylinderCylinderClosest(in Solid colA, in Solid colB)
    {
        var closest = new Closest { Gap = float.PositiveInfinity };
        var pa = colA.Position;
        var pb = colB.Position;
        var aa = colA.U1;
        var ab = colB.U1;

        for (var s = 0; s < 2; s++)
        {
            var sign = s == 0 ? 1f : -1f;
            var capA = sign * aa;
            if (Vector3.Dot(pb - pa, capA) >= 0f)
            {
                var cc = pa + colA.Hy * capA;
                var support = CylinderSupportNear(colB, -capA, cc);
                if (Radial(support - cc, aa).LengthSquared() <= colA.Radius * colA.Radius)
                {
                    var gap = Vector3.Dot(support - cc, capA);
                    closest.Keep(gap, -capA, support - gap * capA, support);
                }
            }
            var capB = sign * ab;
            if (Vector3.Dot(pa - pb, capB) >= 0f)
            {
                var cc = pb + colB.Hy * capB;
                var support = CylinderSupportNear(colA, -capB, cc);
                if (Radial(support - cc, ab).LengthSquared() <= colB.Radius * colB.Radius)
                {
                    var gap = Vector3.Dot(support - cc, capB);
                    closest.Keep(gap, capB, support, support - gap * capB);
                }
            }
        }

        // Side against side: the axes' closest points, both within their half-widths.
        var w = pa - pb;
        var b = Vector3.Dot(aa, ab);
        var denom = 1f - b * b;
        float sa, sb;
        if (denom > 1e-6f)
        {
            var d = Vector3.Dot(aa, w);
            var e = Vector3.Dot(ab, w);
            sa = (b * e - d) / denom;
            sb = (e - b * d) / denom;
        }
        else
        {
            // Parallel: the middle of the axial overlap.
            var t0 = Vector3.Dot(pb - colB.Hy * ab - pa, aa);
            var t1 = Vector3.Dot(pb + colB.Hy * ab - pa, aa);
            sa = 0.5f * (MathF.Max(-colA.Hy, MathF.Min(t0, t1)) + MathF.Min(colA.Hy, MathF.Max(t0, t1)));
            sb = Vector3.Dot(pa + sa * aa - pb, ab);
        }
        if (MathF.Abs(sa) <= colA.Hy && MathF.Abs(sb) <= colB.Hy)
        {
            var xa = pa + sa * aa;
            var xb = pb + sb * ab;
            var between = xa - xb;
            var length = between.Length();
            if (length > 1e-6f)
            {
                var n = between / length;
                closest.Keep(length - colA.Radius - colB.Radius, n, xa - colA.Radius * n, xb + colB.Radius * n);
            }
        }

        // Every rim against the other solid.
        for (var s = 0; s < 2; s++)
        {
            var sign = s == 0 ? 1f : -1f;
            var rimB = pb + sign * colB.Hy * ab;
            var angleB = RimMinimum(colA, rimB, colB.U0, colB.U2, colB.Radius);
            var onB = rimB + colB.Radius * (MathF.Cos(angleB) * colB.U0 + MathF.Sin(angleB) * colB.U2);
            var gapB = CylinderDistance(colA, onB, out var surfaceA, out var outwardA);
            closest.Keep(gapB, -outwardA, surfaceA, onB);

            var rimA = pa + sign * colA.Hy * aa;
            var angleA = RimMinimum(colB, rimA, colA.U0, colA.U2, colA.Radius);
            var onA = rimA + colA.Radius * (MathF.Cos(angleA) * colA.U0 + MathF.Sin(angleA) * colA.U2);
            var gapA = CylinderDistance(colB, onA, out var surfaceB, out var outwardB);
            closest.Keep(gapA, outwardB, onA, surfaceB);
        }
        return closest;
    }

    /// <summary>The cylinder cap facing the box against the box face most anti-parallel to it, measured along the cap normal.</summary>
    private static void CylinderCapOnBox(SimCollider cyl, SimCollider box, Vector3 n, float margin, List<ContactPoint> outList)
    {
        var a = cyl.U1;
        var s = Vector3.Dot(n, a) > 0f ? -1f : 1f;
        var capN = s * a;
        var cc = cyl.Position + s * cyl.Hy * a;
        var capIndex = s > 0f ? 0 : 1;

        ReadOnlySpan<Vector3> axes = [box.U0, box.U1, box.U2];
        ReadOnlySpan<float> extents = [box.Hx, box.Hy, box.Hz];
        var face = 0;
        var faceDot = -1f;
        for (var i = 0; i < 3; i++)
        {
            var d = MathF.Abs(Vector3.Dot(axes[i], capN));
            if (d > faceDot) { faceDot = d; face = i; }
        }
        var faceSign = Vector3.Dot(axes[face], capN) > 0f ? -1f : 1f;
        var faceN = faceSign * axes[face];
        var fc = box.Position + extents[face] * faceN;
        var j = (face + 1) % 3;
        var k = (face + 2) % 3;
        var denom = Vector3.Dot(capN, faceN);
        var faceId = (capIndex << 3) | (face << 1) | (faceSign > 0f ? 0 : 1);

        Span<Vector3> corners = stackalloc Vector3[4];
        ReadOnlySpan<float> sj = [-1f, 1f, 1f, -1f];
        ReadOnlySpan<float> sk = [-1f, -1f, 1f, 1f];
        for (var m = 0; m < 4; m++) corners[m] = fc + sj[m] * extents[j] * axes[j] + sk[m] * extents[k] * axes[k];

        var limit = cyl.Radius * (1f + InsideTolerance);
        var candidates = new List<ContactPoint>(20);
        void Add(Vector3 incident, float gap, int feature)
        {
            if (gap <= margin)
                candidates.Add(CylinderContact(cyl, box, n, incident - gap * 0.5f * capN, gap, feature, ContactSupport.Material,
                    ContactSupport.Material));
        }

        for (var m = 0; m < 4; m++)
        {
            var rel = corners[m] - cc;
            var x = Vector3.Dot(rel, cyl.U0);
            var y = Vector3.Dot(rel, cyl.U2);
            if (x * x + y * y <= limit * limit) Add(corners[m], Vector3.Dot(rel, capN), Feature(CylinderFeature.PartnerVertex, (faceId << 2) | m));

            var next = corners[(m + 1) % 4] - cc;
            var (t0, t1) = ClipCircle(x, y, Vector3.Dot(next, cyl.U0) - x, Vector3.Dot(next, cyl.U2) - y, cyl.Radius);
            if (t0 > t1) continue;
            var edge = corners[(m + 1) % 4] - corners[m];
            if (t0 > 0f) { var p = corners[m] + t0 * edge; Add(p, Vector3.Dot(p - cc, capN), Feature(CylinderFeature.EdgeCrossing, (faceId << 3) | (m << 1))); }
            if (t1 < 1f) { var p = corners[m] + t1 * edge; Add(p, Vector3.Dot(p - cc, capN), Feature(CylinderFeature.EdgeCrossing, (faceId << 3) | (m << 1) | 1)); }
        }

        if (MathF.Abs(denom) > 1e-6f)
            // Eight fixed rim samples and the rim point deepest toward the face (the radial direction of −faceN).
            for (var sample = 0; sample < 9; sample++)
            {
                var rim = cc + cyl.Radius * (sample < 8 ? RimDirection(cyl, sample) : RadialUnit(-faceN, a, cyl.U0));
                var gap = Vector3.Dot(fc - rim, faceN) / denom;
                var onFace = rim + gap * capN - fc;
                if (MathF.Abs(Vector3.Dot(onFace, axes[j])) <= extents[j] * (1f + InsideTolerance) &&
                    MathF.Abs(Vector3.Dot(onFace, axes[k])) <= extents[k] * (1f + InsideTolerance))
                    Add(rim + gap * capN, gap, Feature(CylinderFeature.CapSample, (capIndex << 4) | sample));
            }

        if (candidates.Count == 0) return;
        ReduceManifold(candidates, n);
        outList.AddRange(candidates);
    }

    /// <summary>The generator line facing the box: clipped to an aligned box face, overlapped with a parallel box edge, or one point
    /// against a crossing edge. Every point rolls on the axis support.</summary>
    private static void CylinderSideOnBox(SimCollider cyl, SimCollider box, Vector3 n, float separation, float margin,
        List<ContactPoint> outList)
    {
        var (start, length) = Generator(cyl, -n);
        void Add(Vector3 p, float gap, int detail, Vector3 normal)
        {
            if (gap <= margin)
                outList.Add(CylinderContact(cyl, box, normal, p, gap, Feature(CylinderFeature.Side, detail), ContactSupport.Axis,
                    ContactSupport.Material));
        }

        ReadOnlySpan<Vector3> axes = [box.U0, box.U1, box.U2];
        ReadOnlySpan<float> extents = [box.Hx, box.Hy, box.Hz];
        var face = 0;
        var faceDot = -1f;
        for (var i = 0; i < 3; i++)
        {
            var d = MathF.Abs(Vector3.Dot(axes[i], n));
            if (d > faceDot) { faceDot = d; face = i; }
        }

        // Classify the finite support before emitting constraints. A normal near a face
        // normal can still belong to its edge. The face-facing generator's projection
        // decides whether the finite face exists under the cylinder; using the pair
        // normal here moves an edge support onto the clipping border and makes ulps
        // alternate a line with an arbitrary closest endpoint.
        var faceN = (Vector3.Dot(axes[face], n) >= 0f ? 1f : -1f) * axes[face];
        var fc = box.Position + extents[face] * faceN;
        var j = (face + 1) % 3;
        var k = (face + 2) % 3;
        var (faceStart, faceLength) = Generator(cyl, -faceN);
        var rel = faceStart - fc;
        float t0 = 0f, t1 = 1f;
        ClipSlab(Vector3.Dot(rel, axes[j]), Vector3.Dot(faceLength, axes[j]), extents[j], ref t0, ref t1);
        ClipSlab(Vector3.Dot(rel, axes[k]), Vector3.Dot(faceLength, axes[k]), extents[k], ref t0, ref t1);

        if (faceDot >= EdgeFaceAlignment && t0 <= t1)
        {
            var p0 = faceStart + t0 * faceLength;
            var g0 = Vector3.Dot(p0 - fc, faceN);
            Add(p0 - g0 * 0.5f * faceN, g0, 0, faceN);
            if (t1 - t0 > 1e-6f)
            {
                var p1 = faceStart + t1 * faceLength;
                var g1 = Vector3.Dot(p1 - fc, faceN);
                Add(p1 - g1 * 0.5f * faceN, g1, 1, faceN);
            }
            return;
        }

        var edge = SupportEdge(box, n);
        var edgeLength = edge.D.Length();
        if (edgeLength > 1e-9f && MathF.Abs(Vector3.Dot(edge.D, cyl.U1)) >= EdgeFaceAlignment * edgeLength)
        {
            // A box edge along the generator: their common stretch, at most two points.
            var a = cyl.U1;
            var tau0 = Vector3.Dot(edge.P - cyl.Position, a);
            var tau1 = Vector3.Dot(edge.P + edge.D - cyl.Position, a);
            var lo = MathF.Max(-cyl.Hy, MathF.Min(tau0, tau1));
            var hi = MathF.Min(cyl.Hy, MathF.Max(tau0, tau1));
            if (lo > hi) return;
            // Side emission rule (review F4): the edge's closest point to the axis (a skewed edge's interior distance minimum) and both
            // stretch ends, each with its own radial signed gap and radial normal, so an end is a speculative row before it arrives.
            var u = start + cyl.Hy * a - cyl.Position;
            var e0 = ClosestOnSegment(edge.P, edge.D, cyl.Position + lo * a + u);
            var e1 = ClosestOnSegment(edge.P, edge.D, cyl.Position + hi * a + u);
            var (_, nearest) = ClosestPoints(cyl.Position - cyl.Hy * a, 2f * cyl.Hy * a, edge.P, edge.D);
            RadialPoint(e0, 2);
            if ((e1 - e0).Length() > 1e-6f) RadialPoint(e1, 3);
            if ((nearest - e0).Length() > 1e-3f && (nearest - e1).Length() > 1e-3f) RadialPoint(nearest, 5);
            return;
        }

        var (pa, pb) = ClosestPoints(start, length, edge.P, edge.D);
        Add((pa + pb) * 0.5f, separation, 4, n);

        // A box point against the curved side: its radial signed gap and its own radial normal (from the box toward the cylinder).
        void RadialPoint(Vector3 q, int detail)
        {
            if (MathF.Abs(Vector3.Dot(q - cyl.Position, cyl.U1)) > cyl.Hy + 1e-4f) return;
            var gap = SideDistance(Solid.Of(cyl), q, out var onSide);
            if (gap > margin) return;
            var radial = RadialUnit(q - cyl.Position, cyl.U1, -n);
            outList.Add(CylinderContact(cyl, box, -radial, (q + onSide) * 0.5f, gap, Feature(CylinderFeature.Side, detail), ContactSupport.Axis,
                ContactSupport.Material));
        }
    }

    /// <summary>Two cylinders. A SAT over both axes, their cross product, the direction between the closest points of the two axis
    /// segments and the radial directions from each axis to the other's cap centres (exact support projections) proves separation
    /// early. The gap and normal come from the exact closest features (`CylinderCylinderClosest`); overlapping, the deepest feature or
    /// the SAT, whichever is shallower. Contacts: a rim (tilted) cylinder gives one point; cap on cap the two discs' common region (reduced to four); a
    /// cap on a side the generator's chord across the disc; side on side two points when parallel within 2° (each end's own distance
    /// to the other side), one otherwise. No point claims more depth than the pair.</summary>
    public static void CollideCylinderCylinder(SimCollider colA, SimCollider colB, float margin, List<ContactPoint> outList)
    {
        var pa = colA.Position;
        var pb = colB.Position;
        var dp = pa - pb;
        var aa = colA.U1;
        var ab = colB.U1;
        var best = float.NegativeInfinity;
        var bestAxis = Vector3.Zero;
        bool Axis(Vector3 l, bool face) =>
            SatAxis(l, CylinderProjection(colA, l), CylinderProjection(colB, l), dp, margin, face, ref best, ref bestAxis);
        bool Direction(Vector3 d)
        {
            var length = d.Length();
            return !(length > 1e-6f) || Axis(d / length, false);
        }

        if (!Axis(aa, true) || !Axis(ab, true)) return;
        if (!Direction(Vector3.Cross(aa, ab))) return;
        var axisA = (Start: pa - colA.Hy * aa, Length: 2f * colA.Hy * aa);
        var axisB = (Start: pb - colB.Hy * ab, Length: 2f * colB.Hy * ab);
        var (xa, xb) = ClosestPoints(axisA.Start, axisA.Length, axisB.Start, axisB.Length);
        if (!Direction(xa - xb)) return;
        for (var capIndex = 0; capIndex < 2; capIndex++)
        {
            var ccA = pa + (capIndex == 0 ? colA.Hy : -colA.Hy) * aa;
            var ccB = pb + (capIndex == 0 ? colB.Hy : -colB.Hy) * ab;
            if (!Direction(Radial(ccB - pa, aa)) || !Direction(Radial(ccA - pb, ab))) return;
        }

        if (float.IsNegativeInfinity(best)) return;
        // The SAT axes above only prove separation; the gap and normal come from the exact closest features.
        var closest = CylinderCylinderClosest(Solid.Of(colA), Solid.Of(colB));
        if (closest.Gap > margin) return;
        // Apart, the closest features are the exact gap; overlapping, the deepest closest feature or the SAT, whichever is shallower.
        // (No shrunk core here: two cores' rims give a late, non-monotone gap at touch — review N9.)
        var exact = closest.Gap > 0f || closest.Gap > best;
        var n = exact ? closest.Normal : bestAxis;
        var separation = exact ? closest.Gap : best;
        var tA = MathF.Abs(Vector3.Dot(n, aa));
        var tB = MathF.Abs(Vector3.Dot(n, ab));
        var supportA = tA <= SideAlignment ? ContactSupport.Axis : ContactSupport.Material;
        var supportB = tB <= SideAlignment ? ContactSupport.Axis : ContactSupport.Material;
        var rimRegimeA = tA > SideAlignment && tA < EdgeFaceAlignment;
        var rimRegimeB = tB > SideAlignment && tB < EdgeFaceAlignment;
        var first = outList.Count;
        void Add(List<ContactPoint> list, Vector3 p, float gap, int feature)
        {
            if (gap <= margin) list.Add(CylinderContact(colA, colB, n, p, gap, feature, supportA, supportB));
        }

        if (rimRegimeA || rimRegimeB)
        {
            var ra = CylinderSupport(colA, -n);
            var rb = CylinderSupport(colB, n);
            var p = exact ? (closest.OnA + closest.OnB) * 0.5f
                : rimRegimeA && rimRegimeB ? (ra + rb) * 0.5f : rimRegimeA ? ra - separation * 0.5f * n : rb + separation * 0.5f * n;
            var detail = (Vector3.Dot(ra - pa, aa) >= 0f ? 0 : 1) | (Vector3.Dot(rb - pb, ab) >= 0f ? 0 : 2);
            Add(outList, p, separation, Feature(CylinderFeature.Rim, detail));
        }
        else if (tA >= EdgeFaceAlignment && tB >= EdgeFaceAlignment) DiscOnDisc();
        else if (tA >= EdgeFaceAlignment) Chord(colA, colB, n, 0);
        else if (tB >= EdgeFaceAlignment) Chord(colB, colA, -n, 4);
        else if (MathF.Abs(Vector3.Dot(aa, ab)) >= LineAlignment)
        {
            // Parallel sides: the two generators' common stretch along A's axis.
            var (ga, _) = Generator(colA, -n);
            var (gb, gbLength) = Generator(colB, n);
            var u = ga + colA.Hy * aa - pa;
            var tau0 = Vector3.Dot(gb - pa, aa);
            var tau1 = Vector3.Dot(gb + gbLength - pa, aa);
            var lo = MathF.Max(-colA.Hy, MathF.Min(tau0, tau1));
            var hi = MathF.Min(colA.Hy, MathF.Max(tau0, tau1));
            if (lo <= hi)
            {
                // Each end's gap is its own distance to B's curved side, so the up to 2° of skew the line tolerance allows reads B's
                // surface curving away rather than a pairing along the pair normal (review F4).
                var s0 = pa + lo * aa + u;
                var g0 = SideDistance(Solid.Of(colB), s0, out var b0);
                Add(outList, (s0 + b0) * 0.5f, g0, Feature(CylinderFeature.Side, 0));
                if (hi - lo > 1e-6f)
                {
                    var s1 = pa + hi * aa + u;
                    var g1 = SideDistance(Solid.Of(colB), s1, out var b1);
                    Add(outList, (s1 + b1) * 0.5f, g1, Feature(CylinderFeature.Side, 1));
                }
            }
        }
        else
        {
            var (ga, gaLength) = Generator(colA, -n);
            var (gb, gbLength) = Generator(colB, n);
            var (sa, sb) = ClosestPoints(ga, gaLength, gb, gbLength);
            Add(outList, exact ? (closest.OnA + closest.OnB) * 0.5f : (sa + sb) * 0.5f, separation, Feature(CylinderFeature.Side, 2));
            // Side emission rule (review F4): each facing-generator end within the margin joins as its own row, measured by its radial
            // signed gap to the other side with its own radial normal, so a skewed side's far end is a speculative row before it lands.
            SideEnd(ga, colB, true, 16);
            SideEnd(ga + gaLength, colB, true, 17);
            SideEnd(gb, colA, false, 18);
            SideEnd(gb + gbLength, colA, false, 19);
            ReduceSideManifold(outList, first);
        }

        // The same actual closest witness is considered for empty and populated
        // emissions; its normal, support points and signed gap stay together.
        KeepClosest(colA, colB, outList, first, closest, separation,
            MathF.Abs(Vector3.Dot(closest.Normal, aa)) <= SideAlignment ? ContactSupport.Axis : ContactSupport.Material,
            MathF.Abs(Vector3.Dot(closest.Normal, ab)) <= SideAlignment ? ContactSupport.Axis : ContactSupport.Material);
        NoDeeperThan(outList, first, separation);

        // A generator end x of one cylinder against the other's curved side (within its half-width and the margin, and not the closest
        // point again): its radial signed gap, with the radial normal turned to point from B toward A.
        void SideEnd(Vector3 x, SimCollider other, bool onA, int detail)
        {
            if (MathF.Abs(Vector3.Dot(x - other.Position, other.U1)) > other.Hy) return;
            var gap = SideDistance(Solid.Of(other), x, out var onSide);
            if (gap > margin || gap <= separation + 1e-4f) return;
            var radial = RadialUnit(x - other.Position, other.U1, onA ? n : -n);
            outList.Add(CylinderContact(colA, colB, onA ? radial : -radial, (x + onSide) * 0.5f, gap, Feature(CylinderFeature.Side, detail),
                supportA, supportB));
        }

        // A's facing cap is the reference disc; B's facing cap is measured along A's cap normal.
        void DiscOnDisc()
        {
            var sA = Vector3.Dot(n, aa) > 0f ? -1f : 1f;
            var capNA = sA * aa;
            var ccA = pa + sA * colA.Hy * aa;
            var sB = Vector3.Dot(n, ab) >= 0f ? 1f : -1f;
            var capNB = sB * ab;
            var ccB = pb + sB * colB.Hy * ab;
            var denom = Vector3.Dot(capNA, capNB);
            var capBits = (sA > 0f ? 0 : 1) | (sB > 0f ? 0 : 2);
            var limitA = colA.Radius * (1f + InsideTolerance);
            var limitB = colB.Radius * (1f + InsideTolerance);
            var candidates = new List<ContactPoint>(20);
            // Eight fixed samples of each rim, and each rim's point deepest toward the other cap.
            for (var k = 0; k < 9; k++)
            {
                var rim = ccA + colA.Radius * (k < 8 ? RimDirection(colA, k) : RadialUnit(-capNB, aa, colA.U0));
                var gap = Vector3.Dot(ccB - rim, capNB) / denom;
                if (Radial(rim + gap * capNA - ccB, ab).LengthSquared() <= limitB * limitB)
                    Add(candidates, rim + gap * 0.5f * capNA, gap, Feature(CylinderFeature.CapSample, (capBits << 4) | k));
                var other = ccB + colB.Radius * (k < 8 ? RimDirection(colB, k) : RadialUnit(-capNA, ab, colB.U0));
                if (Radial(other - ccA, aa).LengthSquared() <= limitA * limitA)
                {
                    var otherGap = Vector3.Dot(other - ccA, capNA);
                    Add(candidates, other - otherGap * 0.5f * capNA, otherGap, Feature(CylinderFeature.PartnerSample, (capBits << 4) | k));
                }
            }
            // The two rims' crossings, in A's cap plane (B's rim projected as a circle within the cap tolerance).
            var cx = Vector3.Dot(ccB - ccA, colA.U0);
            var cy = Vector3.Dot(ccB - ccA, colA.U2);
            var d = MathF.Sqrt(cx * cx + cy * cy);
            if (d > 1e-6f && d < colA.Radius + colB.Radius && d > MathF.Abs(colA.Radius - colB.Radius))
            {
                var along = (d * d + colA.Radius * colA.Radius - colB.Radius * colB.Radius) / (2f * d);
                var across = MathF.Sqrt(MathF.Max(0f, colA.Radius * colA.Radius - along * along));
                var ex = cx / d;
                var ey = cy / d;
                for (var root = 0; root < 2; root++)
                {
                    var sign = root == 0 ? 1f : -1f;
                    var p = ccA + (along * ex - sign * across * ey) * colA.U0 + (along * ey + sign * across * ex) * colA.U2;
                    var gap = Vector3.Dot(ccB - p, capNB) / denom;
                    Add(candidates, p + gap * 0.5f * capNA, gap, Feature(CylinderFeature.RimCrossing, (capBits << 1) | root));
                }
            }
            if (candidates.Count == 0) return;
            ReduceManifold(candidates, n);
            outList.AddRange(candidates);
        }

        // The generator of the side cylinder facing the cap, clipped to the cap disc: at most two points.
        void Chord(SimCollider capCyl, SimCollider sideCyl, Vector3 towardCap, int detail)
        {
            var axis = capCyl.U1;
            var s = Vector3.Dot(towardCap, axis) > 0f ? -1f : 1f;
            var capN = s * axis;
            var cc = capCyl.Position + s * capCyl.Hy * axis;
            var (start, length) = Generator(sideCyl, towardCap);
            var rel = start - cc;
            var (t0, t1) = ClipCircle(Vector3.Dot(rel, capCyl.U0), Vector3.Dot(rel, capCyl.U2), Vector3.Dot(length, capCyl.U0),
                Vector3.Dot(length, capCyl.U2), capCyl.Radius * (1f + InsideTolerance));
            if (t0 > t1) return;
            var p0 = start + t0 * length;
            var g0 = Vector3.Dot(p0 - cc, capN);
            Add(outList, p0 - g0 * 0.5f * capN, g0, Feature(CylinderFeature.Side, detail | (s > 0f ? 0 : 2)));
            if (t1 - t0 > 1e-6f)
            {
                var p1 = start + t1 * length;
                var g1 = Vector3.Dot(p1 - cc, capN);
                Add(outList, p1 - g1 * 0.5f * capN, g1, Feature(CylinderFeature.Side, detail | 1 | (s > 0f ? 0 : 2)));
            }
        }
    }

    /// <summary>A sphere's speculative contact (positive gap, closing within two substeps h: the solve stops what this substep
    /// reaches, the relax pass what the next one would) takes its normal at the predicted touch instead of at the current closest
    /// point. Restitution resolves an impact in the substep that stops it, so a normal
    /// read from the gap would tilt the bounce off a curved feature (a rim, an edge, another sphere) by up to about v·h/R. The touch
    /// pose is found by conservative advancement of the sphere's centre along the pair's relative linear velocity (at most four
    /// probes of the same pair kernel); the contact keeps the distance still to close along the touch normal as its gap, and its
    /// point stays on the advanced sphere's centre line (a sphere's normal impulse has no torque). A pair of two moving spheres, or a
    /// probe that misses, separates or leaves the horizon, keeps the contact as generated.</summary>
    public static void AdvanceSpeculativeSphereContact(SimCollider colA, SimCollider colB, float margin, float h, List<ContactPoint> contacts,
        int first, SimCollider probe, List<ContactPoint> probeContacts)
    {
        if (contacts.Count - first != 1) return;
        static bool Moving(SimCollider c) => c.Shape == ShapeKind.Sphere && c.Body.Motion == RigidMotionKind.Dynamic;
        // Two moving spheres: no single contact point keeps both lever arms on the normal, so the pair keeps its contact.
        if (Moving(colA) && Moving(colB)) return;
        // Advance the moving sphere when there is one: the contact point is placed on its centre line, so its arm stays normal.
        var sphereIsA = Moving(colA) || (colA.Shape == ShapeKind.Sphere && !Moving(colB));
        if (!sphereIsA && colB.Shape != ShapeKind.Sphere) return;
        var sphere = sphereIsA ? colA : colB;
        var other = sphereIsA ? colB : colA;
        if (other.Shape == ShapeKind.Plane) return;
        var contact = contacts[first];
        if (!(contact.Gap > 0f)) return;

        // A contact's normal points from its B toward its A (the pair kernels choose the order); read it toward the sphere.
        var sign = contact.ColA == sphere ? 1f : -1f;
        var velocity = (sphere.Body.Motion == RigidMotionKind.Dynamic ? sphere.Body.LinearVelocity : Vector3.Zero) -
            (other.Body.Motion == RigidMotionKind.Dynamic ? other.Body.LinearVelocity : Vector3.Zero);
        var normal = sign * contact.Normal;
        var closing = -Vector3.Dot(velocity, normal);
        if (!(closing > 0f)) return;
        var t = contact.Gap / closing;
        if (!(t <= 2f * h)) return;

        probe.Index = sphere.Index; probe.Id = sphere.Id; probe.BodySlot = sphere.BodySlot; probe.Body = sphere.Body;
        probe.Shape = ShapeKind.Sphere; probe.Radius = sphere.Radius;
        probe.Qx = sphere.Qx; probe.Qy = sphere.Qy; probe.Qz = sphere.Qz; probe.Qw = sphere.Qw;
        probe.U0 = sphere.U0; probe.U1 = sphere.U1; probe.U2 = sphere.U2;
        var centre = sphere.Position;
        float gap = 0f;
        var feature = contact.Feature;
        for (var probeIndex = 0; probeIndex < 4; probeIndex++)
        {
            var at = centre + velocity * t;
            probe.Px = at.X; probe.Py = at.Y; probe.Pz = at.Z;
            probeContacts.Clear();
            GenerateManifold(sphereIsA ? probe : colA, sphereIsA ? colB : probe, margin, probeContacts);
            if (probeContacts.Count != 1) return;
            var touch = probeContacts[0];
            normal = (touch.ColA == probe ? 1f : -1f) * touch.Normal;
            closing = -Vector3.Dot(velocity, normal);
            if (!(closing > 0f)) return;
            // Still to close along this normal from the current pose: what the advance closed plus the probe's own gap.
            gap = closing * t + touch.Gap;
            feature = touch.Feature;
            if (MathF.Abs(touch.Gap) <= 1e-6f) break;
            t += touch.Gap / closing;
            if (!(t >= 0f && t <= 2f * h)) return;
        }
        if (!(gap > 0f)) return;

        var n = sign * normal;
        var point = centre - normal * (sphere.Radius + gap * .5f);
        contact.Nx = n.X; contact.Ny = n.Y; contact.Nz = n.Z;
        contact.Px = point.X; contact.Py = point.Y; contact.Pz = point.Z;
        contact.Gap = gap;
        contact.Feature = feature;
    }

    /// <summary>The narrowphase is total over the admitted shapes (ingest rejects any other). Planes and hollow colliders are static,
    /// so neither meets a plane or another hollow collider in a candidate pair. One rule per pair: a hollow collider meets a sphere,
    /// box or cylinder through its exact signed distance (engine-hollow-shapes).</summary>
    public static void GenerateManifold(SimCollider colA, SimCollider colB, float margin, List<ContactPoint> outList)
    {
        if (IsHollow(colA.Shape) || IsHollow(colB.Shape))
        {
            var hollowIsA = IsHollow(colA.Shape);
            var hollow = hollowIsA ? colA : colB;
            var partner = hollowIsA ? colB : colA;
            CollideHollow(partner, hollow, margin, outList);
            return;
        }
        switch (colA.Shape, colB.Shape)
        {
            case (ShapeKind.Sphere, ShapeKind.Sphere): CollideSphereSphere(colA, colB, margin, outList); break;
            case (ShapeKind.Sphere, ShapeKind.Plane): CollideSpherePlane(colA, colB, margin, outList); break;
            case (ShapeKind.Plane, ShapeKind.Sphere): CollideSpherePlane(colB, colA, margin, outList); break;
            case (ShapeKind.Box, ShapeKind.Sphere): CollideBoxSphere(colA, colB, margin, outList); break;
            case (ShapeKind.Sphere, ShapeKind.Box): CollideBoxSphere(colB, colA, margin, outList); break;
            case (ShapeKind.Box, ShapeKind.Plane): CollideBoxPlane(colA, colB, margin, outList); break;
            case (ShapeKind.Plane, ShapeKind.Box): CollideBoxPlane(colB, colA, margin, outList); break;
            case (ShapeKind.Box, ShapeKind.Box): CollideBoxBox(colA, colB, margin, outList); break;
            case (ShapeKind.Cylinder, ShapeKind.Plane): CollideCylinderPlane(colA, colB, margin, outList); break;
            case (ShapeKind.Plane, ShapeKind.Cylinder): CollideCylinderPlane(colB, colA, margin, outList); break;
            case (ShapeKind.Cylinder, ShapeKind.Sphere): CollideCylinderSphere(colA, colB, margin, outList); break;
            case (ShapeKind.Sphere, ShapeKind.Cylinder): CollideCylinderSphere(colB, colA, margin, outList); break;
            case (ShapeKind.Cylinder, ShapeKind.Box): CollideCylinderBox(colA, colB, margin, outList); break;
            case (ShapeKind.Box, ShapeKind.Cylinder): CollideCylinderBox(colB, colA, margin, outList); break;
            case (ShapeKind.Cylinder, ShapeKind.Cylinder): CollideCylinderCylinder(colA, colB, margin, outList); break;
            case (ShapeKind.Plane, ShapeKind.Plane): break;
        }
    }
}
