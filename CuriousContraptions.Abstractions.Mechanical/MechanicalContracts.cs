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
