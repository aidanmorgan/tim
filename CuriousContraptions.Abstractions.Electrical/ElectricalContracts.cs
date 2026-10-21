using System.Collections.Generic;

namespace CuriousContraptions.Abstractions.Electrical;

public enum ElectricalRole : byte
{
    Source = 0,
    Switch = 1,
    Consumer = 2,
    Conductor = 3
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
public enum SupplyCapabilityKind : byte { Source, SwitchedContact }

public readonly record struct SupplyCapabilityDescriptor(
    SupplyCapabilityKind Kind, PortSocket? Input, PortSocket Output);

public interface ISupplyElement : IElectricalElement
{
    SupplyCapabilityDescriptor Supply { get; }
}
