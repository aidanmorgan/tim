using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.ImpactLever;

/// <summary>The Impact Lever (CAT-034): a static base with fulcrum and end stops carrying a balanced dynamic beam
/// on a revolute hinge constraint. Falling loads turn one end and fling payloads upward at the other. End rope sockets
/// allow rope-driven lifting.</summary>
public sealed class ImpactLeverElementDeclaration : IHingeElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "impact_lever",
        Category: ElementCategory.Motion,
        DisplayName: "Impact Lever",
        Description: "A passive hinged beam flings payloads upward when struck by falling weights.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 34,
        IconPath: "res://elements/ImpactLever/icon.svg",
        ScenePath: "res://elements/ImpactLever/impact_lever.tscn",
        Parameters:
        [
            new("beam_mass", ParameterUnit.Kilogram, ParameterKind.Real, Minimum: 0.5f, Maximum: 20f, Default: 2f, Options: []),
            new("initial_angle", ParameterUnit.Degree, ParameterKind.Real, Minimum: -30f, Maximum: 30f, Default: 0f, Options: [])
        ]);

    public ElementPresentation Presentation => new(
        PickRadius: 2f,
        ResizeAxes: PresentationAxes.None,
        Bindings: [],
        Bodies: [new("Visual/Beam", new PackageBodyIndex(1), Vector3.Zero)],
        Spans: [],
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 1);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(
            PortSocket.TieA,
            SimulationDomain.Rope,
            PortDirection.Bidirectional,
            null,
            new(-1.8f, 0f, 0f),
            -Vector3.UnitX,
            RopeAttachmentRole.Load,
            null,
            new PackageBodyIndex(1)),
        new(
            PortSocket.TieB,
            SimulationDomain.Rope,
            PortDirection.Bidirectional,
            null,
            new(1.8f, 0f, 0f),
            Vector3.UnitX,
            RopeAttachmentRole.Load,
            null,
            new PackageBodyIndex(1))
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f)),
        new("Visual/Beam", new(0f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Rope;

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

    public IReadOnlyList<RevoluteHingeDescriptor> Hinges =>
    [
        new(
            BaseFrame: new(Vector3.Zero, Quaternion.Identity),
            MovingFrame: new(Vector3.Zero, Quaternion.Identity),
            LowerLimit: -MathF.PI / 6f,
            UpperLimit: MathF.PI / 6f,
            Stiffness: MechanicalValue.Fixed(0f),
            Damping: MechanicalValue.Fixed(0f),
            InitialAngle: MechanicalValue.FromParameter("initial_angle"),
            BodiesCollide: false,
            MovingBody: new(
                MassFrame: new(
                    Mass: new(new ParameterId("beam_mass"), 0f, 1f, 0f, 0f),
                    CentreOfMass: MassVector.Constant(Vector3.Zero),
                    PrincipalMoments: new(
                        new(new ParameterId("beam_mass"), 0f, (0.12f * 0.12f + 0.55f * 0.55f) / 3f, 0f, 0f),
                        new(new ParameterId("beam_mass"), 0f, (1.8f * 1.8f + 0.55f * 0.55f) / 3f, 0f, 0f),
                        new(new ParameterId("beam_mass"), 0f, (1.8f * 1.8f + 0.12f * 0.12f) / 3f, 0f, 0f)),
                    PrincipalFrame: Quaternion.Identity,
                    LinearDrag: 0f,
                    Gravity: new Vector3(0f, -9.81f, 0f)),
                Colliders: []))
    ];
}
