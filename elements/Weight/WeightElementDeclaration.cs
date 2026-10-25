using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Weight;

/// <summary>The Weight (CAT-067, Story 10.2a): a dynamic blue solid cylinder, axis +Y, whose <c>mass</c> parameter (0.25–8 kg,
/// default 4) is both its compiled mass and its size. The body origin is the base centre, so the collider sits h above it and a mass
/// edit grows the Weight upward. Radius and half-height are linear in mass, exact at 0.25 and 8 kg against the legacy
/// r = 0.85·0.32∛m and h = 0.675·0.32∛m; the art scales uniformly with the radius. It has no rope socket until Story 10.2b.</summary>
public sealed class WeightElementDeclaration : IParameterizedMassElement, ISceneGeometryElement, IParameterizedGeometryElement
{
    public ElementPresentation Presentation => new(
        PickRadius: .7f,
        ResizeAxes: PresentationAxes.None,
        Bindings: [],
        Bodies: [],
        Spans: [],
        Parameters: [new(Node: "Visual", Property: PresentationProperty.UniformScale, Parameter: "mass", Multiplier: .136734f, Offset: .453070f)]);

    public ElementMetadata Metadata => new(
        Id: "weight",
        Category: ElementCategory.Motion,
        DisplayName: "Weight",
        Description: "A heavy cylindrical load. Larger weights are heavier.",
        Color: new(0.27f, 0.39f, 0.61f, 1f),
        PaletteOrder: 31,
        IconPath: "res://elements/Weight/icon.svg",
        ScenePath: "res://elements/Weight/weight.tscn",
        Parameters:
        [
            new(Key: "mass", Unit: ParameterUnit.Kilogram, Kind: ParameterKind.Real, Minimum: .25f, Maximum: 8f, Default: 4f, Options: [],
                EditorPolicy: ParameterEditorPolicy.Clamp, RunEdit: ParameterRunEdit.BuildOnly)
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0);

    /// <summary>No socket yet: the rope Tie and its Load role arrive with the rope domain (Story 10.2b).</summary>
    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    /// <summary>One material: bounce 0.08, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s.</summary>
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(Root: ".", Material: new(Restitution: 0.08f, Friction: 0.3f, RollingResistance: 0.03f, BounceThreshold: 0.1f))
    ];

    /// <summary>The scene's cylinder (r, h, r) and its centre h above the base, linear in mass: identical X and Z radius lanes, and one
    /// law for the half-height and the offset.</summary>
    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(Collider: 0, Component: GeometryComponent.HalfExtentX, Parameter: "mass", Multiplier: .04808397f, Offset: .15932827f),
        new(Collider: 0, Component: GeometryComponent.HalfExtentY, Parameter: "mass", Multiplier: .03818433f, Offset: .12652539f),
        new(Collider: 0, Component: GeometryComponent.HalfExtentZ, Parameter: "mass", Multiplier: .04808397f, Offset: .15932827f),
        new(Collider: 0, Component: GeometryComponent.OffsetY, Parameter: "mass", Multiplier: .03818433f, Offset: .12652539f)
    ];

    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];

    /// <summary>A load owns no event row.</summary>
    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    /// <summary>The compiled mass is the <c>mass</c> parameter.</summary>
    public MechanicalValue Mass => MechanicalValue.FromParameter("mass");

    /// <summary>At the 4 kg default (one truth with <see cref="Mass"/>): the centre of mass at the cylinder's centre, h above the base,
    /// and the solid-cylinder moments m(r²/4 + h²/3), m r²/2. The compiler derives both from the bound collider per instance.</summary>
    public RigidBodyMassProperties MassProperties => new(
        Mass: 4f,
        CentreOfMass: new Vector3(0f, 0.27926272f, 0f),
        InertiaTensor: new Vector3(0.22765f, 0.24733f, 0.22765f),
        LinearDrag: 0f,
        Gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
