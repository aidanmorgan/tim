using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Ball;

public sealed class BallElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.46f, PresentationAxes.None,
        [], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "ball",
        Category: ElementCategory.Motion,
        DisplayName: "Basketball",
        Description: "A lively, medium-weight ball. Rolls, falls, and transfers momentum.",
        Color: new(0.96f, 0.49f, 0.22f, 1f),
        PaletteOrder: 1,
        IconPath: "res://elements/Ball/icon.svg",
        ScenePath: "res://elements/Ball/ball.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The Basketball: a 0.34 m sphere with the catalogue ball material.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.55f, 0.3f, 0.035f, 0.1f))
    ];

    /// <summary>A ball owns no event row: the impacts it takes are its strikers' records (Story 9A.22).</summary>
    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    public PayloadPolicy Payload => PayloadPolicy.CaptureTarget;

    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(
        mass: 1f,
        centreOfMass: Vector3.Zero,
        principalMoments: new Vector3(0.04624f, 0.04624f, 0.04624f),
        principalFrame: Quaternion.Identity,
        linearDrag: 0.04f,
        gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
