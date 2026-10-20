using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>The response's contact-work section: the stores, then the occurrence rows, each exactly its population (Story 9R.5b:
/// no fixed work or occurrence capacity). Occurrence work and collider slots are u32.</summary>
internal static class WorkshopContactWorkWire
{
    internal const int StoreBytes = 32;
    internal const int OccurrenceBytes = 40;
    internal static int ByteLength(int stores, int occurrences) =>
        WorkshopWire.Width((long)stores * StoreBytes + (long)occurrences * OccurrenceBytes);
    internal static void Write(PhysicsContactWorkRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength(read.Count, read.OccurrenceCount)) throw new ArgumentException("Invalid contact-work read width.");
        bytes.Clear();
        var occurrencesOffset = read.Count * StoreBytes;
        for (var i=0; i<read.Count; i++)
        {
            var value=read[i]; value.Validate(); var slot=bytes.Slice(i*StoreBytes,StoreBytes);
            U64(slot,0,value.Id.Value); U64(slot,8,value.Owner.Value);
            U32(slot,16,value.OccurrenceCount); BinaryPrimitives.WriteSingleLittleEndian(slot[20..],value.RemainingEnergy.Value);
            BinaryPrimitives.WriteSingleLittleEndian(slot[24..],value.SuppliedEnergy.Value);
        }
        for (var i=0; i<read.OccurrenceCount; i++)
        {
            var value=read.Occurrence(i); value.Validate();
            var slot=bytes.Slice(occurrencesOffset+i*OccurrenceBytes,OccurrenceBytes);
            U32(slot,0,value.Work.Value); U32(slot,4,value.Collider.Value);
            U64(slot,8,value.Target.Value); U32(slot,16,value.Sequence); U32(slot,20,value.EventOrdinal);
            H(slot,24,value.EventPhase); BinaryPrimitives.WriteSingleLittleEndian(slot[26..],value.ApproachSpeed.Value); BinaryPrimitives.WriteSingleLittleEndian(slot[30..],value.Debit.Value);
            slot[34]=checked((byte)value.Effect);
        }
    }
    internal static PhysicsContactWorkRead Read(ReadOnlySpan<byte> bytes, int count, int occurrences)
    {
        if (count < 0 || occurrences < 0 || bytes.Length != ByteLength(count, occurrences))
            throw new ArgumentException("Invalid contact-work population or padding.");
        var values=new ContactWorkRead[count];
        var events=new ContactWorkOccurrence[occurrences];
        var occurrencesOffset = count * StoreBytes;
        for (var i=0; i<count; i++)
        {
            var slot=bytes.Slice(i*StoreBytes,StoreBytes);
            if (slot[28..].IndexOfAnyExcept((byte)0)>=0) throw new ArgumentException("Invalid work-store padding.");
            values[i]=new(new(R64(slot,0)),new(R64(slot,8)),R32(slot,16),new(BinaryPrimitives.ReadSingleLittleEndian(slot[20..])),new(BinaryPrimitives.ReadSingleLittleEndian(slot[24..])));
        }
        for (var i=0; i<occurrences; i++)
        {
            var slot=bytes.Slice(occurrencesOffset+i*OccurrenceBytes,OccurrenceBytes);
            if (slot[35..].IndexOfAnyExcept((byte)0)>=0) throw new ArgumentException("Invalid work-occurrence padding.");
            events[i]=new(new(R32(slot,0)),new(R32(slot,4)),
                new(R64(slot,8)),R32(slot,16),R32(slot,20),RH(slot,24),new(BinaryPrimitives.ReadSingleLittleEndian(slot[26..])),new(BinaryPrimitives.ReadSingleLittleEndian(slot[30..])),(ContactWorkEffect)slot[34]);
        }
        return PhysicsContactWorkRead.Adopt(values,events);
    }
    private static void U64(Span<byte> bytes,int offset,ulong value)=>BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..],value);
    private static void U32(Span<byte> bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..],value);
    private static ulong R64(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
    private static uint R32(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static void H(Span<byte> bytes,int offset,Half value)=>BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],BitConverter.HalfToUInt16Bits(value));
    private static Half RH(ReadOnlySpan<byte> bytes,int offset)=>BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
}
