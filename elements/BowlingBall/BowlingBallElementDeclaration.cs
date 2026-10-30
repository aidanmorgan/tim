using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.BowlingBall;

/// <summary>The Bowling ball (CAT-014): a 0.28 m, 4 kg hard shell with low bounce (owner decision, 9 Oct 2026). Its whole ball material
/// is declared here: radius as the sphere's half-extent, mass and drag as mass properties, and bounce, friction, bounce threshold and
/// rolling resistance as the collider material. It is not buoyant.</summary>
public sealed class BowlingBallElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.4f, PresentationAxes.None,
        [], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "bowling",
        Category: ElementCategory.Motion,
        DisplayName: "Bowling ball",
        Description: "A heavy, low-bounce ball: four times the Basketball's mass and smaller.",
        Color: new(0.27f, 0.39f, 0.61f, 1f),
        PaletteOrder: 10,
        IconPath: "res://elements/BowlingBall/icon.svg",
        ScenePath: "res://elements/BowlingBall/bowling.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.14f, 0.3f, 0.03f, 0.1f))
    ];

    /// <summary>A ball owns no event row: the impacts it takes are its strikers' records (Story 9A.22).</summary>
    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    /// <summary>A solid sphere's inertia, 2/5 m r².</summary>
    public PayloadPolicy Payload => PayloadPolicy.CaptureTarget;

    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(
        mass: 4f,
        centreOfMass: Vector3.Zero,
        principalMoments: new Vector3(0.12544f, 0.12544f, 0.12544f),
        principalFrame: Quaternion.Identity,
        linearDrag: 0.04f,
        gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
