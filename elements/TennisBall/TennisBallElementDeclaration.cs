using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.TennisBall;

public sealed class TennisBallElementDeclaration : IMechanicalElement, ISceneGeometryElement,
    IParameterizedGeometryElement, IParameterizedMaterialElement, IAmbientLoadElement
{
    public ElementMetadata Metadata => new(Id: "tennis", Category: ElementCategory.Motion, DisplayName: "Tennis ball",
        Description: "A lightweight, high-bounce ball with configurable material.", Color: new(184f / 255f, 219f / 255f, 89f / 255f, 1f),
        PaletteOrder: 64, IconPath: "res://elements/TennisBall/icon.svg", ScenePath: "res://elements/TennisBall/tennis.tscn",
        Parameters: [
            new("mass", ParameterUnit.Kilogram, ParameterKind.Real, 1f / 1024f, 1024f, .35f, []),
            new("bounce", ParameterUnit.None, ParameterKind.Real, 0f, 1f, .85f, []),
            new("radius", ParameterUnit.Metre, ParameterKind.Real, 1f / 16f, 16f, .25f, []),
            new("buoyancy", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, 0f, 16f, 0f, []),
            new("drag", ParameterUnit.InverseSecond, ParameterKind.Real, 0f, .125f, .04f, [])
        ]);

    public ElementPresentation Presentation => new(.35f, PresentationAxes.None, [], [], [],
        [new("Visual", PresentationProperty.UniformScale, "radius", 4f, 0f)]);
    public ElementFootprint Footprint => new(1, 1, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports => [];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(.85f, .3f, 0f, .1f))];
    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
    [
        new(0, GeometryComponent.HalfExtentX, "radius", 1f, 0f),
        new(0, GeometryComponent.HalfExtentY, "radius", 1f, 0f),
        new(0, GeometryComponent.HalfExtentZ, "radius", 1f, 0f)
    ];
    public IReadOnlyList<MaterialParameterBinding> MaterialBindings =>
        [new(new(0), MaterialComponent.Restitution, new("bounce"))];
    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;
    public KinematicMode KinematicMode => KinematicMode.Dynamic;
    public PayloadPolicy Payload => PayloadPolicy.CaptureTarget;
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    public MassFrameDeclaration MassFrame => new(
        BodyMassSourceDeclaration.Declared(new(new ParameterId("mass"), 0f, 1f, 0f, 0f),
        new(PrincipalMomentOperation.ScaledMassLengthSquared, default,
            new(new ParameterId("mass"), 0f), new(new ParameterId("radius"), 0f), new(.4f))), MassVector.Constant(Vector3.Zero),
        Quaternion.Identity, MassScalar.Constant(0),
        WorldAccelerationDeclaration.Constant(new(0f, -9.81f, 0f)));

    public IReadOnlyList<AmbientLoadDescriptor> AmbientLoads =>
        [new(new(0), new(new ParameterId("buoyancy"), Vector3.Zero, Vector3.UnitY),
            new(new ParameterId("drag"), 0, 1, 0, 0), null)];
}
