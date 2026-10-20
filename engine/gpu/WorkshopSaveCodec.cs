using System;
using System.Linq;
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

    public static byte[] Encode(WorkshopSavedConstruction save)
    {
        // The committed construction is valid by invariant: a failure here is a defect, never a refusal.
        save.Construction.Validate();
        if (!AllocatorIsValid(save)) throw new ArgumentException("Saved construction allocator is invalid.");
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

    /// <summary>Untrusted stored bytes. A malformed save is the player's refusal (UnsupportedSave, with the failure as its
    /// cause); a rule's own rejection keeps its reason; any other exception is a defect and escapes as one.</summary>
    public static WorkshopSavedConstruction Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16) throw new WorkshopEditRejectedException(WorkshopEditRejection.UnsupportedSave, "Unsupported construction save.");
        var save = Read(bytes);
        try { save.Construction.Validate(); }
        // The structural validators' declared input rejection: the stored construction is not admissible.
        catch (Exception error) when (IsInvalidStoredValue(error))
        {
            throw new WorkshopEditRejectedException(WorkshopEditRejection.UnsupportedSave, error.Message, error);
        }
        if (!AllocatorIsValid(save))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.UnsupportedSave, "Saved construction allocator is invalid.");
        return save;
    }

    // The FlatBuffers reader and the record decoder: only here are the reader's bounds failures a malformed save.
    private static WorkshopSavedConstruction Read(ReadOnlySpan<byte> bytes)
    {
        var bb = new ByteBuffer(bytes.ToArray());
        try
        {
            var saved = SavedConstruction.GetRootAsSavedConstruction(bb);
            if (saved.Magic != Magic || saved.Version != Version || !saved.Construction.HasValue)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.UnsupportedSave, "Unsupported construction save.");
            return new(WorkshopWireFlatBuffers.DecodeConstructionRecord(saved.Construction.Value), new(saved.NextBodyId));
        }
        catch (Exception error) when (IsMalformedRecord(error))
        {
            throw new WorkshopEditRejectedException(WorkshopEditRejection.UnsupportedSave,
                error.GetType() == typeof(ArgumentException) ? error.Message : "Unsupported construction save.", error);
        }
    }

    /// <summary>The structural validators' declared input rejection of a stored value: exactly ArgumentException, or
    /// ArgumentOutOfRangeException for an out-of-range field. Anything else from validation is a defect.</summary>
    internal static bool IsInvalidStoredValue(Exception error) =>
        error.GetType() == typeof(ArgumentException) || error.GetType() == typeof(ArgumentOutOfRangeException);

    /// <summary>What malformed record bytes produce while reading: the decoder's input rejection and the FlatBuffers
    /// reader's own bounds failures (IndexOutOfRangeException, OverflowException), only inside Read.</summary>
    internal static bool IsMalformedRecord(Exception error) =>
        IsInvalidStoredValue(error) || error.GetType() == typeof(IndexOutOfRangeException) || error.GetType() == typeof(OverflowException);

    private static bool AllocatorIsValid(WorkshopSavedConstruction save) =>
        save.NextBodyId.Value != 0 && save.Construction.Instances.All(instance => save.NextBodyId.Value > instance.Id.Value);
}
