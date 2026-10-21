using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PoweredGate;

/// <summary>The scene-owned frame and blade, with declared material, mass and fixed prismatic slider values.</summary>
public sealed class PoweredGateElementDeclaration : ISliderElement, IElectricalElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(1.5f, PresentationAxes.None,
        [
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActuatorState, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 232f / 255f),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedActuatorState, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 183f / 255f),
            new("Visual/Visual_Indicator", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedActuatorState, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None, 0f, 100f / 255f)
        ], [new("Visual/Blade", PresentationBody.Secondary, Vector3.Zero)], [], []);

    public ElementMetadata Metadata => new(
        Id: "powered_gate",
        Category: ElementCategory.Motion,
        DisplayName: "Powered gate",
        Description: "Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes.",
        Color: new(0.97f, 0.796f, 0.322f, 1f),
        PaletteOrder: 20,
        IconPath: "res://elements/PoweredGate/icon.svg",
        ScenePath: "res://elements/PoweredGate/powered_gate.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 1);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(0f, 1.65f, .92f), Vector3.UnitZ)
    ];

    /// <summary>The 16-box ring around the 0.65 m bore (each box turned about +X to its angle), the motor housing above it and the
    /// two blade rails.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f)),
        new("Visual/Blade", new(0.1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.PrismaticLimit, "Position:f32;Lower:bool;Upper:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    /// <summary>The blade slides along the frame's +Y.</summary>
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints =>
    [
        new("slider", Vector3.Zero, Quaternion.Identity, Vector3.UnitY)
    ];

    public IReadOnlyList<PrismaticSliderDescriptor> Sliders =>
    [
        new(new(Vector3.Zero, Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
            0f, 1.55f, MechanicalValue.Fixed(14f), MechanicalValue.Fixed(4f), MechanicalValue.Fixed(0f), false,
            new(1f, new Vector3(0f, -9.81f, 0f), 0f,
                []))
    ];

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("motor", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 24f, Capacitance: 0f, MaxPower: 20f)
    ];
}
