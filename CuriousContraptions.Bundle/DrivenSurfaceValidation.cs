using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

internal static class DrivenSurfaceValidation
{
    public static IEnumerable<BundleError> Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        var ids = new HashSet<LocalDrivenSurfaceId>();
        var faces = new HashSet<(PackageBodyIndex, BodyColliderIndex, BoxFace)>();
        foreach (var surface in mechanical.DrivenSurfaces)
        {
            var path = entry.Metadata.Id + ".DrivenSurfaces";
            if (surface.Id.Value == 0 || !ids.Add(surface.Id) ||
                !faces.Add((surface.Carrier, surface.Collider, surface.Face)))
                yield return new(BundleErrorCode.DuplicateId, path);
            if (!Enum.IsDefined(surface.Face) || surface.Source.Kind != CoordinateKind.Revolute ||
                surface.Source.Index < 0 || surface.Source.Index >= mechanical.Hinges.Count)
                yield return new(BundleErrorCode.OutOfRange, path + ".SourceOrFace");
            var colliders = Colliders(entry, surface.Carrier);
            if (colliders is null || surface.Collider.Value < 0 || surface.Collider.Value >= colliders.Count)
            {
                yield return new(BundleErrorCode.UnresolvedReference, path + ".Collider");
                continue;
            }
            var collider = colliders[surface.Collider.Value];
            if (collider.ShapeKind != ColliderShapeKind.Box ||
                !ValidDirection(surface, collider.Rotation))
                yield return new(BundleErrorCode.OutOfRange, path + ".Geometry");
            if (!ValidTravel(surface.TravelPerRadian, entry.Metadata.Parameters))
                yield return new(BundleErrorCode.OutOfRange, path + ".TravelPerRadian");
        }
    }

    private static IReadOnlyList<ColliderDescriptor>? Colliders(BundleEntry entry, PackageBodyIndex body)
    {
        if (body.Value == 0) return entry.Colliders;
        var mechanical = entry.Mechanical!;
        var index = body.Value - 1;
        if (index < 0) return null;
        if (index < mechanical.Sliders.Count) return mechanical.Sliders[index].MovingBody.Colliders;
        index -= mechanical.Sliders.Count;
        return index < mechanical.Hinges.Count ? mechanical.Hinges[index].MovingBody.Colliders : null;
    }

    private static bool ValidDirection(DrivenSurfaceDescriptor surface, Quaternion rotation)
    {
        if (!Enum.IsDefined(surface.Face)) return false;
        var length = surface.LocalDirection.LengthSquared();
        var normal = Vector3.Transform(BoxFaces.Normal(surface.Face), rotation);
        return float.IsFinite(length) && MathF.Abs(length - 1f) <= 1e-4f &&
            MathF.Abs(Vector3.Dot(normal, surface.LocalDirection)) <= 1e-4f;
    }

    private static bool ValidTravel(MechanicalValue travel, IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (travel.Source == MechanicalValueSource.Constant)
            return travel.Parameter is null && float.IsFinite(travel.Constant) && travel.Constant != 0f;
        if (travel.Source != MechanicalValueSource.Parameter || travel.Constant != 0f) return false;
        var matches = parameters.Where(parameter => parameter.Key == travel.Parameter).ToArray();
        if (matches.Length != 1) return false;
        var value = matches[0];
        return value.Kind == ParameterKind.Real && value.Unit == ParameterUnit.MetrePerRadian &&
            value.RunEdit == ParameterRunEdit.BuildOnly &&
            float.IsFinite(value.Minimum) && float.IsFinite(value.Maximum) &&
            (value.Minimum > 0f || value.Maximum < 0f);
    }
}
