using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

internal static class WorkshopContactWorkWire
{
    internal const int RecordBytes = 64;
    internal const int ByteLength = PhysicsSceneDeclaration.ContactWorkCapacity * RecordBytes;
    internal static void Write(PhysicsContactWorkRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid contact-work read width.");
        bytes.Clear();
        for (var i = 0; i < read.Count; i++)
        {
            var value = read[i]; value.Validate(); var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            U64(slot, 0, value.Id.Value); U64(slot, 8, value.Owner.Value); U64(slot, 16, value.Target.Value);
            U32(slot, 24, value.OccurrenceCount); U32(slot, 28, value.EventOrdinal); U64(slot, 32, value.Collider.Value);
            H(slot, 40, value.EventPhase); H(slot, 42, value.ApproachSpeed.Value);
            H(slot, 44, value.RemainingEnergy.Value); H(slot, 46, value.LastDebit.Value);
        }
    }
    internal static PhysicsContactWorkRead Read(ReadOnlySpan<byte> bytes, uint count)
    {
        if (bytes.Length != ByteLength || count > PhysicsSceneDeclaration.ContactWorkCapacity ||
            bytes[(checked((int)count) * RecordBytes)..].IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid contact-work population or padding.");
        Span<ContactWorkRead> values = stackalloc ContactWorkRead[PhysicsSceneDeclaration.ContactWorkCapacity];
        for (var i = 0; i < count; i++)
        {
            var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            if (slot[48..].IndexOfAnyExcept((byte)0) >= 0) throw new ArgumentException("Invalid contact-work padding.");
            values[i] = new(new(R64(slot, 0)), new(R64(slot, 8)), new(R64(slot, 16)), R32(slot, 24),
                new(R64(slot, 32)), R32(slot, 28), RH(slot, 40), new(RH(slot, 42)), new(RH(slot, 44)), new(RH(slot, 46)));
        }
        return new(values[..checked((int)count)]);
    }
    private static void U64(Span<byte> bytes, int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..], value);
    private static void U32(Span<byte> bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);
    private static ulong R64(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
    private static uint R32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static void H(Span<byte> bytes, int offset, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..], BitConverter.HalfToUInt16Bits(value));
    private static Half RH(ReadOnlySpan<byte> bytes, int offset) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
}
