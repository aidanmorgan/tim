using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Clutch;

/// <summary>The Clutch (CAT-018, Story 11.2a): an electrically actuated rotary coupling between two finite-inertia +Z shafts.
/// Electrical power supplied on <see cref="PortId.PowerIn"/> linearly closes the internal transmission over <c>close_seconds</c>
/// [0.05, 2.0] s; only when <see cref="TransmissionEngagementPhase.Engaged"/> does it couple input and output shafts (ratio +1).
/// Supply loss immediately enters <see cref="TransmissionEngagementPhase.Opening"/> and uncouples the shafts while each retains its own
/// angular momentum subject only to bearing friction (0.02 N·m).</summary>
public sealed class ClutchElementDeclaration : IHingeElement, IMechanicalTransmissionElement, ISupplyElement, ISceneGeometryElement
{
    private const float ShaftMass = .25f;
    private const float ShaftRadius = .28f;
    private const float ShaftWidth = .13f;
    private const float BearingFrictionTorque = .02f;

    private static readonly LocalMechanicalTransmissionId CouplingId = new(1);

    public ElementPresentation Presentation => new(1.1f, PresentationAxes.None,
        [
            new("Visual/PlateLeft", PresentationProperty.LocalX, PresentationDriver.CommittedTransmissionClosure,
                PresentationValueMapping.Identity, -.25f, -.09f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                Transmission: CouplingId),
            new("Visual/PlateRight", PresentationProperty.LocalX, PresentationDriver.CommittedTransmissionClosure,
                PresentationValueMapping.Identity, .25f, .09f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                Transmission: CouplingId),
            new("Visual/PlateLeft/Spin", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedRevoluteAngle,
                PresentationValueMapping.Identity, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                Actuator: new(CoordinateKind.Revolute, 0)),
            new("Visual/PlateRight/Spin", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedRevoluteAngle,
                PresentationValueMapping.Identity, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                Actuator: new(CoordinateKind.Revolute, 1)),
            new("Visual/Lamp", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTransmissionPhase,
                PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                FourStateValues: new(85f / 255f, 102f / 255f, 247f / 255f, 85f / 255f), Transmission: CouplingId),
            new("Visual/Lamp", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTransmissionPhase,
                PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                FourStateValues: new(101f / 255f, 184f / 255f, 203f / 255f, 101f / 255f), Transmission: CouplingId),
            new("Visual/Lamp", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTransmissionPhase,
                PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                FourStateValues: new(115f / 255f, 201f / 255f, 82f / 255f, 115f / 255f), Transmission: CouplingId)
        ],
        [
            new("Visual/InputShaft", new PackageBodyIndex(1), Vector3.Zero),
            new("Visual/OutputShaft", new PackageBodyIndex(2), Vector3.Zero)
        ], [], []);

    public ElementMetadata Metadata => new(
        Id: "clutch",
        Category: ElementCategory.Power,
        DisplayName: "Clutch",
        Description: "Electrically actuated coupling between two finite-inertia shafts. Closes over close_seconds when powered and uncouples immediately on power loss.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 38,
        IconPath: "res://elements/Clutch/icon.svg",
        ScenePath: "res://elements/Clutch/clutch.tscn",
        Parameters:
        [
            new("close_seconds", ParameterUnit.Second, ParameterKind.Real, Minimum: .05f, Maximum: 2f, Default: .2f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 3, MovingBodies: 2, PrismaticConstraints: 0, RevoluteConstraints: 2);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.DriveIn, SimulationDomain.Mechanical, PortDirection.Input, PortRole.DriveIn, null,
            new Vector3(-.65f, 0f, .55f), Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0), CoordinatePerRadian: 1f),
        new(PortId.Drive, SimulationDomain.Mechanical, PortDirection.Output, PortRole.Drive, null,
            new Vector3(.65f, 0f, .55f), Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 1), CoordinatePerRadian: 1f),
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null,
            new Vector3(0f, -.35f, .5f), Vector3.UnitZ)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    /// <summary>The housing and both shafts take the catalogue's rigid-part contact material: restitution 0, friction 0.3, bounce
    /// threshold 0.1 m/s.</summary>
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0f, 0.3f, 0f, 0.1f)),
        new("Visual/InputShaft", new(0f, 0.3f, 0f, 0.1f)),
        new("Visual/OutputShaft", new(0f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

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
        ShaftHinge(new Vector3(-.65f, 0f, .43f)),
        ShaftHinge(new Vector3(.65f, 0f, .43f))
    ];

    private static RevoluteHingeDescriptor ShaftHinge(Vector3 baseOffset)
    {
        var axial = ShaftMass * ShaftRadius * ShaftRadius / 2f;
        var transverse = ShaftMass * (3f * ShaftRadius * ShaftRadius + ShaftWidth * ShaftWidth) / 12f;
        return new(
            Support: new(0),
            BaseFrame: new(baseOffset, Quaternion.Identity),
            MovingFrame: new(Vector3.Zero, Quaternion.Identity),
            LowerLimit: -MathF.PI,
            UpperLimit: MathF.PI,
            Travel: RevoluteTravel.Continuous,
            Stiffness: MechanicalValue.Fixed(0f),
            Damping: MechanicalValue.Fixed(0f),
            FrictionTorque: MechanicalValue.Fixed(BearingFrictionTorque),
            InitialAngle: MechanicalValue.Fixed(0f),
            BodiesCollide: false,
            MovingBody: new(
                MassFrame: MassFrameDeclaration.Constant(
                    mass: ShaftMass,
                    centreOfMass: Vector3.Zero,
                    principalMoments: new Vector3(transverse, axial, transverse),
                    principalFrame: new Quaternion(MathF.Sqrt(.5f), 0f, 0f, MathF.Sqrt(.5f)),
                    linearDrag: 0f,
                    gravity: new Vector3(0f, -9.81f, 0f)),
                Colliders: []),
            Drive: null);
    }

    public IReadOnlyList<MechanicalTransmissionDescriptor> Transmissions =>
    [
        new(CouplingId, new CoordinateReference(CoordinateKind.Revolute, 0), new CoordinateReference(CoordinateKind.Revolute, 1),
            MechanicalValue.Fixed(1f), MechanicalValue.FromParameter("close_seconds"))
    ];

    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyConsumerDescriptor(new("consumer"), [new SupplyTransmissionTarget(CouplingId)])], [],
        [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("coil", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 12f * 12f / 20f, Capacitance: 0f, MaxPower: 20f)
    ];
}
