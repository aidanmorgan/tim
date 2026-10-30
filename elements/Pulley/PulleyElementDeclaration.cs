using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Pulley;

public sealed class PulleyElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(Id: "pulley", Category: ElementCategory.Motion, DisplayName: "Pulley",
        Description: "Redirect a continuous rope. Connect both sides to loads or anchors; unfinished ropes cannot carry tension.",
        Color: new(.84f, .61f, .28f, 1f), PaletteOrder: 33, IconPath: "res://elements/Pulley/icon.svg",
        ScenePath: "res://elements/Pulley/pulley.tscn", Parameters: []);
    public ElementPresentation Presentation => new(.65f, PresentationAxes.None,
        [
            new("Visual/Wheel", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedWheelAngle,
                0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None)
        ], [], [], []);
    public ElementFootprint Footprint => new(1, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.Tie, SimulationDomain.Rope, PortDirection.Bidirectional, null,
            new(0f, .4f, .12f), Vector3.UnitY, RopeAttachmentRole.Guide)
    ];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(1f, .3f, 0f, .1f))];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Rope;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero,
        Quaternion.Identity, 0f, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
