using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Funnel;

/// <summary>The Funnel (CAT-030): a static hollow frustum 1.8 m long that gathers balls from a 1.3 m inlet radius into the 0.65 m tube
/// bore. Its colliders are true hollow shapes (engine-hollow-shapes): the Frustum wall (outer 1.35 m at the inlet to 0.7 m at the outlet,
/// radial wall 0.05 m) between its collars, a 9 cm Tube stub from each collar to its mouth (bores 1.3 and 0.65 m), and the two drawn
/// torus collars. Restitution 0.15 (CAT-030).</summary>
public sealed class FunnelElementDeclaration : IMechanicalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.75f, PresentationAxes.None,
        [], [], [], []);

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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.15f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public PayloadPolicy Payload => PayloadPolicy.None;

    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(
        mass: 0f,
        centreOfMass: Vector3.Zero,
        principalMoments: Vector3.Zero,
        principalFrame: Quaternion.Identity,
        linearDrag: 0f,
        gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

}
