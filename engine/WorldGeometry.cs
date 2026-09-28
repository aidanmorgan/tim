using Godot;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CuriousContraptions.Physics;

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
    private static readonly ConditionalWeakTable<MachinePart,SceneTraceGeometry> TraceGeometry=new();
    private static readonly CompoundGeometry RayPoint=new([new(new ConvexHull([default]),Transform3D.Identity)]);
    private static readonly CompoundGeometry TraceWorkbench=new(
        new[]{Workbench.Deck,Workbench.Base}.Select(box=>new ConvexInstance(new ConvexBox(CollisionVector.From(box.Half)),
            new(Basis.Identity,box.At))).ToArray());

    /// <summary>Nearest finite-radius contact against all visible solid proxies and the workbench.
    /// Equal-distance contacts use workbench first, then ordinal part ID and proxy declaration order.
    /// Initial overlaps take priority over touching contacts. Both ignored parts are excluded entirely.
    /// ExcludeBodies omits dynamic spheres and hinged boxes; other declared solid proxies remain queryable.
    /// Penetration is the nonnegative separation depth along the returned world-space normal.</summary>
    public static WorldSweepResult Sweep(MachineWorld world, Vector3 origin, float radius,
        Vector3 displacement, MachinePart? ignoredOwner = null, MachinePart? ignoredBody = null,
        SweepBodyMode bodies = SweepBodyMode.IncludeBodies)
    {
        return CaptureSweep(world, ignoredOwner, ignoredBody, bodies).Sweep(origin, radius, displacement);
    }

    /// <summary>Immutable solid geometry for repeated queries at one simulation instant.
    /// Re-capture after movement, positional repair, visibility/proxy changes or contact callbacks.
    /// Changing body velocity alone does not invalidate captured solid geometry.</summary>
    public static SweepSnapshot CaptureSweep(MachineWorld world, MachinePart? ignoredOwner,
        MachinePart? ignoredBody, SweepBodyMode bodies)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!Enum.IsDefined(bodies)) throw new ArgumentOutOfRangeException(nameof(bodies));
        var snapshot = new SweepSnapshot();
        foreach (var box in new[] { Workbench.Deck, Workbench.Base })
            snapshot.Add(Transform3D.Identity, p=>SphereSweep.BoxSurface(p-box.At,box.Half),
                SweepObstacleKind.Workbench,null,SweepSurfaceKind.Box);
        foreach (var part in world.CollisionParts.Where(p=>p.Visible && p.PhysicsOwner.Visible && p!=ignoredOwner && p.PhysicsOwner!=ignoredOwner && p!=ignoredBody)
                     .OrderBy(p=>p.Uid,StringComparer.Ordinal))
        {
            var pose = part.Transform;
            foreach (var box in part.Boxes)
                snapshot.Add(pose,p=>SphereSweep.BoxSurface(p-box.At,box.Half),SweepObstacleKind.Part,part,SweepSurfaceKind.Box);
            if (bodies == SweepBodyMode.IncludeBodies)
                foreach (var hinge in part.HingedBodies)
                    snapshot.Add(hinge.Pose, p=>SphereSweep.BoxSurface(p,hinge.Half),
                        SweepObstacleKind.Part,part,SweepSurfaceKind.Box);
            foreach (var sphere in part.Spheres)
                snapshot.Add(pose,p=>SphereSweep.SphereSurface(p-sphere.At,sphere.Radius),SweepObstacleKind.Part,part,SweepSurfaceKind.Sphere);
            foreach (var tube in part.Tubes)
                snapshot.Add(pose*tube.Pose,tube.Surface,SweepObstacleKind.Part,part,SweepSurfaceKind.Tube);
            foreach (var bend in part.Bends)
                snapshot.Add(pose*bend.Pose,bend.Surface,SweepObstacleKind.Part,part,SweepSurfaceKind.Bend);
            foreach (var frustum in part.Frustums)
                snapshot.Add(pose*frustum.Pose,frustum.Surface,SweepObstacleKind.Part,part,SweepSurfaceKind.Frustum);
            if (part.Dynamic && bodies == SweepBodyMode.IncludeBodies)
            {
                var radius = part.Radius;
                snapshot.Add(pose,p=>SphereSweep.SphereSurface(p,radius),SweepObstacleKind.Part,part,SweepSurfaceKind.Body);
            }
        }
        return snapshot;
    }

    public sealed class SweepSnapshot
    {
        internal SweepSnapshot() { }
        private readonly System.Collections.Generic.List<PreparedSurface> _surfaces = new();
        private readonly record struct PreparedSurface(Transform3D Inverse, Basis Basis,
            Func<Vector3,(Vector3 Normal,float Distance)> Surface, SweepObstacleKind Kind,
            MachinePart? Part, SweepSurfaceKind Shape);

        internal void Add(Transform3D pose, Func<Vector3,(Vector3 Normal,float Distance)> surface,
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
            _surfaces.Add(new(pose.AffineInverse(), basis, surface, kind, part, shape));
        }

        public WorldSweepResult Sweep(Vector3 origin, float radius, Vector3 displacement)
        {
            var best = new WorldSweepResult(SphereSweepStatus.Clear, displacement.Length(), Vector3.Zero,
                SweepObstacleKind.None, null, SweepSurfaceKind.None, 0);
            foreach (var surface in _surfaces)
            {
                var hit = SphereSweep.Cast(surface.Inverse*origin, radius,
                    surface.Inverse.Basis*displacement, surface.Surface);
                if (hit.Status == SphereSweepStatus.Clear || best.Status == SphereSweepStatus.Overlapping) continue;
                if (hit.Status != SphereSweepStatus.Overlapping && best.Status != SphereSweepStatus.Clear
                    && hit.Distance >= best.Distance) continue;
                best = new(hit.Status, hit.Distance, surface.Basis*hit.Normal, surface.Kind,
                    surface.Part, surface.Shape, hit.Penetration);
            }
            return best;
        }
    }
    public static float Trace(TraceMedium medium, MachineWorld world, Vector3 origin, Vector3 direction, float range,
        MachinePart? emitter, MachinePart? receiver = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(!Enum.IsDefined(medium)) throw new ArgumentOutOfRangeException(nameof(medium));
        if(!origin.IsFinite()||!direction.IsFinite()||direction.LengthSquared()==0||!float.IsFinite(range)||range<0)
            throw new ArgumentException("A trace requires finite origin, nonzero direction and nonnegative range.");
        var rayBody=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(CollisionVector.From(origin)),
            CollisionVector.From(direction),default);
        var ray=new CompoundMotion(RayPoint,rayBody.CreateTrajectory(range));
        var stationary=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default).CreateTrajectory(range);
        double closest=range;
        void Test(CompoundGeometry geometry)
        {
            var result=CompoundCollision.Cast(ray,new(geometry,stationary),closest,0);
            if(result.Status!=ConvexSweepStatus.Clear) closest=result.Time;
        }
        foreach(var part in world.CollisionParts)
        {
            if(!part.Visible||!part.PhysicsOwner.Visible||part==emitter||part.PhysicsOwner==emitter||
                part==receiver||part.PhysicsOwner==receiver) continue;
            if(!TraceGeometry.TryGetValue(part,out var geometry)||!geometry.Matches(part))
            {
                geometry=new(part);
                TraceGeometry.Remove(part); TraceGeometry.Add(part,geometry);
            }
            var collider=geometry.For(medium);
            if(collider is not null) Test(collider);
        }
        Test(TraceWorkbench);
        return (float)closest;
    }
}
