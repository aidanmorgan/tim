using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Bundle;

internal static class BundleMaterial
{
    public static IReadOnlyList<ColliderDescriptor> Apply(BundleEntry entry, PackageBodyIndex body,
        IReadOnlyList<ColliderDescriptor> shapes, IReadOnlyDictionary<string, float> values)
    {
        var result = shapes.ToArray();
        foreach (var binding in entry.MaterialBindings.Where(binding => binding.Body == body))
        {
            if (!values.TryGetValue(binding.Parameter.Value, out var value))
                throw new ArgumentException("Material parameter is unresolved.");
            ValidateValue(binding.Component, value);
            for (var i = 0; i < result.Length; i++)
                result[i] = result[i] with { Material = Set(result[i].Material, binding.Component, value) };
        }
        return result;
    }

    public static IReadOnlyList<ColliderDescriptor> Shapes(BundleEntry entry, PackageBodyIndex body)
    {
        if (body.Value == 0 && entry.Footprint.Bodies > 0) return entry.Colliders;
        var sliders = entry.Mechanical?.Sliders ?? [];
        var hinges = entry.Mechanical?.Hinges ?? [];
        var index = body.Value - 1;
        if (index >= 0 && index < sliders.Count) return sliders[index].MovingBody.Colliders;
        index -= sliders.Count;
        if (index >= 0 && index < hinges.Count) return hinges[index].MovingBody.Colliders;
        throw new ArgumentException("Material body is unresolved.");
    }

    public static void Validate(BundleEntry entry)
    {
        var writers = new HashSet<(PackageBodyIndex, MaterialComponent)>();
        foreach (var binding in entry.MaterialBindings)
        {
            if (Shapes(entry, binding.Body).Count == 0 || !writers.Add((binding.Body, binding.Component)))
                throw new ArgumentException("Material body is absent or has duplicate property writers.");
            var unit = binding.Component == MaterialComponent.BounceThreshold ? ParameterUnit.MetrePerSecond : ParameterUnit.None;
            var parameters = entry.Metadata.Parameters.Where(p => p.Key == binding.Parameter.Value &&
                p.Kind == ParameterKind.Real && p.Unit == unit && p.RunEdit == ParameterRunEdit.BuildOnly).ToArray();
            if (binding.Parameter == default || parameters.Length != 1)
                throw new ArgumentException("Material parameter has an incompatible reference.");
            ValidateValue(binding.Component, parameters[0].Minimum);
            ValidateValue(binding.Component, parameters[0].Maximum);
        }
    }

    private static MaterialProperties Set(MaterialProperties material, MaterialComponent component, float value) => component switch
    {
        MaterialComponent.Restitution => material with { Restitution = value },
        MaterialComponent.Friction => material with { Friction = value },
        MaterialComponent.RollingResistance => material with { RollingResistance = value },
        MaterialComponent.BounceThreshold => material with { BounceThreshold = value },
        _ => throw new ArgumentException("Unknown material component.")
    };

    private static void ValidateValue(MaterialComponent component, float value)
    {
        var maximum = component switch
        {
            MaterialComponent.Restitution or MaterialComponent.Friction => 1f,
            MaterialComponent.RollingResistance => .1f,
            MaterialComponent.BounceThreshold => 64f,
            _ => throw new ArgumentException("Unknown material component.")
        };
        if (!float.IsFinite(value) || value < 0 || value > maximum)
            throw new ArgumentException("Material parameter leaves its physical domain.");
    }
}
