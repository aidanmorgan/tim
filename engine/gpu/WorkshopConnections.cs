using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public enum WorkshopConnectionDomain : uint { Activation = 1, Electrical = 2 }
public enum WorkshopSocket : uint { ActivationOut = 1, SetIn = 2, ResetIn = 3, ActivationIn = 4, Supply = 7, PowerIn = 8 }
public enum WorkshopPortDirection : uint { Input = 1, Output = 2 }
public readonly record struct WorkshopPort(WorkshopSocket Socket, WorkshopConnectionDomain Domain, WorkshopPortDirection Direction);
public readonly record struct WorkshopConnection(GpuBodyId Source, WorkshopSocket Output, GpuBodyId Target,
    WorkshopSocket Input, WorkshopConnectionDomain Domain);

/// <summary>Catalogue ports declare capability; unsupported electrical routes are never admitted.</summary>
public static class WorkshopPorts
{
    public static ReadOnlySpan<WorkshopPort> For(PartId id) => id.Value switch
    {
        "switch" => SwitchPorts,
        "lamp" => LampPorts,
        "delay" => DelayPorts,
        "domino" => DominoPorts,
        "battery" => BatteryPorts,
        "bumper" => StoragePorts,
        "ball_detector" => BallDetectorPorts,
        "powered_gate" => GatePorts,
        "pressure_plate" => PressurePlatePorts,
        "counter" => CounterPorts,
        "clock" => ClockPorts,
        "latch" => LatchPorts,
        "ball" or "bowling" or "basket" or "ramp" or "wall" or "spring" or "tennis" or "balloon" or "pipe" or "pipe_bend_45" or "pipe_bend_90" or "funnel" => [],
        _ => throw new ArgumentException("Unsupported port owner.")
    };
    private static readonly WorkshopPort[] LatchPorts =
    [
        new(WorkshopSocket.SetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
        new(WorkshopSocket.ResetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input),
        // Level output: high while memory is On (owner decision 18 Oct 2026). Listed before Supply so the drawer keeps Supply's row.
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output),
        new(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] ClockPorts =
    [
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input),
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] BatteryPorts =
    [
        new(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] StoragePorts =
    [
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input)
    ];
    private static readonly WorkshopPort[] GatePorts =
    [
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input)
    ];
    private static readonly WorkshopPort[] PressurePlatePorts =
    [
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input),
        new(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] CounterPorts =
    [
        new(WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output),
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input),
        new(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output)
    ];
    // A Domino signals only; it declares no activation input, so wiring into one is rejected at the port check.
    private static readonly WorkshopPort[] DominoPorts =
    [
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] SwitchPorts =
    [
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] BallDetectorPorts =
    [
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] LampPorts =
    [
        new(WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input)
    ];

    private static readonly WorkshopPort[] DelayPorts =
    [
        new(WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)
    ];

    /// <summary>Declared f32 socket anchor in the part's local frame (metres); presentation-only wire artwork endpoint.</summary>
    public static Vector3 LocalPosition(PartId id, WorkshopSocket socket) => (id.Value, socket) switch
    {
        ("switch", WorkshopSocket.ActivationOut) => new(.45f, 0f, .4f),
        ("lamp", WorkshopSocket.ActivationIn) => Vector3.Zero,
        ("battery", WorkshopSocket.Supply) => new(0f, .62f, 0f),
        ("bumper", WorkshopSocket.PowerIn) => new(0f, 0f, .65f),
        ("delay", WorkshopSocket.ActivationIn) => new(-.72f, 0f, 0f),
        ("delay", WorkshopSocket.ActivationOut) => new(.72f, 0f, 0f),
        ("domino", WorkshopSocket.ActivationOut) => new(0f, .55f, 0f),
        ("ball_detector", WorkshopSocket.ActivationOut) => new(0f, 1.12f, 0f),
        ("powered_gate", WorkshopSocket.PowerIn) => new(0f, 1.65f, .92f),
        ("pressure_plate", WorkshopSocket.PowerIn) => new(-1.18f, -.08f, 0f),
        ("pressure_plate", WorkshopSocket.Supply) => new(1.18f, -.08f, 0f),
        ("counter", WorkshopSocket.ActivationIn) => new(0f, .72f, .1f),
        ("counter", WorkshopSocket.ActivationOut) => new(0f, -.72f, .1f),
        ("counter", WorkshopSocket.PowerIn) => new(-.93f, 0f, 0f),
        ("counter", WorkshopSocket.Supply) => new(.93f, 0f, 0f),
        ("clock", WorkshopSocket.PowerIn) => new(-.78f, 0f, 0f),
        ("clock", WorkshopSocket.ActivationOut) => new(.78f, 0f, 0f),
        ("latch", WorkshopSocket.SetIn) => new(-.45f, .72f, 0f),
        ("latch", WorkshopSocket.ResetIn) => new(.45f, .72f, 0f),
        ("latch", WorkshopSocket.PowerIn) => new(-.93f, 0f, 0f),
        ("latch", WorkshopSocket.Supply) => new(.93f, 0f, 0f),
        ("latch", WorkshopSocket.ActivationOut) => new(0f, -.56f, .38f),
        _ => throw new ArgumentException("This socket is not admitted for placement.")
    };
    public static bool Has(PartId id, WorkshopSocket socket, WorkshopConnectionDomain domain, WorkshopPortDirection direction)
    {
        foreach (var port in For(id))
            if (port.Socket == socket && port.Domain == domain && port.Direction == direction) return true;
        return false;
    }
}

/// <summary>Immutable authored edges; neither storage nor connection validation executes activation.</summary>
public sealed class WorkshopConnections : IReadOnlyList<WorkshopConnection>, IEquatable<WorkshopConnections>
{
    public const int Capacity = 8;
    private readonly WorkshopConnection[] _items;
    public static WorkshopConnections Empty { get; } = new();
    public WorkshopConnections(params WorkshopConnection[] items) => _items = (WorkshopConnection[])items.Clone();
    public int Count => _items.Length;
    public WorkshopConnection this[int index] => _items[index];
    public IEnumerator<WorkshopConnection> GetEnumerator() => ((IEnumerable<WorkshopConnection>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public WorkshopConnections With(WorkshopConnection connection) => new([.. _items, connection]);
    public WorkshopConnections Without(GpuBodyId id) => new(_items.Where(link => link.Source != id && link.Target != id).ToArray());
    public WorkshopConnections Without(WorkshopConnection connection) => new(_items.Where(link => link != connection).ToArray());
    public bool HasSource(GpuBodyId id)
    {
        foreach (var link in _items) if (link.Source == id) return true;
        return false;
    }
    public void Validate(WorkshopInstances instances)
    {
        if (Count > Capacity) throw new ArgumentException("Connection capacity exceeded.");
        var seen = new HashSet<WorkshopConnection>();
        foreach (var link in _items)
        {
            if (link.Source.Value == 0 || link.Target.Value == 0 || link.Source == link.Target ||
                !Enum.IsDefined(link.Output) || !Enum.IsDefined(link.Input) ||
                !Enum.IsDefined(link.Domain) || !seen.Add(link))
                throw new ArgumentException("Unsupported or duplicate connection.");
            var source = instances.FirstOrDefault(part => part.Id == link.Source);
            var target = instances.FirstOrDefault(part => part.Id == link.Target);
            if (source is null || target is null ||
                !WorkshopPorts.Has(source.PartId, link.Output, link.Domain, WorkshopPortDirection.Output) ||
                !WorkshopPorts.Has(target.PartId, link.Input, link.Domain, WorkshopPortDirection.Input))
                throw new ArgumentException("Connection does not own matching declared ports.");
            if (link.Domain == WorkshopConnectionDomain.Activation)
            {
                if (link.Output != WorkshopSocket.ActivationOut) throw new ArgumentException("Unsupported activation output.");
                if (link.Input is not (WorkshopSocket.ActivationIn or WorkshopSocket.SetIn or WorkshopSocket.ResetIn)) throw new ArgumentException("Unsupported activation input.");
                // A latch output is a level, so it drives Signal lamps only, and such a lamp follows that latch alone.
                if (source.PartId == PartId.Latch && target.PartId != PartId.SignalLamp)
                    throw new ArgumentException("A latch output can only light a Signal lamp.");
                if (target.PartId == PartId.SignalLamp && _items.Count(other => other.Domain == WorkshopConnectionDomain.Activation && other.Target == link.Target) > 1 &&
                    _items.Any(other => other.Domain == WorkshopConnectionDomain.Activation && other.Target == link.Target &&
                        instances.FirstOrDefault(part => part.Id == other.Source)?.PartId == PartId.Latch))
                    throw new ArgumentException("A lamp that follows a latch takes no other signal.");
            }
            else
            {
                if (link.Output != WorkshopSocket.Supply || link.Input != WorkshopSocket.PowerIn)
                    throw new ArgumentException("Unsupported electrical socket.");
                foreach (var previous in seen)
                    if (previous != link && previous.Target == link.Target && previous.Input == link.Input &&
                        previous.Domain == WorkshopConnectionDomain.Electrical)
                        throw new ArgumentException("This power input already has a supplier.");
            }
        }
    }
    public bool Equals(WorkshopConnections? other) =>
        other is not null && _items.Length == other._items.Length && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => Equals(obj as WorkshopConnections);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items) hash.Add(item);
        return hash.ToHashCode();
    }
}
