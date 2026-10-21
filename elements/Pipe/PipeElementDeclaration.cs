using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Pipe;

/// <summary>The Clear pipe (CAT-048): a static hollow tube whose length is a typed parameter (1 to 8 m, default 3.6 m). Its colliders are
/// declared at the default length: 16 shell boxes around the 0.65 m bore, spanning half the length, and 8 collar rim boxes at each end.</summary>
public sealed class PipeElementDeclaration : IMechanicalElement, ISceneGeometryElement, IParameterizedGeometryElement
{
    private const float DefaultLength = 3.6f;

    public ElementPresentation Presentation => new(2.2f, PresentationAxes.X,
        [], [], [], [new("Visual/Visual_Shell", PresentationProperty.ScaleY, "length", 1f / DefaultLength, 0),
            new("Visual/Visual_Collar_Start", PresentationProperty.LocalX, "length", -.5f, 0),
            new("Visual/Visual_Collar_End", PresentationProperty.LocalX, "length", .5f, 0)],
        new(new(DefaultLength, 1.56f, 1.56f), new(.5f, 0, 0), new(.09f, .78f, 0),
            [new("length", PresentationAxes.X)]));

    public ElementMetadata Metadata => new(
        Id: "pipe",
        Category: ElementCategory.Motion,
        DisplayName: "Clear pipe",
        Description: "A hollow tube. Gravity carries balls through the open bore; tilt it downhill to guide them. The clear shell transmits light; cream collars are opaque.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 15,
        IconPath: "res://elements/Pipe/icon.svg",
        ScenePath: "res://elements/Pipe/pipe.tscn",
        Parameters:
        [
            new("length", ParameterUnit.Metre, ParameterKind.Real, Minimum: 1f, Maximum: 8f, Default: DefaultLength, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [new(TubeEnd.Start, "length", new Vector3(-.5f, 0f, 0f), new Vector3(-.09f, 0f, 0f)),
        new(TubeEnd.End, "length", new Vector3(.5f, 0f, 0f), new Vector3(.09f, 0f, 0f))];

    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(0, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(1, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(2, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(3, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(4, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(5, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(6, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(7, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(8, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(9, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(10, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(11, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(12, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(13, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(14, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(15, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(16, GeometryComponent.OffsetX, "length", -.5f, 0f),
        new(17, GeometryComponent.OffsetX, "length", -.5f, 0f),
        new(18, GeometryComponent.OffsetX, "length", -.5f, 0f),
        new(19, GeometryComponent.OffsetX, "length", -.5f, 0f),
        new(20, GeometryComponent.OffsetX, "length", .5f, 0f),
        new(21, GeometryComponent.OffsetX, "length", .5f, 0f),
        new(22, GeometryComponent.OffsetX, "length", .5f, 0f),
        new(23, GeometryComponent.OffsetX, "length", .5f, 0f)
    ];

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
