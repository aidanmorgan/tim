using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.Battery;

public sealed class BatteryElementDeclaration : IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "battery",
        Category: ElementCategory.Power,
        DisplayName: "Battery",
        Description: "Supplies electricity through a wire; activation signals cannot replace a battery.",
        Color: new(0.87f, 0.44f, 0.35f, 1f),
        PaletteOrder: 11,
        IconPath: "res://elements/Battery/icon.svg",
        ScenePath: "res://elements/Battery/battery.tscn",
        Parameters:
        [
            new("enabled", ParameterUnit.None, ParameterKind.Choice, Minimum: 0f, Maximum: 1f, Default: 1f, Options: ["Disabled", "Enabled"])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(0f, .62f, 0f), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            HalfExtents: new Vector3(0.425f, 0.525f, 0.375f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
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
