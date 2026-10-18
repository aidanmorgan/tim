using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.Battery;

public sealed class BatteryElementDeclaration : IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "battery",
        Category: "Power",
        DisplayNameKey: "part_battery",
        IconPath: "res://parts/icons/battery.png",
        ScenePath: "res://parts/scenes/battery.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("power_out", SimulationDomain.Electrical, PortDirection.Output, new Vector3(0, 0.4f, 0), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.425f, 0.525f, 0.375f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.ElectricalPowered, "Voltage:f32;Current:f32")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Electrical;

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("source", ElectricalRole.Source, RatedVoltage: 12f, Resistance: 0.1f, Capacitance: 0f, MaxPower: 100f)
    ];
}
