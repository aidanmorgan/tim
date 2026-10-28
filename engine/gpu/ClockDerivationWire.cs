using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Integral nanosecond interop: three signed stamps, u32 epoch and zero padding.
/// Corrections use an i64 plus the existing closed outcome selector and zero padding.</summary>
public static class ClockDerivationWire
{
    public const int ExchangeBytes = 32;
    public const int CorrectionBytes = 16;
    public static ClockDerivationExchange DecodeExchange(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ExchangeBytes || BinaryPrimitives.ReadUInt32LittleEndian(bytes[28..]) != 0)
            throw new ArgumentException("Unsupported clock derivation exchange.");
        var result = new ClockDerivationExchange(BinaryPrimitives.ReadInt64LittleEndian(bytes),
            BinaryPrimitives.ReadInt64LittleEndian(bytes[8..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[16..]),
            new(BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..])));
        _ = WorkshopClockDerivation.IsNarrow(result);
        return result;
    }
    public static byte[] EncodeCorrection(long correction, ClockDerivationOutcome outcome)
    {
        if (outcome is not (ClockDerivationOutcome.Consistent or ClockDerivationOutcome.Slewed or ClockDerivationOutcome.Rederived))
            throw new ArgumentOutOfRangeException(nameof(outcome));
        var bytes = new byte[CorrectionBytes];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, correction);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), (uint)outcome);
        return bytes;
    }
}
