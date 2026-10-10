using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

public enum WorkshopConnectionDomain : uint { Activation = 1, Electrical = 2 }
public enum WorkshopSocket : uint { ActivationOut = 1, ActivationIn = 4, Supply = 7, PowerIn = 8 }
public enum WorkshopPortDirection : uint { Input = 1, Output = 2 }
public readonly record struct WorkshopPort(WorkshopSocket Socket, WorkshopConnectionDomain Domain, WorkshopPortDirection Direction);
public readonly record struct WorkshopConnection(GpuBodyId Source, WorkshopSocket Output, GpuBodyId Target,
    WorkshopSocket Input, WorkshopConnectionDomain Domain);

/// <summary>Catalogue ports declare capability; unsupported electrical routes are never admitted.</summary>
public static class WorkshopPorts
{
    public static ReadOnlySpan<WorkshopPort> For(WorkshopPartKind kind) => kind switch
    {
        WorkshopPartKind.ImpactSwitch => SwitchPorts,
        WorkshopPartKind.SignalLamp => LampPorts,
        WorkshopPartKind.Delay => DelayPorts,
        WorkshopPartKind.Domino => DominoPorts,
        WorkshopPartKind.Battery => BatteryPorts,
        WorkshopPartKind.PinballBumper => StoragePorts,
        WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall or WorkshopPartKind.Receiver or WorkshopPartKind.Ramp or WorkshopPartKind.Wall => [],
        _ => throw new ArgumentException("Unsupported port owner.")
    };
    private static readonly WorkshopPort[] BatteryPorts =
    [
        new(WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output)
    ];
    private static readonly WorkshopPort[] StoragePorts =
    [
        new(WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input)
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
    private static readonly WorkshopPort[] LampPorts =
    [
        new(WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input)
    ];

    private static readonly WorkshopPort[] DelayPorts =
    [
        new(WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
        new(WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output)
    ];

    public static MetreVector LocalPosition(WorkshopPartKind kind, WorkshopSocket socket) => (kind, socket) switch
    {
        (WorkshopPartKind.ImpactSwitch, WorkshopSocket.ActivationOut) => new((Half).45, (Half)0, (Half).4),
        (WorkshopPartKind.SignalLamp, WorkshopSocket.ActivationIn) => default,
        (WorkshopPartKind.Battery, WorkshopSocket.Supply) => new((Half)0, (Half).62f, (Half)0),
        (WorkshopPartKind.PinballBumper, WorkshopSocket.PowerIn) => new((Half)0, (Half)0, (Half).65f),
        (WorkshopPartKind.Delay, WorkshopSocket.ActivationIn) => new((Half)(-.72),(Half)0,(Half)0),
        (WorkshopPartKind.Delay, WorkshopSocket.ActivationOut) => new((Half).72,(Half)0,(Half)0),
        (WorkshopPartKind.Domino, WorkshopSocket.ActivationOut) => new((Half)0, (Half).55, (Half)0),
        _ => throw new ArgumentException("This socket is not admitted for placement.")
    };
    public static bool Has(WorkshopPartKind kind, WorkshopSocket socket, WorkshopConnectionDomain domain, WorkshopPortDirection direction)
    {
        foreach (var port in For(kind))
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
                !WorkshopPorts.Has(source.Kind, link.Output, link.Domain, WorkshopPortDirection.Output) ||
                !WorkshopPorts.Has(target.Kind, link.Input, link.Domain, WorkshopPortDirection.Input))
                throw new ArgumentException("Connection does not own matching declared ports.");
            if (link.Domain == WorkshopConnectionDomain.Electrical)
                foreach (var previous in seen)
                    if (previous != link && previous.Target == link.Target && previous.Input == link.Input &&
                        previous.Domain == WorkshopConnectionDomain.Electrical)
                        throw new ArgumentException("This power input already has a supplier.");
        }
    }
    public bool Equals(WorkshopConnections? other) => other is not null && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => obj is WorkshopConnections other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode(); foreach (var item in _items) hash.Add(item); return hash.ToHashCode();
    }
}
