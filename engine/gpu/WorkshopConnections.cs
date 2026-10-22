using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

public enum WorkshopConnectionDomain : uint { Activation = 1, Electrical = 2 }
/// <summary>The engine's sockets. Each value equals the bundle's <c>PortSocket</c> of the same name, and is the wire and save
/// value.</summary>
public enum WorkshopSocket : uint { ActivationOut = 1, SetIn = 2, ResetIn = 3, ActivationIn = 4, FirstIn = 5, SecondIn = 6, Supply = 7, PowerIn = 8 }
public enum WorkshopPortDirection : uint { Input = 1, Output = 2 }
/// <summary>What an activation port carries (Story 9R.5): an output declares what it emits, an input the strongest signal it admits.
/// A Pulse is one occurrence; a Level is a committed high/low value (the latch's memory, the plate's held load). An input that
/// admits a Level also admits a Pulse.</summary>
public enum WorkshopSignalKind : uint { Pulse = 1, Level = 2 }
/// <summary>One declared socket, admitted from the element bundle (<see cref="ElementPortCatalogue"/>). Signal is declared on every
/// activation port and absent (null) on every electrical port; LocalPosition is the declared f32 anchor in the part's local frame
/// (metres), the wire artwork's endpoint.</summary>
public readonly record struct WorkshopPort(WorkshopSocket Socket, WorkshopConnectionDomain Domain, WorkshopPortDirection Direction,
    WorkshopSignalKind? Signal, Vector3 LocalPosition);
public readonly record struct WorkshopConnection(GpuBodyId Source, WorkshopSocket Output, GpuBodyId Target,
    WorkshopSocket Input, WorkshopConnectionDomain Domain);

/// <summary>Immutable authored edges; neither storage nor connection validation executes activation. There is no connection limit
/// (Story 9R.5b).</summary>
public sealed class WorkshopConnections : IReadOnlyList<WorkshopConnection>, IEquatable<WorkshopConnections>
{
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
        ArgumentNullException.ThrowIfNull(instances);
        Validate(id => instances.FirstOrDefault(part => part.Id == id)?.PartId);
    }

    /// <summary>The connection rules over the parts the construction names: a typed refusal for every inadmissible connection.
    /// The ports are the loaded bundle's declared ports (Story 9A.17). A stored save's pre-validation applies them to its saved
    /// part identities before any instance is constructed.</summary>
    internal void Validate(Func<GpuBodyId, PartId?> partOf)
    {
        ArgumentNullException.ThrowIfNull(partOf);
        if (_items.Length == 0) return;
        var ports = DeclaredPorts.Current;
        var seen = new HashSet<WorkshopConnection>();
        foreach (var link in _items)
        {
            if (link.Source.Value == 0 || link.Target.Value == 0 || link.Source == link.Target ||
                !Enum.IsDefined(link.Output) || !Enum.IsDefined(link.Input) ||
                !Enum.IsDefined(link.Domain) || !seen.Add(link))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported or duplicate connection.");
            var source = partOf(link.Source);
            var target = partOf(link.Target);
            if (source is null || target is null ||
                !ports.Has(source.Value, link.Output, link.Domain, WorkshopPortDirection.Output) ||
                !ports.Has(target.Value, link.Input, link.Domain, WorkshopPortDirection.Input))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Connection does not own matching declared ports.");
            if (link.Domain == WorkshopConnectionDomain.Activation)
            {
                if (link.Output != WorkshopSocket.ActivationOut)
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported activation output.");
                if (link.Input is not (WorkshopSocket.ActivationIn or WorkshopSocket.SetIn or WorkshopSocket.ResetIn))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported activation input.");
                // A level output (latch, plate) drives only an input that admits a level (the Signal lamp), and such an input then
                // follows that one level alone. The rule reads the declared port kinds, never the part identity.
                if (ports.Signal(source.Value, link.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level &&
                    ports.Signal(target.Value, link.Input, WorkshopPortDirection.Input) != WorkshopSignalKind.Level)
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.LevelDrivesLampOnly, "A level output can only light a Signal lamp.");
                var feeds = _items.Where(other => other.Domain == WorkshopConnectionDomain.Activation && other.Target == link.Target && other.Input == link.Input).ToArray();
                if (feeds.Length > 1 && feeds.Any(other => partOf(other.Source) is { } feeder &&
                        ports.Has(feeder, other.Output, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output) &&
                        ports.Signal(feeder, other.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.LampFollowsOneLevel, "A lamp that follows a level output takes no other signal.");
            }
            else
            {
                if (link.Output != WorkshopSocket.Supply || link.Input is not (WorkshopSocket.PowerIn or WorkshopSocket.FirstIn or WorkshopSocket.SecondIn))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported electrical socket.");
                foreach (var previous in seen)
                    if (previous != link && previous.Target == link.Target && previous.Input == link.Input &&
                        previous.Domain == WorkshopConnectionDomain.Electrical &&
                        DeclaredPhysics.Of(target.Value).Electrical?.Supply?.Kind is not (SupplyCapabilityKind.SwitchedContact or SupplyCapabilityKind.LogicGate))
                        throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied, "This power input already has a supplier.");
            }
        }
        // Every electrical wire is a potential path, open contacts included: a loop through a nonmonotone gate is refused here, in
        // build mode, before any construction or solve sees it. The rule reads each part's declared gate law, never its identity.
        var loop = SupplyCycles.NonmonotoneCycle(
            _items.Where(link => link.Domain == WorkshopConnectionDomain.Electrical).Select(link => (link.Source, link.Target)),
            owner => DeclaredPhysics.Of(partOf(owner)!.Value).Electrical?.Supply?.Operation);
        if (loop.Length != 0)
        {
            // The bottom status line wraps but never truncates: name the first few parts (ascending ids) and count the rest.
            var named = string.Join(", ", loop.Take(LoopPartsNamed).Select(owner => owner.Value));
            var rest = loop.Length > LoopPartsNamed ? $" and {loop.Length - LoopPartsNamed} more" : "";
            throw new WorkshopEditRejectedException(WorkshopEditRejection.NonmonotoneFeedback,
                $"Break the wire loop through parts {named}{rest}. This gate's output cannot feed its own inputs.");
        }
    }
    /// <summary>How many part ids a feedback refusal names before it counts the remainder.</summary>
    internal const int LoopPartsNamed = 4;
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
