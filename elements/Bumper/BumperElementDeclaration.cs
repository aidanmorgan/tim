using CuriousContraptions.Presentation;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.Bumper;

/// <summary>The Pinball Bumper (CAT-015): a static compound head, upper ring and cap whose finite contact-work store kicks a qualifying ball radially. Its one
/// typed parameter, strength, is the store's target speed; the preload is a 1 kg reference mass at that speed (32 J at the default
/// 8 m/s, 200 J at 20 m/s). The editor clamps a finite strength into 0–20 m/s.</summary>
public sealed class BumperElementDeclaration : IContactWorkElement, IEnergyStoreElement, ISupplyElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.8f, PresentationAxes.None,
        [
            new("Visual/BumperHead", PresentationProperty.UniformScale, PresentationDriver.Event, PresentationValueMapping.Identity, 1f, .88f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, Event: SimulationEventKind.Impact, EventMagnitude: PresentationEventMagnitude.Positive, ImpulsePeak: new(.5f), ImpulseVisibility: AnimationImpulseVisibility.AdvanceWhileHidden, ImpulseTiming: AnimationImpulseTiming.EventTime),
            new("Visual/ImpactRing", PresentationProperty.UniformScale, PresentationDriver.Event, PresentationValueMapping.Identity, 1f, 1.24f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, Event: SimulationEventKind.Impact, EventMagnitude: PresentationEventMagnitude.Positive, ImpulsePeak: new(.5f), ImpulseVisibility: AnimationImpulseVisibility.AdvanceWhileHidden, ImpulseTiming: AnimationImpulseTiming.EventTime),
            new("Visual/ImpactRing", PresentationProperty.AlbedoRed, PresentationDriver.Event, PresentationValueMapping.Identity, 1f, 247f / 255f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, Event: SimulationEventKind.Impact, EventMagnitude: PresentationEventMagnitude.Positive, ImpulsePeak: new(.5f), ImpulseVisibility: AnimationImpulseVisibility.AdvanceWhileHidden, ImpulseTiming: AnimationImpulseTiming.EventTime),
            new("Visual/ImpactRing", PresentationProperty.AlbedoGreen, PresentationDriver.Event, PresentationValueMapping.Identity, 240f / 255f, 203f / 255f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, Event: SimulationEventKind.Impact, EventMagnitude: PresentationEventMagnitude.Positive, ImpulsePeak: new(.5f), ImpulseVisibility: AnimationImpulseVisibility.AdvanceWhileHidden, ImpulseTiming: AnimationImpulseTiming.EventTime),
            new("Visual/ImpactRing", PresentationProperty.AlbedoBlue, PresentationDriver.Event, PresentationValueMapping.Identity, 194f / 255f, 82f / 255f, PresentationCurve.SineSquaredPulse, .32f, PresentationOverlap.SaturatingSum, 0f, Event: SimulationEventKind.Impact, EventMagnitude: PresentationEventMagnitude.Positive, ImpulsePeak: new(.5f), ImpulseVisibility: AnimationImpulseVisibility.AdvanceWhileHidden, ImpulseTiming: AnimationImpulseTiming.EventTime)
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

    public IReadOnlyList<ContactWorkDescriptor> ContactWorks =>
        [new(new("work"), new(0), new(1), MechanicalValue.FromParameter("strength"), .05f, 72)];

    public IReadOnlyList<EnergyStoreDescriptor> EnergyStores =>
        [new(new(1), new(0), Preload, Preload, MechanicalValue.Fixed(float.MaxValue))];

    private static EnergyExpression Preload => EnergyExpression.Kinetic(MechanicalValue.Fixed(1f), MechanicalValue.FromParameter("strength"));

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, new("PowerIn", SocketGlyph.Text), null, new Vector3(0f, 0f, .65f), Vector3.UnitZ)
    ];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.Impact)];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

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
        [new SupplyConsumerDescriptor(new("consumer"), [new SupplyEnergyStoreTarget(new(1))])], [],
        [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("coil", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 10f, Capacitance: 0f, MaxPower: 50f)
    ];
}
