using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

// Local-space, finite cone emitter; intensity is a game unit, not a calibrated lumen value.
public readonly record struct LightEmitter(Vector3 At, Vector3 Direction, float Range, float ConeCosine, float Intensity);
public readonly record struct LightSample(Vector3 At, Vector3 Normal, float Weight);

/// <summary>One snapshot per fixed tick, all receiver outputs committed together.
/// Opaque collision proxies share the physical world. No reflection, refraction or ambient-sky power.</summary>
public static class LightNetwork
{
    private const float Epsilon = .0001f;

    public static void Solve(MachineWorld world)
    {
        var emitters = world.Parts.Where(p => p.Visible && p.LightSource.HasValue)
            .OrderBy(p => p.Uid, StringComparer.Ordinal)
            .Select(p => (Part: p, Source: p.LightSource!.Value, Transform: p.Transform)).ToArray();
        var readings = new Dictionary<MachinePart, float>();
        foreach (var receiver in world.Parts)
        {
            var total = 0f;
            if (receiver.Visible)
            foreach (var sample in receiver.LightSamples)
            foreach (var (part, source, transform) in emitters)
            {
                if (part == receiver) continue;
                var origin = transform * source.At;
                var end = receiver.Transform * sample.At;
                var offset = end - origin;
                var distance = offset.Length();
                if (distance < Epsilon || distance > source.Range) continue;
                var direction = offset / distance;
                if ((transform.Basis * source.Direction).Normalized().Dot(direction) < source.ConeCosine) continue;
                var facing = Mathf.Max(0, (receiver.Basis * sample.Normal).Normalized().Dot(-direction));
                if (facing == 0 || Trace(world, origin, direction, distance, part, receiver) < distance - Epsilon) continue;
                total += source.Intensity * facing * sample.Weight / Mathf.Max(1, distance * distance);
            }
            readings.Add(receiver, total);
        }
        foreach (var (receiver, total) in readings) receiver.ReceiveLight(total);
    }

    public static float Trace(MachineWorld world, Vector3 origin, Vector3 direction, float range,
        MachinePart emitter, MachinePart? receiver = null)
    {
        var closest = range;
        foreach (var part in world.Parts)
        {
            if (!part.Visible || part == emitter || part == receiver) continue;
            var inverse = part.Transform.AffineInverse();
            var local = inverse * origin;
            var ray = inverse.Basis * direction;
            foreach (var box in part.Boxes)
                closest = BoxDistance(local - box.At, ray, box.Half, closest);
            foreach (var sphere in part.Spheres)
                closest = SphereDistance(local - sphere.At, ray, sphere.Radius, closest);
            if (part.Dynamic) closest = SphereDistance(local, ray, part.Radius, closest);
        }
        closest = BoxDistance(origin - Workbench.Deck.At, direction, Workbench.Deck.Half, closest);
        return BoxDistance(origin - Workbench.Base.At, direction, Workbench.Base.Half, closest);
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
