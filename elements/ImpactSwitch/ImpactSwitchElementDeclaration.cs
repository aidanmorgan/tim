using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.ImpactSwitch;

/// <summary>The Impact Switch (CAT-063): a static base and button; a qualifying impact presses the button and sends one activation.
/// Its latched contact passes independently supplied electrical power; activation never creates supply.</summary>
public sealed class ImpactSwitchElementDeclaration : IMechanicalElement, ISignalElement, ISupplyElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.65f, PresentationAxes.None,
        [
            new("Visual/Button", PresentationProperty.LocalY, PresentationDriver.Event, PresentationValueMapping.Identity, .06f, -.02f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, Event: SimulationEventKind.TriggerActivated, SignalNode: new("signal")),
            new("Visual/Button", PresentationProperty.AlbedoRed, PresentationDriver.Event, PresentationValueMapping.Identity, .94f, 191f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, Event: SimulationEventKind.TriggerActivated, SignalNode: new("signal")),
            new("Visual/Button", PresentationProperty.AlbedoGreen, PresentationDriver.Event, PresentationValueMapping.Identity, .43f, 245f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, Event: SimulationEventKind.TriggerActivated, SignalNode: new("signal")),
            new("Visual/Button", PresentationProperty.AlbedoBlue, PresentationDriver.Event, PresentationValueMapping.Identity, .33f, 176f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, Event: SimulationEventKind.TriggerActivated, SignalNode: new("signal"))
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
        new(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, new Vector3(.45f, 0f, .4f), Vector3.UnitZ),
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new(-.58f, 0f, 0f), -Vector3.UnitX),
        new(PortId.Supply, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, new(.58f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The base, then the button.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.TriggerActivated, new("signal"))];

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

    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyContactDescriptor(new("contact"), new(new("signal"), SignalContactState.Latched))], [],
        [
            new(PortId.PowerIn, new(new("contact"), SupplyTerminal.PowerIn)),
            new(PortId.Supply, new(new("contact"), SupplyTerminal.Output))
        ]);
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes => [new("switched_contact", ElectricalRole.Switch, 12f, .05f, 0f, 100f)];

    public IReadOnlyList<SignalNodeDescriptor> SignalNodes => [new SignalContactDescriptor(new("signal"), new(0), new("trigger_threshold"))];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts =>
    [
        new(PortId.ActivationOut, new(new("signal"), SignalTerminal.Output))
    ];
}
