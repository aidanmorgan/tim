using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.Battery;

public sealed class BatteryElementDeclaration : ISupplyElement, ISceneGeometryElement
{
    public ElementPresentation Presentation => new(.8f, PresentationAxes.None,
        [
            new("Visual/Terminal", PresentationProperty.AlbedoRed, PresentationDriver.CommittedSupply, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Terminal", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedSupply, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Terminal", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedSupply, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, PresentationTimerPhase.None),
            new("Visual/Mark_0", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCharge, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.25f, PresentationTimerPhase.None),
            new("Visual/Mark_0", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCharge, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.25f, PresentationTimerPhase.None),
            new("Visual/Mark_0", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCharge, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.25f, PresentationTimerPhase.None),
            new("Visual/Mark_1", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCharge, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.5f, PresentationTimerPhase.None),
            new("Visual/Mark_1", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCharge, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.5f, PresentationTimerPhase.None),
            new("Visual/Mark_1", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCharge, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.5f, PresentationTimerPhase.None),
            new("Visual/Mark_2", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCharge, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.75f, PresentationTimerPhase.None),
            new("Visual/Mark_2", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCharge, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.75f, PresentationTimerPhase.None),
            new("Visual/Mark_2", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCharge, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0.75f, PresentationTimerPhase.None),
            new("Visual/Mark_3", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCharge, 85f / 255f, 247f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 1f, PresentationTimerPhase.None),
            new("Visual/Mark_3", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedCharge, 101f / 255f, 203f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 1f, PresentationTimerPhase.None),
            new("Visual/Mark_3", PresentationProperty.AlbedoBlue, PresentationDriver.CommittedCharge, 115f / 255f, 82f / 255f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 1f, PresentationTimerPhase.None)
        ], [], [], []);

    public ElementMetadata Metadata => new(
        Id: "battery",
        Category: ElementCategory.Power,
        DisplayName: "Battery",
        Description: "Supplies electricity through a wire; activation signals cannot replace a battery.",
        Color: new(0.87f, 0.44f, 0.35f, 1f),
        PaletteOrder: 11,
        IconPath: "res://elements/Battery/icon.svg",
        ScenePath: "res://elements/Battery/battery.tscn",
        // The finite store (EL-196; owner defaults of 10 Oct 2026: 3600 J, 120 W, full and enabled). The enable state is the one
        // value a player may change while the machine runs.
        Parameters:
        [
            new("capacity", ParameterUnit.Joule, ParameterKind.Real, Minimum: 60f, Maximum: 14400f, Default: 3600f, Options: [],
                EditorPolicy: ParameterEditorPolicy.Clamp),
            new("power_limit", ParameterUnit.Watt, ParameterKind.Real, Minimum: 10f, Maximum: 480f, Default: 120f, Options: [],
                EditorPolicy: ParameterEditorPolicy.Clamp),
            new("initial_charge", ParameterUnit.None, ParameterKind.Real, Minimum: 0f, Maximum: 1f, Default: 1f, Options: [],
                EditorPolicy: ParameterEditorPolicy.Clamp),
            new("enabled", ParameterUnit.None, ParameterKind.Choice, Minimum: 0f, Maximum: 1f, Default: 1f, Options: ["Disabled", "Enabled"],
                RunEdit: ParameterRunEdit.RunControl)
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, new Vector3(0f, .62f, 0f), Vector3.UnitY)
    ];

    public IReadOnlyList<ColliderDescriptor> Colliders => [];

    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(1f, 0.3f, 0f, 0.1f))
    ];

    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.ElectricalPowered)];

    public SimulationDomain SupportedDomains => SimulationDomain.Electrical;

    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.Source, null, PortSocket.Supply,
        Store: new("capacity", "power_limit", "initial_charge", "enabled"));

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("source", ElectricalRole.Source, RatedVoltage: 12f, Resistance: 0.1f, Capacitance: 0f, MaxPower: 100f)
    ];
}
