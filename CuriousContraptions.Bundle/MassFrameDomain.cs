using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

internal static class MassFrameDomain
{
    public static void Validate(MassFrameDeclaration frame, KinematicMode mode, IReadOnlyList<ParameterDescriptor> parameters)
    {
        var values = parameters.ToDictionary(p => new ParameterId(p.Key), p => p.Default);
        frame.Evaluate(values).Validate(mode);
        var domain = new MassPolynomialDomain(parameters);
        var isStatic = mode == KinematicMode.Static;
        domain.Range(frame.Mass, isStatic ? 0f : 1f / 1024f, isStatic ? 0f : 1024f);
        foreach (var scalar in new[] { frame.CentreOfMass.X, frame.CentreOfMass.Y, frame.CentreOfMass.Z })
            domain.Range(scalar, isStatic ? 0f : -16f, isStatic ? 0f : 16f);
        RequireParameter(frame.LinearDrag.Parameter, ParameterUnit.InverseSecond, parameters);
        domain.Range(frame.LinearDrag, 0f, isStatic ? 0f : .125f);
        Acceleration(frame.Gravity, isStatic, parameters, values);
        Moments(frame.PrincipalMoments, isStatic, parameters, domain);
    }

    private static ParameterDescriptor RequireParameter(ParameterId? key, ParameterUnit unit,
        IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (key is null) return default;
        var matches = parameters.Where(p => p.Key == key.Value.Value && p.Kind == ParameterKind.Real &&
            p.Unit == unit && p.RunEdit == ParameterRunEdit.BuildOnly).ToArray();
        if (matches.Length != 1) throw new ArgumentException("Unresolved or incompatible physical parameter.");
        return matches[0];
    }

    private static void Acceleration(WorldAccelerationDeclaration acceleration, bool isStatic,
        IReadOnlyList<ParameterDescriptor> parameters, Dictionary<ParameterId, float> values)
    {
        var parameter = RequireParameter(acceleration.Parameter, ParameterUnit.MetrePerSecondSquared, parameters);
        // Norm squared is convex on this one affine parameter interval.
        foreach (var endpoint in new[] { parameter.Minimum, parameter.Maximum })
        {
            if (acceleration.Parameter is { } key) values[key] = endpoint;
            if (acceleration.Evaluate(values) != Vector3.Zero && isStatic)
                throw new ArgumentException("A static body cannot declare acceleration.");
        }
    }

    private static void Moments(PrincipalMomentDeclaration declaration, bool isStatic,
        IReadOnlyList<ParameterDescriptor> parameters, MassPolynomialDomain domain)
    {
        if (declaration.Operation == PrincipalMomentOperation.PolynomialVector)
        {
            var p = declaration.Polynomial;
            var moments = new[] { p.X, p.Y, p.Z };
            foreach (var scalar in moments)
                domain.Range(scalar, isStatic ? 0 : 1f / 4294967296, isStatic ? 0 : 524288, !isStatic);
            for (var i = 0; i < 3; i++)
                if (domain.Bounds((moments[(i + 1) % 3], 1), (moments[(i + 2) % 3], 1), (moments[i], -1)).Min < 0)
                    throw new ArgumentException("Principal moments are nonphysical inside the parameter domain.");
            return;
        }
        if (isStatic) throw new ArgumentException("A static body cannot have positive moments.");
        declaration.ValidateCoefficients();
        var mass = OperandBounds(declaration.Mass, ParameterUnit.Kilogram, parameters);
        var length = OperandBounds(declaration.Length, ParameterUnit.Metre, parameters);
        CheckMoments(declaration.Coefficients * (mass.Min * length.Min * length.Min));
        CheckMoments(declaration.Coefficients * (mass.Max * length.Max * length.Max));
    }

    private static (float Min, float Max) OperandBounds(MomentOperand operand, ParameterUnit unit,
        IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (operand.Parameter is null)
        {
            var value = operand.Evaluate(new Dictionary<ParameterId, float>());
            return (value, value);
        }
        var p = RequireParameter(operand.Parameter, unit, parameters);
        if (operand.Constant != 0 || p.Minimum <= 0 || !float.IsFinite(p.Minimum) || !float.IsFinite(p.Maximum))
            throw new ArgumentException("Invalid principal-moment operand domain.");
        return (p.Minimum, p.Maximum);
    }

    private static void CheckMoments(Vector3 moments)
    {
        foreach (var value in new[] { moments.X, moments.Y, moments.Z })
            if (!float.IsFinite(value) || value < 1f / 4294967296 || value >= 524288)
                throw new ArgumentException("Principal moments leave their physical domain.");
    }
}
