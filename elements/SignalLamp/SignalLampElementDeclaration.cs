using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SignalLamp;

public sealed class SignalLampElementDeclaration : IElectricalElement, IIndicatorElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.65f, PresentationAxes.None,
        [
            new("Visual/Bulb", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActivation, 85f / 255f, 1f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Bulb", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedActivation, 101f / 255f, 240f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Bulb", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedActivation, 115f / 255f, 165f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Bulb", PresentationProperty.EmissionRed, PresentationDriver.CommittedActivation, 0f, 233f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Bulb", PresentationProperty.EmissionGreen, PresentationDriver.CommittedActivation, 0f, 178f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Bulb", PresentationProperty.EmissionBlue, PresentationDriver.CommittedActivation, 0f, 76f / 255f, PresentationCurve.SmoothStep, .16f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "lamp",
        Category: ElementCategory.Goals,
        DisplayName: "Signal lamp",
        Description: "Lights when its ActivationIn socket receives a signal.",
        Color: new(0.98f, 0.86f, 0.51f, 1f),
        PaletteOrder: 4,
        IconPath: "res://elements/SignalLamp/icon.svg",
        ScenePath: "res://elements/SignalLamp/lamp.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    /// <summary>The only input that admits a level: a lamp then follows that one level output alone.</summary>
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Level, Vector3.Zero, -Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Lit:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Electrical | SimulationDomain.Signal;

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("lamp_bulb", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 48f, Capacitance: 0f, MaxPower: 5f)
    ];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("lamp_trigger", SignalTriggerMode.Level, Threshold: 0.5f, PulseDurationSeconds: 0f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;

    /// <summary>The lamp indicates on its ActivationIn: fed by a level output it follows that level, otherwise it holds its first
    /// input until Reset.</summary>
    public SignalIndicatorDescriptor Indicator => new(PortSocket.ActivationIn);
}
