using CuriousContraptions.Bundle;
using System;
using System.Collections.Generic;
using System.Linq;
using Google.FlatBuffers;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopSavedConstruction(WorkshopConstruction Construction, GpuBodyId NextBodyId);

/// <summary>Construction-only storage boundary; shares the one construction record codec with GPU admission. The game and its save
/// ship in one bundle with one schema (owner decision, 21 Oct 2026): a save is one size-prefixed FlatBuffers root of the schema's
/// root type, carrying the <see cref="WireRootIdentifier.SavedConstruction"/> identifier, and stored bytes that are not an
/// admissible save of this schema are refused structurally, never migrated.</summary>
public static class WorkshopSaveCodec
{
    public static byte[] Encode(ISupplyQueries supply, IBundleServices bundle, WorkshopSavedConstruction save)
    {
        // The committed construction is valid by invariant: a failure here is a defect, never a refusal.
        save.Construction.Validate(supply, bundle);
        if (!AllocatorIsValid(save)) throw new ArgumentException("Saved construction allocator is invalid.");
        var builder = new FlatBufferBuilder(2048);
        var record = WorkshopAuthoringFlatBuffers.EncodeConstructionRecord(supply, bundle, builder, save.Construction);
        SavedConstruction.StartSavedConstruction(builder);
        SavedConstruction.AddNextBodyId(builder, save.NextBodyId.Value);
        SavedConstruction.AddConstruction(builder, record);
        SavedConstruction.FinishSizePrefixedSavedConstructionBuffer(builder, SavedConstruction.EndSavedConstruction(builder));
        return builder.SizedByteArray();
    }

    /// <summary>The browser store's text (untrusted): base64 of one save, validated against the loaded bundle's
    /// <paramref name="elements"/>.</summary>
    public static WorkshopSavedConstruction DecodeStored(ICommandCodec codec, ISupplyQueries supply, IBundleServices bundle, IReadOnlySet<PartId> elements, string stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        var bytes = new byte[stored.Length];
        if (!Convert.TryFromBase64String(stored, bytes, out var length))
            throw new WorkshopSaveRejectedException(WorkshopSaveRejection.NotBase64, "This save cannot be loaded: the stored text is not a save.");
        return Decode(codec, supply, bundle, elements, bytes.AsSpan(0, length));
    }

    /// <summary>Untrusted stored bytes. The save frame (size prefix, identifier and the verifier, which refuses a field the schema
    /// does not define in any table) and then structural pre-validation, against the loaded bundle's <paramref name="elements"/>,
    /// are the only refusals (<see cref="WorkshopSaveRejectedException"/>). Domain construction then runs on an admitted record,
    /// and any exception it raises is a defect that escapes as itself.</summary>
    public static WorkshopSavedConstruction Decode(ICommandCodec codec, ISupplyQueries supply, IBundleServices bundle, IReadOnlySet<PartId> elements, ReadOnlySpan<byte> bytes)
    {
        var saved = Open(codec, bytes);
        WorkshopSavePrevalidation.Check(supply, saved, elements);
        return Construct(supply, bundle, saved);
    }

    /// <summary>The verified save root. Core's frame boundary admits the frame through <see cref="ICommandCodec.ValidateSaveFrame"/>,
    /// and a frame fault is the save's typed refusal naming the same reason; the root is then read from the size-stripped buffer.</summary>
    internal static SavedConstruction Open(ICommandCodec codec, ReadOnlySpan<byte> bytes)
    {
        try { codec.ValidateSaveFrame(bytes); }
        catch (WireFaultException fault)
        {
            var reason = fault.Reason switch
            {
                WireFaultReason.Truncated => WorkshopSaveRejection.Truncated,
                WireFaultReason.TrailingBytes => WorkshopSaveRejection.TrailingBytes,
                WireFaultReason.UnknownIdentifier => WorkshopSaveRejection.UnknownIdentifier,
                WireFaultReason.VerifierRejected => WorkshopSaveRejection.VerifierRejected,
                _ => throw new InvalidOperationException($"The save frame cannot fault with {fault.Reason}.", fault),
            };
            throw new WorkshopSaveRejectedException(reason, "This save cannot be loaded: " + fault.Message, fault);
        }
        return SavedConstruction.GetRootAsSavedConstruction(ByteBufferUtil.RemoveSizePrefix(new ByteBuffer(bytes.ToArray())));
    }

    /// <summary>Domain construction of a pre-validated save. Nothing here is a refusal: the domain's argument guards are
    /// programmer-error faults.</summary>
    internal static WorkshopSavedConstruction Construct(ISupplyQueries supply, IBundleServices bundle, SavedConstruction saved)
    {
        var record = saved.Construction ?? throw new InvalidOperationException("A pre-validated save holds its construction.");
        var save = new WorkshopSavedConstruction(WorkshopAuthoringFlatBuffers.MapConstructionRecord(record), new(saved.NextBodyId));
        save.Construction.Validate(supply, bundle);
        if (!AllocatorIsValid(save)) throw new InvalidOperationException("A pre-validated save's allocator is invalid.");
        return save;
    }

    private static bool AllocatorIsValid(WorkshopSavedConstruction save) =>
        save.NextBodyId.Value != 0 && save.Construction.Instances.All(instance => save.NextBodyId.Value > instance.Id.Value);
}
