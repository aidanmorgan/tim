using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.BallDetector;

public sealed class BallDetectorElementDeclaration : IMechanicalElement, ISignalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

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
    public IReadOnlyList<ColliderDescriptor> Colliders
    {
        get
        {
            var colliders = new List<ColliderDescriptor>(17);
            const float ringRadius = 0.675f;
            for (var i = 0; i < 16; i++)
            {
                var theta = i * (MathF.Tau / 16f);
                var halfTheta = theta * 0.5f;
                var rotation = Quaternion.Normalize(new Quaternion(MathF.Sin(halfTheta), 0, 0, MathF.Cos(halfTheta)));
                colliders.Add(new(ColliderShapeKind.Box, new Vector3(0.28f, 0.025f, 0.14f),
                    new Vector3(0, ringRadius * MathF.Cos(theta), ringRadius * MathF.Sin(theta)), rotation, Surface));
            }
            colliders.Add(new(ColliderShapeKind.Box, new Vector3(0.25f, 0.12f, 0.25f), new Vector3(0, 0.93f, 0), Quaternion.Identity, Surface));
            return colliders;
        }
    }

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.Passage, "BodyId:u64;Ordinal:u32")
    ];

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

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;
}
