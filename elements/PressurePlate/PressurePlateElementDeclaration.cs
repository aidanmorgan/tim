using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.PressurePlate;

public sealed class PressurePlateElementDeclaration : IMechanicalElement, IElectricalElement, ISignalElement
{
    public ElementMetadata Metadata => new(
        Id: "pressure_plate",
        Category: "Control",
        DisplayNameKey: "part_pressure_plate",
        IconPath: "res://parts/icons/pressure_plate.png",
        ScenePath: "res://parts/scenes/pressure_plate.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(-0.6f, 0, 0), -Vector3.UnitX),
        new("power_out", SimulationDomain.Electrical, PortDirection.Output, new Vector3(0.6f, 0, 0), Vector3.UnitX),
        // Story 9R.5 D3: the level ActivationOut on the +Z edge, level with the supply sockets (scene Loc_Port_activation_out).
        new("signal_out", SimulationDomain.Signal, PortDirection.Output, new Vector3(0, -0.08f, 1.18f), Vector3.UnitZ)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.9f, 0.05f, 0.9f),
            Offset: new Vector3(0, 0.03f, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.5f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.ContactLoad, "Load:f32;Threshold:f32;Active:bool"),
        new(SimulationEventKind.LatchSet, "State:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("switch", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("pressure", SignalTriggerMode.Level, Threshold: 0.5f, PulseDurationSeconds: 0f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;
}
