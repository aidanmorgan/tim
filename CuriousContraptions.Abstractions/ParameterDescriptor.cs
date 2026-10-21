using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

/// <summary>What one authored parameter holds. A Choice parameter selects one of its named options.</summary>
public enum ParameterKind : byte
{
    Real = 1,
    WholeNumber = 2,
    Boolean = 3,
    Choice = 4
}

/// <summary>The unit of a parameter's values.</summary>
public enum ParameterUnit : byte
{
    None = 1,
    Metre = 2,
    Kilogram = 3,
    Second = 4,
    MetrePerSecond = 5,
    MetrePerSecondSquared = 6,
    NewtonPerMetre = 7,
    NewtonSecondPerMetre = 8
}

/// <summary>One typed authored parameter of an element: its key, unit and kind, with f32 bounds and default. A Choice parameter
/// lists its options by name; its minimum is 0, its maximum is the last option's index and its default is an index.</summary>
public readonly record struct ParameterDescriptor(
    string Key,
    ParameterUnit Unit,
    ParameterKind Kind,
    float Minimum,
    float Maximum,
    float Default,
    IReadOnlyList<string> Options);
