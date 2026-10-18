using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.StateLatch;

public sealed class StateLatchElementDeclaration : ISignalElement, IElectricalElement
{
    public ElementMetadata Metadata => new(
        Id: "latch",
        Category: "Control",
        DisplayNameKey: "part_latch",
        IconPath: "res://parts/icons/latch.png",
        ScenePath: "res://parts/scenes/latch.tscn",
        Parameters: new Dictionary<string, object>());

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("set_in", SimulationDomain.Signal, PortDirection.Input, new Vector3(-0.45f, 0.72f, 0), Vector3.UnitY),
        new("reset_in", SimulationDomain.Signal, PortDirection.Input, new Vector3(0.45f, 0.72f, 0), Vector3.UnitY),
        new("power_in", SimulationDomain.Electrical, PortDirection.Input, new Vector3(-0.93f, 0, 0), -Vector3.UnitX),
        // Level output: high while memory is On (owner decision 18 Oct 2026); drives Signal lamps.
        new("activation_out", SimulationDomain.Signal, PortDirection.Output, new Vector3(0, -0.56f, 0.38f), Vector3.UnitZ),
        new("supply", SimulationDomain.Electrical, PortDirection.Output, new Vector3(0.93f, 0, 0), Vector3.UnitX)
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
        // Emitted once per memory transition only: P0 = 1 (Off→On) or 0 (On→Off).
        new(SimulationEventKind.LatchSet, "State:f32")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("set_pulse", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.SetReset;

    public SignalCounterDescriptor? Counter => null;

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("switched_contact", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];
}
