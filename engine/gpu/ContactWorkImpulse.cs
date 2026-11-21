using System;
using System.Runtime.Intrinsics;

namespace CuriousContraptions.Gpu;

/// <summary>Which bound decided a payment: the declared target speed, or the finite work the caller made available.</summary>
public enum ContactWorkLimit : byte { Target, Budget }
/// <summary>A paid normal impulse. The caller owns qualification, allocation and the finite store.</summary>
public readonly record struct ContactWorkPayment(float NormalImpulse, float Debit, ContactWorkLimit Limit);
/// <summary>What the store could do: pay (the payment may still be zero when the contact needs none), nothing because it
/// is empty or holds less than the minimum payable work (a 0 J capacity, such as a strength-0 Bumper, is a legal empty
/// store), or nothing because it is impossible (a non-finite or negative capacity, or available work that is non-finite,
/// negative or above the capacity), which the caller must report.</summary>
public enum ContactWorkStore : byte { Payable, BelowMinimum, Impossible }
public readonly record struct ContactWorkResponse(float Debit, Vector128<float> Impulse, ContactWorkStore Store, bool CloseStore);

/// <summary>
/// Shared contact-space work law, after ordinary collision response. Inverse effective mass
/// includes the translational and rotational response at the actual contact point.
/// Allocation policy (full or partial payment) belongs to the caller, not this law.
/// </summary>
public static class ContactWorkImpulse
{
    /// <summary>
    /// The minimum payable work as a fraction of the store's declared capacity (Story 9.4b, decided 18 Oct 2026 under owner
    /// delegation, playability): 2^-18, that is 1.2e-4 J of the 32 J Bumper preload, 32 to 64 f32 ULPs of the capacity and
    /// far below any visible launch. A store below it never pays, and a payout that would leave less than it spends the
    /// whole store, so f32 rounding can never leave dust that a later contact pays from.
    /// </summary>
    public const float MinimumPayableFraction = 1f / (1 << 18);

    public static float MinimumPayableWork(float capacity) => capacity * MinimumPayableFraction;

    /// <summary>Chooses actual paid contact work and the sourced minimum/dust disposition.
    /// The caller alone settles the shared balance; a scalar subtraction never defines delivered work.</summary>
    public static ContactWorkResponse FromStore(float outgoingNormalSpeed, float targetNormalSpeed,
        float inverseEffectiveMass, FiniteEnergyBalance balance, float capacity, Vector128<float> normal)
    {
        try { balance.Validate(capacity); }
        catch (ArgumentException) { return new(0, Vector128<float>.Zero, ContactWorkStore.Impossible, false); }
        var minimum = MinimumPayableWork(capacity);
        if (balance.Available == 0 || balance.CompareTo(minimum) < 0)
            return new(0, Vector128<float>.Zero, ContactWorkStore.BelowMinimum, false);
        var unpaid = new ContactWorkResponse(0, Vector128<float>.Zero, ContactWorkStore.Payable, false);
        var payment = TowardTarget(outgoingNormalSpeed, targetNormalSpeed, inverseEffectiveMass, balance.Available);
        if (payment.Debit <= 0) return unpaid;
        if (payment.Limit == ContactWorkLimit.Target && balance.Debit(payment.Debit).CompareTo(minimum) >= 0)
            return new(payment.Debit, normal * payment.NormalImpulse, ContactWorkStore.Payable, false);
        // The sourced whole-store policy funds a fresh budget-limited impulse, then closes the store.
        payment = FromAuthorizedWork(outgoingNormalSpeed, inverseEffectiveMass, balance.Available, ContactWorkLimit.Budget);
        return payment.Debit <= 0 ? unpaid
            : new(payment.Debit, normal * payment.NormalImpulse, ContactWorkStore.Payable, true);
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
        return requestedWork < availableWork
            ? FromAuthorizedWork(outgoingNormalSpeed, inverseEffectiveMass, requestedWork, ContactWorkLimit.Target)
            : FromAuthorizedWork(outgoingNormalSpeed, inverseEffectiveMass, availableWork, ContactWorkLimit.Budget);
    }

    /// <summary>The impulse whose contact-space work is the largest f32 value not above <paramref name="authorizedWork"/>;
    /// <paramref name="limit"/> records which bound authorized that work.</summary>
    public static ContactWorkPayment FromAuthorizedWork(float outgoingNormalSpeed,
        float inverseEffectiveMass, float authorizedWork, ContactWorkLimit limit)
    {
        if (!float.IsFinite(outgoingNormalSpeed) || outgoingNormalSpeed < 0 ||
            !float.IsFinite(inverseEffectiveMass) || inverseEffectiveMass <= 0 ||
            !float.IsFinite(authorizedWork) || authorizedWork <= 0 || !Enum.IsDefined(limit))
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
                return new(impulse, debit, limit);
            impulse = MathF.BitDecrement(impulse);
        }
        return default;
    }
}
