using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SystemClock;

public sealed class SystemClockElementDeclaration : ISignalElement, IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "clock",
        Category: "Control",
        DisplayNameKey: "part_clock",
        IconPath: "res://parts/icons/clock.png",
        ScenePath: "res://parts/scenes/clock.tscn",
        Parameters: new Dictionary<string, object>
        {
            ["interval_seconds"] = 1.0f
        });

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(-0.78f, 0, 0), -Vector3.UnitX),
        new("activation_out", SimulationDomain.Signal, PortDirection.Output, new Vector3(0.78f, 0, 0), Vector3.UnitX)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.7f, 0.85f, 0.3f),
            Offset: Vector3.Zero,
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f)),
        new(
            ColliderShapeKind.Box,
            Dimensions: new Vector3(0.8f, 0.1f, 0.425f),
            Offset: new Vector3(0, -0.95f, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 0.1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "PulseCount:i32;Interval:f32")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("clock_tick", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("oscillator_consumer", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 10f, Capacitance: 0f, MaxPower: 50f)
    ];
}
