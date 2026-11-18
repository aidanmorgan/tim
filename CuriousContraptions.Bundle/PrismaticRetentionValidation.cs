using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class PrismaticRetentionValidation
{
    public static IEnumerable<BundleError> Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        var path = entry.Metadata.Id + ".Retentions";
        if (mechanical.Retentions is null)
        {
            yield return new(BundleErrorCode.DeclarationCount, path);
            yield break;
        }
        var ids = new HashSet<LocalPrismaticRetentionId>();
        var coordinates = new HashSet<CoordinateReference>();
        var transmissions = new HashSet<LocalMechanicalTransmissionId>();
        foreach (var retention in mechanical.Retentions)
        {
            if (retention.Id.Value == 0 || !ids.Add(retention.Id))
                yield return new(BundleErrorCode.DuplicateId, path);
            if (retention.Coordinate.Kind != CoordinateKind.Prismatic || retention.Coordinate.Index < 0 ||
                retention.Coordinate.Index >= mechanical.Sliders.Count || !coordinates.Add(retention.Coordinate))
            {
                yield return new(BundleErrorCode.OutOfRange, path + ".Coordinate");
                continue;
            }
            var slider = mechanical.Sliders[retention.Coordinate.Index];
            if (!ValidSlider(slider, entry.Metadata.Parameters))
                yield return new(BundleErrorCode.OutOfRange, path + ".Coordinate");
            // A missing relation table is already a DeclarationCount refusal at .Transmissions; it resolves no winding relation.
            if (mechanical.Transmissions is null)
            {
                yield return new(BundleErrorCode.OutOfRange, path + ".WindingTransmission");
                continue;
            }
            var relations = mechanical.Transmissions.Where(t => t.Id == retention.WindingTransmission).ToArray();
            if (!transmissions.Add(retention.WindingTransmission) || relations.Length != 1 ||
                relations[0].Driven != retention.Coordinate ||
                relations[0].ConstraintKind != TransmissionConstraintKind.NonPositiveVelocity ||
                relations[0].CloseSeconds.HasValue)
                yield return new(BundleErrorCode.OutOfRange, path + ".WindingTransmission");
        }
    }

    private static bool ValidSlider(PrismaticSliderDescriptor slider, IReadOnlyList<ParameterDescriptor> parameters)
    {
        return slider.Drive is null && MechanicalValueDomain.SliderTravel(slider, parameters) &&
            MechanicalValueDomain.TryRange(slider.LowerLimit, parameters, ParameterUnit.Metre, out _, out var lower) &&
            lower < -PhysicalTolerance.ActuatorStopSlopMetres &&
            MechanicalValueDomain.TryRange(slider.UpperLimit, parameters, ParameterUnit.Metre, out var upperMin, out var upperMax) &&
            PhysicsDeclarationBounds.Zero(upperMin) && PhysicsDeclarationBounds.Zero(upperMax) &&
            // Only the retention law's positive stiffness (½kq²) is checked here; a stiffness the ordinary slider validator
            // cannot resolve in N/m is refused there once, not again as a retention error.
            (!MechanicalValueDomain.TryRange(slider.Stiffness, parameters, ParameterUnit.NewtonPerMetre, out var stiffness, out _) || stiffness > 0f);
    }
}
