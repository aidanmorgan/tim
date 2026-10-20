using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.Counter;

public sealed class CounterElementDeclaration : ISignalElement, IElectricalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "counter",
        Category: ElementCategory.Control,
        DisplayNameKey: "part_counter",
        IconPath: null,
        ScenePath: "res://parts/scenes/counter.tscn",
        Parameters:
        [
            new("target_count", ParameterUnit.None, ParameterKind.WholeNumber, Minimum: 1f, Maximum: 9f, Default: 3f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("ActivationIn", SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(0f, .72f, .1f), Vector3.UnitY),
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(0f, -.72f, .1f), -Vector3.UnitY),
        new("PowerIn", SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-.93f, 0f, 0f), -Vector3.UnitX),
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
