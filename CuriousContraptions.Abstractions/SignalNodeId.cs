using System;

namespace CuriousContraptions.Abstractions;

/// <summary>A package-local signal capability identity, independent of its public sockets.</summary>
public readonly record struct SignalNodeId : IComparable<SignalNodeId>
{
    public string Value { get; }
    public SignalNodeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
    public int CompareTo(SignalNodeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(SignalNodeId left, SignalNodeId right) => left.CompareTo(right) < 0;
    public static bool operator <=(SignalNodeId left, SignalNodeId right) => left.CompareTo(right) <= 0;
    public static bool operator >(SignalNodeId left, SignalNodeId right) => left.CompareTo(right) > 0;
    public static bool operator >=(SignalNodeId left, SignalNodeId right) => left.CompareTo(right) >= 0;
}
