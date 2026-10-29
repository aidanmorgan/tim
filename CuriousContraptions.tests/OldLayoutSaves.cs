using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>Raw malformed-table controls and the pre-Domino fixtures still used by boundary tests.</summary>
internal static class OldLayoutSaves
{
    /// <summary>One old table layout and the construction values its pinned saves were written with.</summary>
    internal sealed record Layout(int FieldCount, int ParametersSlot, int CellY, ulong NextBodyId)
    {
        /// <summary>The 14-field table of 2065d3c5 (before Story 9A.32): 6 mass, 7 bounce, 8 length, 9 threshold, 10 half_extents,
        /// 11 friction, 12 target_count, 13 parameters.</summary>
        public static Layout PreDomino { get; } = new(14, 13, 16, 2);
    }

    /// <summary>One f32/u32 scalar at an old slot.</summary>
    internal readonly record struct Slot(int Index, float Value = 0f, uint? Unsigned = null);

    internal static byte[] Save(Layout layout, string part, IReadOnlyList<Slot> slots, IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        return SaveFixture.Finish(builder, Record(builder, layout, part, slots, parameters), layout.NextBodyId);
    }

    /// <summary>The same old-layout construction carried by a Construct command (the worker's command decode boundary).</summary>
    internal static byte[] Command(Layout layout, string part, IReadOnlyList<Slot> slots, IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        var bytes = new byte[RuntimeSessionId.ByteLength];
        new RuntimeSessionId(1, 2).WriteTo(bytes);
        var session = Wire.WorkshopResponse.CreateSessionVectorBlock(builder, bytes);
        var record = Record(builder, layout, part, slots, parameters);
        builder.StartTable(12);
        Wire.WorkshopCommand.AddSequence(builder, 1);
        Wire.WorkshopCommand.AddKind(builder, CommandKind.Construct);
        Wire.WorkshopCommand.AddEpoch(builder, 1);
        Wire.WorkshopCommand.AddRevision(builder, 1);
        Wire.WorkshopCommand.AddRevisionKind(builder, (uint)ExpectedRevisionKind.Exact);
        Wire.WorkshopCommand.AddSession(builder, session);
        Wire.WorkshopCommand.AddCadence(builder, 1);
        Wire.WorkshopCommand.AddProjection(builder, 1);
        builder.AddOffset(11, record.Value, 0);
        builder.FinishSizePrefixed(Wire.WorkshopCommand.EndWorkshopCommand(builder).Value, WorkshopWireFlatBuffers.CommandIdentifier);
        return builder.SizedByteArray();
    }

    private static Offset<ConstructionRecord> Record(FlatBufferBuilder builder, Layout layout, string part, IReadOnlyList<Slot> slots,
        IReadOnlyList<(string Key, float Value)>? parameters)
    {
        var name = builder.CreateString(part);
        VectorOffset? parameterVector = null;
        if (parameters is not null)
            parameterVector = InstanceRecord.CreateParametersVector(builder, parameters.Select(p =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(p.Key), p.Value)).ToArray());
        builder.StartTable(layout.FieldCount);
        if (parameterVector is { } vector) builder.AddOffset(layout.ParametersSlot, vector.Value, 0);
        builder.AddUlong(0, 1, 0);
        builder.AddOffset(1, name.Value, 0);
        builder.AddBool(2, false, false);
        builder.AddStruct(3, Vec3i.CreateVec3i(builder, 0, layout.CellY, 0).Value, 0);
        builder.AddStruct(4, Vec3f.CreateVec3f(builder, 0, 0, 0).Value, 0);
        builder.AddStruct(5, Quat4f.CreateQuat4f(builder, 0, 0, 0, 1).Value, 0);
        foreach (var slot in slots)
        {
            if (slot.Unsigned is { } u) builder.AddUint(slot.Index, u, 0);
            else builder.AddFloat(slot.Index, slot.Value, 0);
        }
        var instance = new Offset<InstanceRecord>(builder.EndTable());
        var instances = ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, 1);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)settings.Simulation,
            (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator,
            settings.PresentationRate.Denominator));
        ConstructionRecord.AddInstances(builder, instances);
        return ConstructionRecord.EndConstructionRecord(builder);
    }

    /// <summary>One old record: its element and the old slots and parameters it stored.</summary>
    internal sealed record OldRecord(string Name, string Part, Slot[] Slots, (string Key, float Value)[] Parameters)
    {
        public required Layout Layout { get; init; }
        public byte[] Save() => OldLayoutSaves.Save(Layout, Part, Slots, Parameters);
        public byte[] Command() => OldLayoutSaves.Command(Layout, Part, Slots, Parameters);
    }

    /// <summary>2065d3c5 records whose parameters vector (slot 13) lies beyond the current 9-field table: a zero-parameter domino and
    /// lamp each carrying an undeclared parameter, and a bare Basketball with the empty parameters vector the base encoder wrote for
    /// every generic instance.</summary>
    internal static IEnumerable<OldRecord> PreDominoRecords()
    {
        var layout = Layout.PreDomino;
        yield return new("domino with a parameter", "domino", [], [("mass", .5f)]) { Layout = layout };
        yield return new("lamp with a parameter", "lamp", [], [("radius", .5f)]) { Layout = layout };
        yield return new("bare ball, empty parameters vector", "ball", [], []) { Layout = layout };
    }

    /// <summary>The old Domino record's material as its encoder stored it: the half-precision-rounded 0.125 × 0.55 × 0.325 m, 0.4 kg,
    /// bounce 0.05 and friction 0.6, widened to f32 (exact constants, so the old bytes depend on no current narrowing).</summary>
    internal static readonly System.Numerics.Vector3 OldDominoHalfExtents = new(.125f, .5498046875f, .324951171875f);
    internal const float OldDominoMass = .39990234375f, OldDominoBounce = .04998779296875f, OldDominoFriction = .60009765625f;

}
