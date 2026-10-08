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
        var emitters = world.Parts.Where(p => p.LightSource.HasValue)
            .OrderBy(p => p.Uid, StringComparer.Ordinal)
            .Select(p => (Part: p, Source: p.LightSource!.Value,
                State: WorldGeometry.CaptureSpatialState(world,new(p,MachinePart.RootBody))))
            .Where(p=>p.State.Enabled).Select(p=>(p.Part,p.Source,Transform:p.State.Pose.ToScene())).ToArray();
        var readings = new Dictionary<MachinePart, float>();
        foreach (var receiver in world.Parts)
        {
            var total = 0f;
            var samples=receiver.LightSamples.ToArray();
            if(samples.Length==0) { readings.Add(receiver,0); continue; }
            var state=WorldGeometry.CaptureSpatialState(world,new(receiver,MachinePart.RootBody));
            var transformReceiver=state.Pose.ToScene();
            if (state.Enabled)
            foreach (var sample in samples)
            foreach (var (part, source, transform) in emitters)
            {
                if (part == receiver) continue;
                var origin = transform * source.At;
                var end = transformReceiver * sample.At;
                var offset = end - origin;
                var distance = offset.Length();
                if (distance < Epsilon || distance > source.Range) continue;
                var direction = offset / distance;
                if ((transform.Basis * source.Direction).Normalized().Dot(direction) < source.ConeCosine) continue;
                var facing = Mathf.Max(0, (transformReceiver.Basis * sample.Normal).Normalized().Dot(-direction));
                if (facing == 0 || WorldGeometry.Trace(TraceMedium.Light, world, origin, direction, distance, part, receiver) < distance - Epsilon) continue;
                total += source.Intensity * facing * sample.Weight / Mathf.Max(1, distance * distance);
            }
            readings.Add(receiver, total);
        }
        foreach (var (receiver, total) in readings) receiver.ReceiveLight(total);
    }

}
