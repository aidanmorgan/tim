using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SignalLamp;

public sealed class SignalLampElementDeclaration : IElectricalElement, ISignalElement
{
    public ElementMetadata Metadata => new(
        Id: "lamp",
        Category: "Indicators",
        DisplayNameKey: "part_lamp",
        IconPath: "res://parts/icons/lamp.png",
        ScenePath: "res://parts/scenes/lamp.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("signal_in", SimulationDomain.Signal, PortDirection.Input, new Vector3(0, -0.35f, 0), -Vector3.UnitY),
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(0, -0.35f, 0), -Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.425f, 0.1f, 0.425f),
            Offset: new Vector3(0, -0.35f, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Lit:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Electrical | SimulationDomain.Signal;

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("lamp_bulb", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 48f, Capacitance: 0f, MaxPower: 5f)
    ];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("lamp_trigger", SignalTriggerMode.Level, Threshold: 0.5f, PulseDurationSeconds: 0f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;
}
