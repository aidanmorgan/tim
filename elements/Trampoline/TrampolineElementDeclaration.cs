using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Trampoline;

/// <summary>The Trampoline (CAT-065): a static frame carrying a 2.4 × 1.8 m compliant membrane bed at rest 0.2 m above the origin. Two
/// side rails and two end rails, 0.2 m tall, have their tops on the rest plane and their inner faces on the footprint edges; four gold
/// corner posts outside the footprint stand on the back plate and hold the rails, leaving the sides open so the compression reads from
/// the side. The bed yields up to 0.65 m, where the back plate's top face (restitution 0) stops the stroke; a body loads the bed only
/// after it enters from above. The default tension (300 N/m) keeps the 1 kg Basketball dropped from 4 m above the frame clear of the
/// back plate.</summary>
public sealed class TrampolineElementDeclaration : ICompliantSurfaceElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "trampoline",
        Category: ElementCategory.Motion,
        DisplayName: "Trampoline",
        Description: "A passive compliant membrane bed rebounds falling payloads upwards.",
        Color: new(0.4f, 0.722f, 0.788f, 1f),
        PaletteOrder: 35,
        IconPath: "res://elements/Trampoline/icon.svg",
        ScenePath: "res://elements/Trampoline/trampoline.tscn",
        Parameters:
        [
            new("tension", ParameterUnit.NewtonPerMetre, ParameterKind.Real, Minimum: 120f, Maximum: 1200f, Default: 300f, Options: []),
            new("damping_ratio", ParameterUnit.None, ParameterKind.Real, Minimum: 0.08f, Maximum: 0.8f, Default: 0.12f, Options: [])
        ]);

    /// <summary>The bed visual's rest height: its 0.02 m slab's top face on the 0.2 m rest plane.</summary>
    private const float BedRestY = 0.19f;
    /// <summary>The membrane's stroke: the back plate's top face is the stroke floor, 0.65 m below the rest plane.</summary>
    private const float MaxStroke = 0.65f;
    /// <summary>The gap the bed visual keeps above the back plate's top face at full stroke, so the two faces never coincide.</summary>
    private const float FloorClearance = 0.001f;
    /// <summary>The arrival depth a, 0.10 m: a body sample that crosses into the bed's footprint no deeper than a below the rest plane
    /// lands on the bed; deeper, it entered under the frame and is never pushed until it rises to the plane. Derivation (Story 10.5 review
    /// loop 4): at tilt all four diagonal samples sit on a body's lowest end, so a hidden end becomes a sample only when the support flips
    /// to it, measured at most 0.046 m below the plane (the 1.1 m Domino, the catalogue's widest body, tipping in from a rail at 300 N/m).
    /// a gives about twice that, and this frame's side opening (rest plane to rail underside, 0.2 m) is at least 2a, so a body entering
    /// under a rail is always Under. It must be re-derived if the frame, the sample law or the catalogue's widest body changes.</summary>
    private const float ArrivalDepth = 0.10f;

    /// <summary>The bed sags with the committed deflection d: its local Y is From + (To − From)·d = BedRestY − d·(MaxStroke −
    /// FloorClearance)/MaxStroke, flat at rest and after Reset, and at full stroke its top face stays 1 mm above the back plate's. The
    /// slab therefore reads at most 1 mm shallower than the deepest contact (at full stroke), invisible beside the contacting body.</summary>
    public ElementPresentation Presentation => new(
        PickRadius: 1.8f,
        ResizeAxes: PresentationAxes.None,
        Bindings:
        [
            new("Visual/Visual_Bed", PresentationProperty.LocalY, PresentationDriver.CommittedSurfaceDeflection, PresentationValueMapping.Identity,
                BedRestY, BedRestY - (MaxStroke - FloorClearance) / MaxStroke, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                PresentationTimerPhase.None)
        ],
        Bodies: [],
        Spans: [],
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0f, 0.3f, 0f, 0.1f))
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

    public CompliantSurfaceDescriptor CompliantSurface => new(
        HalfExtents: new Vector2(1.2f, 0.9f),
        RestHeight: 0.2f,
        MaxStroke: MaxStroke,
        ArrivalDepth: ArrivalDepth,
        Tension: MechanicalValue.FromParameter("tension"),
        DampingRatio: MechanicalValue.FromParameter("damping_ratio"));
}
