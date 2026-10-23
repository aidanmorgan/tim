using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.DelayBox;

/// <summary>The Delay Box (CAT-022): a static housing that waits its authored duration after an activation, then activates its
/// output once. The duration is a typed parameter with the runtime's default and admitted range.</summary>
public sealed class DelayBoxElementDeclaration : ITimerElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.9f, PresentationAxes.None,
        [
            new("Visual/CountdownHand", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedTimerProgress, 0f, -6.2831855f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTimerPhase, 85f / 255f, 85f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Ready),
            new("Visual/Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTimerPhase, 101f / 255f, 101f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Ready),
            new("Visual/Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTimerPhase, 115f / 255f, 115f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Ready),
            new("Visual/Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTimerPhase, 85f / 255f, 232f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Counting),
            new("Visual/Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTimerPhase, 101f / 255f, 183f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Counting),
            new("Visual/Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTimerPhase, 115f / 255f, 100f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Counting),
            new("Visual/Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTimerPhase, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Finished),
            new("Visual/Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTimerPhase, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Finished),
            new("Visual/Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTimerPhase, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.Finished)
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

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(-.72f, 0f, 0f), -Vector3.UnitX),
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(.72f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The foot of the housing.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.TriggerActivated)];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("activation", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    public SignalTimerDescriptor Timer => new("delay_seconds", TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation);
}
