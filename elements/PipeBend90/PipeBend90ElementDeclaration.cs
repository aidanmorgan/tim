using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PipeBend90;

/// <summary>The 90° pipe bend (CAT-050): a static hollow torus segment. Its colliders are 8 slices of 8 wall boxes around the 0.65 m bore,
/// then the start and end collar rims (2 top/bottom and 2 left/right boxes each). Each row is (offset x, y, z, rotation x, y, z, w); the
/// rotation is normalised as the runtime table normalises it.</summary>
public sealed class PipeBend90ElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
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
        new(".", new(1f, 0.3f, 0f, 0.1f))
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
