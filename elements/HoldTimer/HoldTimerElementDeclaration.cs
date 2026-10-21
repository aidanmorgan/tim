using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.HoldTimer;

/// <summary>CAT-033: a rearming timed, independently supplied contact.</summary>
public sealed class HoldTimerElementDeclaration : ITimerElement, ISupplyElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new("hold_timer", ElementCategory.Control, "Hold timer",
        "An activation closes the supplied contact for the configured window. Busy triggers are ignored; expiry rearms it.",
        new(.40f, .72f, .79f, 1f), 25, "res://elements/HoldTimer/icon.svg", "res://elements/HoldTimer/hold_timer.tscn",
        [new("hold_seconds", ParameterUnit.Second, ParameterKind.Real, .1f, 12f, 2f, [])]);
    public ElementFootprint Footprint => new(1, 0, 0);
    public SimulationDomain SupportedDomains => SimulationDomain.Signal | SimulationDomain.Electrical;
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new(0f,.65f,.2f), Vector3.UnitY),
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new(-.78f,0f,0f), -Vector3.UnitX),
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new(.78f,0f,0f), Vector3.UnitX)
    ];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(1f,.3f,0f,.1f))];
    public IReadOnlyList<EventDescriptor> Events => [];
    public IReadOnlyList<SignalTriggerDescriptor> Triggers => [new("activation", SignalTriggerMode.Pulse,1f,.1f)];
    public SignalLatchMode LatchMode => SignalLatchMode.None;
    public SignalCounterDescriptor? Counter => null;
    public SignalTimerDescriptor Timer => new("hold_seconds", TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.None);
    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply);
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes => [new("switched_contact", ElectricalRole.Switch,12f,.05f,0f,100f)];
    public ElementPresentation Presentation => new(.95f, PresentationAxes.None,
    [
        new("Visual/Remaining", PresentationProperty.ScaleX, PresentationDriver.CommittedTimerRemaining, .001f,1f,PresentationCurve.Linear,0f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None),
        new("Visual/Remaining", PresentationProperty.Visible, PresentationDriver.CommittedTimerActive,0f,1f,PresentationCurve.Linear,0f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None),
        new("Visual/Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedTimerActive,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None),
        new("Visual/Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedTimerActive,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None),
        new("Visual/Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedTimerActive,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None)
    ],[],[],[]);
}
