using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static class CanonicalBodyCodec
{
    public static byte[] Encode(this CanonicalBody body)
    {
        var bytes = new byte[CanonicalBody.ByteLength];
        body.Write(bytes);
        return bytes;
    }

    public static void Write(this CanonicalBody body, Span<byte> data)
    {
        body.Validate();
        var (Id, Epoch, Tick, Cell, Local, Velocity) = body;
        if (data.Length != CanonicalBody.ByteLength) throw new ArgumentException("Invalid canonical destination length.");
        data.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(data[8..], Epoch);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..], Tick);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(data[48..], (int)CellScale.Metres);
        BinaryPrimitives.WriteInt32LittleEndian(data[52..], (int)TimeScale.Seconds);
        BinaryPrimitives.WriteInt32LittleEndian(data[32..], Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(data[36..], Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(data[40..], Cell.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[56..], Local.X); BinaryPrimitives.WriteSingleLittleEndian(data[60..], Local.Y); BinaryPrimitives.WriteSingleLittleEndian(data[64..], Local.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[68..], Velocity.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[72..], Velocity.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[76..], Velocity.Z);
    }

    public static CanonicalBody Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length != CanonicalBody.ByteLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(data) != 0 || // bytes 0..3: zero padding (one bundle, one schema)
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != (uint)BodyCandidateStatus.Committed ||
            BinaryPrimitives.ReadInt32LittleEndian(data[48..]) != (int)CellScale.Metres ||
            BinaryPrimitives.ReadInt32LittleEndian(data[52..]) != (int)TimeScale.Seconds ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[44..]) != 0)
            throw new ArgumentException("Unsupported canonical body record.");
        var body = new CanonicalBody(
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            BinaryPrimitives.ReadUInt64LittleEndian(data[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(data[16..]),
            new(BinaryPrimitives.ReadInt32LittleEndian(data[32..]), BinaryPrimitives.ReadInt32LittleEndian(data[36..]), BinaryPrimitives.ReadInt32LittleEndian(data[40..])),
            new(BinaryPrimitives.ReadSingleLittleEndian(data[56..]), BinaryPrimitives.ReadSingleLittleEndian(data[60..]), BinaryPrimitives.ReadSingleLittleEndian(data[64..])),
            new(BinaryPrimitives.ReadSingleLittleEndian(data[68..]), BinaryPrimitives.ReadSingleLittleEndian(data[72..]),
                BinaryPrimitives.ReadSingleLittleEndian(data[76..])));
        body.Validate();
        return body;
    }
}
