using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

internal static class WorkshopTimerWire
{
    internal const int RecordBytes = 128;
    internal const int ByteLength = ActivationNetwork.Capacity * RecordBytes;
    internal static void Write(PhysicsTimerRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid timer read width.");
        bytes.Clear();
        for (var i = 0; i < read.Count; i++)
        {
            var value = read[i]; value.Validate(); var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            U64(slot,0,value.Node.Value); U32(slot,8,(uint)value.Phase);
            U64(slot,16,value.StartedTick); U64(slot,24,value.DueTick);
            if (value.Phase == ActivationTimerPhase.Ready) continue;
            var input = value.Input;
            U32(slot,32,(uint)input.Kind); U32(slot,36,input.Time.Ordinal); H(slot,40,input.Time.Phase);
            H(slot,42,input.Cause.ApproachSpeed.Value); U64(slot,48,input.Emitter.Value);
            U64(slot,56,input.Cause.Source.Value); U64(slot,64,input.Cause.Body.Value);
            U64(slot,72,input.Cause.Collider.Value); U32(slot,80,input.Cause.Time.Ordinal); H(slot,84,input.Cause.Time.Phase);
        }
    }
    internal static PhysicsTimerRead Read(ReadOnlySpan<byte> bytes, uint count)
    {
        if (bytes.Length != ByteLength || count > ActivationNetwork.Capacity ||
            bytes[(checked((int)count) * RecordBytes)..].IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid timer population or padding.");
        Span<ActivationTimerState> values = stackalloc ActivationTimerState[ActivationNetwork.Capacity];
        for (var i = 0; i < count; i++)
        {
            var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            if (slot[12..16].IndexOfAnyExcept((byte)0) >= 0 || slot[44..48].IndexOfAnyExcept((byte)0) >= 0 ||
                slot[86..].IndexOfAnyExcept((byte)0) >= 0) throw new ArgumentException("Invalid timer record padding.");
            var phase = (ActivationTimerPhase)R32(slot,8);
            ActivationOccurrence input = default;
            if (phase == ActivationTimerPhase.Ready)
            {
                if (slot[32..].IndexOfAnyExcept((byte)0) >= 0) throw new ArgumentException("Ready timer retained input.");
            }
            else input = new((ActivationOccurrenceKind)R32(slot,32),new(R64(slot,48)),new(R32(slot,36),RH(slot,40)),
                new(new(R64(slot,56)),new(R64(slot,64)),new(R64(slot,72)),new(R32(slot,80),RH(slot,84)),new(RH(slot,42))));
            values[i] = new(new(R64(slot,0)),phase,R64(slot,16),R64(slot,24),input);
        }
        return new(values[..checked((int)count)]);
    }
    private static void U64(Span<byte> bytes,int offset,ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..],value);
    private static void U32(Span<byte> bytes,int offset,uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..],value);
    private static ulong R64(ReadOnlySpan<byte> bytes,int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
    private static uint R32(ReadOnlySpan<byte> bytes,int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static void H(Span<byte> bytes,int offset,Half value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],BitConverter.HalfToUInt16Bits(value));
    private static Half RH(ReadOnlySpan<byte> bytes,int offset) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
}
