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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.14f, 0.3f, 0.03f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.Impact, "Impulse:f32;Contact:vec3;Normal:vec3")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    /// <summary>A solid sphere's inertia, 2/5 m r².</summary>
    public RigidBodyMassProperties MassProperties => new(
        Mass: 4f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: new Vector3(0.12544f, 0.12544f, 0.12544f),
        LinearDrag: 0.04f,
        Gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
