using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions.Mechanical;

public enum PrincipalMomentOperation : byte { None = 0, PolynomialVector = 1, ScaledMassLengthSquared = 2 }

/// <summary>A positive dimensional operand, constant or a declared parameter.</summary>
public readonly record struct MomentOperand(ParameterId? Parameter, float Constant)
{
    public float Evaluate(IReadOnlyDictionary<ParameterId, float> parameters)
    {
        var value = Constant;
        if (Parameter is { } key)
        {
            if (Constant != 0f || key == default || !parameters.TryGetValue(key, out value))
                throw new ArgumentException("Invalid principal-moment operand.");
        }
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentException("Principal-moment operand must be finite and positive.");
        return value;
    }
}

/// <summary>Explicit principal moments in kg·m², resolved before simulation.</summary>
public readonly record struct PrincipalMomentDeclaration(PrincipalMomentOperation Operation, MassVector Polynomial,
    MomentOperand Mass, MomentOperand Length, Vector3 Coefficients)
{
    public static PrincipalMomentDeclaration FromPolynomial(MassVector value) =>
        new(PrincipalMomentOperation.PolynomialVector, value, default, default, Vector3.Zero);

    public static PrincipalMomentDeclaration Constant(Vector3 value) => FromPolynomial(MassVector.Constant(value));

    public Vector3 Evaluate(IReadOnlyDictionary<ParameterId, float> parameters)
    {
        if (Operation == PrincipalMomentOperation.PolynomialVector)
        {
            if (Mass != default || Length != default || Coefficients != Vector3.Zero)
                throw new ArgumentException("Polynomial moments contain inactive operands.");
            return Polynomial.Evaluate(parameters);
        }
        if (Operation != PrincipalMomentOperation.ScaledMassLengthSquared || Polynomial != default)
            throw new ArgumentException("Invalid principal-moment operation.");
        ValidateCoefficients();
        var mass = Mass.Evaluate(parameters);
        var length = Length.Evaluate(parameters);
        var result = Coefficients * (mass * length * length);
        if (!float.IsFinite(result.X) || !float.IsFinite(result.Y) || !float.IsFinite(result.Z))
            throw new ArgumentException("Principal-moment product overflow.");
        return result;
    }

    public void ValidateCoefficients()
    {
        var c = Coefficients;
        if (!float.IsFinite(c.X) || !float.IsFinite(c.Y) || !float.IsFinite(c.Z) ||
            c.X <= 0f || c.Y <= 0f || c.Z <= 0f ||
            c.X > c.Y + c.Z || c.Y > c.X + c.Z || c.Z > c.X + c.Y)
            throw new ArgumentException("Nonphysical principal-moment coefficients.");
    }
}
