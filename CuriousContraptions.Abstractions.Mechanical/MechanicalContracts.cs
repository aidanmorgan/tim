using System.Numerics;

namespace CuriousContraptions.Abstractions.Mechanical;

public enum KinematicMode : byte
{
    Static = 0,
    Dynamic = 1,
    Kinematic = 2
}

public readonly record struct RigidBodyMassProperties(
    float Mass,
    Vector3 CentreOfMass,
    Vector3 InertiaTensor,
    float LinearDrag,
    Vector3 Gravity);

public readonly record struct AnchorPointDescriptor(
    string Name,
    Vector3 LocalPosition,
    Quaternion LocalRotation,
    Vector3 Axis);

public interface IMechanicalElement : IPuzzleElementDeclaration
{
    KinematicMode KinematicMode { get; }
    RigidBodyMassProperties MassProperties { get; }
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints { get; }
}

/// <summary>A joint frame in one body's local space. Local Y is the slide axis.</summary>
public readonly record struct SliderFrame(
    Vector3 Position,
    Quaternion Rotation);

/// <summary>The moving body a slider carries, in its own local frame: its mass, gravity, linear drag and colliders.</summary>
public readonly record struct SliderBodyDescriptor(
    float Mass,
    Vector3 Gravity,
    float LinearDrag,
    IReadOnlyList<ColliderDescriptor> Colliders);

/// <summary>One prismatic slider joining the element's base body to a moving body. At rest the two frames coincide; the moving body
/// may travel from <see cref="LowerLimit"/> to <see cref="UpperLimit"/> along the base frame's local Y. The axial spring's stiffness
/// and damping, and the starting compression (how far below rest the moving body starts), are the element parameters these keys
/// name.</summary>
public readonly record struct PrismaticSliderDescriptor(
    SliderFrame BaseFrame,
    SliderFrame MovingFrame,
    float LowerLimit,
    float UpperLimit,
    string StiffnessParameter,
    string DampingParameter,
    string CompressionParameter,
    bool BodiesCollide,
    SliderBodyDescriptor MovingBody);

/// <summary>A mechanical element whose base body carries moving bodies on prismatic sliders.</summary>
public interface ISliderElement : IMechanicalElement
{
    IReadOnlyList<PrismaticSliderDescriptor> Sliders { get; }
}
