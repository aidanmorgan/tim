using Godot;
using System;
using System.Linq;

namespace CuriousContraptions;

public enum TraceMedium { Light, Sound, Air }
public enum SweepBodyMode { IncludeBodies, ExcludeBodies }
public enum SweepSurfaceKind { None, Box, Sphere, Tube, Bend, Frustum, Body }
public enum SweepObstacleKind { None, Part, Workbench }
public readonly record struct WorldSweepResult(SphereSweepStatus Status, float Distance, Vector3 Normal,
    SweepObstacleKind Kind, MachinePart? Part, SweepSurfaceKind Surface, float Penetration);

/// <summary>Direct-path geometry query. Air cannot pass through transparent solid proxies.</summary>
public static class WorldGeometry
{
    private const float Epsilon=.0001f;

    /// <summary>Nearest finite-radius contact against all visible solid proxies and the workbench.
    /// Equal-distance contacts use workbench first, then ordinal part ID and proxy declaration order.
    /// Initial overlaps take priority over touching contacts. Both ignored parts are excluded entirely.
    /// ExcludeBodies omits dynamic sphere bodies only; their declared solid proxies remain queryable.
    /// Penetration is the nonnegative separation depth along the returned world-space normal.</summary>
    public static WorldSweepResult Sweep(MachineWorld world, Vector3 origin, float radius,
        Vector3 displacement, MachinePart? ignoredOwner = null, MachinePart? ignoredBody = null,
        SweepBodyMode bodies = SweepBodyMode.IncludeBodies)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!Enum.IsDefined(bodies)) throw new ArgumentOutOfRangeException(nameof(bodies));
        var best = new WorldSweepResult(SphereSweepStatus.Clear, displacement.Length(), Vector3.Zero,
            SweepObstacleKind.None, null, SweepSurfaceKind.None, 0);
        void Candidate(Transform3D pose, Func<Vector3,(Vector3 Normal,float Distance)> surface,
            SweepObstacleKind kind, MachinePart? part, SweepSurfaceKind shape)
        {
            var basis = pose.Basis;
            if (!pose.Origin.IsFinite() || !basis.X.IsFinite() || !basis.Y.IsFinite() || !basis.Z.IsFinite()
                || Mathf.Abs(basis.X.LengthSquared()-1) > .0001f
                || Mathf.Abs(basis.Y.LengthSquared()-1) > .0001f
                || Mathf.Abs(basis.Z.LengthSquared()-1) > .0001f
                || Mathf.Abs(basis.X.Dot(basis.Y)) > .0001f
                || Mathf.Abs(basis.X.Dot(basis.Z)) > .0001f
                || Mathf.Abs(basis.Y.Dot(basis.Z)) > .0001f)
                throw new InvalidOperationException("Sphere sweeps require rigid proxy transforms.");
            var inverse = pose.AffineInverse();
            var hit = SphereSweep.Cast(inverse*origin, radius, inverse.Basis*displacement, surface);
            if (hit.Status == SphereSweepStatus.Clear) return;
            if (best.Status == SphereSweepStatus.Overlapping) return;
            if (hit.Status != SphereSweepStatus.Overlapping && best.Status != SphereSweepStatus.Clear
                && hit.Distance >= best.Distance) return;
            best = new(hit.Status, hit.Distance, basis*hit.Normal, kind, part, shape, hit.Penetration);
        }
        // Always query the workbench, including in an empty world; this also validates query inputs.
        foreach (var box in new[] { Workbench.Deck, Workbench.Base })
            Candidate(Transform3D.Identity, p=>SphereSweep.BoxSurface(p-box.At,box.Half),
                SweepObstacleKind.Workbench,null,SweepSurfaceKind.Box);
        foreach (var part in world.Parts.Where(p=>p.Visible && p!=ignoredOwner && p!=ignoredBody)
                     .OrderBy(p=>p.Uid,StringComparer.Ordinal))
        {
            foreach (var box in part.Boxes)
                Candidate(part.Transform,p=>SphereSweep.BoxSurface(p-box.At,box.Half),SweepObstacleKind.Part,part,SweepSurfaceKind.Box);
            foreach (var sphere in part.Spheres)
                Candidate(part.Transform,p=>SphereSweep.SphereSurface(p-sphere.At,sphere.Radius),SweepObstacleKind.Part,part,SweepSurfaceKind.Sphere);
            foreach (var tube in part.Tubes)
                Candidate(part.Transform*tube.Pose,tube.Surface,SweepObstacleKind.Part,part,SweepSurfaceKind.Tube);
            foreach (var bend in part.Bends)
                Candidate(part.Transform*bend.Pose,bend.Surface,SweepObstacleKind.Part,part,SweepSurfaceKind.Bend);
            foreach (var frustum in part.Frustums)
                Candidate(part.Transform*frustum.Pose,frustum.Surface,SweepObstacleKind.Part,part,SweepSurfaceKind.Frustum);
            if (part.Dynamic && bodies == SweepBodyMode.IncludeBodies)
                Candidate(part.Transform,p=>SphereSweep.SphereSurface(p,part.Radius),SweepObstacleKind.Part,part,SweepSurfaceKind.Body);
        }
        return best;
    }
    public static float Trace(TraceMedium medium, MachineWorld world, Vector3 origin, Vector3 direction, float range,
        MachinePart? emitter, MachinePart? receiver = null)
    {
        if(!Enum.IsDefined(medium))throw new ArgumentOutOfRangeException(nameof(medium));
        var closest = range;
        foreach (var part in world.Parts)
        {
            if (!part.Visible || part == emitter || part == receiver) continue;
            var inverse = part.Transform.AffineInverse();
            var local = inverse * origin;
            var ray = inverse.Basis * direction;
            foreach (var box in part.Boxes.Where(b => medium==TraceMedium.Air || b.Opaque))
                closest = BoxDistance(local - box.At, ray, box.Half, closest);
            foreach (var sphere in part.Spheres)
                closest = SphereDistance(local - sphere.At, ray, sphere.Radius, closest);
            foreach (var tube in part.Tubes.Where(t => medium==TraceMedium.Air || t.Opaque))
            {
                var tubeInverse = tube.Pose.AffineInverse();
                closest = tube.RayDistance(tubeInverse * local, tubeInverse.Basis * ray, closest);
            }
            if(medium==TraceMedium.Air)
            {
                foreach(var bend in part.Bends)
                {
                    var pose=bend.Pose.AffineInverse();
                    closest=SurfaceDistance(pose*local,pose.Basis*ray,closest,p=>bend.Surface(p).Distance);
                }
                foreach(var frustum in part.Frustums)
                {
                    var pose=frustum.Pose.AffineInverse();
                    closest=SurfaceDistance(pose*local,pose.Basis*ray,closest,p=>frustum.Surface(p).Distance);
                }
            }
            if (part.Dynamic) closest = SphereDistance(local, ray, part.Radius, closest);
        }
        closest = BoxDistance(origin - Workbench.Deck.At, direction, Workbench.Deck.Half, closest);
        return BoxDistance(origin - Workbench.Base.At, direction, Workbench.Base.Half, closest);
    }

    private static float SurfaceDistance(Vector3 origin,Vector3 ray,float range,Func<Vector3,float> surface)
    {
        // Signed distance to the actual curved shell, not an opaque box around its bore.
        // Advance never skips a nearer surface; epsilon makes tangencies terminate.
        var speed=ray.Length();
        if(speed<1e-8f)throw new ArgumentException("Geometry ray direction cannot be zero.");
        for(var distance=0f;distance<range;)
        {
            var gap=surface(origin+ray*distance);
            if(gap<=Epsilon)return distance;
            distance+=gap/speed;
        }
        return range;
    }
    private static float BoxDistance(Vector3 origin, Vector3 ray, Vector3 half, float maximum)
    {
        var near = 0f;
        var far = maximum;
        for (var axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(ray[axis]) < Epsilon)
            {
                if (Mathf.Abs(origin[axis]) > half[axis]) return maximum;
                continue;
            }
            var a = (-half[axis] - origin[axis]) / ray[axis];
            var b = (half[axis] - origin[axis]) / ray[axis];
            near = Mathf.Max(near, Mathf.Min(a, b));
            far = Mathf.Min(far, Mathf.Max(a, b));
            if (near > far) return maximum;
        }
        return near;
    }

    private static float SphereDistance(Vector3 origin, Vector3 ray, float radius, float maximum)
    {
        var c = origin.LengthSquared() - radius * radius;
        if (c <= 0) return 0;
        var b = origin.Dot(ray);
        var discriminant = b * b - ray.LengthSquared() * c;
        if (discriminant < 0) return maximum;
        var near = (-b - Mathf.Sqrt(discriminant)) / ray.LengthSquared();
        return near >= 0 ? Mathf.Min(near, maximum) : maximum;
    }
}
