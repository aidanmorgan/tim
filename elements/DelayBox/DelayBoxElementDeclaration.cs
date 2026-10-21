using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.DelayBox;

/// <summary>The Delay Box (CAT-022): a static housing that waits its authored duration after an activation, then activates its
/// output once. The duration is a typed parameter with the runtime's default and admitted range.</summary>
public sealed class DelayBoxElementDeclaration : ISignalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

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
            new("duration_seconds", ParameterUnit.Second, ParameterKind.Real, Minimum: 0.1f, Maximum: 12f, Default: 1f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("ActivationIn", SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, new Vector3(-.72f, 0f, 0f), -Vector3.UnitX),
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(.72f, 0f, 0f), Vector3.UnitX)
    ];

    /// <summary>The foot of the housing.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.675f, 0.075f, 0.4f), Offset: new Vector3(0, -0.7f, 0), Rotation: Quaternion.Identity, Material: Surface)
    ];

    /// <summary>The delayed output pulse, once per armed activation.</summary>
    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Duration:f32")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Signal;

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("activation", SignalTriggerMode.Pulse, Threshold: 1f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;
}
