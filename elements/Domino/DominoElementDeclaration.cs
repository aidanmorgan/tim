using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.Domino;

public sealed class DominoElementDeclaration : IMechanicalElement, ISignalElement
{
    public ElementMetadata Metadata => new(
        Id: "domino",
        Category: ElementCategory.Motion,
        DisplayName: "Domino",
        Description: "A physical tile: knock it over to hit the next domino. Its activation output signals once it has turned 45° from its placed pose.",
        Color: new(0.91f, 0.83f, 0.65f, 1f),
        PaletteOrder: 9,
        IconPath: "res://elements/Domino/icon.svg",
        ScenePath: "res://elements/Domino/domino.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(0f, .55f, 0f), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            HalfExtents: new Vector3(0.125f, 0.55f, 0.325f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.05f, Friction: 0.6f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Angle:f32;Toppled:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0.4f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: new Vector3(0.007f, 0.007f, 0.001f),
        LinearDrag: 0f,
        Gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("toppled", SignalTriggerMode.Edge, Threshold: 0.707f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.SetReset;

    public SignalCounterDescriptor? Counter => null;
}
