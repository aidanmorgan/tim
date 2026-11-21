// Contact-work mathematical contracts: accepted impulse work, conservative shared settlement,
// sourced minimum/dust closure and exhausted controls. Scene behavior is covered by native integration.
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ContactWorkLedgerPropertyTests
{
    private static readonly Vector128<float> Up = Vector128.Create(0f, 1f, 0f, 0f);
    private const float Target = 8f;
    private const float Preload = 32f;
    // Rounding residue, not gameplay: 8 ULPs of the 32 J preload.
    private static readonly float Dust = 8 * (MathF.BitIncrement(Preload) - Preload);

    private static float RequestedWork(float v, float k)
    {
        var j = (Target - v) / k; // the exact f32 expression of ContactWorkImpulse.TowardTarget
        return j * (v + .5f * k * j);
    }

    private static double Delivered(float v, float k, float impulse) => impulse * ((double)v + .5 * k * impulse);

    /// <summary>Speeds whose f32 request lies within `ulps` ULPs below `budget` for inverse mass k.</summary>
    private static IEnumerable<float> SpeedsJustBelowBudget(float k, float budget, int ulps)
    {
        // For k the request is j(v + kj/2) with j = (8 - v)/k, i.e. (64 - v^2)/(2k); solve for the boundary speed.
        var root = MathF.Sqrt(MathF.Max(0f, Target * Target - 2f * k * budget));
        var v = root;
        for (var step = 0; step < 4096; step++, v = MathF.BitIncrement(v))
        {
            var requested = RequestedWork(v, k);
            if (requested < budget && budget - requested <= ulps * (MathF.BitIncrement(budget) - budget)) yield return v;
        }
        v = root;
        for (var step = 0; step < 4096; step++, v = MathF.BitDecrement(v))
        {
            if (v < 0) break;
            var requested = RequestedWork(v, k);
            if (requested < budget && budget - requested <= ulps * (MathF.BitIncrement(budget) - budget)) yield return v;
        }
    }

    // A request can reach the 32 J store only when k < 1 (max request at rest is 32/k); k = .25 is the 4 kg Bowling ball.
    public static TheoryData<float> InverseMasses() => new() { .2f, .25f, .5f, .75f, .9f };

    [Theory]
    [MemberData(nameof(InverseMasses))]
    public void ATargetLimitedPayoutNeverLeavesDustThatALaterContactPays(float k)
    {
        var found = 0;
        foreach (var v in SpeedsJustBelowBudget(k, Preload, 64))
        {
            found++;
            var balance = new FiniteEnergyBalance(Preload, 0);
            var paid = ContactWorkImpulse.FromStore(v, Target, k, balance, Preload, Up);
            var remaining = paid.CloseStore ? default : balance.Debit(paid.Debit);
            if (remaining.Available > 0 && remaining.Available <= Dust)
            {
                var later = ContactWorkImpulse.FromStore(.5f, Target, k, remaining, Preload, Up);
                Assert.Equal(0, later.Debit);
            }
        }
        Assert.True(found > 0);
    }

    private static IEnumerable<(float v, float k, float a)> Sweep()
    {
        for (var s = 0; s < 120; s++)
        for (var m = 0; m < 60; m++)
        foreach (var a in new[] { 32f, 31.999998f, 16f, 2.5f, .75f, 1e-3f, 3.0517578e-5f })
            yield return (.01f + s * .0663f, .05f + m * .0571f, a);
    }

    [Fact]
    public void EveryPayoutConservativelyFundsItsActualImpulseWork()
    {
        foreach (var (v, k, a) in Sweep())
        {
            var balance = new FiniteEnergyBalance(a, 0);
            var result = ContactWorkImpulse.FromStore(v, Target, k, balance, Preload, Up);
            if (result.Debit == 0) continue;
            var delivered = Delivered(v, k, result.Impulse.GetElement(1));
            var remaining = result.CloseStore ? default : balance.Debit(result.Debit);
            var spent = a - (double)remaining.Bulk - remaining.Remainder;
            var workRounding = 2 * (MathF.BitIncrement(result.Debit) - result.Debit);
            Assert.True(delivered <= spent + workRounding); // Independent contact-work equation, evaluated above in double.
            Assert.InRange(Math.Abs(result.Debit - delivered), 0, 8 * (double)workRounding);
            if (!result.CloseStore)
                Assert.InRange(spent - result.Debit, 0, FiniteEnergyBalance.LossBound(Preload, 0, result.Debit, 0, 1));
            else
            {
                Assert.Equal(0, remaining.Available);
                Assert.InRange(spent - delivered, -workRounding, a * 8 * 1.1920929e-7);
            }
        }
    }

    public static TheoryData<float, float, float> Hostile() => new()
    {
        { 0f, 1f, float.Epsilon }, { 0f, 1f, 1e-38f }, { 7.999999f, 1f, 32f }, { 0f, 1e-30f, 32f }, { 0f, 1e30f, 32f },
        { 0f, float.Epsilon, 32f }, { 1e-38f, 1f, 32f }, { 0f, 1f, float.MaxValue }, { 7.9999995f, .25f, 1e-30f },
        { 0f, 1f, -0f }, { 0f, 1f, float.NaN }, { float.NaN, 1f, 32f }, { 0f, float.NaN, 32f }, { -1f, 1f, 32f },
        { 9f, 1f, 32f }, { 0f, 1f, float.PositiveInfinity },
    };

    [Theory]
    [MemberData(nameof(Hostile))]
    public void HostileInputsNeverOverspendOrGoNegative(float v, float k, float a)
    {
        var balance = new FiniteEnergyBalance(a, 0);
        var result = ContactWorkImpulse.FromStore(v, Target, k, balance, Preload, Up);
        Assert.True(float.IsFinite(result.Debit) && result.Debit >= 0);
        Assert.True(float.IsFinite(result.Impulse.GetElement(1)) && result.Impulse.GetElement(1) >= 0);
        if (float.IsFinite(a) && a > 0 && a <= Preload)
        {
            var remaining = result.CloseStore ? default : balance.Debit(result.Debit);
            remaining.Validate(Preload);
            Assert.True(result.Debit <= a);
        }
        else Assert.Equal(0, result.Debit);
    }

    [Theory]
    [InlineData(.25f, 1)]
    [InlineData(1f / .6f, 2)]
    [InlineData(1f, 3)]
    public void ManyPayoutsFromOneStoreConservePaidWorkAndStopAtExhaustion(float k, int seed)
    {
        var random = new Random(seed);
        var store = new FiniteEnergyBalance(Preload, 0);
        double paidTotal = 0, closingLoss = 0;
        var exhausted = false; var payouts = 0;
        for (var contact = 0; contact < 400; contact++)
        {
            var v = contact == 0 ? 7.5f : (float)(7.7 + random.NextDouble() * .29);
            var payment = ContactWorkImpulse.FromStore(v, Target, k, store, Preload, Up);
            if (exhausted) Assert.Equal(0, payment.Debit);
            if (payment.Debit > 0) payouts++;
            paidTotal += payment.Debit;
            if (payment.CloseStore)
            {
                closingLoss += store.Bulk + (double)store.Remainder - payment.Debit;
                store = default;
            }
            else store = store.Debit(payment.Debit);
            store.Validate(Preload);
            Assert.False(store.Available > 0 && store.Available <= Dust);
            if (store.Available == 0) exhausted = true;
        }
        Assert.True(payouts >= 3 && exhausted);
        Assert.InRange(Preload - paidTotal - closingLoss, 0,
            FiniteEnergyBalance.LossBound(Preload, 0, Preload, 0, 400));
    }
}
