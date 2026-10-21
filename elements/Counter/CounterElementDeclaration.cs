using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.Counter;

public sealed class CounterElementDeclaration : ISignalElement, IElectricalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "counter",
        Category: ElementCategory.Control,
        DisplayName: "Counter",
        Description: "Counts trigger events, lighting one dot per event. At three it emits once and keeps its electrical contact closed until Reset. Connect a supply to power downstream devices.",
        Color: new(0.91f, 0.72f, 0.39f, 1f),
        PaletteOrder: 22,
        IconPath: "res://elements/Counter/icon.svg",
        ScenePath: "res://elements/Counter/counter.tscn",
        Parameters:
        [
            new("target_count", ParameterUnit.None, ParameterKind.WholeNumber, Minimum: 1f, Maximum: 9f, Default: 3f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(0f, .72f, .1f), Vector3.UnitY),
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(0f, -.72f, .1f), -Vector3.UnitY),
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-.93f, 0f, 0f), -Vector3.UnitX),
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(.93f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
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
