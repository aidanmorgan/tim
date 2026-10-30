using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.ImpactSwitch;

/// <summary>The Impact Switch (CAT-063): a static base and button; a qualifying impact presses the button and sends one activation.
/// Its latched contact passes independently supplied electrical power; activation never creates supply.</summary>
public sealed class ImpactSwitchElementDeclaration : IMechanicalElement, IContactTriggerElement, ISupplyElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.65f, PresentationAxes.None,
        [
            new("Visual/Button", PresentationProperty.LocalY, PresentationDriver.Event, .06f, -.02f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.TriggerActivated),
            new("Visual/Button", PresentationProperty.AlbedoRed, PresentationDriver.Event, .94f, 191f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.TriggerActivated),
            new("Visual/Button", PresentationProperty.AlbedoGreen, PresentationDriver.Event, .43f, 245f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.TriggerActivated),
            new("Visual/Button", PresentationProperty.AlbedoBlue, PresentationDriver.Event, .33f, 176f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.TriggerActivated)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "switch",
        Category: ElementCategory.Power,
        DisplayName: "Impact switch",
        Description: "A qualifying impact sends one activation and closes the independently supplied electrical contact.",
        Color: new(0.94f, 0.43f, 0.33f, 1f),
        PaletteOrder: 3,
        IconPath: "res://elements/ImpactSwitch/icon.svg",
        ScenePath: "res://elements/ImpactSwitch/switch.tscn",
        Parameters:
        [
            new("trigger_threshold", ParameterUnit.MetrePerSecond, ParameterKind.Real, Minimum: 0f, Maximum: 64f, Default: 0.8f, Options: [], EditorPolicy: ParameterEditorPolicy.Clamp)
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(.45f, 0f, .4f), Vector3.UnitZ),
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new(-.58f, 0f, 0f), -Vector3.UnitX),
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new(.58f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The base, then the button.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.TriggerActivated)];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal | SimulationDomain.Electrical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public PayloadPolicy Payload => PayloadPolicy.None;

    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(
        mass: 0f,
        centreOfMass: Vector3.Zero,
        principalMoments: Vector3.Zero,
        principalFrame: Quaternion.Identity,
        linearDrag: 0f,
        gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply);
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes => [new("switched_contact", ElectricalRole.Switch, 12f, .05f, 0f, 100f)];

    public SignalContactDescriptor Contact => new("trigger_threshold");
    public IReadOnlyList<SignalTriggerDescriptor> Triggers => [];
}
