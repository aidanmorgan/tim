using System;

namespace CuriousContraptions.Gpu;

/// <summary>
/// Named performance.now boundary. The input is a context-local binary64 millisecond reading.
/// Exact significand arithmetic rounds once to integral nanoseconds, nearest with ties to even.
/// The configured native error plus this at-most-half-nanosecond conversion remains unqualified
/// until actual platform evidence establishes the declared total 100,000 ns profile.
/// </summary>
public static class WorkshopNativeClock
{
    public static MonotonicNanoseconds FromMilliseconds(double milliseconds)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds));
        var bits = BitConverter.DoubleToUInt64Bits(milliseconds);
        var exponent = (int)((bits >> 52) & 0x7ff);
        var significand = bits & 0x000f_ffff_ffff_ffffUL;
        if (exponent != 0) significand |= 1UL << 52;
        if (significand == 0) return new(0);
        var power = exponent == 0 ? -1074 : exponent - 1023 - 52;
        // Any nonzero normal reading with this exponent exceeds the I64 nanosecond range.
        if (power >= 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        UInt128 numerator = (UInt128)significand * 1_000_000;
        var shift = -power;
        if (shift >= 128) return new(0);
        var denominator = (UInt128)1 << shift;
        var quotient = numerator >> shift;
        var remainder = numerator & (denominator - 1);
        var twice = remainder * 2;
        if (twice > denominator || (twice == denominator && (quotient & 1) != 0)) quotient++;
        if (quotient > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        return new((long)quotient);
    }
}
