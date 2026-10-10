using System;
using System.Runtime.Intrinsics;

namespace CuriousContraptions.Gpu;

/// <summary>Implicit spring coefficients. Damping is N·s/m, not a damping ratio.</summary>
public readonly record struct SoftConstraintCoefficients(float Gamma, float BiasRate)
{
    public static SoftConstraintCoefficients FromSpring(float stiffness, float damping, float step)
    {
        if (!float.IsFinite(stiffness) || stiffness <= 0 || !float.IsFinite(damping) || damping < 0 ||
            !float.IsFinite(step) || step <= 0)
            throw new ArgumentException("A spring requires positive finite stiffness/step and nonnegative finite damping.");
        var denominator = damping + step * stiffness;
        var impulseDenominator = step * denominator;
        if (!float.IsFinite(denominator) || denominator <= 0 ||
            !float.IsFinite(impulseDenominator) || impulseDenominator <= 0)
            throw new ArgumentException("Spring intermediates must fit the canonical f32 domain.");
        var gamma = 1f / impulseDenominator;
        var bias = stiffness / denominator;
        if (!float.IsFinite(gamma) || gamma <= 0 || !float.IsFinite(bias) || bias <= 0)
            throw new ArgumentException("Spring coefficients must fit the canonical f32 domain.");
        return new(gamma, bias);
    }
}

public readonly record struct ConstraintRowImpulse(Vector128<float> Accumulated, Vector128<float> Delta);

/// <summary>Four independent scalar Jacobian rows. Callers own geometry, phase and warm-start state.</summary>
public static class SoftConstraint
{
    /// <summary>
    /// Solve Jv + biasRate*C + gamma*lambda = 0. Project the accumulated impulse,
    /// never the incremental impulse. Bounds and finite declarations are admitted before stepping.
    /// A non-finite numerical candidate leaves that lane's finite impulse unchanged.
    /// </summary>
    public static ConstraintRowImpulse Solve4(Vector128<float> inverseEffectiveMass,
        Vector128<float> velocity, Vector128<float> error, Vector128<float> gamma,
        Vector128<float> biasRate, Vector128<float> accumulated, Vector128<float> minimum,
        Vector128<float> maximum)
    {
        var delta = -(velocity + biasRate * error + gamma * accumulated) /
            (inverseEffectiveMass + gamma);
        var next = Vector128.Min(maximum, Vector128.Max(minimum, accumulated + delta));
        // Scalar validity masks do not change the four-wide numerical law; a bad lane cannot
        // poison another row or fault a valid simulation tick.
        for (var lane = 0; lane < 4; lane++)
        {
            var old = accumulated.GetElement(lane);
            if (!float.IsFinite(old)) old = 0;
            if (!(inverseEffectiveMass.GetElement(lane) > 0) || !float.IsFinite(next.GetElement(lane)) ||
                !float.IsFinite(delta.GetElement(lane)))
                next = next.WithElement(lane, old);
            accumulated = accumulated.WithElement(lane, old);
        }
        return new(next, next - accumulated);
    }
}
