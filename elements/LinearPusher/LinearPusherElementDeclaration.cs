using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.LinearPusher;

public sealed class LinearPusherElementDeclaration : ISliderElement, ISupplyElement, ISceneGeometryElement, IClippedExtrusionElement
{
    private static readonly Quaternion AxialFrame = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2f);
    public ElementMetadata Metadata => new(Id: "linear_pusher", ElementCategory.Power, "Linear pusher",
        "Separate extend and retract commands move the head. Both commands, no command, or lost power lock its current position.",
        new(.4f, .72f, .79f, 1f), 40, "res://elements/LinearPusher/icon.svg", ScenePath: "res://elements/LinearPusher/linear_pusher.tscn",
        [new("stroke", ParameterUnit.Metre, ParameterKind.Real, .25f, 3f, 2.4f, []),
         new("speed", ParameterUnit.MetrePerSecond, ParameterKind.Real, .25f, 4f, 1.5f, []),
         new("acceleration", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, 1f, 30f, 12f, []),
         new("force", ParameterUnit.Newton, ParameterKind.Real, 1f, 100f, 40f, [])]);
    public ElementFootprint Footprint => new(2, 1, 1, 0);
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero,
        Quaternion.Identity, 0f, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(0f, .3f, 0f, .1f)), new("Visual/Head", new(0f, .3f, 0f, .1f))];
    public IReadOnlyList<PrismaticSliderDescriptor> Sliders =>
    [
        new(new(new(.85f, 0, 0), AxialFrame), new(Vector3.Zero, AxialFrame),
            MechanicalValue.Fixed(0f), MechanicalValue.FromParameter("stroke"), MechanicalValue.Fixed(0f),
            MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), false,
            new(MassFrameDeclaration.Constant(.5f, Vector3.Zero, new(.0125f), Quaternion.Identity, 0f, new(0, -9.81f, 0))),
            new(MechanicalValue.FromParameter("stroke"), MechanicalValue.FromParameter("speed"),
                MechanicalValue.FromParameter("acceleration"), MechanicalValue.FromParameter("force"),
                MechanicalValue.Multiply(MechanicalOperand.FromParameter("force", ParameterUnit.Newton),
                    MechanicalOperand.FromParameter("speed", ParameterUnit.MetrePerSecond)),
                PrismaticTargetSelection.Directional, PrismaticInactiveResponse.SelfLock, new("positive"), new("negative")))
    ];
    public IReadOnlyList<ClippedExtrusionDescriptor> Extrusions =>
        [new(new(CoordinateKind.Prismatic, 0), new(1), new(0), new(1), -.25f, 0f, new(.06f))];
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, new("PowerIn", SocketGlyph.Power), null, new(-.45f, -.4f, .5f), Vector3.UnitZ),
        new(new("ExtendIn"), SimulationDomain.Electrical, PortDirection.Input, PortRole.FirstIn, new("Extend", SocketGlyph.Extend), null, new(.15f, -.4f, .5f), Vector3.UnitZ),
        new(new("RetractIn"), SimulationDomain.Electrical, PortDirection.Input, PortRole.SecondIn, new("Retract", SocketGlyph.Retract), null, new(.55f, -.4f, .5f), Vector3.UnitZ),
        new(new("RetractedOut"), SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, new("Retracted", SocketGlyph.Retracted), null, new(-.45f, .4f, .45f), Vector3.UnitZ),
        new(new("ExtendedOut"), SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, new("Extended", SocketGlyph.Extended), null, new(.45f, .4f, .45f), Vector3.UnitZ)
    ];
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
        [new("motor", ElectricalRole.Consumer, 12f, 24f, 0f, 400f), new("distribution", ElectricalRole.LogicGate, 12f, .05f, 0f, 400f)];
    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyLogicDescriptor(new("power"), LogicGateOperation.Nor),
         new SupplyConsumerDescriptor(new("motor"), [new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0))]),
         new SupplyConsumerDescriptor(new("positive"), []), new SupplyConsumerDescriptor(new("negative"), []),
         new SupplyContactDescriptor(new("lower"), new PrismaticContactControl(new(CoordinateKind.Prismatic, 0), CoordinateEndpoint.Lower)),
         new SupplyContactDescriptor(new("upper"), new PrismaticContactControl(new(CoordinateKind.Prismatic, 0), CoordinateEndpoint.Upper))],
        [new(new(new("power"), SupplyTerminal.Output), new(new("motor"), SupplyTerminal.PowerIn)),
         new(new(new("power"), SupplyTerminal.Output), new(new("lower"), SupplyTerminal.PowerIn)),
         new(new(new("power"), SupplyTerminal.Output), new(new("upper"), SupplyTerminal.PowerIn))],
        [new(PortId.PowerIn, new(new("power"), SupplyTerminal.PowerIn)),
         new(new("ExtendIn"), new(new("positive"), SupplyTerminal.PowerIn)),
         new(new("RetractIn"), new(new("negative"), SupplyTerminal.PowerIn)),
         new(new("RetractedOut"), new(new("lower"), SupplyTerminal.Output)),
         new(new("ExtendedOut"), new(new("upper"), SupplyTerminal.Output))]);
    public ElementPresentation Presentation => new(1.3f, PresentationAxes.None,
        [Indicator(PresentationProperty.AlbedoRed, .333f, .4f, .969f),
         Indicator(PresentationProperty.AlbedoGreen, .396f, .72f, .796f),
         Indicator(PresentationProperty.AlbedoBlue, .451f, .79f, .322f)],
        [new("Visual/Head", new(1), Vector3.Zero)],
        [new("Visual/Head", Vector3.Zero, "Visual/Rod", new(.6f, 0, 0), .25f)], []);
    private static PresentationBinding Indicator(PresentationProperty property, float held, float moving, float blocked) =>
        new("Visual/Indicator", property, PresentationDriver.CommittedPrismaticPhase, PresentationValueMapping.StateValues,
            0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
            Actuator: new(CoordinateKind.Prismatic, 0),
            PrismaticValues: new(held, moving, moving, blocked, blocked, held));
}
