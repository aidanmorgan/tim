using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Elements.Balloon;

public sealed class BalloonElementDeclaration : IMechanicalElement, ISceneGeometryElement,
    IParameterizedGeometryElement, IParameterizedMaterialElement, IGasStoreElement, IGasShapeElement,
    IContactIntegrityElement, IAmbientLoadElement, IAirflowReceiverElement
{
    private const float DryRadius = .0625f;
    private const float VolumeFactor = 4f * MathF.PI / 3f;
    public ElementMetadata Metadata => new(Id: "balloon", Category: ElementCategory.Motion, DisplayName: "Balloon",
        Description: "A sealed buoyant envelope. Radius is measured in Standard atmosphere. Tie at its center; strong impacts release its gas.",
        Color: new(237f / 255f, 99f / 255f, 120f / 255f, 1), PaletteOrder: 65,
        IconPath: "res://elements/Balloon/icon.svg", ScenePath: "res://elements/Balloon/balloon.tscn",
        Parameters: [
            new("mass", ParameterUnit.Kilogram, ParameterKind.Real, .125f, 8, .5f, []),
            new("bounce", ParameterUnit.None, ParameterKind.Real, 0, 1, .2f, []),
            new("radius", ParameterUnit.Metre, ParameterKind.Real, .18f, .5f, .36f, []),
            new("buoyancy", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, 0, 16, 11.5f, []),
            new("drag", ParameterUnit.InverseSecond, ParameterKind.Real, 0, 4, .4f, [])
        ]);
    public ElementPresentation Presentation => new(.45f, PresentationAxes.None,
        [
            new("Envelope", PresentationProperty.UniformScale, PresentationDriver.CommittedGasRadius,
                PresentationValueMapping.ClampToEndpoints, DryRadius, 16, PresentationCurve.Linear, 0,
                PresentationOverlap.Replace, 0, GasShape: new(new(0), new(0))),
            new("Neck", PresentationProperty.LocalY, PresentationDriver.CommittedGasRadius,
                PresentationValueMapping.Identity, 0, -1, PresentationCurve.Linear, 0,
                PresentationOverlap.Replace, 0, GasShape: new(new(0), new(0)))
        ], [], [], []);
    public ElementFootprint Footprint => new(1, 1, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports =>
        [new(PortId.Tie, SimulationDomain.Rope, PortDirection.Bidirectional, PortRole.Tie,
            new("Center tie", SocketGlyph.Text), null, Vector3.Zero, -Vector3.UnitY, RopeAttachmentRole.Load)];
    public IReadOnlyList<AirflowReceiverDescriptor> AirflowReceivers =>
        [new(new(1), new(new(0), new(0)), AirflowSampling.ColliderNormalized,
            [new(new(0, .5f, 0), .25f), new(new(0, -.5f, 0), .25f),
             new(new(0, 0, .5f), .25f), new(new(0, 0, -.5f), .25f)])];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(.2f, .3f, 0, .1f))];
    public IReadOnlyList<ColliderParameterBinding> ColliderBindings =>
        [new(0, GeometryComponent.HalfExtentX, "radius", 1, 0),
         new(0, GeometryComponent.HalfExtentY, "radius", 1, 0),
         new(0, GeometryComponent.HalfExtentZ, "radius", 1, 0)];
    public IReadOnlyList<MaterialParameterBinding> MaterialBindings =>
        [new(new(0), MaterialComponent.Restitution, new("bounce"))];
    public IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings => [];
    public IReadOnlyList<EventDescriptor> Events => [new(SimulationEventKind.IntegrityBroken, Integrity: new(1))];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Rope;
    public KinematicMode KinematicMode => KinematicMode.Dynamic;
    public PayloadPolicy Payload => PayloadPolicy.CaptureTarget;
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public MassFrameDeclaration MassFrame => new(
        new(BodyMassSourceKind.CenteredInventory, new(new ParameterId("mass"), 0, 1, 0, 0),
            default, [new(1)], new(.4f * DryRadius * DryRadius)),
        MassVector.Constant(Vector3.Zero), Quaternion.Identity, MassScalar.Constant(0),
        WorldAccelerationDeclaration.Constant(new(0, -9.81f, 0)));
    public IReadOnlyList<GasStoreDescriptor> GasStores =>
        [new(new(1), new(0), new(2000, 3000, 1, 1000, 1, 200000),
            new(new ParameterId("radius"), -VolumeFactor * DryRadius * DryRadius * DryRadius, 0, 0, VolumeFactor),
            102325, 300, 1000, .001f)];
    public IReadOnlyList<GasShapeDescriptor> GasShapes => [new(new(1), new(0), new(0), DryRadius)];
    public IReadOnlyList<ContactIntegrityDescriptor> IntegrityObservers => [new(new(1), new(0), new(0), new(1), 3, .1f)];
    public IReadOnlyList<AmbientLoadDescriptor> AmbientLoads =>
        [new(new(0), new(new ParameterId("buoyancy"), Vector3.Zero, Vector3.UnitY),
            new(new ParameterId("drag"), 0, 1, 0, 0), new LocalGasStoreId(1))];
}
