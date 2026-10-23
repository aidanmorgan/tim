using System.Collections.Generic;

namespace CuriousContraptions.Abstractions.Electrical;

public enum ElectricalRole : byte
{
    Source = 0,
    Switch = 1,
    Consumer = 2,
    Conductor = 3,
    LogicGate = 4
}

public readonly record struct ElectricalNodeDescriptor(
    string NodeId,
    ElectricalRole Role,
    float RatedVoltage,
    float Resistance,
    float Capacitance,
    float MaxPower);

public interface IElectricalElement : IPuzzleElementDeclaration
{
    IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes { get; }
}

/// <summary>A declared supply route; switched contacts conduct only their own committed closed state.</summary>
public enum SupplyCapabilityKind : byte { Source, SwitchedContact, LogicGate }
/// <summary>The declared law of a supplied two-input gate. And and Or are monotone; Nand, Nor and Xor are not, so none of them
/// may sit on a directed electrical cycle.</summary>
public enum LogicGateOperation : byte { And = 0, Nand = 1, Nor = 2, Or = 3, Xor = 4 }

/// <summary>A Source's finite store, by the keys of the element's own parameters: <see cref="Capacity"/> (Real, Joule),
/// <see cref="PowerLimit"/> (Real, Watt), <see cref="InitialCharge"/> (Real, the fraction of capacity held at Run) and
/// <see cref="Enable"/> (a Choice of exactly <c>Disabled</c>, <c>Enabled</c>). Each range lies inside
/// <see cref="ElectricalSourceEnvelope"/>.</summary>
public readonly record struct SupplySourceStoreDescriptor(string Capacity, string PowerLimit, string InitialCharge, string Enable);

/// <summary>The bounds every declared finite electrical store lies within: the store's runtime domain.</summary>
public static class ElectricalSourceEnvelope
{
    public const float MinimumCapacity = 60f;
    public const float MaximumCapacity = 14400f;
    public const float MinimumPower = 10f;
    public const float MaximumPower = 480f;
}

/// <summary>A declared supply route. A Source declares its finite <see cref="Store"/>; no other kind declares one.</summary>
public readonly record struct SupplyCapabilityDescriptor(
    SupplyCapabilityKind Kind, PortSocket? Input, PortSocket Output, LogicGateOperation? Operation = null,
    SupplySourceStoreDescriptor? Store = null);

public interface ISupplyElement : IElectricalElement
{
    SupplyCapabilityDescriptor Supply { get; }
}
