using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.ReverseTransmission;

/// <summary>Two finite-inertia shafts coupled by the shared passive reversing law.</summary>
public sealed class ReverseTransmissionElementDeclaration : IHingeElement, IMechanicalTransmissionElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "reverse_transmission",
        Category: ElementCategory.Motion,
        DisplayName: "Reverse gear",
        Description: "Reverses rotation at the same speed; two reversers restore the original direction.",
        Color: new(.84f, .61f, .28f, 1f),
        PaletteOrder: 39,
        IconPath: "res://elements/ReverseTransmission/icon.svg",
        ScenePath: "res://elements/ReverseTransmission/reverse_transmission.tscn",
        Parameters: []);
    public ElementFootprint Footprint => new(3, 2, 0, 2);
    public ElementPresentation Presentation => new(.9f, PresentationAxes.None, [],
        [new("Visual/InputWheel", new(1), Vector3.Zero), new("Visual/OutputWheel", new(2), Vector3.Zero)], [], []);
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortId.DriveIn, SimulationDomain.Mechanical, PortDirection.Input, PortRole.DriveIn, new("DriveIn", SocketGlyph.Text), null,
            new(-.36f, 0f, .4f), Vector3.UnitZ, Coordinate: new(CoordinateKind.Revolute, 0), CoordinatePerRadian: -1f,
            MechanicalRim: new(new(0), new(-.36f, 0f, .39f), Quaternion.Identity, .32f, 1f)),
        new(PortId.Drive, SimulationDomain.Mechanical, PortDirection.Output, PortRole.Drive, new("Drive", SocketGlyph.Text), null,
            new(.36f, 0f, .4f), Vector3.UnitZ, Coordinate: new(CoordinateKind.Revolute, 1), CoordinatePerRadian: -1f,
            MechanicalRim: new(new(0), new(.36f, 0f, .39f), Quaternion.Identity, .32f, 1f))
    ];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
    [
        new(".", new(0f, .3f, 0f, .1f)),
        new("Visual/InputWheel", new(0f, .3f, 0f, .1f)),
        new("Visual/OutputWheel", new(0f, .3f, 0f, .1f))
    ];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public PayloadPolicy Payload => PayloadPolicy.None;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero,
        Quaternion.Identity, 0f, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<RevoluteHingeDescriptor> Hinges => [Wheel(-.36f), Wheel(.36f)];
    private static RevoluteHingeDescriptor Wheel(float x)
    {
        const float mass = .25f, radius = .32f, width = .14f;
        var axial = mass * radius * radius / 2f;
        var transverse = mass * (3f * radius * radius + width * width) / 12f;
        return new(new(0), new(new(x, 0f, .32f), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
            -MathF.PI, MathF.PI, RevoluteTravel.Continuous,
            MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f),
            false, new(MassFrameDeclaration.Constant(mass, Vector3.Zero, new(transverse, axial, transverse),
                new(MathF.Sqrt(.5f), 0f, 0f, MathF.Sqrt(.5f)), 0f, new(0f, -9.81f, 0f)), []), null);
    }
    public IReadOnlyList<MechanicalTransmissionDescriptor> Transmissions =>
        [new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Revolute, 1), MechanicalValue.Fixed(-1f), TransmissionConstraintKind.Bilateral)];
}
