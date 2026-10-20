using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>The response's activation section: exactly its population (Story 9R.5b: no fixed row capacity).</summary>
internal static class WorkshopActivationWire
{
    internal const int RecordBytes = 96;
    internal static int ByteLength(int count) => WorkshopWire.Width((long)count * RecordBytes);
    internal static void Write(PhysicsActivationRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength(read.Count)) throw new ArgumentException("Invalid activation read width.");
        bytes.Clear();
        for (var i = 0; i < read.Count; i++)
        {
            var value = read[i]; value.Validate(); var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            U64(slot, 0, value.Node.Value); U64(slot, 8, value.Owner.Value); U64(slot, 16, value.Source.Value);
            U64(slot, 24, value.Body.Value); U64(slot, 32, value.Collider.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[40..], (uint)value.Phase);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[44..], value.EventOrdinal);
            H(slot, 48, value.EventPhase); H(slot, 50, value.ApproachSpeed.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[52..], (uint)value.Kind); U64(slot, 56, value.Emitter.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[64..], value.CauseOrdinal); H(slot, 68, value.CausePhase);
        }
    }
    internal static PhysicsActivationRead Read(ReadOnlySpan<byte> bytes, int count)
    {
        if (count < 0 || bytes.Length != ByteLength(count)) throw new ArgumentException("Invalid activation count or padding.");
        var values = new ActivationLatch[count];
        for (var i = 0; i < count; i++)
        {
            var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            if (slot[70..].IndexOfAnyExcept((byte)0) >= 0) throw new ArgumentException("Invalid activation padding.");
            values[i] = new(new(R64(slot, 0)), new(R64(slot, 8)), (ActivationPhase)BinaryPrimitives.ReadUInt32LittleEndian(slot[40..]),
                new(R64(slot, 16)), new(R64(slot, 24)), new(R64(slot, 32)), BinaryPrimitives.ReadUInt32LittleEndian(slot[44..]),
                RH(slot, 48), new(RH(slot, 50)), (ActivationOccurrenceKind)BinaryPrimitives.ReadUInt32LittleEndian(slot[52..]),
                new(R64(slot, 56)), BinaryPrimitives.ReadUInt32LittleEndian(slot[64..]), RH(slot, 68));
        }
        return new(values);
    }
    private static void U64(Span<byte> value, int offset, ulong number) => BinaryPrimitives.WriteUInt64LittleEndian(value[offset..], number);
    private static ulong R64(ReadOnlySpan<byte> value, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(value[offset..]);
    private static void H(Span<byte> value, int offset, Half number) => BinaryPrimitives.WriteUInt16LittleEndian(value[offset..], BitConverter.HalfToUInt16Bits(number));
    private static Half RH(ReadOnlySpan<byte> value, int offset) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(value[offset..]));
}
