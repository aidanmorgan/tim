using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class MachineWorld
{
    private enum FlightContactKind { None, Surface, Body, MotionLimit }
    // Only overlapping bodies need positional repair; touching bodies must not
    // gain an artificial outward displacement on every gravity-driven contact.
    private const float FlightSeparation = .00001f;
    private const int MaximumFlightContacts = 4096;
    /// <summary>Largest sweep-loop iteration count in any substep of the latest tick, including its final clear sweep.</summary>
    public int MaximumFlightIterationsThisStep { get; private set; }
    private readonly Dictionary<(MachinePart Body, MachinePart? Surface), float> _surfaceDampingBudgets = new();
    private readonly Dictionary<(MachinePart First, MachinePart Second), BodyContactConvergence> _bodyContactConvergence = new();

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
        body.ConstrainVelocity();
        _surfaceDampingBudgets[key] = Mathf.Max(0, budget - friction.SpeedLoss);
    }

    /// <summary>Advance all bodies on one shared clock, resolving the earliest impact before
    /// consuming the remaining substep. Forces are sampled by Step, not applied again per impact.</summary>
    private void AdvanceBodies(float duration)
    {
        var bodies = Bodies.OrderBy(body => body.Uid, StringComparer.Ordinal).ToArray();
        double remaining = duration;
        var contacts = 0;
        var geometry = new Dictionary<MachinePart, WorldGeometry.SweepSnapshot>();
        _surfaceDampingBudgets.Clear();
        _bodyContactConvergence.Clear();
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
                if (!body.Visible || !body.FreeMotion) continue;
                var stop = body.TimeToMotionLimit();
                if (stop <= time)
                {
                    time = Math.Max(0, stop); first = body; kind = FlightContactKind.MotionLimit;
                }
                if (!geometry.TryGetValue(body, out var snapshot))
                {
                    snapshot = WorldGeometry.CaptureSweep(this, body, null, SweepBodyMode.ExcludeBodies);
                    geometry.Add(body, snapshot);
                }
                var hit = snapshot.Sweep(body.Position, body.Radius, body.Velocity * interval);
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
                if (!a.Visible || !b.Visible || (!a.FreeMotion && !b.FreeMotion)) continue;
                var hit = MovingSphereSweep.Cast(a.Position, a.Radius, a.Velocity,
                    b.Position, b.Radius, b.Velocity, interval);
                if (hit.Status == SphereSweepStatus.Clear ||
                    (kind != FlightContactKind.None && hit.Time >= time)) continue;
                if (_bodyContactConvergence.TryGetValue((a, b), out var convergence) &&
                    convergence.HasConverged(a.Velocity, b.Velocity, hit, interval)) continue;
                time = hit.Time;
                first = a; second = b;
                pair = hit;
                kind = FlightContactKind.Body;
            }
            MaximumFlightIterationsThisStep = Math.Max(MaximumFlightIterationsThisStep, ++contacts);
            if (contacts > MaximumFlightContacts)
                throw new InvalidOperationException($"Flight contacts did not converge; remaining time was not discarded. Tick={Ticks}, kind={kind}, first={first?.Uid} position={first?.Position} velocity={first?.Velocity}, second={second?.Uid} position={second?.Position} velocity={second?.Velocity}, time={time}, penetration={pair.Penetration}, normal={pair.Normal}.");
            if (time > 0)
            {
                _bodyContactConvergence.Clear();
                geometry.Clear();
                foreach (var body in bodies)
                    if (body.Visible) body.Position += body.Velocity * time;
            }
            remaining -= time;
            if (kind == FlightContactKind.None) return;
            var target = first!;
            switch (kind)
            {
                case FlightContactKind.Surface:
                    geometry.Clear(); // Separation and OnContact can change any solid proxy.
                    var response = target.InverseMassResponse(surface.Normal);
                    var inverseMass = surface.Normal.Dot(response);
                    if (inverseMass <= 0)
                        throw new InvalidOperationException("A guide prevents separation from a solid surface.");
                    if (surface.Penetration > 0)
                        target.Position += response * ((surface.Penetration + FlightSeparation) / inverseMass);
                    var speed = target.Velocity.Dot(surface.Normal);
                    if (speed >= 0) break;
                    var bounce = surface.Kind switch
                    {
                        SweepObstacleKind.Workbench => 1,
                        SweepObstacleKind.Part => surface.Part!.SurfaceBounce,
                        _ => throw new InvalidOperationException("Surface contact has no obstacle.")
                    };
                    target.Velocity -= response * (speed * (1 + target.Bounce * bounce) / inverseMass);
                    target.ConstrainVelocity();
                    if (surface.Surface == SweepSurfaceKind.Box && Math.Abs(speed) < .45f && surface.Normal.Y > .7f)
                        target.Velocity -= surface.Normal * target.Velocity.Dot(surface.Normal);
                    ApplySurfaceFriction(target, surface.Part, surface.Normal, speed);
                    surface.Part?.OnContact(target, -speed, this);
                    target.ConstrainVelocity();
                    break;
                case FlightContactKind.Body:
                    var other = second!;
                    _bodyContactConvergence.TryGetValue((target, other), out var previous);
                    _bodyContactConvergence[(target, other)] = previous.Observe(target.Velocity, other.Velocity, pair.Normal);
                    if (pair.Penetration > 0) geometry.Clear();
                    BodyContact.Resolve(target, other, pair.Normal, pair.Penetration,
                        Mathf.Min(target.Bounce, other.Bounce));
                    break;
                case FlightContactKind.MotionLimit:
                    geometry.Clear();
                    target.ReachMotionLimit();
                    break;
                default:
                    throw new InvalidOperationException("Unsupported flight contact.");
            }
        }
    }
}
