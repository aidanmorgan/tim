using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Bumper;

/// <summary>The Pinball Bumper (CAT-015): a static sphere whose finite contact-work store kicks a qualifying ball radially. Its one
/// typed parameter, strength, is the store's target speed; the preload is a 1 kg reference mass at that speed (32 J at the default
/// 8 m/s, 200 J at 20 m/s). The editor clamps a finite strength into 0–20 m/s.</summary>
public sealed class BumperElementDeclaration : IContactWorkElement, IElectricalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.8f, PresentationAxes.None,
        [
            new("Visual/BumperHead", PresentationProperty.UniformScale, PresentationDriver.ContactImpulse, 1f, .88f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, PresentationTimerPhase.None),
            new("Visual/ImpactRing", PresentationProperty.UniformScale, PresentationDriver.ContactImpulse, 1f, 1.24f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, PresentationTimerPhase.None),
            new("Visual/ImpactRing", PresentationProperty.AlbedoRed, PresentationDriver.ContactImpulse, 1f, 247f / 255f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, PresentationTimerPhase.None),
            new("Visual/ImpactRing", PresentationProperty.AlbedoGreen, PresentationDriver.ContactImpulse, 240f / 255f, 203f / 255f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, PresentationTimerPhase.None),
            new("Visual/ImpactRing", PresentationProperty.AlbedoBlue, PresentationDriver.ContactImpulse, 194f / 255f, 82f / 255f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, PresentationTimerPhase.None)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "bumper",
        Category: ElementCategory.Motion,
        DisplayName: "Pinball bumper",
        Description: "A round spring-loaded bumper kicks a ball away from its point of impact.",
        Color: new(0.87f, 0.44f, 0.35f, 1f),
        PaletteOrder: 7,
        IconPath: "res://elements/Bumper/icon.svg",
        ScenePath: "res://elements/Bumper/bumper.tscn",
        Parameters:
        [
            new("strength", ParameterUnit.MetrePerSecond, ParameterKind.Real, Minimum: 0f, Maximum: 20f, Default: 8f, Options: [], EditorPolicy: ParameterEditorPolicy.Clamp)
        ]);

    public ContactWorkDescriptor ContactWork => new("strength", ReferenceMass: 1f, Threshold: .05f, CooldownPhysicalSteps: 72);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(0f, 0f, .65f), Vector3.UnitZ)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Impulse:f32;Contact:vec3;Normal:vec3"),
        new(SimulationEventKind.ContactLoad, "Load:f32;BodyId:u64")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("coil", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 10f, Capacitance: 0f, MaxPower: 50f)
    ];
}
