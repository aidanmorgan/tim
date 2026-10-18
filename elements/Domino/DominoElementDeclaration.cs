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
        Category: "Mechanisms",
        DisplayNameKey: "part_domino",
        IconPath: "res://parts/icons/domino.png",
        ScenePath: "res://parts/scenes/domino.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("signal_out", SimulationDomain.Signal, PortDirection.Output, new Vector3(0, 0.2f, 0), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.15f, 0.45f, 0.05f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.5f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Angle:f32;Toppled:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Dynamic;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0.1f,
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
