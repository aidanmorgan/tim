using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Story 9A.23 D8: the main-thread event ring cursor's one production consumer, the goal-cue wake. The goal is a committed
/// level (<see cref="WorkshopGoalEvaluator"/> stays its only authority), so it is not polled on every pump: a record that can change it
/// marks a pending wake at that record's tick, and the goal is evaluated once a committed read of that tick or later is installed. The
/// record usually arrives before its read (the worker publishes a tick's records first), so the wake is held until then.
/// <list type="bullet">
/// <item>A Captured goal is woken by its receiver frame's records (the Captured record of its sensor), never by another owner's.</item>
/// <item>An ActivatedAfter goal is woken by every record: its target node may be an indicator that publishes nothing, latched in the tick
/// of an upstream source's record (the switch → delay → lamp chain of delayed_signal), and every network transition is published by some
/// source in its own tick.</item>
/// </list>
/// A reported lap may have lost a waking record: the goal is evaluated at once and again when a read past every delivered record is
/// installed. A goal whose capture sensor is not in the installed scene has nothing to wake it (and nothing can capture it).</summary>
public sealed class WorkshopGoalWake
{
    private readonly HashSet<GpuBodyId> _owners = new(16);
    private readonly bool _anyRecord;
    private ulong? _wakeTick;
    private bool _now;

    public WorkshopGoalWake(WorkshopGoal goal, PhysicsSceneDeclaration scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        goal.Validate();
        switch (goal.Kind)
        {
            case WorkshopGoalKind.None: break;
            case WorkshopGoalKind.Captured:
                foreach (var sensor in scene.Sensors)
                    if (sensor.Frame == goal.Target) foreach(var body in scene.Bodies)
                        if(body.Id==goal.Body && body.Motion==RigidMotionKind.Dynamic && sensor.Targets.Contains(body.Id,body.Payload)) _owners.Add(sensor.Frame);
                break;
            case WorkshopGoalKind.ActivatedAfter: _anyRecord = true; break;
            default: throw new ArgumentException("Unknown goal kind.");
        }
    }

    /// <summary>Records the cursor skipped in this world, cumulative.</summary>
    public ulong Lapped { get; private set; }
    /// <summary>The committed tick a pending wake waits for, if any.</summary>
    public ulong? WakeTick => _wakeTick;

    /// <summary>Takes one delivery of the main-thread cursor while <paramref name="installed"/> is the latest committed read's tick.</summary>
    public void Observe(EventRingDelivery delivery, SimulationTick installed)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ulong? newest = null;
        foreach (var record in delivery.Records)
        {
            newest = Math.Max(newest ?? 0, record.Tick);
            if (_anyRecord || _owners.Contains(new(record.Source))) Wake(record.Tick);
        }
        if (delivery.Skipped == 0 || (!_anyRecord && _owners.Count == 0)) return;
        Lapped = checked(Lapped + delivery.Skipped);
        _now = true;
        Wake(newest ?? checked(installed.Value + 1));
    }

    /// <summary>Whether the goal should be evaluated against the installed committed read.</summary>
    public bool Due(SimulationTick installed) => _now || (_wakeTick is { } tick && installed.Value >= tick);

    /// <summary>The goal was evaluated against the installed read: a solve ends the wake; otherwise only a wake the read covered.</summary>
    public void Evaluated(SimulationTick installed, bool solved)
    {
        _now = false;
        if (solved || (_wakeTick is { } tick && installed.Value >= tick)) _wakeTick = null;
    }

    private void Wake(ulong tick) => _wakeTick = Math.Max(_wakeTick ?? 0, tick);
}
