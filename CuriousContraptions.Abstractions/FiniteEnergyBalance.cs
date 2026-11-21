using System;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

/// <summary>One conservative finite balance in joules. Remainder belongs to the same source as Bulk;
/// it preserves small payments without a second supply or a consumer-specific credit.</summary>
public readonly record struct FiniteEnergyBalance(float Bulk, float Remainder)
{
    public float Available => Down(Bulk, Remainder);
    public float Display => Add(Bulk, Remainder);

    public void Validate(float capacity)
    {
        if (!float.IsFinite(capacity) || capacity < 0 ||
            !float.IsFinite(Bulk) || Bulk < 0 || !float.IsFinite(Remainder) || Remainder < 0 ||
            (Bulk == 0 && Remainder != 0) || Remainder > Spacing(Bulk) ||
            CompareTo(capacity) > 0)
            throw new ArgumentException("Invalid finite energy balance.");
    }

    public int CompareTo(float value)
    {
        if (!float.IsFinite(value) || value < 0)
            throw new ArgumentException("Invalid energy threshold.");
        if (Bulk > value) return 1;
        if (Bulk == value) return Remainder == 0 ? 0 : 1;
        if (Bulk == 0 || value * .5f > Bulk) return -1;
        // The remaining subtraction is exact by Sterbenz.
        return Remainder.CompareTo(Subtract(value, Bulk));
    }

    public float Headroom(float capacity)
    {
        Validate(capacity);
        return Down(Down(capacity, -Bulk), -Remainder);
    }

    public FiniteEnergyBalance Debit(float work)
    {
        if (!float.IsFinite(work) || work < 0 || work > Available)
            throw new ArgumentException("Work exceeds its finite balance.");
        if (work == 0) return this;
        var retained = MathF.Min(work, Remainder);
        var remainder = Down(Remainder, -retained);
        var bulkWork = Up(work, -retained);
        return bulkWork == 0 ? new(Bulk, remainder) : FloorSplit(Bulk, -bulkWork);
    }

    public FiniteEnergyBalance Credit(float work, float capacity)
    {
        if (!float.IsFinite(work) || work < 0 || work > Headroom(capacity))
            throw new ArgumentException("Credit exceeds finite capacity.");
        if (work == 0) return this;
        var sum = FloorSplit(Bulk, work);
        return FloorSplit(sum.Bulk, Down(Remainder, sum.Remainder));
    }

    /// <summary>Difference between represented balances; intended for a bounded transaction observation,
    /// never for recovering payment from rounded display or cumulative counters.</summary>
    public float DecreaseTo(FiniteEnergyBalance after) =>
        Add(Subtract(Bulk, after.Bulk), Subtract(Remainder, after.Remainder));

    /// <summary>Conservative accounting error envelope in joules for the fixed balance operations.
    /// Work terms are scaled before summing, so a finite transaction need not fit an unscaled aggregate.</summary>
    public static float LossBound(float capacity, float credit, float released, float dissipated, uint transactions)
    {
        const float unitRoundoff = 1f / (1 << 24);
        return (64f * unitRoundoff * unitRoundoff * capacity) * transactions +
            8f * unitRoundoff * credit + 8f * unitRoundoff * released +
            8f * unitRoundoff * dissipated + (64f * float.Epsilon) * transactions;
    }

    private static FiniteEnergyBalance FloorSplit(float a, float b)
    {
        var (sum, error) = TwoSum(a, b);
        if (error >= 0) return new(sum == 0 ? 0 : sum, error == 0 ? 0 : error);
        var bulk = MathF.BitDecrement(sum);
        return new(bulk, Down(Subtract(sum, bulk), error));
    }

    private static float Down(float a, float b)
    {
        var (sum, error) = TwoSum(a, b);
        var result = error < 0 ? MathF.BitDecrement(sum) : sum;
        return result == 0 ? 0 : result;
    }

    private static float Up(float a, float b)
    {
        var (sum, error) = TwoSum(a, b);
        var result = error > 0 ? MathF.BitIncrement(sum) : sum;
        return result == 0 ? 0 : result;
    }

    private static (float Sum, float Error) TwoSum(float a, float b)
    {
        // Callers bound the exact result to [0, MaxValue], with a >= 0 and b >= -a.
        // Separately rounded operations preserve the residual, including gradual underflow.
        var sum = Add(a, b);
        var z = Subtract(sum, a);
        var error = Add(Subtract(a, Subtract(sum, z)), Subtract(b, z));
        return (sum, error);
    }

    private static float Spacing(float value)
    {
        var exponent = MathF.ILogB(value);
        return value == 0 || exponent < -126 ? float.Epsilon : MathF.ScaleB(1f, exponent - 23);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Add(float a, float b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Subtract(float a, float b) => a - b;
}
