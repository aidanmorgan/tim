using System.Numerics;

namespace CuriousContraptions.Abstractions;

public enum PortDirection : byte
{
    Input = 0,
    Output = 1,
    Bidirectional = 2
}

/// <summary>What a signal port carries: an output declares what it emits, an input the strongest signal it admits. A Pulse is one
/// occurrence; a Level is a held value.</summary>
public enum PortSignalKind : byte
{
    Pulse = 1,
    Level = 2
}

/// <summary>One declared socket. <see cref="PortId"/> is the socket's name; <see cref="Signal"/> is declared on every signal port
/// and null on every other port.</summary>
public readonly record struct PortDescriptor(
    string PortId,
    SimulationDomain Domain,
    PortDirection Direction,
    PortSignalKind? Signal,
    Vector3 LocalPosition,
    Vector3 Normal);
