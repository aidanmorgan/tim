using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Funnel;

/// <summary>The Funnel (CAT-030): a static hollow frustum 1.8 m long that gathers balls from a 1.3 m inlet radius into the 0.65 m tube
/// bore. Its colliders are 4 rings of 16 wall boxes tilted to the taper, then 4 collar rim boxes at the inlet and 4 at the outlet.</summary>
public sealed class FunnelElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "funnel",
        Category: ElementCategory.Motion,
        DisplayName: "Funnel",
        Description: "A wide hollow funnel. Gathers falling balls from a wide 2.6-unit mouth into the standard 1.3-unit tube bore by gravity and contact alone.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 19,
        IconPath: "res://elements/Funnel/icon.svg",
        ScenePath: "res://elements/Funnel/funnel.tscn",
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
