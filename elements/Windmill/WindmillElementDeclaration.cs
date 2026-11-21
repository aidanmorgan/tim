using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.Windmill;

public sealed class WindmillElementDeclaration : IHingeElement, IRotaryAirflowCaptureElement,
    ICoordinateTravelElement, ISceneGeometryElement
{
    private static readonly Quaternion AxisFrame = new(0, MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f));
    private const float TransverseInertia = (3f * .7f * .7f + .075f * .075f) / 12f;
    public ElementMetadata Metadata => new(Id: "windmill", ElementCategory.Power, "Windmill",
        "Airflow turns the blades and drives connected machines. Rear airflow reverses rotation; loads slow the shaft.",
        new(.4f, .721569f, .788235f, 1), 67, "res://elements/Windmill/icon.svg",
        ScenePath: "res://elements/Windmill/windmill.tscn",
        [new("radians_per_force", ParameterUnit.RadianPerSecondPerNewton, ParameterKind.Real, .1f, 4, .6666667f, [])]);
    public ElementFootprint Footprint => new(2, 1, 0, 1);
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
        [new(".", new(0, .3f, 0, .1f)), new("Visual/Rotor", new(0, .3f, 0, .1f))];
    public IReadOnlyList<RevoluteHingeDescriptor> Hinges =>
        [new(new(0), new(new(-.12f, 0, 0), AxisFrame), new(Vector3.Zero, AxisFrame),
            -MathF.PI, MathF.PI, RevoluteTravel.Continuous,
            MechanicalValue.Fixed(0), MechanicalValue.Fixed(0), MechanicalValue.Fixed(0),
            MechanicalValue.Fixed(0), false,
            new(MassFrameDeclaration.Constant(1, Vector3.Zero, new(.245f, TransverseInertia, TransverseInertia),
                Quaternion.Identity, 0, Vector3.Zero), []), null)];
    public IReadOnlyList<RotaryAirflowCaptureDescriptor> RotaryAirflowCaptures =>
        [new(new(1), new(0), new(CoordinateKind.Revolute, 0),
            [new(new(-.12f, .42f, 0), .25f), new(new(-.12f, -.42f, 0), .25f),
             new(new(-.12f, 0, .42f), .25f), new(new(-.12f, 0, -.42f), .25f)],
            [new(new(0), new(0)), new(new(0), new(1)), new(new(0), new(2)), new(new(0), new(3)),
             new(new(1), new(0)), new(new(1), new(1)), new(new(1), new(2)), new(new(1), new(3)), new(new(1), new(4))],
            MechanicalValue.Fixed(.4f), MechanicalValue.FromParameter("radians_per_force"),
            MechanicalValue.Fixed(.05f), MechanicalValue.Fixed(12))];
    public IReadOnlyList<CoordinateTravelDescriptor> CoordinateTravelObservers =>
        [new(new(1), new(CoordinateKind.Revolute, 0), 2 * MathF.PI)];
    public IReadOnlyList<EventDescriptor> Events =>
        [new(SimulationEventKind.CoordinateTravelReached, CoordinateTravel: new LocalCoordinateTravelId(1))];
    public IReadOnlyList<PortDescriptor> Ports =>
        [new(PortId.Drive, SimulationDomain.Mechanical, PortDirection.Output, PortRole.Drive,
            new("Drive", SocketGlyph.Text), null, new(.25f, -.1f, .52f), Vector3.UnitZ,
            Coordinate: new(CoordinateKind.Revolute, 0), CoordinatePerRadian: 1,
            MechanicalRim: new(new(0), new(.25f, -.1f, .44f), Quaternion.Identity, .24f, -1))];
    public ElementPresentation Presentation => new(.9f, PresentationAxes.None,
        [new("Visual/Output", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedRevoluteAngle,
            PresentationValueMapping.Identity, 0, -1, PresentationCurve.Linear, 0, PresentationOverlap.Replace, 0,
            Actuator: new(CoordinateKind.Revolute, 0))],
        [new("Visual/Rotor", new(1), Vector3.Zero)], [], []);
}
