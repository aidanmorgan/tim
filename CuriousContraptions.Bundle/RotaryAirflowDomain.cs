using System.Collections.Immutable;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class RotaryAirflowDomain
{
    public static void Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) return;
        ArgumentNullException.ThrowIfNull(mechanical.RotaryAirflowCaptures);
        var ids = new HashSet<LocalRotaryAirflowCaptureId>();
        var coordinates = new HashSet<CoordinateReference>();
        foreach (var capture in mechanical.RotaryAirflowCaptures)
        {
            if (capture is null || capture.Id.Value == 0 || !ids.Add(capture.Id) ||
                !coordinates.Add(capture.Coordinate))
                throw new ArgumentException("Duplicate rotary identity or coordinate writer.");
            if (capture.Frame.Value < 0 || capture.Frame.Value >= entry.Footprint.Bodies ||
                capture.Coordinate.Kind != CoordinateKind.Revolute || capture.Coordinate.Index < 0 ||
                capture.Coordinate.Index >= mechanical.Hinges.Count)
                throw new ArgumentException("Unresolved rotary frame or coordinate.");
            var hinge = mechanical.Hinges[capture.Coordinate.Index];
            if (hinge.Drive is not null || hinge.Damping != MechanicalValue.Fixed(0))
                throw new ArgumentException("Rotary capture requires sole ownership of coordinate damping without supplied drive.");
            Exclusions(entry, capture);
            Ratings(entry, capture);
        }
    }

    private static void Exclusions(BundleEntry entry, RotaryAirflowCaptureDescriptor capture)
    {
        ArgumentNullException.ThrowIfNull(capture.InternalOccluders);
        ArgumentNullException.ThrowIfNull(capture.Samples);
        var seen = new HashSet<LocalColliderReference>();
        foreach (var reference in capture.InternalOccluders)
            if (!seen.Add(reference) || AirflowDomain.Collider(entry, reference).Participation != CollisionParticipation.Enabled)
                throw new ArgumentException("Invalid rotary internal occluder.");
        AirflowSampleValidation.Validate(capture.Samples.ToImmutableArray());
    }

    private static void Ratings(BundleEntry entry, RotaryAirflowCaptureDescriptor capture)
    {
        AirflowDomain.Scalar(entry, capture.MaximumPitch, ParameterUnit.MetrePerRadian, true);
        AirflowDomain.Scalar(entry, capture.Response, ParameterUnit.RadianPerSecondPerNewton, true);
        AirflowDomain.Scalar(entry, capture.CutInForce, ParameterUnit.Newton, true);
        AirflowDomain.Scalar(entry, capture.MaximumRate, ParameterUnit.RadianPerSecond, true);
        MechanicalValueDomain.TryRange(capture.MaximumPitch, entry.Metadata.Parameters, ParameterUnit.MetrePerRadian, out var minPitch, out var maxPitch);
        MechanicalValueDomain.TryRange(capture.Response, entry.Metadata.Parameters, ParameterUnit.RadianPerSecondPerNewton, out var minResponse, out var maxResponse);
        MechanicalValueDomain.TryRange(capture.CutInForce, entry.Metadata.Parameters, ParameterUnit.Newton, out var minCutIn, out _);
        if (!float.IsFinite(maxPitch / minResponse) || !(minPitch / maxResponse > 0) || !(minResponse * minCutIn > 0))
            throw new ArgumentException("Rotary rating combinations are not representable.");
    }
}
