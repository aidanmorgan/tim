using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.Counter;

public sealed class CounterElementDeclaration : ISignalElement, IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "counter",
        Category: "Logic",
        DisplayNameKey: "part_counter",
        IconPath: "res://parts/icons/counter.png",
        ScenePath: "res://parts/scenes/counter.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("signal_in", SimulationDomain.Signal, PortDirection.Input, new Vector3(-0.8f, 0, 0), -Vector3.UnitX),
        new("signal_out", SimulationDomain.Signal, PortDirection.Output, new Vector3(0.8f, 0, 0), Vector3.UnitX),
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(0, -0.6f, -0.3f), -Vector3.UnitZ),
        new("power_out", SimulationDomain.Electrical, PortDirection.Output, new Vector3(0, -0.6f, 0.3f), Vector3.UnitZ)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.825f, 0.625f, 0.325f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f)),
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.9f, 0.08f, 0.425f),
            Offset: new Vector3(0, -0.72f, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.CounterReached, "Count:i32;Target:i32")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("pulse", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => new(Threshold: 3, ResetOnThreshold: false);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("latch_switch", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];
}
