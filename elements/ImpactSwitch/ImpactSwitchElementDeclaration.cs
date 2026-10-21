using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.ImpactSwitch;

/// <summary>The Impact Switch (CAT-063): a static base and button; a qualifying impact presses the button and sends one activation.
/// The trigger threshold is a typed parameter with the runtime's default and admitted range. Story 9.5b adds its electrical mode.</summary>
public sealed class ImpactSwitchElementDeclaration : IMechanicalElement, ISignalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "switch",
        Category: ElementCategory.Power,
        DisplayName: "Impact switch",
        Description: "A qualifying impact presses the button and sends one activation to connected signal parts.",
        Color: new(0.94f, 0.43f, 0.33f, 1f),
        PaletteOrder: 3,
        IconPath: "res://elements/ImpactSwitch/icon.svg",
        ScenePath: "res://elements/ImpactSwitch/switch.tscn",
        Parameters:
        [
            new("trigger_threshold", ParameterUnit.MetrePerSecond, ParameterKind.Real, Minimum: 0f, Maximum: 64f, Default: 0.8f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("ActivationOut", SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, new Vector3(.45f, 0f, .4f), Vector3.UnitZ)
    ];

    /// <summary>The base, then the button.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.55f, 0.125f, 0.5f), Offset: new Vector3(0, -0.15f, 0), Rotation: Quaternion.Identity, Material: Surface),
        new(ColliderShapeKind.Box, HalfExtents: new Vector3(0.4f, 0.09f, 0.375f), Offset: new Vector3(0, 0.05f, 0), Rotation: Quaternion.Identity, Material: Surface)
    ];

    /// <summary>One activation per qualifying impact.</summary>
    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.TriggerActivated, "Speed:f32;Threshold:f32")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("impact", SignalTriggerMode.Edge, Threshold: 0.8f, PulseDurationSeconds: 0.1f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;
}
