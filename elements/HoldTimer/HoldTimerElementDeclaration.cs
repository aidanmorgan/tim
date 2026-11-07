using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.HoldTimer;

/// <summary>CAT-033: a rearming timed, independently supplied contact.</summary>
public sealed class HoldTimerElementDeclaration : ISignalElement, ISupplyElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "hold_timer",
        Category: ElementCategory.Control,
        DisplayName: "Hold timer",
        Description: "An activation closes the supplied contact for the configured window. Busy triggers are ignored; expiry rearms it.",
        Color: new(.40f, .72f, .79f, 1f),
        PaletteOrder: 25,
        IconPath: "res://elements/HoldTimer/icon.svg",
        ScenePath: "res://elements/HoldTimer/hold_timer.tscn",
        Parameters: [new("hold_seconds", ParameterUnit.Second, ParameterKind.Real, .1f, 12f, 2f, [])]);
    public ElementFootprint Footprint => new(1, 0, 0, 0);
    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, PortSignalKind.Pulse, new(0f,.65f,.2f), Vector3.UnitY),
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new(-.78f,0f,0f), -Vector3.UnitX),
        new(PortId.Supply, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, new(.78f,0f,0f), Vector3.UnitX)
    ];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(1f,.3f,0f,.1f))];
    public IReadOnlyList<EventDescriptor> Events => [];
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes => [new SignalTimerDescriptor(new("signal"), new("hold_seconds"), TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.None)];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts =>
    [
        new(PortId.ActivationIn, new(new("signal"), SignalTerminal.Input))
    ];
    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyContactDescriptor(new("contact"), new(new("signal"), SignalContactState.TimerCounting))], [],
        [
            new(PortId.PowerIn, new(new("contact"), SupplyTerminal.PowerIn)),
            new(PortId.Supply, new(new("contact"), SupplyTerminal.Output))
        ]);
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes => [new("switched_contact", ElectricalRole.Switch,12f,.05f,0f,100f)];
    public ElementPresentation Presentation => new(.95f, PresentationAxes.None,
    [
        new("Visual/Remaining", PresentationProperty.ScaleX, PresentationDriver.CommittedTimerRemaining, PresentationValueMapping.Identity, .001f,1f,PresentationCurve.Linear,0f,PresentationOverlap.Replace,0f,SignalNode: new("signal")),
        new("Visual/Remaining", PresentationProperty.Visible, PresentationDriver.CommittedTimerActive, PresentationValueMapping.Identity,0f,1f,PresentationCurve.Linear,0f,PresentationOverlap.Replace,0f,SignalNode: new("signal")),
        new("Visual/Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTimerActive, PresentationValueMapping.Identity,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,SignalNode: new("signal")),
        new("Visual/Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTimerActive, PresentationValueMapping.Identity,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,SignalNode: new("signal")),
        new("Visual/Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTimerActive, PresentationValueMapping.Identity,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,SignalNode: new("signal"))
    ],[],[],[]);
}
