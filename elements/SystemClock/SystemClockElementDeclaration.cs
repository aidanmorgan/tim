using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SystemClock;

public sealed class SystemClockElementDeclaration : ISignalElement, IElectricalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.1f, PresentationAxes.None,
        [
            new("Visual/PendulumPivot", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.SineCyclesToUnit, -.42f, .42f, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 30f, SignalNode: new("signal")),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedOscillatorPulse, PresentationValueMapping.Identity, 85f / 255f, 247f / 255f, PresentationCurve.LinearDecay, .25f, PresentationOverlap.Maximum, 0f, PresentationTimerPhase.None, SignalNode: new("signal")),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedOscillatorPulse, PresentationValueMapping.Identity, 101f / 255f, 203f / 255f, PresentationCurve.LinearDecay, .25f, PresentationOverlap.Maximum, 0f, PresentationTimerPhase.None, SignalNode: new("signal")),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedOscillatorPulse, PresentationValueMapping.Identity, 115f / 255f, 82f / 255f, PresentationCurve.LinearDecay, .25f, PresentationOverlap.Maximum, 0f, PresentationTimerPhase.None, SignalNode: new("signal"))
        ], [], [], []);

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
            // The editor clamps an entry into range (the former interval spin box's behaviour); it is a Build-only value.
            new("interval_seconds", ParameterUnit.Second, ParameterKind.Real, Minimum: 0.1f, Maximum: 12f, Default: 1f, Options: [],
                EditorPolicy: ParameterEditorPolicy.Clamp)
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new Vector3(-.78f, 0f, 0f), -Vector3.UnitX),
        new(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, new Vector3(.78f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The housing and its foot.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.TriggerActivated, new("signal"))];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;


    /// <summary>The pendulum pulses its ActivationOut once per declared interval while its PowerIn is supplied (CAT-017).</summary>
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes => [new SignalOscillatorDescriptor(new("signal"), new("interval_seconds"), PortId.PowerIn)];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts =>
    [
        new(PortId.ActivationOut, new(new("signal"), SignalTerminal.Output))
    ];

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("oscillator_consumer", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 10f, Capacitance: 0f, MaxPower: 50f)
    ];
}
