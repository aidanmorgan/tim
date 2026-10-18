using System.Numerics;

namespace CuriousContraptions.Abstractions;

public enum PortDirection : byte
{
    Input = 0,
    Output = 1,
    Bidirectional = 2
}

public readonly record struct PortDescriptor(
    string PortId,
    SimulationDomain Domain,
    PortDirection Direction,
    Vector3 LocalPosition,
    Vector3 Normal);
