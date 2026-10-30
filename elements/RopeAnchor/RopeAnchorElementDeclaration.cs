using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.RopeAnchor;

public sealed class RopeAnchorElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(Id: "rope_anchor", Category: ElementCategory.Motion, DisplayName: "Rope anchor",
        Description: "A fixed tie-off for a rope. Connect a load to suspend it or make a pendulum.",
        Color: new(.84f, .61f, .28f, 1f), PaletteOrder: 32, IconPath: "res://elements/RopeAnchor/icon.svg",
        ScenePath: "res://elements/RopeAnchor/rope_anchor.tscn", Parameters: []);
    public ElementPresentation Presentation => new(.55f, PresentationAxes.None, [], [], [], []);
    public ElementFootprint Footprint => new(1, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.Tie, SimulationDomain.Rope, PortDirection.Bidirectional, null,
            new(0f, 0f, .18f), Vector3.UnitZ, RopeAttachmentRole.Anchor)
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
