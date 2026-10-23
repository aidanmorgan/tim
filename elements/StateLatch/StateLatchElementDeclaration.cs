using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.StateLatch;

public sealed class StateLatchElementDeclaration : ISignalElement, ISupplyElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.2f, PresentationAxes.None,
        [
            new("Visual/Rocker", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedActivation, -.25f, .25f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/IndicatorLamp", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActivation, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/IndicatorLamp", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedActivation, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/IndicatorLamp", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedActivation, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None)
        ], [], [], []);

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
        new(PortSocket.SetIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(-.45f, .72f, 0f), Vector3.UnitY),
        new(PortSocket.ResetIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(.45f, .72f, 0f), Vector3.UnitY),
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-.93f, 0f, 0f), -Vector3.UnitX),
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Level, new Vector3(0f, -.56f, .38f), Vector3.UnitZ),
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(.93f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    /// <summary>One record per memory transition only (Level 1 Off→On, 0 On→Off); a redundant request publishes nothing.</summary>
    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.MemoryChanged)];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("set_pulse", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.SetReset;

    public SignalCounterDescriptor? Counter => null;

    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("switched_contact", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];
}
