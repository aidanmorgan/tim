using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

internal static class WorkshopContactWorkWire
{
    internal const int StoreBytes = 32;
    internal const int OccurrenceBytes = 32;
    internal const int StoresByteLength = PhysicsSceneDeclaration.ContactWorkCapacity * StoreBytes;
    internal const int OccurrencesByteLength = PhysicsContactWorkRead.OccurrenceCapacity * OccurrenceBytes;
    internal const int ByteLength = StoresByteLength + OccurrencesByteLength;
    internal static void Write(PhysicsContactWorkRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid contact-work read width.");
        bytes.Clear();
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
            var slot=bytes.Slice(StoresByteLength+i*OccurrenceBytes,OccurrenceBytes);
            BinaryPrimitives.WriteUInt16LittleEndian(slot,value.Work.Value);
            BinaryPrimitives.WriteUInt16LittleEndian(slot[2..],value.Collider.Value);
            U64(slot,4,value.Target.Value); U32(slot,12,value.Sequence); U32(slot,16,value.EventOrdinal);
            H(slot,20,value.EventPhase); BinaryPrimitives.WriteSingleLittleEndian(slot[22..],value.ApproachSpeed.Value); BinaryPrimitives.WriteSingleLittleEndian(slot[26..],value.Debit.Value);
            slot[30]=checked((byte)value.Effect);
        }
    }
    internal static PhysicsContactWorkRead Read(ReadOnlySpan<byte> bytes, byte count, byte occurrences)
    {
        if (bytes.Length != ByteLength || count > PhysicsSceneDeclaration.ContactWorkCapacity ||
            occurrences > PhysicsContactWorkRead.OccurrenceCapacity ||
            bytes.Slice(count*StoreBytes,StoresByteLength-count*StoreBytes).IndexOfAnyExcept((byte)0)>=0 ||
            bytes[(StoresByteLength+occurrences*OccurrenceBytes)..].IndexOfAnyExcept((byte)0)>=0)
            throw new ArgumentException("Invalid contact-work population or padding.");
        Span<ContactWorkRead> values=stackalloc ContactWorkRead[PhysicsSceneDeclaration.ContactWorkCapacity];
        Span<ContactWorkOccurrence> events=stackalloc ContactWorkOccurrence[PhysicsContactWorkRead.OccurrenceCapacity];
        for (var i=0; i<count; i++)
        {
            var slot=bytes.Slice(i*StoreBytes,StoreBytes);
            if (slot[28..].IndexOfAnyExcept((byte)0)>=0) throw new ArgumentException("Invalid work-store padding.");
            values[i]=new(new(R64(slot,0)),new(R64(slot,8)),R32(slot,16),new(BinaryPrimitives.ReadSingleLittleEndian(slot[20..])),new(BinaryPrimitives.ReadSingleLittleEndian(slot[24..])));
        }
        for (var i=0; i<occurrences; i++)
        {
            var slot=bytes.Slice(StoresByteLength+i*OccurrenceBytes,OccurrenceBytes);
            if (slot[31..].IndexOfAnyExcept((byte)0)>=0) throw new ArgumentException("Invalid work-occurrence padding.");
            events[i]=new(new(BinaryPrimitives.ReadUInt16LittleEndian(slot)),new(BinaryPrimitives.ReadUInt16LittleEndian(slot[2..])),
                new(R64(slot,4)),R32(slot,12),R32(slot,16),RH(slot,20),new(BinaryPrimitives.ReadSingleLittleEndian(slot[22..])),new(BinaryPrimitives.ReadSingleLittleEndian(slot[26..])),(ContactWorkEffect)slot[30]);
        }
        return new(values[..count],events[..occurrences]);
    }
    private static void U64(Span<byte> bytes,int offset,ulong value)=>BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..],value);
    private static void U32(Span<byte> bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..],value);
    private static ulong R64(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
    private static uint R32(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static void H(Span<byte> bytes,int offset,Half value)=>BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],BitConverter.HalfToUInt16Bits(value));
    private static Half RH(ReadOnlySpan<byte> bytes,int offset)=>BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
}
