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
    Vector3 PrincipalMoments,
    Quaternion PrincipalFrame,
    float LinearDrag,
    Vector3 Gravity)
{

    public void Validate(KinematicMode mode)
    {
        if (!Enum.IsDefined(mode) || mode == KinematicMode.Kinematic)
            throw new ArgumentException("Unsupported mass-frame motion.");
        if (!Finite(CentreOfMass) || !Finite(PrincipalMoments) || !Finite(Gravity) ||
            !float.IsFinite(Mass) || !float.IsFinite(LinearDrag) ||
            !float.IsFinite(PrincipalFrame.X) || !float.IsFinite(PrincipalFrame.Y) ||
            !float.IsFinite(PrincipalFrame.Z) || !float.IsFinite(PrincipalFrame.W) ||
            MathF.Abs(PrincipalFrame.LengthSquared() - 1f) > 1e-4f ||
            Vector3.Abs(CentreOfMass).X > 16f || Vector3.Abs(CentreOfMass).Y > 16f || Vector3.Abs(CentreOfMass).Z > 16f)
            throw new ArgumentException("Invalid mass frame.");
        if (mode == KinematicMode.Static)
        {
            if (Mass != 0f || CentreOfMass != Vector3.Zero || PrincipalMoments != Vector3.Zero ||
                PrincipalFrame != Quaternion.Identity || Gravity != Vector3.Zero || LinearDrag != 0f)
                throw new ArgumentException("Static mass frames must be canonical zero.");
            return;
        }
        if (Mass < 1f / 1024f || Mass > 1024f || LinearDrag < 0f || LinearDrag > .125f || Gravity.LengthSquared() > 256f)
            throw new ArgumentException("Mass or forces outside the physical domain.");
        var p = PrincipalMoments;
        const float minimum = 1f / 4294967296f;
        if (p.X < minimum || p.Y < minimum || p.Z < minimum || p.X >= 524288f || p.Y >= 524288f || p.Z >= 524288f ||
            p.X > p.Y + p.Z || p.Y > p.X + p.Z || p.Z > p.X + p.Y)
            throw new ArgumentException("Principal moments must describe a physical mass distribution.");
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public readonly record struct AnchorPointDescriptor(
    string Name,
    Vector3 LocalPosition,
    Quaternion LocalRotation,
    Vector3 Axis);

public interface IMechanicalElement : IPuzzleElementDeclaration
{
    KinematicMode KinematicMode { get; }
    MassFrameDeclaration MassFrame { get; }
    PayloadPolicy Payload { get; }
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints { get; }
}

/// <summary>A joint frame in one body's local space. Local Y is the slide axis.</summary>
public readonly record struct SliderFrame(
    Vector3 Position,
    Quaternion Rotation);

/// <summary>The moving body a slider carries, in its own local frame: its mass, gravity, linear drag and colliders.</summary>
public readonly record struct SliderBodyDescriptor(
    MassFrameDeclaration MassFrame,
    IReadOnlyList<ColliderDescriptor> Colliders);

/// <summary>A bounded drive on one slider, enabled while the element's PowerIn supply route is closed. It moves the slider toward
/// <see cref="Target"/> (m, within the slider's limits) at the stopping-profile speed sign(d)·min(<see cref="MaxSpeed"/>,
/// √(2·<see cref="StoppingAcceleration"/>·|d|), |d|/h) for a substep h, changing speed by at most <see cref="StoppingAcceleration"/> (m/s²), holding
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

/// <summary>A hinge joint frame in one body's local space. Local Z is the hinge rotation axis.</summary>
public readonly record struct HingeFrame(
    Vector3 Position,
    Quaternion Rotation);

/// <summary>The moving body a hinge carries, in its own local frame: its mass, gravity, linear drag and colliders.</summary>
public readonly record struct HingeBodyDescriptor(
    MassFrameDeclaration MassFrame,
    IReadOnlyList<ColliderDescriptor> Colliders);

/// <summary>One revolute hinge joining the element's base body to a moving body. At rest the two frames coincide; the moving body
/// may rotate from <see cref="LowerLimit"/> to <see cref="UpperLimit"/> (radians) around the base frame's local Z. The axial spring's stiffness
/// and damping, and the starting angle, are the element parameters these keys name.</summary>
public readonly record struct RevoluteHingeDescriptor(
    HingeFrame BaseFrame,
    HingeFrame MovingFrame,
    float LowerLimit,
    float UpperLimit,
    MechanicalValue Stiffness,
    MechanicalValue Damping,
    MechanicalValue InitialAngle,
    bool BodiesCollide,
    HingeBodyDescriptor MovingBody);

/// <summary>A mechanical element whose base body carries moving bodies on revolute hinges.</summary>
public interface IHingeElement : IMechanicalElement
{
    IReadOnlyList<RevoluteHingeDescriptor> Hinges { get; }
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
