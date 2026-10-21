using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>Hand-built stored saves, framed exactly as the save codec frames one (size-prefixed "CCWS" root), and the typed
/// assertion of a save's refusal with the reason that refused it.</summary>
internal static class SaveFixture
{
    public static byte[] Finish(FlatBufferBuilder builder, Offset<ConstructionRecord> record, ulong nextBodyId)
    {
        SavedConstruction.StartSavedConstruction(builder);
        SavedConstruction.AddNextBodyId(builder, nextBodyId);
        SavedConstruction.AddConstruction(builder, record);
        builder.FinishSizePrefixed(SavedConstruction.EndSavedConstruction(builder).Value, WorkshopSaveCodec.Identifier);
        return builder.SizedByteArray();
    }

    /// <summary>A save of <paramref name="construction"/> encoded without its domain validation (a forged or tampered save).</summary>
    public static byte[] Forged(WorkshopConstruction construction, ulong nextBodyId)
    {
        var builder = new FlatBufferBuilder(1024);
        return Finish(builder, WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, construction, validate: false), nextBodyId);
    }

    public static WorkshopSaveRejectedException Refused(WorkshopSaveRejection reason, Action decode)
    {
        var refusal = Assert.Throws<WorkshopSaveRejectedException>(decode);
        Assert.Equal(reason, refusal.Reason);
        return refusal;
    }
}
