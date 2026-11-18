using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

internal static class ClippedExtrusionValidation
{
    public static IEnumerable<BundleError> Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        if (mechanical.Extrusions is null)
        {
            yield return new(BundleErrorCode.OutOfRange, entry.Metadata.Id + ".Extrusions");
            yield break;
        }
        var written = new HashSet<(PackageBodyIndex, BodyColliderIndex)>();
        foreach (var extrusion in mechanical.Extrusions)
        {
            var path = entry.Metadata.Id + ".Extrusions";
            if (!written.Add((extrusion.MaterialBody, extrusion.Collider)))
                yield return new(BundleErrorCode.DuplicateId, path + ".Collider");
            if (extrusion.Coordinate.Kind != CoordinateKind.Prismatic || extrusion.Coordinate.Index < 0 ||
                extrusion.Coordinate.Index >= mechanical.Sliders.Count || extrusion.ClippingBody.Value != 0 ||
                extrusion.MaterialBody.Value != extrusion.Coordinate.Index + 1)
            {
                yield return new(BundleErrorCode.UnresolvedReference, path + ".CoordinateBodies");
                continue;
            }
            var slider = mechanical.Sliders[extrusion.Coordinate.Index];
            if (extrusion.Collider.Value < 0 || extrusion.Collider.Value >= slider.MovingBody.Colliders.Count)
            {
                yield return new(BundleErrorCode.UnresolvedReference, path + ".Collider");
                continue;
            }
            var collider = slider.MovingBody.Colliders[extrusion.Collider.Value];
            if (!Domain(extrusion, slider, entry.Metadata.Parameters) || !Aligned(extrusion, slider, collider))
                yield return new(BundleErrorCode.OutOfRange, path + ".Geometry");
            if (mechanical.DrivenSurfaces.Any(surface => surface.Carrier == extrusion.MaterialBody && surface.Collider == extrusion.Collider))
                yield return new(BundleErrorCode.DuplicateId, path + ".GeometryWriter");
        }
    }

    private static bool Domain(ClippedExtrusionDescriptor extrusion, PrismaticSliderDescriptor slider,
        IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (!float.IsFinite(extrusion.Cut) || !float.IsFinite(extrusion.DistalOffset) ||
            MathF.Abs(extrusion.Cut) > 16f || MathF.Abs(extrusion.DistalOffset) > 16f ||
            !float.IsFinite(extrusion.HalfExtents.X) || !float.IsFinite(extrusion.HalfExtents.Y) ||
            extrusion.HalfExtents.X < 1f / 1024f || extrusion.HalfExtents.X > 16f ||
            extrusion.HalfExtents.Y < 1f / 1024f || extrusion.HalfExtents.Y > 16f ||
            !MechanicalValueDomain.TryRange(slider.LowerLimit, parameters, ParameterUnit.Metre, out var lower, out _) ||
            !MechanicalValueDomain.TryRange(slider.UpperLimit, parameters, ParameterUnit.Metre, out _, out var upper)) return false;
        return extrusion.DistalOffset + lower - extrusion.Cut > 0f &&
            extrusion.DistalOffset + upper - extrusion.Cut <= 32f;
    }

    private static bool Aligned(ClippedExtrusionDescriptor extrusion, PrismaticSliderDescriptor slider, ColliderDescriptor collider)
    {
        var centre = slider.MovingFrame.Position + Vector3.Transform(
            new Vector3(0f, (extrusion.Cut + extrusion.DistalOffset) * .5f, 0f), slider.MovingFrame.Rotation);
        var half = new Vector3(extrusion.HalfExtents.X, (extrusion.DistalOffset - extrusion.Cut) * .5f, extrusion.HalfExtents.Y);
        // Same authored-frame admission precision as slider rotations; this excludes an independent collider axis/offset.
        return collider.ShapeKind == ColliderShapeKind.Box && Vector3.Distance(collider.Offset, centre) <= 1e-4f &&
            Vector3.Distance(collider.HalfExtents, half) <= 1e-4f &&
            MathF.Abs(Quaternion.Dot(collider.Rotation, slider.MovingFrame.Rotation)) >= 1f - 1e-4f;
    }
}
