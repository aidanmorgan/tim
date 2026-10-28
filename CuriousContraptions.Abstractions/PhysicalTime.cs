using System;

namespace CuriousContraptions.Abstractions;

/// <summary>A view of the existing clock: an absolute 480 Hz ordinal and its bounded f32 phase.
/// Subtract anchors before evaluating local seconds; reduce complete periods before evaluating a loop.</summary>
public readonly record struct PhysicalTime : IComparable<PhysicalTime>
{
    public const uint Frequency = 480;
    public const uint UnitsPerOrdinal = 4096;
    public ulong Ordinal { get; }
    public float Phase { get; }

    public PhysicalTime(ulong ordinal, float phase = 0)
    {
        if (!PhysicalPhase.IsAdmitted(ordinal, phase))
            throw new ArgumentOutOfRangeException(nameof(phase));
        Ordinal = ordinal;
        Phase = phase;
    }

    /// <summary>Convert an integral/rational clock sample, rounding only its bounded local phase.</summary>
    public static PhysicalTime FromRatio(UInt128 secondsNumerator, ulong secondsDenominator)
    {
        ArgumentOutOfRangeException.ThrowIfZero(secondsDenominator);
        var scaled = checked(secondsNumerator * Frequency);
        var ordinal = checked((ulong)(scaled / secondsDenominator));
        var remainder = scaled % secondsDenominator;
        var phase = (float)remainder / secondsDenominator * UnitsPerOrdinal;
        if (phase >= PhysicalPhase.Limit)
        {
            ordinal = checked(ordinal + 1);
            phase -= UnitsPerOrdinal;
        }
        return new(ordinal, phase);
    }

    public static PhysicalTime FromNanoseconds(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nanoseconds);
        return FromRatio((ulong)nanoseconds, 1_000_000_000);
    }

    public PhysicalTime AddElapsed(PhysicalTime from, PhysicalTime to)
    {
        var ordinal = (Int128)Ordinal + (Int128)to.Ordinal - from.Ordinal;
        var phase = Phase + to.Phase - from.Phase;
        var carry = (int)MathF.Floor((phase + PhysicalPhase.Limit) / UnitsPerOrdinal);
        ordinal += carry;
        phase -= carry * UnitsPerOrdinal;
        return new(checked((ulong)ordinal), phase);
    }

    public float SecondsSince(PhysicalTime anchor) =>
        ((float)((Int128)Ordinal - anchor.Ordinal) + (Phase - anchor.Phase) / UnitsPerOrdinal) / Frequency;

    /// <summary>Reduce whole elapsed seconds by integer bits before adding the bounded local phase.
    /// Evaluation is stateless and allocation-free, even after a long-running session.</summary>
    public float PeriodicPhaseSince(PhysicalTime anchor, float duration, float rate, uint periods = 1)
    {
        if (!float.IsFinite(duration) || duration <= 0 || !float.IsFinite(rate) || periods is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(duration));
        var elapsedOrdinals = (Int128)Ordinal - anchor.Ordinal;
        var seconds = elapsedOrdinals / Frequency;
        var localSeconds = ((float)(elapsedOrdinals % Frequency) +
            (Phase - anchor.Phase) / UnitsPerOrdinal) / Frequency;
        var carry = (int)MathF.Floor(localSeconds);
        seconds += carry;
        localSeconds -= carry;
        var periodSeconds = duration * periods;
        // When the doubled duration exceeds f32, every finite rate is already within this period.
        // This is range reduction in the same algorithm, without an overflowing product.
        float Reduce(float value) => float.IsPositiveInfinity(periodSeconds)
            ? value / duration : (value % periodSeconds) / duration;
        var contribution = Reduce(rate) * (seconds < 0 ? -1 : 1);
        var count = checked((ulong)Int128.Abs(seconds));
        var phase = 0f;
        while (count != 0)
        {
            if ((count & 1) != 0) phase = (phase + contribution) % periods;
            count >>= 1;
            contribution = (contribution + contribution) % periods;
        }
        phase = (phase + Reduce(localSeconds * rate)) % periods;
        if (phase < 0) phase += periods;
        return phase >= periods ? 0 : phase;
    }

    public static PhysicalTime Max(PhysicalTime left, PhysicalTime right) => left >= right ? left : right;

    public int CompareTo(PhysicalTime other)
    {
        var ordinal = Ordinal.CompareTo(other.Ordinal);
        return ordinal != 0 ? ordinal : Phase.CompareTo(other.Phase);
    }
    public static bool operator <(PhysicalTime left, PhysicalTime right) => left.CompareTo(right) < 0;
    public static bool operator >(PhysicalTime left, PhysicalTime right) => left.CompareTo(right) > 0;
    public static bool operator <=(PhysicalTime left, PhysicalTime right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PhysicalTime left, PhysicalTime right) => left.CompareTo(right) >= 0;
}
