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

public interface IEnergyStoreElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.EnergyStoreDescriptor> EnergyStores { get; }
}

public interface IAirflowSourceElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.AirflowSourceDescriptor> AirflowSources { get; }
}

public interface IAirflowReceiverElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.AirflowReceiverDescriptor> AirflowReceivers { get; }
}

public interface ICoordinateTravelElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.CoordinateTravelDescriptor> CoordinateTravelObservers { get; }
}

public interface IRotaryAirflowCaptureElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.RotaryAirflowCaptureDescriptor> RotaryAirflowCaptures { get; }
}

public interface IChamberRegionElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.ChamberRegionDescriptor> ChamberRegions { get; }
}

public interface IFiniteWorkActuatorElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.FiniteWorkActuatorDescriptor> FiniteWorkActuators { get; }
}

public interface IContactWorkElement : IMechanicalElement
{
    IReadOnlyList<ContactWorkDescriptor> ContactWorks { get; }
}

public interface ICompliantSurfaceElement : IMechanicalElement
{
    CompliantSurfaceDescriptor CompliantSurface { get; }
}

public interface IDrivenSurfaceElement : IMechanicalElement
{
    IReadOnlyList<DrivenSurfaceDescriptor> DrivenSurfaces { get; }
}

public interface IPrismaticRetentionElement : IMechanicalElement
{
    IReadOnlyList<PrismaticRetentionDescriptor> Retentions { get; }
}

public interface IMechanicalTransmissionElement : IMechanicalElement
{
    IReadOnlyList<MechanicalTransmissionDescriptor> Transmissions { get; }
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

public interface IGasStoreElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.GasStoreDescriptor> GasStores { get; }
}

public interface IGasShapeElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.GasShapeDescriptor> GasShapes { get; }
}

public interface IContactIntegrityElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.ContactIntegrityDescriptor> IntegrityObservers { get; }
}

public interface IAmbientLoadElement : IMechanicalElement
{
    IReadOnlyList<CuriousContraptions.Gpu.AmbientLoadDescriptor> AmbientLoads { get; }
}
