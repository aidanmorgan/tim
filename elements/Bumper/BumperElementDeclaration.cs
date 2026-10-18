using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Bumper;

public sealed class BumperElementDeclaration : IMechanicalElement, IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "bumper",
        Category: "Obstacles",
        DisplayNameKey: "part_bumper",
        IconPath: "res://parts/icons/bumper.png",
        ScenePath: "res://parts/scenes/bumper.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(0, -0.3f, 0), -Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Sphere,
            Dimensions: new Vector3(0.65f, 0, 0),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 1.0f, Friction: 0.1f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Impulse:f32;Contact:vec3;Normal:vec3"),
        new(SimulationEventKind.ContactLoad, "Load:f32;BodyId:u64")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

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
        new("coil", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 10f, Capacitance: 0f, MaxPower: 50f)
    ];
}
