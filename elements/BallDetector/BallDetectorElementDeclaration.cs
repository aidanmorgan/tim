using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.BallDetector;

public sealed class BallDetectorElementDeclaration : IMechanicalElement, IPassageSensorElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.15f, PresentationAxes.None,
        [
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoRed, PresentationDriver.ActivationPulse, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .35f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.ActivationPulse, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .35f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.ActivationPulse, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .35f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "ball_detector",
        Category: ElementCategory.Control,
        DisplayName: "Ball detector",
        Description: "An open collar sends one trigger when a ball passes in the arrow direction. It must clear the upstream side before triggering again. Snaps to matching tube mouths; does not supply electricity.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 16,
        IconPath: "res://elements/BallDetector/icon.svg",
        ScenePath: "res://elements/BallDetector/ball_detector.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(0f, 1.12f, 0f), Vector3.UnitY)
    ];

    /// <summary>The 16-box ring around the 0.65 m bore (each box turned about +X to its angle) and the sensor housing above it.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.Passage)];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("passage", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    /// <summary>The aperture is the collar's 0.65 m bore; a ball rearms once it clears the upstream side by 0.02 m beyond its own
    /// radius.</summary>
    public SignalPassageDescriptor Passage => new(ApertureRadius: .65f, RearmClearance: .02f);
}
