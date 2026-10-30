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

/// <summary>The closed set of sockets an element can declare (Story 9A.17). A scene's socket locator is named
/// <c>Loc_Port_&lt;PortSocket&gt;</c> exactly. The numbers are the sockets' wire and save values.</summary>
public enum PortSocket : uint
{
    ActivationOut = 1,
    SetIn = 2,
    ResetIn = 3,
    ActivationIn = 4,
    FirstIn = 5,
    SecondIn = 6,
    Supply = 7,
    PowerIn = 8,
    Tie = 9,
    TieA = 10,
    TieB = 11
}

/// <summary>One declared socket, and the only source of its domain, direction, signal kind and anchor position (Story 9A.17).
/// <see cref="Signal"/> is declared on every signal port and null on every other port; <see cref="LocalPosition"/> is the anchor in
/// the element's local frame (metres).</summary>
public readonly record struct PortDescriptor(
    PortSocket Socket,
    SimulationDomain Domain,
    PortDirection Direction,
    PortSignalKind? Signal,
    Vector3 LocalPosition,
    Vector3 Normal,
    RopeAttachmentRole AttachmentRole = RopeAttachmentRole.None,
    PortPositionBinding? PositionBinding = null,
    PackageBodyIndex Body = default);

public enum RopeAttachmentRole : byte { None = 0, Anchor = 1, Load = 2, Guide = 3 }

/// <summary>A linear, package-owned socket position in metres, evaluated from one real parameter.</summary>
public readonly record struct PortPositionBinding(ParameterId Parameter, Vector3 Multiplier, Vector3 Offset)
{
    public Vector3 Evaluate(float value) => value * Multiplier + Offset;
}
