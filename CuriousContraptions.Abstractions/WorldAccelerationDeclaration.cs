using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions.Mechanical;

/// <summary>World acceleration in m/s², affine in one acceleration-valued parameter.</summary>
public readonly record struct WorldAccelerationDeclaration(ParameterId? Parameter, Vector3 Offset, Vector3 Coefficient)
{
    public static WorldAccelerationDeclaration Constant(Vector3 value) => new(null, value, Vector3.Zero);

    public Vector3 Evaluate(IReadOnlyDictionary<ParameterId, float> parameters)
    {
        if (!Finite(Offset) || !Finite(Coefficient))
            throw new ArgumentException("Acceleration coefficients must be finite.");
        var value = 0f;
        if (Parameter is { } key)
        {
            if (key == default || !parameters.TryGetValue(key, out value) || !float.IsFinite(value))
                throw new ArgumentException("Acceleration requires its declared finite parameter.");
        }
        else if (Coefficient != Vector3.Zero)
            throw new ArgumentException("Constant acceleration has no parameter coefficient.");
        var result = Offset + Coefficient * value;
        if (!Finite(result) || result.LengthSquared() > 256f)
            throw new ArgumentException("Acceleration exceeds its physical domain.");
        return result;
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
