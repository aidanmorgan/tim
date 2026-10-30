using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Ramp;

public sealed class RampElementDeclaration : IMechanicalElement, ISceneGeometryElement, IParameterizedGeometryElement
{
    public ElementPresentation Presentation => new(1.5f, PresentationAxes.XZ,
        [], [], [], [new("Visual", PresentationProperty.ScaleX, "length", 1f / 3f, 0),
            new("Visual", PresentationProperty.ScaleZ, "width", 1f / 1.3f, 0)],
        new(new(3f, .18f, 1.3f), new(.5f, 0, 0), Vector3.Zero,
            [new("length", PresentationAxes.X), new("width", PresentationAxes.Z)]));

    public ElementMetadata Metadata => new(
        Id: "ramp",
        Category: ElementCategory.Structure,
        DisplayName: "Ramp",
        Description: "Rotate a ramp to guide a falling ball. Its width extends into depth.",
        Color: new(0.76f, 0.56f, 0.32f, 1f),
        PaletteOrder: 8,
        IconPath: "res://elements/Ramp/icon.svg",
        ScenePath: "res://elements/Ramp/ramp.tscn",
        Parameters:
        [
            new("length", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.125f, Maximum: 4f, Default: 3f, Options: []),
            new("width", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.125f, Maximum: 4f, Default: 1.3f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];

    /// <summary>The slab (collider 0) and the two rails (colliders 1 and 2, at z −0.598 and +0.598 for the default 1.3 m width)
    /// follow the art's own scaling: the Visual node scales X by length / 3 and Z by width / 1.3, so each rail is as long as the
    /// slab, and its 0.055 m depth and its offset from the centre line scale with the width. Its 0.055 m height does not scale.</summary>
    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(0, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(0, GeometryComponent.HalfExtentZ, "width", .5f, 0f),
        new(1, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(1, GeometryComponent.HalfExtentZ, "width", .0275f / 1.3f, 0f),
        new(1, GeometryComponent.OffsetZ, "width", -.598f / 1.3f, 0f),
        new(2, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(2, GeometryComponent.HalfExtentZ, "width", .0275f / 1.3f, 0f),
        new(2, GeometryComponent.OffsetZ, "width", .598f / 1.3f, 0f)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
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
