using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.Baseball;

public sealed class BaseballElementDeclaration : IMechanicalElement, ISceneGeometryElement, IAmbientLoadElement, IAirflowReceiverElement
{
    public ElementMetadata Metadata => new(Id: "baseball", ElementCategory.Motion, "Baseball",
        "A medium-light seamed ball with moderate bounce.", new(232f / 255f, 220f / 255f, 203f / 255f, 1),
        68, "res://elements/Baseball/icon.svg", ScenePath: "res://elements/Baseball/baseball.tscn", []);
    public ElementPresentation Presentation => new(.35f, PresentationAxes.None, [], [], [], []);
    public ElementFootprint Footprint => new(1, 1, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports => [];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(.35f, .3f, .03f, .1f))];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;
    public KinematicMode KinematicMode => KinematicMode.Dynamic;
    public PayloadPolicy Payload => PayloadPolicy.CaptureTarget;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(.65f, Vector3.Zero,
        new Vector3(.01625f), Quaternion.Identity, 0f, new(0, -9.81f, 0));
    public IReadOnlyList<AmbientLoadDescriptor> AmbientLoads =>
        [new(new(0), WorldAccelerationDeclaration.Constant(Vector3.Zero), MassScalar.Constant(.04f), null)];
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<AirflowReceiverDescriptor> AirflowReceivers =>
        [new(new(1), new(new(0), new(0)), AirflowSampling.ColliderNormalized,
            [new(new(0, .5f, 0), .25f), new(new(0, -.5f, 0), .25f),
             new(new(0, 0, .5f), .25f), new(new(0, 0, -.5f), .25f)])];
}
