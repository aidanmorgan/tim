using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Wall;

public sealed class WallElementDeclaration : IMechanicalElement
{
    public ElementMetadata Metadata => new(
        Id: "wall",
        Category: ElementCategory.Structure,
        DisplayName: "Wall",
        Description: "A solid panel. Move, rotate or stretch its three local dimensions to guide and contain balls.",
        Color: new(0.76f, 0.56f, 0.32f, 1f),
        PaletteOrder: 5,
        IconPath: "res://elements/Wall/icon.svg",
        ScenePath: "res://elements/Wall/wall.tscn",
        Parameters:
        [
            new("width", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.4f, Maximum: 8f, Default: 3f, Options: []),
            new("height", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.4f, Maximum: 6f, Default: 2f, Options: []),
            new("thickness", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.12f, Maximum: 2f, Default: 0.25f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The wall at the default dimensions: half its width, height and thickness.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            HalfExtents: new Vector3(1.5f, 1f, 0.125f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
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
}
