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
/// <summary>The declared law of a supplied two-input gate. And is monotone; Nand is not, so it may never sit on a directed
/// electrical cycle.</summary>
public enum LogicGateOperation : byte { And = 0, Nand = 1 }

public readonly record struct SupplyCapabilityDescriptor(
    SupplyCapabilityKind Kind, PortSocket? Input, PortSocket Output, LogicGateOperation? Operation = null);

public interface ISupplyElement : IElectricalElement
{
    SupplyCapabilityDescriptor Supply { get; }
}
