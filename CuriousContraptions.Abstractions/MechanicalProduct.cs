using System;
using System.Collections.Generic;

namespace CuriousContraptions.Abstractions.Mechanical;

/// <summary>A nonrecursive, dimensioned scalar operand. Product operands cannot themselves contain products.</summary>
public readonly record struct MechanicalOperand(MechanicalValueSource Source, ParameterUnit Unit, float Constant, string? Parameter)
{
    public static MechanicalOperand Fixed(float value, ParameterUnit unit) => new(MechanicalValueSource.Constant, unit, value, null);
    public static MechanicalOperand FromParameter(string key, ParameterUnit unit) => new(MechanicalValueSource.Parameter, unit, 0f, key);

    public float Evaluate(IReadOnlyDictionary<string, float> parameters)
    {
        var value = Source switch
        {
            MechanicalValueSource.Constant when Parameter is null => Constant,
            MechanicalValueSource.Parameter when Constant == 0f && Parameter is { } key && parameters.TryGetValue(key, out var found) => found,
            _ => throw new ArgumentException("Invalid mechanical product operand.")
        };
        if (!float.IsFinite(value) || value < 0f) throw new ArgumentException("Invalid mechanical product operand domain.");
        return value;
    }
}

/// <summary>The admitted dimensional product is force times linear speed, in watts.</summary>
public readonly record struct MechanicalProduct(MechanicalOperand Left, MechanicalOperand Right)
{
    public bool HasPowerUnits =>
        (Left.Unit == ParameterUnit.Newton && Right.Unit == ParameterUnit.MetrePerSecond) ||
        (Right.Unit == ParameterUnit.Newton && Left.Unit == ParameterUnit.MetrePerSecond);

    public float Evaluate(IReadOnlyDictionary<string, float> parameters)
    {
        if (!HasPowerUnits) throw new ArgumentException("Unsupported mechanical product units.");
        var result = Left.Evaluate(parameters) * Right.Evaluate(parameters);
        if (!float.IsFinite(result)) throw new ArgumentException("Mechanical product overflow.");
        return result;
    }
}
