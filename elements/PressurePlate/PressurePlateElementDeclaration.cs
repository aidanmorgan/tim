using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.PressurePlate;

public sealed class PressurePlateElementDeclaration : IMechanicalElement, ISupplyElement, ISignalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.5f, PresentationAxes.None,
        [
            new("Visual/Stud_0", PresentationProperty.AlbedoRed, PresentationDriver.CommittedContactLoadPhase, 85f / 255f, 247f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 232f / 255f),
            new("Visual/Stud_0", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedContactLoadPhase, 101f / 255f, 203f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 183f / 255f),
            new("Visual/Stud_0", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedContactLoadPhase, 115f / 255f, 82f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 100f / 255f),
            new("Visual/Stud_1", PresentationProperty.AlbedoRed, PresentationDriver.CommittedContactLoadPhase, 85f / 255f, 247f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 232f / 255f),
            new("Visual/Stud_1", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedContactLoadPhase, 101f / 255f, 203f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 183f / 255f),
            new("Visual/Stud_1", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedContactLoadPhase, 115f / 255f, 82f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 100f / 255f),
            new("Visual/Stud_2", PresentationProperty.AlbedoRed, PresentationDriver.CommittedContactLoadPhase, 85f / 255f, 247f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 232f / 255f),
            new("Visual/Stud_2", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedContactLoadPhase, 101f / 255f, 203f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 183f / 255f),
            new("Visual/Stud_2", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedContactLoadPhase, 115f / 255f, 82f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 100f / 255f),
            new("Visual/Stud_3", PresentationProperty.AlbedoRed, PresentationDriver.CommittedContactLoadPhase, 85f / 255f, 247f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 232f / 255f),
            new("Visual/Stud_3", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedContactLoadPhase, 101f / 255f, 203f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 183f / 255f),
            new("Visual/Stud_3", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedContactLoadPhase, 115f / 255f, 82f / 255f, PresentationCurve.CubicSmoothStep, .125f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 100f / 255f)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "pressure_plate",
        Category: ElementCategory.Control,
        DisplayName: "Pressure plate",
        Description: "Senses resting mass on its top plate. Closes an internal electrical circuit while its threshold mass is met.",
        Color: new(0.91f, 0.72f, 0.39f, 1f),
        PaletteOrder: 21,
        IconPath: "res://elements/PressurePlate/icon.svg",
        ScenePath: "res://elements/PressurePlate/pressure_plate.tscn",
        Parameters:
        [
            new("minimum_mass", ParameterUnit.Kilogram, ParameterKind.Real, Minimum: 0.1f, Maximum: 16f, Default: 0.5f, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    /// <summary>Story 9R.5 D3/D4: the level ActivationOut on the +Z edge, level with the supply sockets, precedes Supply.</summary>
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(-1.18f, -.08f, 0f), -Vector3.UnitX),
        new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Level, new Vector3(0f, -.08f, 1.18f), Vector3.UnitZ),
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(1.18f, -.08f, 0f), Vector3.UnitX)
    ];

    /// <summary>The base slab and the top face that carries the contact load.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0.1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.ContactLoad, "Load:f32;Threshold:f32;Active:bool"),
        new(SimulationEventKind.LatchSet, "State:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical | SimulationDomain.Signal;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply);

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("switch", ElectricalRole.Switch, RatedVoltage: 12f, Resistance: 0.05f, Capacitance: 0f, MaxPower: 100f)
    ];

    public IReadOnlyList<SignalTriggerDescriptor> Triggers =>
    [
        new("pressure", SignalTriggerMode.Level, Threshold: 0.5f, PulseDurationSeconds: 0f)
    ];

    public SignalLatchMode LatchMode => SignalLatchMode.None;

    public SignalCounterDescriptor? Counter => null;
}
