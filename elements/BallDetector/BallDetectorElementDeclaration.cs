using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.BallDetector;

/// <summary>The Ball detector (CAT-002). Its scene's colliders: a 16-box ring (each box turned about +X to its angle) solid from the
/// 0.65 m bore to the drawn 0.82 m collar, its tangential width 2 · 0.82 · tan(π/16) so the outer faces meet and the end faces are
/// closed; the sensor housing above it; and the ActivationOut socket and indicator spheres. A true ring kind replaces the boxes in
/// engine-hollow-shapes.</summary>
public sealed class BallDetectorElementDeclaration : IMechanicalElement, IPassageSensorElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.15f, PresentationAxes.None,
        [
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoRed, PresentationDriver.Event, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .35f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.Passage),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.Event, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .35f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.Passage),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.Event, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .35f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, Event: SimulationEventKind.Passage)
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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(0f, 1.12f, 0f), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    /// <summary>The detector's documented contact material (CAT-002 §2): restitution 0.15, friction 0.3, threshold 0.1 m/s.</summary>
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.15f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.Passage)];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;

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

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("passage", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    /// <summary>The aperture is the collar's 0.65 m bore; a ball rearms once it clears the upstream side by 0.02 m beyond its own
    /// radius.</summary>
    public SignalPassageDescriptor Passage => new(ApertureRadius: .65f, RearmClearance: .02f);
}
