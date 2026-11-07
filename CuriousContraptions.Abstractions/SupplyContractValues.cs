using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Abstractions.Electrical;

/// <summary>A package-local supply capability, independent of owner identity and public port roles.</summary>
public readonly record struct LocalSupplyNodeId : IComparable<LocalSupplyNodeId>
{
    public string Value { get; }
    public LocalSupplyNodeId(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value; }
    public int CompareTo(LocalSupplyNodeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(LocalSupplyNodeId left, LocalSupplyNodeId right) => left.CompareTo(right) < 0;
    public static bool operator <=(LocalSupplyNodeId left, LocalSupplyNodeId right) => left.CompareTo(right) <= 0;
    public static bool operator >(LocalSupplyNodeId left, LocalSupplyNodeId right) => left.CompareTo(right) > 0;
    public static bool operator >=(LocalSupplyNodeId left, LocalSupplyNodeId right) => left.CompareTo(right) >= 0;
}

public readonly record struct LocalContactWorkId
{
    public string Value { get; }
    public LocalContactWorkId(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value; }
}

public enum SupplyTerminal : byte { Output, PowerIn, FirstIn, SecondIn }
public readonly record struct SupplyEndpoint(LocalSupplyNodeId Node, SupplyTerminal Terminal);
public readonly record struct SupplyConnection(SupplyEndpoint Source, SupplyEndpoint Target);
public readonly record struct SupplyPortBinding(PortId Port, SupplyEndpoint Endpoint);
public sealed record SupplyGraphDescriptor(IReadOnlyList<SupplyNodeDescriptor> Nodes,
    IReadOnlyList<SupplyConnection> Edges, IReadOnlyList<SupplyPortBinding> Ports);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Capability")]
[JsonDerivedType(typeof(SupplySourceDescriptor), "Source")]
[JsonDerivedType(typeof(SupplyContactDescriptor), "Contact")]
[JsonDerivedType(typeof(SupplyLogicDescriptor), "Logic")]
[JsonDerivedType(typeof(SupplyConsumerDescriptor), "Consumer")]
public abstract record SupplyNodeDescriptor(LocalSupplyNodeId Id);
public sealed record SupplySourceDescriptor(LocalSupplyNodeId Id, SupplySourceStoreDescriptor Store) : SupplyNodeDescriptor(Id);
public sealed record SupplyContactDescriptor(LocalSupplyNodeId Id, SignalContactControl Control) : SupplyNodeDescriptor(Id);
public sealed record SupplyLogicDescriptor(LocalSupplyNodeId Id, LogicGateOperation Operation) : SupplyNodeDescriptor(Id);
public sealed record SupplyConsumerDescriptor(LocalSupplyNodeId Id, IReadOnlyList<SupplyTarget> Targets) : SupplyNodeDescriptor(Id);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Target")]
[JsonDerivedType(typeof(SupplyContactWorkTarget), "ContactWork")]
[JsonDerivedType(typeof(SupplyDriveTarget), "Drive")]
public abstract record SupplyTarget;
public sealed record SupplyContactWorkTarget(LocalContactWorkId Store) : SupplyTarget;
public sealed record SupplyDriveTarget(CoordinateReference Coordinate) : SupplyTarget;

/// <summary>The reusable terminal contract; sockets expose endpoints without selecting a product recipe.</summary>
public static class SupplyTerminals
{
    public static bool Admits(SupplyNodeDescriptor node, SupplyTerminal terminal) => node switch
    {
        SupplySourceDescriptor => terminal == SupplyTerminal.Output,
        SupplyContactDescriptor => terminal is SupplyTerminal.Output or SupplyTerminal.PowerIn,
        SupplyLogicDescriptor => Enum.IsDefined(terminal),
        SupplyConsumerDescriptor => terminal == SupplyTerminal.PowerIn,
        _ => false
    };
    public static bool CanConnect(SupplyNodeDescriptor source, SupplyEndpoint output,
        SupplyNodeDescriptor target, SupplyEndpoint input) =>
        output.Terminal == SupplyTerminal.Output && input.Terminal != SupplyTerminal.Output &&
        Admits(source, output.Terminal) && Admits(target, input.Terminal);
}

