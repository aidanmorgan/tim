using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

internal static class MechanicalTransmissionValidation
{
    public static IEnumerable<BundleError> Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        var path = entry.Metadata.Id + ".Transmissions";
        if (mechanical.Transmissions is null)
        {
            yield return new(BundleErrorCode.DeclarationCount, path);
            yield break;
        }
        var ids = new HashSet<LocalMechanicalTransmissionId>();
        var outputs = PortCoordinates(entry.Ports, PortRole.Drive);
        var inputs = PortCoordinates(entry.Ports, PortRole.DriveIn);
        foreach (var item in mechanical.Transmissions)
        {
            if (item.Id.Value == 0 || !ids.Add(item.Id))
                yield return new(BundleErrorCode.DuplicateId, path);
            if (!ValidCoordinate(item.Driving, mechanical) ||
                !ValidCoordinate(item.Driven, mechanical) ||
                item.Driving == item.Driven)
                yield return new(BundleErrorCode.OutOfRange, path + ".Coordinates");
            else if (!outputs.Add(item.Driving) || !inputs.Add(item.Driven))
                yield return new(BundleErrorCode.OutOfRange, path + ".Role");
            if (!ValidRatio(item.Ratio, entry.Metadata.Parameters, RatioUnit(item.Driving.Kind, item.Driven.Kind)) || !Enum.IsDefined(item.ConstraintKind))
                yield return new(BundleErrorCode.OutOfRange, path + ".Ratio");
            if (item.CloseSeconds is { } closeSeconds && !ValidCloseSeconds(closeSeconds, entry.Metadata.Parameters))
                yield return new(BundleErrorCode.OutOfRange, path + ".CloseSeconds");
        }
    }

    /// <summary>The coordinates of every mechanical port of one role, of any coordinate kind: one connection per role.</summary>
    private static HashSet<CoordinateReference> PortCoordinates(IReadOnlyList<PortDescriptor> ports, PortRole role)
    {
        var indices = new HashSet<CoordinateReference>();
        foreach (var port in ports)
            if (port.Domain == SimulationDomain.Mechanical && port.Role == role && port.Coordinate is { } coord)
                indices.Add(coord);
        return indices;
    }

    private static bool ValidCoordinate(CoordinateReference coordinate, MechanicalPayload mechanical) =>
        coordinate.Index >= 0 && coordinate.Kind switch
        {
            CoordinateKind.Revolute => coordinate.Index < mechanical.Hinges.Count,
            CoordinateKind.Prismatic => coordinate.Index < mechanical.Sliders.Count,
            _ => false
        };

    private static ParameterUnit RatioUnit(CoordinateKind driving, CoordinateKind driven) =>
        driving == driven ? ParameterUnit.None : driving == CoordinateKind.Revolute
            ? ParameterUnit.MetrePerRadian : ParameterUnit.RadianPerMetre;

    private static bool ValidRatio(MechanicalValue ratio, IReadOnlyList<ParameterDescriptor> parameters, ParameterUnit unit)
    {
        if (ratio.Product is not null) return false;
        if (ratio.Source == MechanicalValueSource.Constant)
            return ratio.Parameter is null && float.IsFinite(ratio.Constant) && ratio.Constant != 0f;
        if (ratio.Source != MechanicalValueSource.Parameter || ratio.Constant != 0f) return false;
        var matches = parameters.Where(p => p.Key == ratio.Parameter).ToArray();
        if (matches.Length != 1) return false;
        var parameter = matches[0];
        return parameter.Kind == ParameterKind.Real && parameter.Unit == unit &&
            parameter.RunEdit == ParameterRunEdit.BuildOnly &&
            float.IsFinite(parameter.Minimum) && float.IsFinite(parameter.Maximum) &&
            (parameter.Minimum > 0f || parameter.Maximum < 0f);
    }

    private static bool ValidCloseSeconds(MechanicalValue closeSeconds, IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (closeSeconds.Product is not null) return false;
        if (closeSeconds.Source == MechanicalValueSource.Constant)
            return closeSeconds.Parameter is null && MechanicalTransmissionDeclaration.IsTimedDuration(closeSeconds.Constant);
        if (closeSeconds.Source != MechanicalValueSource.Parameter || closeSeconds.Constant != 0f) return false;
        var matches = parameters.Where(p => p.Key == closeSeconds.Parameter).ToArray();
        if (matches.Length != 1) return false;
        var parameter = matches[0];
        return parameter.Kind == ParameterKind.Real && parameter.Unit == ParameterUnit.Second &&
            parameter.RunEdit == ParameterRunEdit.BuildOnly &&
            MechanicalTransmissionDeclaration.IsTimedDuration(parameter.Minimum) &&
            MechanicalTransmissionDeclaration.IsTimedDuration(parameter.Maximum) &&
            parameter.Minimum <= parameter.Maximum;
    }
}
