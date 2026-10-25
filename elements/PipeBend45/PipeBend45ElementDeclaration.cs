using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PipeBend45;

/// <summary>The 45° pipe bend (CAT-049): a static hollow torus segment, a distinct element from the 90° bend. Its colliders are true
/// hollow shapes (engine-hollow-shapes): the 45° Torus wall (centreline 2.4 m, bore 0.65 m, wall to 0.7 m) between its collars, a
/// 9 cm Tube stub from each collar to its mouth, and the two drawn torus collars. Restitution 0.15 (CAT-049).</summary>
public sealed class PipeBend45ElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(2.2f, PresentationAxes.None,
        [], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "pipe_bend_45",
        Category: ElementCategory.Motion,
        DisplayName: "45° pipe bend",
        Description: "A 45-degree hollow curve. Turns falling balls smoothly through a gentle angle under gravity.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 17,
        IconPath: "res://elements/PipeBend45/icon.svg",
        ScenePath: "res://elements/PipeBend45/pipe_bend_45.tscn",
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

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

}
