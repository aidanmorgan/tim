using System;

namespace CuriousContraptions;

/// <summary>An element's identity: the bundle's string element id, which saves and messages carry as it is (Story 9A.15). There is
/// no numeric form.</summary>
public readonly record struct PartId : IComparable<PartId>
{
    private readonly string? _value;
    public string Value => _value ?? throw new InvalidOperationException("PartId is not initialized.");

    public PartId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PartId cannot be null or whitespace.", nameof(value));
        _value = value;
    }

    public int CompareTo(PartId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(PartId left, PartId right) => left.CompareTo(right) < 0;
    public static bool operator <=(PartId left, PartId right) => left.CompareTo(right) <= 0;
    public static bool operator >(PartId left, PartId right) => left.CompareTo(right) > 0;
    public static bool operator >=(PartId left, PartId right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value;

}
