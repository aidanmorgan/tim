using System;
using System.Collections.Generic;
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

/// <summary>A dynamic body whose compiled mass is a declared Real kilogram parameter (the body mass bound to a parameter). The fixed
/// <see cref="RigidBodyMassProperties.Mass"/> is that parameter's default, so one truth holds at defaults; the compiler evaluates
/// <see cref="Mass"/> per instance. Inertia follows from the compiled mass and the (possibly parameter-bound) collider.</summary>
public interface IParameterizedMassElement : IMechanicalElement
{
    MechanicalValue Mass { get; }
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

/// <summary>A bounded drive on one slider, enabled while the element's PowerIn supply route is closed. It moves the slider toward
/// <see cref="Target"/> (m, within the slider's limits) at the stopping-profile speed sign(d)·min(<see cref="MaxSpeed"/>,
/// √(2·<see cref="StoppingAcceleration"/>·|d|)), changing speed by at most <see cref="StoppingAcceleration"/> (m/s²), holding
/// against the slider's own spring and the moving body's gravity, with its force bounded by <see cref="MaxEffort"/> (N) and its
/// power by <see cref="MaxPower"/> (W).</summary>
public readonly record struct PrismaticDriveDescriptor(
    MechanicalValue Target,
    MechanicalValue MaxSpeed,
    MechanicalValue StoppingAcceleration,
    MechanicalValue MaxEffort,
    MechanicalValue MaxPower);

/// <summary>One prismatic slider joining the element's base body to a moving body. At rest the two frames coincide; the moving body
/// may travel from <see cref="LowerLimit"/> to <see cref="UpperLimit"/> along the base frame's local Y. The axial spring's stiffness
/// and damping, and the starting compression (how far below rest the moving body starts), are the element parameters these keys
/// name. <see cref="Drive"/> is the slider's supplied drive, or null for a passive slider.</summary>
public readonly record struct PrismaticSliderDescriptor(
    SliderFrame BaseFrame,
    SliderFrame MovingFrame,
    float LowerLimit,
    float UpperLimit,
    MechanicalValue Stiffness,
    MechanicalValue Damping,
    MechanicalValue Compression,
    bool BodiesCollide,
    SliderBodyDescriptor MovingBody,
    PrismaticDriveDescriptor? Drive);

/// <summary>A mechanical element whose base body carries moving bodies on prismatic sliders.</summary>
public interface ISliderElement : IMechanicalElement
{
    IReadOnlyList<PrismaticSliderDescriptor> Sliders { get; }
}

/// <summary>A mechanical value is either fixed authoring data or a declared real parameter.</summary>
public enum MechanicalValueSource : byte { Constant, Parameter }

public readonly record struct MechanicalValue(MechanicalValueSource Source, float Constant, string? Parameter)
{
    public static MechanicalValue Fixed(float value) => new(MechanicalValueSource.Constant, value, null);
    public static MechanicalValue FromParameter(string key) => new(MechanicalValueSource.Parameter, 0f, key);

    public float Evaluate(IReadOnlyDictionary<string, float> parameters) => Source switch
    {
        MechanicalValueSource.Constant when Parameter is null && float.IsFinite(Constant) => Constant,
        MechanicalValueSource.Parameter when Constant == 0f && Parameter is { } key &&
            parameters.TryGetValue(key, out var value) && float.IsFinite(value) => value,
        _ => throw new ArgumentException("Invalid or unresolved mechanical value.")
    };
}

/// <summary>Package-owned residence volume. UpperMargin extends its local Y maximum; remaining values bind declared parameters.</summary>
public readonly record struct ResidenceCaptureDescriptor(
    Vector3 Minimum, Vector3 Maximum, MechanicalValue UpperMargin,
    MechanicalValue SpeedLimit, MechanicalValue Dwell, string ParticipationParameter,
    PlanarGuideDescriptor Guide);

/// <summary>A bounded lateral guide in the same owner frame as its residence volume.</summary>
public readonly record struct PlanarGuideDescriptor(
    Vector3 Minimum, Vector3 Maximum, float SupportHeight,
    MechanicalValue SupportMargin, MechanicalValue MaximumAcceleration);

public interface IResidenceCaptureElement : IMechanicalElement
{
    ResidenceCaptureDescriptor Capture { get; }
}

/// <summary>A finite contact-work store on the element's static body. Its target speed is the named Real m/s parameter; its preload is
/// the kinetic energy (½·ReferenceMass)·s·s of the reference mass at that speed, so the parameter range bounds the store. A contact
/// approaching faster than <see cref="Threshold"/> qualifies once per target every <see cref="CooldownPhysicalSteps"/> physical
/// steps.</summary>
public readonly record struct ContactWorkDescriptor(
    string StrengthParameter, float ReferenceMass, float Threshold, uint CooldownPhysicalSteps);

public interface IContactWorkElement : IMechanicalElement
{
    ContactWorkDescriptor ContactWork { get; }
}
