using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SystemClock;

public sealed class SystemClockElementDeclaration : ISignalElement, IElectricalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "clock",
        Category: ElementCategory.Control,
        DisplayName: "Repeating clock",
        Description: "Connect electricity to start the pendulum. Sends one trigger each second, starting after a full interval. Losing power resets its timing; reconnecting begins a fresh interval.",
        Color: new(0.91f, 0.72f, 0.39f, 1f),
        PaletteOrder: 23,
        IconPath: "res://elements/SystemClock/icon.svg",
        ScenePath: "res://elements/SystemClock/clock.tscn",
        Parameters:
        [
            new("interval_seconds", ParameterUnit.Second, ParameterKind.Real, Minimum: 0.1f, Maximum: 12f, Default: 1f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-.78f, 0f, 0f), -Vector3.UnitX),
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(.78f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
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
