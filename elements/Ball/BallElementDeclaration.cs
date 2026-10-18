using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Ball;

public sealed class BallElementDeclaration : IMechanicalElement
{
    public ElementMetadata Metadata => new(
        Id: "ball",
        Category: "Balls",
        DisplayNameKey: "part_ball",
        IconPath: "res://parts/icons/ball.png",
        ScenePath: "res://parts/scenes/ball.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Sphere,
            Dimensions: new Vector3(0.34f, 0, 0),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.8f, Friction: 0.3f, RollingResistance: 0.035f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.Impact, "Impulse:f32;Contact:vec3;Normal:vec3")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0.6f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: new Vector3(0.027f, 0.027f, 0.027f),
        LinearDrag: 0.04f,
        Gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
