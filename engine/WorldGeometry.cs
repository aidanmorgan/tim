using Godot;
using System;
using System.Linq;

namespace CuriousContraptions;

public enum TraceMedium { Light, Sound, Air }

/// <summary>Direct-path geometry query. Air cannot pass through transparent solid proxies.</summary>
public static class WorldGeometry
{
    private const float Epsilon=.0001f;
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
