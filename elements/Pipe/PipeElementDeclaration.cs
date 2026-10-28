using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Pipe;

/// <summary>The Clear pipe (CAT-048): a static hollow tube whose length is a typed parameter (1 to 8 m, default 3.6 m). Its colliders
/// are true hollow shapes (engine-hollow-shapes): the shell is a Tube between the 0.65 m bore and the drawn 0.7 m wall, reaching both
/// mouths (length + 0.18 m), so two snapped pipes' bores meet at the shared mouth plane; each collar is the drawn full torus (centreline
/// 0.715 m, tube 0.065 m) at its end of the length.</summary>
public sealed class PipeElementDeclaration : IMechanicalElement, ISceneGeometryElement, IParameterizedGeometryElement
{
    private const float DefaultLength = 3.6f;
    /// <summary>Each mouth lies this far beyond its collar: the shell reaches it.</summary>
    private const float MouthReach = .09f;
    /// <summary>The drawn shell's height at the default length: the length plus both mouth reaches.</summary>
    private const float ShellLength = DefaultLength + 2 * MouthReach;

    public ElementPresentation Presentation => new(2.2f, PresentationAxes.X,
        [], [], [], [new("Visual/Visual_Shell", PresentationProperty.ScaleY, "length", 1f / ShellLength, 2 * MouthReach / ShellLength),
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

    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [new(TubeEnd.Start, "length", new Vector3(-.5f, 0f, 0f), new Vector3(-MouthReach, 0f, 0f)),
        new(TubeEnd.End, "length", new Vector3(.5f, 0f, 0f), new Vector3(MouthReach, 0f, 0f))];

    /// <summary>The shell Tube's half-length (its Y lane) reaches both mouths; the collars follow the length's ends.</summary>
    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(0, GeometryComponent.HalfExtentY, "length", .5f, MouthReach),
        new(1, GeometryComponent.OffsetX, "length", -.5f, 0f),
        new(2, GeometryComponent.OffsetX, "length", .5f, 0f)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    /// <summary>The pipe family's documented restitution 0.15 (CAT-048).</summary>
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
