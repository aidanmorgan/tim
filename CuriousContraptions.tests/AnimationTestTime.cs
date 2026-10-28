using CuriousContraptions.Abstractions;
namespace CuriousContraptions.Tests;

/// <summary>Test arrangement boundary for independently specified sample seconds.
/// Long-origin precision tests construct integer ordinals directly instead.</summary>
internal static class AnimationTestTime
{
    public static PhysicalTime Seconds(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        var steps = seconds * 480;
        var ordinal = checked((ulong)Math.Floor(steps + .5));
        var phase = (float)((steps - ordinal) * 4096);
        if (phase >= 2048) { ordinal = checked(ordinal + 1); phase -= 4096; }
        return new(ordinal, phase);
    }
}
