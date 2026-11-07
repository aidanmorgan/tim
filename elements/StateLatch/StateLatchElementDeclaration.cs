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
            new("Visual/Rocker", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedActivation, PresentationValueMapping.Identity, -.25f, .25f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, SignalNode: new("signal")),
            new("Visual/IndicatorLamp", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActivation, PresentationValueMapping.Identity, 85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, SignalNode: new("signal")),
            new("Visual/IndicatorLamp", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedActivation, PresentationValueMapping.Identity, 101f / 255f, 203f / 255f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, SignalNode: new("signal")),
            new("Visual/IndicatorLamp", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedActivation, PresentationValueMapping.Identity, 115f / 255f, 82f / 255f, PresentationCurve.SmoothStep, .125f, PresentationOverlap.Replace, 0f, SignalNode: new("signal"))
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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    /// <summary>The level output is high while memory is On (owner decision 18 Oct 2026) and is listed before Supply.</summary>
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.SetIn, SimulationDomain.Signal, PortDirection.Input, PortRole.SetIn, PortSignalKind.Pulse, new Vector3(-.45f, .72f, 0f), Vector3.UnitY),
        new(PortId.ResetIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ResetIn, PortSignalKind.Pulse, new Vector3(.45f, .72f, 0f), Vector3.UnitY),
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new Vector3(-.93f, 0f, 0f), -Vector3.UnitX),
        new(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Level, new Vector3(0f, -.56f, .38f), Vector3.UnitZ),
        new(PortId.Supply, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, new Vector3(.93f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    /// <summary>One record per memory transition only (Level 1 Off→On, 0 On→Off); a redundant request publishes nothing.</summary>
    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.MemoryChanged, new("signal"))];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;


    /// <summary>SetIn turns the memory On and ResetIn turns it Off; a same-tick pair resolves to Reset (CAT-037).</summary>
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes => [new SignalMemoryDescriptor(new("signal"))];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts =>
    [
        new(PortId.SetIn, new(new("signal"), SignalTerminal.Set)),
        new(PortId.ResetIn, new(new("signal"), SignalTerminal.Reset)),
        new(PortId.ActivationOut, new(new("signal"), SignalTerminal.Output))
    ];

    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyContactDescriptor(new("contact"), new(new("signal"), SignalContactState.Latched))], [],
        [
            new(PortId.PowerIn, new(new("contact"), SupplyTerminal.PowerIn)),
            new(PortId.Supply, new(new("contact"), SupplyTerminal.Output))
        ]);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("switched_contact", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];
}
