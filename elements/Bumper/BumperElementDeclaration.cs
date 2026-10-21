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
        Category: ElementCategory.Motion,
        DisplayName: "Pinball bumper",
        Description: "A round spring-loaded bumper kicks a ball away from its point of impact.",
        Color: new(0.87f, 0.44f, 0.35f, 1f),
        PaletteOrder: 7,
        IconPath: "res://elements/Bumper/icon.svg",
        ScenePath: "res://elements/Bumper/bumper.tscn",
        Parameters:
        [
            new("strength", ParameterUnit.MetrePerSecond, ParameterKind.Real, Minimum: 0f, Maximum: 20f, Default: 8f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("PowerIn", SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(0f, 0f, .65f), Vector3.UnitZ)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Sphere,
            HalfExtents: new Vector3(0.65f, 0.65f, 0.65f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
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
