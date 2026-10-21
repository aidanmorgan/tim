using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Bundle;

/// <summary>The shared bounded evaluator used by placement, decoding and worker compilation.</summary>
public static class BundleGeometry
{
    public static IReadOnlyDictionary<string, float> Parameters(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null)
    {
        var values = entry.Metadata.Parameters.ToDictionary(p => p.Key, p => p.Default, StringComparer.Ordinal);
        if (supplied is not null)
            foreach (var (key, value) in supplied)
            {
                var parameter = entry.Metadata.Parameters.SingleOrDefault(p => p.Key == key);
                if (!values.ContainsKey(key) || !float.IsFinite(value) || value < parameter.Minimum || value > parameter.Maximum)
                    throw new ArgumentException($"'{entry.Metadata.Id}' has an invalid parameter '{key}'.");
                values[key] = value;
            }
        return values;
    }

    public static IReadOnlyList<ColliderDescriptor> Colliders(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null)
    {
        if (entry.Colliders.Count == 0) throw new ArgumentException($"'{entry.Metadata.Id}' has no baked colliders.");
        var parameters = Parameters(entry, supplied);
        var colliders = entry.Colliders.ToArray();
        foreach (var binding in entry.ColliderBindings)
        {
            if ((uint)binding.Collider >= (uint)colliders.Length) throw new ArgumentException("Collider binding is out of range.");
            var value = binding.Multiplier * parameters[binding.Parameter] + binding.Offset;
            var collider = colliders[binding.Collider];
            collider = binding.Component switch
            {
                GeometryComponent.HalfExtentX => collider with { HalfExtents = collider.HalfExtents with { X = value } },
                GeometryComponent.HalfExtentY => collider with { HalfExtents = collider.HalfExtents with { Y = value } },
                GeometryComponent.HalfExtentZ => collider with { HalfExtents = collider.HalfExtents with { Z = value } },
                GeometryComponent.OffsetX => collider with { Offset = collider.Offset with { X = value } },
                GeometryComponent.OffsetY => collider with { Offset = collider.Offset with { Y = value } },
                GeometryComponent.OffsetZ => collider with { Offset = collider.Offset with { Z = value } },
                _ => throw new ArgumentException("Undefined geometry component.")
            };
            if (!float.IsFinite(value) || collider.HalfExtents.X <= 0 || collider.HalfExtents.Y <= 0 || collider.HalfExtents.Z <= 0)
                throw new ArgumentException("Evaluated collider has invalid geometry.");
            colliders[binding.Collider] = collider;
        }
        return colliders;
    }

    public static IReadOnlyList<TubeMouthDescriptor> Mouths(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null)
    {
        var parameters = Parameters(entry, supplied);
        return entry.TubeMouths.Select(mouth =>
        {
            var bindings = entry.TubeMouthBindings.Where(binding => binding.End == mouth.End).ToArray();
            if (bindings.Length > 1) throw new ArgumentException("Duplicate tube-mouth binding.");
            return bindings.Length == 0 ? mouth : mouth with
            { Position = bindings[0].Multiplier * parameters[bindings[0].Parameter] + bindings[0].Offset };
        }).ToArray();
    }
}
