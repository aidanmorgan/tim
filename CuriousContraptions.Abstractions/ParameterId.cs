using System;

namespace CuriousContraptions.Abstractions;

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
