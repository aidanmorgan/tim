using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Simulation;

internal enum ShapeKind : uint
{
    Sphere = 0,
    Box = 1,
    Plane = 2
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
    public ushort DragBits;
    public Vector3 ComLocal;
    public Quaternion Principal;
    public Vector3 InvI;
    public float[] InvIWorld = new float[9];
    public float Cmx, Cmy, Cmz;
    public float SupportExtent;
    public int DynamicIndex;
    public uint Motion;

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

    public float RAx, RAy, RAz;
    public float RBx, RBy, RBz;
    public Vector3 T1;
    public Vector3 T2;
    public float NormalMass;
    public float TangentMass1;
    public float TangentMass2;
    public float Restitution;
    public float Threshold;
    public float Friction;
    public ulong Key;
    public float LambdaN;
    public float LambdaT1;
    public float LambdaT2;
    public float RelVel;
    public float MaxImpulse;
    public bool Bounced;

    public Vector3 Normal => new(Nx, Ny, Nz);
    public Vector3 Point => new(Px, Py, Pz);
}

internal sealed class ContactManifold
{
    public List<ContactPoint> Points = new();
    public float Rolling;
    public float RollingMass1;
    public float RollingMass2;
    public float LambdaR1;
    public float LambdaR2;
}

internal static class WorkshopCollisionKernels
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
        if (b.Motion != 1) return;
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
        if (b.Motion != 1) return Vector3.Zero;
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

    public static float ColliderExtent(SimCollider c)
    {
        if (c.Shape == ShapeKind.Sphere) return c.Radius;
        if (c.Shape == ShapeKind.Box) return MathF.Sqrt(c.Hx * c.Hx + c.Hy * c.Hy + c.Hz * c.Hz);
        return 0f;
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

    public static void GenerateManifold(SimCollider colA, SimCollider colB, float margin, List<ContactPoint> outList)
    {
        var a = colA.Shape;
        var b = colB.Shape;
        if (a == ShapeKind.Sphere && b == ShapeKind.Sphere) CollideSphereSphere(colA, colB, margin, outList);
        else if (a == ShapeKind.Sphere && b == ShapeKind.Plane) CollideSpherePlane(colA, colB, margin, outList);
        else if (a == ShapeKind.Plane && b == ShapeKind.Sphere) CollideSpherePlane(colB, colA, margin, outList);
        else if (a == ShapeKind.Box && b == ShapeKind.Sphere) CollideBoxSphere(colA, colB, margin, outList);
        else if (a == ShapeKind.Sphere && b == ShapeKind.Box) CollideBoxSphere(colB, colA, margin, outList);
        else if (a == ShapeKind.Box && b == ShapeKind.Plane) CollideBoxPlane(colA, colB, margin, outList);
        else if (a == ShapeKind.Plane && b == ShapeKind.Box) CollideBoxPlane(colB, colA, margin, outList);
        else if (a == ShapeKind.Box && b == ShapeKind.Box) CollideBoxBox(colA, colB, margin, outList);
    }
}
