using System;

namespace CuriousContraptions.Abstractions;

public enum TargetSelectorKind : byte
{
    AllDynamic = 0,
    AllPayloads = 1,
    Named = 2
}

public readonly record struct TargetSelector(TargetSelectorKind Kind = TargetSelectorKind.AllDynamic, string? NamedTarget = null)
{
    public static TargetSelector AllDynamic => new(TargetSelectorKind.AllDynamic);
    public static TargetSelector AllPayloads => new(TargetSelectorKind.AllPayloads);
    public static TargetSelector Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new(TargetSelectorKind.Named, name);
    }
}
