using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.SignalLamp;

public sealed class SignalLampElementDeclaration : IElectricalElement, ISignalElement
{
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
        new("ActivationIn", SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Level, Vector3.Zero, -Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(
            ColliderShapeKind.Box,
            HalfExtents: new Vector3(0.425f, 0.1f, 0.425f),
            Offset: new Vector3(0, -0.35f, 0),
            Rotation: Quaternion.Identity,
            Material: new MaterialProperties(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f))
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
}
