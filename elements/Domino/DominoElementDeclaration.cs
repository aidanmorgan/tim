using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.Domino;

public sealed class DominoElementDeclaration : IMechanicalElement, IOrientationSensorElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.6f, PresentationAxes.None,
        [], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "domino",
        Category: ElementCategory.Motion,
        DisplayName: "Domino",
        Description: "A physical tile: knock it over to hit the next domino. Its activation output signals once it has turned 45° from its placed pose.",
        Color: new(0.91f, 0.83f, 0.65f, 1f),
        PaletteOrder: 9,
        IconPath: "res://elements/Domino/icon.svg",
        ScenePath: "res://elements/Domino/domino.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(0f, .55f, 0f), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.05f, 0.6f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.TriggerActivated)];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    public PayloadPolicy Payload => PayloadPolicy.None;

    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(
        mass: 0.4f,
        centreOfMass: Vector3.Zero,
        principalMoments: new Vector3((.4f / 3f) * (.55f * .55f + .325f * .325f), (.4f / 3f) * (.125f * .125f + .325f * .325f), (.4f / 3f) * (.125f * .125f + .55f * .55f)),
        principalFrame: Quaternion.Identity,
        linearDrag: 0f,
        gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("toppled", SignalTriggerMode.Edge, Threshold: 0.707f, PulseDurationSeconds: 0.1f)
    ];

    /// <summary>The tile's ActivationOut fires once it has turned 45° from its placed pose (CAT-023).</summary>
    public SignalOrientationDescriptor Orientation => new(45f);
}
