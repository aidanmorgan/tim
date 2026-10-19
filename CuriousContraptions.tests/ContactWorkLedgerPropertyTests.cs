// Story 9.4b AC1 regression: CAT-015 contact-work ledger properties of engine/gpu/ContactWorkImpulse.cs.
// Adopted from the Murdoch-2 review probe MurdochContactLedgerTests.cs (sha256 b51400e0…). The only change from the probe is
// the store capacity argument FromStore now takes (the 32 J preload for every store the probe derives from it); the class
// and test names say what they prove, and every assertion is unchanged.
// They target what the implementer's store-limited sweep skips:
//   a target-limited payout whose request is a few ULPs below the store leaves rounding dust, which a later "empty"
//   contact then pays from (the cat-015a symptom by another route);
//   exact ledger closure remaining + debit == available in f32 for every payout;
//   the debit is never less than the work actually delivered (no energy created) and exceeds it by at most a stated
//   bound (the D3 "dissipated" amount is quantified, not assumed);
//   the single BitDecrement correction in the target branch is sufficient for every swept case;
//   hostile inputs (subnormal/tiny stores, huge speeds, extreme inverse masses) never yield a negative, non-finite or
//   over-budget result;
//   a long sequence of payouts from one 32 J store closes exactly and never pays after exhaustion or from dust.
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
        var dustCases = new List<string>();
        var found = 0;
        foreach (var v in SpeedsJustBelowBudget(k, Preload, 64))
        {
            found++;
            var paid = ContactWorkImpulse.FromStore(v, Target, k, Preload, Preload, Up);
            if (paid.Remaining > 0f && paid.Remaining <= Dust)
            {
                var later = ContactWorkImpulse.FromStore(.5f, Target, k, paid.Remaining, Preload, Up);
                if (later.Debit > 0f) dustCases.Add($"v={v:R} remaining={paid.Remaining:R} later debit={later.Debit:R}");
            }
        }
        Assert.True(found > 0, $"k={k}: the search found no near-budget target-limited speed (probe incomplete)");
        Assert.True(dustCases.Count == 0, $"k={k}: {dustCases.Count} payouts paid from rounding dust, e.g. {string.Join("; ", dustCases.GetRange(0, Math.Min(3, dustCases.Count)))}");
    }

    private static IEnumerable<(float v, float k, float a)> Sweep()
    {
        for (var s = 0; s < 120; s++)
        for (var m = 0; m < 60; m++)
        foreach (var a in new[] { 32f, 31.999998f, 16f, 2.5f, .75f, 1e-3f, 3.0517578e-5f })
            yield return (.01f + s * .0663f, .05f + m * .0571f, a);
    }

    [Fact]
    public void EveryPayoutClosesExactlyAndNeverDebitsLessThanItDelivers()
    {
        var failures = new List<string>();
        double worstBudgetExcess = 0, worstTargetExcess = 0;
        foreach (var (v, k, a) in Sweep())
        {
            var payment = ContactWorkImpulse.TowardTarget(v, Target, k, a);
            var result = ContactWorkImpulse.FromStore(v, Target, k, a, Preload, Up);
            if (result.Debit == 0f) continue;
            var impulse = result.Impulse.GetElement(1);
            var delivered = Delivered(v, k, impulse);
            if (result.Remaining + result.Debit != a) failures.Add($"T2 closure v={v} k={k} a={a}: {result.Remaining}+{result.Debit}");
            // Slack: two f32 ULPs of the debit (the f32 evaluation of the same work). Beyond it, the impulse delivers energy
            // the store never paid for.
            if (delivered - result.Debit > 2 * (MathF.BitIncrement(result.Debit) - result.Debit)) failures.Add($"T3 under-debit v={v} k={k} a={a}: debit {result.Debit} < delivered {delivered}");
            if (result.Debit < payment.Debit) failures.Add($"T4 single-ULP correction insufficient v={v} k={k} a={a}: {result.Debit} < {payment.Debit}");
            var excess = (result.Debit - delivered) / a;
            if (payment.Limit == ContactWorkLimit.Budget) worstBudgetExcess = Math.Max(worstBudgetExcess, excess);
            else worstTargetExcess = Math.Max(worstTargetExcess, excess);
        }
        // The dissipated share D3 accepts: bounded by the 4-step impulse rounding (about 8 f32 ULPs of relative work),
        // never a visible fraction of the store. Both maxima are printed in the failure so the review quantifies D3.
        const double Limit = 8 * 1.1920929e-7;
        if (worstBudgetExcess > Limit) failures.Add($"T3 budget-limited over-debit up to {worstBudgetExcess:E3} of the store");
        if (worstTargetExcess > Limit) failures.Add($"T3 target-limited over-debit up to {worstTargetExcess:E3} of the store");
        Assert.True(failures.Count == 0, $"{failures.Count} ledger violations; first: {string.Join(" | ", failures.GetRange(0, Math.Min(5, failures.Count)))}");
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
        var result = ContactWorkImpulse.FromStore(v, Target, k, a, Preload, Up);
        Assert.True(float.IsFinite(result.Debit) && result.Debit >= 0f, $"debit {result.Debit}");
        Assert.True(float.IsFinite(result.Impulse.GetElement(1)) && result.Impulse.GetElement(1) >= 0f, $"impulse {result.Impulse}");
        if (float.IsFinite(a) && a > 0)
        {
            Assert.True(float.IsFinite(result.Remaining) && result.Remaining >= 0f, $"remaining {result.Remaining}");
            Assert.True(result.Debit <= a, $"debit {result.Debit} exceeds the store {a}");
        }
        else Assert.Equal(0f, result.Debit); // a non-finite or non-positive store pays nothing
    }

    [Theory]
    [InlineData(.25f, 1)]
    [InlineData(1f / .6f, 2)]
    [InlineData(1f, 3)]
    public void ManyPayoutsFromOneStoreCloseExactlyAndStopAtExhaustion(float k, int seed)
    {
        var random = new Random(seed);
        var store = Preload; double paidTotal = 0; var exhausted = false; var payouts = 0;
        for (var contact = 0; contact < 400; contact++)
        {
            // The first contact is a drop; later ones return near the 8 m/s launch speed, so they are small top-ups.
            var v = contact == 0 ? 7.5f : (float)(7.7 + random.NextDouble() * .29);
            var r = ContactWorkImpulse.FromStore(v, Target, k, store, Preload, Up);
            Assert.True(r.Remaining >= 0f && r.Remaining <= store, $"contact {contact}: store {store} -> {r.Remaining}");
            Assert.Equal(store, r.Remaining + r.Debit);
            if (exhausted) Assert.Equal(0f, r.Debit);
            if (r.Debit > 0f) { payouts++; Assert.True(r.Debit >= Dust || store <= Dust, $"contact {contact}: dust payout {r.Debit:R} from store {store:R}"); }
            paidTotal += r.Debit;
            store = r.Remaining;
            Assert.False(store > 0f && store <= Dust, $"contact {contact}: store left as dust {store:R}");
            if (store == 0f) exhausted = true;
        }
        Assert.True(payouts >= 3 && exhausted, $"probe incomplete: {payouts} payouts, exhausted={exhausted}");
        Assert.InRange(Math.Abs(paidTotal + store - Preload), 0, 400 * 2e-6);
    }
}
