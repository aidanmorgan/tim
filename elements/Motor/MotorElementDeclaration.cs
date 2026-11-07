using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Motor;

/// <summary>The Electric motor (CAT-042, Story 11.1a): a static housing carrying a dynamic rotor on a continuous revolute hinge about
/// its local +Z. Electricity on PowerIn supplies the hinge's velocity drive, which turns the rotor counterclockwise about +Z toward the
/// <c>speed</c> parameter with at most the <c>torque</c> parameter; unpowered, 0.05 N·m of bearing friction spins it down. The speed is
/// also a run-time control (owner, 3 November 2026): during Run it sets the drive's committed target from the next physics step, and
/// Reset and a save keep the authored speed. The torque is build-only. The supply lamp follows the drive's committed supplied state. The
/// Drive socket and its mechanical coupling are Story 11.1b.</summary>
public sealed class MotorElementDeclaration : IHingeElement, ISupplyElement, ISceneGeometryElement
{
    private const float RotorMass = .5f;
    private const float RotorRadius = .3f;
    private const float RotorWidth = .13f;

    /// <summary>The supply lamp blends slate #556573 to gold #f7cb52 over 0.1 s while the drive is supplied (CAT-042 §2).</summary>
    public ElementPresentation Presentation => new(.9f, PresentationAxes.None,
        [
            new("Visual/Lamp", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActuatorDrive, PresentationValueMapping.Identity, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Actuator: new(CoordinateKind.Revolute, 0)),
            new("Visual/Lamp", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedActuatorDrive, PresentationValueMapping.Identity, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Actuator: new(CoordinateKind.Revolute, 0)),
            new("Visual/Lamp", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedActuatorDrive, PresentationValueMapping.Identity, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Actuator: new(CoordinateKind.Revolute, 0))
        ], [new("Visual/Rotor", new PackageBodyIndex(1), Vector3.Zero)], [], []);

    public ElementMetadata Metadata => new(
        Id: "motor",
        Category: ElementCategory.Power,
        DisplayName: "Electric motor",
        Description: "Electricity turns its shaft.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 36,
        IconPath: "res://elements/Motor/icon.svg",
        ScenePath: "res://elements/Motor/motor.tscn",
        Parameters:
        [
            new("speed", ParameterUnit.RadianPerSecond, ParameterKind.Real, Minimum: 0f, Maximum: 20f, Default: 6f, Options: [],
                RunEdit: ParameterRunEdit.RunControl),
            // The floor is twice the bearing friction, so the weakest Motor still starts (CAT-042 §6).
            new("torque", ParameterUnit.NewtonMetre, ParameterKind.Real, Minimum: .1f, Maximum: 100f, Default: 20f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 1);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new Vector3(-.55f, 0f, 0f), -Vector3.UnitX)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    /// <summary>The housing and the rotor take the catalogue's rigid-part contact material (CAT-042 §6): restitution 0, friction 0.3,
    /// bounce threshold 0.1 m/s.</summary>
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0f, 0.3f, 0f, 0.1f)),
        new("Visual/Rotor", new(0f, 0.3f, 0f, 0.1f))
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

    /// <summary>The rotor's solid-disc moments in its collider's axes (the cylinder's own axis is its local Y): transverse
    /// m·(3r² + w²)/12, axial m·r²/2. The principal frame is the collider's turn Rx(90°), which lays the axial moment on the hinge's
    /// +Z.</summary>
    public IReadOnlyList<RevoluteHingeDescriptor> Hinges =>
    [
        new(
            BaseFrame: new(new Vector3(0f, 0f, .54f), Quaternion.Identity),
            MovingFrame: new(Vector3.Zero, Quaternion.Identity),
            LowerLimit: -MathF.PI,
            UpperLimit: MathF.PI,
            Travel: RevoluteTravel.Continuous,
            Stiffness: MechanicalValue.Fixed(0f),
            Damping: MechanicalValue.Fixed(0f),
            // Owner, 3 November 2026: an unpowered rotor spins down. 0.05 N·m stops it from 6 rad/s in about 2.7 s.
            FrictionTorque: MechanicalValue.Fixed(.05f),
            InitialAngle: MechanicalValue.Fixed(0f),
            BodiesCollide: false,
            MovingBody: new(
                MassFrame: MassFrameDeclaration.Constant(
                    mass: RotorMass,
                    centreOfMass: Vector3.Zero,
                    principalMoments: new Vector3(
                        RotorMass * (3f * RotorRadius * RotorRadius + RotorWidth * RotorWidth) / 12f,
                        RotorMass * RotorRadius * RotorRadius / 2f,
                        RotorMass * (3f * RotorRadius * RotorRadius + RotorWidth * RotorWidth) / 12f),
                    principalFrame: new Quaternion(MathF.Sqrt(.5f), 0f, 0f, MathF.Sqrt(.5f)),
                    linearDrag: 0f,
                    gravity: new Vector3(0f, -9.81f, 0f)),
                Colliders: []),
            Drive: new(TargetSpeed: MechanicalValue.FromParameter("speed"), MaxTorque: MechanicalValue.FromParameter("torque")))
    ];

    /// <summary>Inert electrical data (CAT-042 §6, decision 10): supply is on/off, with drain deferred. The rating is the drive's peak
    /// mechanical power, 100 N·m × 20 rad/s = 2000 W at 12 V.</summary>
    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyConsumerDescriptor(new("consumer"), [new SupplyDriveTarget(new(CoordinateKind.Revolute, 0))])], [],
        [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("motor", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 12f * 12f / 2000f, Capacitance: 0f, MaxPower: 2000f)
    ];
}
