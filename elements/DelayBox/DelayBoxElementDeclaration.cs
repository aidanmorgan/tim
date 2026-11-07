using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.DelayBox;

/// <summary>The Delay Box (CAT-022): a static housing that waits its authored duration after an activation, then activates its
/// output once. The duration is a typed parameter with the runtime's default and admitted range.</summary>
public sealed class DelayBoxElementDeclaration : ISignalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.9f, PresentationAxes.None,
        [
            new("Visual/CountdownHand", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedTimerProgress, PresentationValueMapping.Identity, 0f, -6.2831855f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SignalNode: new("signal")),
            new("Visual/Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTimerPhase, PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SignalNode: new("signal"), StateValues: new(85f / 255f, 232f / 255f, 247f / 255f)),
            new("Visual/Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTimerPhase, PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SignalNode: new("signal"), StateValues: new(101f / 255f, 183f / 255f, 203f / 255f)),
            new("Visual/Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTimerPhase, PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SignalNode: new("signal"), StateValues: new(115f / 255f, 100f / 255f, 82f / 255f))
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "delay",
        Category: ElementCategory.Control,
        DisplayName: "Delay",
        Description: "Wait after an activation, then activate the output once. Select to set the duration. Reset rearms the timer.",
        Color: new(0.84f, 0.61f, 0.28f, 1f),
        PaletteOrder: 6,
        IconPath: "res://elements/DelayBox/icon.svg",
        ScenePath: "res://elements/DelayBox/delay.tscn",
        Parameters:
        [
            new("delay_seconds", ParameterUnit.Second, ParameterKind.Real, Minimum: 0.1f, Maximum: 12f, Default: 1f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, PortSignalKind.Pulse, new Vector3(-.72f, 0f, 0f), -Vector3.UnitX),
        new(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, new Vector3(.72f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The foot of the housing.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.TriggerActivated, new("signal"))];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal;


    public IReadOnlyList<SignalNodeDescriptor> SignalNodes => [new SignalTimerDescriptor(new("signal"), new("delay_seconds"), TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation)];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts =>
    [
        new(PortId.ActivationIn, new(new("signal"), SignalTerminal.Input)),
        new(PortId.ActivationOut, new(new("signal"), SignalTerminal.Output))
    ];
}
