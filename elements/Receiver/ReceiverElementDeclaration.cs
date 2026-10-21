using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Receiver;

/// <summary>The Receiver (CAT-004): a static open box of five walls. Its capture settings and its guiding force region are typed
/// parameters with the runtime's defaults and admitted ranges.</summary>
public sealed class ReceiverElementDeclaration : IMechanicalElement
{
    private static readonly MaterialProperties Wall = new(Restitution: 0.12f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "basket",
        Category: ElementCategory.Goals,
        DisplayNameKey: "part_basket",
        IconPath: null,
        ScenePath: "res://elements/Receiver/basket.tscn",
        Parameters:
        [
            new("capture_margin", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0f, Maximum: 1f, Default: 0.02f, Options: []),
            new("capture_speed_limit", ParameterUnit.MetrePerSecond, ParameterKind.Real, Minimum: 1f / 1024f, Maximum: 64f, Default: 1.5f, Options: []),
            new("capture_dwell", ParameterUnit.Second, ParameterKind.Real, Minimum: 1f / 1024f, Maximum: 30f, Default: 0.35f, Options: []),
            new("capture_participation", ParameterUnit.None, ParameterKind.Choice, Minimum: 0f, Maximum: 1f, Default: 1f, Options: ["Disabled", "Enabled"]),
            new("force_support_margin", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0f, Maximum: 1f, Default: 0f, Options: []),
            new("force_maximum_acceleration", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, Minimum: 0f, Maximum: 12f, Default: 0f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The floor, the two side walls, the back wall and the lower front lip.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        Box(new Vector3(0f, -0.45f, 0f), new Vector3(0.75f, 0.075f, 0.75f)),
        Box(new Vector3(-0.75f, 0f, 0f), new Vector3(0.06f, 0.5f, 0.8f)),
        Box(new Vector3(0.75f, 0f, 0f), new Vector3(0.06f, 0.5f, 0.8f)),
        Box(new Vector3(0f, 0f, -0.75f), new Vector3(0.75f, 0.5f, 0.06f)),
        Box(new Vector3(0f, -0.2f, 0.75f), new Vector3(0.75f, 0.3f, 0.06f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    private static ColliderDescriptor Box(Vector3 offset, Vector3 halfExtents) =>
        new(ColliderShapeKind.Box, halfExtents, offset, Quaternion.Identity, Wall);
}
