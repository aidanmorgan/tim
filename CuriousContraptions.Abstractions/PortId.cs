using System;

namespace CuriousContraptions.Abstractions;

/// <summary>A package-defined port identity, converted only at declaration and wire boundaries.</summary>
public readonly record struct PortId : IComparable<PortId>
{
    public string Value { get; }
    public PortId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
    public int CompareTo(PortId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(PortId left, PortId right) => left.CompareTo(right) < 0;
    public static bool operator <=(PortId left, PortId right) => left.CompareTo(right) <= 0;
    public static bool operator >(PortId left, PortId right) => left.CompareTo(right) > 0;
    public static bool operator >=(PortId left, PortId right) => left.CompareTo(right) >= 0;
    public override string ToString() => Value ?? string.Empty;

    public static PortId ActivationOut => new("ActivationOut");
    public static PortId SetIn => new("SetIn");
    public static PortId ResetIn => new("ResetIn");
    public static PortId ActivationIn => new("ActivationIn");
    public static PortId FirstIn => new("FirstIn");
    public static PortId SecondIn => new("SecondIn");
    public static PortId Supply => new("Supply");
    public static PortId PowerIn => new("PowerIn");
    public static PortId Tie => new("Tie");
    public static PortId TieA => new("TieA");
    public static PortId TieB => new("TieB");
}
