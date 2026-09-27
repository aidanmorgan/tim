using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class MachineWorld
{
    private enum FlightContactKind { None, Surface, Body }
    // Only overlapping bodies need positional repair; touching bodies must not
    // gain an artificial outward displacement on every gravity-driven contact.
    private const float FlightSeparation = .00001f;
    private const int MaximumFlightContacts = 4096;
    private readonly Dictionary<(MachinePart Body, MachinePart? Surface), float> _surfaceDampingBudgets = new();

    // Shared by swept impacts and rope-projection contacts. Geometry iterations
    // must not multiply damping, even when the same surface is revisited.
    private void ApplySurfaceFriction(MachinePart body, MachinePart? obstacle, Vector3 normal, float incomingNormalSpeed)
    {
        var key = (body, obstacle);
        var tangent = body.Velocity - normal * body.Velocity.Dot(normal);
        if (!_surfaceDampingBudgets.TryGetValue(key, out var budget))
            budget = tangent.Length() * (Realistic ? .015f : .002f);
        var normalChange = body.Velocity.Dot(normal) - incomingNormalSpeed;
        var friction = SurfaceFriction.Apply(body.Velocity, normal, normalChange, budget, Realistic ? .6f : .3f);
        body.Velocity = friction.Velocity;
        _surfaceDampingBudgets[key] = Mathf.Max(0, budget - friction.SpeedLoss);
    }

    /// <summary>Advance all bodies on one shared clock, resolving the earliest impact before
    /// consuming the remaining substep. Forces are sampled by Step, not applied again per impact.</summary>
    private void AdvanceBodies(float duration)
    {
        var bodies = Bodies.OrderBy(body => body.Uid, StringComparer.Ordinal).ToArray();
        double remaining = duration;
        var contacts = 0;
        _surfaceDampingBudgets.Clear();
        while (remaining > 0)
        {
            var interval = (float)remaining;
            var time = interval;
            var kind = FlightContactKind.None;
            MachinePart? first = null, second = null;
            WorldSweepResult surface = default;
            MovingSphereHit pair = default;
            foreach (var body in bodies)
            {
                if (!body.Visible) continue;
                var hit = WorldGeometry.Sweep(this, body.Position, body.Radius,
                    body.Velocity * interval, body, bodies: SweepBodyMode.ExcludeBodies);
                if (hit.Status == SphereSweepStatus.Clear) continue;
                var speed = body.Velocity.Length();
                var candidate = speed > 0 ? Math.Min(interval, hit.Distance / speed) : 0;
                if (kind != FlightContactKind.None && candidate >= time) continue;
                time = candidate;
                first = body;
                surface = hit;
                kind = FlightContactKind.Surface;
            }
            for (var i = 0; i < bodies.Length; i++)
            for (var j = i + 1; j < bodies.Length; j++)
            {
                var a = bodies[i]; var b = bodies[j];
                if (!a.Visible || !b.Visible) continue;
                var hit = MovingSphereSweep.Cast(a.Position, a.Radius, a.Velocity,
                    b.Position, b.Radius, b.Velocity, interval);
                if (hit.Status == SphereSweepStatus.Clear ||
                    (kind != FlightContactKind.None && hit.Time >= time)) continue;
                time = hit.Time;
                first = a; second = b;
                pair = hit;
                kind = FlightContactKind.Body;
            }
            if (++contacts > MaximumFlightContacts)
                throw new InvalidOperationException("Flight contacts did not converge; remaining time was not discarded.");
            foreach (var body in bodies)
                if (body.Visible) body.Position += body.Velocity * time;
            remaining -= time;
            if (kind == FlightContactKind.None) return;
            var target = first!;
            switch (kind)
            {
                case FlightContactKind.Surface:
                    if (surface.Penetration > 0)
                        target.Position += surface.Normal * (surface.Penetration + FlightSeparation);
                    var speed = target.Velocity.Dot(surface.Normal);
                    if (speed >= 0) break;
                    var bounce = surface.Kind switch
                    {
                        SweepObstacleKind.Workbench => 1,
                        SweepObstacleKind.Part => surface.Part!.SurfaceBounce,
                        _ => throw new InvalidOperationException("Surface contact has no obstacle.")
                    };
                    target.Velocity -= surface.Normal * speed * (1 + target.Bounce * bounce);
                    if (surface.Surface == SweepSurfaceKind.Box && Math.Abs(speed) < .45f && surface.Normal.Y > .7f)
                        target.Velocity -= surface.Normal * target.Velocity.Dot(surface.Normal);
                    ApplySurfaceFriction(target, surface.Part, surface.Normal, speed);
                    surface.Part?.OnContact(target, -speed, this);
                    break;
                case FlightContactKind.Body:
                    var other = second!;
                    var total = target.Mass + other.Mass;
                    var separation = pair.Normal * (pair.Penetration > 0 ? pair.Penetration + FlightSeparation : 0);
                    target.Position += separation * (other.Mass / total);
                    other.Position -= separation * (target.Mass / total);
                    var impact = SphereImpact.Resolve(target.Velocity, target.Mass,
                        other.Velocity, other.Mass, pair.Normal, Mathf.Min(target.Bounce, other.Bounce));
                    target.Velocity = impact.FirstVelocity;
                    other.Velocity = impact.SecondVelocity;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported flight contact.");
            }
        }
    }
}
