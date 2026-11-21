using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class CoordinateTravelDomain
{
    public static void Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) return;
        ArgumentNullException.ThrowIfNull(mechanical.CoordinateTravelObservers);
        var ids = new HashSet<LocalCoordinateTravelId>();
        foreach (var observer in mechanical.CoordinateTravelObservers)
            if (observer.Id.Value == 0 || !ids.Add(observer.Id) ||
                observer.Coordinate.Kind != CoordinateKind.Revolute || observer.Coordinate.Index < 0 ||
                observer.Coordinate.Index >= mechanical.Hinges.Count ||
                !float.IsFinite(observer.Threshold) || observer.Threshold <= 0)
                throw new ArgumentException("Invalid exact coordinate travel observer.");
    }
}
