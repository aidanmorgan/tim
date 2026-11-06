using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static class RuntimeSessionCodec
{
    /// <summary>The session's 16 little-endian bytes (low word, then high word) in the fixed-layout records that carry it.</summary>
    public static void WriteTo(this RuntimeSessionId session, Span<byte> destination)
    {
        session.Validate();
        if (destination.Length != RuntimeSessionId.ByteLength) throw new ArgumentException("A runtime session occupies 16 bytes.");
        BinaryPrimitives.WriteUInt64LittleEndian(destination, session.Low);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], session.High);
    }

    public static RuntimeSessionId Read(ReadOnlySpan<byte> source)
    {
        if (source.Length != RuntimeSessionId.ByteLength) throw new ArgumentException("A runtime session occupies 16 bytes.");
        var session = new RuntimeSessionId(BinaryPrimitives.ReadUInt64LittleEndian(source), BinaryPrimitives.ReadUInt64LittleEndian(source[8..]));
        session.Validate();
        return session;
    }
}
