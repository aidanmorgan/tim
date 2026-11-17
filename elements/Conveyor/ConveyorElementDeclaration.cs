using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Conveyor;

/// <summary>The Conveyor belt (CAT-019, Story 11.1b): a static carrier box carrying an unpowered dynamic roller on a continuous
/// revolute hinge about local +Z, with a declared driven surface on its top face (+Y) coupled to that roller. Mechanical shaft coupling
/// on DriveIn spins the roller and drives surface transport along local +X with realistic friction and reaction torque. Drive relays
/// rotation onward. Unloaded belt transfers zero work; power cut decelerates the belt to rest under friction.</summary>
public sealed class ConveyorElementDeclaration : IHingeElement, IDrivenSurfaceElement, ISceneGeometryElement
{
    private const float RollerMass = .5f;
    private const float RollerRadius = .16f;
    private const float RollerWidth = 1.3f;

    public ElementPresentation Presentation => new(.85f, PresentationAxes.None,
        [], [new("Visual/Roller", new PackageBodyIndex(1), Vector3.Zero)], [], []);

    public ElementMetadata Metadata => new(
        Id: "conveyor",
        Category: ElementCategory.Motion,
        DisplayName: "Conveyor belt",
        Description: "Connect a motor, windmill or another conveyor to its input pulley. Its output passes rotation onward; a reverse transmission reverses travel.",
        Color: new(0.84f, 0.61f, 0.28f, 1f),
        PaletteOrder: 37,
        IconPath: "res://elements/Conveyor/icon.svg",
        ScenePath: "res://elements/Conveyor/conveyor.tscn",
        Parameters:
        [
            new("length", ParameterUnit.Metre, ParameterKind.Real, Minimum: 1.0f, Maximum: 10.0f, Default: 3.0f, Options: []),
            new("width", ParameterUnit.Metre, ParameterKind.Real, Minimum: 0.6f, Maximum: 4.0f, Default: 1.2f, Options: []),
            new("surface_per_radian", ParameterUnit.MetrePerRadian, ParameterKind.Real, Minimum: 0.1f, Maximum: 5.0f, Default: 0.6666667f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 1);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.DriveIn, SimulationDomain.Mechanical, PortDirection.Input, PortRole.DriveIn, null,
            new Vector3(-1.35f, -.07f, .78f), Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0), CoordinatePerRadian: 1f),
        new(PortId.Drive, SimulationDomain.Mechanical, PortDirection.Output, PortRole.Drive, null,
            new Vector3(1.35f, -.07f, .78f), Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0), CoordinatePerRadian: 1f)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.05f, 0.3f, 0f, 0.1f)),
        new("Visual/Roller", new(0.05f, 0.3f, 0f, 0.1f))
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

    public IReadOnlyList<RevoluteHingeDescriptor> Hinges =>
    [
        new(
            BaseFrame: new(new Vector3(-1.35f, -.07f, 0f), Quaternion.Identity),
            MovingFrame: new(Vector3.Zero, Quaternion.Identity),
            LowerLimit: -MathF.PI,
            UpperLimit: MathF.PI,
            Travel: RevoluteTravel.Continuous,
            Stiffness: MechanicalValue.Fixed(0f),
            Damping: MechanicalValue.Fixed(0f),
            FrictionTorque: MechanicalValue.Fixed(0.02f),
            InitialAngle: MechanicalValue.Fixed(0f),
            BodiesCollide: false,
            MovingBody: new(
                MassFrame: MassFrameDeclaration.Constant(
                    mass: RollerMass,
                    centreOfMass: Vector3.Zero,
                    principalMoments: new Vector3(
                        RollerMass * (3f * RollerRadius * RollerRadius + RollerWidth * RollerWidth) / 12f,
                        RollerMass * RollerRadius * RollerRadius / 2f,
                        RollerMass * (3f * RollerRadius * RollerRadius + RollerWidth * RollerWidth) / 12f),
                    principalFrame: new Quaternion(MathF.Sqrt(.5f), 0f, 0f, MathF.Sqrt(.5f)),
                    linearDrag: 0f,
                    gravity: new Vector3(0f, -9.81f, 0f)),
                Colliders: []),
            Drive: null)
    ];

    public IReadOnlyList<DrivenSurfaceDescriptor> DrivenSurfaces =>
    [
        new(Id: new(1), Carrier: new(0), Collider: new(0), Face: BoxFace.PositiveY,
            Source: new(CoordinateKind.Revolute, 0), LocalDirection: Vector3.UnitX,
            TravelPerRadian: MechanicalValue.FromParameter("surface_per_radian"))
    ];
}
