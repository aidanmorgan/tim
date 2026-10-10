using System;
using System.Runtime.Intrinsics;

namespace CuriousContraptions.Gpu;

/// <summary>A paid normal impulse. The caller owns qualification, allocation and the finite store.</summary>
public readonly record struct ContactWorkPayment(float NormalImpulse, float Debit);
public readonly record struct ContactWorkResponse(float Remaining, float Debit, Vector128<float> Impulse);

/// <summary>
/// Shared contact-space work law, after ordinary collision response. Inverse effective mass
/// includes the translational and rotational response at the actual contact point.
/// Allocation policy (full or partial payment) belongs to the caller, not this law.
/// </summary>
public static class ContactWorkImpulse
{
    /// <summary>F32 reservoir payment and SIMD projection into the contact normal.</summary>
    public static ContactWorkResponse FromStore(float outgoingNormalSpeed, float targetNormalSpeed,
        float inverseEffectiveMass, float availableWork, Vector128<float> normal)
    {
        var payment = TowardTarget(outgoingNormalSpeed, targetNormalSpeed, inverseEffectiveMass, availableWork);
        if (payment.Debit <= 0) return new(availableWork, 0, Vector128<float>.Zero);
        var remaining = availableWork - payment.Debit;
        if (availableWork - remaining < payment.Debit)
            remaining = MathF.Max(0, MathF.BitDecrement(remaining));
        var debit = availableWork - remaining;
        if (!(debit > 0)) return new(availableWork, 0, Vector128<float>.Zero);
        return new(remaining, debit, normal * payment.NormalImpulse);
    }

    /// <summary>Spend at most the caller's finite budget, stopping at the declared target speed.</summary>
    public static ContactWorkPayment TowardTarget(float outgoingNormalSpeed, float targetNormalSpeed,
        float inverseEffectiveMass, float availableWork)
    {
        if (!float.IsFinite(outgoingNormalSpeed) || outgoingNormalSpeed < 0 ||
            !float.IsFinite(targetNormalSpeed) || targetNormalSpeed <= outgoingNormalSpeed ||
            !float.IsFinite(inverseEffectiveMass) || inverseEffectiveMass <= 0 ||
            !float.IsFinite(availableWork) || availableWork <= 0)
            return default;

        var requestedImpulse = (targetNormalSpeed - outgoingNormalSpeed) / inverseEffectiveMass;
        var requestedWork = requestedImpulse *
            (outgoingNormalSpeed + .5f * inverseEffectiveMass * requestedImpulse);
        return FromAuthorizedWork(outgoingNormalSpeed, inverseEffectiveMass,
            MathF.Min(requestedWork, availableWork));
    }

    public static ContactWorkPayment FromAuthorizedWork(float outgoingNormalSpeed,
        float inverseEffectiveMass, float authorizedWork)
    {
        if (!float.IsFinite(outgoingNormalSpeed) || outgoingNormalSpeed < 0 ||
            !float.IsFinite(inverseEffectiveMass) || inverseEffectiveMass <= 0 ||
            !float.IsFinite(authorizedWork) || authorizedWork <= 0)
            return default;

        // Stable positive root of W = v*j + k*j*j/2; no subtractive cancellation.
        var discriminant = outgoingNormalSpeed * outgoingNormalSpeed +
            2f * inverseEffectiveMass * authorizedWork;
        var denominator = MathF.Sqrt(discriminant) + outgoingNormalSpeed;
        var impulse = 2f * authorizedWork / denominator;
        if (!float.IsFinite(impulse) || impulse <= 0) return default;

        // Round toward affordable work. Numerical residuals never fault the physics tick.
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var debit = impulse * (outgoingNormalSpeed + .5f * inverseEffectiveMass * impulse);
            if (float.IsFinite(debit) && debit > 0 && debit <= authorizedWork)
                return new(impulse, debit);
            impulse = MathF.BitDecrement(impulse);
        }
        return default;
    }
}
