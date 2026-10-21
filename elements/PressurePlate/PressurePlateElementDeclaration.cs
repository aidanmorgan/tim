using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.PressurePlate;

public sealed class PressurePlateElementDeclaration : IMechanicalElement, IElectricalElement, ISignalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "pressure_plate",
        Category: ElementCategory.Control,
        DisplayName: "Pressure plate",
        Description: "Senses resting mass on its top plate. Closes an internal electrical circuit while its threshold mass is met.",
        Color: new(0.91f, 0.72f, 0.39f, 1f),
        PaletteOrder: 21,
        IconPath: "res://elements/PressurePlate/icon.svg",
        ScenePath: "res://elements/PressurePlate/pressure_plate.tscn",
        Parameters:
        [
            new("minimum_mass", ParameterUnit.Kilogram, ParameterKind.Real, Minimum: 0.1f, Maximum: 16f, Default: 0.5f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    /// <summary>Story 9R.5 D3/D4: the level ActivationOut on the +Z edge, level with the supply sockets, precedes Supply.</summary>
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("PowerIn", SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-1.18f, -.08f, 0f), -Vector3.UnitX),
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Level, new Vector3(0f, -.08f, 1.18f), Vector3.UnitZ),
        new("Supply", SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(1.18f, -.08f, 0f), Vector3.UnitX)
    ];

    /// <summary>The base slab and the top face that carries the contact load.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(1.1f, 0.1f, 1.1f), Offset: new Vector3(0, -0.12f, 0), Rotation: Quaternion.Identity, Material: Surface),
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.9f, 0.05f, 0.9f), Offset: new Vector3(0, 0.03f, 0), Rotation: Quaternion.Identity, Material: Surface)
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
