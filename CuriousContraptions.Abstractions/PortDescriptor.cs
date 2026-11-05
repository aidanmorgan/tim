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

public enum PortRole : uint
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
    TieB = 11,
    Drive = 12,
    DriveIn = 13
}

/// <summary>One declared socket, and the only source of its domain, direction, signal kind and anchor position.
/// <see cref="Signal"/> is declared on every signal port and null on every other port; <see cref="LocalPosition"/> is the anchor in
/// the frame of the owning <see cref="Body"/> (metres): the element's local frame for body 0, otherwise that moving body's frame. Its
/// scene locator is a direct child of that body's scene root.</summary>
public readonly record struct PortDescriptor(
    PortId Id,
    SimulationDomain Domain,
    PortDirection Direction,
    PortRole Role,
    PortSignalKind? Signal,
    Vector3 LocalPosition,
    Vector3 Normal,
    RopeAttachmentRole AttachmentRole = RopeAttachmentRole.None,
    PortPositionBinding? PositionBinding = null,
    PackageBodyIndex Body = default,
    float CoordinatePerRadian = 0f,
    float GuideRadius = 0f,
    Vector3 GuideOffset = default);

public enum RopeAttachmentRole : byte { None = 0, Anchor = 1, Load = 2, Guide = 3 }

/// <summary>A linear, package-owned socket position in metres, evaluated from one real parameter.</summary>
public readonly record struct PortPositionBinding(ParameterId Parameter, Vector3 Multiplier, Vector3 Offset)
{
    public Vector3 Evaluate(float value) => value * Multiplier + Offset;
}
