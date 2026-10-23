using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.Counter;

public sealed class CounterElementDeclaration : IPulseCountElement, ISupplyElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.2f, PresentationAxes.None,
        [
            new("Visual/DotLamp_0", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 1f, PresentationTimerPhase.None),
            new("Visual/DotLamp_0", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 1f, PresentationTimerPhase.None),
            new("Visual/DotLamp_0", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 1f, PresentationTimerPhase.None),
            new("Visual/DotLamp_1", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 2f, PresentationTimerPhase.None),
            new("Visual/DotLamp_1", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 2f, PresentationTimerPhase.None),
            new("Visual/DotLamp_1", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 2f, PresentationTimerPhase.None),
            new("Visual/DotLamp_2", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 3f, PresentationTimerPhase.None),
            new("Visual/DotLamp_2", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 3f, PresentationTimerPhase.None),
            new("Visual/DotLamp_2", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 3f, PresentationTimerPhase.None),
            new("Visual/DotLamp_3", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 4f, PresentationTimerPhase.None),
            new("Visual/DotLamp_3", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 4f, PresentationTimerPhase.None),
            new("Visual/DotLamp_3", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 4f, PresentationTimerPhase.None),
            new("Visual/DotLamp_4", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 5f, PresentationTimerPhase.None),
            new("Visual/DotLamp_4", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 5f, PresentationTimerPhase.None),
            new("Visual/DotLamp_4", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 5f, PresentationTimerPhase.None),
            new("Visual/DotLamp_5", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 6f, PresentationTimerPhase.None),
            new("Visual/DotLamp_5", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 6f, PresentationTimerPhase.None),
            new("Visual/DotLamp_5", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 6f, PresentationTimerPhase.None),
            new("Visual/DotLamp_6", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 7f, PresentationTimerPhase.None),
            new("Visual/DotLamp_6", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 7f, PresentationTimerPhase.None),
            new("Visual/DotLamp_6", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 7f, PresentationTimerPhase.None),
            new("Visual/DotLamp_7", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 8f, PresentationTimerPhase.None),
            new("Visual/DotLamp_7", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 8f, PresentationTimerPhase.None),
            new("Visual/DotLamp_7", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 8f, PresentationTimerPhase.None),
            new("Visual/DotLamp_8", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCount, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 9f, PresentationTimerPhase.None),
            new("Visual/DotLamp_8", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCount, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 9f, PresentationTimerPhase.None),
            new("Visual/DotLamp_8", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCount, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 9f, PresentationTimerPhase.None)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "counter",
        Category: ElementCategory.Control,
        DisplayName: "Counter",
        Description: "Counts trigger events, lighting one dot per event. When the count reaches its target (1 to 9, default 3) it emits once and keeps its electrical contact closed until Reset. Connect a supply to power downstream devices.",
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

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.ThresholdReached)];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers => [];

    /// <summary>Each crossing counts once; the input that reaches target_count emits once and closes the contact until Reset (CAT-020).</summary>
    public SignalPulseCountDescriptor PulseCount => new("target_count");

    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("latch_switch", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];
}
