using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>The authored assistance profiles a puzzle record carries (its <c>raw_assistance</c> vector): ball, receiver and ramp
/// profiles of three knots each, every knot eleven binary16 lanes.</summary>
internal static class WorkshopPuzzleWire
{
    public const int KnotBytes = 22;
    public const int ProfileBytes = 3 * KnotBytes;
    public const int AssistanceBytes = 3 * ProfileBytes;

    internal static void WriteProfile(Span<byte> bytes, AssistanceProfile profile)
    {
        Knot(bytes[..KnotBytes], profile.Forgiving); Knot(bytes[KnotBytes..(2 * KnotBytes)], profile.Balanced); Knot(bytes[(2 * KnotBytes)..ProfileBytes], profile.Precise);
    }
    internal static AssistanceProfile ReadProfile(ReadOnlySpan<byte> bytes) =>
        new(Knot(bytes[..KnotBytes]), Knot(bytes[KnotBytes..(2 * KnotBytes)]), Knot(bytes[(2 * KnotBytes)..ProfileBytes]));
    private static void Knot(Span<byte> bytes, AssistanceKnot knot)
    {
        H(bytes, 0, knot.Precision); H(bytes, 2, knot.PositionWindow.Value); H(bytes, 4, knot.RotationWindowDegrees);
        H(bytes, 6, knot.MaximumPositionCorrection.Value); H(bytes, 8, knot.MaximumRotationCorrectionDegrees);
        H(bytes, 10, knot.Blend.Value); H(bytes, 12, knot.CaptureMargin.Value); H(bytes, 14, knot.CaptureSpeed.Value);
        H(bytes, 16, knot.CaptureDwell.Value); H(bytes, 18, knot.GuideAcceleration.Value); H(bytes, 20, knot.TriggerThreshold);
    }
    private static AssistanceKnot Knot(ReadOnlySpan<byte> bytes) => new(H(bytes, 0), new(H(bytes, 2)), H(bytes, 4),
        new(H(bytes, 6)), H(bytes, 8), new(H(bytes, 10)), new(H(bytes, 12)), new(H(bytes, 14)), new(H(bytes, 16)), new(H(bytes, 18)), H(bytes, 20));
    private static void H(Span<byte> bytes, int offset, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..], BitConverter.HalfToUInt16Bits(value));
    private static Half H(ReadOnlySpan<byte> bytes, int offset) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
}
