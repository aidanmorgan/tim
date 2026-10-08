using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>Compiled tables an authored population can exhaust. Closed set; display text lives at the exception boundary.</summary>
public enum WorkbenchTable : uint { Instances, Bodies, DynamicBodies, Colliders, Sensors, Guides, Triggers, ContactWork, ActivationNodes }

/// <summary>Explicit rejection when a construction needs more rows than the fixed physics tables hold. Never a fault.</summary>
public sealed class WorkbenchFullException : ArgumentException
{
    public WorkbenchTable Table { get; }
    public WorkbenchFullException(WorkbenchTable table) : base("Workbench is full: " + Describe(table)) => Table = table;
    private static string Describe(WorkbenchTable table) => table switch
    {
        WorkbenchTable.Instances => "part table",
        WorkbenchTable.Bodies => "body table",
        WorkbenchTable.DynamicBodies => "moving body table",
        WorkbenchTable.Colliders => "collider table",
        WorkbenchTable.Sensors => "receiver sensor table",
        WorkbenchTable.Guides => "ball guide table (one Receiver guides the balls)",
        WorkbenchTable.Triggers => "impact trigger table",
        WorkbenchTable.ContactWork => "bumper work table",
        WorkbenchTable.ActivationNodes => "activation table",
        _ => throw new ArgumentException("Undefined workbench table.")
    };
}

/// <summary>Rows one authored part adds to each compiled table; mirrors WorkshopPhysicsCompiler and WorkshopActivationCompiler.</summary>
public readonly record struct WorkbenchFootprint(int Bodies, int DynamicBodies, int Colliders, int Triggers, int ContactWorks, int ActivationNodes)
{
    /// <summary>The compiled workbench plane itself.</summary>
    public static WorkbenchFootprint Plane => new(1, 0, 1, 0, 0, 0);
    public static WorkbenchFootprint Of(WorkshopPartKind kind) => kind switch
    {
        WorkshopPartKind.Basketball or WorkshopPartKind.Domino => new(1, 1, 1, 0, 0, 0),
        WorkshopPartKind.Receiver => new(1, 0, ReceiverGeometry.Walls.Length, 0, 0, 0),
        WorkshopPartKind.Ramp or WorkshopPartKind.Wall => new(1, 0, 1, 0, 0, 0),
        WorkshopPartKind.ImpactSwitch => new(1, 0, 2, 1, 0, 1),
        WorkshopPartKind.SignalLamp or WorkshopPartKind.Delay => new(1, 0, 1, 0, 0, 1),
        WorkshopPartKind.PinballBumper => new(1, 0, 1, 0, 1, 0),
        _ => throw new ArgumentException("Unsupported authored instance declaration.")
    };
    public static WorkbenchFootprint operator +(WorkbenchFootprint a, WorkbenchFootprint b) =>
        new(a.Bodies + b.Bodies, a.DynamicBodies + b.DynamicBodies, a.Colliders + b.Colliders,
            a.Triggers + b.Triggers, a.ContactWorks + b.ContactWorks, a.ActivationNodes + b.ActivationNodes);
}

/// <summary>The only population limit: fixed compiled table capacities. No per-kind count applies.</summary>
public static class WorkbenchCapacity
{
    public static void Validate(IReadOnlyList<IWorkshopInstance> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);
        if (instances.Count > WorkshopInstances.Capacity) throw new WorkbenchFullException(WorkbenchTable.Instances);
        var total = WorkbenchFootprint.Plane; var balls = 0; var receivers = 0;
        foreach (var instance in instances)
        {
            total += WorkbenchFootprint.Of(instance.Kind);
            if (instance.Kind == WorkshopPartKind.Basketball) balls++;
            else if (instance.Kind == WorkshopPartKind.Receiver) receivers++;
        }
        // One contact material per body, so MaterialCapacity is covered by BodyCapacity.
        if (total.Bodies > PhysicsSceneDeclaration.BodyCapacity) throw new WorkbenchFullException(WorkbenchTable.Bodies);
        if (total.DynamicBodies > PhysicsBodyReadSet.Capacity) throw new WorkbenchFullException(WorkbenchTable.DynamicBodies);
        if (total.Colliders > PhysicsSceneDeclaration.ColliderCapacity) throw new WorkbenchFullException(WorkbenchTable.Colliders);
        // Every receiver declares one residence sensor and one planar guide per ball, and the physics document
        // admits one guide per dynamic body, so a second Receiver fits only while no ball is on the workbench.
        var perBallRows = balls * receivers;
        if (perBallRows > PhysicsSceneDeclaration.SensorCapacity) throw new WorkbenchFullException(WorkbenchTable.Sensors);
        if (perBallRows > PhysicsSceneDeclaration.GuideCapacity) throw new WorkbenchFullException(WorkbenchTable.Guides);
        if (receivers > 1 && balls > 0) throw new WorkbenchFullException(WorkbenchTable.Guides);
        if (total.Triggers > PhysicsSceneDeclaration.TriggerCapacity) throw new WorkbenchFullException(WorkbenchTable.Triggers);
        if (total.ContactWorks > PhysicsSceneDeclaration.ContactWorkCapacity) throw new WorkbenchFullException(WorkbenchTable.ContactWork);
        if (total.ActivationNodes > ActivationNetwork.Capacity) throw new WorkbenchFullException(WorkbenchTable.ActivationNodes);
    }

    /// <summary>A Domino compiles an activation node only while it is a connection source, so the node table is checked against the wiring.</summary>
    public static void ValidateConnected(IReadOnlyList<IWorkshopInstance> instances, WorkshopConnections connections)
    {
        ArgumentNullException.ThrowIfNull(instances); ArgumentNullException.ThrowIfNull(connections);
        var nodes = 0;
        foreach (var instance in instances)
            nodes += WorkbenchFootprint.Of(instance.Kind).ActivationNodes +
                (instance.Kind == WorkshopPartKind.Domino && connections.HasSource(instance.Id) ? 1 : 0);
        if (nodes > ActivationNetwork.Capacity) throw new WorkbenchFullException(WorkbenchTable.ActivationNodes);
    }
}
