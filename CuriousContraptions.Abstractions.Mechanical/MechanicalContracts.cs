using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions.Mechanical;

public interface IMechanicalElement : IPuzzleElementDeclaration
{
    KinematicMode KinematicMode { get; }
    MassFrameDeclaration MassFrame { get; }
    PayloadPolicy Payload { get; }
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints { get; }
}

public interface ISliderElement : IMechanicalElement
{
    IReadOnlyList<PrismaticSliderDescriptor> Sliders { get; }
}

public interface IHingeElement : IMechanicalElement
{
    IReadOnlyList<RevoluteHingeDescriptor> Hinges { get; }
}

public interface IResidenceSensorElement : IMechanicalElement
{
    ResidenceSensorDescriptor ResidenceSensor { get; }
}

public interface IPlanarGuideElement : IMechanicalElement
{
    PlanarGuideDescriptor PlanarGuide { get; }
}

public interface IContactWorkElement : IMechanicalElement
{
    ContactWorkDescriptor ContactWork { get; }
}

public interface ICompliantSurfaceElement : IMechanicalElement
{
    CompliantSurfaceDescriptor CompliantSurface { get; }
}

public interface IDrivenSurfaceElement : IMechanicalElement
{
    IReadOnlyList<DrivenSurfaceDescriptor> DrivenSurfaces { get; }
}

public static class BoxFaces
{
    public static Vector3 Normal(BoxFace face) => face switch
    {
        BoxFace.PositiveX => Vector3.UnitX,
        BoxFace.NegativeX => -Vector3.UnitX,
        BoxFace.PositiveY => Vector3.UnitY,
        BoxFace.NegativeY => -Vector3.UnitY,
        BoxFace.PositiveZ => Vector3.UnitZ,
        BoxFace.NegativeZ => -Vector3.UnitZ,
        _ => throw new ArgumentException("Undefined box face.")
    };
}
