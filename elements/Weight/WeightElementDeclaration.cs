using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Weight;

/// <summary>The Weight (CAT-067, Story 10.2a): a dynamic blue solid cylinder, axis +Y, whose <c>mass</c> parameter (0.25–8 kg,
/// default 4) is both its compiled mass and its size. The body origin is the base centre, so the collider sits h above it and a mass
/// edit grows the Weight upward. Radius and half-height are linear in mass, exact at 0.25 and 8 kg against the legacy
/// r = 0.85·0.32∛m and h = 0.675·0.32∛m; the art scales uniformly with the radius. It has no rope socket until Story 10.2b.</summary>
public sealed class WeightElementDeclaration : IMechanicalElement, ISceneGeometryElement, IParameterizedGeometryElement
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
        new(Collider: 0, Component: GeometryComponent.OffsetY, Parameter: "mass", Multiplier: .03818433f, Offset: .12652539f),
        new(Collider: 1, Component: GeometryComponent.HalfExtentX, Parameter: "mass", Multiplier: 0.36f * .136734f, Offset: 0.36f * .453070f),
        new(Collider: 1, Component: GeometryComponent.HalfExtentY, Parameter: "mass", Multiplier: 0.025f * .136734f, Offset: 0.025f * .453070f),
        new(Collider: 1, Component: GeometryComponent.HalfExtentZ, Parameter: "mass", Multiplier: 0.36f * .136734f, Offset: 0.36f * .453070f),
        new(Collider: 1, Component: GeometryComponent.OffsetY, Parameter: "mass", Multiplier: 0.0517f * .136734f, Offset: 0.0517f * .453070f),
        new(Collider: 2, Component: GeometryComponent.Radius, Parameter: "mass", Multiplier: 0.3558f * .136734f, Offset: 0.3558f * .453070f),
        new(Collider: 2, Component: GeometryComponent.HalfExtentY, Parameter: "mass", Multiplier: 0.0075f * .136734f, Offset: 0.0075f * .453070f),
        new(Collider: 2, Component: GeometryComponent.OffsetY, Parameter: "mass", Multiplier: 0.1862f * .136734f, Offset: 0.1862f * .453070f),
        new(Collider: 3, Component: GeometryComponent.Radius, Parameter: "mass", Multiplier: 0.3558f * .136734f, Offset: 0.3558f * .453070f),
        new(Collider: 3, Component: GeometryComponent.HalfExtentY, Parameter: "mass", Multiplier: 0.0075f * .136734f, Offset: 0.0075f * .453070f),
        new(Collider: 3, Component: GeometryComponent.OffsetY, Parameter: "mass", Multiplier: 0.3724f * .136734f, Offset: 0.3724f * .453070f),
        new(Collider: 4, Component: GeometryComponent.Radius, Parameter: "mass", Multiplier: 0.14f * .136734f, Offset: 0.14f * .453070f),
        new(Collider: 4, Component: GeometryComponent.HalfExtentY, Parameter: "mass", Multiplier: 0.0175f * .136734f, Offset: 0.0175f * .453070f),
        new(Collider: 4, Component: GeometryComponent.OffsetY, Parameter: "mass", Multiplier: 0.5771f * .136734f, Offset: 0.5771f * .453070f)
    ];

    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];

    /// <summary>A load owns no event row.</summary>
    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    private const float RadiusSlope = .04808397f;
    private const float RadiusOffset = .15932827f;
    private const float HalfHeightSlope = .03818433f;
    private const float HalfHeightOffset = .12652539f;
    private static readonly ParameterId MassParameter = new("mass");

    public PayloadPolicy Payload => PayloadPolicy.None;

    // Expand the documented cylinder law once in package data; collision geometry contributes no mass.
    public MassFrameDeclaration MassFrame => new(
        new(MassParameter, 0f, 1f, 0f, 0f),
        new(MassScalar.Constant(0f), new(MassParameter, HalfHeightOffset, HalfHeightSlope, 0f, 0f), MassScalar.Constant(0f)),
        new(TransverseMoment, AxialMoment, TransverseMoment), Quaternion.Identity, 0f, new(0f, -9.81f, 0f));

    private static MassScalar AxialMoment => new(MassParameter, 0f,
        .5f * RadiusOffset * RadiusOffset, RadiusSlope * RadiusOffset, .5f * RadiusSlope * RadiusSlope);
    private static MassScalar TransverseMoment => new(MassParameter, 0f,
        RadiusOffset * RadiusOffset / 4f + HalfHeightOffset * HalfHeightOffset / 3f,
        RadiusSlope * RadiusOffset / 2f + 2f * HalfHeightSlope * HalfHeightOffset / 3f,
        RadiusSlope * RadiusSlope / 4f + HalfHeightSlope * HalfHeightSlope / 3f);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
}
