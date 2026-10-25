namespace CuriousContraptions.Tests;

/// <summary>Test inputs that prove a lane carries full f32 precision (Story 9A.44).</summary>
internal static class TestPrecision
{
    /// <summary>Whether a finite f32 needs more than an 11-bit significand (a half-precision lane keeps 10 explicit mantissa bits): some bit of its
    /// 13 low mantissa bits is set, so any narrower lane would change it.</summary>
    public static bool NeedsMoreThanElevenSignificantBits(float value) =>
        float.IsFinite(value) && (BitConverter.SingleToUInt32Bits(value) & 0x1FFFu) != 0;
}
