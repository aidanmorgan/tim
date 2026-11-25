using CuriousContraptions.Presentation;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.Cannon;

public sealed class CannonElementDeclaration : IEnergyStoreElement, IChamberRegionElement,
    IFiniteWorkActuatorElement, ISupplyElement, ISignalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(Id: "cannon", Category: ElementCategory.Motion, DisplayName: "Toy cannon",
        Description: "Load a ball through the open muzzle. A separate electrical supply charges the finite store; an activation fires only a fully charged, seated single ball through a clear muzzle. Empty or obstructed attempts retain charge. It never creates ammunition.",
        Color: new(.4f, .72f, .79f, 1f), PaletteOrder: 42, IconPath: "res://elements/Cannon/icon.svg", ScenePath: "res://elements/Cannon/cannon.tscn",
        Parameters: [new("capacity", ParameterUnit.Joule, ParameterKind.Real, 1f, 100f, 45f, []),
         new("charge_power", ParameterUnit.Watt, ParameterKind.Real, 1f, 100f, 30f, [])]);
    public ElementFootprint Footprint => new(1, 0, 0, 0);
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical | SimulationDomain.Signal;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0f, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(0f, .3f, 0f, .1f))];
    public IReadOnlyList<EnergyStoreDescriptor> EnergyStores =>
        [new(new(1), new(0), EnergyExpression.Direct(MechanicalValue.FromParameter("capacity")),
            EnergyExpression.Direct(MechanicalValue.Fixed(0f)), MechanicalValue.FromParameter("charge_power"))];
    public IReadOnlyList<ChamberRegionDescriptor> ChamberRegions =>
        [new(new(1), new(0), new(0), Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2f),
            .48f, .55f, .6f, .84f, .001f, 1f)];
    public IReadOnlyList<FiniteWorkActuatorDescriptor> FiniteWorkActuators =>
        [new(new(1), new(1), new(1), MechanicalValue.Fixed(12f), .02f)];
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn,
            new("PowerIn", SocketGlyph.Text), null, new(-.45f, -.55f, .55f), Vector3.UnitZ),
        new(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn,
            new("ActivationIn", SocketGlyph.Text), PortSignalKind.Pulse, new(.15f, -.55f, .55f), Vector3.UnitZ)
    ];
    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyConsumerDescriptor(new("charge"), [new SupplyEnergyStoreTarget(new(1))])], [],
        [new(PortId.PowerIn, new(new("charge"), SupplyTerminal.PowerIn))]);
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
        [new("charge", ElectricalRole.Consumer, 12f, 10f, 0f, 100f)];
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes =>
        [new SignalPhysicalRequestDescriptor(new("release"), LocalPhysicalRequestTarget.ForActuator(new(1)), PhysicalRequestOperation.Release)];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts =>
        [new(PortId.ActivationIn, new(new("release"), SignalTerminal.Input))];
    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.FiniteWorkReleased, FiniteWork: new(1))];
    public ElementPresentation Presentation => new(1.1f, PresentationAxes.None,
    [
        new("Visual/ChargeBar", PresentationProperty.ScaleX, PresentationDriver.CommittedStoredFraction,
            PresentationValueMapping.ClampToEndpoints, .001f, 1f, PresentationCurve.Linear, 0f,
            PresentationOverlap.Replace, 0f, Store: new(1)),
        new("Visual/ReadyFlag", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedFiniteWorkState,
            PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f,
            PresentationOverlap.Replace, 0f, FiniteWork: new(1),
            FiniteWorkValues: new(-70f * MathF.PI / 180f, -70f * MathF.PI / 180f, -70f * MathF.PI / 180f,
                0f, -70f * MathF.PI / 180f, -70f * MathF.PI / 180f)),
        Indicator(PresentationProperty.AlbedoRed, 85f / 255f, 102f / 255f, 247f / 255f),
        Indicator(PresentationProperty.AlbedoGreen, 101f / 255f, 184f / 255f, 203f / 255f),
        Indicator(PresentationProperty.AlbedoBlue, 115f / 255f, 201f / 255f, 82f / 255f),
        new("Visual/Sleeve", PresentationProperty.LocalX, PresentationDriver.Event,
            PresentationValueMapping.Identity, -.22f, -.38f, PresentationCurve.SmoothRiseFall, .53f,
            PresentationOverlap.HyperbolicTangentSum, 0f, Event: SimulationEventKind.FiniteWorkReleased,
            FiniteWork: new(1), ImpulsePeak: new(.08f / .53f), ImpulseVisibility: AnimationImpulseVisibility.DeferUntilVisible, ImpulseTiming: AnimationImpulseTiming.EventTimeRetainFirstFrame)
    ], [], [], []);
    private static PresentationBinding Indicator(PresentationProperty property, float slate, float cyan, float gold) =>
        new("Visual/Indicator", property, PresentationDriver.CommittedFiniteWorkState,
            PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f,
            PresentationOverlap.Replace, 0f, FiniteWork: new(1),
            FiniteWorkValues: new(slate, slate, cyan, gold, slate, slate));
}
