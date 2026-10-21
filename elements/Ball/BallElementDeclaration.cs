using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Ball;

public sealed class BallElementDeclaration : IMechanicalElement
{
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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The Basketball: a 0.34 m sphere with the catalogue ball material.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Sphere,
            HalfExtents: new Vector3(0.34f, 0.34f, 0.34f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.55f, Friction: 0.3f, RollingResistance: 0.035f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.Impact, "Impulse:f32;Contact:vec3;Normal:vec3")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 1f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: new Vector3(0.04624f, 0.04624f, 0.04624f),
        LinearDrag: 0.04f,
        Gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
