using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public enum WorkshopConnectionDomain : uint { Activation = 1, Electrical = 2, Rope = 3 }
public enum WorkshopPortDirection : uint { Input = 1, Output = 2, Bidirectional = 3 }
/// <summary>What an activation port carries (Story 9R.5): an output declares what it emits, an input the strongest signal it admits.
/// A Pulse is one occurrence; a Level is a committed high/low value (the latch's memory, a declared sensor's held contact load). An input that
/// admits a Level also admits a Pulse.</summary>
public enum WorkshopSignalKind : uint { Pulse = 1, Level = 2 }
/// <summary>One declared socket, admitted from the element bundle (<see cref="ElementPortCatalogue"/>). Signal is declared on every
/// activation port and absent (null) on every electrical port; LocalPosition is the declared f32 anchor in the part's local frame
/// (metres), the wire artwork's endpoint.</summary>
public readonly record struct WorkshopPort(WorkshopSocket Socket, WorkshopConnectionDomain Domain, WorkshopPortDirection Direction,
    WorkshopSignalKind? Signal, Vector3 LocalPosition, RopeAttachmentRole AttachmentRole = RopeAttachmentRole.None, PortPositionBinding? PositionBinding = null,
    CuriousContraptions.Abstractions.PackageBodyIndex Body = default);
public readonly record struct WorkshopConnection(GpuBodyId Source, WorkshopSocket Output, GpuBodyId Target,
    WorkshopSocket Input, WorkshopConnectionDomain Domain, float? RopeLength = null);

/// <summary>Immutable authored edges; neither storage nor connection validation executes activation. There is no connection limit
/// (Story 9R.5b).</summary>
public sealed class WorkshopConnections : IReadOnlyList<WorkshopConnection>, IEquatable<WorkshopConnections>
{
    private readonly WorkshopConnection[] _items;
    public static WorkshopConnections Empty { get; } = new();
    public WorkshopConnections(params WorkshopConnection[] items) => _items = (WorkshopConnection[])items.Clone();
    public static WorkshopConnection MeasureRope(WorkshopInstances instances, GpuBodyId source, GpuBodyId target)
    {
        Vector3 Attachment(GpuBodyId owner)
        {
            if (instances.SingleOrDefault(p => p.Id == owner) is not WorkshopGenericInstance instance)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Absent rope endpoint.");
            var local = DeclaredPorts.Current.LocalPosition(instance, WorkshopSocket.Tie);
            return new Vector3((instance.Cell.X + instance.Local.X) / 16f, (instance.Cell.Y + instance.Local.Y) / 16f,
                (instance.Cell.Z + instance.Local.Z) / 16f) + Vector3.Transform(local,
                new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W));
        }
        var link = new WorkshopConnection(source, WorkshopSocket.Tie, target, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope,
            Vector3.Distance(Attachment(source), Attachment(target)));
        new WorkshopConnections(link).Validate(instances);
        return link;
    }
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
        var seen = new HashSet<WorkshopConnection>(16);
        foreach (var link in _items)
        {
            if (link.Source.Value == 0 || link.Target.Value == 0 || link.Source == link.Target ||
                !Enum.IsDefined(link.Output) || !Enum.IsDefined(link.Input) ||
                !Enum.IsDefined(link.Domain) || !seen.Add(link))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported or duplicate connection.");
            var source = partOf(link.Source);
            var target = partOf(link.Target);
            var directionFrom = link.Domain == WorkshopConnectionDomain.Rope ? WorkshopPortDirection.Bidirectional : WorkshopPortDirection.Output;
            var directionTo = link.Domain == WorkshopConnectionDomain.Rope ? WorkshopPortDirection.Bidirectional : WorkshopPortDirection.Input;
            if (source is null || target is null ||
                !ports.Has(source.Value, link.Output, link.Domain, directionFrom) ||
                !ports.Has(target.Value, link.Input, link.Domain, directionTo))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Connection does not own matching declared ports.");
            if (link.Domain != WorkshopConnectionDomain.Rope && link.RopeLength is not null)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Only rope connections carry length.");
            if (link.Domain == WorkshopConnectionDomain.Activation)
            {
                if (link.Output != WorkshopSocket.ActivationOut)
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported activation output.");
                if (link.Input is not (WorkshopSocket.ActivationIn or WorkshopSocket.SetIn or WorkshopSocket.ResetIn))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported activation input.");
                // A level output (the latch, a declared contact-load sensor) drives only an input that admits a level (a declared indicator input), and such an
                // input then follows that one level alone. The rule reads the declared port kinds, never the part identity.
                if (ports.Signal(source.Value, link.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level &&
                    ports.Signal(target.Value, link.Input, WorkshopPortDirection.Input) != WorkshopSignalKind.Level)
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.LevelDrivesLevelInputOnly, "A level output can only light a Signal lamp.");
                var feeds = _items.Where(other => other.Domain == WorkshopConnectionDomain.Activation && other.Target == link.Target && other.Input == link.Input).ToArray();
                if (feeds.Length > 1 && feeds.Any(other => partOf(other.Source) is { } feeder &&
                        ports.Has(feeder, other.Output, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output) &&
                        ports.Signal(feeder, other.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.LevelInputFollowsOneLevel, "A lamp that follows a level output takes no other signal.");
            }
            else if (link.Domain == WorkshopConnectionDomain.Electrical)
            {
                if (link.Output != WorkshopSocket.Supply || link.Input is not (WorkshopSocket.PowerIn or WorkshopSocket.FirstIn or WorkshopSocket.SecondIn))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported electrical socket.");
                foreach (var previous in seen)
                    if (previous != link && previous.Target == link.Target && previous.Input == link.Input &&
                        previous.Domain == WorkshopConnectionDomain.Electrical &&
                        DeclaredPhysics.Of(target.Value).Electrical?.Supply?.Kind is not (SupplyCapabilityKind.SwitchedContact or SupplyCapabilityKind.LogicGate))
                        throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied, "This power input already has a supplier.");
            }
            else if (link.Domain == WorkshopConnectionDomain.Rope)
            {
                if (link.Output is not (WorkshopSocket.Tie or WorkshopSocket.TieA or WorkshopSocket.TieB) ||
                    link.Input is not (WorkshopSocket.Tie or WorkshopSocket.TieA or WorkshopSocket.TieB))
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported rope socket.");
                if (link.RopeLength is not { } length || !float.IsFinite(length) || length < .05f || length > 200f)
                    throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Rope length must be between 0.05 and 200 metres.");
            }
        }
        _ = RopeTopology.Compile(_items, endpoint =>
            ports.Of(partOf(endpoint.Body)!.Value).ToArray().Single(port => port.Socket == endpoint.Socket).AttachmentRole);
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
