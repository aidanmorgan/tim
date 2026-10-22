using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Receiver;

/// <summary>The Receiver (CAT-004): a static open box of five walls. Its capture settings and its guiding force region are typed
/// parameters with the runtime's defaults and admitted ranges.</summary>
public sealed class ReceiverElementDeclaration : IResidenceCaptureElement, ISceneGeometryElement
{

    public ResidenceCaptureDescriptor Capture => new(
        new(-.66f,-.4f,-.66f), new(.66f,.45f,.66f),
        MechanicalValue.FromParameter("capture_margin"),
        MechanicalValue.FromParameter("capture_speed_limit"),
        MechanicalValue.FromParameter("capture_dwell"), "capture_participation",
        new(new(-1.1f,.5f,-1.1f),new(1.1f,1.5f,1.1f),.5f,
            MechanicalValue.FromParameter("force_support_margin"),
            MechanicalValue.FromParameter("force_maximum_acceleration")));

    public ElementPresentation Presentation => new(.85f, PresentationAxes.None,
        [
            new("Visual/Visual_Halo", PresentationProperty.UniformScale, PresentationDriver.CapturePulse, 1.02f, 1.02f, PresentationCurve.SmoothStep, .5f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Visual_Halo", PresentationProperty.AlbedoRed, PresentationDriver.CapturePulse, 189f / 255f, 1f, PresentationCurve.SmoothStep, .5f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Visual_Halo", PresentationProperty.AlbedoGreen, PresentationDriver.CapturePulse, 244f / 255f, 1f, PresentationCurve.SmoothStep, .5f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Visual_Halo", PresentationProperty.AlbedoBlue, PresentationDriver.CapturePulse, 189f / 255f, 1f, PresentationCurve.SmoothStep, .5f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "basket",
        Category: ElementCategory.Goals,
        DisplayName: "Receiver",
        Description: "Catch and hold the named ball. The halo shows the current tolerance.",
        Color: new(0.29f, 0.67f, 0.58f, 1f),
        PaletteOrder: 2,
        IconPath: "res://elements/Receiver/icon.svg",
        ScenePath: "res://elements/Receiver/basket.tscn",
        Parameters:
        [
            new("capture_margin", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0f, Maximum: 1f, Default: 0.02f, Options: []),
            new("capture_speed_limit", ParameterUnit.MetrePerSecond, ParameterKind.Real, Minimum: 1f / 1024f, Maximum: 64f, Default: 1.5f, Options: []),
            new("capture_dwell", ParameterUnit.Second, ParameterKind.Real, Minimum: 1f / 1024f, Maximum: 30f, Default: 0.35f, Options: []),
            new("capture_participation", ParameterUnit.None, ParameterKind.Choice, Minimum: 0f, Maximum: 1f, Default: 1f, Options: ["Disabled", "Enabled"]),
            new("force_support_margin", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0f, Maximum: 1f, Default: 0f, Options: []),
            new("force_maximum_acceleration", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, Minimum: 0f, Maximum: 12f, Default: 0f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The floor, the two side walls, the back wall and the lower front lip.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.12f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

}
