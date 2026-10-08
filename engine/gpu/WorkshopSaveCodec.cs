using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum WorkshopSaveVersion : uint { CanonicalConstruction = 9 }
public readonly record struct WorkshopSavedConstruction(WorkshopConstruction Construction, GpuBodyId NextBodyId);

/// <summary>Construction-only storage boundary; shares the canonical declaration codec with GPU admission.</summary>
public static class WorkshopSaveCodec
{
    private const uint Magic = 0x53574343; // CCWS
    private const int HeaderBytes = 24;
    public const int ByteLength = HeaderBytes + WorkshopWire.ConstructionBytes;

    public static byte[] Encode(WorkshopSavedConstruction save)
    {
        Validate(save);
        var bytes = new byte[ByteLength];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)WorkshopSaveVersion.CanonicalConstruction);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), ByteLength);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), save.NextBodyId.Value);
        WorkshopWire.WriteConstruction(bytes.AsSpan(HeaderBytes), save.Construction);
        return bytes;
    }

    public static WorkshopSavedConstruction Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteLength || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic ||
            (WorkshopSaveVersion)BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]) != WorkshopSaveVersion.CanonicalConstruction ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]) != ByteLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]) != 0)
            throw new ArgumentException("Unsupported construction save.");
        var save = new WorkshopSavedConstruction(WorkshopWire.ReadConstruction(bytes[HeaderBytes..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[16..])));
        Validate(save);
        return save;
    }

    private static void Validate(WorkshopSavedConstruction save)
    {
        save.Construction.Validate();
        if (save.NextBodyId.Value == 0)
            throw new ArgumentException("Saved construction allocator is invalid.");
        foreach (var instance in save.Construction.Instances)
            if (save.NextBodyId.Value <= instance.Id.Value)
                throw new ArgumentException("Saved construction allocator is invalid.");
    }
}
