using System.Collections.Generic;
using CuriousContraptions.Abstractions;

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

/// <summary>The declared law of a supplied two-input gate. And and Or are monotone; Nand, Nor and Xor are not, so none of them
/// may sit on a directed electrical cycle.</summary>
public enum LogicGateOperation : byte { And = 0, Nand = 1, Nor = 2, Or = 3, Xor = 4 }

/// <summary>A Source's finite store, by the keys of the element's own parameters: <see cref="Capacity"/> (Real, Joule),
/// <see cref="PowerLimit"/> (Real, Watt), <see cref="InitialCharge"/> (Real, the fraction of capacity held at Run) and
/// <see cref="Enable"/> (a Choice of exactly <c>Disabled</c>, <c>Enabled</c>). Each range lies inside
/// the finite positive f32 domain.</summary>
public readonly record struct SupplySourceStoreDescriptor(ParameterId Capacity, ParameterId PowerLimit, ParameterId InitialCharge, ParameterId Enable);

