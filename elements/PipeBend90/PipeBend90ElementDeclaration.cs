using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PipeBend90;

/// <summary>The 90° pipe bend (CAT-050): a static hollow torus segment. Its colliders are true hollow shapes (engine-hollow-shapes): the
/// 90° Torus wall (centreline 2.4 m, bore 0.65 m, wall to 0.7 m) between its collars, a 9 cm Tube stub from each collar to its mouth,
/// and the two drawn torus collars. Restitution 0.15 (CAT-050).</summary>
public sealed class PipeBend90ElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(2.2f, PresentationAxes.None,
        [], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "pipe_bend_90",
        Category: ElementCategory.Motion,
        DisplayName: "90° pipe bend",
        Description: "A 90-degree hollow curve. Turns falling balls through a right angle with a smooth hollow bend.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 18,
        IconPath: "res://elements/PipeBend90/icon.svg",
        ScenePath: "res://elements/PipeBend90/pipe_bend_90.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.15f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

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

}
