using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.StateLatch;

public sealed class StateLatchElementDeclaration : ISignalElement, IElectricalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "latch",
        Category: ElementCategory.Control,
        DisplayName: "Set/Reset latch",
        Description: "Set closes the electrical contact; Reset opens it. Reset wins simultaneous triggers. Remembers its state without power, but needs a connected supply to power another device.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 24,
        IconPath: "res://elements/StateLatch/icon.svg",
        ScenePath: "res://elements/StateLatch/latch.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    /// <summary>The level output is high while memory is On (owner decision 18 Oct 2026) and is listed before Supply.</summary>
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("SetIn", SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(-.45f, .72f, 0f), Vector3.UnitY),
        new("ResetIn", SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(.45f, .72f, 0f), Vector3.UnitY),
        new("PowerIn", SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-.93f, 0f, 0f), -Vector3.UnitX),
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Level, new Vector3(0f, -.56f, .38f), Vector3.UnitZ),
        new("Supply", SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(.93f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.825f, 0.625f, 0.325f), Offset: Vector3.Zero, Rotation: Quaternion.Identity, Material: Surface),
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.9f, 0.08f, 0.425f), Offset: new Vector3(0, -0.72f, 0), Rotation: Quaternion.Identity, Material: Surface)
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
