using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Ramp;

public sealed class RampElementDeclaration : IMechanicalElement, ISceneGeometryElement, IParameterizedGeometryElement
{
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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The slab at the default dimensions: half the length, half the 0.18 m thickness and half the width.</summary>
    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];

    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(0, GeometryComponent.HalfExtentX, "length", .5f, 0f),
        new(0, GeometryComponent.HalfExtentZ, "width", .5f, 0f)
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
