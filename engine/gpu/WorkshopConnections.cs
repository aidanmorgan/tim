using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public enum WorkshopConnectionDomain : uint { Activation = 1, Electrical = 2, Rope = 3, Mechanical = 4 }
public enum WorkshopPortDirection : uint { Input = 1, Output = 2, Bidirectional = 3 }
/// <summary>What an activation port carries (Story 9R.5): an output declares what it emits, an input the strongest signal it admits.
/// A Pulse is one occurrence; a Level is a committed high/low value (the latch's memory, a declared sensor's held contact load). An input that
/// admits a Level also admits a Pulse.</summary>
public enum WorkshopSignalKind : uint { Pulse = 1, Level = 2 }
/// <summary>One declared socket, admitted from the element bundle (<see cref="ElementPortCatalogue"/>). Signal is declared on every
/// activation port and absent (null) on every electrical port; LocalPosition is the declared f32 anchor in the part's local frame
/// (metres), the wire artwork's endpoint.</summary>
public readonly record struct WorkshopPort(PortId Id, PortRole Role, WorkshopConnectionDomain Domain, WorkshopPortDirection Direction,
    WorkshopSignalKind? Signal, Vector3 LocalPosition, RopeAttachmentRole AttachmentRole = RopeAttachmentRole.None, PortPositionBinding? PositionBinding = null,
    CuriousContraptions.Abstractions.PackageBodyIndex Body = default,
    float GuideRadius = 0f,
    Vector3 GuideOffset = default,
    CoordinateReference? Coordinate = null,
    float CoordinatePerRadian = 0f);
public readonly record struct WorkshopConnection(GpuBodyId Source, PortId Output, GpuBodyId Target,
    PortId Input, WorkshopConnectionDomain Domain, float? RopeLength = null);

/// <summary>Immutable authored edges; neither storage nor connection validation executes activation. There is no connection limit
/// (Story 9R.5b).</summary>
public sealed class WorkshopConnections : IReadOnlyList<WorkshopConnection>, IEquatable<WorkshopConnections>
{
    private readonly WorkshopConnection[] _items;
    public static WorkshopConnections Empty { get; } = new();
    public WorkshopConnections(params WorkshopConnection[] items) => _items = (WorkshopConnection[])items.Clone();
    public static WorkshopConnection MeasureRope(WorkshopInstances instances, GpuBodyId source, GpuBodyId target, PortId output = default, PortId input = default)
    {
        output = output == default ? DefaultRopePort(source) : output;
        input = input == default ? DefaultRopePort(target) : input;
        Vector3 Attachment(GpuBodyId owner, PortId portId)
        {
            if (instances.SingleOrDefault(p => p.Id == owner) is not WorkshopGenericInstance instance)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Absent rope endpoint.");
            var local = DeclaredPorts.Current.LocalPosition(instance, portId);
            return new Vector3((instance.Cell.X + instance.Local.X) / 16f, (instance.Cell.Y + instance.Local.Y) / 16f,
                (instance.Cell.Z + instance.Local.Z) / 16f) + Vector3.Transform(local,
                new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W));
        }
        PortId DefaultRopePort(GpuBodyId owner)
        {
            if (instances.SingleOrDefault(p => p.Id == owner) is not WorkshopGenericInstance instance)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Absent rope endpoint.");
            foreach (var p in DeclaredPorts.Current.Of(instance.PartId))
                if (p.Domain == WorkshopConnectionDomain.Rope) return p.Id;
            return PortId.Tie;
        }
        var link = new WorkshopConnection(source, output, target, input, WorkshopConnectionDomain.Rope,
            Vector3.Distance(Attachment(source, output), Attachment(target, input)));
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
        _ = new WorkshopSignalGraph(instances, this);
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
        var mechanicalOutputs = new HashSet<(GpuBodyId, CoordinateReference)>();
        var mechanicalInputs = new HashSet<(GpuBodyId, CoordinateReference)>();
        foreach (var link in _items)
        {
            ValidateIdentity(link, seen);
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
            var outRole = ports.Role(source.Value, link.Output);
            var inRole = ports.Role(target.Value, link.Input);
            if (link.Domain == WorkshopConnectionDomain.Activation)
                ValidateActivation(ports, source.Value, target.Value, link, outRole, inRole);
            else if (link.Domain == WorkshopConnectionDomain.Electrical)
                ValidateElectrical(target.Value, link, outRole, inRole, seen);
            else if (link.Domain == WorkshopConnectionDomain.Rope)
                ValidateRope(link, outRole, inRole);
            else if (link.Domain == WorkshopConnectionDomain.Mechanical)
                ValidateMechanical(ports, source.Value, target.Value, link, outRole, inRole, mechanicalOutputs, mechanicalInputs);
        }
        _ = RopeTopology.Compile(_items, endpoint =>
            ports.Of(partOf(endpoint.Body)!.Value).ToArray().Single(port => port.Id == endpoint.Socket).AttachmentRole);
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
    private static void ValidateIdentity(WorkshopConnection link, HashSet<WorkshopConnection> seen)
    {
        if (link.Source.Value == 0 || link.Target.Value == 0 || (link.Source == link.Target && link.Domain != WorkshopConnectionDomain.Activation) ||
            string.IsNullOrWhiteSpace(link.Output.Value) || string.IsNullOrWhiteSpace(link.Input.Value) ||
            !Enum.IsDefined(link.Domain) || !seen.Add(link))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported or duplicate connection.");
    }

    private static void ValidateActivation(ElementPortCatalogue ports, PartId source, PartId target, WorkshopConnection link, PortRole outRole, PortRole inRole)
    {
        if (outRole != PortRole.ActivationOut)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported activation output.");
        if (inRole is not (PortRole.ActivationIn or PortRole.SetIn or PortRole.ResetIn))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported activation input.");
        // A level output (the latch, a declared contact-load sensor) drives only an input that admits a level (a declared indicator input), and such an
        // input then follows that one level alone. The rule reads the declared port kinds, never the part identity.
        if (ports.Signal(source, link.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level &&
            ports.Signal(target, link.Input, WorkshopPortDirection.Input) != WorkshopSignalKind.Level)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.LevelDrivesLevelInputOnly, "A level output can only light a Signal lamp.");
    }

    private static void ValidateElectrical(PartId target, WorkshopConnection link, PortRole outRole, PortRole inRole, HashSet<WorkshopConnection> seen)
    {
        if (outRole != PortRole.Supply || inRole is not (PortRole.PowerIn or PortRole.FirstIn or PortRole.SecondIn))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported electrical socket.");
        foreach (var previous in seen)
        {
            if (previous != link && previous.Source == link.Source && previous.Target == link.Target && previous.Input == link.Input && previous.Domain == WorkshopConnectionDomain.Electrical)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Duplicate electrical connection from same source to same input.");
            if (previous != link && previous.Target == link.Target && previous.Input == link.Input &&
                previous.Domain == WorkshopConnectionDomain.Electrical &&
                DeclaredPhysics.Of(target).Electrical?.Supply?.Kind is not (SupplyCapabilityKind.SwitchedContact or SupplyCapabilityKind.LogicGate))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied, "This power input already has a supplier.");
        }
    }

    private static void ValidateRope(WorkshopConnection link, PortRole outRole, PortRole inRole)
    {
        if (outRole is not (PortRole.Tie or PortRole.TieA or PortRole.TieB) ||
            inRole is not (PortRole.Tie or PortRole.TieA or PortRole.TieB))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported rope socket.");
        if (link.RopeLength is not { } length || !float.IsFinite(length) || length < .05f || length > 200f)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Rope length must be between 0.05 and 200 metres.");
    }

    private static void ValidateMechanical(ElementPortCatalogue ports, PartId source, PartId target, WorkshopConnection link, PortRole outRole, PortRole inRole,
        HashSet<(GpuBodyId, CoordinateReference)> mechanicalOutputs, HashSet<(GpuBodyId, CoordinateReference)> mechanicalInputs)
    {
        if (outRole != PortRole.Drive || inRole != PortRole.DriveIn)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Unsupported mechanical socket.");
        var output = ports.Of(source).ToArray().Single(port => port.Id == link.Output);
        var input = ports.Of(target).ToArray().Single(port => port.Id == link.Input);
        var sourceCoordinate = MechanicalCoordinate(source, output);
        var targetCoordinate = MechanicalCoordinate(target, input);
        var ratio = output.CoordinatePerRadian / input.CoordinatePerRadian;
        if (!float.IsFinite(ratio) || ratio == 0f)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Mechanical ratio must be representable, finite and nonzero.");
        if (!mechanicalOutputs.Add((link.Source, sourceCoordinate)) || !mechanicalInputs.Add((link.Target, targetCoordinate)))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Duplicate mechanical drive connection to the same coordinate role.");
    }

    internal static CoordinateReference MechanicalCoordinate(PartId part, WorkshopPort port)
    {
        if (port.Coordinate is not { Kind: CoordinateKind.Revolute } coordinate ||
            coordinate.Index < 0 || coordinate.Index >= (DeclaredPhysics.Of(part).Mechanical?.Hinges.Count ?? 0) ||
            !float.IsFinite(port.CoordinatePerRadian) || port.CoordinatePerRadian == 0f)
            throw new WorkshopEditRejectedException(WorkshopEditRejection.InvalidConnection, "Mechanical endpoint requires an existing revolute coordinate and finite nonzero scale.");
        return coordinate;
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
