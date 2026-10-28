using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Wall;

public sealed class WallElementDeclaration : IMechanicalElement, ISceneGeometryElement, IParameterizedGeometryElement
{
    public ElementPresentation Presentation => new(1.5f, PresentationAxes.All,
        [], [], [], [new("Visual", PresentationProperty.ScaleX, "width", 1, 0),
            new("Visual", PresentationProperty.ScaleY, "height", 1, 0),
            new("Visual", PresentationProperty.ScaleZ, "thickness", 1, 0)],
        new(new(3f, 2f, .25f), new(.5f), Vector3.Zero,
            [new("width", PresentationAxes.X), new("height", PresentationAxes.Y), new("thickness", PresentationAxes.Z)]));

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
    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];

    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(0, GeometryComponent.HalfExtentX, "width", .5f, 0f),
        new(0, GeometryComponent.HalfExtentY, "height", .5f, 0f),
        new(0, GeometryComponent.HalfExtentZ, "thickness", .5f, 0f)
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
