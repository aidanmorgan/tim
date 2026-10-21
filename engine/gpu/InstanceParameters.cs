using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>A package-defined parameter identity, converted only at declaration and wire boundaries.</summary>
public readonly record struct ParameterId : IComparable<ParameterId>
{
    public string Value { get; }
    public ParameterId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A parameter needs an identity.");
        Value = value;
    }
    public int CompareTo(ParameterId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(ParameterId left, ParameterId right) => left.CompareTo(right) < 0;
    public static bool operator <=(ParameterId left, ParameterId right) => left.CompareTo(right) <= 0;
    public static bool operator >(ParameterId left, ParameterId right) => left.CompareTo(right) > 0;
    public static bool operator >=(ParameterId left, ParameterId right) => left.CompareTo(right) >= 0;
    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Immutable authored f32 values. Admission requires the complete declared set, never fills submitted gaps.</summary>
public sealed class InstanceParameterCollection : IReadOnlyDictionary<ParameterId, float>, IEquatable<InstanceParameterCollection>
{
    private readonly ImmutableSortedDictionary<ParameterId, float> _values;
    public static InstanceParameterCollection Empty { get; } = new([]);
    public InstanceParameterCollection(IEnumerable<KeyValuePair<ParameterId, float>> values)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<ParameterId, float>();
        foreach (var pair in values)
        {
            if (pair.Key == default || !builder.TryAdd(pair.Key, pair.Value))
                throw new ArgumentException("Invalid or duplicate parameter identity.");
        }
        _values = builder.ToImmutable();
    }
    public float this[ParameterId key] => _values[key];
    public IEnumerable<ParameterId> Keys => _values.Keys;
    public IEnumerable<float> Values => _values.Values;
    public int Count => _values.Count;
    public bool ContainsKey(ParameterId key) => _values.ContainsKey(key);
    public bool TryGetValue(ParameterId key, out float value) => _values.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<ParameterId, float>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static InstanceParameterCollection Defaults(PartId part) => new(DeclaredPhysics.Of(part).Metadata.Parameters
        .Select(p => KeyValuePair.Create(new ParameterId(p.Key), p.Default)));

    public void Validate(PartId part)
    {
        var schema = DeclaredPhysics.Of(part).Metadata.Parameters;
        if (Count != schema.Count) throw new ArgumentException("Missing or unknown declared parameters.");
        foreach (var descriptor in schema)
        {
            if (!TryGetValue(new(descriptor.Key), out var value) || !float.IsFinite(value) ||
                value < descriptor.Minimum || value > descriptor.Maximum ||
                (descriptor.Kind != ParameterKind.Real && value != MathF.Truncate(value)))
                throw new ArgumentException("Invalid declared parameter value.");
        }
    }

    /// <summary>The bundle evaluator's resource-key boundary.</summary>
    public IReadOnlyDictionary<string, float> ToBundleValues() =>
        _values.ToDictionary(pair => pair.Key.Value, pair => pair.Value, StringComparer.Ordinal);

    public bool Equals(InstanceParameterCollection? other) => other is not null && _values.SequenceEqual(other._values);
    public override bool Equals(object? obj) => obj is InstanceParameterCollection other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var pair in _values) hash.Add(pair);
        return hash.ToHashCode();
    }
}
