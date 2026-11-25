using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Elements.WoundSpring;

public sealed class WoundSpringElementDeclaration : ISliderElement, IHingeElement,
    IMechanicalTransmissionElement, IPrismaticRetentionElement, ISignalElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(Id: "wound_spring", ElementCategory.Motion, "Wound spring",
        "A belt winds a finite spring. Its latch holds charge after power stops; a separate trigger releases the guided plunger.",
        new(.4f, .72f, .79f, 1f), 41, "res://elements/WoundSpring/icon.svg",
        ScenePath: "res://elements/WoundSpring/wound_spring.tscn",
        [new("stiffness", ParameterUnit.NewtonPerMetre, ParameterKind.Real, 40f, 240f, 160f, []),
         new("stroke", ParameterUnit.Metre, ParameterKind.Real, .2f, .8f, .8f, []),
         new("winding_lead", ParameterUnit.MetrePerRadian, ParameterKind.Real, .025f, .2f, .1f, [])]);
    public ElementFootprint Footprint => new(3, 2, 1, 1);
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Signal;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero,
        Quaternion.Identity, 0f, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
        [new(".", new(0f, .3f, 0f, .1f)), new("Visual/Head", new(0f, .3f, 0f, .1f)),
         new("Visual/Shaft", new(0f, .3f, 0f, .1f))];
    public IReadOnlyList<PrismaticSliderDescriptor> Sliders =>
    [
        new(new(new(0f, .9f, 0f), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
            MechanicalValue.NegatedParameter("stroke"), MechanicalValue.Fixed(0f),
            MechanicalValue.FromParameter("stiffness"), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), false,
            new(MassFrameDeclaration.Constant(.5f, Vector3.Zero, new(.02048f), Quaternion.Identity,
                0f, new(0f, -9.81f, 0f))), null)
    ];
    public IReadOnlyList<RevoluteHingeDescriptor> Hinges
    {
        get
        {
            const float mass = .25f, radius = .26f, width = .1f;
            var axial = mass * radius * radius / 2f;
            var transverse = mass * (3f * radius * radius + width * width) / 12f;
            return [new(new(0), new(new(-.62f, -.55f, .5f), Quaternion.Identity),
                new(Vector3.Zero, Quaternion.Identity), -MathF.PI, MathF.PI, RevoluteTravel.Continuous,
                MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f),
                false, new(MassFrameDeclaration.Constant(mass, Vector3.Zero, new(transverse, axial, transverse),
                    new(MathF.Sqrt(.5f), 0f, 0f, MathF.Sqrt(.5f)), 0f, new(0f, -9.81f, 0f))), null)];
        }
    }
    public IReadOnlyList<MechanicalTransmissionDescriptor> Transmissions =>
        [new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Prismatic, 0),
            MechanicalValue.FromParameter("winding_lead"), TransmissionConstraintKind.NonPositiveVelocity)];
    public IReadOnlyList<PrismaticRetentionDescriptor> Retentions =>
        [new(new(1), new(CoordinateKind.Prismatic, 0), new(1))];
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.DriveIn, SimulationDomain.Mechanical, PortDirection.Input, PortRole.DriveIn,
            new("DriveIn", SocketGlyph.Text), null, new(-.62f, -.55f, .55f), Vector3.UnitZ,
            Coordinate: new(CoordinateKind.Revolute, 0), CoordinatePerRadian: -1f,
            MechanicalRim: new(new(0), new(-.62f, -.55f, .55f), Quaternion.Identity, .26f, 1f)),
        new(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn,
            new("ActivationIn", SocketGlyph.Text), PortSignalKind.Pulse, new(.62f, -.55f, .55f), Vector3.UnitZ)
    ];
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes =>
        [new SignalPhysicalRequestDescriptor(new("release"), CuriousContraptions.Gpu.LocalPhysicalRequestTarget.ForRetention(new(1)), PhysicalRequestOperation.Release)];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts => [new(PortId.ActivationIn, new(new("release"), SignalTerminal.Input))];
    public ElementPresentation Presentation => new(1.25f, PresentationAxes.None,
        [new("Visual/Latch", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedRetentionReleased,
            PresentationValueMapping.Identity, 0f, -.65f, PresentationCurve.Linear, .08125f,
            PresentationOverlap.Replace, 0f, Retention: new(1))],
        [new("Visual/Head", new(1), Vector3.Zero), new("Visual/Shaft", new(2), Vector3.Zero)],
        [new("Visual/Head", new(0f, -.32f, 0f), "Visual/Coil", new(0f, -.8f, 0f), 1.38f)],
        [new("Visual/Mark0", PresentationProperty.LocalY, "stroke", 0f, .8f),
         new("Visual/Mark1", PresentationProperty.LocalY, "stroke", -.25f, .8f),
         new("Visual/Mark2", PresentationProperty.LocalY, "stroke", -.5f, .8f),
         new("Visual/Mark3", PresentationProperty.LocalY, "stroke", -.75f, .8f),
         new("Visual/Mark4", PresentationProperty.LocalY, "stroke", -1f, .8f)]);
}
