using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Ramp;

public sealed class RampElementDeclaration : IMechanicalElement
{
    public ElementMetadata Metadata => new(
        Id: "ramp",
        Category: ElementCategory.Structure,
        DisplayNameKey: "part_ramp",
        IconPath: null,
        ScenePath: "res://parts/scenes/ramp.tscn",
        Parameters:
        [
            new("length", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.125f, Maximum: 4f, Default: 3f, Options: []),
            new("width", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.125f, Maximum: 4f, Default: 1.3f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The slab at the default dimensions: half the length, half the 0.18 m thickness and half the width.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            HalfExtents: new Vector3(1.5f, 0.09f, 0.65f),
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
