using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.Fan;

public sealed class FanElementDeclaration : IHingeElement, IEnergyStoreElement, IAirflowSourceElement,
    ISupplyElement, ISignalElement, ISceneGeometryElement
{
    private enum SupplyMode { SelfContained, Electrical, Shaft }
    private static ChoicePredicate Select(SupplyMode mode) => new(new("supply"), (int)mode);
    private static readonly Quaternion AxisFrame = new(0, MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f));

    public ElementMetadata Metadata => new(Id: "fan", ElementCategory.Power, "Fan",
        "A fan blows along its arrow. Choose Self-contained activation, Electrical input or Shaft input. Solid obstacles block the cone.",
        new(.4f, 184f / 255f, 201f / 255f, 1), 66, "res://elements/Fan/icon.svg", ScenePath: "res://elements/Fan/fan.tscn",
        [new("supply", ParameterUnit.None, ParameterKind.Choice, 0, 2, 0, ["Self-contained", "Electrical", "Shaft"]),
         new("powered", ParameterUnit.None, ParameterKind.Choice, 0, 1, 1, ["Self-contained: Off", "Self-contained: On"]),
         new("force", ParameterUnit.Newton, ParameterKind.Real, 0, 40, 9, []),
         new("reach", ParameterUnit.Metre, ParameterKind.Real, float.Epsilon, 12, 5, []),
         new("width", ParameterUnit.Metre, ParameterKind.Real, float.Epsilon, 4, .85f, [])]);
    public ElementFootprint Footprint => new(2, 1, 0, 1);
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical | SimulationDomain.Signal;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.MemoryChanged, new("enable"))];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
        [new(".", new(1, .3f, 0, .1f)), new("Visual/Hub", new(0, .3f, 0, .1f))];

    public IReadOnlyList<RevoluteHingeDescriptor> Hinges =>
        [new(new(0), new(Vector3.Zero, AxisFrame), new(Vector3.Zero, AxisFrame), -MathF.PI, MathF.PI,
            RevoluteTravel.Continuous, MechanicalValue.Fixed(0), MechanicalValue.Fixed(0), MechanicalValue.Fixed(0),
            MechanicalValue.Fixed(0), false,
            new(MassFrameDeclaration.Constant(.15f, Vector3.Zero, new(.001176f), Quaternion.Identity, 0, Vector3.Zero)), null)];

    public IReadOnlyList<EnergyStoreDescriptor> EnergyStores =>
        [new(new(1), new(0), EnergyExpression.Direct(MechanicalValue.Fixed(14400)),
            EnergyExpression.Direct(MechanicalValue.Fixed(14400)), MechanicalValue.Fixed(0), Select(SupplyMode.SelfContained))];

    public IReadOnlyList<AirflowSourceDescriptor> AirflowSources =>
        [new(new(1), new(0), Vector3.Zero, Vector3.UnitX,
            MechanicalValue.FromParameter("reach"), MechanicalValue.FromParameter("width"), MechanicalValue.Fixed(12),
            MechanicalValue.FromParameter("force"),
            MechanicalValue.Multiply(MechanicalOperand.FromParameter("force", ParameterUnit.Newton),
                MechanicalOperand.Fixed(12, ParameterUnit.MetrePerSecond)),
            [new(new(0), new(0)), new(new(0), new(1)), new(new(0), new(2)), new(new(1), new(0))],
            [new StoredAirflowInput(Select(SupplyMode.SelfContained), new(1)) { Enable = new(new("powered"), new("enable")) },
             new ElectricalAirflowInput(Select(SupplyMode.Electrical), new("consumer")) { Enable = null },
             new CoordinateAirflowInput(Select(SupplyMode.Shaft), new(CoordinateKind.Revolute, 0), PortId.DriveIn, -1) { Enable = null }])];

    public IReadOnlyList<PortDescriptor> Ports =>
        [new(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn,
            new("ActivationIn", SocketGlyph.Text), PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX,
            Availability: Select(SupplyMode.SelfContained)),
         new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn,
            new("PowerIn", SocketGlyph.Text), null, new(-.3f, -.55f, 0), -Vector3.UnitX,
            Availability: Select(SupplyMode.Electrical)),
         new(PortId.DriveIn, SimulationDomain.Mechanical, PortDirection.Input, PortRole.DriveIn,
            new("DriveIn", SocketGlyph.Text), null, new(-.7f, 0, 0), -Vector3.UnitX,
            Coordinate: new(CoordinateKind.Revolute, 0), CoordinatePerRadian: -1,
            MechanicalRim: new(new(0), new(-.7f, 0, 0), new(0, -MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f)), .14f, 1),
            Availability: Select(SupplyMode.Shaft))];
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes => [new SignalMemoryDescriptor(new("enable"))];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts => [new(PortId.ActivationIn, new(new("enable"), SignalTerminal.Set))];
    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyConsumerDescriptor(new("consumer"), [new SupplyContinuousWorkTarget(new(1))])], [],
        [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]);
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
        [new("consumer", ElectricalRole.Consumer, 12, .3f, 0, 480)];

    public ElementPresentation Presentation => new(.75f, PresentationAxes.None,
        [FlowBinding(SupplyMode.SelfContained), FlowBinding(SupplyMode.Electrical),
         new("Visual/BladeAxis/Blades", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedRevoluteAngle,
            PresentationValueMapping.Identity, 0, 1, PresentationCurve.Linear, 0, PresentationOverlap.Replace, 0,
            Actuator: new(CoordinateKind.Revolute, 0), Selection: Select(SupplyMode.Shaft))],
        [new("Visual/Hub", new(1), Vector3.Zero)], [], []);

    private static PresentationBinding FlowBinding(SupplyMode mode) =>
        new("Visual/BladeAxis/Blades", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedFlowSpeed,
            PresentationValueMapping.Identity, 0, 1.5f, PresentationCurve.IntegratedRate, 0, PresentationOverlap.Replace, 0,
            Airflow: new(1), Selection: Select(mode));
}
