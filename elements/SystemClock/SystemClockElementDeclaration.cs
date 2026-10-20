using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SystemClock;

public sealed class SystemClockElementDeclaration : ISignalElement, IElectricalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "clock",
        Category: ElementCategory.Control,
        DisplayNameKey: "part_clock",
        IconPath: null,
        ScenePath: "res://parts/scenes/clock.tscn",
        Parameters:
        [
            new("interval_seconds", ParameterUnit.Second, ParameterKind.Real, Minimum: 0.1f, Maximum: 12f, Default: 1f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("PowerIn", SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-.78f, 0f, 0f), -Vector3.UnitX),
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(.78f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.7f, 0.85f, 0.3f), Offset: Vector3.Zero, Rotation: Quaternion.Identity, Material: Surface),
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.8f, 0.1f, 0.425f), Offset: new Vector3(0, -0.95f, 0), Rotation: Quaternion.Identity, Material: Surface)
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
