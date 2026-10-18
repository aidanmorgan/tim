using System;
using Google.FlatBuffers;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public enum WorkshopSaveVersion : uint { CanonicalConstruction = 13 }
public readonly record struct WorkshopSavedConstruction(WorkshopConstruction Construction, GpuBodyId NextBodyId);

/// <summary>Construction-only storage boundary; shares the canonical declaration codec with GPU admission.</summary>
public static class WorkshopSaveCodec
{
    public const uint Magic = 0x53574343; // CCWS
    public const uint Version = (uint)WorkshopSaveVersion.CanonicalConstruction;
    public const int ByteLength = 24 + WorkshopWire.ConstructionBytes;

    public static byte[] Encode(WorkshopSavedConstruction save)
    {
        Validate(save);
        var builder = new FlatBufferBuilder(2048);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, save.Construction);
        SavedConstruction.StartSavedConstruction(builder);
        SavedConstruction.AddMagic(builder, Magic);
        SavedConstruction.AddVersion(builder, Version);
        SavedConstruction.AddNextBodyId(builder, save.NextBodyId.Value);
        SavedConstruction.AddConstruction(builder, crOffset);
        var end = SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        return builder.SizedByteArray();
    }

    public static WorkshopSavedConstruction Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16) throw new ArgumentException("Unsupported construction save.");
        var bb = new ByteBuffer(bytes.ToArray());
        SavedConstruction saved;
        try
        {
            saved = SavedConstruction.GetRootAsSavedConstruction(bb);
            if (saved.Magic != Magic || saved.Version != Version || !saved.Construction.HasValue)
                throw new ArgumentException("Unsupported construction save.");
            var construction = WorkshopWireFlatBuffers.DecodeConstructionRecord(saved.Construction.Value);
            var save = new WorkshopSavedConstruction(construction, new(saved.NextBodyId));
            Validate(save);
            return save;
        }
        catch (ArgumentException ex) when (ex.GetType() == typeof(ArgumentException))
        {
            throw;
        }
        catch
        {
            throw new ArgumentException("Unsupported construction save.");
        }
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
