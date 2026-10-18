using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PoweredGate;

public sealed class PoweredGateElementDeclaration : IMechanicalElement, IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "powered_gate",
        Category: "Mechanisms",
        DisplayNameKey: "part_powered_gate",
        IconPath: "res://parts/icons/powered_gate.png",
        ScenePath: "res://parts/scenes/powered_gate.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(0, -0.7f, 0), -Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.1f, 0.8f, 0.1f),
            Offset: new Vector3(-0.7f, 0, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f)),
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.1f, 0.8f, 0.1f),
            Offset: new Vector3(0.7f, 0, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f)),
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.6f, 0.75f, 0.05f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.PrismaticLimit, "Position:f32;Lower:bool;Upper:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

    public KinematicMode KinematicMode => KinematicMode.Kinematic;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 1.0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: new Vector3(0.1f, 0.1f, 0.1f),
        LinearDrag: 0.05f,
        Gravity: new Vector3(0, -9.81f, 0));

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints =>
    [
        new("slider", Vector3.Zero, Quaternion.Identity, Vector3.UnitY)
    ];

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("motor", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 24f, Capacitance: 0f, MaxPower: 20f)
    ];
}
