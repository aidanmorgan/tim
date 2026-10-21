using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Springboard;

/// <summary>The Springboard (CAT-062): a static base carrying a 0.25 kg plate on one prismatic slider. The plate rests 0.14 m above
/// the base frame and may compress 0.25 m below rest; its spring's stiffness and damping, and its starting compression (preload), are
/// typed parameters with the runtime's defaults and admitted ranges.</summary>
public sealed class SpringboardElementDeclaration : ISliderElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.9f, PresentationAxes.None,
        [], [new("Visual/Plate", PresentationBody.Secondary, Vector3.Zero)], [new("Visual/Plate", new(0, -.075f, 0), "Visual/Coil", new(0, -.3f, 0), .365f)], []);

    public ElementMetadata Metadata => new(
        Id: "spring",
        Category: ElementCategory.Motion,
        DisplayName: "Springboard",
        Description: "A moving plate compresses its spring under contact and returns stored elastic energy.",
        Color: new(0.96f, 0.7f, 0.33f, 1f),
        PaletteOrder: 12,
        IconPath: "res://elements/Springboard/icon.svg",
        ScenePath: "res://elements/Springboard/springboard.tscn",
        Parameters:
        [
            new("stiffness", ParameterUnit.NewtonPerMetre, ParameterKind.Real, Minimum: 120f, Maximum: 1200f, Default: 400f, Options: []),
            new("damping", ParameterUnit.NewtonSecondPerMetre, ParameterKind.Real, Minimum: 0f, Maximum: 8f, Default: 0.2f, Options: []),
            new("initial_compression", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0f, Maximum: 0.2f, Default: 0f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 1);

    public IReadOnlyList<PortDescriptor> Ports => [];

    /// <summary>The base slab.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0f, 0.1f, 0f, 0.05f)),
        new("Visual/Plate", new(0f, 0.1f, 0f, 0.05f))
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

    public IReadOnlyList<PrismaticSliderDescriptor> Sliders =>
    [
        new(
            BaseFrame: new(new Vector3(0f, 0.14f, 0f), Quaternion.Identity),
            MovingFrame: new(Vector3.Zero, Quaternion.Identity),
            LowerLimit: -0.25f,
            UpperLimit: 0f,
            Stiffness: MechanicalValue.FromParameter("stiffness"),
            Damping: MechanicalValue.FromParameter("damping"),
            Compression: MechanicalValue.FromParameter("initial_compression"),
            BodiesCollide: false,
            MovingBody: new(
                Mass: 0.25f,
                Gravity: new Vector3(0f, -9.81f, 0f),
                LinearDrag: 0f,
                Colliders: []))
    ];
}
